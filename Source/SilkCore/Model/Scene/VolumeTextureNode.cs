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


    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, materialVariable?.Id ?? (ushort)0);

    protected override bool CanRender(RenderContext context) => base.CanRender(context) && materialVariable != null;

    protected override RenderCore OnCreateRenderCore() => new VolumeRenderCore { DefaultStateBinding = StateType.All };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Volume3D];

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
