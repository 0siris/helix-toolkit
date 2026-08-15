/*
The MIT License(MIT)
Copyright(c) 2020 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
public abstract class MaterialGeometryNode : GeometryNode {
    private MaterialCore? material;
    private MaterialVariable? materialVariable;

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
        var newVar = material is { } currentMaterial && RenderCore is IMaterialRenderParams &&
                     EffectsManager is { } effectsManager && EffectTechnique is { } technique
                         ? effectsManager.MaterialVariableManager.Register(currentMaterial, technique)
                         : null;
        RemoveAndDispose(ref materialVariable);
        materialVariable = newVar;
        if (RenderCore is IMaterialRenderParams core) core.MaterialVariables = newVar;
    }

    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, materialVariable?.Id ?? (ushort)0);

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
