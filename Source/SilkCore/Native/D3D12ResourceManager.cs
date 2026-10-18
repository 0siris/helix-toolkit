using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.ComponentModel;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Shares productive Direct3D 12 geometry and texture resources for one render-host lifetime.
/// </summary>
internal sealed class SilkD3D12ResourceManager : IDisposable {
    /// <summary>
    ///     The Direct3D 12 device used for native allocation.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The shader-visible heap receiving shared texture descriptors.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap textureHeap;

    /// <summary>
    ///     Geometry resources keyed by their existing model identity.
    /// </summary>
    private readonly Dictionary<IGeometryBufferModel, GeometryEntry> geometries =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Matrix-instance resources keyed by their existing model identity.
    /// </summary>
    private readonly Dictionary<IElementsBufferModel<Matrix>, InstanceEntry> instances =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Texture resources keyed by the existing content identifier contract.
    /// </summary>
    private readonly Dictionary<Guid, SilkD3D12TextureModelResource> textures = [];

    /// Counts the live binding registrations for each shared texture.
    private readonly Dictionary<Guid, int> textureReferences = [];

    /// Holds explicitly removed textures until the preceding presentation fence completes.
    private readonly List<SilkD3D12TextureModelResource> retiredTextures = [];

    /// Replaced instance allocations awaiting a completed presentation fence.
    private readonly List<IDisposable> retiredInstances = [];

    /// <summary>
    ///     Synchronizes resource lookup, creation, and disposal.
    /// </summary>
    private readonly object syncRoot = new();

    /// <summary>
    ///     Initializes a Direct3D 12 resource manager.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="textureHeap">The shader-visible texture descriptor heap.</param>
    internal SilkD3D12ResourceManager(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap textureHeap
    ) {
        device.AsGuardNotNull();
        textureHeap.AsGuardNotNull();
        ObjectDisposedException.ThrowIf(textureHeap.IsDisposed, textureHeap);
        if (textureHeap.Type != DescriptorHeapType.CbvSrvUav || !textureHeap.IsShaderVisible)
            throw new ArgumentException("Textures require a shader-visible CBV/SRV/UAV heap.", nameof(textureHeap));

        this.device = device;
        this.textureHeap = textureHeap;
    }

    /// <summary>
    ///     Gets the number of cached geometry models.
    /// </summary>
    internal int GeometryCount {
        get {
            lock (syncRoot) return geometries.Count;
        }
    }

    /// <summary>
    ///     Gets the number of cached texture content identifiers.
    /// </summary>
    internal int TextureCount {
        get {
            lock (syncRoot) return textures.Count;
        }
    }

    /// <summary>
    ///     Gets the number of cached matrix-instance models.
    /// </summary>
    internal int InstanceCount {
        get {
            lock (syncRoot) return instances.Count;
        }
    }

