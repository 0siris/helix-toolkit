/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public sealed class Sprite2DRenderCore : RenderCore {
            private readonly ConstantBufferComponent globalTransformCB;

            private SamplerStateProxy sampler;

            private int samplerSlot;

            private ShaderPass spritePass;

            private int texSlot;

            private ShaderResourceViewProxy textureView;

            public Sprite2DRenderCore()
                : base(RenderType.ScreenSpaced) {
                globalTransformCB = AddComponent(new ConstantBufferComponent(
                                                     new ConstantBufferDescription(DefaultBufferNames.GlobalTransformCB,
                                                         GlobalTransformStruct.SizeInBytes)));
            }

            public IAttachableBufferModel Buffer { get; set; }

            public Matrix ProjectionMatrix { get; set; } = Matrix.Identity;

            public void UpdateTexture(TextureModel texture, ITextureResourceManager manager) {
                var tex = manager.Register(texture, true);
                RemoveAndDispose(ref textureView);
                textureView = tex;
            }

            public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
                if (Buffer == null || textureView == null || spritePass.IsNULL) return;
                var slot = 0;
                if (!Buffer.AttachBuffers(deviceContext, ref slot, EffectTechnique.EffectsManager)) return;
                var globalTrans = context.GlobalTransform;
                globalTrans.Projection = ProjectionMatrix;
                globalTransformCB.Upload(deviceContext, ref globalTrans);
                spritePass.BindShader(deviceContext);
                spritePass.BindStates(deviceContext, StateType.All);
                spritePass.PixelShader.BindTexture(deviceContext, texSlot, textureView);
                spritePass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
                deviceContext.SetViewport(0, 0, context.ActualWidth, context.ActualHeight);
                deviceContext.SetScissorRectangle(0, 0, (int) context.ActualWidth, (int) context.ActualHeight);
                deviceContext.DrawIndexed(Buffer.IndexBuffer.ElementCount, 0, 0);
                RaiseInvalidateRender();
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                spritePass = technique[DefaultPassNames.Default];
                texSlot = spritePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SpriteTB);
                samplerSlot =
                    spritePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SpriteSampler);
                sampler = EffectTechnique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
                return true;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref textureView);
                RemoveAndDispose(ref sampler);
            }
        }
    }
}
