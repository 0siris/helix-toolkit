using HelixToolkit.SharpDX.Core.Utilities;

namespace SilkCore.Tests.Utilities;

/// <summary>Verifies that resource disposal respects pool entry identity and transferred ownership.</summary>
public sealed class ReferenceCountedPoolTests {
    /// <summary>Ensures a detached resource cannot remove a newer resource stored under its former key.</summary>
    /// <param name="autoDispose">Whether the pool automatically disposes unreferenced entries.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void DisposingDetachedEntryPreservesReplacement(bool autoDispose) {
        using var pool = new ResourcePool(autoDispose);
        Resource? original = null;
        Resource? replacement = null;
        try {
            var created = pool.TryCreateOrGet("shared", 0, out original);
            Assert.True(created);
            Assert.NotNull(original);
            var detached = pool.TryDetach("shared", out var transferred);
            Assert.True(detached);
            Assert.Same(original, transferred);
            Assert.NotNull(transferred);
            var replaced = pool.TryCreateOrGet("shared", 0, out replacement);
            Assert.True(replaced);
            Assert.NotNull(replacement);
            Assert.NotSame(original, replacement);

            original.Dispose();
            transferred.Dispose();

            Assert.True(original.IsDisposed);
            Assert.Equal(1, pool.DictionaryCount);
            var found = pool.TryGet("shared", out var retained);
            Assert.True(found);
            Assert.Same(replacement, retained);
            Assert.NotNull(retained);
            retained.Dispose();
        } finally {
            original?.ForceDispose();
            replacement?.ForceDispose();
        }
    }

    /// <summary>Ensures disposing the current resource still removes its own cache entry.</summary>
    /// <param name="autoDispose">Whether the pool automatically disposes unreferenced entries.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void DisposingCurrentEntryEvictsIt(bool autoDispose) {
        using var pool = new ResourcePool(autoDispose);
        Resource? resource = null;
        try {
            var created = pool.TryCreateOrGet("current", 0, out resource);
            Assert.True(created);
            Assert.NotNull(resource);

            resource.ForceDispose();

            Assert.True(resource.IsDisposed);
            Assert.Equal(0, pool.DictionaryCount);
            var found = pool.TryGet("current", out _);
            Assert.False(found);
        } finally {
            resource?.ForceDispose();
        }
    }

    /// <summary>Ensures automatic return-to-pool disposal remains safe when it reenters the pool's lock.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ReleasingLastExternalReferenceDisposesAutomaticEntry() {
        using var pool = new ResourcePool(autoDispose: true);
        Resource? resource = null;
        try {
            var created = pool.TryCreateOrGet("automatic", 0, out resource);
            Assert.True(created);
            Assert.NotNull(resource);

            resource.Dispose();

            Assert.True(resource.IsDisposed);
            Assert.Equal(0, pool.DictionaryCount);
        } finally {
            resource?.ForceDispose();
        }
    }

    /// <summary>Provides independently reference-counted resources without native devices or windows.</summary>
    private sealed class ResourcePool : ReferenceCountedDictionaryPool<string, Resource, int> {
        /// <summary>Creates a test pool with the requested disposal policy.</summary>
        /// <param name="autoDispose">Whether unused entries are automatically disposed.</param>
        public ResourcePool(bool autoDispose) : base(autoDispose) { }

        /// <summary>Allows creation of each requested test resource.</summary>
        /// <param name="key">The requested resource key.</param>
        /// <param name="argument">The unused factory argument.</param>
        /// <returns><see langword="true"/>.</returns>
        protected override bool CanCreate(ref string key, ref int argument) => true;

        /// <summary>Creates a distinct test resource for a cache miss.</summary>
        /// <param name="key">The requested resource key.</param>
        /// <param name="argument">The unused factory argument.</param>
        /// <returns>The new resource.</returns>
        protected override Resource OnCreate(ref string key, ref int argument) => new();
    }

    /// <summary>Exercises the actual toolkit reference counter without additional resources.</summary>
    private sealed class Resource : DisposeObject { }
}
