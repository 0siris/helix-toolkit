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

public interface IPostEffectMeshXRayGrid : IPostEffect {
    Color4 Color { get; set; }

    int GridDensity { get; set; }

    float DimmingFactor { get; set; }

    float BlendingFactor { get; set; }

    string XRayDrawingPassName { get; set; }

    bool UseDepthOcclusion { get; set; }
}

/// <summary>
/// </summary>
public class PostEffectMeshXRayGridCore : RenderCore, IPostEffectMeshXRayGrid {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEffectMeshXRayGridCore" /> class.
    /// </summary>
    public PostEffectMeshXRayGridCore() : base(RenderType.PostEffect) {
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.BorderEffectCb,
                                       BorderEffectStruct.SizeInBytes)));
        Color = new Color4(0, 0, 1, 1);
    }

    protected override bool OnAttach(IRenderTechnique technique) => true;

    protected override void OnDetach() { }

    protected override bool OnUpdateCanRenderFlag() 
        => IsAttached && !string.IsNullOrEmpty(EffectName);

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
        //First pass, draw onto stencil buffer
        foreach (var mesh in context.RenderHost.PerFrameNodesWithPostEffect) {
            if (!mesh.TryGetPostEffect(EffectName, out var effect)) 
                continue;
            
            currentCores.Add((mesh, effect));
            
            RenderPass(mesh, DefaultPassNames.EffectMeshXRayGridP1);
        }

        //Second pass, remove not covered part from stencil buffer
        if (UseDepthOcclusion)
            for (var i = 0; i < currentCores.Count; ++i) {
                var mesh = currentCores[i].SceneNode;
                RenderPass(mesh, DefaultPassNames.EffectMeshXRayGridP2);
            }

        OnUpdatePerModelStruct(context);
        modelCb.Upload(deviceContext, ref modelStruct);
        
        //Thrid pass, draw mesh with grid overlay
        foreach (var (mesh, effect) in currentCores) {
            var color = Color;
            if (effect.TryGetAttribute(EffectAttributeNames.ColorAttributeName, out var attribute) 
                && attribute is string colorStr) 
                color = colorStr.ToColor4();
            
            if (modelStruct.Color != color) {
                modelStruct.Color = color;
                modelCb.Upload(deviceContext, ref modelStruct);
            }

            RenderPass(mesh, XRayDrawingPassName,
                      pass => {
                          if (mesh.RenderCore is IMaterialRenderParams material)
                              material.MaterialVariables.BindMaterialResources(context, deviceContext, pass);
                      });
        }

        currentCores.Clear();

        void RenderPass(SceneNode mesh, string passName, Action<ShaderPass>? bindResources = null) {
            context.CustomPassName = passName;
            var pass = mesh.EffectTechnique?[passName];
            if (pass is null || pass.IsNull) 
                return;
            
            pass.BindShader(deviceContext);
            pass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
            bindResources?.Invoke(pass);
            mesh.RenderCustom(context, deviceContext);
        }
    }

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.Param.M11 = gridDensity;
        modelStruct.Param.M12 = dimmingFactor;
        modelStruct.Param.M13 = blendingFactor;
    }

#region Variables

    private readonly List<(SceneNode SceneNode, IEffectAttributes Effect)> currentCores = [];
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
    } = DefaultRenderTechniqueNames.PostEffectMeshXRayGrid;

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

    private int gridDensity = 8;

    /// <summary>
    ///     Gets or sets the grid density.
    /// </summary>
    /// <value>
    ///     The grid density.
    /// </value>
    public int GridDensity {
        get => gridDensity;
        set => SetAffectsRender(ref gridDensity, value);
    }

    private float dimmingFactor = 0.8f;

    /// <summary>
    ///     Gets or sets the dim factor on original color
    /// </summary>
    /// <value>
    ///     The dim factor.
    /// </value>
    public float DimmingFactor {
        get => dimmingFactor;
        set => SetAffectsRender(ref dimmingFactor, value);
    }

    private float blendingFactor = 1f;

    /// <summary>
    ///     Gets or sets the blending factor for grid and original mesh color blending
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public float BlendingFactor {
        get => blendingFactor;
        set => SetAffectsRender(ref blendingFactor, value);
    }

    /// <summary>
    ///     Gets or sets the name of the x ray drawing pass. This is the final pass to draw mesh and grid overlay onto render
    ///     target
    /// </summary>
    /// <value>
    ///     The name of the x ray drawing pass.
    /// </value>
    public string XRayDrawingPassName { get; set; } = DefaultPassNames.EffectMeshXRayGridP3;

    /// <summary>
    ///     Uses the scene depth buffer to hide x-ray parts that are not occluded.
    ///     Disable this for overlays that must stay visible after OIT rendering.
    /// </summary>
    public bool UseDepthOcclusion {
        get;
        set => SetAffectsRender(ref field, value);
    } = true;

#endregion
}
