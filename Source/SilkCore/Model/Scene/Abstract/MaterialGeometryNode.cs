/*
The MIT License(MIT)
Copyright(c) 2020 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

public abstract class MaterialGeometryNode : GeometryNode {
    private MaterialCore? material;

    /// <summary>
    ///     Specifiy if model material is transparent.
    ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
    ///     are preserved.
    /// </summary>
    public bool IsTransparent {
        get;
        set {
            if (Set(ref field, value))
                if (RenderType == RenderType.Opaque || RenderType == RenderType.Transparent)
                    RenderType = value ? RenderType.Transparent : RenderType.Opaque;
        }
    }

    public MaterialCore? Material {
        get => material;
        set {
            if (!Set(ref material, value))
                return;

            if (RenderCore is MeshRenderCore meshRenderCore) meshRenderCore.D3D12Material = material;
            if (RenderCore is PointLineRenderCore pointLineRenderCore)
                pointLineRenderCore.D3D12Material = material;

            InvalidateRender();
        }
    }

    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, 0);

    protected override bool CanRender(RenderContext context) =>
        base.CanRender(context) && material is not null;
}
