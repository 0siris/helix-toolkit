using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises the actual descriptor allocator at page boundaries without creating any window.
public sealed class D3D12EffectBindingPoolTests {
    /// Verifies lazy pages, the exact root-layout capacity, released-range reuse and complete table disposal.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpPagesRespectNativeCapacitiesAndReuseReturnedTables() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var ordinaryResources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var ordinarySamplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var ordinary = new SilkD3D12GraphicsBindings(ordinaryResources, ordinarySamplers);
        using var pool = new D3D12EffectBindingPool(device);
        var slots = new List<SilkD3D12GraphicsBindings>();
        try {
            Assert.Equal(0, pool.PageCount);
            for (var index = 0; index < 138; index++) slots.Add(pool.Allocate());
            Assert.Equal(1, pool.PageCount);
            Assert.Equal(138 * 118, pool.ResourceDescriptorCount);
            Assert.Equal(138 * 10, pool.SamplerDescriptorCount);
            var previous = slots[0].ResourceTableStart.GpuHandle.Ptr;
            slots[0].Dispose();
            slots[0] = pool.Allocate();
            var replacement = slots[0].ResourceTableStart.GpuHandle.Ptr;
            Assert.Equal(previous, replacement);
            Assert.Equal(1, pool.PageCount);
            slots.Add(pool.Allocate());
            Assert.Equal(2, pool.PageCount);
            Assert.Equal(139 * 118, pool.ResourceDescriptorCount);
            Assert.Equal(139 * 10, pool.SamplerDescriptorCount);
            Assert.Equal(118, ordinaryResources.Count);
            Assert.Equal(10, ordinarySamplers.Count);
            var tableHandles = slots.Select(slot => slot.ResourceTableStart.GpuHandle.Ptr).ToArray();
            var distinctHandles = tableHandles.Distinct().Count();
            Assert.Equal(slots.Count, distinctHandles);
        } finally {
            foreach (var slot in slots) slot.Dispose();
        }
        Assert.Equal(0, pool.ResourceDescriptorCount);
        Assert.Equal(0, pool.SamplerDescriptorCount);
        pool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pool.Allocate());
        device.ThrowIfDeviceRemoved();
    }
}
