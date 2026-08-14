/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Utilities;
/// <summary>
///     Buffer based UAV/SRV view container.
/// </summary>
public sealed class UavBufferViewProxy : IDisposable {
    private bool disposedValue;
    private Resource resource;

    private ShaderResourceViewProxy srv;

    private UnorderedAccessView uav;

    public UavBufferViewProxy(
        DeviceContextProxy context,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    )
        : this(context, ref bufferDesc, ref uavDesc) {
        srv = new ShaderResourceViewProxy(context, resource);
        srv.CreateTextureView(ref srvDesc);
    }

    public UavBufferViewProxy(
        DeviceContextProxy context,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc
    ) {
        resource = new Buffer(context, bufferDesc);
        uav = context.NativeDevice.CreateUnorderedAccessView(resource, uavDesc);
    }

    public UavBufferViewProxy(
        DeviceContextProxy context,
        ref Texture2DDescription texture2DDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    ) {
        throw new NotSupportedException("Texture based UAV views are migrated with the texture resource port.");
    }

    public UavBufferViewProxy(
        object device,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    ) {
        // Legacy construction without a native DeviceContextProxy is kept only for callers
        // that are migrated in a later pass.
    }

    public UavBufferViewProxy(
        object device,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc
    ) {
        // Legacy construction without a native DeviceContextProxy is kept only for callers
        // that are migrated in a later pass.
    }

    public UavBufferViewProxy(
        object device,
        ref Texture2DDescription texture2DDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    ) {
        // Texture based UAV views are migrated with the texture resource port.
    }

    public Resource Resource => resource;
    public UnorderedAccessView Uav => uav;
    public ShaderResourceViewProxy Srv => srv;

    public void Dispose() {
        Dispose(true);
    }

    public void CopyCount(DeviceContextProxy device, Buffer destBuffer, int offset) {
        device.CopyStructureCount(destBuffer, offset, Uav);
    }

    public static implicit operator UnorderedAccessView?(UavBufferViewProxy? proxy) => proxy?.uav;

    public static implicit operator ShaderResourceViewProxy?(UavBufferViewProxy? proxy) => proxy?.srv;

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
