/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Model.Material.Variables;
/// <summary>
///     Physics based rendering material
/// </summary>
public class PbrMaterialVariable : MaterialVariable {
    private const int Numtextures = 7;
    private const int Numsamplers = 4;

    private const int AlbedoMapIdx = 0,
                      NormalMapIdx = 1,
                      RmMapIdx = 2,
                      EmissiveMapIdx = 3,
                      IrradianceMapIdx = 4,
                      DisplaceMapIdx = 5,
                      AoMapIdx = 6;

    private const int SurfaceSamplerIdx = 0, IblSamplerIdx = 1, ShadowSamplerIdx = 2, DisplaceSamplerIdx = 3;

    private readonly PbrMaterialCore material;
    private readonly SamplerStateProxy?[] samplerResources = new SamplerStateProxy?[Numsamplers];
    private readonly IStatePoolManager statePoolManager;
    private readonly ITextureResourceManager textureManager;
    private readonly ShaderResourceViewProxy?[] textureResources = new ShaderResourceViewProxy?[Numtextures];
    private int samplerSurfaceSlot, samplerIblSlot, samplerShadowSlot, samplerDisplaceSlot;

    private int texDiffuseSlot,
                texNormalSlot,
                texRmSlot,
                texEmissiveSlot,
                texIrradianceSlot,
                texDisplaceSlot,
                texShadowSlot,
                texAoSlot,
                texSsaoSlot,
                texEnvironmentSlot;

    private uint textureIndex;

    public PbrMaterialVariable(
        IEffectsManager manager,
        IRenderTechnique technique,
        PbrMaterialCore core,
        string defaultPassName = DefaultPassNames.Pbr
    )
        : base(manager, technique, DefaultMeshConstantBufferDesc, core) {
        textureManager = manager.MaterialTextureManager;
        statePoolManager = manager.StateManager;
        material = core;
        MaterialPass = technique[defaultPassName];
        OitPass = technique[DefaultPassNames.PbroitPass];
        OitDepthPeelingInit = technique[DefaultPassNames.OitDepthPeelingInit];
        OitDepthPeeling = technique[DefaultPassNames.PbroitdpPass];
        TessellationPass = technique[DefaultPassNames.MeshPbrTriTessellation];
        TessellationOitPass = technique[DefaultPassNames.MeshPbrTriTessellationOit];
        TessellationOitdpPass = technique[DefaultPassNames.MeshPbrTriTessellationOitdp];
        WireframePass = technique[DefaultPassNames.Wireframe];
        WireframeOitPass = technique[DefaultPassNames.WireframeOitPass];
        WireframeOitdpPass = technique[DefaultPassNames.WireframeOitdpPass];
        ShadowPass = technique[DefaultPassNames.ShadowPass];
        DepthPass = technique[DefaultPassNames.DepthPrepass];
        UpdateMappings(MaterialPass);
        CreateTextureViews();
        CreateSamplers();
    }

    private bool HasTextures => textureIndex != 0;

    public ShaderPass MaterialPass { get; }

    public ShaderPass OitPass { get; }

    public ShaderPass OitDepthPeelingInit { get; }

    public ShaderPass OitDepthPeeling { get; }

    public ShaderPass TessellationPass { get; }

    public ShaderPass TessellationOitPass { get; }

    public ShaderPass TessellationOitdpPass { get; }

    public ShaderPass ShadowPass { get; }

    public ShaderPass WireframePass { get; }

    public ShaderPass WireframeOitPass { get; }

    public ShaderPass WireframeOitdpPass { get; }

