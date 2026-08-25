/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class VolumeTextureNode : SceneNode {
    private MaterialCore? material;

    private MaterialVariable? materialVariable;

    /// <summary>
    /// </summary>
    public MaterialCore? Material {
        get => material;
        set {
            if (!Set(ref material, value)) return;
            if (RenderCore is VolumeRenderCore volumeRenderCore)
                volumeRenderCore.D3D12Material = material;
            if (EffectsManager != null) {
                if (IsAttached) {
                    AttachMaterial();
                    InvalidateRender();
                } else {
                    Detach();
                    Attach(EffectsManager);
                }
            }
        }
    }


    protected override bool OnAttach(IEffectsManager effectsManager) {
        if (base.OnAttach(effectsManager)) {
            AttachMaterial();
            return true;
        }

        return false;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref materialVariable);
        base.OnDetach();
    }

    /// <inheritdoc />
    internal override bool AttachD3D12() {
        if (RenderCore is VolumeRenderCore core) core.D3D12Material = material;
        return material is IVolumeTextureMaterial && base.AttachD3D12();
    }

    protected virtual void AttachMaterial() {
        if (EffectsManager is not { } effectsManager || RenderCore is not VolumeRenderCore core) {
            RemoveAndDispose(ref materialVariable);
            return;
        }

        var newVar = material is { } currentMaterial && EffectTechnique is { } technique
                         ? effectsManager.MaterialVariableManager.Register(currentMaterial, technique)
                         : EmptyMaterialVariable.EmptyVariable;
        
        RemoveAndDispose(ref materialVariable);
        materialVariable = core.MaterialVariables = newVar;
    }


    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, materialVariable?.Id ?? 0);

    protected override bool CanRender(RenderContext context) =>
        base.CanRender(context) && (materialVariable != null || RenderCore.IsD3D12Attached && material is not null);

    protected override RenderCore OnCreateRenderCore() => new VolumeRenderCore { DefaultStateBinding = StateType.All };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Volume3D];

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
