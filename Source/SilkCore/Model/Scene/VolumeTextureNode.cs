/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class VolumeTextureNode : SceneNode {
    private MaterialCore material;

    private MaterialVariable materialVariable;

    /// <summary>
    /// </summary>
    public MaterialCore Material {
        get => material;
        set {
            if (Set(ref material, value))
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

    protected virtual void AttachMaterial() {
        var newVar = material != null && RenderCore is VolumeRenderCore
                         ? EffectsManager.MaterialVariableManager.Register(material, EffectTechnique)
                         : EmptyMaterialVariable.EmptyVariable;
        
        RemoveAndDispose(ref materialVariable);
        if (RenderCore is VolumeRenderCore core) 
            materialVariable = core.MaterialVariables = newVar;
    }


    protected override OrderKey OnUpdateRenderOrderKey() {
        return OrderKey.Create(RenderOrder, materialVariable == null ? (ushort)0 : materialVariable.Id);
    }

    protected override bool CanRender(RenderContext context) {
        return base.CanRender(context) && materialVariable != null;
    }

    protected override RenderCore OnCreateRenderCore() {
        return new VolumeRenderCore { DefaultStateBinding = StateType.All };
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
        return effectsManager[DefaultRenderTechniqueNames.Volume3D];
    }

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        return false;
    }
}
