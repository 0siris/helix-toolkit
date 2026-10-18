/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.UWP;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
///     Shares reference-counted geometry buffers by buffer type and geometry identifier.
/// </summary>
public sealed class GeometryBufferManager : IGeometryBufferManager {
    /// The structured logger used by the original registry diagnostics.
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    /// The original two-key buffer registry.
    private readonly DoubleKeyDictionary<Type, Guid, IDisposeObject> bufferDictionary = new();
    /// The effects manager assigned to new buffers.
    private readonly IEffectsManager manager;
    /// Whether the registry has been disposed.
    private bool disposedValue;

    /// <summary>
    ///     Creates the geometry buffer registry.
    /// </summary>
    /// <param name="manager">The effects manager assigned to new buffers.</param>
    public GeometryBufferManager(IEffectsManager manager) => this.manager = manager;

    /// Gets the number of shared buffers.
    public int Count => bufferDictionary.Count();

    /// <summary>
    ///     Registers a model's reference to the original shared geometry buffer.
    /// </summary>
    /// <typeparam name="T">The geometry buffer type.</typeparam>
    /// <param name="modelGuid">The model identifier.</param>
    /// <param name="geometry">The geometry, or <see langword="null"/> for an empty buffer.</param>
    /// <returns>The shared buffer, or the empty buffer for an absent geometry or model identifier.</returns>
    public IGeometryBufferModel Register<T>(Guid modelGuid, Geometry3D? geometry)
        where T : class, IGeometryBufferModel, IDisposeObject, new() {
        if (geometry is null || modelGuid == Guid.Empty) return EmptyGeometryBufferModel.Empty;
        lock (bufferDictionary) {
            IGeometryBufferModel container;
            if (bufferDictionary.TryGetValue(typeof(T), geometry.Guid, out var obj)) {
                Logger.Verbose("Existing buffer found, GeometryGUID = {GeometryGuid}", geometry.Guid);
                container = (IGeometryBufferModel)obj;
                obj.IncRef();
            } else {
                Logger.Verbose("Buffer not found, create new buffer. GeometryGUID = {GeometryGuid}", geometry.Guid);
                container = new T();
                var id = geometry.Guid;
                obj = (IDisposeObject)container;
                obj.Disposed += (s, e) => {
                    Logger.Verbose("Disposing Geometry Buffer. GeometryGUID = {GeometryGuid}", id);
                    lock (bufferDictionary) bufferDictionary.Remove(typeof(T), id);
                };
                container.EffectsManager = manager;
                container.Geometry = geometry;
                bufferDictionary.Add(typeof(T), geometry.Guid, obj);
            }
            return container;
        }
    }

    /// <summary>
    ///     Releases every shared buffer and clears the original registry.
    /// </summary>
    public void Dispose() {
        if (disposedValue) return;
        lock (bufferDictionary) {
            foreach (var buffer in bufferDictionary.Values.ToArray()) buffer.ForceDispose();
            bufferDictionary.Clear();
        }
        disposedValue = true;
    }
}
