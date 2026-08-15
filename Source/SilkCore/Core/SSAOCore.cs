/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class SsaoCore : RenderCore {
    private const int KernalSize = 32;
    private const Format Depthformat = Format.FormatD32Float;
    private const Format Rendertargetformat = Format.FormatR16G16B16A16Float;
    private const Format Ssaotargetformat = Format.FormatR16Float;
    private readonly Vector4[] kernels = new Vector4[KernalSize];
    private readonly ConstantBufferComponent ssaoCb;

    private OffScreenTextureSize offScreenTextureSize = OffScreenTextureSize.Half;

    private float radius = 0.5f;
    private SsaoParamStruct ssaoParam;
    private ShaderPass? ssaoPass, ssaoBlur;
    private int ssaoTexSlot, noiseTexSlot, surfaceSampleSlot, noiseSamplerSlot, depthSlot;

    private Texture2DDescription ssaoTextureDesc = new() {
        CpuAccessFlags = CpuAccessFlags.None,
        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
        Format = Format.FormatR16Float,
        SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
        OptionFlags = ResourceOptionFlags.None,
        Usage = ResourceUsage.Default,
        ArraySize = 1,
        MipLevels = 1
    };

    private ShaderResourceViewProxy? ssaoView, ssaoNoiseView;
    private SamplerStateProxy? surfaceSampler, noiseSampler, blurSampler;
    private int width, height;

    public SsaoCore() : base(RenderType.PreProc) {
        ssaoCb = AddComponent(new ConstantBufferComponent(
                                  new ConstantBufferDescription(DefaultBufferNames.Ssaocb,
                                                                SsaoParamStruct.SizeInBytes)));
    }

    public float Radius {
        get => radius;
        set => SetAffectsRender(ref radius, value);
    }

    public SsaoQuality Quality {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                offScreenTextureSize = value == SsaoQuality.High
                                           ? OffScreenTextureSize.Full
                                           : OffScreenTextureSize.Half;
        }
    } = SsaoQuality.Low;

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        EnsureTextureResources((int)context.ActualWidth, (int)context.ActualHeight, deviceContext);
        if (ssaoPass is not { } pass
            || ssaoBlur is not { } blur
            || ssaoView is not { } view
            || ssaoNoiseView is not { } noiseView
            || surfaceSampler is not { } surfaceSamplerValue
            || noiseSampler is not { } noiseSamplerValue
            || blurSampler is not { } blurSamplerValue
            || !ssaoCb.IsAttached
            || ssaoCb.ModelConstBuffer is not { } modelConstBuffer)
            return;

        var texScale = (int)offScreenTextureSize;
        var viewport = context.Viewport;
        using var ds = context.GetOffScreenDs(offScreenTextureSize, Depthformat);
        using var rt0 = context.GetOffScreenRt(offScreenTextureSize, Rendertargetformat);
        using var rt1 = context.GetOffScreenRt(offScreenTextureSize, Ssaotargetformat);
        var w = (int)(context.ActualWidth /
                      texScale); // Make sure to set correct viewport width/height by quality
        var h = (int)(context.ActualHeight / texScale);
        deviceContext.SetRenderTarget(ds,
                                      rt0,
                                      true,
                                      new Color4(0, 0, 0, 1),
                                      true,
                                      DepthStencilClearFlags.Depth);
        deviceContext.SetViewport(0, 0, w, h);
        deviceContext.SetScissorRectangle(0, 0, w, h);
        IRenderTechnique? currTechnique = null;
        var ssaoPass1 = ShaderPass.NullPass;
        var frustum = context.BoundingFrustum;
        for (var i = 0; i < context.RenderHost.PerFrameOpaqueNodesInFrustum.Count; ++i) {
            var node = context.RenderHost.PerFrameOpaqueNodesInFrustum[i];
            if (node.EffectTechnique is not { } technique) continue;
            if (currTechnique != technique) {
                currTechnique = technique;
                ssaoPass1 = technique[DefaultPassNames.MeshSsaoPass];
            }

            if (ssaoPass1.IsNull) continue;
            node.RenderDepth(context, deviceContext, ssaoPass1);
        }

        var invProjection = context.ProjectionMatrix.Inverted();
        ssaoParam.InvProjection = invProjection;
        ssaoParam.NoiseScale = new Vector2(w / 4f, h / 4f);
        ssaoParam.Radius = radius;
        ssaoParam.TextureScale = texScale;
         modelConstBuffer.UploadDataToBuffer(deviceContext,
                                                   dataBox => {
                                                       Debug.Assert(UnsafeHelper.SizeOf(kernels)
                                                                    + UnsafeHelper
                                                                        .SizeOf(ref ssaoParam) <=
                                                                     modelConstBuffer.BufferDesc.SizeInBytes);
                                                       var nextPtr =
                                                           UnsafeHelper.Write(
                                                               dataBox.DataPointer,
                                                               kernels,
                                                               0,
                                                               kernels.Length);
                                                       UnsafeHelper.Write(nextPtr, ref ssaoParam);
                                                   });
        deviceContext.SetRenderTarget(rt1);
         pass.BindShader(deviceContext);
         pass.BindStates(deviceContext, StateType.All);
         pass.PixelShader.BindTexture(deviceContext, ssaoTexSlot, rt0);
         pass.PixelShader.BindTexture(deviceContext, noiseTexSlot, noiseView);
         pass.PixelShader.BindTexture(deviceContext, depthSlot, ds);
         pass.PixelShader.BindSampler(deviceContext, surfaceSampleSlot, surfaceSamplerValue);
         pass.PixelShader.BindSampler(deviceContext, noiseSamplerSlot, noiseSamplerValue);
        deviceContext.Draw(4, 0);

         pass.PixelShader.BindTexture(deviceContext, depthSlot, null);

         deviceContext.SetRenderTarget(view);
        deviceContext.SetViewport(ref viewport);
        deviceContext.SetScissorRectangle(ref viewport);
         blur.BindShader(deviceContext);
         blur.BindStates(deviceContext, StateType.All);
         blur.PixelShader.BindTexture(deviceContext, ssaoTexSlot, rt1);
         blur.PixelShader.BindSampler(deviceContext, surfaceSampleSlot, blurSamplerValue);
        deviceContext.Draw(4, 0);
         context.SharedResource.SsaoMap = view;

        context.RenderHost.SetDefaultRenderTargets(false);
         deviceContext.SetShaderResource(PixelShader.Type, ssaoTexSlot, view);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTextureResources(int w, int h, DeviceContextProxy deviceContext) {
        if (w != width || h != height) {
            RemoveAndDispose(ref ssaoView);
            width = w;
            height = h;
            if (width > 10 && height > 10) {
                ssaoTextureDesc.Width = width;
                ssaoTextureDesc.Height = height;
                ssaoView = new ShaderResourceViewProxy(deviceContext, ssaoTextureDesc);
                ssaoView.CreateTextureView();
                ssaoView.CreateRenderTargetView();
            }
        }
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        if (technique.IsNull) return false;
        width = height = 0;
        ssaoPass = technique[DefaultPassNames.Default];
        ssaoBlur = technique[DefaultPassNames.EffectBlurHorizontal];
        if (ssaoPass.IsNull || ssaoBlur.IsNull) return false;
        ssaoTexSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SsaoMapTb);
        noiseTexSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SsaoNoiseTb);
        depthSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SsaoDepthTb);
        surfaceSampleSlot =
            ssaoPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
        noiseSamplerSlot =
            ssaoPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.NoiseSampler);
        surfaceSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.SsaoSamplerClamp);
        noiseSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.SsaoNoise);
        blurSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
        return InitialParameters();
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref surfaceSampler);
        RemoveAndDispose(ref noiseSampler);
        RemoveAndDispose(ref blurSampler);
        RemoveAndDispose(ref ssaoView);
        RemoveAndDispose(ref ssaoNoiseView);
    }

    private bool InitialParameters() {
        ssaoParam.Radius = radius;
        var rnd = new Random((int)Stopwatch.GetTimestamp());
        var thres = Math.Cos(Math.PI / 2 - Math.PI / 12);
        for (var i = 0; i < 32; ++i)
            while (true) {
                var x = rnd.NextFloat(-1, 1);
                var y = rnd.NextFloat(-1, 1);
                var z = rnd.NextFloat(1e-3f, 1);
                var v = SilkMath.Normalize(new Vector3(x, y, z));
                var angle = SilkMath.Dot(v, Vector3.UnitZ);
                if (SilkMath.Dot(v, Vector3.UnitZ) < thres) continue;
                var scale = i / 32f;
                scale = 0.1f + 0.9f * scale;
                v *= scale;
                kernels[i] = new Vector4(v.X, v.Y, v.Z, 0);
                break;
            }

        var noise = new Vector3[4 * 4];
        for (var i = 0; i < 16; ++i) {
            var x = rnd.NextFloat(-1, 1);
            var y = rnd.NextFloat(-1, 1);
            noise[i] = SilkMath.Normalize(new Vector3(x, y, 0));
        }

        if (Device is not { } device) return false;
        ssaoNoiseView = ShaderResourceViewProxy
            .CreateView(device, noise, 4, 4, Format.FormatR32G32B32Float, true, false);
        return ssaoNoiseView is not null;
    }
}
