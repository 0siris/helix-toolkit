/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
///     Provides a way to render child elements always on top of other elements.
///     This is rendered at the same level of screen spaced group items.
///     Child items do not support post effects.
/// </summary>
public class TopMostGroupNode : GroupNode {
    public TopMostGroupNode() {
        AffectsGlobalVariable = true;
    }

    public bool EnableTopMost {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                RenderType = value ? RenderType.ScreenSpaced : RenderType.Opaque;
        }
    } = true;

    protected override RenderCore OnCreateRenderCore() {
        var core = new TopMostMeshRenderCore();
        return core;
    }
}
