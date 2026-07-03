/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        /// <summary>
        /// </summary>
        public class SkyDomeRenderCore : GeometryRenderCore, ISkyboxRenderParams {
            /// <summary>
            ///     Initializes a new instance of the <see cref="SkyBoxRenderCore" /> class.
            /// </summary>
            public SkyDomeRenderCore() {
                RasterDescription = DefaultRasterDescriptions.RSSkyDome;
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
                    skyBuffer = new SkyDomeBufferModel();
                    skyBuffer.Geometry = SphereMesh;
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
                if (CubeTexture != null) {
                    cubeTextureRes = new ShaderResourceViewProxy(Device);
                    cubeTextureRes.CreateView(cubeTexture);
                    if (cubeTextureRes.TextureView != null && cubeTextureRes.TextureView.Description.Dimension ==
                        ShaderResourceViewDimension.TextureCube)
                        MipMapLevels = cubeTextureRes.TextureView.Description.TextureCube.MipLevels;
                }
            }

            protected override void OnDetach() {
                MipMapLevels = 0;
                RemoveAndDispose(ref textureSampler);
                RemoveAndDispose(ref cubeTextureRes);
                GeometryBuffer = null;
                RemoveAndDispose(ref skyBuffer);
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
                context.SharedResource.EnvironementMap = cubeTextureRes;
                context.SharedResource.EnvironmentMapMipLevels = MipMapLevels;
                if (SkipRendering) return;
                DefaultShaderPass.BindShader(deviceContext);
                DefaultShaderPass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
                DefaultShaderPass.PixelShader.BindTexture(deviceContext, cubeTextureSlot, cubeTextureRes);
                DefaultShaderPass.PixelShader.BindSampler(deviceContext, textureSamplerSlot, textureSampler);
                deviceContext.DrawIndexed(GeometryBuffer.IndexBuffer.ElementCount, 0, 0);
            }

            protected sealed override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) { }

            protected sealed override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) { }

            protected sealed override void OnRenderDepth(
                RenderContext context,
                DeviceContextProxy deviceContext,
                ShaderPass customPass
            ) { }

            /// <summary>
            /// </summary>
            private sealed class SkyDomeBufferModel : MeshGeometryBufferModel<Vector3> {
                public SkyDomeBufferModel() : base(SilkMath.Vector3SizeInBytes) {
                    Topology = PrimitiveTopology.TriangleList;
                }

                protected override void OnCreateVertexBuffer(
                    DeviceContextProxy context,
                    IElementsBufferProxy buffer,
                    int bufferIndex,
                    Geometry3D geometry,
                    IDeviceResources deviceResources
                ) {
                    if (bufferIndex == 0 && geometry != null && geometry.Positions != null &&
                        geometry.Positions.Count > 0)
                        buffer.UploadDataToBuffer(context, geometry.Positions, geometry.Positions.Count);
                }
            }

        #region Default Mesh

            private static readonly MeshGeometry3D SphereMesh;

            static SkyDomeRenderCore() {
                var builder = new MeshBuilder(false, false);
                builder.AddSphere(Vector3.Zero);
                SphereMesh = builder.ToMesh();
            }

        #endregion

        #region Variables

            private ShaderResourceViewProxy cubeTextureRes;
            private int cubeTextureSlot;
            private SamplerStateProxy textureSampler;
            private int textureSamplerSlot;
            private ShaderPass DefaultShaderPass;
            private SkyDomeBufferModel skyBuffer;

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
    }
}
