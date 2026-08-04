/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class SkyBoxRenderCore : GeometryRenderCore, ISkyboxRenderParams {
#region Default Mesh

    private static readonly Vector3Collection BoxPositions = [
        new Vector3(-10.0f, 10.0f, -10.0f),
        new Vector3(-10.0f, -10.0f, -10.0f),
        new Vector3(10.0f, -10.0f, -10.0f),
        new Vector3(10.0f, -10.0f, -10.0f),
        new Vector3(10.0f, 10.0f, -10.0f),
        new Vector3(-10.0f, 10.0f, -10.0f),

        new Vector3(-10.0f, -10.0f, 10.0f),
        new Vector3(-10.0f, -10.0f, -10.0f),
        new Vector3(-10.0f, 10.0f, -10.0f),
        new Vector3(-10.0f, 10.0f, -10.0f),
        new Vector3(-10.0f, 10.0f, 10.0f),
        new Vector3(-10.0f, -10.0f, 10.0f),

        new Vector3(10.0f, -10.0f, -10.0f),
        new Vector3(10.0f, -10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, -10.0f),
        new Vector3(10.0f, -10.0f, -10.0f),

        new Vector3(-10.0f, -10.0f, 10.0f),
        new Vector3(-10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, -10.0f, 10.0f),
        new Vector3(-10.0f, -10.0f, 10.0f),

        new Vector3(-10.0f, 10.0f, -10.0f),
        new Vector3(10.0f, 10.0f, -10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(10.0f, 10.0f, 10.0f),
        new Vector3(-10.0f, 10.0f, 10.0f),
        new Vector3(-10.0f, 10.0f, -10.0f),

        new Vector3(-10.0f, -10.0f, -10.0f),
        new Vector3(-10.0f, -10.0f, 10.0f),
        new Vector3(10.0f, -10.0f, -10.0f),
        new Vector3(10.0f, -10.0f, -10.0f),
        new Vector3(-10.0f, -10.0f, 10.0f),
        new Vector3(10.0f, -10.0f, 10.0f)
    ];

#endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkyBoxRenderCore" /> class.
    /// </summary>
    public SkyBoxRenderCore() {
        RasterDescription = DefaultRasterDescriptions.RSSkybox;
    }

    /// <summary>
    ///     Called when [attach].
    /// </summary>
    /// <param name="technique">The technique.</param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderTechnique technique) {
        if (base.OnAttach(technique)) {
            DefaultShaderPass = technique[DefaultPassNames.Default];
            OnDefaultPassChanged(DefaultShaderPass);
            skyBuffer = new SkyBoxBufferModel {
                Geometry = new PointGeometry3D { Positions = BoxPositions },
                Topology = PrimitiveTopology.TriangleList
            };
            GeometryBuffer = skyBuffer;
            UpdateTexture();
            textureSampler = technique.EffectsManager.StateManager.Register(SamplerDescription);
            return true;
        }

        return false;
    }

    private void UpdateTexture() {
        MipMapLevels = 0;
        RemoveAndDispose(ref cubeTextureRes);

        if (cubeTexture != null) {
            cubeTextureRes = new ShaderResourceViewProxy(Device);
            cubeTextureRes.CreateView(cubeTexture);
            if (cubeTextureRes.TextureView != null && cubeTextureRes.TextureView.Description.Dimension ==
                ShaderResourceViewDimension.TextureCube)
                MipMapLevels = cubeTextureRes.TextureView.Description.TextureCube.MipLevels;
        }
    }

    protected override void OnDetach() {
        MipMapLevels = 0;
        GeometryBuffer = null;
        RemoveAndDispose(ref skyBuffer);
        RemoveAndDispose(ref textureSampler);
        RemoveAndDispose(ref cubeTextureRes);
        base.OnDetach();
    }

    /// <summary>
    ///     Called when [default pass changed].
    /// </summary>
    /// <param name="pass">The pass.</param>
    protected void OnDefaultPassChanged(ShaderPass pass) {
        cubeTextureSlot = pass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderCubeTextureName);
        textureSamplerSlot = pass.PixelShader.SamplerMapping.TryGetBindSlot(ShaderCubeTextureSamplerName);
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) {
        if (context.Camera.CreateLeftHandSystem && RasterDescription.IsFrontCounterClockwise) {
            var desc = RasterDescription;
            desc.IsFrontCounterClockwise = false;
            RasterDescription = desc;
            RaiseInvalidateRender();
            return;
        }

        context.SharedResource.EnvironementMap = cubeTextureRes;
        context.SharedResource.EnvironmentMapMipLevels = MipMapLevels;
        if (SkipRendering) return;
        DefaultShaderPass.BindShader(deviceContext);
        DefaultShaderPass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
        DefaultShaderPass.PixelShader.BindTexture(deviceContext, cubeTextureSlot, cubeTextureRes);
        DefaultShaderPass.PixelShader.BindSampler(deviceContext, textureSamplerSlot, textureSampler);
        deviceContext.Draw(GeometryBuffer.VertexBuffer[0].ElementCount, 0);
    }

    /// <summary>
    ///     Called when [render shadow].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    protected sealed override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) { }

    protected sealed override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) { }

    protected sealed override void OnRenderDepth(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass customPass
    ) { }

    /// <summary>
    /// </summary>
    private sealed class SkyBoxBufferModel : PointGeometryBufferModel<Vector3> {
        public SkyBoxBufferModel() : base(SilkMath.Vector3SizeInBytes) {
            Topology = PrimitiveTopology.TriangleList;
        }

        protected override void OnCreateVertexBuffer(
            DeviceContextProxy context,
            IElementsBufferProxy buffer,
            int bufferIndex,
            Geometry3D? geometry,
            IDeviceResources deviceResources
        ) {
            // -- set geometry if given
            if (geometry is {Positions.Count: > 0})
                buffer.UploadDataToBuffer(context, geometry.Positions, geometry.Positions.Count);
            else
                buffer.UploadDataToBuffer(context, Array.Empty<Vector3>(), 0);
        }
    }

