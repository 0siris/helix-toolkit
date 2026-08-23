/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using SilkD3D12CommandAllocatorPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12CommandAllocator>;
using SilkD3D12CommandListPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12GraphicsCommandList>;
using SilkD3D12CommandQueuePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12CommandQueue>;
using SilkD3D12DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Device>;
using SilkD3D12FencePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Fence>;
using SilkD3D12PipelineStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12PipelineState>;
using SilkD3D12RootSignaturePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12RootSignature>;
using SilkD3DBlobPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Core.Native.ID3D10Blob>;
using D3DPrimitiveTopology = Silk.NET.Core.Native.D3DPrimitiveTopology;

namespace HelixToolkit.SharpDX.Core.Native;

public sealed unsafe class SilkD3D12Device : IDisposable {
    private SilkD3D12DevicePtr nativeDevice;

    /// <summary>
    ///     Gets the shared resource-state tracker for resources created by this device.
    /// </summary>
    internal D3D12ResourceStateTracker ResourceStates { get; } = new();

    internal SilkD3D12Device(SilkD3D12DevicePtr nativeDevice, SilkFeatureLevel featureLevel) {
        if (nativeDevice.Handle == null) throw new ArgumentNullException(nameof(nativeDevice));

        this.nativeDevice = nativeDevice;
        FeatureLevel = featureLevel;
    }

    public nint NativePointer => (nint)nativeDevice.Handle;

