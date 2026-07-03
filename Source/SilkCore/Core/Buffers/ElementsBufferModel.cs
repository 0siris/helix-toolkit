/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        /// <summary>
        ///     Used for managing instance buffer update
        /// </summary>
        public class ElementsBufferModel<T> : DisposeObject, IElementsBufferModel<T> where T : unmanaged {
            public static readonly ElementsBufferModel<T> Empty = new(0);
            private VertexBufferBinding bufferBinding;
            private IElementsBufferProxy elementBuffer;

            private IList<T> elements;
            private volatile bool instanceChanged = true;

            public ElementsBufferModel(int structSize) {
                StructSize = structSize;
            }

            public int StructSize { get; }

            public event EventHandler<EventArgs> ElementChanged;
            public Guid GUID { get; } = Guid.NewGuid();

            public bool Initialized { get; private set; }

            public bool HasElements { get; private set; }
            public IElementsBufferProxy Buffer => elementBuffer;

            public bool Changed => instanceChanged;

            public IList<T> Elements {
                get => elements;
                set {
                    if (elements != value) {
                        elements = value;
                        instanceChanged = true;
                        HasElements = elements != null && elements.Any();
                        ElementChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public int ElementCount => HasElements ? Elements.Count : 0;

            public void Initialize() {
                elementBuffer = new DynamicBufferProxy(StructSize, BindFlags.VertexBuffer);
                Initialized = true;
                instanceChanged = true;
            }

            public virtual void AttachBuffer(DeviceContextProxy context, ref int vertexBufferStartSlot) {
                if (HasElements) {
                    if (instanceChanged)
                        lock (elementBuffer) {
                            if (instanceChanged) {
                                elementBuffer.UploadDataToBuffer(context, elements, elements.Count);
                                instanceChanged = false;
                                bufferBinding =
                                    new VertexBufferBinding(Buffer.Buffer, Buffer.StructureSize, Buffer.Offset);
                            }
                        }

                    context.SetVertexBuffers(vertexBufferStartSlot, bufferBinding);
                }

                ++vertexBufferStartSlot;
            }

            public void DisposeAndClear() {
                Initialized = false;
                RemoveAndDispose(ref elementBuffer);
            }

            protected override void OnDispose(bool disposeManagedResources) {
                DisposeAndClear();
                base.OnDispose(disposeManagedResources);
            }
        }

        public class MatrixInstanceBufferModel : ElementsBufferModel<Matrix> {
            public MatrixInstanceBufferModel()
                : base(SilkMath.MatrixSizeInBytes) { }
        }

        public class InstanceParamsBufferModel<T> : ElementsBufferModel<T> where T : unmanaged {
            public InstanceParamsBufferModel(int structSize) : base(structSize) { }
        }

        public class VertexBoneIdBufferModel<T> : ElementsBufferModel<T> where T : unmanaged {
            public VertexBoneIdBufferModel(int structSize) : base(structSize) { }
        }
    }
}
