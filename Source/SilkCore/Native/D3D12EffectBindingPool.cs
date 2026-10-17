using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns shader-visible heap pages for immutable, per-draw effect tables without consuming scene descriptors.
/// </summary>
/// <remarks>
///     Draw owners retain and reuse their tables only after the host fence completes. This owner never resets a
///     live descriptor allocation. All draw owners must be disposed before this pool and its native heap pages.
/// </remarks>
internal sealed class D3D12EffectBindingPool : IDisposable {
    /// The resource capacity of each independently bound shader-visible page.
    internal const int ResourcePageCapacity = 16_384;

    /// The sampler capacity of each page, within the native shader-visible sampler heap limit.
    internal const int SamplerPageCapacity = 2_048;

    /// The resource descriptors required by one complete shared-root-signature table.
    private static readonly int ResourceTableCount = SilkD3D12GraphicsBindings.ConstantBufferCount +
        SilkD3D12GraphicsBindings.ShaderResourceCount + SilkD3D12GraphicsBindings.UnorderedAccessCount;

    /// The device used to create pages lazily when the retained draw slots need more capacity.
    private readonly SilkD3D12Device _device;

    /// Heap pairs retained at the renderer's high-water draw count and reused by existing draw slots.
    private readonly List<Page> _pages = [];

    /// <summary>
    ///     Creates an empty descriptor page owner without allocating native heaps until the first effect draw.
    /// </summary>
    /// <param name="device">The renderer's native device.</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">The device is <see langword="null"/>.</exception>
    internal D3D12EffectBindingPool(SilkD3D12Device device) {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
    }

    /// Gets the number of retained shader-visible heap pairs.
    internal int PageCount => _pages.Count;

    /// Gets the number of resource descriptors still owned by effect draw tables.
    internal int ResourceDescriptorCount => _pages.Sum(page => page.Resources.Count);

    /// Gets the number of sampler descriptors still owned by effect draw tables.
    internal int SamplerDescriptorCount => _pages.Sum(page => page.Samplers.Count);

    /// Gets whether the native heap pages have been released.
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Allocates an independent full table pair, adding a page when no existing page has a complete pair free.
    /// </summary>
    /// <returns>The draw-owned tables whose binding operations select their own native heap pair.</returns>
    /// <exception cref="ObjectDisposedException">The heap page owner has been disposed.</exception>
    internal SilkD3D12GraphicsBindings Allocate() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        foreach (var page in _pages)
            if (page.Resources.Capacity - page.Resources.Count >= ResourceTableCount &&
                page.Samplers.Capacity - page.Samplers.Count >= SilkD3D12GraphicsBindings.SamplerCount)
                return new SilkD3D12GraphicsBindings(page.Resources, page.Samplers);

        var replacement = new Page(_device);
        try {
            var bindings = new SilkD3D12GraphicsBindings(replacement.Resources, replacement.Samplers);
            _pages.Add(replacement);
            return bindings;
        } catch {
            replacement.Dispose();
            throw;
        }
    }

    /// Releases the native heaps after all submitted draws and their draw-owned tables have completed cleanup.
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var page in _pages) page.Dispose();
        IsDisposed = true;
    }

    /// Owns one fixed-size pair; complete table allocations are its only descriptor users.
    private sealed class Page : IDisposable {
        /// <summary>
        ///     Creates one complete resource/sampler pair and releases partial allocation on failure.
        /// </summary>
        /// <param name="device">The native device.</param>
        internal Page(SilkD3D12Device device) {
            Resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, ResourcePageCapacity, true);
            try {
                Samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, SamplerPageCapacity, true);
            } catch {
                Resources.Dispose();
                throw;
            }
        }

        /// Gets the resource descriptor page.
        internal SilkD3D12DescriptorHeap Resources { get; }

        /// Gets the sampler descriptor page.
        internal SilkD3D12DescriptorHeap Samplers { get; }

        /// Releases both native heaps without taking ownership of draw-specific constants or table leases.
        public void Dispose() {
            Samplers.Dispose();
            Resources.Dispose();
        }
    }
}
