/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class MeshNode : MaterialGeometryNode, IDynamicReflectable {
    private IInvertNormal InvertNormalCore => RenderCore as IInvertNormal
        ?? throw new InvalidOperationException("Invert-normal render core is not initialized.");

    private IMeshRenderParams MeshRenderParams => RenderCore as IMeshRenderParams
        ?? throw new InvalidOperationException("Mesh render core is not initialized.");

    private IDynamicReflectable DynamicReflectableCore => RenderCore as IDynamicReflectable
        ?? throw new InvalidOperationException("Dynamic-reflector render core is not initialized.");

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new MeshRenderCore();

    /// <summary>
    ///     Called when [create buffer model].
    /// </summary>
    /// <param name="modelGuid"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) => geometry is
    {
        IsDynamic: true
    }
        ? (EffectsManager ?? throw new InvalidOperationException("Effects manager is required.")).GeometryBufferManager.Register<DynamicMeshGeometryBufferModel>(
            modelGuid,
            geometry)
        : (EffectsManager ?? throw new InvalidOperationException("Effects manager is required.")).GeometryBufferManager
            .Register<DefaultMeshGeometryBufferModel>(modelGuid, geometry);

    /// <summary>
    ///     Create raster state description.
    /// </summary>
    /// <returns></returns>
    protected override RasterizerStateDescription CreateRasterState() => new() {
        FillMode = FillMode,
        CullMode = CullMode,
        DepthBias = DepthBias,
        DepthBiasClamp = -1000,
        SlopeScaledDepthBias = SlopeScaledDepthBias,
        IsDepthClipEnabled = IsDepthClipEnabled,
        IsFrontCounterClockwise = FrontCcw,
        IsMultisampleEnabled = IsMsaaEnabled,
        IsScissorEnabled = !IsThrowingShadow && IsScissorEnabled
    };

    protected override bool OnCheckGeometry(Geometry3D? geometry) => base.OnCheckGeometry(geometry) && geometry is MeshGeometry3D;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => Geometry is MeshGeometry3D mesh
            && mesh.HitTest(context, totalModelMatrix, ref hits, WrapperSource ?? this);

    #region Properties

    /// <summary>
    ///     Gets or sets a value indicating whether [front CCW].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [front CCW]; otherwise, <c>false</c>.
    /// </value>
    public bool FrontCcw {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = true;

    /// <summary>
    ///     Gets or sets the cull mode.
    /// </summary>
    /// <value>
    ///     The cull mode.
    /// </value>
    public CullMode CullMode {
        get;
        set {
            if (Set(ref field, value)) OnRasterStateChanged();
        }
    } = CullMode.None;

    /// <summary>
    ///     Gets or sets a value indicating whether [invert normal].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [invert normal]; otherwise, <c>false</c>.
    /// </value>
    public bool InvertNormal {
        get => InvertNormalCore.InvertNormal;
        set => InvertNormalCore.InvertNormal = value;
    }

    /// <summary>
    ///     Gets or sets the color of the wireframe.
    /// </summary>
    /// <value>
    ///     The color of the wireframe.
    /// </value>
    public Color4 WireframeColor {
        get => MeshRenderParams.WireframeColor;
        set => MeshRenderParams.WireframeColor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render wireframe].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderWireframe {
        get => MeshRenderParams.RenderWireframe;
        set => MeshRenderParams.RenderWireframe = value;
    }

    /// <summary>
    ///     Gets or sets the dynamic reflector.
    /// </summary>
    /// <value>
    ///     The dynamic reflector.
    /// </value>
    public IDynamicReflector? DynamicReflector {
        get => DynamicReflectableCore.DynamicReflector;
        set => DynamicReflectableCore.DynamicReflector = value;
    }

    #endregion
}
