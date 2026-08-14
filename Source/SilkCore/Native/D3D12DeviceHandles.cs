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

namespace HelixToolkit.SharpDX.Core.Native;

public sealed unsafe class SilkD3D12Device : IDisposable {
    private SilkD3D12DevicePtr nativeDevice;

    internal SilkD3D12Device(SilkD3D12DevicePtr nativeDevice, SilkFeatureLevel featureLevel) {
        if (nativeDevice.Handle == null) throw new ArgumentNullException(nameof(nativeDevice));

        this.nativeDevice = nativeDevice;
        FeatureLevel = featureLevel;
    }

    public nint NativePointer => (nint)nativeDevice.Handle;

    internal ID3D12Device* Handle => nativeDevice.Handle;

    internal ref SilkD3D12DevicePtr NativeDevice => ref nativeDevice;

    public SilkFeatureLevel FeatureLevel { get; }

    public bool IsDisposed { get; private set; }

    public SilkD3D12CommandQueue CreateCommandQueue(
        CommandListType type = CommandListType.Direct
    ) {
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
        SilkMarshal.ThrowHResult(nativeDevice.CreateFence<ID3D12Fence>(initialValue, FenceFlags.None, out var fence));
        return new SilkD3D12Fence(fence, initialValue);
    }

    public SilkD3D12RootSignature CreateEmptyRootSignature(
        RootSignatureFlags flags = RootSignatureFlags.AllowInputAssemblerInputLayout
    ) {
        var desc = new RootSignatureDesc {
            NumParameters = 0,
            PParameters = null,
            NumStaticSamplers = 0,
            PStaticSamplers = null,
            Flags = flags
        };

        SilkD3DBlobPtr signature = default;
        SilkD3DBlobPtr errors = default;
        SilkMarshal.ThrowHResult(SilkD3D12DeviceFactory.Api.SerializeRootSignature<ID3D10Blob, ID3D10Blob>(in desc,
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

    internal ID3D12CommandQueue* Handle => nativeQueue.Handle;

    internal ref SilkD3D12CommandQueuePtr NativeQueue => ref nativeQueue;

    public bool IsDisposed { get; private set; }

    public ulong Signal(SilkD3D12Fence fence) {
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

    internal ID3D12RootSignature* Handle => nativeRootSignature.Handle;

    internal ref SilkD3D12RootSignaturePtr NativeRootSignature => ref nativeRootSignature;

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

    internal ref SilkD3D12CommandAllocatorPtr CommandAllocator => ref commandAllocator;

    internal ref SilkD3D12CommandListPtr CommandList => ref commandList;

    public bool IsDisposed { get; private set; }

    public void Reset() {
        SilkMarshal.ThrowHResult(commandAllocator.Reset());
        SilkMarshal.ThrowHResult(commandList.Reset(commandAllocator, default(SilkD3D12PipelineStatePtr)));
    }

    public void Close() {
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

    internal ID3D12Fence* Handle => nativeFence.Handle;

    internal ref SilkD3D12FencePtr NativeFence => ref nativeFence;

    public ulong CurrentValue => currentValue;

    public ulong CompletedValue => nativeFence.GetCompletedValue();

    public bool IsDisposed { get; private set; }

    internal ulong NextValue() => ++currentValue;

    public void Dispose() {
        if (IsDisposed) return;

        nativeFence.Dispose();
        IsDisposed = true;
    }
}
