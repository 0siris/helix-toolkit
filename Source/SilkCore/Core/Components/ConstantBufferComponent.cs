/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Components;

/// <summary>
/// </summary>
public sealed class ConstantBufferComponent : CoreComponent {
    private readonly ConstantBufferDescription bufferDesc;
    private readonly Lock @lock = new();

    [MemberNotNullWhen(true, nameof(Storage))]
    [MemberNotNullWhen(true, nameof(ModelConstBuffer))]
    public override bool IsAttached => base.IsAttached;

    private ArrayStorage? Storage {
        get;
        set {
            if (value != field) {
                field?.Dispose();
                field = null;
            } else {
                field = value;
            }
            
        }
    }

    private int storageId = -1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConstantBufferComponent" /> class.
    /// </summary>
    /// <param name="desc">The desc.</param>
    public ConstantBufferComponent(ConstantBufferDescription desc) 
        => bufferDesc = desc.AssertNotNull("Can' be null");

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConstantBufferComponent" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="structSize">Size of the structure.</param>
    public ConstantBufferComponent(string name, int structSize) 
        => bufferDesc = new ConstantBufferDescription(name, structSize);

    /// <summary>
    ///     Gets or sets the model constant buffer.
    /// </summary>
    /// <value>
    ///     The model constant buffer.
    /// </value>
    public ConstantBufferProxy? ModelConstBuffer {
        get;
        private set {
            if (value != field) {
                field?.Dispose();
                field = null;
            }
            else {
                field = value;
            }
        }
    }

    protected override void OnAttach(IRenderTechnique technique) {
        lock (@lock) {
            ModelConstBuffer = technique.ConstantBufferPool.Register(bufferDesc);
            Storage = technique.EffectsManager.StructArrayPool.Register(bufferDesc.StructSize);
            storageId = Storage.GetId();
        }
    }

    protected override void OnDetach() {
        lock (@lock) {
            ModelConstBuffer = null;
            Storage?.ReleaseId(storageId);
            Storage = null;
            storageId = -1;
        }
    }

    /// <summary>
    ///     Uploads the specified device context. This uploads internal byte buffer only.
    /// </summary>
    /// <param name="deviceContext">The device context.</param>
    public bool Upload(DeviceContextProxy deviceContext) {
        lock (@lock) {
            if (!IsAttached)
                return false;
            
            var array = Storage.GetArray();
            var off = Storage.GetOffSet(storageId);
            ModelConstBuffer.UploadDataToBuffer(deviceContext, array, ModelConstBuffer.StructureSize, off);
            return true;
        }
    }

    /// <summary>
    ///     Uploads the specified device context. This function writes a external struct and writes remains byte buffer
    ///     by offset = input struct size/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="deviceContext">The device context.</param>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    public bool Upload<T>(DeviceContextProxy deviceContext, ref T data) where T : unmanaged {
        lock (@lock) {
            if (!IsAttached) 
                return false;
            
            var structSize = UnsafeHelper.SizeOf<T>();
            if (ModelConstBuffer.StructureSize < structSize) {
#if DEBUG
                throw new ArgumentOutOfRangeException(
                    $"Try to write value out of range. StructureSize {structSize}" +
                    $" > Constant Buffer Size {ModelConstBuffer.StructureSize}");
#else
                            return false;
#endif
            }

            var box = ModelConstBuffer.Map(deviceContext);
            unsafe {
                var pBuf = (byte*)box.DataPointer.ToPointer();
                *(T*)pBuf = data;
            }

            ModelConstBuffer.Unmap(deviceContext);
            return true;

        }
    }

    /// <summary>
    ///     Writes the value into internal byte buffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValueByName<T>(string name, T value) where T : unmanaged {
        if (!IsAttached) 
            return;
        
        lock (@lock) {
            if (!IsAttached) 
                return;
            
            if (ModelConstBuffer.TryGetVariableByName(name, out var variable)) {
                if (UnsafeHelper.SizeOf<T>() > variable.Size) {
                    var structSize = UnsafeHelper.SizeOf<T>();
                    throw new ArgumentException(
                        $"Input struct size {structSize} is larger than shader variable {variable.Name} size {variable.Size}");
                }

                if (!Storage.Write(storageId, variable.StartOffset, ref value))
                    throw new ArgumentException($"Failed to write value on {name}");
            } else {
#if DEBUG
                throw new ArgumentException(
                    $"Variable not found in constant buffer {bufferDesc.Name}. Variable = {name}");
#else
                                Logger.Warn("Variable not found in constant buffer {Value0}. Variable = {Value1}", bufferDesc.Name, name);
#endif
            }
        }
    }

    /// <summary>
    ///     Writes the value into internal byte buffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <param name="offset">The offset.</param>
    public void WriteValue<T>(T value, int offset) where T : unmanaged {
        if (!IsAttached) 
            return;
        
        lock (@lock) {
            if (IsAttached) 
                Storage.Write(storageId, offset, ref value);
        }
    }

    public bool ReadValueByName<T>(string name, out T value) where T : unmanaged {
        var v = default(T);
        if (IsAttached)
            lock (@lock) {
                if (IsAttached) {
                    if (ModelConstBuffer.TryGetVariableByName(name, out var variable))
                        return Storage.Read(storageId, variable.StartOffset, out value);
#if DEBUG
                    throw new ArgumentException(
                        $"Variable not found in constant buffer {bufferDesc.Name}. Variable = {name}");
#else
                                Logger.Warn("Variable not found in constant buffer {Value0}. Variable = {Value1}", bufferDesc.Name, name);
                                value = v;
                                return false;
#endif
                }
            }

        value = v;
        return false;
    }

    public bool ReadValue<T>(int offset, out T value) where T : unmanaged {
        var v = default(T);
        if (IsAttached)
            lock (@lock) {
                if (IsAttached) 
                    return Storage.Read(storageId, offset, out value);
            }

        value = v;
        return false;
    }
}