/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D3D12Range = Silk.NET.Direct3D12.Range;
using SilkD3DBlobPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Core.Native.ID3D10Blob>;
using SilkD3D12DescriptorHeapPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12DescriptorHeap>;
using SilkD3D12ResourcePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Resource>;
using SilkD3D12RootSignaturePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12RootSignature>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Describes one CPU texture subresource before Direct3D 12 placement alignment is applied.
/// </summary>
/// <param name="Data">The source bytes.</param>
/// <param name="RowPitch">The source byte count per row.</param>
/// <param name="SlicePitch">The source byte count for one complete two-dimensional depth slice.</param>
internal readonly record struct D3D12SubresourceData(
    ReadOnlyMemory<byte> Data,
    uint RowPitch,
    uint SlicePitch
);

/// <summary>
///     Owns a fixed-size Direct3D 12 descriptor heap and its index allocator.
/// </summary>
public sealed unsafe class SilkD3D12DescriptorHeap : IDisposable {
    private readonly D3D12DescriptorIndexAllocator allocator;
    private SilkD3D12DescriptorHeapPtr nativeHeap;

    /// <summary>
    ///     Initializes a Direct3D 12 descriptor heap wrapper.
    /// </summary>
    /// <param name="nativeHeap">The native descriptor heap.</param>
    /// <param name="type">The descriptor heap type.</param>
    /// <param name="capacity">The descriptor capacity.</param>
    /// <param name="descriptorSize">The native descriptor stride.</param>
    /// <param name="shaderVisible">Whether shaders can access the heap.</param>
    internal SilkD3D12DescriptorHeap(
        SilkD3D12DescriptorHeapPtr nativeHeap,
        DescriptorHeapType type,
        int capacity,
        uint descriptorSize,
        bool shaderVisible
    ) {
        if (nativeHeap.Handle == null) throw new ArgumentNullException(nameof(nativeHeap));

        this.nativeHeap = nativeHeap;
        allocator = new D3D12DescriptorIndexAllocator(capacity);
        Type = type;
        DescriptorSize = descriptorSize;
        IsShaderVisible = shaderVisible;
    }

    /// <summary>
    ///     Gets the native descriptor heap pointer.
    /// </summary>
    public nint NativePointer => (nint) nativeHeap.Handle;

    /// <summary>
    ///     Gets the native descriptor heap for command recording.
    /// </summary>
    internal ID3D12DescriptorHeap* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeHeap.Handle;
        }
    }

    /// <summary>
    ///     Gets the descriptor heap type.
    /// </summary>
    public DescriptorHeapType Type { get; }

    /// <summary>
    ///     Gets the descriptor capacity.
    /// </summary>
    public int Capacity => allocator.Capacity;

    /// <summary>
    ///     Gets the number of allocated descriptors.
    /// </summary>
    public int Count => allocator.Count;

    /// <summary>
    ///     Gets the native descriptor stride.
    /// </summary>
    public uint DescriptorSize { get; }

    /// <summary>
    ///     Gets whether shaders can access the descriptor heap.
    /// </summary>
    public bool IsShaderVisible { get; }

    /// <summary>
    ///     Gets whether the native descriptor heap has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Allocates one descriptor from the heap.
    /// </summary>
    /// <returns>The allocated CPU and optional GPU descriptor handles.</returns>
    public SilkD3D12Descriptor Allocate() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return CreateDescriptor(allocator.Allocate());
    }

    /// <summary>
    ///     Allocates one contiguous descriptor range.
    /// </summary>
    /// <param name="count">The number of descriptors in the range.</param>
    /// <returns>The descriptors in increasing heap-index order.</returns>
    internal SilkD3D12Descriptor[] AllocateRange(int count) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var firstIndex = allocator.AllocateRange(count);
        var descriptors = new SilkD3D12Descriptor[count];
        for (var offset = 0; offset < count; offset++)
            descriptors[offset] = CreateDescriptor(firstIndex + offset);
        return descriptors;
    }

    /// <summary>
    ///     Creates the managed descriptor wrapper for one allocated heap index.
    /// </summary>
    /// <param name="index">The allocated heap index.</param>
    /// <returns>The descriptor wrapper.</returns>
    private SilkD3D12Descriptor CreateDescriptor(int index) {
        var offset = (nuint) index * DescriptorSize;
        var cpuStart = nativeHeap.GetCPUDescriptorHandleForHeapStart();
        var gpuStart = IsShaderVisible
            ? nativeHeap.GetGPUDescriptorHandleForHeapStart()
            : default;

        return new SilkD3D12Descriptor(this,
            index,
            new CpuDescriptorHandle(cpuStart.Ptr + offset),
            IsShaderVisible
                ? new GpuDescriptorHandle(gpuStart.Ptr + (ulong) offset)
                : default);
    }

    /// <summary>
    ///     Returns a descriptor index to this heap.
    /// </summary>
    /// <param name="index">The descriptor index.</param>
    internal void Release(int index) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        allocator.Release(index);
    }

    /// <summary>
    ///     Releases the native descriptor heap.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        nativeHeap.Dispose();
        IsDisposed = true;
    }
}

/// <summary>
///     Represents one allocated Direct3D 12 descriptor.
/// </summary>
public sealed class SilkD3D12Descriptor : IDisposable {
    private SilkD3D12DescriptorHeap? owner;

    /// <summary>
    ///     Initializes one allocated descriptor.
    /// </summary>
    /// <param name="owner">The owning descriptor heap.</param>
    /// <param name="index">The descriptor index.</param>
    /// <param name="cpuHandle">The CPU descriptor handle.</param>
    /// <param name="gpuHandle">The GPU descriptor handle, if shader visible.</param>
    internal SilkD3D12Descriptor(
        SilkD3D12DescriptorHeap owner,
        int index,
        CpuDescriptorHandle cpuHandle,
        GpuDescriptorHandle gpuHandle
    ) {
        this.owner = owner;
        Index = index;
        Type = owner.Type;
        CpuHandle = cpuHandle;
        GpuHandle = gpuHandle;
    }

    /// <summary>
    ///     Gets the zero-based heap index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets the descriptor heap type.
    /// </summary>
    public DescriptorHeapType Type { get; }

    /// <summary>
    ///     Gets the CPU descriptor handle.
    /// </summary>
    public CpuDescriptorHandle CpuHandle { get; }

    /// <summary>
    ///     Gets the GPU descriptor handle, or zero for a non-shader-visible heap.
    /// </summary>
    public GpuDescriptorHandle GpuHandle { get; }

    /// <summary>
    ///     Gets whether this allocation has been returned to its heap.
    /// </summary>
    public bool IsDisposed => owner is null;

    /// <summary>
    ///     Returns the descriptor to its heap.
    /// </summary>
    public void Dispose() {
        var heap = Interlocked.Exchange(ref owner, null);
        heap?.Release(Index);
    }
}

/// <summary>
///     Owns one committed Direct3D 12 resource.
/// </summary>
public sealed unsafe class SilkD3D12Resource : IDisposable {
    private readonly D3D12ResourceStateTracker stateTracker;
    private SilkD3D12ResourcePtr nativeResource;

