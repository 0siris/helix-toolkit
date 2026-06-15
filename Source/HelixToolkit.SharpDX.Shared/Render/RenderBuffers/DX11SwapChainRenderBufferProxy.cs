/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Render
    {
        using Core2D;
        using Utilities;
        /// <summary>
        /// 
        /// </summary>
        public class DX11SwapChainRenderBufferProxy : DX11RenderBufferProxyBase
        {
            private SwapChain1 swapChain;
            private ShaderResourceViewProxy backBuffer;
            /// <summary>
            /// Gets the swap chain.
            /// </summary>
            /// <value>
            /// The swap chain.
            /// </value>
            public SwapChain1 SwapChain
            {
                get
                {
                    return swapChain;
                }
            }
            /// <summary>
            /// The surface pointer
            /// </summary>
            protected readonly System.IntPtr surfacePtr;
            /// <summary>
            /// Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy"/> class.
            /// </summary>
            /// <param name="surfacePointer">The surface pointer.</param>
            /// <param name="deviceResource"></param>
            public DX11SwapChainRenderBufferProxy(System.IntPtr surfacePointer, IDeviceResources deviceResource) : base(deviceResource)
            {
                surfacePtr = surfacePointer;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy"/> class.
            /// </summary>
            /// <param name="surfacePointer">The surface pointer.</param>
            /// <param name="deviceResource"></param>
            /// <param name="useDepthStencilBuffer"></param>
            public DX11SwapChainRenderBufferProxy(System.IntPtr surfacePointer, IDeviceResources deviceResource, bool useDepthStencilBuffer)
                : base(deviceResource, useDepthStencilBuffer)
            {
                surfacePtr = surfacePointer;
            }
            /// <summary>
            /// Called when [create render target and depth buffers].
            /// </summary>
            /// <param name="width">The width.</param>
            /// <param name="height">The height.</param>
            /// <returns></returns>
            protected override ShaderResourceViewProxy OnCreateBackBuffer(int width, int height)
            {
                if (swapChain == null || swapChain.IsDisposed)
                {
                    swapChain = CreateSwapChain(surfacePtr);
                }
                else
                {
                    RemoveAndDispose(ref d2dTarget);
                    RemoveAndDispose(ref backBuffer);                   
                    swapChain.ResizeBuffers(swapChain.Description1.BufferCount, TargetWidth, TargetHeight, swapChain.Description.ModeDescription.Format, swapChain.Description.Flags);
                }
                backBuffer = CreateBackBufferTexture(width, height);
                d2dTarget = new D2DTargetProxy();
                d2dTarget.Initialize(swapChain, DeviceContext2D);
                return backBuffer;
            }

            private ShaderResourceViewProxy CreateBackBufferTexture(int width, int height)
            {
                var desc = new Texture2DDescription
                {
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

            private SwapChain1 CreateSwapChain(System.IntPtr surfacePointer)
            {
                var desc = CreateSwapChainDescription();
                return new SwapChain1(desc, surfacePointer);
            }

            /// <summary>
            /// Creates the swap chain description.
            /// </summary>
            /// <returns>A swap chain description</returns>
            /// <remarks>
            /// This method can be overloaded in order to modify default parameters.
            /// </remarks>
            protected virtual SwapChainDescription1 CreateSwapChainDescription()
            {
                var sampleCount = 1;
                var sampleQuality = 0;
                var desc = new SwapChainDescription1()
                {
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

            private readonly PresentParameters presentParams = new PresentParameters();

            /// <summary>
            /// Presents this instance.
            /// </summary>
            /// <returns></returns>
            public override bool Present()
            {
                var res = swapChain.Present(VSyncInterval, PresentFlags.None, presentParams);
                if (res.Success)
                {
                    return true;
                }
                else
                {
                    swapChain.Present(VSyncInterval, PresentFlags.Restart, presentParams);
                    return false;
                }
            }
            /// <summary>
            /// Must release swapchain at last after all its created resources have been released.
            /// </summary>
            public void DisposeAndClear()
            {
                RemoveAndDispose(ref d2dTarget);
                RemoveAndDispose(ref backBuffer);
                RemoveAndDispose(ref swapChain);
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                DisposeAndClear();
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
