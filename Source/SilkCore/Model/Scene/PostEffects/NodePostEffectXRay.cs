/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.PostEffects;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;
/// <summary>
/// </summary>
public class NodePostEffectXRay : SceneNode {
    private IPostEffectMeshXRay XRayCore => RenderCore as IPostEffectMeshXRay
        ?? throw new InvalidOperationException("X-ray post-effect render core is not initialized.");

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
        get => XRayCore.EffectName;
        set => XRayCore.EffectName = value;
    }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => XRayCore.Color;
        set => XRayCore.Color = value;
    }

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    public float OutlineFadingFactor {
        get => XRayCore.OutlineFadingFactor;
        set => XRayCore.OutlineFadingFactor = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable double pass].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable double pass]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDoublePass {
        get => XRayCore.EnableDoublePass;
        set => XRayCore.EnableDoublePass = value;
    }

    #endregion
}