    /// <summary>
    ///     Initializes a committed Direct3D 12 resource wrapper.
    /// </summary>
    /// <param name="nativeResource">The native resource.</param>
    /// <param name="description">The native resource description.</param>
    /// <param name="sizeInBytes">The buffer size.</param>
    /// <param name="heapType">The resource heap type.</param>
    /// <param name="initialState">The initial resource state.</param>
    /// <param name="stateTracker">The owning device's resource-state tracker.</param>
    internal SilkD3D12Resource(
        SilkD3D12ResourcePtr nativeResource,
        ResourceDesc description,
        ulong sizeInBytes,
        HeapType heapType,
        ResourceStates initialState,
        D3D12ResourceStateTracker stateTracker
    ) {
        if (nativeResource.Handle == null) throw new ArgumentNullException(nameof(nativeResource));

        this.nativeResource = nativeResource;
        Description = description;
        SizeInBytes = sizeInBytes;
        HeapType = heapType;
        this.stateTracker = stateTracker;
        stateTracker.Track(NativePointer, initialState);
    }

    /// <summary>
    ///     Gets the native resource pointer.
    /// </summary>
    public nint NativePointer => (nint) nativeResource.Handle;

    /// <summary>
    ///     Gets the GPU virtual address.
    /// </summary>
    public ulong GpuVirtualAddress => nativeResource.GetGPUVirtualAddress();

    /// <summary>
    ///     Gets the native resource description.
    /// </summary>
    public ResourceDesc Description { get; }

    /// <summary>
    ///     Gets the buffer size.
    /// </summary>
    public ulong SizeInBytes { get; }

    /// <summary>
    ///     Gets the resource heap type.
    /// </summary>
    public HeapType HeapType { get; }

    /// <summary>
    ///     Gets the last recorded resource state.
    /// </summary>
    public ResourceStates State => stateTracker.GetState(NativePointer);

    /// <summary>
    ///     Gets whether the native resource has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Copies bytes into an upload-heap buffer.
    /// </summary>
    /// <param name="data">The bytes to copy.</param>
    /// <param name="destinationOffset">The byte offset in the destination buffer.</param>
    public void Write(ReadOnlySpan<byte> data, ulong destinationOffset = 0) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Description.Dimension != ResourceDimension.Buffer || HeapType != HeapType.Upload)
            throw new InvalidOperationException("Only upload-heap buffers can be written directly.");
        ValidateRange(destinationOffset, (ulong) data.Length);

        void* mapped = null;
        SilkMarshal.ThrowHResult(nativeResource.Map(0, (D3D12Range*) null, &mapped));
        try {
            data.CopyTo(new Span<byte>((byte*) mapped + (nint) destinationOffset, data.Length));
        } finally {
            var writtenRange = new D3D12Range((nuint) destinationOffset,
                (nuint) (destinationOffset + (ulong) data.Length));
            nativeResource.Unmap(0, &writtenRange);
        }
    }

    /// <summary>
    ///     Reads bytes from a readback-heap buffer.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="sourceOffset">The byte offset in the source buffer.</param>
    /// <returns>The copied bytes.</returns>
    public byte[] Read(int length, ulong sourceOffset = 0) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Description.Dimension != ResourceDimension.Buffer || HeapType != HeapType.Readback)
            throw new InvalidOperationException("Only readback-heap buffers can be read directly.");
        length.Guard().Range(0, int.MaxValue);
        ValidateRange(sourceOffset, (ulong) length);

        var readRange = new D3D12Range((nuint) sourceOffset, (nuint) (sourceOffset + (ulong) length));
        void* mapped = null;
        SilkMarshal.ThrowHResult(nativeResource.Map(0, &readRange, &mapped));
        try {
            return new ReadOnlySpan<byte>((byte*) mapped + (nint) sourceOffset, length).ToArray();
        } finally {
            var emptyRange = new D3D12Range(0, 0);
            nativeResource.Unmap(0, &emptyRange);
        }
    }

    /// <summary>
    ///     Releases the native resource.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        stateTracker.Untrack(NativePointer);
        nativeResource.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Gets the native resource handle for command recording.
    /// </summary>
    internal ID3D12Resource* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeResource.Handle;
        }
    }

    /// <summary>
    ///     Records a state change in the owning device's tracker.
    /// </summary>
    /// <param name="desiredState">The desired resource state.</param>
    /// <param name="transition">The required transition.</param>
    /// <returns><see langword="true" /> when a barrier is required.</returns>
    internal bool TryTransition(ResourceStates desiredState, out D3D12ResourceTransition transition) =>
        stateTracker.TryTransition(NativePointer, desiredState, out transition);

    /// <summary>
    ///     Validates a byte range against the buffer size.
    /// </summary>
    /// <param name="offset">The byte offset.</param>
    /// <param name="length">The byte length.</param>
    private void ValidateRange(ulong offset, ulong length) {
        if (offset > SizeInBytes || length > SizeInBytes - offset)
            throw new ArgumentOutOfRangeException(nameof(length), "The requested byte range exceeds the resource.");
    }
}

/// <summary>
///     Adds the first Direct3D 12 resource and submission operations to the bootstrap wrappers.
/// </summary>
public static unsafe class SilkD3D12RuntimeExtensions {
    /// <summary>
    ///     Gets the native device-removal status.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <returns>The device-removal HRESULT, or a success code while the device is healthy.</returns>
    public static int GetDeviceRemovedReason(this SilkD3D12Device device) {
        device.GuardNotNull();
        return device.NativeDevice.GetDeviceRemovedReason();
    }

    /// <summary>
    ///     Throws the native device-removal error when the device is no longer healthy.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    public static void ThrowIfDeviceRemoved(this SilkD3D12Device device) =>
        SilkMarshal.ThrowHResult(device.GetDeviceRemovedReason());

    /// <summary>
    ///     Creates the shared renderer root signature covering the existing CBV, SRV, UAV, and sampler registers.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <returns>The shared renderer root signature.</returns>
    public static SilkD3D12RootSignature CreateDefaultRootSignature(this SilkD3D12Device device) {
        device.GuardNotNull();

        var resourceDescriptions = D3D12DefaultRootSignatureLayout.ResourceRanges;
        var nativeRanges = stackalloc DescriptorRange[resourceDescriptions.Count + 1];
        for (var index = 0; index < resourceDescriptions.Count; index++) {
            var range = resourceDescriptions[index];
            nativeRanges[index] = new DescriptorRange {
                RangeType = range.Type,
                NumDescriptors = range.DescriptorCount,
                BaseShaderRegister = range.BaseShaderRegister,
                OffsetInDescriptorsFromTableStart = uint.MaxValue
            };
        }

        var sampler = D3D12DefaultRootSignatureLayout.SamplerRange;
        nativeRanges[resourceDescriptions.Count] = new DescriptorRange {
            RangeType = sampler.Type,
            NumDescriptors = sampler.DescriptorCount,
            BaseShaderRegister = sampler.BaseShaderRegister,
            OffsetInDescriptorsFromTableStart = uint.MaxValue
        };

        var parameters = stackalloc RootParameter[2];
        parameters[0] = new RootParameter {
            ParameterType = RootParameterType.TypeDescriptorTable,
            ShaderVisibility = ShaderVisibility.All,
            DescriptorTable = new RootDescriptorTable {
                NumDescriptorRanges = (uint) resourceDescriptions.Count,
                PDescriptorRanges = nativeRanges
            }
        };
        parameters[1] = new RootParameter {
            ParameterType = RootParameterType.TypeDescriptorTable,
            ShaderVisibility = ShaderVisibility.All,
            DescriptorTable = new RootDescriptorTable {
                NumDescriptorRanges = 1,
                PDescriptorRanges = &nativeRanges[resourceDescriptions.Count]
            }
        };

        var description = new RootSignatureDesc {
            NumParameters = 2,
            PParameters = parameters,
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout | RootSignatureFlags.AllowStreamOutput
        };
        SilkD3DBlobPtr signature = default;
        SilkD3DBlobPtr errors = default;
        SilkMarshal.ThrowHResult(SilkD3D12DeviceFactory.Api.SerializeRootSignature(in description,
            D3DRootSignatureVersion.Version1,
            ref signature,
            ref errors));

        try {
            SilkMarshal.ThrowHResult(device.NativeDevice.CreateRootSignature(0,
                signature.Handle->GetBufferPointer(),
                signature.Handle->GetBufferSize(),
                out SilkD3D12RootSignaturePtr rootSignature));
            return new SilkD3D12RootSignature(rootSignature);
        } finally {
            errors.Dispose();
            signature.Dispose();
        }
    }

