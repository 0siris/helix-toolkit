/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Render.RenderBuffers;
/// <summary>
/// </summary>
public class DX11SwapChainRenderBufferProxy : DX11RenderBufferProxyBase {
    private readonly PresentParameters presentParams = new();

    /// <summary>
    ///     The surface pointer
    /// </summary>
    protected readonly nint SurfacePtr;

    private ShaderResourceViewProxy? backBuffer;
    private SwapChain1? swapChain;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy" /> class.
    /// </summary>
    /// <param name="surfacePointer">The surface pointer.</param>
    /// <param name="deviceResource"></param>
    public DX11SwapChainRenderBufferProxy(nint surfacePointer, IDeviceResources deviceResource) : base(
        deviceResource) {
        SurfacePtr = surfacePointer;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy" /> class.
    /// </summary>
    /// <param name="surfacePointer">The surface pointer.</param>
    /// <param name="deviceResource"></param>
    /// <param name="useDepthStencilBuffer"></param>
    public DX11SwapChainRenderBufferProxy(
        nint surfacePointer,
        IDeviceResources deviceResource,
        bool useDepthStencilBuffer
    )
        : base(deviceResource, useDepthStencilBuffer) {
        SurfacePtr = surfacePointer;
    }

    /// <summary>
    ///     Gets the swap chain.
    /// </summary>
    /// <value>
    ///     The swap chain.
    /// </value>
    public SwapChain1? SwapChain => swapChain;

    /// <summary>
    ///     Called when [create render target and depth buffers].
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns></returns>
    protected override ShaderResourceViewProxy OnCreateBackBuffer(int width, int height) {
        if (swapChain is null || swapChain.IsDisposed) {
            swapChain = CreateSwapChain(SurfacePtr);
        } else {
            D2DTarget = null;
            RemoveAndDispose(ref backBuffer);
            DeviceResources.NativeDeviceResources.ImmediateContext.ClearState();
            DeviceResources.NativeDeviceResources.ImmediateContext.Flush();
            swapChain.ResizeBuffers(swapChain.Description1.BufferCount,
                                    width,
                                    height,
                                    swapChain.Description.ModeDescription.Format,
                                    swapChain.Description.Flags);
        }

        var currentSwapChain = swapChain ?? throw new InvalidOperationException("Swap chain was not created.");
        var newBackBuffer = new ShaderResourceViewProxy(DeviceResources, currentSwapChain.GetBackBuffer());
        if (newBackBuffer.Resource is not Texture2D texture)
            throw new InvalidOperationException("The swap chain back buffer is not a 2D texture.");

        backBuffer = newBackBuffer;
        D2DTarget = new D2DTargetProxy();
        D2DTarget.Initialize(texture, DeviceContext2D);
        return newBackBuffer;
    }

    private SwapChain1 CreateSwapChain(nint surfacePointer) {
        var desc = CreateSwapChainDescription();
        return new SwapChain1(desc, surfacePointer, DeviceResources.NativeDeviceResources.Device);
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
            Flags = SwapChainFlags.AllowModeSwitch
        };
        return desc;
    }

    /// <summary>
    ///     Presents this instance.
    /// </summary>
    /// <returns></returns>
    public override bool Present() => swapChain is { } currentSwapChain
        && currentSwapChain.Present(VSyncInterval, PresentFlags.None, presentParams).Success;

    /// <summary>
    ///     Must release swapchain at last after all its created resources have been released.
    /// </summary>
    public void DisposeAndClear() {
        D2DTarget = null;
        RemoveAndDispose(ref backBuffer);
        RemoveAndDispose(ref swapChain);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        DisposeAndClear();
        base.OnDispose(disposeManagedResources);
    }
}
