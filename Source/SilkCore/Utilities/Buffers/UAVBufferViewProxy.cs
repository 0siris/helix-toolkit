/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using Buffer = HelixToolkit.SharpDX.Core.Native.Buffer;

namespace HelixToolkit.SharpDX.Core.Utilities.Buffers;
/// <summary>
///     Buffer based UAV/SRV view container.
/// </summary>
public sealed class UavBufferViewProxy : IDisposable {
    private bool disposedValue;
    private NativeD3DResource? resource;

    private ShaderResourceViewProxy? srv;

    private UnorderedAccessView? uav;

    public UavBufferViewProxy(
        DeviceContextProxy context,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    )
        : this(context, ref bufferDesc, ref uavDesc) {
        srv = new ShaderResourceViewProxy(context, Resource);
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
        var nativeDevice = device as NativeD3DDevice
            ?? throw new ArgumentException("A native D3D device is required.", nameof(device));
        resource = new Buffer(nativeDevice.CreateBuffer(bufferDesc), nativeDevice, bufferDesc);
        uav = nativeDevice.CreateUnorderedAccessView(resource, uavDesc);
        srv = new ShaderResourceViewProxy(resource, nativeDevice.CreateShaderResourceView(resource, srvDesc));
    }

    public UavBufferViewProxy(
        object device,
        ref BufferDescription bufferDesc,
        ref UnorderedAccessViewDescription uavDesc
    ) {
        var nativeDevice = device as NativeD3DDevice
            ?? throw new ArgumentException("A native D3D device is required.", nameof(device));
        resource = new Buffer(nativeDevice.CreateBuffer(bufferDesc), nativeDevice, bufferDesc);
        uav = nativeDevice.CreateUnorderedAccessView(resource, uavDesc);
    }

    public UavBufferViewProxy(
        object device,
        ref Texture2DDescription texture2DDesc,
        ref UnorderedAccessViewDescription uavDesc,
        ref ShaderResourceViewDescription srvDesc
    ) {
        throw new NotSupportedException("Texture based UAV views are not supported by this proxy.");
    }

    public Resource Resource => resource ?? throw new ObjectDisposedException(nameof(UavBufferViewProxy));
    public UnorderedAccessView Uav => uav ?? throw new ObjectDisposedException(nameof(UavBufferViewProxy));
    public ShaderResourceViewProxy? Srv => srv;

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