    public ShaderPass DepthPass { get; }

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
        AddPropertyBinding(nameof(PbrMaterialCore.AlbedoColor),
                           () => { WriteValue(PhongPbrMaterialStruct.DiffuseStr, material.AlbedoColor); });
        AddPropertyBinding(nameof(PbrMaterialCore.EmissiveColor),
                           () => { WriteValue(PhongPbrMaterialStruct.EmissiveStr, material.EmissiveColor); });
        AddPropertyBinding(nameof(PbrMaterialCore.MetallicFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.ConstantMetallic, material.MetallicFactor);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RoughnessFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RoughnessStr, material.RoughnessFactor);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.AmbientOcclusionFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.AmbientOcclusionStr,
                                          material.AmbientOcclusionFactor);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.ReflectanceFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.ReflectanceStr, material.ReflectanceFactor);
                           });

        AddPropertyBinding(nameof(PbrMaterialCore.ClearCoatStrength),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.ClearCoatStr, material.ClearCoatStrength);
                           });

        AddPropertyBinding(nameof(PbrMaterialCore.ClearCoatRoughness),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.ClearCoatRoughnessStr,
                                          material.ClearCoatRoughness);
                           });

        AddPropertyBinding(nameof(PbrMaterialCore.RenderAlbedoMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasDiffuseMapStr,
                                          material.RenderAlbedoMap && textureResources[AlbedoMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderEmissiveMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasEmissiveMapStr,
                                          material.RenderEmissiveMap && textureResources[EmissiveMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderNormalMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasNormalMapStr,
                                          material.RenderNormalMap && textureResources[NormalMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderDisplacementMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasDisplacementMapStr,
                                          material.RenderDisplacementMap &&
                                          textureResources[DisplaceMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderIrradianceMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasIrradianceMapStr,
                                          material.RenderIrradianceMap &&
                                          textureResources[IrradianceMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderRoughnessMetallicMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasRmMapStr,
                                          material.RenderRoughnessMetallicMap &&
                                          textureResources[RmMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderAmbientOcclusionMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasAoMapStr,
                                          material.RenderAmbientOcclusionMap &&
                                          textureResources[AoMapIdx] != null
                                              ? 1
                                              : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.EnableAutoTangent),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.EnableAutoTangent, material.EnableAutoTangent);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.DisplacementMapScaleMask),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.DisplacementMapScaleMaskStr,
                                          material.DisplacementMapScaleMask);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderShadowMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RenderShadowMapStr,
                                          material.RenderShadowMap ? 1 : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RenderEnvironmentMap),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.HasCubeMapStr,
                                          material.RenderEnvironmentMap ? 1 : 0);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.MaxTessellationDistance),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MaxTessDistanceStr,
                                          material.MaxTessellationDistance);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.MinTessellationDistance),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MinTessDistanceStr,
                                          material.MinTessellationDistance);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.MaxDistanceTessellationFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MaxDistTessFactorStr,
                                          material.MaxDistanceTessellationFactor);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.MinDistanceTessellationFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.MinDistTessFactorStr,
                                          material.MinDistanceTessellationFactor);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.UvTransform),
                           () => {
                               Matrix m = material.UvTransform;
                               WriteValue(PhongPbrMaterialStruct.UvTransformR1Str, m.Column1);
                               WriteValue(PhongPbrMaterialStruct.UvTransformR2Str, m.Column2);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.AlbedoMap),
                           () => {
                               CreateTextureView(material.AlbedoMap, AlbedoMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderAlbedoMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.EmissiveMap),
                           () => {
                               CreateTextureView(material.EmissiveMap, EmissiveMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderEmissiveMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.NormalMap),
                           () => {
                               CreateTextureView(material.NormalMap, NormalMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderNormalMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.IrradianceMap),
                           () => {
                               CreateTextureView(material.IrradianceMap, IrradianceMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderIrradianceMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.DisplacementMap),
                           () => {
                               CreateTextureView(material.DisplacementMap, DisplaceMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderDisplacementMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.RoughnessMetallicMap),
                           () => {
                               CreateTextureView(material.RoughnessMetallicMap, RmMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderRoughnessMetallicMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.AmbientOcculsionMap),
                           () => {
                               CreateTextureView(material.AmbientOcculsionMap, AoMapIdx);
                               TriggerPropertyAction(nameof(PbrMaterialCore.RenderAmbientOcclusionMap));
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.SurfaceMapSampler),
                           () => { CreateSampler(material.SurfaceMapSampler, SurfaceSamplerIdx); });
        AddPropertyBinding(nameof(PbrMaterialCore.IblSampler),
                           () => { CreateSampler(material.IblSampler, IblSamplerIdx); });
        AddPropertyBinding(nameof(PbrMaterialCore.DisplacementMapSampler),
                           () => { CreateSampler(material.DisplacementMapSampler, DisplaceSamplerIdx); });
        AddPropertyBinding(nameof(PbrMaterialCore.EnableTessellation),
                           () => { EnableTessellation = material.EnableTessellation; });

        WriteValue(PhongPbrMaterialStruct.RenderPbr, true); // Make sure to set this flag
        AddPropertyBinding(nameof(PbrMaterialCore.EnableFlatShading),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RenderFlat, material.EnableFlatShading);
                           });
        AddPropertyBinding(nameof(PbrMaterialCore.VertexColorBlendingFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.VertColorBlending,
                                          material.VertexColorBlendingFactor);
                           });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateTextureView(TextureModel? texture, int index) {
        var newTexture = texture == null ? null : textureManager.Register(texture);
        RemoveAndDispose(ref textureResources[index]);
        textureResources[index] = newTexture;
        if (textureResources[index] != null)
            textureIndex |= 1u << index;
        else
            textureIndex &= ~(1u << index);
    }

    private void CreateTextureViews() {
        CreateTextureView(material.AlbedoMap, AlbedoMapIdx);
        CreateTextureView(material.NormalMap, NormalMapIdx);
        CreateTextureView(material.DisplacementMap, DisplaceMapIdx);
        CreateTextureView(material.EmissiveMap, EmissiveMapIdx);
        CreateTextureView(material.IrradianceMap, IrradianceMapIdx);
        CreateTextureView(material.RoughnessMetallicMap, RmMapIdx);
        CreateTextureView(material.AmbientOcculsionMap, AoMapIdx);
    }

    private void CreateSamplers() {
        var newSurfaceSampler = statePoolManager.Register(material.SurfaceMapSampler);
        var newIblSampler = statePoolManager.Register(material.IblSampler);
        var newDisplaceSampler = statePoolManager.Register(material.DisplacementMapSampler);
        var newShadowSampler = statePoolManager.Register(DefaultSamplers.ShadowSampler);
        RemoveAndDispose(ref samplerResources[SurfaceSamplerIdx]);
        RemoveAndDispose(ref samplerResources[IblSamplerIdx]);
        RemoveAndDispose(ref samplerResources[DisplaceSamplerIdx]);
        RemoveAndDispose(ref samplerResources[ShadowSamplerIdx]);
        samplerResources[SurfaceSamplerIdx] = newSurfaceSampler;
        samplerResources[IblSamplerIdx] = newIblSampler;
        samplerResources[DisplaceSamplerIdx] = newDisplaceSampler;
        samplerResources[ShadowSamplerIdx] = newShadowSampler;
    }

    private void CreateSampler(SamplerStateDescription desc, int index) {
        var newRes = statePoolManager.Register(desc);
        RemoveAndDispose(ref samplerResources[index]);
        samplerResources[index] = newRes;
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
            shaderPass.PixelShader.BindSampler(deviceContext,
                                               samplerShadowSlot,
                                               samplerResources[ShadowSamplerIdx]);
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
        shader.BindTexture(context, texDisplaceSlot, textureResources[DisplaceMapIdx]);
        shader.BindSampler(context, samplerDisplaceSlot, samplerResources[DisplaceSamplerIdx]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnBindMaterialTextures(DeviceContextProxy context, DomainShader shader) {
        if (shader.IsNull) return;
        shader.BindTexture(context, texDisplaceSlot, textureResources[DisplaceMapIdx]);
        shader.BindSampler(context, samplerDisplaceSlot, samplerResources[DisplaceSamplerIdx]);
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
        shader.BindTexture(deviceContext, texDiffuseSlot, textureResources[AlbedoMapIdx]);
        shader.BindTexture(deviceContext, texNormalSlot, textureResources[NormalMapIdx]);
        shader.BindTexture(deviceContext, texRmSlot, textureResources[RmMapIdx]);
        shader.BindTexture(deviceContext, texAoSlot, textureResources[AoMapIdx]);
        shader.BindTexture(deviceContext, texEmissiveSlot, textureResources[EmissiveMapIdx]);
        shader.BindTexture(deviceContext, texIrradianceSlot, textureResources[IrradianceMapIdx]);

        shader.BindSampler(deviceContext, samplerSurfaceSlot, samplerResources[SurfaceSamplerIdx]);
        shader.BindSampler(deviceContext, samplerIblSlot, samplerResources[IblSamplerIdx]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMappings(ShaderPass shaderPass) {
        texDiffuseSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.DiffuseMapTb);
        texEmissiveSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.EmissiveTb);
        texNormalSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.NormalMapTb);
        texRmSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.RmMapTb);
        texAoSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.AoMapTb);
        texShadowSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.ShadowMapTb);
        texIrradianceSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.IrradianceMap);
        texSsaoSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SsaoMapTb);
        texEnvironmentSlot =
            shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.CubeMapTb);
        samplerSurfaceSlot =
            shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
        samplerIblSlot =
            shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.IblSampler);
        samplerShadowSlot =
            shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.ShadowMapSampler);

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

    public override void Draw(
        DeviceContextProxy deviceContext,
        IAttachableBufferModel? bufferModel,
        int instanceCount
    ) {
        if (bufferModel?.IndexBuffer is { } indexBuffer)
            DrawIndexed(deviceContext, indexBuffer.ElementCount, instanceCount);
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

    public override ShaderPass GetDepthPass(RenderType renderType, RenderContext context) => DepthPass;

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

    protected override void OnDispose(bool disposeManagedResources) {
        for (var i = 0; i < samplerResources.Length; ++i) RemoveAndDispose(ref samplerResources[i]);
        for (var i = 0; i < textureResources.Length; ++i) RemoveAndDispose(ref textureResources[i]);
        base.OnDispose(disposeManagedResources);
    }
}
