/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Numerics;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Describes the facts used to rank one DXGI hardware adapter.
/// </summary>
/// <param name="Index">The DXGI enumeration index.</param>
/// <param name="IsSoftware">Whether DXGI marks the adapter as software.</param>
/// <param name="DedicatedVideoMemory">The adapter's dedicated video memory.</param>
/// <param name="SupportsD3D12">Whether device creation succeeded at the requested feature level.</param>
internal readonly record struct D3D12AdapterCandidate(
    int Index,
    bool IsSoftware,
    nuint DedicatedVideoMemory,
    bool SupportsD3D12
);

/// <summary>
///     Selects the strongest supported hardware adapter using deterministic DXGI facts.
/// </summary>
internal static class D3D12AdapterSelector {
    /// <summary>
    ///     Selects the supported non-software adapter with the most dedicated video memory.
    /// </summary>
    /// <param name="candidates">The enumerated adapter candidates.</param>
    /// <returns>The selected DXGI index.</returns>
    /// <exception cref="PlatformNotSupportedException">No suitable adapter exists.</exception>
    public static int Select(IReadOnlyList<D3D12AdapterCandidate> candidates) {
        var selectedIndex = -1;
        nuint selectedMemory = 0;
        foreach (var candidate in candidates) {
            if (candidate.IsSoftware || !candidate.SupportsD3D12)
                continue;
            if (selectedIndex >= 0 && candidate.DedicatedVideoMemory <= selectedMemory)
                continue;

            selectedIndex = candidate.Index;
            selectedMemory = candidate.DedicatedVideoMemory;
        }

        return selectedIndex >= 0
            ? selectedIndex
            : throw new PlatformNotSupportedException("No Direct3D 12 hardware adapter is available.");
    }
}

/// <summary>
///     Describes one contiguous descriptor range in the shared renderer root signature.
/// </summary>
/// <param name="Type">The descriptor range type.</param>
/// <param name="BaseShaderRegister">The first shader register.</param>
/// <param name="DescriptorCount">The number of registers in the range.</param>
internal readonly record struct D3D12DescriptorRangeDescription(
    DescriptorRangeType Type,
    uint BaseShaderRegister,
    uint DescriptorCount
);

/// <summary>
///     Defines the minimal shared root-signature layout that covers all current repository shader registers.
/// </summary>
internal static class D3D12DefaultRootSignatureLayout {
    /// <summary>
    ///     Gets the CBV, SRV, and UAV ranges stored in the resource descriptor table.
    /// </summary>
    public static IReadOnlyList<D3D12DescriptorRangeDescription> ResourceRanges { get; } = [
        new(DescriptorRangeType.Cbv, 0, 10),
        new(DescriptorRangeType.Srv, 0, 103),
        new(DescriptorRangeType.Uav, 0, 5)
    ];

    /// <summary>
    ///     Gets the sampler range stored in the sampler descriptor table.
    /// </summary>
    public static D3D12DescriptorRangeDescription SamplerRange { get; } =
        new(DescriptorRangeType.Sampler, 0, 10);
}

/// <summary>
///     Provides deterministic Direct3D 12 resource-layout calculations.
/// </summary>
internal static class D3D12ResourceLayout {
    /// <summary>
    ///     Aligns a non-zero constant-buffer size to the required 256-byte boundary.
    /// </summary>
    /// <param name="sizeInBytes">The unaligned byte size.</param>
    /// <returns>The aligned byte size.</returns>
    public static ulong AlignConstantBufferSize(ulong sizeInBytes) {
        if (sizeInBytes == 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        if (sizeInBytes > ulong.MaxValue - 255) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
        return (sizeInBytes + 255) & ~255UL;
    }

    /// <summary>
    ///     Calculates the complete mip-chain length for non-zero dimensions.
    /// </summary>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="depth">The texture depth.</param>
    /// <returns>The number of mip levels.</returns>
    public static uint CalculateMipLevels(uint width, uint height = 1, uint depth = 1) {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth == 0) throw new ArgumentOutOfRangeException(nameof(depth));

        return (uint) BitOperations.Log2(Math.Max(width, Math.Max(height, depth))) + 1;
    }

    /// <summary>
    ///     Calculates the native subresource index for a mip, array slice, and plane.
    /// </summary>
    /// <param name="mipSlice">The mip slice.</param>
    /// <param name="arraySlice">The array slice.</param>
    /// <param name="planeSlice">The plane slice.</param>
    /// <param name="mipLevels">The mip-level count.</param>
    /// <param name="arraySize">The array size.</param>
    /// <returns>The native subresource index.</returns>
    public static uint CalculateSubresource(
        uint mipSlice,
        uint arraySlice,
        uint planeSlice,
        uint mipLevels,
        uint arraySize
    ) {
        if (mipLevels == 0) throw new ArgumentOutOfRangeException(nameof(mipLevels));
        if (arraySize == 0) throw new ArgumentOutOfRangeException(nameof(arraySize));
        if (mipSlice >= mipLevels) throw new ArgumentOutOfRangeException(nameof(mipSlice));
        if (arraySlice >= arraySize) throw new ArgumentOutOfRangeException(nameof(arraySlice));

        return checked(mipSlice + arraySlice * mipLevels + planeSlice * mipLevels * arraySize);
    }
}

