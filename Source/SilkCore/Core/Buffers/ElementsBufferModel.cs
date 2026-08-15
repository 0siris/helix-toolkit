/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
///     Used for managing instance buffer update
/// </summary>
public class ElementsBufferModel<T> : DisposeObject, IElementsBufferModel<T> where T : unmanaged {
    public static readonly ElementsBufferModel<T> Empty = new(0);
    private VertexBufferBinding bufferBinding;
    
    /// <summary>
    /// We have locks on this reference
    /// </summary>
    private IElementsBufferProxy? elementBuffer;

    private IList<T>? elements;

    /// <summary>
    /// Indicates whether the instance data has been modified and requires a buffer update.
    /// Used to track changes in the data model that impact the associated instance buffer.
    /// 
    /// Set to true when:
    /// - Elements has changed
    /// - first init
    /// 
    /// Set to false when:
    /// - Buffer data is uploaded in AttachBuffer() to elementBuffer
    /// </summary>
    private volatile bool instanceChanged = true;

    public ElementsBufferModel(int structSize) => StructSize = structSize;

    public int StructSize { get; }

    public event EventHandler<EventArgs>? ElementChanged;
    public Guid Guid { get; } = Guid.NewGuid();

    [MemberNotNullWhen(true, nameof(elementBuffer))]
    public bool Initialized { get; private set; }

    [MemberNotNullWhen(true, nameof(elements))]
    [MemberNotNullWhen(true, nameof(Elements))]
    public bool HasElements { get; private set; }
    public IElementsBufferProxy? Buffer => elementBuffer;

    public bool Changed => instanceChanged;
    
    public IList<T>? Elements {
        get => elements;
        set {
            if (ReferenceEquals(elements, value))
                return;
            
            elements = value;
            instanceChanged = true;
            HasElements = elements != null && elements.Any();
            ElementChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int ElementCount => HasElements ? Elements.Count : 0;

    [MemberNotNull(nameof(elementBuffer))]
    public void Initialize() {
        elementBuffer = new DynamicBufferProxy(StructSize, BindFlags.VertexBuffer);
        Initialized = true;
        instanceChanged = true;
    }

    public virtual void AttachBuffer(DeviceContextProxy context, ref int vertexBufferStartSlot) {
        if (HasElements) {
            if (instanceChanged) {
                if (elementBuffer is not { } currentBuffer || elements is not { } currentElements)
                    return;

                lock (currentBuffer) {
                    if (instanceChanged) {
                        currentBuffer.UploadDataToBuffer(context, currentElements, currentElements.Count);
                        instanceChanged = false;
                        bufferBinding = new VertexBufferBinding(currentBuffer.Buffer,
                                                               currentBuffer.StructureSize,
                                                               currentBuffer.Offset);
                    }
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

public class MatrixInstanceBufferModel() : ElementsBufferModel<Matrix>(SilkMath.MatrixSizeInBytes);

public class InstanceParamsBufferModel<T>(int structSize) : ElementsBufferModel<T>(structSize)
    where T : unmanaged;

public class VertexBoneIdBufferModel<T>(int structSize) : ElementsBufferModel<T>(structSize)
    where T : unmanaged;
