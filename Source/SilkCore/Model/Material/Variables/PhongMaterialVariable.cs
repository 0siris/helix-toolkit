/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
///     Default PhongMaterial Variables
/// </summary>
public class PhongMaterialVariables : MaterialVariable {
    private const int Numtextures = 6;

    private const int DiffuseIdx = 0,
                      AlphaIdx = 1,
                      NormalIdx = 2,
                      DisplaceIdx = 3,
                      SpecularColorIdx = 4,
                      EmissiveIdx = 5;

    private readonly PhongMaterialCore material;
    private readonly IStatePoolManager statePoolManager;

    private readonly ITextureResourceManager textureManager;
    private readonly ShaderResourceViewProxy?[] textureResources = new ShaderResourceViewProxy?[Numtextures];

    private int samplerDiffuseSlot, samplerDisplaceSlot, samplerShadowSlot;
    private SamplerStateProxy? surfaceSampler, displacementSampler, shadowSampler;

    private int texDiffuseSlot,
                texAlphaSlot,
                texNormalSlot,
                texDisplaceSlot,
                texShadowSlot,
                texSpecularSlot,
                texEmissiveSlot,
                texSsaoSlot,
                texEnvironmentSlot;

    private uint textureIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PhongMaterialVariables" /> class.
    /// </summary>
    /// <param name="manager">The manager.</param>
    /// <param name="technique">The technique.</param>
    /// <param name="materialCore">The material core.</param>
    /// <param name="defaultPassName">Default pass name</param>
    public PhongMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique,
        PhongMaterialCore materialCore,
        string defaultPassName = DefaultPassNames.Default
    )
        : base(manager, technique, DefaultMeshConstantBufferDesc, materialCore) {
        material = materialCore;
        texDiffuseSlot = texAlphaSlot = texDisplaceSlot = texNormalSlot = -1;
        samplerDiffuseSlot = samplerDisplaceSlot = samplerShadowSlot = -1;
        textureManager = manager.MaterialTextureManager;
        statePoolManager = manager.StateManager;

        MaterialPass = technique[defaultPassName];
        OitPass = technique[DefaultPassNames.OitPass];
        OitDepthPeelingInit = technique[DefaultPassNames.OitDepthPeelingInit];
        OitDepthPeeling = technique[DefaultPassNames.OitDepthPeeling];
        ShadowPass = technique[DefaultPassNames.ShadowPass];
        WireframePass = technique[DefaultPassNames.Wireframe];
        WireframeOitPass = technique[DefaultPassNames.WireframeOitPass];
        WireframeOitdpPass = technique[DefaultPassNames.WireframeOitdpPass];
        TessellationPass = technique[DefaultPassNames.MeshTriTessellation];
        TessellationOitPass = technique[DefaultPassNames.MeshTriTessellationOit];
        TessellationOitdpPass = technique[DefaultPassNames.MeshPbrTriTessellationOitdp];
        DepthPass = technique[DefaultPassNames.DepthPrepass];
        UpdateMappings(MaterialPass);
        EnableTessellation = materialCore.EnableTessellation;
    }

    private bool HasTextures => textureIndex != 0;

    public ShaderPass MaterialPass { get; }

    public ShaderPass OitPass { get; }

    public ShaderPass OitDepthPeelingInit { get; }

    public ShaderPass OitDepthPeeling { get; }

    public ShaderPass ShadowPass { get; }

    public ShaderPass WireframePass { get; }

    public ShaderPass WireframeOitPass { get; }

    public ShaderPass WireframeOitdpPass { get; }

    public ShaderPass TessellationPass { get; }

    public ShaderPass TessellationOitPass { get; }

    public ShaderPass TessellationOitdpPass { get; }

    public ShaderPass DepthPass { get; }

    /// <summary>
    /// </summary>
    public string ShaderAlphaTexName { get; } = DefaultBufferNames.AlphaMapTb;

    /// <summary>
    /// </summary>
    public string ShaderDiffuseTexName { get; } = DefaultBufferNames.DiffuseMapTb;

    /// <summary>
    /// </summary>
    public string ShaderNormalTexName { get; } = DefaultBufferNames.NormalMapTb;

    /// <summary>
    /// </summary>
    public string ShaderDisplaceTexName { get; } = DefaultBufferNames.DisplacementMapTb;

    /// <summary>
    ///     Gets or sets the name of the shader shadow tex.
    /// </summary>
    /// <value>
    ///     The name of the shader shadow tex.
    /// </value>
    public string ShaderShadowTexName { get; } = DefaultBufferNames.ShadowMapTb;

    /// <summary>
    ///     Gets the shader specular texture.
    /// </summary>
    /// <value>
    ///     The shader specular texture.
    /// </value>
    public string ShaderSpecularTexName { get; } = DefaultBufferNames.SpecularTb;

    /// <summary>
    ///     Gets the name of the shader emissive tex.
    /// </summary>
    /// <value>
    ///     The name of the shader emissive tex.
    /// </value>
    public string ShaderEmissiveTexName { get; } = DefaultBufferNames.EmissiveTb;

    /// <summary>
    /// </summary>
    public string ShaderSamplerDiffuseTexName { get; } = DefaultSamplerStateNames.SurfaceSampler;

    /// <summary>
    /// </summary>
    public string ShaderSamplerDisplaceTexName { get; } = DefaultSamplerStateNames.DisplacementMapSampler;

    /// <summary>
    /// </summary>
    public string ShaderSamplerShadowMapName { get; } = DefaultSamplerStateNames.ShadowMapSampler;

    public bool EnableTessellation {
        get;
        private set {
            if (Set(ref field, value)) {
                UpdateMappings(CurrentMaterialPass);
                InvalidateRenderer();
            }
        }
    }

    private ShaderPass CurrentMaterialPass => EnableTessellation ? TessellationPass : MaterialPass;

    protected override void OnInitialPropertyBindings() {
        AddPropertyBinding(nameof(PhongMaterialCore.DiffuseColor),
                           () => { WriteValue(PhongPbrMaterialStruct.DiffuseStr, material.DiffuseColor); });
        AddPropertyBinding(nameof(PhongMaterialCore.AmbientColor),
                           () => { WriteValue(PhongPbrMaterialStruct.AmbientStr, material.AmbientColor); });
        AddPropertyBinding(nameof(PhongMaterialCore.EmissiveColor),
                           () => { WriteValue(PhongPbrMaterialStruct.EmissiveStr, material.EmissiveColor); });
        AddPropertyBinding(nameof(PhongMaterialCore.ReflectiveColor),
                           () => { WriteValue(PhongPbrMaterialStruct.ReflectStr, material.ReflectiveColor); });
        AddPropertyBinding(nameof(PhongMaterialCore.SpecularColor),
                           () => { WriteValue(PhongPbrMaterialStruct.SpecularStr, material.SpecularColor); });
        AddPropertyBinding(nameof(PhongMaterialCore.SpecularShininess),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.ShininessStr, material.SpecularShininess);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.DisplacementMapScaleMask),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.DisplacementMapScaleMaskStr,
                                          material.DisplacementMapScaleMask);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderShadowMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RenderShadowMapStr,
                                          material.RenderShadowMap ? 1 : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderEnvironmentMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasCubeMapStr,
                                          material.RenderEnvironmentMap ? 1 : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.UvTransform),
                           () => {
                               Matrix m = material.UvTransform;
                               WriteValue(PhongPbrMaterialStruct.UvTransformR1Str, m.Column1);
                               WriteValue(PhongPbrMaterialStruct.UvTransformR2Str, m.Column2);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.EnableAutoTangent),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.EnableAutoTangent, material.EnableAutoTangent);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.MaxTessellationDistance),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MaxTessDistanceStr,
                                          material.MaxTessellationDistance);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.MaxDistanceTessellationFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MaxDistTessFactorStr,
                                          material.MaxDistanceTessellationFactor);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.MinTessellationDistance),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MinTessDistanceStr,
                                          material.MinTessellationDistance);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.MinDistanceTessellationFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MinDistTessFactorStr,
                                          material.MinDistanceTessellationFactor);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderDiffuseMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasDiffuseMapStr,
                                          material.RenderDiffuseMap && textureResources[DiffuseIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderDiffuseAlphaMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasDiffuseAlphaMapStr,
                                          material.RenderDiffuseAlphaMap && textureResources[AlphaIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderNormalMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasNormalMapStr,
                                          material.RenderNormalMap && textureResources[NormalIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderSpecularColorMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasSpecularColorMap,
                                          material.RenderSpecularColorMap &&
                                          textureResources[SpecularColorIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderDisplacementMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasDisplacementMapStr,
                                          material.RenderDisplacementMap &&
                                          textureResources[DisplaceIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.RenderEmissiveMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasEmissiveMapStr,
                                          material.RenderEmissiveMap && textureResources[EmissiveIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.EnableFlatShading),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RenderFlat, material.EnableFlatShading);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.VertexColorBlendingFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.VertColorBlending,
                                          material.VertexColorBlendingFactor);
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.DiffuseMap),
                           () => {
                               CreateTextureView(material.DiffuseMap, DiffuseIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderDiffuseMap));
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.DiffuseAlphaMap),
                           () => {
                               CreateTextureView(material.DiffuseAlphaMap, AlphaIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderDiffuseAlphaMap));
                           });

        AddPropertyBinding(nameof(PhongMaterialCore.NormalMap),
                           () => {
                               CreateTextureView(material.NormalMap, NormalIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderNormalMap));
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.DisplacementMap),
                           () => {
                               CreateTextureView(material.DisplacementMap, DisplaceIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderDisplacementMap));
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.SpecularColorMap),
                           () => {
                               CreateTextureView(material.SpecularColorMap, SpecularColorIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderSpecularColorMap));
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.DiffuseMapSampler),
                           () => {
                               var newSampler = statePoolManager.Register(material.DiffuseMapSampler);
                               RemoveAndDispose(ref surfaceSampler);
                               surfaceSampler = newSampler;
                           });

        AddPropertyBinding(nameof(PhongMaterialCore.DisplacementMapSampler),
                           () => {
                               var newDisplaceSampler =
                                   statePoolManager.Register(material.DisplacementMapSampler);
                               RemoveAndDispose(ref displacementSampler);
                               displacementSampler = newDisplaceSampler;
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.EmissiveMap),
                           () => {
                               CreateTextureView(material.EmissiveMap, EmissiveIdx);
                               TriggerPropertyAction(nameof(PhongMaterialCore.RenderEmissiveMap));
                           });
        AddPropertyBinding(nameof(PhongMaterialCore.EnableTessellation),
                           () => { EnableTessellation = material.EnableTessellation; });

        shadowSampler = statePoolManager.Register(DefaultSamplers.ShadowSampler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateTextureView(TextureModel? textureModel, int index) {
        var newTexture = textureModel == null ? null : textureManager.Register(textureModel);
        RemoveAndDispose(ref textureResources[index]);
        textureResources[index] = newTexture;
        if (textureResources[index] != null)
            textureIndex |= 1u << index;
        else
            textureIndex &= ~(1u << index);
    }

    public override bool BindMaterialResources(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass shaderPass
    ) {
        if (HasTextures) {
            OnBindMaterialTextures(deviceContext, shaderPass.VertexShader);
            OnBindMaterialTextures(deviceContext, shaderPass.DomainShader);
            OnBindMaterialTextures(context, deviceContext, shaderPass.PixelShader);
        }

        if (material.RenderShadowMap && context.IsShadowMapEnabled) {
            shaderPass.PixelShader.BindTexture(deviceContext, texShadowSlot, context.SharedResource.ShadowView);
            shaderPass.PixelShader.BindSampler(deviceContext, samplerShadowSlot, shadowSampler);
        }

        shaderPass.PixelShader.BindTexture(deviceContext, texSsaoSlot, context.SharedResource.SsaoMap);
        shaderPass.PixelShader.BindTexture(deviceContext,
                                           texEnvironmentSlot,
                                           context.SharedResource.EnvironementMap);
        return true;
    }

    /// <summary>
    ///     Actual bindings
    /// </summary>
    /// <param name="context"></param>
    /// <param name="shader"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnBindMaterialTextures(DeviceContextProxy context, VertexShader shader) {
        if (shader.IsNull) return;
        var idx = shader.ShaderStageIndex;
        shader.BindTexture(context, texDisplaceSlot, textureResources[DisplaceIdx]);
        shader.BindSampler(context, samplerDisplaceSlot, displacementSampler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnBindMaterialTextures(DeviceContextProxy context, DomainShader shader) {
        if (shader.IsNull) return;
        var idx = shader.ShaderStageIndex;
        shader.BindTexture(context, texDisplaceSlot, textureResources[DisplaceIdx]);
        shader.BindSampler(context, samplerDisplaceSlot, displacementSampler);
    }

    /// <summary>
    ///     Actual bindings
    /// </summary>
    /// <param name="context"></param>
    /// <param name="deviceContext"></param>
    /// <param name="shader"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnBindMaterialTextures(
        RenderContext context,
        DeviceContextProxy deviceContext,
        PixelShader shader
    ) {
        if (shader.IsNull) return;
        var idx = shader.ShaderStageIndex;
        shader.BindTexture(deviceContext, texDiffuseSlot, textureResources[DiffuseIdx]);
        shader.BindTexture(deviceContext, texNormalSlot, textureResources[NormalIdx]);
        shader.BindTexture(deviceContext, texAlphaSlot, textureResources[AlphaIdx]);
        shader.BindTexture(deviceContext, texSpecularSlot, textureResources[SpecularColorIdx]);
        shader.BindTexture(deviceContext, texEmissiveSlot, textureResources[EmissiveIdx]);
        shader.BindSampler(deviceContext, samplerDiffuseSlot, surfaceSampler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMappings(ShaderPass shaderPass) {
        texDiffuseSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderDiffuseTexName);
        texAlphaSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderAlphaTexName);
        texNormalSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderNormalTexName);
        texShadowSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderShadowTexName);
        texSpecularSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderSpecularTexName);
        texEmissiveSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderEmissiveTexName);
        texSsaoSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SsaoMapTb);
        texEnvironmentSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.CubeMapTb);
        samplerDiffuseSlot = shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(ShaderSamplerDiffuseTexName);
        samplerShadowSlot = shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(ShaderSamplerShadowMapName);
        if (!shaderPass.DomainShader.IsNull && material.EnableTessellation) {
            texDisplaceSlot =
                shaderPass.DomainShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames
                    .DisplacementMapTb);
            samplerDisplaceSlot =
                shaderPass.DomainShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames
                                                                          .DisplacementMapSampler);
        } else {
            texDisplaceSlot =
                shaderPass.VertexShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames
                    .DisplacementMapTb);
            samplerDisplaceSlot =
                shaderPass.VertexShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames
                                                                          .DisplacementMapSampler);
        }
    }


    /// <summary>
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    protected override void OnDispose(bool disposeManagedResources) {
        for (var i = 0; i < textureResources.Length; ++i) RemoveAndDispose(ref textureResources[i]);
        RemoveAndDispose(ref surfaceSampler);
        RemoveAndDispose(ref displacementSampler);
        RemoveAndDispose(ref shadowSampler);
        base.OnDispose(disposeManagedResources);
    }

    public override ShaderPass GetPass(RenderType renderType, RenderContext context) {
        if (renderType == RenderType.Transparent)
            switch (context.OitRenderStage) {
                case OitRenderStage.SinglePassWeighted:
                    return EnableTessellation ? TessellationOitPass : OitPass;
                case OitRenderStage.DepthPeelingInitMinMaxZ:
                    return OitDepthPeelingInit;
                case OitRenderStage.DepthPeeling:
                    return EnableTessellation ? TessellationOitdpPass : OitDepthPeeling;
            }

        return CurrentMaterialPass;
    }

    public override ShaderPass GetShadowPass(RenderType renderType, RenderContext context) => ShadowPass;

    public override ShaderPass GetWireframePass(RenderType renderType, RenderContext context) {
        if (renderType == RenderType.Transparent)
            switch (context.OitRenderStage) {
                case OitRenderStage.SinglePassWeighted:
                    return WireframeOitPass;
                case OitRenderStage.DepthPeelingInitMinMaxZ:
                    return OitDepthPeelingInit;
                case OitRenderStage.DepthPeeling:
                    return WireframeOitdpPass;
            }

        return WireframePass;
    }

    public override ShaderPass GetDepthPass(RenderType renderType, RenderContext context) => DepthPass;

    public override void Draw(
        DeviceContextProxy deviceContext,
        IAttachableBufferModel? bufferModel,
        int instanceCount
    ) {
        if (bufferModel?.IndexBuffer is { } indexBuffer)
            DrawIndexed(deviceContext, indexBuffer.ElementCount, instanceCount);
    }
}
