/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class NodePostEffectBloom : SceneNode {
    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PostEffectBloomCore();

    /// <summary>
    ///     Override this function to set render technique during Attach Host.
    ///     <para>
    ///         If <see cref="SceneNode.OnSetRenderTechnique" /> is set, then <see cref="SceneNode.OnSetRenderTechnique" />
    ///         instead of <see cref="SceneNode.OnCreateRenderTechnique" /> function will be called.
    ///     </para>
    /// </summary>
    /// <param name="effectsManager"></param>
    /// <returns>
    ///     Return RenderTechnique
    /// </returns>
    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.PostEffectBloom];

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
        get => (RenderCore as IPostEffectBloom).EffectName;
        set => (RenderCore as IPostEffectBloom).EffectName = value;
    }

    /// <summary>
    ///     Gets or sets the color of the threshold.
    /// </summary>
    /// <value>
    ///     The color of the threshold.
    /// </value>
    public Color4 ThresholdColor {
        get => (RenderCore as IPostEffectBloom).ThresholdColor;
        set => (RenderCore as IPostEffectBloom).ThresholdColor = value;
    }

    /// <summary>
    ///     Gets or sets the number of blur pass.
    /// </summary>
    /// <value>
    ///     The number of blur pass.
    /// </value>
    public int NumberOfBlurPass {
        get => (RenderCore as IPostEffectBloom).NumberOfBlurPass;
        set => (RenderCore as IPostEffectBloom).NumberOfBlurPass = value;
    }

    /// <summary>
    ///     Gets or sets the bloom extract intensity.
    /// </summary>
    /// <value>
    ///     The bloom extract intensity.
    /// </value>
    public float BloomExtractIntensity {
        get => (RenderCore as IPostEffectBloom).BloomExtractIntensity;
        set => (RenderCore as IPostEffectBloom).BloomExtractIntensity = value;
    }

    /// <summary>
    ///     Gets or sets the bloom pass intensity.
    /// </summary>
    /// <value>
    ///     The bloom pass intensity.
    /// </value>
    public float BloomPassIntensity {
        get => (RenderCore as IPostEffectBloom).BloomPassIntensity;
        set => (RenderCore as IPostEffectBloom).BloomPassIntensity = value;
    }

    /// <summary>
    ///     Gets or sets the bloom combine intensity.
    /// </summary>
    /// <value>
    ///     The bloom combine intensity.
    /// </value>
    public float BloomCombineIntensity {
        get => (RenderCore as IPostEffectBloom).BloomCombineIntensity;
        set => (RenderCore as IPostEffectBloom).BloomCombineIntensity = value;
    }

    /// <summary>
    ///     Gets or sets the bloom combine saturation.
    /// </summary>
    /// <value>
    ///     The bloom combine saturation.
    /// </value>
    public float BloomCombineSaturation {
        get => (RenderCore as IPostEffectBloom).BloomCombineSaturation;
        set => (RenderCore as IPostEffectBloom).BloomCombineSaturation = value;
    }

    #endregion
}
