using System.Reflection;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;

namespace SilkCore.Tests;

/// <summary>Checks that closed scene cores return descriptor capacity while live cores keep their resources.</summary>
public sealed class D3D12DisposedCoreCacheTests {
    /// <summary>Repeated scene replacement reuses a bounded real allocator instead of exhausting its contiguous ranges.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ClosedSceneCoresReturnDescriptorRangesTest() {
        var allocator = new D3D12DescriptorIndexAllocator(48);
        using var live = new MeshRenderCore();
        using var liveLease = new DescriptorLease(allocator, 24);
        var cache = new Dictionary<MeshRenderCore, DescriptorLease> { [live] = liveLease };
        for (var iteration = 0; iteration < 10; iteration++) {
            var closed = new MeshRenderCore();
            var released = new DescriptorLease(allocator, 24);
            cache.Add(closed, released);
            closed.Dispose();
            Release(cache);
            Assert.Equal(24, allocator.Count);
            Assert.True(released.IsDisposed);
            Assert.False(liveLease.IsDisposed);
            Assert.Single(cache);
            Assert.Same(liveLease, cache[live]);
        }
        live.Dispose();
        Release(cache);
        Assert.Empty(cache);
        Assert.Equal(0, allocator.Count);
    }

    /// <summary>
    ///     Calls the actual renderer cache release path without requiring a graphics adapter.
    /// </summary>
    /// <param name="cache">The cache owning real descriptor-index leases.</param>
    private static void Release(Dictionary<MeshRenderCore, DescriptorLease> cache) {
        var method = typeof(SilkD3D12SceneRenderer).GetMethod("ReleaseDisposedResources", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var operation = method.MakeGenericMethod(typeof(MeshRenderCore), typeof(DescriptorLease));
        operation.Invoke(null, [cache]);
    }

    /// <summary>Owns one real contiguous allocator range through the resource disposal contract.</summary>
    private sealed class DescriptorLease : IDisposable {
        /// The real allocator receiving returned descriptor indices.
        private readonly D3D12DescriptorIndexAllocator allocator;
        /// The first allocated index.
        private readonly int start;
        /// The allocated contiguous descriptor count.
        private readonly int count;
        /// Gets whether the resource has returned its descriptor range.
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Allocates an actual contiguous range in the bounded allocator.
        /// </summary>
        /// <param name="allocator">The bounded runtime allocator.</param>
        /// <param name="count">The resource's descriptor count.</param>
        public DescriptorLease(D3D12DescriptorIndexAllocator allocator, int count) {
            this.allocator = allocator;
            this.count = count;
            start = allocator.AllocateRange(count);
        }

        /// <summary>Returns each descriptor index exactly once.</summary>
        public void Dispose() {
            if (IsDisposed) return;
            IsDisposed = true;
            for (var index = start; index < start + count; index++) allocator.Release(index);
        }
    }
}
