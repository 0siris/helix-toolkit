/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class Sprite2DRenderCore : RenderCore {
    private readonly ConstantBufferComponent globalTransformCb;

    private SamplerStateProxy? sampler;

    private int samplerSlot;

    private ShaderPass spritePass = ShaderPass.NullPass;

    private int texSlot;

    private ShaderResourceViewProxy? textureView;

    public Sprite2DRenderCore()
        : base(RenderType.ScreenSpaced) {
        globalTransformCb = AddComponent(new ConstantBufferComponent(
                                             new ConstantBufferDescription(DefaultBufferNames.GlobalTransformCb,
                                                                           GlobalTransformStruct.SizeInBytes)));
    }

    public IAttachableBufferModel? Buffer { get; set; }

    public Matrix ProjectionMatrix { get; set; } = Matrix.Identity;

    public void UpdateTexture(TextureModel texture, ITextureResourceManager manager) {
        var tex = manager.Register(texture, true);
        RemoveAndDispose(ref textureView);
        textureView = tex;
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (Buffer is not { } buffer
            || textureView is null
            || sampler is not { } state
            || spritePass.IsNull
            || EffectTechnique is not { } technique
            || buffer.IndexBuffer is not { } indexBuffer)
            return;
        var slot = 0;
        if (!buffer.AttachBuffers(deviceContext, ref slot, technique.EffectsManager)) return;
        var globalTrans = context.GlobalTransform;
        globalTrans.Projection = ProjectionMatrix;
        globalTransformCb.Upload(deviceContext, ref globalTrans);
        spritePass.BindShader(deviceContext);
        spritePass.BindStates(deviceContext, StateType.All);
        spritePass.PixelShader.BindTexture(deviceContext, texSlot, textureView);
        spritePass.PixelShader.BindSampler(deviceContext, samplerSlot, state);
        deviceContext.SetViewport(0, 0, context.ActualWidth, context.ActualHeight);
        deviceContext.SetScissorRectangle(0, 0, (int)context.ActualWidth, (int)context.ActualHeight);
        deviceContext.DrawIndexed(indexBuffer.ElementCount, 0, 0);
        RaiseInvalidateRender();
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        spritePass = technique[DefaultPassNames.Default];
        texSlot = spritePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SpriteTb);
        samplerSlot =
            spritePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SpriteSampler);
        sampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
        return true;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref textureView);
        RemoveAndDispose(ref sampler);
    }
}
