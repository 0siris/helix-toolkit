/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.PostEffects;

public sealed class PostEffectFxaa : RenderCore, IPostEffect {
    private readonly ConstantBufferComponent modelCb;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    private ShaderPass fxaaPass;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    private ShaderPass lumaPass;
    private BorderEffectStruct modelStruct;

    private SamplerStateProxy? Sampler {
        get;
        set {
            if (value != field)
                field?.Dispose();

            field = value;
        }
    }

    private int samplerSlot;

    private int textureSlot;

    public PostEffectFxaa() : base(RenderType.GlobalEffect) {
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.BorderEffectCb,
                                       BorderEffectStruct.SizeInBytes)));
    }

    /// <summary>
    ///     Gets or sets the fxaa level.
    /// </summary>
    /// <value>
    ///     The fxaa level.
    /// </value>
    public FxaaLevel FxaaLevel {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = FxaaLevel.None;

    public string EffectName {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = DefaultRenderTechniqueNames.PostEffectFxaa;

    protected override bool OnUpdateCanRenderFlag()
        => IsAttached && !string.IsNullOrEmpty(EffectName) && FxaaLevel != FxaaLevel.None;

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.Color = new Color4(1 / context.ActualWidth,
                                       1 / context.ActualHeight,
                                       modelStruct.Color.GetBlue(),
                                       modelStruct.Color.GetAlpha());
        switch (FxaaLevel) {
            case FxaaLevel.Low:
                modelStruct.Param.M11 = 0.25f;   //fxaaQualitySubpix
                modelStruct.Param.M12 = 0.250f;  // FxaaFloat fxaaQualityEdgeThreshold,
                modelStruct.Param.M13 = 0.0833f; // FxaaFloat fxaaQualityEdgeThresholdMin,
                break;
            case FxaaLevel.Medium:
                modelStruct.Param.M11 = 0.50f;
                modelStruct.Param.M12 = 0.166f;
                modelStruct.Param.M13 = 0.0625f;
                break;
            case FxaaLevel.High:
                modelStruct.Param.M11 = 0.75f;
                modelStruct.Param.M12 = 0.125f;
                modelStruct.Param.M13 = 0.0625f;
                break;
            case FxaaLevel.Ultra:
                modelStruct.Param.M11 = 1.00f;
                modelStruct.Param.M12 = 0.063f;
                modelStruct.Param.M13 = 0.0312f;
                break;
        }
    }
}
