/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class NodePostEffectXRay : SceneNode {
    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PostEffectMeshXRayCore();

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    #region Properties

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get => (RenderCore as IPostEffectMeshXRay).EffectName;
        set => (RenderCore as IPostEffectMeshXRay).EffectName = value;
    }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => (RenderCore as IPostEffectMeshXRay).Color;
        set => (RenderCore as IPostEffectMeshXRay).Color = value;
    }

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    public float OutlineFadingFactor {
        get => (RenderCore as IPostEffectMeshXRay).OutlineFadingFactor;
        set => (RenderCore as IPostEffectMeshXRay).OutlineFadingFactor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable double pass].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable double pass]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDoublePass {
        get => (RenderCore as IPostEffectMeshXRay).EnableDoublePass;
        set => (RenderCore as IPostEffectMeshXRay).EnableDoublePass = value;
    }

    #endregion
}
