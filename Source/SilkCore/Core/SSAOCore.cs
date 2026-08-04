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

public sealed class SSAOCore : RenderCore {
    private const int KernalSize = 32;
    private const Format DEPTHFORMAT = Format.FormatD32Float;
    private const Format RENDERTARGETFORMAT = Format.FormatR16G16B16A16Float;
    private const Format SSAOTARGETFORMAT = Format.FormatR16Float;
    private readonly Vector4[] kernels = new Vector4[KernalSize];
    private readonly ConstantBufferComponent ssaoCB;

    private OffScreenTextureSize offScreenTextureSize = OffScreenTextureSize.Half;

    private SSAOQuality quality = SSAOQuality.Low;
    private float radius = 0.5f;
    private SSAOParamStruct ssaoParam;
    private ShaderPass ssaoPass, ssaoBlur;
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

    private ShaderResourceViewProxy ssaoView, ssaoNoiseView;
    private SamplerStateProxy surfaceSampler, noiseSampler, blurSampler;
    private int width, height;

    public SSAOCore() : base(RenderType.PreProc) {
        ssaoCB = AddComponent(new ConstantBufferComponent(
                                  new ConstantBufferDescription(DefaultBufferNames.SSAOCB,
                                                                SSAOParamStruct.SizeInBytes)));
    }

    public float Radius {
        get => radius;
        set => SetAffectsRender(ref radius, value);
    }

    public SSAOQuality Quality {
        get => quality;
        set {
            if (SetAffectsRender(ref quality, value))
                offScreenTextureSize = value == SSAOQuality.High
                                           ? OffScreenTextureSize.Full
                                           : OffScreenTextureSize.Half;
        }
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        EnsureTextureResources((int)context.ActualWidth, (int)context.ActualHeight, deviceContext);
        var texScale = (int)offScreenTextureSize;
        var viewport = context.Viewport;
        using var ds = context.GetOffScreenDS(offScreenTextureSize, DEPTHFORMAT);
        using var rt0 = context.GetOffScreenRT(offScreenTextureSize, RENDERTARGETFORMAT);
        using var rt1 = context.GetOffScreenRT(offScreenTextureSize, SSAOTARGETFORMAT);
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
        IRenderTechnique currTechnique = null;
        var ssaoPass1 = ShaderPass.NullPass;
        var frustum = context.BoundingFrustum;
        for (var i = 0; i < context.RenderHost.PerFrameOpaqueNodesInFrustum.Count; ++i) {
            var node = context.RenderHost.PerFrameOpaqueNodesInFrustum[i];
            if (currTechnique != node.EffectTechnique) {
                currTechnique = node.EffectTechnique;
                ssaoPass1 = currTechnique[DefaultPassNames.MeshSSAOPass];
            }

            if (ssaoPass1.IsNULL) continue;
            node.RenderDepth(context, deviceContext, ssaoPass1);
        }

        var invProjection = context.ProjectionMatrix.Inverted();
        ssaoParam.InvProjection = invProjection;
        ssaoParam.NoiseScale = new Vector2(w / 4f, h / 4f);
        ssaoParam.Radius = radius;
        ssaoParam.TextureScale = texScale;
        ssaoCB.ModelConstBuffer.UploadDataToBuffer(deviceContext,
                                                   dataBox => {
                                                       Debug.Assert(UnsafeHelper.SizeOf(kernels)
                                                                    + UnsafeHelper
                                                                        .SizeOf(ref ssaoParam) <=
                                                                    ssaoCB.ModelConstBuffer.bufferDesc
                                                                          .SizeInBytes);
                                                       var nextPtr =
                                                           UnsafeHelper.Write(
                                                               dataBox.DataPointer,
                                                               kernels,
                                                               0,
                                                               kernels.Length);
                                                       UnsafeHelper.Write(nextPtr, ref ssaoParam);
                                                   });
        deviceContext.SetRenderTarget(rt1);
        ssaoPass.BindShader(deviceContext);
        ssaoPass.BindStates(deviceContext, StateType.All);
        ssaoPass.PixelShader.BindTexture(deviceContext, ssaoTexSlot, rt0);
        ssaoPass.PixelShader.BindTexture(deviceContext, noiseTexSlot, ssaoNoiseView);
        ssaoPass.PixelShader.BindTexture(deviceContext, depthSlot, ds);
        ssaoPass.PixelShader.BindSampler(deviceContext, surfaceSampleSlot, surfaceSampler);
        ssaoPass.PixelShader.BindSampler(deviceContext, noiseSamplerSlot, noiseSampler);
        deviceContext.Draw(4, 0);

        ssaoPass.PixelShader.BindTexture(deviceContext, depthSlot, null);

        deviceContext.SetRenderTarget(ssaoView);
        deviceContext.SetViewport(ref viewport);
        deviceContext.SetScissorRectangle(ref viewport);
        ssaoBlur.BindShader(deviceContext);
        ssaoBlur.BindStates(deviceContext, StateType.All);
        ssaoBlur.PixelShader.BindTexture(deviceContext, ssaoTexSlot, rt1);
        ssaoBlur.PixelShader.BindSampler(deviceContext, surfaceSampleSlot, blurSampler);
        deviceContext.Draw(4, 0);
        context.SharedResource.SSAOMap = ssaoView;

        context.RenderHost.SetDefaultRenderTargets(false);
        deviceContext.SetShaderResource(PixelShader.Type, ssaoTexSlot, ssaoView);
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
        if (ssaoPass.IsNULL || ssaoBlur.IsNULL) return false;
        ssaoTexSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SSAOMapTB);
        noiseTexSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SSAONoiseTB);
        depthSlot =
            ssaoPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SSAODepthTB);
        surfaceSampleSlot =
            ssaoPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
        noiseSamplerSlot =
            ssaoPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.NoiseSampler);
        surfaceSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.SSAOSamplerClamp);
        noiseSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.SSAONoise);
        blurSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerClampAni1);
        InitialParameters();
        return true;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref surfaceSampler);
        RemoveAndDispose(ref noiseSampler);
        RemoveAndDispose(ref blurSampler);
        RemoveAndDispose(ref ssaoView);
        RemoveAndDispose(ref ssaoNoiseView);
    }

    private void InitialParameters() {
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

        ssaoNoiseView = ShaderResourceViewProxy
            .CreateView(Device, noise, 4, 4, Format.FormatR32G32B32Float, true, false);
    }
}