    /// <summary>
    ///     Creates a native descriptor heap with deterministic index allocation.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="type">The descriptor heap type.</param>
    /// <param name="capacity">The descriptor capacity.</param>
    /// <param name="shaderVisible">Whether shaders can access the heap.</param>
    /// <returns>The created descriptor heap.</returns>
    public static SilkD3D12DescriptorHeap CreateDescriptorHeap(
        this SilkD3D12Device device,
        DescriptorHeapType type,
        int capacity,
        bool shaderVisible = false
    ) {
        device.GuardNotNull();
        capacity.Guard().Range(1, int.MaxValue);
        if (shaderVisible && type is not (DescriptorHeapType.CbvSrvUav or DescriptorHeapType.Sampler))
            throw new ArgumentException("Only CBV/SRV/UAV and sampler heaps can be shader visible.", nameof(type));

        var description = new DescriptorHeapDesc {
            Type = type,
            NumDescriptors = (uint) capacity,
            Flags = shaderVisible
                ? DescriptorHeapFlags.ShaderVisible
                : DescriptorHeapFlags.None
        };
        SilkMarshal.ThrowHResult(device.NativeDevice.CreateDescriptorHeap(in description,
            out SilkD3D12DescriptorHeapPtr heap));

        return new SilkD3D12DescriptorHeap(heap,
            type,
            capacity,
            device.NativeDevice.GetDescriptorHandleIncrementSize(type),
            shaderVisible);
    }

    /// <summary>
    ///     Creates one committed Direct3D 12 buffer.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="sizeInBytes">The buffer size.</param>
    /// <param name="heapType">The resource heap type.</param>
    /// <param name="flags">The resource flags.</param>
    /// <returns>The created buffer resource.</returns>
    public static SilkD3D12Resource CreateBuffer(
        this SilkD3D12Device device,
        ulong sizeInBytes,
        HeapType heapType = HeapType.Default,
        ResourceFlags flags = ResourceFlags.None
    ) {
        device.GuardNotNull();
        if (sizeInBytes == 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        if (heapType != HeapType.Default && flags != ResourceFlags.None)
            throw new ArgumentException("Upload and readback buffers do not support resource flags.", nameof(flags));

        var initialState = heapType switch {
            HeapType.Upload => ResourceStates.GenericRead,
            HeapType.Readback => ResourceStates.CopyDest,
            HeapType.Default => ResourceStates.Common,
            _ => throw new ArgumentOutOfRangeException(nameof(heapType), heapType, "Unsupported buffer heap type.")
        };
        var heapProperties = new HeapProperties {
            Type = heapType,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };
        var description = new ResourceDesc {
            Dimension = ResourceDimension.Buffer,
            Width = sizeInBytes,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatUnknown,
            SampleDesc = new SampleDesc(1, 0),
            Layout = TextureLayout.LayoutRowMajor,
            Flags = flags
        };

        SilkMarshal.ThrowHResult(device.NativeDevice.CreateCommittedResource(in heapProperties,
            HeapFlags.None,
            in description,
            initialState,
            null,
            out SilkD3D12ResourcePtr resource));
        return new SilkD3D12Resource(resource,
            description,
            sizeInBytes,
            heapType,
            initialState,
            device.ResourceStates);
    }

    /// <summary>
    ///     Creates one committed two-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="format">The texture format.</param>
    /// <param name="flags">The resource flags.</param>
    /// <param name="initialState">The initial resource state.</param>
    /// <param name="arraySize">The texture array size.</param>
    /// <param name="mipLevels">The number of mip levels.</param>
    /// <returns>The created texture resource.</returns>
    public static SilkD3D12Resource CreateTexture2D(
        this SilkD3D12Device device,
        uint width,
        uint height,
        Format format,
        ResourceFlags flags = ResourceFlags.None,
        ResourceStates initialState = ResourceStates.CopyDest,
        ushort arraySize = 1,
        ushort mipLevels = 1
    ) {
        device.GuardNotNull();
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (format == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(format));
        if (arraySize == 0) throw new ArgumentOutOfRangeException(nameof(arraySize));
        if (mipLevels == 0) throw new ArgumentOutOfRangeException(nameof(mipLevels));

        var heapProperties = new HeapProperties {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };
        var description = new ResourceDesc {
            Dimension = ResourceDimension.Texture2D,
            Width = width,
            Height = height,
            DepthOrArraySize = arraySize,
            MipLevels = mipLevels,
            Format = format,
            SampleDesc = new SampleDesc(1, 0),
            Layout = TextureLayout.LayoutUnknown,
            Flags = flags
        };

        SilkMarshal.ThrowHResult(device.NativeDevice.CreateCommittedResource(in heapProperties,
            HeapFlags.None,
            in description,
            initialState,
            null,
            out SilkD3D12ResourcePtr resource));
        return new SilkD3D12Resource(resource,
            description,
            0,
            HeapType.Default,
            initialState,
            device.ResourceStates);
    }

    /// <summary>
    ///     Creates one committed one-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="format">The texture format.</param>
    /// <param name="flags">The resource flags.</param>
    /// <param name="initialState">The initial resource state.</param>
    /// <param name="arraySize">The texture array size.</param>
    /// <param name="mipLevels">The mip-level count.</param>
    /// <returns>The created texture resource.</returns>
    public static SilkD3D12Resource CreateTexture1D(
        this SilkD3D12Device device,
        uint width,
        Format format,
        ResourceFlags flags = ResourceFlags.None,
        ResourceStates initialState = ResourceStates.CopyDest,
        ushort arraySize = 1,
        ushort mipLevels = 1
    ) {
        device.GuardNotNull();
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (format == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(format));
        if (arraySize == 0) throw new ArgumentOutOfRangeException(nameof(arraySize));
        if (mipLevels == 0) throw new ArgumentOutOfRangeException(nameof(mipLevels));

        var heapProperties = new HeapProperties {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };
        var description = new ResourceDesc {
            Dimension = ResourceDimension.Texture1D,
            Width = width,
            Height = 1,
            DepthOrArraySize = arraySize,
            MipLevels = mipLevels,
            Format = format,
            SampleDesc = new SampleDesc(1, 0),
            Layout = TextureLayout.LayoutUnknown,
            Flags = flags
        };
        SilkMarshal.ThrowHResult(device.NativeDevice.CreateCommittedResource(in heapProperties,
            HeapFlags.None,
            in description,
            initialState,
            null,
            out SilkD3D12ResourcePtr resource));
        return new SilkD3D12Resource(resource,
            description,
            0,
            HeapType.Default,
            initialState,
            device.ResourceStates);
    }

    /// <summary>
    ///     Creates one committed three-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="depth">The texture depth.</param>
    /// <param name="format">The texture format.</param>
    /// <param name="flags">The resource flags.</param>
    /// <param name="initialState">The initial resource state.</param>
    /// <param name="mipLevels">The mip-level count.</param>
    /// <returns>The created texture resource.</returns>
    public static SilkD3D12Resource CreateTexture3D(
        this SilkD3D12Device device,
        uint width,
        uint height,
        ushort depth,
        Format format,
        ResourceFlags flags = ResourceFlags.None,
        ResourceStates initialState = ResourceStates.CopyDest,
        ushort mipLevels = 1
    ) {
        device.GuardNotNull();
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth == 0) throw new ArgumentOutOfRangeException(nameof(depth));
        if (format == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(format));
        if (mipLevels == 0) throw new ArgumentOutOfRangeException(nameof(mipLevels));

        var heapProperties = new HeapProperties {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };
        var description = new ResourceDesc {
            Dimension = ResourceDimension.Texture3D,
            Width = width,
            Height = height,
            DepthOrArraySize = depth,
            MipLevels = mipLevels,
            Format = format,
            SampleDesc = new SampleDesc(1, 0),
            Layout = TextureLayout.LayoutUnknown,
            Flags = flags
        };
        SilkMarshal.ThrowHResult(device.NativeDevice.CreateCommittedResource(in heapProperties,
            HeapFlags.None,
            in description,
            initialState,
            null,
            out SilkD3D12ResourcePtr resource));
        return new SilkD3D12Resource(resource,
            description,
            0,
            HeapType.Default,
            initialState,
            device.ResourceStates);
    }

