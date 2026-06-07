/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SilkD3D11DepthStencilViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilView>;
using SilkD3D11RenderTargetViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RenderTargetView>;
using SilkD3D11ShaderResourceViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ShaderResourceView>;
using SilkD3D11UnorderedAccessViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11UnorderedAccessView>;

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
    [Flags]
    public enum DepthStencilClearFlags
    {
        Depth = 1,
        Stencil = 2
    }

    public unsafe sealed class RenderTargetView : IDisposable
    {
        private SilkD3D11RenderTargetViewPtr nativeView;

        internal RenderTargetView(SilkD3D11RenderTargetViewPtr nativeView)
        {
            this.nativeView = nativeView;
        }

        public IntPtr NativePointer => (IntPtr)nativeView.Handle;

        internal ID3D11RenderTargetView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public unsafe sealed class DepthStencilView : IDisposable
    {
        private SilkD3D11DepthStencilViewPtr nativeView;

        internal DepthStencilView(SilkD3D11DepthStencilViewPtr nativeView)
        {
            this.nativeView = nativeView;
        }

        public IntPtr NativePointer => (IntPtr)nativeView.Handle;

        internal ID3D11DepthStencilView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public unsafe sealed class ShaderResourceView : IDisposable
    {
        private SilkD3D11ShaderResourceViewPtr nativeView;

        internal ShaderResourceView(SilkD3D11ShaderResourceViewPtr nativeView)
        {
            this.nativeView = nativeView;
        }

        public IntPtr NativePointer => (IntPtr)nativeView.Handle;

        internal ID3D11ShaderResourceView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public unsafe sealed class UnorderedAccessView : IDisposable
    {
        private SilkD3D11UnorderedAccessViewPtr nativeView;

        internal UnorderedAccessView(SilkD3D11UnorderedAccessViewPtr nativeView)
        {
            this.nativeView = nativeView;
        }

        public IntPtr NativePointer => (IntPtr)nativeView.Handle;

        internal ID3D11UnorderedAccessView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            nativeView.Dispose();
            IsDisposed = true;
        }
    }
}
