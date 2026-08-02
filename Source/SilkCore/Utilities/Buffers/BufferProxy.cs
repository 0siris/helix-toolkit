/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        /// <summary>
        /// </summary>
        public interface IBufferProxy : IDisposable {
            /// <summary>
            ///     Raw Buffer
            /// </summary>
            Buffer Buffer { get; }

            /// <summary>
            ///     Element Size
            /// </summary>
            int StructureSize { get; }

            /// <summary>
            ///     Element count
            /// </summary>
            int ElementCount { get; }

            /// <summary>
            ///     Buffer offset in bytes
            /// </summary>
            int Offset { get; set; }

            /// <summary>
            ///     Buffer binding flag
            /// </summary>
            BindFlags BindFlags { get; }
        }


        /// <summary>
        /// </summary>
        public abstract class BufferProxyBase : DisposeObject, IBufferProxy {
            /// <summary>
            /// </summary>
            protected Buffer buffer;

            /// <summary>
            /// </summary>
            /// <param name="structureSize"></param>
            /// <param name="bindFlags"></param>
            public BufferProxyBase(int structureSize, BindFlags bindFlags) {
                StructureSize = structureSize;
                BindFlags = bindFlags;
            }

            /// <summary>
            ///     <see cref="IBufferProxy.StructureSize" />
            /// </summary>
            public int StructureSize { get; }

            /// <summary>
            ///     <see cref="IBufferProxy.ElementCount" />
            /// </summary>
            public int ElementCount { get; protected set; }

            /// <summary>
            ///     Buffer data offset in bytes.
            ///     <see cref="IBufferProxy.Offset" />
            /// </summary>
            public int Offset { get; set; } = 0;

            /// <summary>
            ///     <see cref="IBufferProxy.Buffer" />
            /// </summary>
            public Buffer Buffer => buffer;

            /// <summary>
            ///     <see cref="IBufferProxy.BindFlags" />
            /// </summary>
            public BindFlags BindFlags { get; }

            public void DisposeAndClear() {
                RemoveAndDispose(ref buffer);
                ElementCount = 0;
            }

            protected override void OnDispose(bool disposeManagedResources) {
                DisposeAndClear();
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