#region Variables

    private ShaderResourceViewProxy cubeTextureRes;
    private int cubeTextureSlot;
    private SamplerStateProxy textureSampler;
    private int textureSamplerSlot;
    private ShaderPass DefaultShaderPass;
    private SkyBoxBufferModel skyBuffer;

#endregion

#region Properties

    private TextureModel cubeTexture;

    /// <summary>
    ///     Gets or sets the cube texture.
    /// </summary>
    /// <value>
    ///     The cube texture.
    /// </value>
    public TextureModel CubeTexture {
        get => cubeTexture;
        set {
            if (SetAffectsRender(ref cubeTexture, value) && IsAttached) UpdateTexture();
        }
    }

    /// <summary>
    ///     Gets the mip map levels for current cube texture.
    /// </summary>
    /// <value>
    ///     The mip map levels.
    /// </value>
    public int MipMapLevels { get; private set; }

    private SamplerStateDescription samplerDescription = DefaultSamplers.EnvironmentSampler;

    /// <summary>
    ///     Gets or sets the sampler description.
    /// </summary>
    /// <value>
    ///     The sampler description.
    /// </value>
    public SamplerStateDescription SamplerDescription {
        get => samplerDescription;
        set {
            if (SetAffectsRender(ref samplerDescription, value) && IsAttached) {
                var newSampler = EffectTechnique.EffectsManager.StateManager.Register(value);
                RemoveAndDispose(ref textureSampler);
                textureSampler = newSampler;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the name of the shader cube texture.
    /// </summary>
    /// <value>
    ///     The name of the shader cube texture.
    /// </value>
    public string ShaderCubeTextureName { get; set; } = DefaultBufferNames.CubeMapTB;

    /// <summary>
    ///     Gets or sets the name of the shader cube texture sampler.
    /// </summary>
    /// <value>
    ///     The name of the shader cube texture sampler.
    /// </value>
    public string ShaderCubeTextureSamplerName { get; set; } = DefaultSamplerStateNames.CubeMapSampler;

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    public bool SkipRendering { get; set; }

#endregion
}