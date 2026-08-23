/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Records visible existing geometry scene nodes through the productive Direct3D 12 render-core paths.
/// </summary>
internal sealed class SilkD3D12SceneRenderer : IDisposable {
    /// <summary>
    ///     The shared shader-visible resource heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     The shared shader-visible sampler heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// <summary>
    ///     Host-lifetime native geometry and texture resources.
    /// </summary>
    private readonly SilkD3D12ResourceManager resources;

    /// <summary>
    ///     Per-core mesh constant buffers and descriptor tables.
    /// </summary>
    private readonly Dictionary<MeshRenderCore, SilkD3D12MeshBindings> meshBindings =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Per-core point, line, and billboard constant buffers and descriptor tables.
    /// </summary>
    private readonly Dictionary<PointLineRenderCore, SilkD3D12PointLineBindings> pointLineBindings =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Nodes attached by this renderer and detached with it.
    /// </summary>
    private readonly HashSet<SceneNode> attachedNodes = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Reused visible-node list for allocation-free per-frame selection.
    /// </summary>
    private readonly FastList<SceneNode> visibleNodes = [];

    /// <summary>
    ///     Initializes one scene renderer over host-owned descriptor heaps.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12SceneRenderer(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.AssertArgumentNotNull();
        resourceHeap.AssertArgumentNotNull();
        samplerHeap.AssertArgumentNotNull();
        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
        resources = new SilkD3D12ResourceManager(device, resourceHeap);
        Device = device;
    }

    /// <summary>
    ///     Gets the Direct3D 12 device used to allocate per-core bindings.
    /// </summary>
    private SilkD3D12Device Device { get; }

    /// <summary>
    ///     Gets the number of nodes selected by the most recent camera-frustum pass.
    /// </summary>
    internal int VisibleCount => visibleNodes.Count;

    /// <summary>
    ///     Gets whether this renderer has released its bindings and shared resources.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Selects, attaches, and records one opaque or transparent scene-node list.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="candidates">The renderable scene candidates in pass order.</param>
    /// <param name="passSelector">Selects the native pass for each scene node.</param>
    /// <param name="transforms">The current camera and viewport transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <param name="environmentMap">The optional existing environment cube map.</param>
    /// <returns>The number of recorded scene-node draws.</returns>
    internal int RenderVisible(
        SilkD3D12CommandContext context,
        FastList<SceneNode> candidates,
        Func<SceneNode, ShaderPass?> passSelector,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        LightsBufferModel? lights = null,
        TextureModel? environmentMap = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.AssertArgumentNotNull();
        candidates.AssertArgumentNotNull();
        passSelector.AssertArgumentNotNull();
        visibleNodes.Clear();
        SceneNodeFrustumSelector.AppendVisible(candidates, visibleNodes, testFrustum, ref frustum);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        var effectiveEnvironmentMap = lights is null
            ? null
            : PrepareEnvironment(context, lights, environmentMap);

        var recorded = 0;
        for (var index = 0; index < visibleNodes.Count; index++) {
            var node = visibleNodes.Items[index];
            if (!node.AttachD3D12()) continue;
            attachedNodes.Add(node);
            node.ComputeTransformMatrix();
            node.RenderCore.ModelMatrix = node.TotalModelMatrixInternal;
            var pass = passSelector(node);
            if (pass is null || pass.IsNull) continue;
            recorded += node.RenderCore switch {
                MeshRenderCore mesh => mesh.TryRenderD3D12(context,
                    resources,
                    pass,
                    GetBindings(mesh),
                    in transforms,
                    lights,
                    effectiveEnvironmentMap)
                    ? 1
                    : 0,
                PointLineRenderCore pointLine => pointLine.TryRenderD3D12(context,
                    resources,
                    pass,
                    GetBindings(pointLine),
                    in transforms)
                    ? 1
                    : 0,
                _ => 0
            };
        }
        return recorded;
    }

    /// <summary>
    ///     Loads and validates the current environment cube map and updates its shared-light metadata.
    /// </summary>
    /// <param name="context">The command context receiving a first-use upload.</param>
    /// <param name="lights">The shared light payload.</param>
    /// <param name="environmentMap">The optional existing environment texture.</param>
    /// <returns>The validated cube map, or <see langword="null" /> when none is usable.</returns>
    internal TextureModel? PrepareEnvironment(
        SilkD3D12CommandContext context,
        LightsBufferModel lights,
        TextureModel? environmentMap
    ) {
        context.AssertArgumentNotNull();
        lights.AssertArgumentNotNull();
        lights.HasEnvironmentMap = false;
        lights.EnvironmentMapMipLevels = 0;
        if (environmentMap is null) return null;
        var texture = resources.GetOrCreate(context, environmentMap);
        if (!texture.IsCubeMap) return null;
        lights.HasEnvironmentMap = true;
        lights.EnvironmentMapMipLevels = texture.Resource.Description.MipLevels;
        return environmentMap;
    }

    /// <summary>
    ///     Rebuilds the shared light payload from existing visible light nodes in scene order.
    /// </summary>
    /// <param name="nodes">The current light nodes.</param>
    /// <param name="destination">The shared destination light model.</param>
    internal static void UpdateLights(FastList<SceneNode> nodes, LightsBufferModel destination) {
        nodes.AssertArgumentNotNull();
        destination.AssertArgumentNotNull();
        destination.ResetLightCount();
        for (var index = 0; index < nodes.Count && destination.LightCount < Constants.MaxLights; index++) {
            if (nodes.Items[index] is not LightNode {Visible: true} node) continue;
            node.ComputeTransformMatrix();
            if (node is AmbientLightNode) {
                destination.AmbientLight = node.Color;
                continue;
            }

            var light = new LightStruct {
                LightType = (int) node.LightType,
                LightColor = node.Color
            };
            switch (node) {
                case DirectionalLightNode directional:
                    light.LightDir = -SilkMath.TransformNormal(directional.Direction, node.TotalModelMatrixInternal)
                        .Normalized()
                        .ToVector4(0);
                    break;
                case SpotLightNode spot:
                    ApplyPoint(ref light, spot, node.TotalModelMatrixInternal);
                    light.LightDir = SilkMath.TransformNormal(spot.Direction, node.TotalModelMatrixInternal)
                        .Normalized()
                        .ToVector4(0);
                    light.LightSpot = new Vector4((float) Math.Cos(spot.OuterAngle / 360f * Math.PI),
                        (float) Math.Cos(spot.InnerAngle / 360f * Math.PI),
                        spot.FallOff,
                        0);
                    break;
                case PointLightNode point:
                    ApplyPoint(ref light, point, node.TotalModelMatrixInternal);
                    break;
                default:
                    continue;
            }

            destination.Lights[destination.LightCount] = light;
            destination.IncrementLightCount();
        }
    }

    /// <summary>
    ///     Applies the shared point-light position, attenuation, and range fields.
    /// </summary>
    /// <param name="light">The destination light structure.</param>
    /// <param name="point">The existing point or spot light.</param>
    /// <param name="modelMatrix">The light-node world transform.</param>
    private static void ApplyPoint(ref LightStruct light, PointLightNode point, in Matrix modelMatrix) {
        light.LightPos = (point.Position + modelMatrix.Row4.ToVector3()).ToVector4();
        light.LightAtt = point.Attenuation.ToVector4(point.Range);
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one mesh core.
    /// </summary>
    /// <param name="core">The existing mesh core.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12MeshBindings GetBindings(MeshRenderCore core) {
        if (meshBindings.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12MeshBindings(Device, resourceHeap, samplerHeap);
        meshBindings.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Gets or creates the descriptor tables for one point, line, or billboard core.
    /// </summary>
    /// <param name="core">The existing point/line core.</param>
    /// <returns>The core's host-lifetime bindings.</returns>
    private SilkD3D12PointLineBindings GetBindings(PointLineRenderCore core) {
        if (pointLineBindings.TryGetValue(core, out var bindings)) return bindings;
        bindings = new SilkD3D12PointLineBindings(Device, resourceHeap, samplerHeap);
        pointLineBindings.Add(core, bindings);
        return bindings;
    }

    /// <summary>
    ///     Detaches nodes and releases per-core bindings and shared native resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var node in attachedNodes) node.DetachD3D12();
        foreach (var bindings in meshBindings.Values) bindings.Dispose();
        foreach (var bindings in pointLineBindings.Values) bindings.Dispose();
        attachedNodes.Clear();
        meshBindings.Clear();
        pointLineBindings.Clear();
        visibleNodes.Clear();
        resources.Dispose();
        IsDisposed = true;
    }
}
