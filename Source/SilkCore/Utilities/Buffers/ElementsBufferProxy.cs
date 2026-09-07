/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Utilities.Buffers;

/// <summary>
/// </summary>
public interface IElementsBufferProxy : IBufferProxy {

    /// <summary>
    ///     Dispose and clear internal buffers. Does not dispose this object.
    /// </summary>
    void DisposeAndClear();
}

/// <summary>
/// </summary>
public sealed class ImmutableBufferProxy : BufferProxyBase, IElementsBufferProxy {
    /// <summary>
    /// </summary>
    /// <param name="structureSize"></param>
    /// <param name="bindFlags"></param>
    /// <param name="optionFlags"></param>
    /// <param name="usage"></param>
    public ImmutableBufferProxy(
        int structureSize,
        BindFlags bindFlags,
        ResourceOptionFlags optionFlags = ResourceOptionFlags.None,
        ResourceUsage usage = ResourceUsage.Immutable
    )
        : base(structureSize, bindFlags) {
        OptionFlags = optionFlags;
        Usage = usage;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImmutableBufferProxy" /> class.
    /// </summary>
    /// <param name="structureSize">Size of the structure.</param>
    /// <param name="bindFlags">The bind flags.</param>
    /// <param name="cpuAccess">The cpu access.</param>
    /// <param name="optionFlags">The option flags.</param>
    /// <param name="usage">The usage.</param>
    public ImmutableBufferProxy(
        int structureSize,
        BindFlags bindFlags,
        CpuAccessFlags cpuAccess,
        ResourceOptionFlags optionFlags = ResourceOptionFlags.None,
        ResourceUsage usage = ResourceUsage.Immutable
    )
        : base(structureSize, bindFlags) {
        OptionFlags = optionFlags;
        Usage = usage;
        CpuAccess = cpuAccess;
    }

    /// <summary>
    /// </summary>
    public ResourceOptionFlags OptionFlags { get; }

    public ResourceUsage Usage { get; } = ResourceUsage.Immutable;

    public CpuAccessFlags CpuAccess { get; } = CpuAccessFlags.None;
}

/// <summary>
/// </summary>
public class DynamicBufferProxy : BufferProxyBase, IElementsBufferProxy {
    public readonly bool CanOverwrite;
    public readonly bool LazyResize = true;

    /// <summary>
    /// </summary>
    /// <param name="structureSize"></param>
    /// <param name="bindFlags"></param>
    /// <param name="optionFlags"></param>
    /// <param name="lazyResize">
    ///     If existing data size is smaller than buffer size, reuse existing. Otherwise create a new
    ///     buffer with exact same size
    /// </param>
    public DynamicBufferProxy(
        int structureSize,
        BindFlags bindFlags,
        ResourceOptionFlags optionFlags = ResourceOptionFlags.None,
        bool lazyResize = true
    )
        : base(structureSize, bindFlags) {
        CanOverwrite = (bindFlags & (BindFlags.VertexBuffer | BindFlags.IndexBuffer)) != 0;
        OptionFlags = optionFlags;
        LazyResize = lazyResize;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicBufferProxy" /> class.
    /// </summary>
    /// <param name="structureSize">Size of the structure.</param>
    /// <param name="bindFlags">The bind flags.</param>
    /// <param name="optionFlags">The option flags.</param>
    /// <param name="lazyResize">if set to <c>true</c> [lazy resize].</param>
    /// <param name="canOverWrite">if set to <c>true</c> [can over write].</param>
    public DynamicBufferProxy(
        int structureSize,
        BindFlags bindFlags,
        bool canOverWrite,
        ResourceOptionFlags optionFlags = ResourceOptionFlags.None,
        bool lazyResize = true
    )
        : base(structureSize, bindFlags) {
        CanOverwrite = canOverWrite;
        OptionFlags = optionFlags;
        LazyResize = lazyResize;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicBufferProxy" /> class.
    /// </summary>
    /// <param name="structureSize">Size of the structure.</param>
    /// <param name="bindFlags">The bind flags.</param>
    /// <param name="canOverWrite">if set to <c>true</c> [can over write].</param>
    /// <param name="cpuAccess">The cpu access.</param>
    /// <param name="optionFlags">The option flags.</param>
    /// <param name="lazyResize">if set to <c>true</c> [lazy resize].</param>
    public DynamicBufferProxy(
        int structureSize,
        BindFlags bindFlags,
        bool canOverWrite,
        CpuAccessFlags cpuAccess,
        ResourceOptionFlags optionFlags = ResourceOptionFlags.None,
        bool lazyResize = true
    )
        : base(structureSize, bindFlags) {
        CanOverwrite = canOverWrite;
        OptionFlags = optionFlags;
        LazyResize = lazyResize;
        CpuAccess = cpuAccess;
    }

    /// <summary>
    /// </summary>
    public ResourceOptionFlags OptionFlags { get; }

    /// <summary>
    ///     Gets the capacity in bytes.
    /// </summary>
    /// <value>
    ///     The capacity.
    /// </value>
    public int Capacity { get; private set; }

    /// <summary>
    ///     Gets the capacity used in bytes.
    /// </summary>
    /// <value>
    ///     The capacity used.
    /// </value>
    public int CapacityUsed { get; private set; }

    public CpuAccessFlags CpuAccess { get; } = CpuAccessFlags.Write;
}

public sealed class StructuredBufferProxy : DynamicBufferProxy {

    /// <summary>
    ///     Initializes a new instance of the <see cref="StructuredBufferProxy" /> class.
    /// </summary>
    /// <param name="structureSize">Size of the structure.</param>
    /// <param name="lazyResize">
    ///     If existing data size is smaller than buffer size, reuse existing.
    ///     Otherwise create a new buffer with exact same size
    /// </param>
    public StructuredBufferProxy(int structureSize, bool lazyResize = true)
        : base(structureSize, BindFlags.ShaderResource, ResourceOptionFlags.BufferStructured, lazyResize) { }
}