/// <summary>
///     Allocates stable indices from one fixed-size Direct3D 12 descriptor heap.
/// </summary>
internal sealed class D3D12DescriptorIndexAllocator {
    private readonly bool[] allocated;
    private readonly SortedSet<int> freeIndices;

    /// <summary>
    ///     Initializes a descriptor index allocator with the requested capacity.
    /// </summary>
    /// <param name="capacity">The number of descriptor indices managed by the allocator.</param>
    public D3D12DescriptorIndexAllocator(int capacity) {
        capacity.AssertArgumentRange(1, int.MaxValue);

        allocated = new bool[capacity];
        freeIndices = new SortedSet<int>(Enumerable.Range(0, capacity));
    }

    /// <summary>
    ///     Gets the total number of managed indices.
    /// </summary>
    public int Capacity => allocated.Length;

    /// <summary>
    ///     Gets the number of currently allocated indices.
    /// </summary>
    public int Count {
        get {
            lock (freeIndices) return Capacity - freeIndices.Count;
        }
    }

    /// <summary>
    ///     Allocates the lowest available descriptor index.
    /// </summary>
    /// <returns>The allocated descriptor index.</returns>
    /// <exception cref="InvalidOperationException">The allocator is exhausted.</exception>
    public int Allocate() {
        lock (freeIndices) {
            if (freeIndices.Count == 0)
                throw new InvalidOperationException("The Direct3D 12 descriptor heap is exhausted.");

            var index = freeIndices.Min;
            freeIndices.Remove(index);
            allocated[index] = true;
            return index;
        }
    }

    /// <summary>
    ///     Allocates the lowest available contiguous descriptor range.
    /// </summary>
    /// <param name="count">The number of contiguous indices.</param>
    /// <returns>The first allocated index.</returns>
    /// <exception cref="InvalidOperationException">No sufficiently large contiguous range is available.</exception>
    public int AllocateRange(int count) {
        count.AssertArgumentRange(1, Capacity);
        lock (freeIndices) {
            var start = -1;
            var previous = -2;
            var length = 0;
            foreach (var index in freeIndices) {
                if (index != previous + 1) {
                    start = index;
                    length = 0;
                }
                previous = index;
                if (++length != count) continue;

                for (var allocatedIndex = start; allocatedIndex < start + count; allocatedIndex++) {
                    freeIndices.Remove(allocatedIndex);
                    allocated[allocatedIndex] = true;
                }
                return start;
            }

            throw new InvalidOperationException("The Direct3D 12 descriptor heap has no contiguous range available.");
        }
    }

    /// <summary>
    ///     Releases a previously allocated descriptor index.
    /// </summary>
    /// <param name="index">The descriptor index to release.</param>
    /// <exception cref="InvalidOperationException">The descriptor index is not allocated.</exception>
    public void Release(int index) {
        index.AssertArgumentRange(0, Capacity - 1);

        lock (freeIndices) {
            if (!allocated[index])
                throw new InvalidOperationException($"Descriptor index {index} is not allocated.");

            allocated[index] = false;
            freeIndices.Add(index);
        }
    }
}

/// <summary>
///     Describes one whole-resource Direct3D 12 state transition.
/// </summary>
/// <param name="Resource">The native resource pointer.</param>
/// <param name="Before">The state before the transition.</param>
/// <param name="After">The state after the transition.</param>
internal readonly record struct D3D12ResourceTransition(
    nint Resource,
    ResourceStates Before,
    ResourceStates After
);

/// <summary>
///     Tracks whole-resource Direct3D 12 states and suppresses redundant transitions.
/// </summary>
internal sealed class D3D12ResourceStateTracker {
    private readonly Dictionary<nint, ResourceStates> states = [];

    /// <summary>
    ///     Gets the number of tracked resources.
    /// </summary>
    public int Count {
        get {
            lock (states) return states.Count;
        }
    }

    /// <summary>
    ///     Starts tracking a native resource in its known initial state.
    /// </summary>
    /// <param name="resource">The native resource pointer.</param>
    /// <param name="initialState">The known initial state.</param>
    public void Track(nint resource, ResourceStates initialState) {
        ValidateResource(resource);
        lock (states)
            if (!states.TryAdd(resource, initialState))
                throw new InvalidOperationException("The Direct3D 12 resource is already tracked.");
    }

