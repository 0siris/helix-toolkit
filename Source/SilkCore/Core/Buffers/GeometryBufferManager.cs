/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        /// <summary>
        ///     Use to manage geometry vertex/index buffers.
        ///     Same geometry with same buffer type will share the same buffer across all models.
        /// </summary>
        public sealed class GeometryBufferManager : IDisposable, IGeometryBufferManager {
            private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

            /// <summary>
            ///     The buffer dictionary. Key1=<see cref="Geometry3D.GUID" />, Key2=Typeof(Buffer)
            /// </summary>
            private readonly DoubleKeyDictionary<Type, Guid, DisposeObject> bufferDictionary = [];

            private readonly IEffectsManager manager;

            /// <summary>
            ///     Initializes a new instance of the <see cref="GeometryBufferManager" /> class.
            /// </summary>
            public GeometryBufferManager(IEffectsManager manager) {
                this.manager = manager;
            }

            public int Count => bufferDictionary.Count();

            /// <summary>
            ///     Registers the specified model unique identifier.
            /// </summary>
            /// <typeparam name="T">Geometry Buffer Type</typeparam>
            /// <param name="modelGuid">The model unique identifier.</param>
            /// <param name="geometry">The geometry.</param>
            /// <returns></returns>
            public IGeometryBufferModel Register<T>(Guid modelGuid, Geometry3D geometry)
                where T : class, IGeometryBufferModel, new() {
                if (geometry == null || modelGuid == Guid.Empty) return EmptyGeometryBufferModel.Empty;
                lock (bufferDictionary) {
                    IGeometryBufferModel container;
                    if (bufferDictionary.TryGetValue(typeof(T), geometry.GUID, out var obj)) {
                        if (Logger.IsEnabled(LogLevel.Trace))
                            Logger.Verbose("Existing buffer found, GeomoetryGUID = {Value0}", geometry.GUID);
                        container = obj as IGeometryBufferModel;
                        obj.IncRef();
                    } else {
                        if (Logger.IsEnabled(LogLevel.Trace))
                            Logger.Verbose("Buffer not found, create new buffer. GeomoetryGUID = {Value0}", geometry.GUID);
                        container = new T();
                        var id = geometry.GUID;
                        obj = container as DisposeObject;
                        obj.Disposed += (s, e) => {
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.Verbose("Disposing Geometry Buffer. GeomoetryGUID = {Value0}", id);
                            lock (bufferDictionary) {
                                bufferDictionary.Remove(typeof(T), id);
                            }
                        };
                        container.EffectsManager = manager;
                        container.Geometry = geometry;
                        bufferDictionary.Add(typeof(T), geometry.GUID, obj);
                    }

                    return container;
                }
            }

            #region IDisposable Support

            private bool disposedValue; // To detect redundant calls

            private void Dispose(bool disposing) {
                if (!disposedValue) {
                    if (disposing)
                        lock (bufferDictionary) {
                            foreach (var buffer in bufferDictionary.Values.ToArray()) buffer.ForceDispose();
                            bufferDictionary.Clear();
                        }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~GeometryBufferManager() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose() {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }

            #endregion
        }
    }
}
