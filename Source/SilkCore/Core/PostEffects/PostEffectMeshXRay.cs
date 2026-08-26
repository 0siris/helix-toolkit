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
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core.PostEffects;

/// <summary>
/// </summary>
public interface IPostEffectMeshXRay : IPostEffect {
    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    Color4 Color { get; set; }

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    float OutlineFadingFactor { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
    ///     artifacts
    /// </summary>
    /// <value>
    ///     <c>true</c> if [double pass]; otherwise, <c>false</c>.
    /// </value>
    bool EnableDoublePass { get; set; }
}

/// <summary>
/// </summary>
public class PostEffectMeshXRayCore : RenderCore, IPostEffectMeshXRay {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEffectMeshXRayCore" /> class.
    /// </summary>
    public PostEffectMeshXRayCore() : base(RenderType.PostEffect) {
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.BorderEffectCb,
                                       BorderEffectStruct.SizeInBytes)));
        Color = new Color4(0, 0, 1, 1);
    }


    protected override bool OnAttach(IRenderTechnique technique) => true;

    protected override void OnDetach() { }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    public override void Render(RenderContext context, DeviceContextProxy deviceContext) { }

    protected override bool OnUpdateCanRenderFlag() 
        => IsAttached && !string.IsNullOrEmpty(EffectName);

#region Variables

    private readonly List<(SceneNode Mesh, IEffectAttributes Effect)> currentCoresBuffer = [];
    private readonly ConstantBufferComponent modelCb;
    private BorderEffectStruct modelStruct;

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
    } = DefaultRenderTechniqueNames.PostEffectMeshXRay;

    /// <summary>
    ///     Gets or sets the color of the border.
    /// </summary>
    /// <value>
    ///     The color of the border.
    /// </value>
    public Color4 Color {
        get => modelStruct.Color;
        set => SetAffectsRender(ref modelStruct.Color, value);
    }

    /// <summary>
    ///     Outline fading
    /// </summary>
    public float OutlineFadingFactor {
        get => modelStruct.Param.M11;
        set {
            var current = modelStruct.Param.M11;
            if (SetAffectsRender(ref current, value)) modelStruct.Param.M11 = current;
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
    ///     artifacts
    /// </summary>
    /// <value>
    ///     <c>true</c> if [double pass]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDoublePass {
        get;
        set => SetAffectsRender(ref field, value);
    }

#endregion
}