    /// <summary>
    ///     Stops tracking a native resource.
    /// </summary>
    /// <param name="resource">The native resource pointer.</param>
    /// <returns><see langword="true" /> when the resource was tracked.</returns>
    public bool Untrack(nint resource) {
        ValidateResource(resource);
        lock (states) return states.Remove(resource);
    }

    /// <summary>
    ///     Gets the last recorded state of a tracked resource.
    /// </summary>
    /// <param name="resource">The native resource pointer.</param>
    /// <returns>The tracked resource state.</returns>
    public ResourceStates GetState(nint resource) {
        ValidateResource(resource);
        lock (states)
            return states.TryGetValue(resource, out var state)
                ? state
                : throw new InvalidOperationException("The Direct3D 12 resource is not tracked.");
    }

    /// <summary>
    ///     Changes a tracked resource state and returns the required transition.
    /// </summary>
    /// <param name="resource">The native resource pointer.</param>
    /// <param name="desiredState">The desired resource state.</param>
    /// <param name="transition">The required transition, or the default value when no barrier is needed.</param>
    /// <returns><see langword="true" /> when the state changed.</returns>
    public bool TryTransition(
        nint resource,
        ResourceStates desiredState,
        out D3D12ResourceTransition transition
    ) {
        ValidateResource(resource);
        lock (states) {
            if (!states.TryGetValue(resource, out var currentState))
                throw new InvalidOperationException("The Direct3D 12 resource is not tracked.");

            if (currentState == desiredState) {
                transition = default;
                return false;
            }

            transition = new D3D12ResourceTransition(resource, currentState, desiredState);
            states[resource] = desiredState;
            return true;
        }
    }

    /// <summary>
    ///     Validates a native resource pointer used as a state-tracking key.
    /// </summary>
    /// <param name="resource">The native resource pointer.</param>
    private static void ValidateResource(nint resource) {
        if (resource == nint.Zero)
            throw new ArgumentException("The native resource pointer must not be zero.", nameof(resource));
    }
}

/// <summary>
///     Defers native resource disposal until the GPU fence has completed.
/// </summary>
internal sealed class D3D12DeferredReleaseQueue : IDisposable {
    private readonly Queue<(ulong FenceValue, IDisposable Resource)> pending = [];
    private ulong lastFenceValue;

    /// <summary>
    ///     Gets the number of resources waiting for release.
    /// </summary>
    public int Count => pending.Count;

    /// <summary>
    ///     Enqueues a resource for release after a fence value completes.
    /// </summary>
    /// <param name="fenceValue">The fence value that protects the resource.</param>
    /// <param name="resource">The resource to dispose.</param>
    public void Enqueue(ulong fenceValue, IDisposable resource) {
        resource.AssertArgumentNotNull();
        if (fenceValue < lastFenceValue)
            throw new ArgumentOutOfRangeException(nameof(fenceValue),
                fenceValue,
                "Fence values must be enqueued in nondecreasing order.");

        pending.Enqueue((fenceValue, resource));
        lastFenceValue = fenceValue;
    }

    /// <summary>
    ///     Releases every queued resource protected by a completed fence value.
    /// </summary>
    /// <param name="completedFenceValue">The latest completed fence value.</param>
    /// <returns>The number of disposed resources.</returns>
    public int ReleaseCompleted(ulong completedFenceValue) {
        var released = 0;
        while (pending.TryPeek(out var item) && item.FenceValue <= completedFenceValue) {
            pending.Dequeue().Resource.Dispose();
            released++;
        }

        return released;
    }

    /// <summary>
    ///     Releases every resource still owned by the queue.
    /// </summary>
    public void Dispose() {
        while (pending.TryDequeue(out var item)) item.Resource.Dispose();
    }
}

/// <summary>
///     Tracks the fence value protecting each reusable frame slot.
/// </summary>
internal sealed class D3D12FrameFenceTracker {
    private readonly ulong[] fenceValues;

    /// <summary>
    ///     Initializes a frame-fence tracker.
    /// </summary>
    /// <param name="frameCount">The number of reusable frame slots.</param>
    public D3D12FrameFenceTracker(int frameCount) {
        frameCount.AssertArgumentRange(1, int.MaxValue);
        fenceValues = new ulong[frameCount];
    }

    /// <summary>
    ///     Gets the current frame slot.
    /// </summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    ///     Records the fence protecting the current slot, advances, and returns the fence protecting the next slot.
    /// </summary>
    /// <param name="submittedFenceValue">The fence value protecting the current slot.</param>
    /// <returns>The fence value that must complete before the next slot can be reused.</returns>
    public ulong Advance(ulong submittedFenceValue) {
        fenceValues[CurrentIndex] = submittedFenceValue;
        CurrentIndex = (CurrentIndex + 1) % fenceValues.Length;
        return fenceValues[CurrentIndex];
    }
}
