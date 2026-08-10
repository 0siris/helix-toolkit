/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#define MSAASEPARATE

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class OrderIndependentTransparentRenderCore : RenderCore {
    /// <summary>
    ///     Initializes a new instance of the <see cref="OrderIndependentTransparentRenderCore" /> class.
    /// </summary>
    public OrderIndependentTransparentRenderCore() : base(RenderType.Transparent) { }

    private bool CreateTextureResources(RenderContext context, DeviceContextProxy deviceContext) {
        var currSampleDesc = context.RenderHost.RenderBuffer.ColorBufferSampleDesc;
#if MSAASEPARATE
        hasMsaa = currSampleDesc.Count > 1 || currSampleDesc.Quality > 0;
#endif
        if (width != (int)context.ActualWidth || height != (int)context.ActualHeight
                                              || sampleDesc.Count != currSampleDesc.Count ||
                                              sampleDesc.Quality != currSampleDesc.Quality) {
            RemoveAndDispose(ref colorTarget);
            RemoveAndDispose(ref alphaTarget);
            RemoveAndDispose(ref colorTargetNoMsaa);
            RemoveAndDispose(ref alphaTargetNoMsaa);
            sampleDesc = currSampleDesc;

            width = (int)context.ActualWidth;
            height = (int)context.ActualHeight;
            colorDesc.Width = alphaDesc.Width = width;
            colorDesc.Height = alphaDesc.Height = height;
            colorDesc.SampleDescription = alphaDesc.SampleDescription = sampleDesc;
#if MSAASEPARATE
            if (hasMsaa)
                colorDesc.BindFlags = alphaDesc.BindFlags = BindFlags.RenderTarget;
            else
#endif
                colorDesc.BindFlags = alphaDesc.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;

            colorTarget = new ShaderResourceViewProxy(Device, colorDesc);
            alphaTarget = new ShaderResourceViewProxy(Device, alphaDesc);


            colorTarget.CreateRenderTargetView();
            alphaTarget.CreateRenderTargetView();
#if MSAASEPARATE
            if (!hasMsaa)
#endif
            {
                alphaTarget.CreateTextureView();
                colorTarget.CreateTextureView();
                colorTargetNoMsaa = colorTarget;
                alphaTargetNoMsaa = alphaTarget;
            }
#if MSAASEPARATE
            else {
                colorDesc.SampleDescription = alphaDesc.SampleDescription = new SampleDescription(1, 0);
                colorDesc.BindFlags = alphaDesc.BindFlags = BindFlags.ShaderResource;
                colorTargetNoMsaa = new ShaderResourceViewProxy(Device, colorDesc);
                alphaTargetNoMsaa = new ShaderResourceViewProxy(Device, alphaDesc);
                colorTargetNoMsaa.CreateTextureView();
                alphaTargetNoMsaa.CreateTextureView();
            }
#endif
            RaiseInvalidateRender();
            return true; // Skip this frame if texture resized to reduce latency.
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Bind(RenderContext context, DeviceContextProxy deviceContext) {
        targets = deviceContext.GetRenderTargets(2);
        deviceContext.ClearRenderTargetView(colorTarget, Color.Zero);
        deviceContext.ClearRenderTargetView(alphaTarget, Color.White);
        deviceContext.SetRenderTargets(context.RenderHost.DepthStencilBufferView,
                                       [colorTarget, alphaTarget]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnBind(RenderContext context, DeviceContextProxy deviceContext) {
        deviceContext.SetRenderTargets(context.RenderHost.DepthStencilBufferView, targets);
        for (var i = 0; i < targets.Length; ++i) {
            targets[i]?.Dispose();
            targets[i] = null;
        }
#if MSAASEPARATE
        if (hasMsaa) {
            deviceContext.ResolveSubresource(colorTarget.Resource,
                                             0,
                                             colorTargetNoMsaa.Resource,
                                             0,
                                             colorDesc.Format);
            deviceContext.ResolveSubresource(alphaTarget.Resource,
                                             0,
                                             alphaTargetNoMsaa.Resource,
                                             0,
                                             alphaDesc.Format);
        }
#endif
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        screenQuadPass = technique[DefaultPassNames.Default];
        colorTexIndex =
            screenQuadPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.OitColorTb);
        alphaTexIndex =
            screenQuadPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.OitAlphaTb);
        samplerIndex =
            screenQuadPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SurfaceSampler);
        targetSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.LinearSamplerWrapAni1);
        RenderCount = 0;
        return true;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref targetSampler);
        width = height = 0;
        RemoveAndDispose(ref colorTarget);
        RemoveAndDispose(ref alphaTarget);
        RemoveAndDispose(ref colorTargetNoMsaa);
        RemoveAndDispose(ref alphaTargetNoMsaa);
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        RenderCount = 0;
        if (context.RenderHost.PerFrameTransparentNodes.Count == 0) return;

        if (CreateTextureResources(context, deviceContext)) {
            RaiseInvalidateRender();
            return; // Skip this frame if texture resized to reduce latency.
        }

        Bind(context, deviceContext);

        context.OitRenderStage = OitRenderStage.SinglePassWeighted;
        var parameter = ExternRenderParameter;
        if (!parameter.ScissorRegion.IsEmpty) {
            parameter.RenderTargetView = [colorTarget, alphaTarget];
            RenderCount = context.RenderHost.Renderer.RenderOpaque(context,
                                                                   context.RenderHost.PerFrameTransparentNodes,
                                                                   ref parameter,
                                                                   context.EnableBoundingFrustum);
        } else {
            var frustum = context.BoundingFrustum;
            var count = context.RenderHost.PerFrameTransparentNodes.Count;
            for (var i = 0; i < count; ++i) {
                var renderable = context.RenderHost.PerFrameTransparentNodes[i];
                renderable.RenderCore.Render(context, deviceContext);
                ++RenderCount;
            }
        }

        context.OitRenderStage = OitRenderStage.None;
        UnBind(context, deviceContext);
        screenQuadPass.BindShader(deviceContext);
        screenQuadPass.BindStates(deviceContext,
                                  StateType.BlendState | StateType.DepthStencilState | StateType.RasterState);
        screenQuadPass.PixelShader.BindTexture(deviceContext, colorTexIndex, colorTargetNoMsaa);
        screenQuadPass.PixelShader.BindTexture(deviceContext, alphaTexIndex, alphaTargetNoMsaa);
        screenQuadPass.PixelShader.BindSampler(deviceContext, samplerIndex, targetSampler);
        deviceContext.Draw(4, 0);
    }

