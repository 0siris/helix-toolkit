/*
The MIT License(MIT)
Copyright(c) 2020 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public abstract class MaterialGeometryNode : GeometryNode {
    private MaterialCore material;
    private MaterialVariable materialVariable;

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

    protected virtual void AttachMaterial() {
        var newVar = material != null && RenderCore is IMaterialRenderParams
                         ? EffectsManager.MaterialVariableManager.Register(material, EffectTechnique)
                         : null;
        RemoveAndDispose(ref materialVariable);
        materialVariable = newVar;
        if (RenderCore is IMaterialRenderParams core) core.MaterialVariables = newVar;
    }

    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, materialVariable == null ? (ushort)0 : materialVariable.Id);

    protected override bool CanRender(RenderContext context) => base.CanRender(context) && materialVariable != null;

    protected override bool OnAttach(IEffectsManager effectsManager) {
        if (base.OnAttach(effectsManager)) {
            AttachMaterial();
            return true;
        }

        return false;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref materialVariable);
        if (RenderCore is IMaterialRenderParams core) core.MaterialVariables = null;
        base.OnDetach();
    }
}
