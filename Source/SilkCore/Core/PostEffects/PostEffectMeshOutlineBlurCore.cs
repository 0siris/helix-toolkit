/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

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
        this.UseBlurCore = useBlurCore;
        Color = new Color4(1, 0, 0, 1);
        modelCB = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.BorderEffectCB,
                                       BorderEffectStruct.SizeInBytes)));
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        screenQuadPass = technique.GetPass(DefaultPassNames.ScreenQuad);
        blurPassVertical = technique.GetPass(DefaultPassNames.EffectBlurVertical);
        blurPassHorizontal = technique.GetPass(DefaultPassNames.EffectBlurHorizontal);
        smoothPass = technique.GetPass(DefaultPassNames.EffectOutlineSmooth);
        screenOutlinePass = technique.GetPass(DefaultPassNames.MeshOutline);
        
        textureSlot = screenOutlinePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.DiffuseMapTB);
        
        samplerSlot = screenOutlinePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
        Sampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
        
        if (UseBlurCore) { 
            BlurCore = new PostEffectBlurCore(blurPassVertical,
                                              blurPassHorizontal,
                                              textureSlot,
                                              samplerSlot,
                                              DefaultSamplers.LinearSamplerClampAni1,
                                              technique.EffectsManager);
        }
        return true;
    }

    protected override bool OnUpdateCanRenderFlag() 
        => IsAttached && !string.IsNullOrEmpty(EffectName);

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        using var depthStencilBuffer = context.GetOffScreenDS(TextureSize,
                                                              Format.FormatD32FloatS8X24Uint,
                                                              out var width,
                                                              out var height);
        
        using var renderTargetBuffer = context.GetOffScreenRT(TextureSize, Format.FormatR8G8B8A8Unorm);
        OnUpdatePerModelStruct(context);
        var viewport = context.Viewport;
        
        if (drawMode == OutlineMode.Separated) {
            foreach (var mesh in context.RenderHost.PerFrameNodesWithPostEffect) {
                deviceContext.SetRenderTarget(depthStencilBuffer,
                                              renderTargetBuffer,
                                              true,
                                              new Color4(0, 0, 0, 0),
                                              true,
                                              DepthStencilClearFlags.Stencil,
                                              0);
                
                deviceContext.SetViewport(ref viewport);
                deviceContext.SetScissorRectangle(ref viewport);
                
                if (mesh.TryGetPostEffect(EffectName, out var effect)) {
                    var modeColor = Color;
                    if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) 
                        && attribute is string colorStr) {
                        modeColor = colorStr.ToColor4();
                    }
                    
                    if (modelStruct.Color != modeColor) {
                        modelStruct.Color = modeColor;
                        modelCB.Upload(deviceContext, ref modelStruct);
                    }

                    context.CustomPassName = DefaultPassNames.EffectOutlineP1;
                    var pass = mesh.EffectTechnique?[DefaultPassNames.EffectOutlineP1];
                    if (pass  == null || pass.IsNULL) 
                        continue;
                    
                    pass.BindShader(deviceContext);
                    pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                    mesh.RenderCustom(context, deviceContext);
                    DrawOutline(context, deviceContext, depthStencilBuffer, renderTargetBuffer);
                }
            }
        } else {
            deviceContext.SetRenderTarget(depthStencilBuffer,
                                          renderTargetBuffer,
                                          true,
                                          Transparent,
                                          true,
                                          DepthStencilClearFlags.Stencil,
                                          0);
            deviceContext.SetViewport(ref viewport);
            deviceContext.SetScissorRectangle(ref viewport);

        #region Render objects onto offscreen texture

            var hasMesh = false;
            foreach (var mesh in context.RenderHost.PerFrameNodesWithPostEffect) {
                if (!mesh.TryGetPostEffect(EffectName, out var effect)) 
                    continue;
                
                var modelColor = Color;
                if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName,
                                           out var attribute) && attribute is string colorStr) {
                    modelColor = colorStr.ToColor4();
                }
                
                if (modelStruct.Color != modelColor) {
                    modelStruct.Color = modelColor;
                    modelCB.Upload(deviceContext, ref modelStruct);
                }

                context.CustomPassName = DefaultPassNames.EffectOutlineP1;
                var pass = mesh.EffectTechnique[DefaultPassNames.EffectOutlineP1];
                if (pass.IsNULL) continue;
                pass.BindShader(deviceContext);
                pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                mesh.RenderCustom(context, deviceContext);
                hasMesh = true;
            }

        #endregion

            if (hasMesh) 
                DrawOutline(context, deviceContext, depthStencilBuffer, renderTargetBuffer);
        }
    }

    private void DrawOutline(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderResourceViewProxy depthStencilBuffer,
        ShaderResourceViewProxy source
    ) {
        var buffer = context.RenderHost.RenderBuffer;
        var sourceViewport = new ViewportF(0, 0, buffer.FullResPPBuffer.Width, buffer.FullResPPBuffer.Height);
        deviceContext.SetViewport(ref sourceViewport);
        deviceContext.SetScissorRectangle(ref sourceViewport);

    #region Do Blur Pass

        if (UseBlurCore) {
            for (var i = 0; i < numberOfBlurPass; ++i) {
                BlurCore.Run(context,
                             deviceContext,
                             source,
                             ref sourceViewport,
                             PostEffectBlurCore.BlurDepth.One,
                             ref modelStruct);
            }
        } else {
            blurPassHorizontal.PixelShader.BindSampler(deviceContext, samplerSlot, Sampler);
            for (var i = 0; i < numberOfBlurPass; ++i) {
                deviceContext.SetRenderTarget(context.RenderHost.RenderBuffer.FullResPPBuffer.NextRTV);
                blurPassHorizontal.PixelShader.BindTexture(deviceContext, textureSlot, source);
                blurPassHorizontal.BindShader(deviceContext);
                blurPassHorizontal.BindStates(deviceContext, StateType.All);
                deviceContext.Draw(4, 0);

                deviceContext.SetRenderTarget(source);
                blurPassVertical.PixelShader.BindTexture(deviceContext,
                                                         textureSlot,
                                                         context.RenderHost.RenderBuffer.FullResPPBuffer
                                                                .NextRTV);
                blurPassVertical.BindShader(deviceContext);
                blurPassVertical.BindStates(deviceContext, StateType.All);
                deviceContext.Draw(4, 0);
            }
        }

    #region Draw back with stencil test

        deviceContext.SetRenderTarget(depthStencilBuffer,
                                      context.RenderHost.RenderBuffer.FullResPPBuffer.NextRTV,
                                      true,
                                      new Color4(0, 0, 0, 0),
                                      false);
        
        screenQuadPass.PixelShader.BindTexture(deviceContext, textureSlot, source);
        screenQuadPass.BindShader(deviceContext);
        screenQuadPass.BindStates(deviceContext, StateType.All);
        deviceContext.Draw(4, 0);

    #endregion

    #region Draw outline onto original target

        deviceContext.SetRenderTarget(buffer.FullResPPBuffer.CurrentRTV);
        screenOutlinePass.PixelShader.BindTexture(deviceContext,
                                                  textureSlot,
                                                  context.RenderHost.RenderBuffer.FullResPPBuffer.NextRTV);
        screenOutlinePass.BindShader(deviceContext);
        screenOutlinePass.BindStates(deviceContext, StateType.All);
        deviceContext.Draw(4, 0);
        screenOutlinePass.PixelShader.BindTexture(deviceContext, textureSlot, null);

    #endregion

    #endregion
    }

    protected override void OnDetach() {
        BlurCore = null;
        Sampler = null;
    }

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
            if(value != field)
                field?.Dispose();
            field = value;
        }
    }

    private PostEffectBlurCore? BlurCore {
        get;
        set {
            if(value != field)
                field?.Dispose();
            field = value;
        }
    }

    private ShaderPass screenQuadPass;

    private ShaderPass blurPassVertical;

    private ShaderPass blurPassHorizontal;

    private ShaderPass smoothPass;

    private ShaderPass screenOutlinePass;

    private int textureSlot;

    private int samplerSlot;

    private readonly ConstantBufferComponent modelCB;
    private BorderEffectStruct modelStruct;
    private static readonly OffScreenTextureSize TextureSize = OffScreenTextureSize.Full;
    private static readonly Color4 Transparent = new(0, 0, 0, 0);

    [MemberNotNullWhen(true, nameof(BlurCore))]
    private  bool UseBlurCore { get; }

#endregion

#region Properties

    private string effectName = DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur;

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get => effectName;
        set => SetAffectsCanRenderFlag(ref effectName, value);
    }

    /// <summary>
    ///     Gets or sets the color of the border.
    /// </summary>
    /// <value>
    ///     The color of the border.
    /// </value>
    public Color4 Color {
        get;
        set => SetAffectsRender(ref field, value);
    } = new(1, 0, 0, 1);

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

    private float scaleY = 1;

    /// <summary>
    ///     Gets or sets the scale y.
    /// </summary>
    /// <value>
    ///     The scale y.
    /// </value>
    public float ScaleY {
        get => scaleY;
        set => SetAffectsRender(ref scaleY, value);
    }

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