#region Variables

    private ShaderResourceViewProxy colorTarget;
    private ShaderResourceViewProxy alphaTarget;
    private ShaderResourceViewProxy colorTargetNoMsaa;
    private ShaderResourceViewProxy alphaTargetNoMsaa;
    private SamplerStateProxy targetSampler;

    private SampleDescription sampleDesc = new(1, 0);

    private Texture2DDescription colorDesc = new() {
        Format = Format.FormatR16G16B16A16Float,
        OptionFlags = ResourceOptionFlags.None,
        MipLevels = 1,
        ArraySize = 1,
        Usage = ResourceUsage.Default,
        CpuAccessFlags = CpuAccessFlags.None
    };

    private Texture2DDescription alphaDesc = new() {
        Format = Format.FormatA8Unorm,
        OptionFlags = ResourceOptionFlags.None,
        MipLevels = 1,
        ArraySize = 1,
        Usage = ResourceUsage.Default,
        CpuAccessFlags = CpuAccessFlags.None
    };

    private int width;
    private int height;
#if MSAASEPARATE
    private bool hasMsaa;
#endif
    private ShaderPass screenQuadPass = ShaderPass.NullPass;
    private int colorTexIndex, alphaTexIndex, samplerIndex;
    private RenderTargetView[] targets;

#endregion

#region Properties

    public int RenderCount { get; private set; }

    public RenderParameter ExternRenderParameter { get; set; }

#endregion
}