    /// <summary>
    ///     Gets whether the manager and all owned resources have been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Creates, updates, or reuses the buffers for one default mesh model.
    /// </summary>
    /// <param name="source">The existing mesh buffer model.</param>
    /// <returns>The current native mesh buffers.</returns>
    internal SilkD3D12DefaultMeshBuffers GetOrCreate(DefaultMeshGeometryBufferModel source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            var entry = GetEntry(source);
            entry.Synchronize(source);
            if (entry.MeshBuffers is null) {
                entry.MeshBuffers = SilkD3D12DefaultMeshBuffers.Create(device, source);
                entry.ConsumeDirty();
            } else if (entry.ConsumeDirty())
                try {
                    entry.MeshBuffers.Update(source);
                } catch (InvalidOperationException) {
                    entry.MeshBuffers = entry.MeshBuffers.Recreate(device, source);
                }
            return entry.MeshBuffers;
        }
    }

    /// <summary>
    ///     Creates, updates, or reuses a CPU-skinned default mesh allocation.
    /// </summary>
    /// <param name="source">The existing bone-skinned mesh buffer model.</param>
    /// <param name="boneMatrices">The current bone matrices.</param>
    /// <param name="morphTargets">The existing morph-target payload.</param>
    /// <returns>The current native skinned mesh buffers.</returns>
    internal SilkD3D12DefaultMeshBuffers GetOrCreate(
        DefaultMeshGeometryBufferModel source,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore morphTargets
    ) {
        source.AsGuardNotNull();
        morphTargets.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            var entry = GetEntry(source);
            entry.Synchronize(source);
            if (entry.MeshBuffers is null)
                entry.MeshBuffers = SilkD3D12DefaultMeshBuffers.Create(device, source, boneMatrices, morphTargets);
            else
                try {
                    entry.MeshBuffers.Update(source, boneMatrices, morphTargets);
                } catch (InvalidOperationException) {
                    entry.MeshBuffers = entry.MeshBuffers.Recreate(device, source, boneMatrices, morphTargets);
                }
            entry.ConsumeDirty();
            return entry.MeshBuffers;
        }
    }

    /// <summary>
    ///     Creates, updates, or reuses the buffers for one default line model.
    /// </summary>
    /// <param name="source">The existing line buffer model.</param>
    /// <returns>The current native line buffers.</returns>
    internal SilkD3D12PointLineBuffers GetOrCreate(DefaultLineGeometryBufferModel source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            var entry = GetEntry(source);
            entry.Synchronize(source);
            if (entry.PointLineBuffers is null) {
                entry.PointLineBuffers = SilkD3D12PointLineBuffers.Create(device, source);
                entry.ConsumeDirty();
            } else if (entry.ConsumeDirty())
                try {
                    entry.PointLineBuffers.Update(source);
                } catch (InvalidOperationException) {
                    entry.PointLineBuffers = entry.PointLineBuffers.Recreate(device, source);
                }
            return entry.PointLineBuffers;
        }
    }

    /// <summary>
    ///     Creates, updates, or reuses the buffers for one default point model.
    /// </summary>
    /// <param name="source">The existing point buffer model.</param>
    /// <returns>The current native point buffers.</returns>
    internal SilkD3D12PointLineBuffers GetOrCreate(DefaultPointGeometryBufferModel source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            var entry = GetEntry(source);
            entry.Synchronize(source);
            if (entry.PointLineBuffers is null) {
                entry.PointLineBuffers = SilkD3D12PointLineBuffers.Create(device, source);
                entry.ConsumeDirty();
            } else if (entry.ConsumeDirty())
                try {
                    entry.PointLineBuffers.Update(source);
                } catch (InvalidOperationException) {
                    entry.PointLineBuffers = entry.PointLineBuffers.Recreate(device, source);
                }
            return entry.PointLineBuffers;
        }
    }

    /// <summary>
    ///     Creates, updates, or reuses the vertices for one prepared billboard model.
    /// </summary>
    /// <param name="source">The existing billboard buffer model.</param>
    /// <returns>The current native billboard buffer.</returns>
    internal SilkD3D12PointLineBuffers GetOrCreate(DefaultBillboardBufferModel source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            var entry = GetEntry(source);
            entry.Synchronize(source);
            if (entry.PointLineBuffers is null) {
                entry.PointLineBuffers = SilkD3D12PointLineBuffers.Create(device, source);
                entry.ConsumeDirty();
            } else if (entry.ConsumeDirty())
                try {
                    entry.PointLineBuffers.Update(source);
                } catch (InvalidOperationException) {
                    entry.PointLineBuffers = entry.PointLineBuffers.Recreate(device, source);
                }
            return entry.PointLineBuffers;
        }
    }

    /// <summary>
    ///     Creates, updates, or reuses one matrix-instance vertex stream.
    /// </summary>
    /// <param name="source">The existing matrix-instance model.</param>
    /// <param name="owner">The render core currently binding the stream, when available.</param>
    /// <returns>The current native stream, or <see langword="null" /> when the model is empty.</returns>
    internal SilkD3D12ElementsBuffer<Matrix>? GetOrCreate(IElementsBufferModel<Matrix> source,
        RenderCore? owner = null) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            if (!source.HasElements) {
                return null;
            }
            if (!instances.TryGetValue(source, out var entry)) {
                entry = new InstanceEntry(source);
                instances.Add(source, entry);
            }
            if (owner is not null) entry.Owners.Add(owner);
            if (entry.Buffer is null) {
                entry.Buffer = SilkD3D12ElementsBuffer<Matrix>.Create(device, source);
                entry.ConsumeDirty();
            } else if (entry.ConsumeDirty())
                try {
                    entry.Buffer.Update(source);
                } catch (InvalidOperationException) {
                    var replacement = SilkD3D12ElementsBuffer<Matrix>.Create(device, source);
                    retiredInstances.Add(entry.Buffer);
                    entry.Buffer = replacement;
                }
            return entry.Buffer;
        }
    }

    /// <summary>
    ///     Loads, uploads, or reuses one texture content identifier.
    /// </summary>
    /// <param name="context">The open command context receiving a first-use upload.</param>
    /// <param name="textureModel">The existing texture model.</param>
    /// <returns>The shared native texture resource.</returns>
    internal SilkD3D12TextureModelResource GetOrCreate(
        SilkD3D12CommandContext context,
        TextureModel textureModel
    ) {
        context.AsGuardNotNull();
        textureModel.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            if (textures.TryGetValue(textureModel.Guid, out var texture)) return texture;

            texture = SilkD3D12TextureModelResource.Create(device, context, textureHeap, textureModel);
            textures.Add(textureModel.Guid, texture);
            return texture;
        }
    }

    /// <summary>
    ///     Removes unregistered textures, disposed geometry, and released instances after the presentation fence.
    /// </summary>
    internal void ReleaseDisposedResources() {
        lock (syncRoot) {
            foreach (var texture in retiredTextures) texture.Dispose();
            retiredTextures.Clear();
            foreach (var id in textures.Keys.Where(id => !textureReferences.ContainsKey(id)).ToArray()) {
                textures[id].Dispose();
                textures.Remove(id);
            }
            foreach (var source in geometries.Keys.Where(source => source is IDisposeObject { IsDisposed: true }).ToArray())
                Remove(source);
            foreach (var source in instances.Keys.ToArray()) {
                var entry = instances[source];
                var hadOwners = entry.Owners.Count != 0;
                entry.Owners.RemoveWhere(owner => owner.IsDisposed || !owner.IsAttached
                    || !ReferenceEquals(source, owner switch {
                        GeometryRenderCore geometry => geometry.InstanceBuffer,
                        ParticleRenderCore particle => particle.InstanceBuffer,
                        _ => null
                    }));
                if (!source.Initialized || !source.HasElements || source is IDisposeObject { IsDisposed: true }
                    || hadOwners && entry.Owners.Count == 0)
                    Remove(source);
            }
            foreach (var instance in retiredInstances) instance.Dispose();
            retiredInstances.Clear();
        }
    }

    /// <summary>
    ///     Registers one binding reference to a shared texture, matching the original texture proxy ownership.
    /// </summary>
    /// <param name="context">The command context receiving first-use uploads.</param>
    /// <param name="model">The existing texture model.</param>
    /// <returns>The binding's disposable registration.</returns>
    internal TextureRegistration Register(SilkD3D12CommandContext context, TextureModel model) {
        lock (syncRoot) {
            var texture = GetOrCreate(context, model);
            textureReferences.TryGetValue(model.Guid, out var count);
            textureReferences[model.Guid] = count + 1;
            return new TextureRegistration(this, model.Guid, texture);
        }
    }

    /// <summary>
    ///     Relinquishes a binding reference without releasing resources used by recorded GPU commands.
    /// </summary>
    /// <param name="id">The shared texture identifier.</param>
    private void Unregister(Guid id) {
        lock (syncRoot) {
            if (IsDisposed || !textureReferences.TryGetValue(id, out var count)) return;
            if (count == 1) textureReferences.Remove(id);
            else textureReferences[id] = count - 1;
        }
    }

    /// Owns one reference to a native texture until its actual binding is replaced or disposed.
    internal sealed class TextureRegistration : IDisposable {
        /// The manager retaining the native allocation until a completed fence.
        private SilkD3D12ResourceManager? manager;

        /// <summary>
        ///     Initializes one texture registration.
        /// </summary>
        /// <param name="manager">The owning resource manager.</param>
        /// <param name="id">The shared texture identifier.</param>
        /// <param name="texture">The shared native texture.</param>
        internal TextureRegistration(SilkD3D12ResourceManager manager, Guid id,
            SilkD3D12TextureModelResource texture) {
            this.manager = manager;
            Id = id;
            Texture = texture;
        }

        /// Gets the shared content identifier.
        internal Guid Id { get; }

        /// Gets the registered native texture.
        internal SilkD3D12TextureModelResource Texture { get; }

        /// <summary>
        ///     Checks whether this registration belongs to the supplied manager.
        /// </summary>
        /// <param name="owner">The manager to compare.</param>
        /// <returns>Whether the registration belongs to that manager.</returns>
        internal bool BelongsTo(SilkD3D12ResourceManager owner) => ReferenceEquals(manager, owner);

        /// Relinquishes this reference; native disposal is deferred until the next completed frame fence.
        public void Dispose() {
            manager?.Unregister(Id);
            manager = null;
        }
    }

    /// <summary>
    ///     Removes and disposes one geometry-model allocation.
    /// </summary>
    /// <param name="source">The existing geometry model.</param>
    /// <returns>Whether a cached resource was removed.</returns>
    internal bool Remove(IGeometryBufferModel source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            if (!geometries.Remove(source, out var entry)) return false;
            entry.Dispose();
            return true;
        }
    }

    /// <summary>
    ///     Removes one matrix-instance allocation for release after the completed presentation fence.
    /// </summary>
    /// <param name="source">The existing matrix-instance model.</param>
    /// <returns>Whether a cached resource was removed.</returns>
    internal bool Remove(IElementsBufferModel<Matrix> source) {
        source.AsGuardNotNull();
        lock (syncRoot) {
            ThrowIfDisposed();
            if (!instances.Remove(source, out var entry)) return false;
            retiredInstances.Add(entry);
            return true;
        }
    }

    /// <summary>
    ///     Removes one unregistered texture allocation for release after the completed presentation fence.
    /// </summary>
    /// <param name="contentId">The texture content identifier.</param>
    /// <returns>Whether a cached resource was removed.</returns>
    internal bool Remove(Guid contentId) {
        lock (syncRoot) {
            ThrowIfDisposed();
            if (textureReferences.ContainsKey(contentId) || !textures.Remove(contentId, out var texture)) return false;
            retiredTextures.Add(texture);
            return true;
        }
    }

    /// <summary>
    ///     Releases every cached resource.
    /// </summary>
    public void Dispose() {
        lock (syncRoot) {
            if (IsDisposed) return;

            foreach (var geometry in geometries.Values) geometry.Dispose();
            foreach (var instance in instances.Values) instance.Dispose();
            foreach (var instance in retiredInstances) instance.Dispose();
            retiredInstances.Clear();
            foreach (var texture in textures.Values) texture.Dispose();
            foreach (var texture in retiredTextures) texture.Dispose();
            retiredTextures.Clear();
            textureReferences.Clear();
            geometries.Clear();
            instances.Clear();
            textures.Clear();
            IsDisposed = true;
        }
    }

    /// <summary>
    ///     Gets or creates a tracked geometry entry.
    /// </summary>
    /// <param name="source">The existing geometry model.</param>
    /// <returns>The tracked entry.</returns>
    private GeometryEntry GetEntry(IGeometryBufferModel source) {
        if (geometries.TryGetValue(source, out var entry)) return entry;
        entry = new GeometryEntry(source.Geometry, source.Topology);
        geometries.Add(source, entry);
        return entry;
    }

    /// <summary>
    ///     Throws when the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <summary>
    ///     Tracks one geometry model's current managed source and native allocation.
    /// </summary>
    private sealed class GeometryEntry : IDisposable {
        /// <summary>
        ///     The currently observed managed geometry.
        /// </summary>
        private Geometry3D? geometry;

        /// <summary>
        ///     The most recently observed primitive topology.
        /// </summary>
        private PrimitiveTopology topology;

        /// <summary>
        ///     Whether the native allocation needs an update.
        /// </summary>
        private volatile bool isDirty = true;

        /// <summary>
        ///     Initializes one geometry entry.
        /// </summary>
        /// <param name="geometry">The initial managed geometry.</param>
        /// <param name="topology">The initial primitive topology.</param>
        internal GeometryEntry(Geometry3D? geometry, PrimitiveTopology topology) {
            this.geometry = geometry;
            this.topology = topology;
            if (geometry is not null) geometry.PropertyChanged += OnGeometryChanged;
        }

        /// <summary>
        ///     Gets or sets the native mesh buffers.
        /// </summary>
        internal SilkD3D12DefaultMeshBuffers? MeshBuffers { get; set; }

        /// <summary>
        ///     Gets or sets the native line or point buffers.
        /// </summary>
        internal SilkD3D12PointLineBuffers? PointLineBuffers { get; set; }

        /// <summary>
        ///     Synchronizes managed geometry replacement and topology changes.
        /// </summary>
        /// <param name="source">The current geometry model.</param>
        internal void Synchronize(IGeometryBufferModel source) {
            if (!ReferenceEquals(geometry, source.Geometry)) {
                if (geometry is not null) geometry.PropertyChanged -= OnGeometryChanged;
                geometry = source.Geometry;
                if (geometry is not null) geometry.PropertyChanged += OnGeometryChanged;
                isDirty = true;
            }
            if (topology == source.Topology) return;
            topology = source.Topology;
            isDirty = true;
        }

        /// <summary>
        ///     Returns and clears the dirty flag.
        /// </summary>
        /// <returns>Whether the native allocation needs an update.</returns>
        internal bool ConsumeDirty() {
            if (!isDirty) return false;
            isDirty = false;
            return true;
        }

        /// <summary>
        ///     Releases the observed event and native resources.
        /// </summary>
        public void Dispose() {
            if (geometry is not null) geometry.PropertyChanged -= OnGeometryChanged;
            geometry = null;
            MeshBuffers?.Dispose();
            PointLineBuffers?.Dispose();
        }

        /// <summary>
        ///     Marks the native resource dirty after a managed geometry change.
        /// </summary>
        /// <param name="sender">The changed geometry.</param>
        /// <param name="eventArgs">The property-change information.</param>
        private void OnGeometryChanged(object? sender, PropertyChangedEventArgs eventArgs) => isDirty = true;
    }

    /// <summary>
    ///     Tracks one matrix-instance model and its native allocation.
    /// </summary>
    private sealed class InstanceEntry : IDisposable {
        /// The render cores currently retaining this shared instance stream.
        internal HashSet<RenderCore> Owners { get; } = [];

        /// <summary>
        ///     The observed source model.
        /// </summary>
        private readonly IElementsBufferModel<Matrix> source;

        /// <summary>
        ///     Whether the native allocation needs an update.
        /// </summary>
        private volatile bool isDirty = true;

        /// <summary>
        ///     Initializes one instance entry.
        /// </summary>
        /// <param name="source">The observed source model.</param>
        internal InstanceEntry(IElementsBufferModel<Matrix> source) {
            this.source = source;
            source.ElementChanged += OnElementsChanged;
        }

        /// <summary>
        ///     Gets or sets the native matrix stream.
        /// </summary>
        internal SilkD3D12ElementsBuffer<Matrix>? Buffer { get; set; }

        /// <summary>
        ///     Returns and clears the dirty flag.
        /// </summary>
        /// <returns>Whether the native allocation needs an update.</returns>
        internal bool ConsumeDirty() {
            if (!isDirty) return false;
            isDirty = false;
            return true;
        }

        /// <summary>
        ///     Releases the event subscription and native stream.
        /// </summary>
        public void Dispose() {
            source.ElementChanged -= OnElementsChanged;
            Owners.Clear();
            Buffer?.Dispose();
        }

        /// <summary>
        ///     Marks the native allocation dirty after the managed elements change.
        /// </summary>
        /// <param name="sender">The changed source model.</param>
        /// <param name="eventArgs">The change event.</param>
        private void OnElementsChanged(object? sender, EventArgs eventArgs) => isDirty = true;
    }
}