    /// <summary>
    ///     Creates one committed render-target texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="format">The texture format.</param>
    /// <returns>The created texture resource in render-target state.</returns>
    public static SilkD3D12Resource CreateRenderTargetTexture2D(
        this SilkD3D12Device device,
        uint width,
        uint height,
        Format format
    ) {
        return device.CreateTexture2D(width,
            height,
            format,
            ResourceFlags.AllowRenderTarget,
            ResourceStates.RenderTarget);
    }

    /// <summary>
    ///     Creates one committed depth/stencil texture in depth-write state.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="format">The depth/stencil format.</param>
    /// <returns>The created depth/stencil texture.</returns>
    public static SilkD3D12Resource CreateDepthStencilTexture2D(
        this SilkD3D12Device device,
        uint width,
        uint height,
        Format format
    ) {
        if (format is not (Format.FormatD16Unorm or Format.FormatD24UnormS8Uint or Format.FormatD32Float or
            Format.FormatD32FloatS8X24Uint))
            throw new ArgumentOutOfRangeException(nameof(format), format, "A depth/stencil format is required.");
        return device.CreateTexture2D(width,
            height,
            format,
            ResourceFlags.AllowDepthStencil,
            ResourceStates.DepthWrite);
    }

    /// <summary>
    ///     Creates a default render-target view for a texture in an allocated RTV descriptor.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The render-target texture.</param>
    /// <param name="descriptor">The target RTV descriptor.</param>
    public static void CreateRenderTargetView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture2D ||
            (texture.Description.Flags & ResourceFlags.AllowRenderTarget) == 0)
            throw new ArgumentException("The resource is not a render-target texture.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.Rtv)
            throw new ArgumentException("The descriptor must come from an RTV heap.", nameof(descriptor));

        device.NativeDevice.CreateRenderTargetView(texture.Handle,
            (RenderTargetViewDesc*) null,
            descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a default depth/stencil view for a depth/stencil texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The depth/stencil texture.</param>
    /// <param name="descriptor">The target DSV descriptor.</param>
    public static void CreateDepthStencilView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor
    ) => device.CreateDepthStencilView(texture, descriptor, texture.Description.Format);

    /// <summary>
    ///     Creates a typed depth/stencil view for a compatible depth/stencil texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The depth/stencil texture.</param>
    /// <param name="descriptor">The target DSV descriptor.</param>
    /// <param name="viewFormat">The typed depth/stencil view format.</param>
    public static void CreateDepthStencilView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        Format viewFormat
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture2D ||
            (texture.Description.Flags & ResourceFlags.AllowDepthStencil) == 0)
            throw new ArgumentException("The resource is not a depth/stencil texture.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.Dsv)
            throw new ArgumentException("The descriptor must come from a DSV heap.", nameof(descriptor));
        if (viewFormat is not (Format.FormatD16Unorm or Format.FormatD24UnormS8Uint or Format.FormatD32Float or
            Format.FormatD32FloatS8X24Uint))
            throw new ArgumentOutOfRangeException(nameof(viewFormat), viewFormat,
                "A typed depth/stencil view format is required.");

        var description = new DepthStencilViewDesc {
            Format = viewFormat,
            ViewDimension = DsvDimension.Texture2D,
            Flags = DsvFlags.None,
            Texture2D = new Tex2DDsv(0)
        };
        device.NativeDevice.CreateDepthStencilView(texture.Handle, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a constant-buffer view in a CBV/SRV/UAV descriptor.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="buffer">The upload or default buffer.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    /// <param name="sizeInBytes">The 256-byte-aligned view size.</param>
    public static void CreateConstantBufferView(
        this SilkD3D12Device device,
        SilkD3D12Resource buffer,
        SilkD3D12Descriptor descriptor,
        uint sizeInBytes
    ) {
        device.GuardNotNull();
        buffer.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (buffer.Description.Dimension != ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a buffer.", nameof(buffer));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));
        if (sizeInBytes == 0 || sizeInBytes % 256 != 0 || sizeInBytes > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes));

        var description = new ConstantBufferViewDesc(buffer.GpuVirtualAddress, sizeInBytes);
        device.NativeDevice.CreateConstantBufferView(in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a shader-resource view for a two-dimensional texture, texture array, or cube map.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The texture resource.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    /// <param name="cubeMap">Whether a six-slice texture should be viewed as a cube map.</param>
    public static void CreateShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        bool cubeMap = false
    ) => device.CreateShaderResourceView(texture, descriptor, texture.Description.Format, cubeMap);

    /// <summary>
    ///     Creates a typed shader-resource view for a compatible two-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The texture resource.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    /// <param name="viewFormat">The typed shader-resource view format.</param>
    /// <param name="cubeMap">Whether a six-slice texture should be viewed as a cube map.</param>
    public static void CreateShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        Format viewFormat,
        bool cubeMap = false
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture2D)
            throw new ArgumentException("The resource must be a two-dimensional texture.", nameof(texture));
        if ((texture.Description.Flags & ResourceFlags.DenyShaderResource) != 0)
            throw new ArgumentException("The texture denies shader-resource views.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));
        if (viewFormat == Format.FormatUnknown) throw new ArgumentOutOfRangeException(nameof(viewFormat));
        if (cubeMap && texture.Description.DepthOrArraySize != 6)
            throw new ArgumentException("Cube maps require exactly six array slices.", nameof(texture));

        var description = new ShaderResourceViewDesc {
            Format = viewFormat,
            Shader4ComponentMapping = 5768
        };
        if (cubeMap) {
            description.ViewDimension = SrvDimension.Texturecube;
            description.TextureCube = new TexcubeSrv(0, texture.Description.MipLevels, 0);
        } else if (texture.Description.DepthOrArraySize > 1) {
            description.ViewDimension = SrvDimension.Texture2Darray;
            description.Texture2DArray = new Tex2DArraySrv(0,
                texture.Description.MipLevels,
                0,
                texture.Description.DepthOrArraySize,
                0,
                0);
        } else {
            description.ViewDimension = SrvDimension.Texture2D;
            description.Texture2D = new Tex2DSrv(0, texture.Description.MipLevels, 0, 0);
        }
        device.NativeDevice.CreateShaderResourceView(texture.Handle, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a shader-resource view for a three-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The three-dimensional texture.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    public static void CreateTexture3DShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture3D)
            throw new ArgumentException("The resource must be a three-dimensional texture.", nameof(texture));
        if ((texture.Description.Flags & ResourceFlags.DenyShaderResource) != 0)
            throw new ArgumentException("The texture denies shader-resource views.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));

        var description = new ShaderResourceViewDesc {
            Format = texture.Description.Format,
            Shader4ComponentMapping = 5768,
            ViewDimension = SrvDimension.Texture3D,
            Texture3D = new Tex3DSrv(0, texture.Description.MipLevels, 0)
        };
        device.NativeDevice.CreateShaderResourceView(texture.Handle, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a shader-resource view for a one-dimensional texture or texture array.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The one-dimensional texture.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    public static void CreateTexture1DShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture1D)
            throw new ArgumentException("The resource must be a one-dimensional texture.", nameof(texture));
        if ((texture.Description.Flags & ResourceFlags.DenyShaderResource) != 0)
            throw new ArgumentException("The texture denies shader-resource views.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));

        var description = new ShaderResourceViewDesc {
            Format = texture.Description.Format,
            Shader4ComponentMapping = 5768
        };
        if (texture.Description.DepthOrArraySize > 1) {
            description.ViewDimension = SrvDimension.Texture1Darray;
            description.Texture1DArray = new Tex1DArraySrv {
                MostDetailedMip = 0,
                MipLevels = texture.Description.MipLevels,
                FirstArraySlice = 0,
                ArraySize = texture.Description.DepthOrArraySize,
                ResourceMinLODClamp = 0
            };
        } else {
            description.ViewDimension = SrvDimension.Texture1D;
            description.Texture1D = new Tex1DSrv {
                MostDetailedMip = 0,
                MipLevels = texture.Description.MipLevels,
                ResourceMinLODClamp = 0
            };
        }
        device.NativeDevice.CreateShaderResourceView(texture.Handle, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a structured shader-resource view for a buffer.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="buffer">The structured buffer resource.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    /// <param name="elementCount">The number of exposed elements.</param>
    /// <param name="strideInBytes">The size of one structured element.</param>
    public static void CreateStructuredBufferShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Resource buffer,
        SilkD3D12Descriptor descriptor,
        uint elementCount,
        uint strideInBytes
    ) {
        device.GuardNotNull();
        buffer.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (buffer.Description.Dimension != ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a buffer.", nameof(buffer));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));
        if (elementCount == 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
        if (strideInBytes == 0) throw new ArgumentOutOfRangeException(nameof(strideInBytes));
        if (checked((ulong) elementCount * strideInBytes) > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(elementCount));
        var description = new ShaderResourceViewDesc {
            Format = Format.FormatUnknown,
            Shader4ComponentMapping = 5768,
            ViewDimension = SrvDimension.Buffer,
            Buffer = new BufferSrv {
                FirstElement = 0,
                NumElements = elementCount,
                StructureByteStride = strideInBytes,
                Flags = BufferSrvFlags.None
            }
        };
        device.NativeDevice.CreateShaderResourceView(buffer.Handle, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates an append/consume unordered-access view over a structured buffer and separate counter.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="buffer">The structured data buffer.</param>
    /// <param name="counter">The separate 32-bit counter buffer.</param>
    /// <param name="descriptor">The destination UAV descriptor.</param>
    /// <param name="elementCount">The maximum element count.</param>
    /// <param name="strideInBytes">The structured element stride.</param>
    public static void CreateStructuredBufferUnorderedAccessView(
        this SilkD3D12Device device,
        SilkD3D12Resource buffer,
        SilkD3D12Resource counter,
        SilkD3D12Descriptor descriptor,
        uint elementCount,
        uint strideInBytes
    ) {
        device.GuardNotNull();
        buffer.GuardNotNull();
        counter.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        ObjectDisposedException.ThrowIf(counter.IsDisposed, counter);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (buffer.Description.Dimension != ResourceDimension.Buffer ||
            (buffer.Description.Flags & ResourceFlags.AllowUnorderedAccess) == 0)
            throw new ArgumentException("The buffer must allow unordered access.", nameof(buffer));
        if (counter.Description.Dimension != ResourceDimension.Buffer || counter.SizeInBytes < sizeof(uint))
            throw new ArgumentException("A 32-bit counter buffer is required.", nameof(counter));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));
        if (elementCount == 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
        if (strideInBytes == 0) throw new ArgumentOutOfRangeException(nameof(strideInBytes));
        if (checked((ulong) elementCount * strideInBytes) > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(elementCount));
        var description = new UnorderedAccessViewDesc {
            Format = Format.FormatUnknown,
            ViewDimension = UavDimension.Buffer,
            Buffer = new BufferUav(0,
                elementCount,
                strideInBytes,
                0,
                BufferUavFlags.None)
        };
        device.NativeDevice.CreateUnorderedAccessView(buffer.Handle,
            counter.Handle,
            in description,
            descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a valid null two-dimensional shader-resource descriptor.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    internal static void CreateNullShaderResourceView(
        this SilkD3D12Device device,
        SilkD3D12Descriptor descriptor
    ) {
        device.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));

        var description = new ShaderResourceViewDesc {
            Format = Format.FormatR8G8B8A8Unorm,
            Shader4ComponentMapping = 5768,
            ViewDimension = SrvDimension.Texture2D,
            Texture2D = new Tex2DSrv(0, 1, 0, 0)
        };
        device.NativeDevice.CreateShaderResourceView(null, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates an unordered-access view for one mip level of a two-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The unordered-access texture.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    /// <param name="mipSlice">The exposed mip level.</param>
    public static void CreateUnorderedAccessView(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        uint mipSlice = 0
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (texture.Description.Dimension != ResourceDimension.Texture2D ||
            texture.Description.DepthOrArraySize != 1)
            throw new ArgumentException("The resource must be a non-array two-dimensional texture.",
                nameof(texture));
        if ((texture.Description.Flags & ResourceFlags.AllowUnorderedAccess) == 0)
            throw new ArgumentException("The texture does not allow unordered access.", nameof(texture));
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));
        if (mipSlice >= texture.Description.MipLevels)
            throw new ArgumentOutOfRangeException(nameof(mipSlice));

        var description = new UnorderedAccessViewDesc {
            Format = texture.Description.Format,
            ViewDimension = UavDimension.Texture2D,
            Texture2D = new Tex2DUav(mipSlice, 0)
        };
        device.NativeDevice.CreateUnorderedAccessView(texture.Handle,
            null,
            in description,
            descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a valid null two-dimensional unordered-access descriptor.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="descriptor">The destination CBV/SRV/UAV descriptor.</param>
    internal static void CreateNullUnorderedAccessView(
        this SilkD3D12Device device,
        SilkD3D12Descriptor descriptor
    ) {
        device.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("The descriptor must come from a CBV/SRV/UAV heap.", nameof(descriptor));

        var description = new UnorderedAccessViewDesc {
            Format = Format.FormatR8G8B8A8Unorm,
            ViewDimension = UavDimension.Texture2D,
            Texture2D = new Tex2DUav(0, 0)
        };
        device.NativeDevice.CreateUnorderedAccessView(null, null, in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a sampler descriptor with one address mode on all axes.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="descriptor">The destination sampler descriptor.</param>
    /// <param name="filter">The texture filter.</param>
    /// <param name="addressMode">The texture address mode.</param>
    /// <param name="maxAnisotropy">The anisotropy limit.</param>
    public static void CreateSampler(
        this SilkD3D12Device device,
        SilkD3D12Descriptor descriptor,
        Filter filter = Filter.MinMagMipLinear,
        TextureAddressMode addressMode = TextureAddressMode.Wrap,
        uint maxAnisotropy = 1
    ) {
        device.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (descriptor.Type != DescriptorHeapType.Sampler)
            throw new ArgumentException("The descriptor must come from a sampler heap.", nameof(descriptor));
        if (maxAnisotropy is < 1 or > 16)
            throw new ArgumentOutOfRangeException(nameof(maxAnisotropy));

        var description = new SamplerDesc {
            Filter = (Silk.NET.Direct3D12.Filter) filter,
            AddressU = (Silk.NET.Direct3D12.TextureAddressMode) addressMode,
            AddressV = (Silk.NET.Direct3D12.TextureAddressMode) addressMode,
            AddressW = (Silk.NET.Direct3D12.TextureAddressMode) addressMode,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunc.Always,
            MaxLOD = float.MaxValue
        };
        device.NativeDevice.CreateSampler(in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Creates a sampler descriptor from the renderer's existing sampler-state contract.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="descriptor">The destination sampler descriptor.</param>
    /// <param name="state">The existing sampler-state description.</param>
    public static void CreateSampler(
        this SilkD3D12Device device,
        SilkD3D12Descriptor descriptor,
        SamplerStateDescription state
    ) {
        device.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (descriptor.Type != DescriptorHeapType.Sampler)
            throw new ArgumentException("The descriptor must come from a sampler heap.", nameof(descriptor));
        if (state.MaximumAnisotropy is < 0 or > 16)
            throw new ArgumentOutOfRangeException(nameof(state));

        var description = new SamplerDesc {
            Filter = (Silk.NET.Direct3D12.Filter) state.Filter,
            AddressU = (Silk.NET.Direct3D12.TextureAddressMode) state.AddressU,
            AddressV = (Silk.NET.Direct3D12.TextureAddressMode) state.AddressV,
            AddressW = (Silk.NET.Direct3D12.TextureAddressMode) state.AddressW,
            MipLODBias = state.MipLodBias,
            MaxAnisotropy = checked((uint) Math.Max(1, state.MaximumAnisotropy)),
            ComparisonFunc = state.ComparisonFunction == 0
                ? Silk.NET.Direct3D12.ComparisonFunc.Always
                : (Silk.NET.Direct3D12.ComparisonFunc) state.ComparisonFunction,
            MinLOD = state.MinimumLod,
            MaxLOD = state.MaximumLod
        };
        description.BorderColor[0] = state.BorderColor.X;
        description.BorderColor[1] = state.BorderColor.Y;
        description.BorderColor[2] = state.BorderColor.Z;
        description.BorderColor[3] = state.BorderColor.W;
        device.NativeDevice.CreateSampler(in description, descriptor.CpuHandle);
    }

    /// <summary>
    ///     Gets the copy footprint and allocation size for the first texture subresource.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The source texture.</param>
    /// <param name="totalBytes">The required destination-buffer size.</param>
    /// <returns>The placed copy footprint.</returns>
    public static PlacedSubresourceFootprint GetCopyableFootprint(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        out ulong totalBytes
    ) => device.GetCopyableFootprint(texture, 0, out totalBytes);

    /// <summary>
    ///     Gets the copy footprint and allocation size for one texture subresource.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The source texture.</param>
    /// <param name="subresource">The source subresource index.</param>
    /// <param name="totalBytes">The required destination-buffer size.</param>
    /// <returns>The placed copy footprint.</returns>
    internal static PlacedSubresourceFootprint GetCopyableFootprint(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        uint subresource,
        out ulong totalBytes
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        if (texture.Description.Dimension == ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a texture.", nameof(texture));
        var subresourceCount = texture.Description.Dimension == ResourceDimension.Texture3D
            ? texture.Description.MipLevels
            : checked((uint) (texture.Description.DepthOrArraySize * texture.Description.MipLevels));
        if (subresource >= subresourceCount) throw new ArgumentOutOfRangeException(nameof(subresource));

        var description = texture.Description;
        PlacedSubresourceFootprint footprint;
        ulong requiredBytes;
        device.Handle->GetCopyableFootprints(&description,
            subresource,
            1,
            0,
            &footprint,
            null,
            null,
            &requiredBytes);
        totalBytes = requiredBytes;
        return footprint;
    }

    /// <summary>
    ///     Creates and fills an upload buffer for the first subresource of a two-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The destination texture.</param>
    /// <param name="data">The tightly row-packed source bytes.</param>
    /// <param name="sourceRowPitch">The source byte count per row.</param>
    /// <param name="footprint">The placed destination footprint used for the subsequent copy.</param>
    /// <returns>The filled upload buffer. Keep it alive until the copy fence completes.</returns>
    public static SilkD3D12Resource CreateTextureUploadBuffer(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        ReadOnlySpan<byte> data,
        uint sourceRowPitch,
        out PlacedSubresourceFootprint footprint
    ) {
        var slicePitch = checked(sourceRowPitch * texture.Description.Height);
        var upload = device.CreateTextureUploadBuffer(texture,
            [new D3D12SubresourceData(data.ToArray(), sourceRowPitch, slicePitch)],
            out var footprints);
        footprint = footprints[0];
        return upload;
    }

    /// <summary>
    ///     Creates and fills one upload allocation for all subresources of a two- or three-dimensional texture.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="texture">The destination texture.</param>
    /// <param name="subresources">The source subresources in D3D12 array-major, mip-minor order.</param>
    /// <param name="footprints">The placed footprints used for subsequent copies.</param>
    /// <returns>The filled upload buffer. Keep it alive until the copy fence completes.</returns>
    internal static SilkD3D12Resource CreateTextureUploadBuffer(
        this SilkD3D12Device device,
        SilkD3D12Resource texture,
        IReadOnlyList<D3D12SubresourceData> subresources,
        out PlacedSubresourceFootprint[] footprints
    ) {
        device.GuardNotNull();
        texture.GuardNotNull();
        subresources.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        if (texture.Description.Dimension is not (ResourceDimension.Texture1D or ResourceDimension.Texture2D or
            ResourceDimension.Texture3D))
            throw new ArgumentException("The resource must be a texture.",
                nameof(texture));
        var expectedCount = texture.Description.Dimension == ResourceDimension.Texture3D
            ? texture.Description.MipLevels
            : checked(texture.Description.DepthOrArraySize * texture.Description.MipLevels);
        if (subresources.Count != expectedCount)
            throw new ArgumentException("The source must contain every texture subresource.", nameof(subresources));

        footprints = new PlacedSubresourceFootprint[expectedCount];
        var rowCounts = new uint[expectedCount];
        var rowSizes = new ulong[expectedCount];
        var description = texture.Description;
        ulong totalBytes = 0;
        fixed (PlacedSubresourceFootprint* footprintPointer = footprints)
        fixed (uint* rowCountPointer = rowCounts)
        fixed (ulong* rowSizePointer = rowSizes)
            device.Handle->GetCopyableFootprints(&description,
                0,
                checked((uint) expectedCount),
                0,
                footprintPointer,
                rowCountPointer,
                rowSizePointer,
                &totalBytes);

        if (totalBytes > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(texture), "The upload exceeds the supported managed size.");
        var paddedData = new byte[(int) totalBytes];
        for (var index = 0; index < subresources.Count; index++) {
            var source = subresources[index];
            if (source.RowPitch == 0 || source.SlicePitch == 0)
                throw new ArgumentOutOfRangeException(nameof(subresources), "Source pitches must be positive.");
            if (source.RowPitch < rowSizes[index])
                throw new ArgumentException("A source row is smaller than the native copy footprint.",
                    nameof(subresources));
            var requiredSliceBytes = checked((ulong) source.RowPitch * rowCounts[index]);
            var depth = footprints[index].Footprint.Depth;
            var requiredSourceBytes = checked((ulong) source.SlicePitch * depth);
            if (source.SlicePitch < requiredSliceBytes || (ulong) source.Data.Length != requiredSourceBytes)
                throw new ArgumentException("A source subresource does not contain all required depth slices.",
                    nameof(subresources));
            if (rowSizes[index] > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(subresources), "A source row exceeds managed limits.");

            var footprint = footprints[index];
            for (uint slice = 0; slice < depth; slice++)
            for (uint row = 0; row < rowCounts[index]; row++) {
                var sourceOffset = checked((int) (slice * source.SlicePitch + row * source.RowPitch));
                var destinationOffset = checked((int) (footprint.Offset +
                    slice * footprint.Footprint.RowPitch * rowCounts[index] +
                    row * footprint.Footprint.RowPitch));
                source.Data.Span.Slice(sourceOffset, (int) rowSizes[index])
                    .CopyTo(paddedData.AsSpan(destinationOffset, (int) rowSizes[index]));
            }
        }

        var upload = device.CreateBuffer(totalBytes, HeapType.Upload);
        try {
            upload.Write(paddedData);
            return upload;
        } catch {
            upload.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Records a whole-resource transition when the resource state changes.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="resource">The resource to transition.</param>
    /// <param name="desiredState">The desired resource state.</param>
    /// <returns><see langword="true" /> when a barrier was recorded.</returns>
    public static bool Transition(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource resource,
        ResourceStates desiredState
    ) {
        context.GuardNotNull();
        resource.GuardNotNull();
        if (!resource.TryTransition(desiredState, out var transition)) return false;

        var barrier = new ResourceBarrier {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier {
                PResource = (ID3D12Resource*) transition.Resource,
                Subresource = uint.MaxValue,
                StateBefore = transition.Before,
                StateAfter = transition.After
            }
        };
        context.CommandList.ResourceBarrier(1, in barrier);
        return true;
    }

    /// <summary>
    ///     Records a buffer copy between two committed resources.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="destination">The copy destination.</param>
    /// <param name="destinationOffset">The destination byte offset.</param>
    /// <param name="source">The copy source.</param>
    /// <param name="sourceOffset">The source byte offset.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    public static void CopyBuffer(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        ulong destinationOffset,
        SilkD3D12Resource source,
        ulong sourceOffset,
        ulong sizeInBytes
    ) {
        context.GuardNotNull();
        destination.GuardNotNull();
        source.GuardNotNull();
        if (sizeInBytes == 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        if (destinationOffset > destination.SizeInBytes || sizeInBytes > destination.SizeInBytes - destinationOffset)
            throw new ArgumentOutOfRangeException(nameof(destinationOffset));
        if (sourceOffset > source.SizeInBytes || sizeInBytes > source.SizeInBytes - sourceOffset)
            throw new ArgumentOutOfRangeException(nameof(sourceOffset));

        context.CommandList.CopyBufferRegion(destination.Handle,
            destinationOffset,
            source.Handle,
            sourceOffset,
            sizeInBytes);
    }

    /// <summary>
    ///     Copies every subresource between identically described textures.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="destination">The texture in copy-destination state.</param>
    /// <param name="source">The texture in copy-source state.</param>
    public static void CopyTexture(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        SilkD3D12Resource source
    ) {
        context.GuardNotNull();
        destination.GuardNotNull();
        source.GuardNotNull();
        ObjectDisposedException.ThrowIf(destination.IsDisposed, destination);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (destination.Description.Dimension == ResourceDimension.Buffer ||
            source.Description.Dimension == ResourceDimension.Buffer)
            throw new ArgumentException("Both resources must be textures.");
        if (destination.Description.Width != source.Description.Width ||
            destination.Description.Height != source.Description.Height ||
            destination.Description.DepthOrArraySize != source.Description.DepthOrArraySize ||
            destination.Description.MipLevels != source.Description.MipLevels ||
            destination.Description.Format != source.Description.Format)
            throw new ArgumentException("Texture descriptions must match.");
        if ((destination.State & ResourceStates.CopyDest) == 0)
            throw new InvalidOperationException("The destination texture must be in copy-destination state.");
        if ((source.State & ResourceStates.CopySource) == 0)
            throw new InvalidOperationException("The source texture must be in copy-source state.");
        context.CommandList.CopyResource(destination.Handle, source.Handle);
    }

    /// <summary>
    ///     Copies one placed upload-buffer footprint into the first texture subresource.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="destination">The destination texture.</param>
    /// <param name="source">The source upload buffer.</param>
    /// <param name="footprint">The source copy footprint.</param>
    public static void CopyBufferToTexture(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        SilkD3D12Resource source,
        in PlacedSubresourceFootprint footprint
    ) {
        context.CopyBufferToTexture(destination, source, [footprint]);
    }

    /// <summary>
    ///     Copies placed upload-buffer footprints into every texture subresource.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="destination">The destination texture.</param>
    /// <param name="source">The source upload buffer.</param>
    /// <param name="footprints">The source copy footprints.</param>
    internal static void CopyBufferToTexture(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        SilkD3D12Resource source,
        IReadOnlyList<PlacedSubresourceFootprint> footprints
    ) {
        context.GuardNotNull();
        destination.GuardNotNull();
        source.GuardNotNull();
        footprints.GuardNotNull();
        ObjectDisposedException.ThrowIf(destination.IsDisposed, destination);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (destination.Description.Dimension is not (ResourceDimension.Texture1D or ResourceDimension.Texture2D or
            ResourceDimension.Texture3D))
            throw new ArgumentException("The destination must be a texture.",
                nameof(destination));
        if (source.Description.Dimension != ResourceDimension.Buffer || source.HeapType != HeapType.Upload)
            throw new ArgumentException("The source must be an upload buffer.", nameof(source));
        if (destination.State != ResourceStates.CopyDest)
            throw new InvalidOperationException("The destination must be in copy-destination state.");
        var expectedCount = destination.Description.Dimension == ResourceDimension.Texture3D
            ? destination.Description.MipLevels
            : checked(destination.Description.DepthOrArraySize * destination.Description.MipLevels);
        if (footprints.Count != expectedCount)
            throw new ArgumentException("A copy footprint is required for every destination subresource.",
                nameof(footprints));

        for (var index = 0; index < footprints.Count; index++) {
            var destinationLocation = new TextureCopyLocation {
                PResource = destination.Handle,
                Type = TextureCopyType.SubresourceIndex,
                SubresourceIndex = checked((uint) index)
            };
            var sourceLocation = new TextureCopyLocation {
                PResource = source.Handle,
                Type = TextureCopyType.PlacedFootprint,
                PlacedFootprint = footprints[index]
            };
            context.CommandList.CopyTextureRegion(in destinationLocation, 0, 0, 0, in sourceLocation, null);
        }
    }

    /// <summary>
    ///     Records an unordered-access barrier without changing the tracked resource state.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="resource">The unordered-access resource.</param>
    public static void UavBarrier(this SilkD3D12CommandContext context, SilkD3D12Resource resource) {
        context.GuardNotNull();
        resource.GuardNotNull();
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        if ((resource.Description.Flags & ResourceFlags.AllowUnorderedAccess) == 0)
            throw new ArgumentException("The resource does not allow unordered access.", nameof(resource));

        var barrier = new ResourceBarrier {
            Type = ResourceBarrierType.Uav,
            Flags = ResourceBarrierFlags.None,
            UAV = new ResourceUavBarrier { PResource = resource.Handle }
        };
        context.CommandList.ResourceBarrier(1, in barrier);
    }

    /// <summary>
    ///     Clears a render-target view.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="texture">The render-target texture.</param>
    /// <param name="descriptor">The render-target view descriptor.</param>
    /// <param name="color">Four RGBA color components.</param>
    public static void ClearRenderTarget(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        ReadOnlySpan<float> color
    ) {
        context.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (color.Length != 4)
            throw new ArgumentException("An RGBA clear color must contain four values.", nameof(color));
        if (descriptor.Type != DescriptorHeapType.Rtv)
            throw new ArgumentException("The descriptor must come from an RTV heap.", nameof(descriptor));
        if (texture.Description.Dimension != ResourceDimension.Texture2D ||
            (texture.Description.Flags & ResourceFlags.AllowRenderTarget) == 0)
            throw new ArgumentException("The resource is not a render-target texture.", nameof(texture));
        if (texture.State != ResourceStates.RenderTarget)
            throw new InvalidOperationException("The texture must be in render-target state before clearing.");

        fixed (float* clearColor = color)
            context.CommandList.ClearRenderTargetView(descriptor.CpuHandle,
                clearColor,
                0,
                (Silk.NET.Maths.Box2D<int>*) null);
    }

    /// <summary>
    ///     Clears a depth/stencil view to the supplied depth and stencil values.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="texture">The depth/stencil texture.</param>
    /// <param name="descriptor">The depth/stencil view descriptor.</param>
    /// <param name="depth">The depth clear value in the inclusive zero-to-one range.</param>
    /// <param name="stencil">The stencil clear value.</param>
    public static void ClearDepthStencil(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource texture,
        SilkD3D12Descriptor descriptor,
        float depth = 1,
        byte stencil = 0
    ) {
        context.GuardNotNull();
        texture.GuardNotNull();
        descriptor.GuardNotNull();
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (depth is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (descriptor.Type != DescriptorHeapType.Dsv)
            throw new ArgumentException("The descriptor must come from a DSV heap.", nameof(descriptor));
        if (texture.Description.Dimension != ResourceDimension.Texture2D ||
            (texture.Description.Flags & ResourceFlags.AllowDepthStencil) == 0)
            throw new ArgumentException("The resource is not a depth/stencil texture.", nameof(texture));
        if (texture.State != ResourceStates.DepthWrite)
            throw new InvalidOperationException("The texture must be in depth-write state before clearing.");

        var flags = texture.Description.Format is Format.FormatD24UnormS8Uint or Format.FormatD32FloatS8X24Uint
            ? ClearFlags.ClearFlagDepth | ClearFlags.ClearFlagStencil
            : ClearFlags.ClearFlagDepth;
        context.CommandList.ClearDepthStencilView(descriptor.CpuHandle,
            flags,
            depth,
            stencil,
            0,
            (Silk.NET.Maths.Box2D<int>*) null);
    }

    /// <summary>
    ///     Copies the first texture subresource into a readback buffer.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="destination">The readback destination buffer.</param>
    /// <param name="source">The source texture.</param>
    /// <param name="footprint">The destination copy footprint.</param>
    public static void CopyTextureToBuffer(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        SilkD3D12Resource source,
        in PlacedSubresourceFootprint footprint
    ) => context.CopyTextureToBuffer(destination, source, in footprint, 0);

    /// <summary>
    ///     Copies one texture subresource into a readback buffer.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="destination">The readback destination buffer.</param>
    /// <param name="source">The source texture.</param>
    /// <param name="footprint">The destination copy footprint.</param>
    /// <param name="sourceSubresource">The source subresource index.</param>
    internal static void CopyTextureToBuffer(
        this SilkD3D12CommandContext context,
        SilkD3D12Resource destination,
        SilkD3D12Resource source,
        in PlacedSubresourceFootprint footprint,
        uint sourceSubresource
    ) {
        context.GuardNotNull();
        destination.GuardNotNull();
        source.GuardNotNull();
        ObjectDisposedException.ThrowIf(destination.IsDisposed, destination);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (destination.Description.Dimension != ResourceDimension.Buffer || destination.HeapType != HeapType.Readback)
            throw new ArgumentException("The destination must be a readback buffer.", nameof(destination));
        if (source.Description.Dimension == ResourceDimension.Buffer)
            throw new ArgumentException("The source must be a texture.", nameof(source));
        var subresourceCount = source.Description.Dimension == ResourceDimension.Texture3D
            ? source.Description.MipLevels
            : checked((uint) (source.Description.DepthOrArraySize * source.Description.MipLevels));
        if (sourceSubresource >= subresourceCount)
            throw new ArgumentOutOfRangeException(nameof(sourceSubresource));
        if (destination.State != ResourceStates.CopyDest)
            throw new InvalidOperationException("The destination must be in copy-destination state.");
        if (source.State != ResourceStates.CopySource)
            throw new InvalidOperationException("The source must be in copy-source state.");

        var destinationLocation = new TextureCopyLocation {
            PResource = destination.Handle,
            Type = TextureCopyType.PlacedFootprint,
            PlacedFootprint = footprint
        };
        var sourceLocation = new TextureCopyLocation {
            PResource = source.Handle,
            Type = TextureCopyType.SubresourceIndex,
            SubresourceIndex = sourceSubresource
        };
        context.CommandList.CopyTextureRegion(in destinationLocation, 0, 0, 0, in sourceLocation, null);
    }

    /// <summary>
    ///     Executes one closed graphics command list.
    /// </summary>
    /// <param name="queue">The command queue.</param>
    /// <param name="context">The closed command context.</param>
    public static void Execute(this SilkD3D12CommandQueue queue, SilkD3D12CommandContext context) {
        queue.GuardNotNull();
        context.GuardNotNull();
        var commandList = (ID3D12CommandList*) context.CommandList.Handle;
        queue.NativeQueue.ExecuteCommandLists(1, &commandList);
    }

    /// <summary>
    ///     Waits until a fence reaches the requested value.
    /// </summary>
    /// <param name="fence">The fence to wait for.</param>
    /// <param name="value">The requested completed value.</param>
    /// <param name="timeout">The maximum wait duration.</param>
    /// <exception cref="TimeoutException">The fence did not complete before the timeout.</exception>
    public static void Wait(this SilkD3D12Fence fence, ulong value, TimeSpan timeout) {
        fence.GuardNotNull();
        if (fence.CompletedValue >= value) return;

        using var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        SilkMarshal.ThrowHResult(fence.NativeFence.SetEventOnCompletion(value,
            waitHandle.SafeWaitHandle.DangerousGetHandle().ToPointer()));
        if (!waitHandle.WaitOne(timeout))
            throw new TimeoutException($"The Direct3D 12 fence did not reach {value} within {timeout}.");
    }
}
