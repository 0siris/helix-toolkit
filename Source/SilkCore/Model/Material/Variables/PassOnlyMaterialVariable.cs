/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
/// </summary>
public sealed class PassOnlyMaterialVariable : MaterialVariable {
    private readonly string passName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PassOnlyMaterialVariable" /> class.
    /// </summary>
    /// <param name="passName">Name of the pass.</param>
    /// <param name="technique">The technique.</param>
    /// <param name="shadowPassName">Name of the shadow pass.</param>
    /// <param name="wireframePassName">Name of the wireframe pass.</param>
    /// <param name="depthPassName">Name of the depth pass</param>
    public PassOnlyMaterialVariable(
        string passName,
        IRenderTechnique technique,
        string shadowPassName = DefaultPassNames.ShadowPass,
        string wireframePassName = DefaultPassNames.Wireframe,
        string depthPassName = DefaultPassNames.DepthPrepass
    )
        : base(technique.EffectsManager, technique, DefaultMeshConstantBufferDesc, null) {
        this.passName = passName;
        MaterialPass = technique[passName];
        ShadowPass = technique[shadowPassName];
        WireframePass = technique[wireframePassName];
        DepthPass = technique[depthPassName];
    }

    public ShaderPass MaterialPass { get; }

    public ShaderPass ShadowPass { get; }

    public ShaderPass WireframePass { get; }

    public ShaderPass DepthPass { get; }

    public override bool BindMaterialResources(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass shaderPass
    )
        => true;

    public override ShaderPass GetPass(RenderType renderType, RenderContext context) => MaterialPass;

    public override ShaderPass GetShadowPass(RenderType renderType, RenderContext context) => ShadowPass;

    public override ShaderPass GetWireframePass(RenderType renderType, RenderContext context) => WireframePass;

    public override ShaderPass GetDepthPass(RenderType renderType, RenderContext context) => DepthPass;

    public override void Draw(
        DeviceContextProxy deviceContext,
        IAttachableBufferModel bufferModel,
        int instanceCount
    ) {
        DrawIndexed(deviceContext, bufferModel.IndexBuffer.ElementCount, instanceCount);
    }
}
