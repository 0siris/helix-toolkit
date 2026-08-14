using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Render;
public partial class DeviceContextProxy {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataBox MapSubresource(Buffer resource, MapMode mode, MapFlags flags) => NativeContext.MapSubresource(resource, 0, mode, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataBox MapSubresource(Buffer resource, int subresource, MapMode mode, MapFlags flags) => NativeContext.MapSubresource(resource, subresource, mode, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataBox MapSubresource(Buffer resource, MapMode mode, MapFlags flags, out DataStream stream) => NativeContext.MapSubresource(resource, 0, mode, flags, out stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataBox MapSubresource(
        Resource resource,
        int subresource,
        MapMode mode,
        MapFlags flags,
        out DataStream stream
    )
        => NativeContext.MapSubresource(resource, subresource, mode, flags, out stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataBox MapSubresource(Resource resource, int subresource, MapMode mapType, MapFlags mapFlags) => NativeContext.MapSubresource(resource, subresource, mapType, mapFlags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnmapSubresource(Resource resource, int subresource) {
        NativeContext.UnmapSubresource(resource, subresource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResolveSubresource(
        Resource source,
        int sourceSubresource,
        Resource destination,
        int destinationSubresource,
        Format format
    ) {
        NativeContext.ResolveSubresource(source,
                                         sourceSubresource,
                                         destination,
                                         destinationSubresource,
                                         format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresource(
        Resource dstResourceRef,
        int dstSubresource,
        ResourceRegion? dstBoxRef,
        nint srcDataRef,
        int srcRowPitch,
        int srcDepthPitch
    ) {
        NativeContext.UpdateSubresource(dstResourceRef,
                                        dstSubresource,
                                        dstBoxRef,
                                        srcDataRef,
                                        srcRowPitch,
                                        srcDepthPitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresource(
        DataBox source,
        Resource resource,
        int subresource,
        ref ResourceRegion region
    ) {
        NativeContext.UpdateSubresource(resource,
                                        subresource,
                                        region,
                                        source.DataPointer,
                                        source.RowPitch,
                                        source.SlicePitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresource(DataBox source, Resource resource, int subresource = 0) {
        NativeContext.UpdateSubresource(resource,
                                        subresource,
                                        null,
                                        source.DataPointer,
                                        source.RowPitch,
                                        source.SlicePitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UpdateSubresource<T>(
        T[] data,
        Resource resource,
        int subresource = 0,
        int rowPitch = 0,
        int depthPitch = 0,
        ResourceRegion? region = null
    )
        where T : unmanaged {
        fixed (T* dataPtr = data) {
            NativeContext.UpdateSubresource(resource,
                                            subresource,
                                            region,
                                            (nint)dataPtr,
                                            rowPitch,
                                            depthPitch);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UpdateSubresource<T>(
        ref T data,
        Resource resource,
        int subresource = 0,
        int rowPitch = 0,
        int depthPitch = 0,
        ResourceRegion? region = null
    )
        where T : unmanaged {
        fixed (T* dataPtr = &data) {
            NativeContext.UpdateSubresource(resource,
                                            subresource,
                                            region,
                                            (nint)dataPtr,
                                            rowPitch,
                                            depthPitch);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresourceSafe(
        ref DataBox source,
        Resource resource,
        int srcBytesPerElement,
        int subresource = 0,
        bool isCompressedResource = false
    ) {
        NativeContext.UpdateSubresource(resource,
                                        subresource,
                                        null,
                                        source.DataPointer,
                                        source.RowPitch,
                                        source.SlicePitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresourceSafe(
        ref DataBox source,
        Resource resource,
        int srcBytesPerElement,
        int subresource,
        ResourceRegion region,
        bool isCompressedResource = false
    ) {
        NativeContext.UpdateSubresource(resource,
                                        subresource,
                                        region,
                                        source.DataPointer,
                                        source.RowPitch,
                                        source.SlicePitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresourceSafe<T>(
        T[] data,
        Resource resource,
        int srcBytesPerElement,
        int subresource = 0,
        int rowPitch = 0,
        int depthPitch = 0,
        bool isCompressedResource = false
    )
        where T : unmanaged {
        UpdateSubresource(data, resource, subresource, rowPitch, depthPitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSubresourceSafe<T>(
        ref T data,
        Resource resource,
        int srcBytesPerElement,
        int subresource = 0,
        int rowPitch = 0,
        int depthPitch = 0,
        bool isCompressedResource = false
    )
        where T : unmanaged {
        UpdateSubresource(ref data, resource, subresource, rowPitch, depthPitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyResource(Resource source, Resource destination) {
        NativeContext.CopyResource(source, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyStructureCount(
        Buffer dstBufferRef,
        int dstAlignedByteOffset,
        UnorderedAccessView srcViewRef
    ) {
        NativeContext.CopyStructureCount(dstBufferRef, dstAlignedByteOffset, srcViewRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopySubresourceRegion(
        Resource source,
        int sourceSubresource,
        ResourceRegion? sourceRegion,
        Resource destination,
        int destinationSubResource,
        int dstX = 0,
        int dstY = 0,
        int dstZ = 0
    ) {
        NativeContext.CopySubresourceRegion(source,
                                            sourceSubresource,
                                            sourceRegion,
                                            destination,
                                            destinationSubResource,
                                            dstX,
                                            dstY,
                                            dstZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GenerateMips(ShaderResourceView shaderResourceViewRef) {
        NativeContext.GenerateMips(shaderResourceViewRef);
    }
}
