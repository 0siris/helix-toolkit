using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class PostEffectFXAA : RenderCore, IPostEffect {

    [System.Diagnostics.CodeAnalysis.AllowNull]
    private ShaderPass fxaaPass;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    private ShaderPass lumaPass;
    private BorderEffectStruct modelStruct;

    private int samplerSlot;

    private int textureSlot;

    public PostEffectFXAA() : base(RenderType.GlobalEffect) {
    }

    /// <summary>
    ///     Gets or sets the fxaa level.
    /// </summary>
    /// <value>
    ///     The fxaa level.
    /// </value>
    public FXAALevel FxaaLevel {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = FXAALevel.None;

    public string EffectName {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = DefaultRenderTechniqueNames.PostEffectFxaa;

    protected override bool OnUpdateCanRenderFlag()
        => IsAttached && !string.IsNullOrEmpty(EffectName) && FxaaLevel != FXAALevel.None;

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.Color = new Color4(1 / context.ActualWidth,
                                       1 / context.ActualHeight,
                                       modelStruct.Color.GetBlue(),
                                       modelStruct.Color.GetAlpha());
        switch (FxaaLevel) {
            case FXAALevel.Low:
                modelStruct.Param.M11 = 0.25f;   //fxaaQualitySubpix
                modelStruct.Param.M12 = 0.250f;  // FxaaFloat fxaaQualityEdgeThreshold,
                modelStruct.Param.M13 = 0.0833f; // FxaaFloat fxaaQualityEdgeThresholdMin,
                break;
            case FXAALevel.Medium:
                modelStruct.Param.M11 = 0.50f;
                modelStruct.Param.M12 = 0.166f;
                modelStruct.Param.M13 = 0.0625f;
                break;
            case FXAALevel.High:
                modelStruct.Param.M11 = 0.75f;
                modelStruct.Param.M12 = 0.125f;
                modelStruct.Param.M13 = 0.0625f;
                break;
            case FXAALevel.Ultra:
                modelStruct.Param.M11 = 1.00f;
                modelStruct.Param.M12 = 0.063f;
                modelStruct.Param.M13 = 0.0312f;
                break;
        }
    }
}
