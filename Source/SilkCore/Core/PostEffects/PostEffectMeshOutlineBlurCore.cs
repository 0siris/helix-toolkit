/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.PostEffects;

/// <summary>
/// </summary>
public interface IPostEffectOutlineBlur : IPostEffect {
    /// <summary>
    ///     Gets or sets the color of the border.
    /// </summary>
    /// <value>
    ///     The color of the border.
    /// </value>
    Color4 Color { get; set; }

    /// <summary>
    ///     Gets or sets the scale x.
    /// </summary>
    /// <value>
    ///     The scale x.
    /// </value>
    float ScaleX { get; set; }

    /// <summary>
    ///     Gets or sets the scale y.
    /// </summary>
    /// <value>
    ///     The scale y.
    /// </value>
    float ScaleY { get; set; }

    /// <summary>
    ///     Gets or sets the number of blur pass.
    /// </summary>
    /// <value>
    ///     The number of blur pass.
    /// </value>
    int NumberOfBlurPass { get; set; }
}

/// <summary>
///     Outline blur effect
///     <para>
///         Must not put in shared model across multiple viewport, otherwise may causes performance issue if each
///         viewport sizes are different.
///     </para>
/// </summary>
public class PostEffectMeshOutlineBlurCore : RenderCore, IPostEffectOutlineBlur {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEffectMeshOutlineBlurCore" /> class.
    /// </summary>
    public PostEffectMeshOutlineBlurCore(bool useBlurCore = true) : base(RenderType.PostEffect) {
        UseBlurCore = useBlurCore;
        Color = new Color4(1, 0, 0, 1);
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.BorderEffectCb,
                                       BorderEffectStruct.SizeInBytes)));
    }

    protected override bool OnUpdateCanRenderFlag()
        => IsAttached && !string.IsNullOrEmpty(EffectName);

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.Param.M11 = scaleX;
        modelStruct.Param.M12 = ScaleY;
        modelStruct.Color = new Color4();
        modelStruct.ViewportScale = (int)TextureSize;
    }

    #region Variables

    private SamplerStateProxy? Sampler {
        get;
        set {
            if (value != field)
                field?.Dispose();
            field = value;
        }
    }

    private PostEffectBlurCore? BlurCore {
        get;
        set {
            if (value != field)
                field?.Dispose();
            field = value;
        }
    }

    private ShaderPass screenQuadPass = ShaderPass.NullPass;

    private ShaderPass blurPassVertical = ShaderPass.NullPass;

    private ShaderPass blurPassHorizontal = ShaderPass.NullPass;

    private ShaderPass smoothPass = ShaderPass.NullPass;

    private ShaderPass screenOutlinePass = ShaderPass.NullPass;

    private int textureSlot;

    private int samplerSlot;

    private readonly ConstantBufferComponent modelCb;
    private BorderEffectStruct modelStruct;
    private static readonly OffScreenTextureSize TextureSize = OffScreenTextureSize.Full;
    private static readonly Color4 Transparent = new(0, 0, 0, 0);

    [MemberNotNullWhen(true, nameof(BlurCore))]
    private bool UseBlurCore { get; }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur;

    /// <summary>
    ///     Gets or sets the color of the border.
    /// </summary>
    /// <value>
    ///     The color of the border.
    /// </value>
    public Color4 Color {
        get;
        set => SetAffectsRender(ref field, value);
    }

    private float scaleX = 1;

    /// <summary>
    ///     Gets or sets the scale x.
    /// </summary>
    /// <value>
    ///     The scale x.
    /// </value>
    public float ScaleX {
        get => scaleX;
        set => SetAffectsRender(ref scaleX, value);
    }

    /// <summary>
    ///     Gets or sets the scale y.
    /// </summary>
    /// <value>
    ///     The scale y.
    /// </value>
    public float ScaleY {
        get;
        set => SetAffectsRender(ref field, value);
    } = 1;

    private int numberOfBlurPass = 1;

    /// <summary>
    ///     Gets or sets the number of blur pass.
    /// </summary>
    /// <value>
    ///     The number of blur pass.
    /// </value>
    public int NumberOfBlurPass {
        get => numberOfBlurPass;
        set => SetAffectsRender(ref numberOfBlurPass, value);
    }

    private OutlineMode drawMode = OutlineMode.Merged;

    public OutlineMode DrawMode {
        get => drawMode;
        set => SetAffectsRender(ref drawMode, value);
    }

    #endregion
}
