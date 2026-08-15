/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class NodePostEffectMeshOutlineBlur : SceneNode {
    private IPostEffectOutlineBlur Effect => RenderCore as IPostEffectOutlineBlur
        ?? throw new InvalidOperationException("The outline blur render core is not initialized.");

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PostEffectMeshOutlineBlurCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur];

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
        get => Effect.EffectName;
        set => Effect.EffectName = value;
    }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => Effect.Color;
        set => Effect.Color = value;
    }

    /// <summary>
    ///     Gets or sets the scale x.
    /// </summary>
    /// <value>
    ///     The scale x.
    /// </value>
    public float ScaleX {
        get => Effect.ScaleX;
        set => Effect.ScaleX = value;
    }

    /// <summary>
    ///     Gets or sets the scale y.
    /// </summary>
    /// <value>
    ///     The scale y.
    /// </value>
    public float ScaleY {
        get => Effect.ScaleY;
        set => Effect.ScaleY = value;
    }

    /// <summary>
    ///     Gets or sets the number of blur pass.
    /// </summary>
    /// <value>
    ///     The number of blur pass.
    /// </value>
    public int NumberOfBlurPass {
        get => Effect.NumberOfBlurPass;
        set => Effect.NumberOfBlurPass = value;
    }

    #endregion
}