    internal ID3D12Device* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeDevice.Handle;
        }
    }

    internal ref SilkD3D12DevicePtr NativeDevice {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref nativeDevice;
        }
    }

    public SilkFeatureLevel FeatureLevel { get; }

    public bool IsDisposed { get; private set; }

    public SilkD3D12CommandQueue CreateCommandQueue(
        CommandListType type = CommandListType.Direct
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var desc = new CommandQueueDesc {
            Type = type,
            Priority = (int)CommandQueuePriority.Normal,
            Flags = CommandQueueFlags.None,
            NodeMask = 0
        };

        SilkMarshal.ThrowHResult(nativeDevice.CreateCommandQueue<ID3D12CommandQueue>(in desc, out var queue));
        return new SilkD3D12CommandQueue(queue);
    }

    public SilkD3D12CommandContext CreateCommandContext(
        CommandListType type = CommandListType.Direct
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        SilkMarshal.ThrowHResult(nativeDevice.CreateCommandAllocator<ID3D12CommandAllocator>(type, out var allocator));

        SilkMarshal.ThrowHResult(nativeDevice.CreateCommandList<ID3D12CommandAllocator,
                                     ID3D12PipelineState,
                                     ID3D12GraphicsCommandList>(0,
                                                                type,
                                                                allocator,
                                                                default(SilkD3D12PipelineStatePtr),
                                                                out var commandList));

        SilkMarshal.ThrowHResult(commandList.Close());
        return new SilkD3D12CommandContext(allocator, commandList);
    }

    public SilkD3D12Fence CreateFence(ulong initialValue = 0) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        SilkMarshal.ThrowHResult(nativeDevice.CreateFence<ID3D12Fence>(initialValue, FenceFlags.None, out var fence));
        return new SilkD3D12Fence(fence, initialValue);
    }

    public SilkD3D12RootSignature CreateEmptyRootSignature(
        RootSignatureFlags flags = RootSignatureFlags.AllowInputAssemblerInputLayout
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var desc = new RootSignatureDesc {
            NumParameters = 0,
            PParameters = null,
            NumStaticSamplers = 0,
            PStaticSamplers = null,
            Flags = flags
        };

        SilkD3DBlobPtr signature = default;
        SilkD3DBlobPtr errors = default;
        SilkMarshal.ThrowHResult(SilkD3D12DeviceFactory.Api.SerializeRootSignature(in desc,
                                     D3DRootSignatureVersion.Version1,
                                     ref signature,
                                     ref errors));

        try {
            SilkMarshal.ThrowHResult(nativeDevice.CreateRootSignature<ID3D12RootSignature>(0,
                                         signature.Handle->GetBufferPointer(),
                                         signature.Handle->GetBufferSize(),
                                         out var rootSignature));
            return new SilkD3D12RootSignature(rootSignature);
        } finally {
            errors.Dispose();
            signature.Dispose();
        }
    }

    public void Dispose() {
        if (IsDisposed) return;

        nativeDevice.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class SilkD3D12CommandQueue : IDisposable {
    private SilkD3D12CommandQueuePtr nativeQueue;

    internal SilkD3D12CommandQueue(SilkD3D12CommandQueuePtr nativeQueue) {
        if (nativeQueue.Handle == null) throw new ArgumentNullException(nameof(nativeQueue));

        this.nativeQueue = nativeQueue;
    }

    public nint NativePointer => (nint)nativeQueue.Handle;

    internal ID3D12CommandQueue* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeQueue.Handle;
        }
    }

    internal ref SilkD3D12CommandQueuePtr NativeQueue {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref nativeQueue;
        }
    }

    public bool IsDisposed { get; private set; }

    public ulong Signal(SilkD3D12Fence fence) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        fence.AssertArgumentNotNull();

        var value = fence.NextValue();
        SilkMarshal.ThrowHResult(nativeQueue.Signal(fence.NativeFence, value));
        return value;
    }

    public void Dispose() {
        if (IsDisposed) return;

        nativeQueue.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class SilkD3D12RootSignature : IDisposable {
    private SilkD3D12RootSignaturePtr nativeRootSignature;

    internal SilkD3D12RootSignature(SilkD3D12RootSignaturePtr nativeRootSignature) {
        if (nativeRootSignature.Handle == null) throw new ArgumentNullException(nameof(nativeRootSignature));

        this.nativeRootSignature = nativeRootSignature;
    }

    public nint NativePointer => (nint)nativeRootSignature.Handle;

    internal ID3D12RootSignature* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeRootSignature.Handle;
        }
    }

    internal ref SilkD3D12RootSignaturePtr NativeRootSignature {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref nativeRootSignature;
        }
    }

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeRootSignature.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class SilkD3D12CommandContext : IDisposable {
    private SilkD3D12CommandAllocatorPtr commandAllocator;
    private SilkD3D12CommandListPtr commandList;
    private bool graphicsPipelineBound;
    private bool computePipelineBound;

    internal SilkD3D12CommandContext(
        SilkD3D12CommandAllocatorPtr commandAllocator,
        SilkD3D12CommandListPtr commandList
    ) {
        if (commandAllocator.Handle == null) throw new ArgumentNullException(nameof(commandAllocator));
        if (commandList.Handle == null) throw new ArgumentNullException(nameof(commandList));

        this.commandAllocator = commandAllocator;
        this.commandList = commandList;
    }

    public nint AllocatorPointer => (nint)commandAllocator.Handle;

    public nint CommandListPointer => (nint)commandList.Handle;

    internal ref SilkD3D12CommandAllocatorPtr CommandAllocator {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref commandAllocator;
        }
    }

    internal ref SilkD3D12CommandListPtr CommandList {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref commandList;
        }
    }

    public bool IsDisposed { get; private set; }

    public void Reset() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        SilkMarshal.ThrowHResult(commandAllocator.Reset());
        SilkMarshal.ThrowHResult(commandList.Reset(commandAllocator, default(SilkD3D12PipelineStatePtr)));
        graphicsPipelineBound = false;
        computePipelineBound = false;
    }

    /// <summary>
    ///     Binds a graphics root signature and pipeline state.
    /// </summary>
    /// <param name="rootSignature">The compatible graphics root signature.</param>
    /// <param name="pipelineState">The graphics pipeline state.</param>
    public void SetGraphicsPipeline(
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        rootSignature.AssertArgumentNotNull();
        pipelineState.AssertArgumentNotNull();
        commandList.SetGraphicsRootSignature(rootSignature.Handle);
        commandList.SetPipelineState(pipelineState.Handle);
        graphicsPipelineBound = true;
    }

    /// <summary>
    ///     Binds a compute root signature and pipeline state.
    /// </summary>
    /// <param name="rootSignature">The compatible compute root signature.</param>
    /// <param name="pipelineState">The compute pipeline state.</param>
    public void SetComputePipeline(
        SilkD3D12RootSignature rootSignature,
        SilkD3D12PipelineState pipelineState
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        rootSignature.AssertArgumentNotNull();
        pipelineState.AssertArgumentNotNull();
        commandList.SetComputeRootSignature(rootSignature.Handle);
        commandList.SetPipelineState(pipelineState.Handle);
        computePipelineBound = true;
    }

    /// <summary>
    ///     Binds the shared shader-visible resource and sampler heaps.
    /// </summary>
    /// <param name="resourceHeap">The CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The sampler heap.</param>
    public void SetDescriptorHeaps(
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ValidateShaderVisibleHeap(resourceHeap, DescriptorHeapType.CbvSrvUav, nameof(resourceHeap));
        ValidateShaderVisibleHeap(samplerHeap, DescriptorHeapType.Sampler, nameof(samplerHeap));
        var heaps = stackalloc ID3D12DescriptorHeap*[2] {resourceHeap.Handle, samplerHeap.Handle};
        commandList.SetDescriptorHeaps(2, heaps);
    }

    /// <summary>
    ///     Binds the shared graphics resource and sampler descriptor tables.
    /// </summary>
    /// <param name="resourceTable">The first resource-table descriptor.</param>
    /// <param name="samplerTable">The first sampler-table descriptor.</param>
    public void SetGraphicsDescriptorTables(
        SilkD3D12Descriptor resourceTable,
        SilkD3D12Descriptor samplerTable
    ) {
        ValidateDescriptorTables(resourceTable, samplerTable);
        if (!graphicsPipelineBound)
            throw new InvalidOperationException("Bind the graphics root signature before its descriptor tables.");
        commandList.SetGraphicsRootDescriptorTable(0, resourceTable.GpuHandle);
        commandList.SetGraphicsRootDescriptorTable(1, samplerTable.GpuHandle);
    }

    /// <summary>
    ///     Binds the shared compute resource and sampler descriptor tables.
    /// </summary>
    /// <param name="resourceTable">The first resource-table descriptor.</param>
    /// <param name="samplerTable">The first sampler-table descriptor.</param>
    public void SetComputeDescriptorTables(
        SilkD3D12Descriptor resourceTable,
        SilkD3D12Descriptor samplerTable
    ) {
        ValidateDescriptorTables(resourceTable, samplerTable);
        if (!computePipelineBound)
            throw new InvalidOperationException("Bind the compute root signature before its descriptor tables.");
        commandList.SetComputeRootDescriptorTable(0, resourceTable.GpuHandle);
        commandList.SetComputeRootDescriptorTable(1, samplerTable.GpuHandle);
    }

    /// <summary>
    ///     Sets the input-assembler primitive topology.
    /// </summary>
    /// <param name="topology">The native Direct3D primitive topology value.</param>
    public void SetPrimitiveTopology(PrimitiveTopology topology) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (topology == PrimitiveTopology.Undefined)
            throw new ArgumentOutOfRangeException(nameof(topology));
        commandList.IASetPrimitiveTopology((D3DPrimitiveTopology) topology);
    }

    /// <summary>
    ///     Binds one render-target descriptor without a depth/stencil target.
    /// </summary>
    /// <param name="renderTarget">The render-target view descriptor.</param>
    public void SetRenderTarget(SilkD3D12Descriptor renderTarget) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        renderTarget.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(renderTarget.IsDisposed, renderTarget);
        if (renderTarget.Type != DescriptorHeapType.Rtv)
            throw new ArgumentException("An RTV descriptor is required.", nameof(renderTarget));
        var handle = renderTarget.CpuHandle;
        commandList.OMSetRenderTargets(1, in handle, false, null);
    }

    /// <summary>
    ///     Binds one render-target descriptor and one depth/stencil descriptor.
    /// </summary>
    /// <param name="renderTarget">The render-target view descriptor.</param>
    /// <param name="depthStencil">The depth/stencil view descriptor.</param>
    public void SetRenderTargets(SilkD3D12Descriptor renderTarget, SilkD3D12Descriptor depthStencil) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        renderTarget.AssertArgumentNotNull();
        depthStencil.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(renderTarget.IsDisposed, renderTarget);
        ObjectDisposedException.ThrowIf(depthStencil.IsDisposed, depthStencil);
        if (renderTarget.Type != DescriptorHeapType.Rtv)
            throw new ArgumentException("An RTV descriptor is required.", nameof(renderTarget));
        if (depthStencil.Type != DescriptorHeapType.Dsv)
            throw new ArgumentException("A DSV descriptor is required.", nameof(depthStencil));
        var renderTargetHandle = renderTarget.CpuHandle;
        var depthStencilHandle = depthStencil.CpuHandle;
        commandList.OMSetRenderTargets(1, in renderTargetHandle, false, in depthStencilHandle);
    }

    /// <summary>
    ///     Sets a viewport and matching scissor rectangle from the render-target dimensions.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    public void SetViewport(uint width, uint height) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(width));
        if (height > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(height));
        var viewport = new Silk.NET.Direct3D12.Viewport(0, 0, width, height, 0, 1);
        var scissor = new Silk.NET.Maths.Box2D<int>(0, 0, (int) width, (int) height);
        commandList.RSSetViewports(1, in viewport);
        commandList.RSSetScissorRects(1, in scissor);
    }

    /// <summary>
    ///     Binds one buffer to an input-assembler vertex slot.
    /// </summary>
    /// <param name="slot">The input slot.</param>
    /// <param name="buffer">The vertex buffer.</param>
    /// <param name="strideInBytes">The size of one vertex.</param>
    /// <param name="sizeInBytes">The exposed buffer range, or zero for the complete buffer.</param>
    public void SetVertexBuffer(
        uint slot,
        SilkD3D12Resource buffer,
        uint strideInBytes,
        uint sizeInBytes = 0
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        buffer.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        if (buffer.Description.Dimension != ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a buffer.", nameof(buffer));
        if (strideInBytes == 0) throw new ArgumentOutOfRangeException(nameof(strideInBytes));
        if ((buffer.State & ResourceStates.VertexAndConstantBuffer) == 0)
            throw new InvalidOperationException("The buffer must be in a vertex-buffer-compatible state.");
        var size = sizeInBytes == 0 ? checked((uint) buffer.SizeInBytes) : sizeInBytes;
        if (size > buffer.SizeInBytes) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        var view = new VertexBufferView(buffer.GpuVirtualAddress, size, strideInBytes);
        commandList.IASetVertexBuffers(slot, 1, in view);
    }

    /// <summary>
    ///     Binds a 16-bit or 32-bit input-assembler index buffer.
    /// </summary>
    /// <param name="buffer">The index buffer.</param>
    /// <param name="format">The unsigned index format.</param>
    /// <param name="sizeInBytes">The exposed buffer range, or zero for the complete buffer.</param>
    public void SetIndexBuffer(
        SilkD3D12Resource buffer,
        Silk.NET.DXGI.Format format,
        uint sizeInBytes = 0
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        buffer.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        if (buffer.Description.Dimension != ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a buffer.", nameof(buffer));
        if (format is not (Silk.NET.DXGI.Format.FormatR16Uint or Silk.NET.DXGI.Format.FormatR32Uint))
            throw new ArgumentOutOfRangeException(nameof(format));
        if ((buffer.State & ResourceStates.IndexBuffer) == 0)
            throw new InvalidOperationException("The buffer must be in an index-buffer-compatible state.");
        var size = sizeInBytes == 0 ? checked((uint) buffer.SizeInBytes) : sizeInBytes;
        if (size > buffer.SizeInBytes) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        var view = new IndexBufferView {
            BufferLocation = buffer.GpuVirtualAddress,
            SizeInBytes = size,
            Format = format
        };
        commandList.IASetIndexBuffer(in view);
    }

    /// <summary>
    ///     Binds one buffer as a stream-output target, or clears all stream-output targets.
    /// </summary>
    /// <param name="buffer">The stream-output buffer, or <see langword="null" /> to unbind.</param>
    /// <param name="sizeInBytes">The exposed output range, or zero for the complete buffer.</param>
    public void SetStreamOutputTarget(SilkD3D12Resource? buffer, ulong sizeInBytes = 0) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (buffer is null) {
            commandList.SOSetTargets(0, 0, (StreamOutputBufferView*) null);
            return;
        }
        ObjectDisposedException.ThrowIf(buffer.IsDisposed, buffer);
        if (buffer.Description.Dimension != ResourceDimension.Buffer)
            throw new ArgumentException("The resource must be a buffer.", nameof(buffer));
        if ((buffer.State & ResourceStates.StreamOut) == 0)
            throw new InvalidOperationException("The buffer must be in the stream-output state.");
        var size = sizeInBytes == 0 ? buffer.SizeInBytes : sizeInBytes;
        if (size > buffer.SizeInBytes) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        var view = new StreamOutputBufferView {
            BufferLocation = buffer.GpuVirtualAddress,
            SizeInBytes = size,
            BufferFilledSizeLocation = 0
        };
        commandList.SOSetTargets(0, 1, in view);
    }

    /// <summary>
    ///     Records a non-indexed draw.
    /// </summary>
    /// <param name="vertexCountPerInstance">The vertex count per instance.</param>
    /// <param name="instanceCount">The instance count.</param>
    /// <param name="startVertexLocation">The first vertex.</param>
    /// <param name="startInstanceLocation">The first instance.</param>
    public void DrawInstanced(
        uint vertexCountPerInstance,
        uint instanceCount = 1,
        uint startVertexLocation = 0,
        uint startInstanceLocation = 0
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (vertexCountPerInstance == 0) throw new ArgumentOutOfRangeException(nameof(vertexCountPerInstance));
        if (instanceCount == 0) throw new ArgumentOutOfRangeException(nameof(instanceCount));
        if (!graphicsPipelineBound)
            throw new InvalidOperationException("Bind a graphics pipeline before recording a draw.");
        commandList.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
    }

    /// <summary>
    ///     Records an indexed draw.
    /// </summary>
    /// <param name="indexCountPerInstance">The index count per instance.</param>
    /// <param name="instanceCount">The instance count.</param>
    /// <param name="startIndexLocation">The first index.</param>
    /// <param name="baseVertexLocation">The value added to each index.</param>
    /// <param name="startInstanceLocation">The first instance.</param>
    public void DrawIndexedInstanced(
        uint indexCountPerInstance,
        uint instanceCount = 1,
        uint startIndexLocation = 0,
        int baseVertexLocation = 0,
        uint startInstanceLocation = 0
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (indexCountPerInstance == 0) throw new ArgumentOutOfRangeException(nameof(indexCountPerInstance));
        if (instanceCount == 0) throw new ArgumentOutOfRangeException(nameof(instanceCount));
        if (!graphicsPipelineBound)
            throw new InvalidOperationException("Bind a graphics pipeline before recording a draw.");
        commandList.DrawIndexedInstanced(indexCountPerInstance,
            instanceCount,
            startIndexLocation,
            baseVertexLocation,
            startInstanceLocation);
    }

    /// <summary>
    ///     Records a compute dispatch.
    /// </summary>
    /// <param name="x">The thread-group count along X.</param>
    /// <param name="y">The thread-group count along Y.</param>
    /// <param name="z">The thread-group count along Z.</param>
    public void Dispatch(uint x, uint y, uint z) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
        if (y == 0) throw new ArgumentOutOfRangeException(nameof(y));
        if (z == 0) throw new ArgumentOutOfRangeException(nameof(z));
        if (!computePipelineBound)
            throw new InvalidOperationException("Bind a compute pipeline before recording a dispatch.");
        commandList.Dispatch(x, y, z);
    }

    /// <summary>
    ///     Validates a descriptor heap required by the shared root signature.
    /// </summary>
    /// <param name="heap">The heap to validate.</param>
    /// <param name="expectedType">The required heap type.</param>
    /// <param name="parameterName">The public parameter name.</param>
    private static void ValidateShaderVisibleHeap(
        SilkD3D12DescriptorHeap heap,
        DescriptorHeapType expectedType,
        string parameterName
    ) {
        heap.AssertArgumentNotNull(parameterName);
        if (heap.Type != expectedType || !heap.IsShaderVisible)
            throw new ArgumentException($"A shader-visible {expectedType} heap is required.", parameterName);
    }

    /// <summary>
    ///     Validates resource and sampler descriptors before binding their tables.
    /// </summary>
    /// <param name="resourceTable">The resource-table descriptor.</param>
    /// <param name="samplerTable">The sampler-table descriptor.</param>
    private void ValidateDescriptorTables(
        SilkD3D12Descriptor resourceTable,
        SilkD3D12Descriptor samplerTable
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        resourceTable.AssertArgumentNotNull();
        samplerTable.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(resourceTable.IsDisposed, resourceTable);
        ObjectDisposedException.ThrowIf(samplerTable.IsDisposed, samplerTable);
        if (resourceTable.Type != DescriptorHeapType.CbvSrvUav || resourceTable.GpuHandle.Ptr == 0)
            throw new ArgumentException("A shader-visible CBV/SRV/UAV descriptor is required.", nameof(resourceTable));
        if (samplerTable.Type != DescriptorHeapType.Sampler || samplerTable.GpuHandle.Ptr == 0)
            throw new ArgumentException("A shader-visible sampler descriptor is required.", nameof(samplerTable));
    }

    public void Close() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        SilkMarshal.ThrowHResult(commandList.Close());
    }

    public void Dispose() {
        if (IsDisposed) return;

        commandList.Dispose();
        commandAllocator.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class SilkD3D12Fence : IDisposable {
    private SilkD3D12FencePtr nativeFence;
    private ulong currentValue;

    internal SilkD3D12Fence(SilkD3D12FencePtr nativeFence, ulong initialValue) {
        if (nativeFence.Handle == null) throw new ArgumentNullException(nameof(nativeFence));

        this.nativeFence = nativeFence;
        currentValue = initialValue;
    }

    public nint NativePointer => (nint)nativeFence.Handle;

    internal ID3D12Fence* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeFence.Handle;
        }
    }

    internal ref SilkD3D12FencePtr NativeFence {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return ref nativeFence;
        }
    }

    public ulong CurrentValue => currentValue;

    public ulong CompletedValue {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeFence.GetCompletedValue();
        }
    }

    public bool IsDisposed { get; private set; }

    internal ulong NextValue() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return ++currentValue;
    }

    public void Dispose() {
        if (IsDisposed) return;

        nativeFence.Dispose();
        IsDisposed = true;
    }
}
