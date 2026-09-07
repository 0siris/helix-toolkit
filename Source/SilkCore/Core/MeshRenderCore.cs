/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public class MeshRenderCore : GeometryRenderCore, IMeshRenderParams, IDynamicReflectable {
    protected ModelStruct ModelStruct = new() { World = Matrix.Identity };

    protected override bool OnUpdateCanRenderFlag() => base.OnUpdateCanRenderFlag();

    protected virtual void OnUpdatePerModelStruct(RenderContext context) {
        OnUpdatePerModelStructD3D12();
    }

    /// <summary>
    ///     Updates the renderer-independent fields of the per-model structure for Direct3D 12.
    /// </summary>
    protected virtual void OnUpdatePerModelStructD3D12() {
        ModelStruct.World = ModelMatrix;
        ModelStruct.HasInstances = InstanceBuffer.HasElements ? 1 : 0;
        ModelStruct.Batched = Batched ? 1 : 0;
    }

    /// <summary>
    ///     Records this existing mesh core with camera/model constants through Direct3D 12.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="pass">The selected Direct3D 12 material pass.</param>
    /// <param name="bindings">The in-flight mesh descriptor and constant-buffer bindings.</param>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <param name="environmentMap">The optional shared environment cube map.</param>
    /// <returns>Whether the mesh draw was recorded.</returns>
    internal virtual bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12MeshBindings bindings,
        in GlobalTransformStruct transforms,
        LightsBufferModel? lights = null,
        TextureModel? environmentMap = null
    ) {
        bindings.GuardNotNull();
        OnUpdatePerModelStructD3D12();
        if (lights is not null) bindings.UpdateLights(lights);
        bindings.Update(context, resources, in transforms, in ModelStruct, D3D12Material, environmentMap);
        return base.TryRenderD3D12(context,
            resources,
            pass,
            bindings.ResourceTableStart,
            bindings.SamplerTableStart);
    }

    #region Properties

    /// <summary>
    /// </summary>
    public bool InvertNormal {
        get => ModelStruct.InvertNormal == 1;
        set => SetAffectsRender(ref ModelStruct.InvertNormal, value ? 1 : 0);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render wireframe].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderWireframe {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the color of the wireframe.
    /// </summary>
    /// <value>
    ///     The color of the wireframe.
    /// </value>
    public Color4 WireframeColor {
        get => ModelStruct.WireframeColor;
        set => SetAffectsRender(ref ModelStruct.WireframeColor, value);
    }


    /// <summary>
    ///     Gets or sets the dynamic reflector.
    /// </summary>
    /// <value>
    ///     The dynamic reflector.
    /// </value>
    public IDynamicReflector? DynamicReflector { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="MeshRenderCore" /> is batched.
    /// </summary>
    /// <value>
    ///     <c>true</c> if batched; otherwise, <c>false</c>.
    /// </value>
    public bool Batched { get; set; }

    /// <summary>
    ///     Gets or sets the existing material core used by the Direct3D 12 path.
    /// </summary>
    internal MaterialCore? D3D12Material { get; set; }

    /// <summary>
    ///     Gets the productive opaque DX12 pass name selected by the existing material.
    /// </summary>
    internal string D3D12MaterialPassName => D3D12MeshMaterialData.GetPassName(D3D12Material);

    #endregion
}
