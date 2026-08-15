/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model.Material.Variables;
/// <summary>
/// </summary>
public class PointMaterialVariable : MaterialVariable {
    private readonly PointMaterialCore material;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PointMaterialVariable" /> class.
    /// </summary>
    /// <param name="manager">The manager.</param>
    /// <param name="technique">The technique.</param>
    /// <param name="materialCore">The material core.</param>
    /// <param name="defaultPassName">Default pass name</param>
    public PointMaterialVariable(
        IEffectsManager manager,
        IRenderTechnique technique,
        PointMaterialCore materialCore,
        string defaultPassName = DefaultPassNames.Default
    )
        : base(manager, technique, DefaultPointLineConstantBufferDesc, materialCore) {
        PointPass = technique[defaultPassName];
        ShadowPass = technique[DefaultPassNames.ShadowPass];
        DepthPass = technique[DefaultPassNames.DepthPrepass];
        material = materialCore;
    }

    public ShaderPass PointPass { get; }

    public ShaderPass ShadowPass { get; }

    public ShaderPass DepthPass { get; }

    protected override void OnInitialPropertyBindings() {
        AddPropertyBinding(nameof(PointMaterialCore.PointColor),
                           () => { WriteValue(PointLineMaterialStruct.ColorStr, material.PointColor); });
        AddPropertyBinding(nameof(PointMaterialCore.Width),
                           () => {
                               WriteValue(PointLineMaterialStruct.ParamsStr,
                                          new Vector4(material.Width,
                                                      material.Height,
                                                      (int)material.Figure,
                                                      material.FigureRatio));
                           });
        AddPropertyBinding(nameof(PointMaterialCore.Height),
                           () => {
                               WriteValue(PointLineMaterialStruct.ParamsStr,
                                          new Vector4(material.Width,
                                                      material.Height,
                                                      (int)material.Figure,
                                                      material.FigureRatio));
                           });
        AddPropertyBinding(nameof(PointMaterialCore.Figure),
                           () => {
                               WriteValue(PointLineMaterialStruct.ParamsStr,
                                          new Vector4(material.Width,
                                                      material.Height,
                                                      (int)material.Figure,
                                                      material.FigureRatio));
                           });
        AddPropertyBinding(nameof(PointMaterialCore.FigureRatio),
                           () => {
                               WriteValue(PointLineMaterialStruct.ParamsStr,
                                          new Vector4(material.Width,
                                                      material.Height,
                                                      (int)material.Figure,
                                                      material.FigureRatio));
                           });
        AddPropertyBinding(nameof(PointMaterialCore.EnableDistanceFading),
                           () => {
                               WriteValue(PointLineMaterialStruct.EnableDistanceFading,
                                          material.EnableDistanceFading ? 1 : 0);
                           });
        AddPropertyBinding(nameof(PointMaterialCore.FadingNearDistance),
                           () => {
                               WriteValue(PointLineMaterialStruct.FadeNearDistance,
                                          material.FadingNearDistance);
                           });
        AddPropertyBinding(nameof(PointMaterialCore.FadingFarDistance),
                           () => {
                               WriteValue(PointLineMaterialStruct.FadeFarDistance, material.FadingFarDistance);
                           });
        AddPropertyBinding(nameof(PointMaterialCore.FixedSize),
                           () => { WriteValue(PointLineMaterialStruct.FixedSize, material.FixedSize); });
        AddPropertyBinding(nameof(PointMaterialCore.EnableColorBlending),
                           () => {
                               WriteValue(PointLineMaterialStruct.EnableBlendingStr,
                                          material.EnableColorBlending);
                           });
        AddPropertyBinding(nameof(PointMaterialCore.BlendingFactor),
                           () => {
                               WriteValue(PointLineMaterialStruct.BlendingFactorStr, material.BlendingFactor);
                           });
    }

    public override void Draw(
        DeviceContextProxy deviceContext,
        IAttachableBufferModel? bufferModel,
        int instanceCount
    ) {
        if (bufferModel?.VertexBuffer.FirstOrDefault() is { } vertexBuffer)
            DrawPoints(deviceContext, vertexBuffer.ElementCount, instanceCount);
    }

    public override ShaderPass GetPass(RenderType renderType, RenderContext context) => PointPass;

    public override ShaderPass GetShadowPass(RenderType renderType, RenderContext context) => ShadowPass;

    public override ShaderPass GetWireframePass(RenderType renderType, RenderContext context) => ShaderPass.NullPass;

    public override ShaderPass GetDepthPass(RenderType renderType, RenderContext context) => DepthPass;

    public override bool BindMaterialResources(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass shaderPass
    )
        => true;

    protected override void UpdateInternalVariables(DeviceContextProxy deviceContext) { }
}
