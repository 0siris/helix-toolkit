/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        /// <summary>
        ///     Buffer based UAV/SRV view container.
        /// </summary>
        public sealed class UAVBufferViewProxy : IDisposable {
            private bool disposedValue;
            private Resource resource;

            private ShaderResourceViewProxy srv;

            private UnorderedAccessView uav;

            public UAVBufferViewProxy(
                DeviceContextProxy context,
                ref BufferDescription bufferDesc,
                ref UnorderedAccessViewDescription uavDesc,
                ref ShaderResourceViewDescription srvDesc
            )
                : this(context, ref bufferDesc, ref uavDesc) {
                srv = new ShaderResourceViewProxy(context, resource);
                srv.CreateTextureView(ref srvDesc);
            }

            public UAVBufferViewProxy(
                DeviceContextProxy context,
                ref BufferDescription bufferDesc,
                ref UnorderedAccessViewDescription uavDesc
            ) {
                resource = new Buffer(context, bufferDesc);
                uav = context.NativeDevice.CreateUnorderedAccessView(resource, uavDesc);
            }

            public UAVBufferViewProxy(
                DeviceContextProxy context,
                ref Texture2DDescription texture2DDesc,
                ref UnorderedAccessViewDescription uavDesc,
                ref ShaderResourceViewDescription srvDesc
            ) {
                throw new NotSupportedException("Texture based UAV views are migrated with the texture resource port.");
            }

            public UAVBufferViewProxy(
                object device,
                ref BufferDescription bufferDesc,
                ref UnorderedAccessViewDescription uavDesc,
                ref ShaderResourceViewDescription srvDesc
            ) {
                // Legacy construction without a native DeviceContextProxy is kept only for callers
                // that are migrated in a later pass.
            }

            public UAVBufferViewProxy(
                object device,
                ref BufferDescription bufferDesc,
                ref UnorderedAccessViewDescription uavDesc
            ) {
                // Legacy construction without a native DeviceContextProxy is kept only for callers
                // that are migrated in a later pass.
            }

            public UAVBufferViewProxy(
                object device,
                ref Texture2DDescription texture2DDesc,
                ref UnorderedAccessViewDescription uavDesc,
                ref ShaderResourceViewDescription srvDesc
            ) {
                // Texture based UAV views are migrated with the texture resource port.
            }

            public Resource Resource => resource;
            public UnorderedAccessView UAV => uav;
            public ShaderResourceViewProxy SRV => srv;

            public void Dispose() {
                Dispose(true);
            }

            public void CopyCount(DeviceContextProxy device, Buffer destBuffer, int offset) {
                device.CopyStructureCount(destBuffer, offset, UAV);
            }

            public static implicit operator UnorderedAccessView(UAVBufferViewProxy proxy) {
                return proxy == null ? null : proxy.uav;
            }

            public static implicit operator ShaderResourceViewProxy(UAVBufferViewProxy proxy) {
                return proxy == null ? null : proxy.srv;
            }

            [SuppressMessage("Microsoft.Usage",
                             "CA2213: Disposable fields should be disposed",
                             Justification = "False positive.")]
            private void Dispose(bool disposing) {
                if (disposedValue) return;

                if (disposing) {
                    Disposer.RemoveAndDispose(ref uav);
                    Disposer.RemoveAndDispose(ref srv);
                    Disposer.RemoveAndDispose(ref resource);
                }

                disposedValue = true;
            }
        }
    }
}
