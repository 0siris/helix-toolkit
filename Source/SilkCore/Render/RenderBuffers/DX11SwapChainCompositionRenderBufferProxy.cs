/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public class DX11SwapChainCompositionRenderBufferProxy : DX11RenderBufferProxyBase {
    private readonly PresentParameters presentParams = new();
    private ShaderResourceViewProxy? backBuffer;
    private SwapChain2? swapChain;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy" /> class.
    /// </summary>
    /// <param name="deviceResource"></param>
    public DX11SwapChainCompositionRenderBufferProxy(IDeviceResources deviceResource) : base(deviceResource) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy" /> class.
    /// </summary>
    /// <param name="deviceResource"></param>
    /// <param name="useDepthStencilBuffer"></param>
    public DX11SwapChainCompositionRenderBufferProxy(
        IDeviceResources deviceResource,
        bool useDepthStencilBuffer
    )
        : base(deviceResource, useDepthStencilBuffer) { }

    /// <summary>
    ///     Gets the swap chain.
    /// </summary>
    /// <value>
    ///     The swap chain.
    /// </value>
    public SwapChain2? SwapChain => swapChain;

    /// <summary>
    ///     Called when [create render target and depth buffers].
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns></returns>
    protected override ShaderResourceViewProxy OnCreateBackBuffer(int width, int height) {
        if (swapChain is null || swapChain.IsDisposed)
            swapChain = CreateSwapChain();
        else
            swapChain.ResizeBuffers(swapChain.Description1.BufferCount,
                                    TargetWidth,
                                    TargetHeight,
                                    swapChain.Description.ModeDescription.Format,
                                    swapChain.Description.Flags);

        var backBuffer = CreateBackBufferTexture(width, height);
        if (backBuffer.Resource is not Texture2D texture)
            throw new InvalidOperationException("The swap chain back buffer is not a 2D texture.");
        this.backBuffer = backBuffer;
        d2dTarget = new D2DTargetProxy();
        d2dTarget.Initialize(texture, DeviceContext2D);
        return backBuffer;
    }

    private ShaderResourceViewProxy CreateBackBufferTexture(int width, int height) {
        var desc = new Texture2DDescription {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Format,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            OptionFlags = ResourceOptionFlags.Shared,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };
        return new ShaderResourceViewProxy(DeviceResources, desc);
    }

    private SwapChain2 CreateSwapChain() {
        var desc = CreateSwapChainDescription();
        return new SwapChain2(desc);
    }

    /// <summary>
    ///     Creates the swap chain description.
    /// </summary>
    /// <returns>A swap chain description</returns>
    /// <remarks>
    ///     This method can be overloaded in order to modify default parameters.
    /// </remarks>
    protected virtual SwapChainDescription1 CreateSwapChainDescription() {
        var sampleCount = 1;
        var sampleQuality = 0;
        // SwapChain description

        var desc = new SwapChainDescription1 {
            Width = Math.Max(1, TargetWidth),
            Height = Math.Max(1, TargetHeight),
            // B8G8R8A8_UNorm gives us better performance
            Format = Format,
            Stereo = false,
            SampleDescription = new SampleDescription(sampleCount, sampleQuality),
            Usage = Usage.RenderTargetOutput,
            BufferCount = 2,
            SwapEffect = SwapEffect.FlipSequential,
            Scaling = Scaling.Stretch,
            Flags = SwapChainFlags.None
        };
        return desc;
    }

    /// <summary>
    ///     Presents this instance.
    /// </summary>
    /// <returns></returns>
    public override bool Present() {
        if (swapChain is not { } currentSwapChain) return false;
        var res = currentSwapChain.Present(VSyncInterval, PresentFlags.None, presentParams);
        if (res.Success) return true;

        currentSwapChain.Present(VSyncInterval, PresentFlags.Restart, presentParams);
        return false;
    }

    /// <summary>
    ///     Must release swapchain at last after all its created resources have been released.
    /// </summary>
    public void DisposeAndClear() {
        RemoveAndDispose(ref backBuffer);
        RemoveAndDispose(ref d2dTarget);
        RemoveAndDispose(ref swapChain);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        DisposeAndClear();
        base.OnDispose(disposeManagedResources);
    }
}
