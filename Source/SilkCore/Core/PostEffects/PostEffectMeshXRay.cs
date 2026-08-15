/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

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
    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (context.RenderHost.RenderBuffer is not { } buffer)
            return;

        var depthStencilBuffer = buffer.DepthStencilBufferNoMsaa;
        deviceContext.SetRenderTarget(depthStencilBuffer, buffer.FullResPpBuffer.CurrentRtv);
        
        var viewport = context.Viewport;
        deviceContext.SetViewport(ref viewport);
        deviceContext.SetScissorRectangle(ref viewport);
        deviceContext.ClearDepthStencilView(depthStencilBuffer, DepthStencilClearFlags.Stencil);
        
        if (EnableDoublePass) {
            //pass 1
            foreach (var mesh in context.RenderHost.PerFrameNodesWithPostEffect) {

                if (!mesh.TryGetPostEffect(EffectName, out var effect)) 
                    continue;
                
                //collect mesh and effects for second pass
                currentCoresBuffer.Add((mesh, effect));
                
                RenderXRayP1(mesh);
            }

            modelCb.Upload(deviceContext, ref modelStruct);
            
            //pass 2
            foreach (var (mesh, effect) in currentCoresBuffer) {
                RenderXRayP2(effect, mesh, 
                           _ => mesh.RenderCustom(context, deviceContext));
                
            }

            currentCoresBuffer.Clear();
        } else {
            modelCb.Upload(deviceContext, ref modelStruct);

            foreach (var mesh in context.RenderHost.PerFrameNodesWithPostEffect) {
                if (!mesh.TryGetPostEffect(EffectName, out var effect)) 
                    continue;
                
                RenderXRayP2(effect, mesh, 
                           pass => {
                               deviceContext.SetDepthStencilState(pass.DepthStencilState);
                               mesh.RenderCustom(context, deviceContext);
                           });
            }
        }

        
        void RenderXRayP1(SceneNode mesh) {
            context.CustomPassName = DefaultPassNames.EffectMeshXRayP1;
            var pass = mesh.EffectTechnique?[DefaultPassNames.EffectMeshXRayP1];
            if (pass is null || pass.IsNull)  
                return;
                    
            pass.BindShader(deviceContext);
            pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
            mesh.RenderCustom(context, deviceContext);
        }
        
        void RenderXRayP2(IEffectAttributes effect, SceneNode mesh, Action<ShaderPass> renderMesh) {
            var color = Color;
            if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) 
                && attribute is string colorStr) {
                color = colorStr.ToColor4();
            }
                
            if (modelStruct.Color != color) {
                modelStruct.Color = color;
                modelCb.Upload(deviceContext, ref modelStruct);
            }

            context.CustomPassName = DefaultPassNames.EffectMeshXRayP2;
            var pass = mesh.EffectTechnique?[DefaultPassNames.EffectMeshXRayP2];
            if (pass is null ||pass.IsNull)
                return;
                
            pass.BindShader(deviceContext);
            pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
            renderMesh(pass);
          
        }

    }

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
