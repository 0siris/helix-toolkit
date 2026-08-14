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
/// </summary>
public class DiffuseMaterialVariables : MaterialVariable {
    private const int Numtextures = 1;
    private const int Numsamplers = 1;
    private const int DiffuseIdx = 0;

    private readonly DiffuseMaterialCore material;
    private readonly IStatePoolManager statePoolManager;

    private readonly ITextureResourceManager textureManager;
    private int samplerDiffuseSlot, samplerShadowSlot;
    private SamplerStateProxy samplerResource;

    private int texDiffuseSlot;
    private uint textureIndex;
    private ShaderResourceViewProxy textureResource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiffuseMaterialVariables" /> class.
    /// </summary>
    /// <param name="manager">The manager.</param>
    /// <param name="technique">The technique.</param>
    /// <param name="materialCore">The material core.</param>
    /// <param name="defaultPassName"></param>
    private DiffuseMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique,
        DiffuseMaterialCore materialCore,
        string defaultPassName = DefaultPassNames.Default
    )
        : base(manager, technique, DefaultMeshConstantBufferDesc, materialCore) {
        material = materialCore;
        texDiffuseSlot = -1;
        samplerDiffuseSlot = samplerShadowSlot = -1;
        textureManager = manager.MaterialTextureManager;
        statePoolManager = manager.StateManager;
        MaterialPass = technique[defaultPassName];
        OitPass = technique[DefaultPassNames.DiffuseOit];
        OitDepthPeelingInit = technique[DefaultPassNames.OitDepthPeelingInit];
        OitDepthPeeling = technique[DefaultPassNames.DiffuseOitdp];
        ShadowPass = technique[DefaultPassNames.ShadowPass];
        WireframePass = technique[DefaultPassNames.Wireframe];
        WireframeOitPass = technique[DefaultPassNames.WireframeOitPass];
        WireframeOitdpPass = technique[DefaultPassNames.WireframeOitdpPass];
        DepthPass = technique[DefaultPassNames.DepthPrepass];
        UpdateMappings(MaterialPass);
        CreateTextureViews();
        CreateSamplers();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiffuseMaterialVariables" /> class. This construct will be using the
    ///     PassName pass into constructor only.
    /// </summary>
    /// <param name="passName">Name of the pass.</param>
    /// <param name="manager">The manager.</param>
    /// <param name="technique"></param>
    /// <param name="material">The material.</param>
    public DiffuseMaterialVariables(
        string passName,
        IEffectsManager manager,
        IRenderTechnique technique,
        DiffuseMaterialCore material
    )
        : this(manager, technique, material) {
        MaterialPass = technique[passName];
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

    public ShaderPass DepthPass { get; }

    /// <summary>
    /// </summary>
    public string ShaderDiffuseTexName { get; } = DefaultBufferNames.DiffuseMapTb;

    /// <summary>
    /// </summary>
    public string SamplerDiffuseTexName { get; } = DefaultSamplerStateNames.SurfaceSampler;

    /// <summary>
    /// </summary>
    public string SamplerShadowMapName { get; } = DefaultSamplerStateNames.ShadowMapSampler;

    protected override void OnInitialPropertyBindings() {
        base.OnInitialPropertyBindings();
        AddPropertyBinding(nameof(DiffuseMaterialCore.DiffuseColor),
                           () => { WriteValue(PhongPbrMaterialStruct.DiffuseStr, material.DiffuseColor); });
        AddPropertyBinding(nameof(DiffuseMaterialCore.UvTransform),
                           () => {
                               Matrix m = material.UvTransform;
                               WriteValue(PhongPbrMaterialStruct.UvTransformR1Str, m.Column1);
                               WriteValue(PhongPbrMaterialStruct.UvTransformR2Str, m.Column2);
                           });
        AddPropertyBinding(nameof(DiffuseMaterialCore.DiffuseMap),
                           () => {
                               CreateTextureView(material.DiffuseMap, DiffuseIdx);
                               WriteValue(PhongPbrMaterialStruct.HasDiffuseMapStr,
                                          material.RenderDiffuseMap && textureResource != null ? 1 : 0);
                           });
        AddPropertyBinding(nameof(DiffuseMaterialCore.DiffuseMapSampler),
                           () => {
                               var newSampler = statePoolManager.Register(material.DiffuseMapSampler);
                               RemoveAndDispose(ref samplerResource);
                               samplerResource = newSampler;
                           });
        AddPropertyBinding(nameof(DiffuseMaterialCore.EnableUnLit),
                           () => { WriteValue(PhongPbrMaterialStruct.HasNormalMapStr, material.EnableUnLit); });
        AddPropertyBinding(nameof(DiffuseMaterialCore.EnableFlatShading),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.RenderFlat, material.EnableFlatShading);
                           });
        AddPropertyBinding(nameof(DiffuseMaterialCore.VertexColorBlendingFactor),
                           () => {
                               WriteValue(PhongPbrMaterialStruct.VertColorBlending,
                                          material.VertexColorBlendingFactor);
                           });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateTextureView(TextureModel texture, int index) {
        var newTexture = texture == null ? null : textureManager.Register(texture);
        RemoveAndDispose(ref textureResource);
        textureResource = newTexture;
        if (textureResource != null)
            textureIndex |= 1u << index;
        else
            textureIndex &= ~(1u << index);
    }

    private void CreateTextureViews() {
        if (material != null) {
            CreateTextureView(material.DiffuseMap, DiffuseIdx);
        } else {
            RemoveAndDispose(ref textureResource);
            textureIndex = 0;
        }
    }

    private void CreateSamplers() {
        var newSampler = material == null ? null : statePoolManager.Register(material.DiffuseMapSampler);
        RemoveAndDispose(ref samplerResource);
        samplerResource = newSampler;
    }

    public override bool BindMaterialResources(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass shaderPass
    ) {
        if (HasTextures) OnBindMaterialTextures(deviceContext, shaderPass.PixelShader);
        return true;
    }

    /// <summary>
    ///     Actual bindings
    /// </summary>
    /// <param name="context"></param>
    /// <param name="shader"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnBindMaterialTextures(DeviceContextProxy context, PixelShader shader) {
        if (shader.IsNull) return;
        var idx = shader.ShaderStageIndex;
        shader.BindTexture(context, texDiffuseSlot, textureResource);
        shader.BindSampler(context, samplerDiffuseSlot, samplerResource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMappings(ShaderPass shaderPass) {
        texDiffuseSlot = shaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderDiffuseTexName);
        samplerDiffuseSlot = shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(SamplerDiffuseTexName);
        samplerShadowSlot = shaderPass.PixelShader.SamplerMapping.TryGetBindSlot(SamplerShadowMapName);
    }


    /// <summary>
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    protected override void OnDispose(bool disposeManagedResources) {
        if (disposeManagedResources) {
            RemoveAndDispose(ref samplerResource);
            RemoveAndDispose(ref textureResource);
        }

        base.OnDispose(disposeManagedResources);
    }

    public override ShaderPass GetPass(RenderType renderType, RenderContext context) {
        if (renderType == RenderType.Transparent)
            switch (context.OitRenderStage) {
                case OitRenderStage.SinglePassWeighted:
                    return OitPass;
                case OitRenderStage.DepthPeelingInitMinMaxZ:
                    return OitDepthPeelingInit;
                case OitRenderStage.DepthPeeling:
                    return OitDepthPeeling;
            }

        return MaterialPass;
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

    public override void Draw(
        DeviceContextProxy deviceContext,
        IAttachableBufferModel bufferModel,
        int instanceCount
    ) {
        DrawIndexed(deviceContext, bufferModel.IndexBuffer.ElementCount, instanceCount);
    }
}
