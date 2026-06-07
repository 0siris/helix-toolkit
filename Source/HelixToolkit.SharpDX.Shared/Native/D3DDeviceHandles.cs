/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using Silk.NET.Direct3D11;
using SilkD3D11ContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DeviceContext>;
using SilkD3D11DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Device>;

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
    namespace Native
    {
        internal enum SilkDriverType
        {
            Unknown = 0,
            Hardware,
            Warp,
            Reference,
            Software
        }

        internal enum SilkFeatureLevel
        {
            Unknown = 0,
            Level_9_1,
            Level_9_2,
            Level_9_3,
            Level_10_0,
            Level_10_1,
            Level_11_0,
            Level_11_1
        }

        internal unsafe sealed class SilkD3DDevice : IDisposable
        {
            private SilkD3D11DevicePtr nativeDevice;

            public SilkD3DDevice(SilkD3D11DevicePtr nativeDevice, SilkDriverType driverType, SilkFeatureLevel featureLevel)
            {
                if (nativeDevice.Handle == null)
                {
                    throw new ArgumentNullException(nameof(nativeDevice));
                }

                this.nativeDevice = nativeDevice;
                DriverType = driverType;
                FeatureLevel = featureLevel;
            }

            public IntPtr NativePointer => (IntPtr)nativeDevice.Handle;

            public ID3D11Device* Handle => nativeDevice.Handle;

            public ref SilkD3D11DevicePtr NativeDevice => ref nativeDevice;

            public SilkDriverType DriverType { get; }

            public SilkFeatureLevel FeatureLevel { get; }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                nativeDevice.Dispose();
                IsDisposed = true;
            }
        }

        internal unsafe sealed class SilkD3DDeviceContext : IDisposable
        {
            private SilkD3D11ContextPtr nativeContext;

            public SilkD3DDeviceContext(SilkD3D11ContextPtr nativeContext, bool isDeferred)
            {
                if (nativeContext.Handle == null)
                {
                    throw new ArgumentNullException(nameof(nativeContext));
                }

                this.nativeContext = nativeContext;
                IsDeferred = isDeferred;
            }

            public IntPtr NativePointer => (IntPtr)nativeContext.Handle;

            public ID3D11DeviceContext* Handle => nativeContext.Handle;

            public ref SilkD3D11ContextPtr NativeContext => ref nativeContext;

            public bool IsDeferred { get; }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                nativeContext.Dispose();
                IsDisposed = true;
            }
        }
    }
}
