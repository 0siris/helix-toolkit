/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Maths;
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

            public void ClearState()
            {
                nativeContext.ClearState();
            }

            public void Flush()
            {
                nativeContext.Flush();
            }

            public void Draw(uint vertexCount, uint startVertexLocation)
            {
                nativeContext.Draw(vertexCount, startVertexLocation);
            }

            public void DrawAuto()
            {
                nativeContext.DrawAuto();
            }

            public void DrawIndexed(uint indexCount, uint startIndexLocation, int baseVertexLocation)
            {
                nativeContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            }

            public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation)
            {
                nativeContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            }

            public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
            {
                nativeContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            }

            public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
            {
                nativeContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            }

            public D3DPrimitiveTopology PrimitiveTopology
            {
                get
                {
                    nativeContext.IAGetPrimitiveTopology(out D3DPrimitiveTopology topology);
                    return topology;
                }
                set
                {
                    nativeContext.IASetPrimitiveTopology(value);
                }
            }

            public void SetViewport(float x, float y, float width, float height, float minZ, float maxZ)
            {
                var viewport = new Viewport(x, y, width, height, minZ, maxZ);
                nativeContext.RSSetViewports(1, ref viewport);
            }

            public void SetScissorRectangle(int left, int top, int right, int bottom)
            {
                var rectangle = new Box2D<int>(left, top, right, bottom);
                nativeContext.RSSetScissorRects(1, ref rectangle);
            }

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
