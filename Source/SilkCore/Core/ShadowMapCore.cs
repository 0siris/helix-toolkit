/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define TEST

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        /// <summary>
        /// </summary>
        public class ShadowMapCore : RenderCore, IShadowMapRenderParams {
            /// <summary>
            /// </summary>
            public ShadowMapCore() : base(RenderType.PreProc) {
                modelCB = AddComponent(new ConstantBufferComponent(
                                           new ConstantBufferDescription(
                                               DefaultBufferNames.ShadowParamCB,
                                               ShadowMapParamStruct.SizeInBytes)));
                Bias = 0.0015f;
                Intensity = 0.5f;
                Width = Height = 1024;
            }

            public event EventHandler<UpdateLightSourceEventArgs> OnUpdateLightSource;

            public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
                if (!NeedRender) {
                    modelStruct.HasShadowMap = 0;
                    modelCB.Upload(deviceContext, ref modelStruct);
                    return;
                }

                OnUpdateLightSource?.Invoke(this, new UpdateLightSourceEventArgs(context));
                ++currentFrame;
                currentFrame %= Math.Max(1, UpdateFrequency);
                if (!FoundLightSource || currentFrame != 0) return;
                if (resolutionChanged) {
                    RemoveAndDispose(ref viewResource);
                    viewResource = new ShaderResourceViewProxy(Device, ShadowMapTextureDesc);
                    viewResource.CreateView(DepthStencilViewDesc);
                    viewResource.CreateView(ShaderResourceViewDesc);
                    resolutionChanged = false;
                }

                deviceContext.ClearDepthStencilView(viewResource, DepthStencilClearFlags.Depth);
                var orgFrustum = context.BoundingFrustum;
                var frustum = new BoundingFrustum(LightView * LightProjection);
                context.BoundingFrustum = frustum;
#if !TEST
                deviceContext.SetViewport(0, 0, Width, Height);

                deviceContext.SetDepthStencil(viewResource.DepthStencilView);
                modelStruct.HasShadowMap = context.RenderHost.IsShadowMapEnabled ? 1 : 0;
                modelCB.Upload(deviceContext, ref modelStruct);
                for (var i = 0; i < context.RenderHost.PerFrameOpaqueNodes.Count; ++i) {
                    //Only support opaque object for throwing shadows.
                    var core = context.RenderHost.PerFrameOpaqueNodes[i];
                    if (core.RenderCore.IsThrowingShadow && core.TestViewFrustum(ref frustum))
                        core.RenderShadow(context, deviceContext);
                }

                context.BoundingFrustum = orgFrustum;
                context.RenderHost.SetDefaultRenderTargets(false);
                context.SharedResource.ShadowView = viewResource;
#endif
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                return true;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref viewResource);
                resolutionChanged = true;
            }

            public sealed class UpdateLightSourceEventArgs : EventArgs {
                public UpdateLightSourceEventArgs(RenderContext context) {
                    Context = context;
                }

                public RenderContext Context { get; private set; }
            }

            #region Variables

            private ShaderResourceViewProxy viewResource;
            private int currentFrame;
            private bool resolutionChanged = true;
            private ShadowMapParamStruct modelStruct;

            /// <summary>
            /// </summary>
            protected virtual Texture2DDescription ShadowMapTextureDesc =>
                new() {
                    Format = Format.FormatR32Typeless, //!!!! because of depth and shader resource
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = Width,
                    Height = Height,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource, //!!!!
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

            /// <summary>
            /// </summary>
            protected virtual DepthStencilViewDescription DepthStencilViewDesc =>
                new() {
                    Format = Format.FormatD32Float,
                    Dimension = DepthStencilViewDimension.Texture2D,
                    Texture2D = new DepthStencilViewDescription.Texture2DResource {
                        MipSlice = 0
                    }
                };

            /// <summary>
            /// </summary>
            protected virtual ShaderResourceViewDescription ShaderResourceViewDesc =>
                new() {
                    Format = Format.FormatR32Float,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource {
                        MipLevels = 1,
                        MostDetailedMip = 0
                    }
                };

            private readonly ConstantBufferComponent modelCB;

            #endregion

            #region Properties

            /// <summary>
            /// </summary>
            public int Width {
                get => (int)modelStruct.ShadowMapSize.X;
                set {
                    if (SetAffectsRender(ref modelStruct.ShadowMapSize.X, value)) resolutionChanged = true;
                }
            }

            /// <summary>
            /// </summary>
            public int Height {
                get => (int)modelStruct.ShadowMapSize.Y;
                set {
                    if (SetAffectsRender(ref modelStruct.ShadowMapSize.Y, value)) resolutionChanged = true;
                }
            }

            /// <summary>
            /// </summary>
            public float Intensity {
                get => modelStruct.ShadowMapInfo.X;
                set => SetAffectsRender(ref modelStruct.ShadowMapInfo.X, value);
            }

            /// <summary>
            /// </summary>
            public float Bias {
                get => modelStruct.ShadowMapInfo.Z;
                set => SetAffectsRender(ref modelStruct.ShadowMapInfo.Z, value);
            }

            /// <summary>
            /// </summary>
            public Matrix LightView {
                get => modelStruct.LightView;
                set => SetAffectsRender(ref modelStruct.LightView, value);
            }

            /// <summary>
            /// </summary>
            public Matrix LightProjection {
                get => modelStruct.LightProjection;
                set => SetAffectsRender(ref modelStruct.LightProjection, value);
            }

            /// <summary>
            ///     Set to true if found the light source, otherwise false.
            /// </summary>
            public bool FoundLightSource { get; set; } = false;

            /// <summary>
            ///     Update shadow map every N frames
            /// </summary>
            public int UpdateFrequency { get; set; } = 1;

            public bool NeedRender { get; set; } = true;

            #endregion
        }
    }
}
