/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

public class VolumeTextureNode : SceneNode {
    private MaterialCore? material;

    /// <summary>
    /// </summary>
    public MaterialCore? Material {
        get => material;
        set {
            if (!Set(ref material, value)) return;
            if (RenderCore is VolumeRenderCore volumeRenderCore)
                volumeRenderCore.D3D12Material = material;
            InvalidateRender();
        }
    }

    /// <inheritdoc />
    internal override bool Attach() {
        if (RenderCore is VolumeRenderCore core) core.D3D12Material = material;
        return material is IVolumeTextureMaterial && base.Attach();
    }

    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, 0);

    protected override bool CanRender(RenderContext context) =>
        base.CanRender(context) && material is IVolumeTextureMaterial;

    protected override RenderCore OnCreateRenderCore() => new VolumeRenderCore { DefaultStateBinding = StateType.All };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Volume3D];

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
