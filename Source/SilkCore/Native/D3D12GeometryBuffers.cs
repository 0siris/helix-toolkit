/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the Direct3D 12 vertex and index buffers for one immutable default mesh geometry.
/// </summary>
internal sealed class SilkD3D12DefaultMeshBuffers : IDisposable {
    private readonly SilkD3D12Resource[] vertexBuffers;

    /// <summary>
    ///     Initializes a static mesh buffer set.
    /// </summary>
    /// <param name="vertexBuffers">The three default-mesh vertex streams.</param>
    /// <param name="indexBuffer">The 32-bit index buffer.</param>
    /// <param name="indexCount">The number of indices to draw.</param>
    /// <param name="topology">The input-assembler primitive topology.</param>
    private SilkD3D12DefaultMeshBuffers(
        SilkD3D12Resource[] vertexBuffers,
        SilkD3D12Resource indexBuffer,
        uint indexCount,
        PrimitiveTopology topology
    ) {
        this.vertexBuffers = vertexBuffers;
        IndexBuffer = indexBuffer;
        IndexCount = indexCount;
        Topology = topology;
    }

    /// <summary>
    ///     Gets the 32-bit index buffer.
    /// </summary>
    internal SilkD3D12Resource IndexBuffer { get; }

    /// <summary>
    ///     Gets the number of indices in the mesh.
    /// </summary>
    internal uint IndexCount { get; private set; }

    /// <summary>
    ///     Gets the mesh primitive topology.
    /// </summary>
    internal PrimitiveTopology Topology { get; private set; }

    /// <summary>
    ///     Gets whether the mesh buffers have been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Creates Direct3D 12 buffers from the CPU geometry owned by an existing default mesh buffer model.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default mesh buffer model.</param>
    /// <returns>The created Direct3D 12 mesh buffers.</returns>
    internal static SilkD3D12DefaultMeshBuffers Create(
        SilkD3D12Device device,
        DefaultMeshGeometryBufferModel source
    ) => Create(device, source, []);

    /// <summary>
    ///     Creates Direct3D 12 buffers after applying the existing bone matrices on the CPU.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing bone-skinned mesh buffer model.</param>
    /// <param name="boneMatrices">The current bone matrices, or an empty span for the original vertices.</param>
    /// <param name="morphTargets">The optional existing morph-target payload.</param>
    /// <returns>The created Direct3D 12 mesh buffers.</returns>
    internal static SilkD3D12DefaultMeshBuffers Create(
        SilkD3D12Device device,
        DefaultMeshGeometryBufferModel source,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore? morphTargets = null
    ) {
        device.AssertArgumentNotNull();
        var (defaults, textureCoordinates, colors, indices, topology) = Prepare(source, boneMatrices, morphTargets);
        SilkD3D12Resource[]? vertexBuffers = null;
        SilkD3D12Resource? indexBuffer = null;
        try {
            vertexBuffers = [
                CreateUploadBuffer(device, defaults.AsSpan()),
                CreateUploadBuffer(device, textureCoordinates.AsSpan()),
                CreateUploadBuffer(device, colors.AsSpan())
            ];
            indexBuffer = CreateUploadBuffer(device, indices.AsSpan());
            return new SilkD3D12DefaultMeshBuffers(vertexBuffers,
                indexBuffer,
                checked((uint) indices.Length),
                topology);
        } catch {
            if (vertexBuffers is not null)
                foreach (var buffer in vertexBuffers)
                    buffer.Dispose();
            indexBuffer?.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Updates an existing upload-heap allocation without replacing its native resources.
    /// </summary>
    /// <param name="source">The existing default mesh buffer model.</param>
    /// <exception cref="InvalidOperationException">The updated mesh exceeds an existing buffer capacity.</exception>
    internal void Update(DefaultMeshGeometryBufferModel source) {
        Update(source, []);
    }

    /// <summary>
    ///     Updates an existing allocation after applying the current bone matrices on the CPU.
    /// </summary>
    /// <param name="source">The existing bone-skinned mesh buffer model.</param>
    /// <param name="boneMatrices">The current bone matrices, or an empty span for the original vertices.</param>
    /// <param name="morphTargets">The optional existing morph-target payload.</param>
    /// <exception cref="InvalidOperationException">The updated mesh exceeds an existing buffer capacity.</exception>
    internal void Update(
        DefaultMeshGeometryBufferModel source,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore? morphTargets = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var (defaults, textureCoordinates, colors, indices, topology) = Prepare(source, boneMatrices, morphTargets);
        ValidateCapacity(vertexBuffers[0], defaults, "vertex");
        ValidateCapacity(vertexBuffers[1], textureCoordinates, "texture-coordinate");
        ValidateCapacity(vertexBuffers[2], colors, "color");
        ValidateCapacity(IndexBuffer, indices, "index");
        vertexBuffers[0].Write(MemoryMarshal.AsBytes(defaults.AsSpan()));
        vertexBuffers[1].Write(MemoryMarshal.AsBytes(textureCoordinates.AsSpan()));
        vertexBuffers[2].Write(MemoryMarshal.AsBytes(colors.AsSpan()));
        IndexBuffer.Write(MemoryMarshal.AsBytes(indices.AsSpan()));
        IndexCount = checked((uint) indices.Length);
        Topology = topology;
    }

    /// <summary>
    ///     Creates a larger replacement before disposing this buffer set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default mesh buffer model.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12DefaultMeshBuffers Recreate(
        SilkD3D12Device device,
        DefaultMeshGeometryBufferModel source
    ) => Recreate(device, source, []);

    /// <summary>
    ///     Creates a larger bone-skinned replacement before disposing this buffer set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing bone-skinned mesh buffer model.</param>
    /// <param name="boneMatrices">The current bone matrices, or an empty span for the original vertices.</param>
    /// <param name="morphTargets">The optional existing morph-target payload.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12DefaultMeshBuffers Recreate(
        SilkD3D12Device device,
        DefaultMeshGeometryBufferModel source,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore? morphTargets = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var replacement = Create(device, source, boneMatrices, morphTargets);
        Dispose();
        return replacement;
    }

    /// <summary>
    ///     Binds the default mesh streams, index buffer, and primitive topology.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="vertexBufferStartSlot">The first vertex input slot.</param>
    /// <returns>The first free vertex input slot after the mesh streams.</returns>
    internal uint Bind(SilkD3D12CommandContext context, uint vertexBufferStartSlot = 0) {
        context.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetVertexBuffer(vertexBufferStartSlot, vertexBuffers[0], DefaultVertex.SizeInBytes);
        context.SetVertexBuffer(vertexBufferStartSlot + 1, vertexBuffers[1], SilkMath.Vector2SizeInBytes);
        context.SetVertexBuffer(vertexBufferStartSlot + 2, vertexBuffers[2], SilkMath.Vector4SizeInBytes);
        context.SetIndexBuffer(IndexBuffer, Format.FormatR32Uint);
        context.SetPrimitiveTopology(Topology);
        return vertexBufferStartSlot + (uint) vertexBuffers.Length;
    }

    /// <summary>
    ///     Releases every native mesh resource.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        foreach (var buffer in vertexBuffers)
            buffer.Dispose();
        IndexBuffer.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Creates a directly writable upload-heap buffer containing an unmanaged value span.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="values">The values to upload.</param>
    /// <returns>The initialized upload-heap resource.</returns>
    private static SilkD3D12Resource CreateUploadBuffer<T>(SilkD3D12Device device, ReadOnlySpan<T> values)
        where T : unmanaged {
        var bytes = MemoryMarshal.AsBytes(values);
        var buffer = device.CreateBuffer(checked((ulong) bytes.Length), HeapType.Upload);
        try {
            buffer.Write(bytes);
            return buffer;
        } catch {
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Validates and prepares the four default-mesh streams shared with the legacy geometry buffer model.
    /// </summary>
    /// <param name="source">The existing default mesh buffer model.</param>
    /// <param name="boneMatrices">The current bone matrices.</param>
    /// <param name="morphTargets">The optional existing morph-target payload.</param>
    /// <returns>The prepared vertex streams, indices, and topology.</returns>
    private static (DefaultVertex[] Defaults, Vector2[] TextureCoordinates, Color4[] Colors, int[] Indices,
        PrimitiveTopology Topology) Prepare(
        DefaultMeshGeometryBufferModel source,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore? morphTargets = null
    ) {
        source.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (source.Geometry is not MeshGeometry3D geometry)
            throw new InvalidOperationException("The buffer model must contain mesh geometry.");
        if (geometry.Positions is not {Count: > 0} positions)
            throw new InvalidOperationException("Default mesh geometry requires positions.");
        if (geometry.Indices is not {Count: > 0} indices)
            throw new InvalidOperationException("Default mesh geometry requires indices.");
        ValidateCount(geometry.TextureCoordinates?.Count, positions.Count, nameof(geometry.TextureCoordinates));
        ValidateCount(geometry.Colors?.Count, positions.Count, nameof(geometry.Colors));
        for (var index = 0; index < indices.Count; index++)
            if ((uint) indices[index] >= (uint) positions.Count)
                throw new ArgumentException($"Index {index} refers to a vertex outside the mesh.", nameof(source));

        var defaultBuffer = DefaultMeshGeometryBufferModel.BuildVertexArray(geometry);
        var defaults = defaultBuffer.Length == positions.Count
            ? defaultBuffer
            : defaultBuffer.AsSpan(0, positions.Count).ToArray();
        morphTargets?.ApplyD3D12MorphTargets(defaults);
        if (!boneMatrices.IsEmpty) {
            if (geometry is not BoneSkinnedMeshGeometry3D {VertexBoneIds: { } boneIds} ||
                boneIds.Count != positions.Count)
                throw new InvalidOperationException("Bone-skinned geometry requires one bone identifier per vertex.");
            for (var index = 0; index < defaults.Length; index++) {
                var boneId = boneIds[index];
                defaults[index] = SkinVertex(in defaults[index], in boneId, boneMatrices);
            }
        }
        return (defaults,
            geometry.TextureCoordinates?.ToArray() ?? new Vector2[positions.Count],
            geometry.Colors?.ToArray() ?? new Color4[positions.Count],
            indices.ToArray(),
            source.Topology);
    }

    /// <summary>
    ///     Applies the four weighted bone influences used by the repository skinning shader to one vertex.
    /// </summary>
    /// <param name="vertex">The original mesh vertex.</param>
    /// <param name="boneIds">The four bone indices and weights.</param>
    /// <param name="boneMatrices">The current bone matrices.</param>
    /// <returns>The skinned vertex.</returns>
    internal static DefaultVertex SkinVertex(
        in DefaultVertex vertex,
        in BoneIds boneIds,
        ReadOnlySpan<Matrix> boneMatrices
    ) {
        ReadOnlySpan<int> indices = [boneIds.Bone1, boneIds.Bone2, boneIds.Bone3, boneIds.Bone4];
        ReadOnlySpan<float> weights = [boneIds.Weights.X, boneIds.Weights.Y, boneIds.Weights.Z, boneIds.Weights.W];
        var position = Vector4.Zero;
        var normal = Vector3.Zero;
        var tangent = Vector3.Zero;
        var biTangent = Vector3.Zero;
        for (var influence = 0; influence < indices.Length; influence++) {
            var weight = weights[influence];
            if (weight == 0) continue;
            var boneIndex = indices[influence];
            if ((uint) boneIndex >= (uint) boneMatrices.Length)
                throw new ArgumentOutOfRangeException(nameof(boneIds), $"Bone index {boneIndex} is not available.");
            var matrix = boneMatrices[boneIndex];
            position += TransformPosition(in vertex.Position, in matrix) * weight;
            normal += SilkMath.TransformNormal(vertex.Normal, matrix) * weight;
            tangent += SilkMath.TransformNormal(vertex.Tangent, matrix) * weight;
            biTangent += SilkMath.TransformNormal(vertex.BiTangent, matrix) * weight;
        }
        return new DefaultVertex {
            Position = position,
            Normal = NormalizeOrZero(normal),
            Tangent = NormalizeOrZero(tangent),
            BiTangent = NormalizeOrZero(biTangent)
        };
    }

    /// <summary>
    ///     Transforms one homogeneous position with the renderer's row-vector matrix convention.
    /// </summary>
    /// <param name="position">The source position.</param>
    /// <param name="matrix">The bone matrix.</param>
    /// <returns>The transformed position.</returns>
    private static Vector4 TransformPosition(in Vector4 position, in Matrix matrix) => new(
        position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + position.W * matrix.M41,
        position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + position.W * matrix.M42,
        position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + position.W * matrix.M43,
        position.X * matrix.M14 + position.Y * matrix.M24 + position.Z * matrix.M34 + position.W * matrix.M44);

    /// <summary>
    ///     Normalizes a direction while preserving an all-zero optional stream.
    /// </summary>
    /// <param name="value">The direction.</param>
    /// <returns>The normalized direction or zero.</returns>
    private static Vector3 NormalizeOrZero(Vector3 value) => value.LengthSquared > 0 ? SilkMath.Normalize(value) : value;

    /// <summary>
    ///     Validates an optional per-vertex stream length.
    /// </summary>
    /// <param name="actualCount">The optional stream count.</param>
    /// <param name="vertexCount">The required position count.</param>
    /// <param name="name">The stream name.</param>
    private static void ValidateCount(int? actualCount, int vertexCount, string name) {
        if (actualCount is not null && actualCount != vertexCount)
            throw new ArgumentException($"{name} must contain one value per position.", nameof(actualCount));
    }

    /// <summary>
    ///     Validates that an unmanaged value span fits an existing resource allocation.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="values">The values to upload.</param>
    /// <param name="streamName">The stream name used in the exception.</param>
    private static void ValidateCapacity<T>(
        SilkD3D12Resource buffer,
        ReadOnlySpan<T> values,
        string streamName
    ) where T : unmanaged {
        if ((ulong) MemoryMarshal.AsBytes(values).Length > buffer.SizeInBytes)
            throw new InvalidOperationException(
                $"The updated mesh exceeds the existing {streamName}-buffer capacity.");
    }
}

/// <summary>
///     Owns the Direct3D 12 vertex and optional index buffers for one default line, point, or billboard geometry.
/// </summary>
internal sealed class SilkD3D12PointLineBuffers : IDisposable {
    private readonly uint vertexStride;

    /// <summary>
    ///     Initializes line or point buffers.
    /// </summary>
    /// <param name="vertexBuffer">The shared position/color vertex stream.</param>
    /// <param name="indexBuffer">The optional line index buffer.</param>
    /// <param name="vertexCount">The number of live vertices.</param>
    /// <param name="indexCount">The number of live indices.</param>
    /// <param name="vertexStride">The vertex stride.</param>
    /// <param name="topology">The input-assembler primitive topology.</param>
    private SilkD3D12PointLineBuffers(
        SilkD3D12Resource vertexBuffer,
        SilkD3D12Resource? indexBuffer,
        uint vertexCount,
        uint indexCount,
        uint vertexStride,
        PrimitiveTopology topology
    ) {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        VertexCount = vertexCount;
        IndexCount = indexCount;
        this.vertexStride = vertexStride;
        Topology = topology;
    }

    /// <summary>
    ///     Gets the position/color vertex buffer.
    /// </summary>
    internal SilkD3D12Resource VertexBuffer { get; }

    /// <summary>
    ///     Gets the optional line index buffer.
    /// </summary>
    internal SilkD3D12Resource? IndexBuffer { get; }

    /// <summary>
    ///     Gets the number of live vertices.
    /// </summary>
    internal uint VertexCount { get; private set; }

    /// <summary>
    ///     Gets the number of live indices.
    /// </summary>
    internal uint IndexCount { get; private set; }

    /// <summary>
    ///     Gets the input-assembler primitive topology.
    /// </summary>
    internal PrimitiveTopology Topology { get; }

    /// <summary>
    ///     Gets whether the native resources have been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Creates Direct3D 12 buffers from an existing default line buffer model.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default line buffer model.</param>
    /// <returns>The initialized line buffers.</returns>
    internal static SilkD3D12PointLineBuffers Create(
        SilkD3D12Device device,
        DefaultLineGeometryBufferModel source
    ) {
        device.AssertArgumentNotNull();
        var (vertices, indices) = Prepare(source);
        return Create(device, vertices, indices, LinesVertex.SizeInBytes, PrimitiveTopology.LineList);
    }

    /// <summary>
    ///     Creates a Direct3D 12 vertex buffer from an existing default point buffer model.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default point buffer model.</param>
    /// <returns>The initialized point buffer.</returns>
    internal static SilkD3D12PointLineBuffers Create(
        SilkD3D12Device device,
        DefaultPointGeometryBufferModel source
    ) {
        device.AssertArgumentNotNull();
        var vertices = Prepare(source);
        return Create(device, vertices, ReadOnlySpan<int>.Empty, PointsVertex.SizeInBytes,
            PrimitiveTopology.PointList);
    }

    /// <summary>
    ///     Creates a Direct3D 12 vertex buffer from an existing default billboard buffer model.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default billboard buffer model.</param>
    /// <returns>The initialized billboard buffer.</returns>
    internal static SilkD3D12PointLineBuffers Create(
        SilkD3D12Device device,
        DefaultBillboardBufferModel source
    ) {
        device.AssertArgumentNotNull();
        var vertices = Prepare(source);
        return Create(device, vertices, ReadOnlySpan<int>.Empty, BillboardVertex.SizeInBytes,
            PrimitiveTopology.PointList);
    }

    /// <summary>
    ///     Updates line data within the existing native allocations.
    /// </summary>
    /// <param name="source">The existing default line buffer model.</param>
    internal void Update(DefaultLineGeometryBufferModel source) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Topology != PrimitiveTopology.LineList)
            throw new InvalidOperationException("Point buffers cannot be updated with line geometry.");
        var (vertices, indices) = Prepare(source);
        Update(vertices, indices);
    }

    /// <summary>
    ///     Updates point data within the existing native allocation.
    /// </summary>
    /// <param name="source">The existing default point buffer model.</param>
    internal void Update(DefaultPointGeometryBufferModel source) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Topology != PrimitiveTopology.PointList)
            throw new InvalidOperationException("Line buffers cannot be updated with point geometry.");
        Update(Prepare(source), ReadOnlySpan<int>.Empty);
    }

    /// <summary>
    ///     Updates billboard data within the existing native allocation.
    /// </summary>
    /// <param name="source">The existing default billboard buffer model.</param>
    internal void Update(DefaultBillboardBufferModel source) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Topology != PrimitiveTopology.PointList || vertexStride != BillboardVertex.SizeInBytes)
            throw new InvalidOperationException("The buffer does not contain billboard vertices.");
        Update(Prepare(source), ReadOnlySpan<int>.Empty);
    }

    /// <summary>
    ///     Creates replacement line buffers before disposing this set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default line buffer model.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12PointLineBuffers Recreate(
        SilkD3D12Device device,
        DefaultLineGeometryBufferModel source
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var replacement = Create(device, source);
        Dispose();
        return replacement;
    }

    /// <summary>
    ///     Creates a replacement point buffer before disposing this set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default point buffer model.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12PointLineBuffers Recreate(
        SilkD3D12Device device,
        DefaultPointGeometryBufferModel source
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var replacement = Create(device, source);
        Dispose();
        return replacement;
    }

    /// <summary>
    ///     Creates replacement billboard buffers before disposing this set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing default billboard buffer model.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12PointLineBuffers Recreate(
        SilkD3D12Device device,
        DefaultBillboardBufferModel source
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var replacement = Create(device, source);
        Dispose();
        return replacement;
    }

    /// <summary>
    ///     Binds the vertex buffer, optional index buffer, and primitive topology.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="vertexBufferSlot">The vertex input slot.</param>
    internal void Bind(SilkD3D12CommandContext context, uint vertexBufferSlot = 0) {
        context.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetVertexBuffer(vertexBufferSlot, VertexBuffer, vertexStride);
        if (IndexBuffer is not null)
            context.SetIndexBuffer(IndexBuffer, Format.FormatR32Uint);
        context.SetPrimitiveTopology(Topology);
    }

    /// <summary>
    ///     Releases the owned native resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        VertexBuffer.Dispose();
        IndexBuffer?.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Creates initialized line or point buffers from validated spans.
    /// </summary>
    private static SilkD3D12PointLineBuffers Create<T>(
        SilkD3D12Device device,
        ReadOnlySpan<T> vertices,
        ReadOnlySpan<int> indices,
        int vertexStride,
        PrimitiveTopology topology
    ) where T : unmanaged {
        SilkD3D12Resource? vertexBuffer = null;
        SilkD3D12Resource? indexBuffer = null;
        try {
            vertexBuffer = CreateUploadBuffer(device, vertices);
            if (!indices.IsEmpty)
                indexBuffer = CreateUploadBuffer(device, indices);
            return new SilkD3D12PointLineBuffers(vertexBuffer,
                indexBuffer,
                checked((uint) vertices.Length),
                checked((uint) indices.Length),
                checked((uint) vertexStride),
                topology);
        } catch {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Creates one directly writable upload-heap buffer.
    /// </summary>
    private static SilkD3D12Resource CreateUploadBuffer<T>(SilkD3D12Device device, ReadOnlySpan<T> values)
        where T : unmanaged {
        var bytes = MemoryMarshal.AsBytes(values);
        var buffer = device.CreateBuffer(checked((ulong) bytes.Length), HeapType.Upload);
        try {
            buffer.Write(bytes);
            return buffer;
        } catch {
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Validates and prepares line vertices and indices.
    /// </summary>
    private static (LinesVertex[] Vertices, int[] Indices) Prepare(DefaultLineGeometryBufferModel source) {
        source.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (source.Geometry is not LineGeometry3D geometry)
            throw new InvalidOperationException("The buffer model must contain line geometry.");
        if (geometry.Positions is not {Count: > 0} positions)
            throw new InvalidOperationException("Line geometry requires positions.");
        if (geometry.Indices is not {Count: > 0} indices)
            throw new InvalidOperationException("Line geometry requires indices.");
        ValidateIndices(indices, positions.Count, source);
        return (DefaultLineGeometryBufferModel.BuildVertexArray(geometry), indices.ToArray());
    }

    /// <summary>
    ///     Validates and prepares point vertices.
    /// </summary>
    private static PointsVertex[] Prepare(DefaultPointGeometryBufferModel source) {
        source.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (source.Geometry is not PointGeometry3D geometry)
            throw new InvalidOperationException("The buffer model must contain point geometry.");
        if (geometry.Positions is not {Count: > 0})
            throw new InvalidOperationException("Point geometry requires positions.");
        return DefaultPointGeometryBufferModel.BuildVertexArray(geometry);
    }

    /// <summary>
    ///     Validates and copies prepared billboard vertices.
    /// </summary>
    /// <param name="source">The existing billboard buffer model.</param>
    /// <returns>The prepared billboard vertices.</returns>
    private static BillboardVertex[] Prepare(DefaultBillboardBufferModel source) {
        source.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (source.Geometry is not BillboardBase geometry || !geometry.TryPrepareVerticesForD3D12())
            throw new InvalidOperationException("Billboard geometry requires prepared vertices.");
        return [.. geometry.BillboardVertices];
    }

    /// <summary>
    ///     Rejects indices that refer outside the position stream.
    /// </summary>
    private static void ValidateIndices(IReadOnlyList<int> indices, int vertexCount, object source) {
        for (var index = 0; index < indices.Count; index++)
            if ((uint) indices[index] >= (uint) vertexCount)
                throw new ArgumentException($"Index {index} refers to a vertex outside the geometry.",
                    nameof(source));
    }

    /// <summary>
    ///     Writes validated line or point data into existing allocations.
    /// </summary>
    private void Update<T>(ReadOnlySpan<T> vertices, ReadOnlySpan<int> indices) where T : unmanaged {
        var vertexBytes = MemoryMarshal.AsBytes(vertices);
        var indexBytes = MemoryMarshal.AsBytes(indices);
        if ((ulong) vertexBytes.Length > VertexBuffer.SizeInBytes)
            throw new InvalidOperationException("The updated vertices exceed the existing buffer capacity.");
        if (indexBytes.Length > 0 && (IndexBuffer is null || (ulong) indexBytes.Length > IndexBuffer.SizeInBytes))
            throw new InvalidOperationException("The updated indices exceed the existing buffer capacity.");
        VertexBuffer.Write(vertexBytes);
        IndexBuffer?.Write(indexBytes);
        VertexCount = checked((uint) vertices.Length);
        IndexCount = checked((uint) indices.Length);
    }
}

/// <summary>
///     Owns one directly writable Direct3D 12 vertex stream sourced from an existing element buffer model.
/// </summary>
/// <typeparam name="T">The unmanaged element type.</typeparam>
internal sealed class SilkD3D12ElementsBuffer<T> : IDisposable where T : unmanaged {
    /// <summary>
    ///     Initializes an element buffer wrapper.
    /// </summary>
    /// <param name="resource">The upload-heap vertex buffer.</param>
    /// <param name="elementCount">The number of live elements.</param>
    private SilkD3D12ElementsBuffer(SilkD3D12Resource resource, uint elementCount) {
        Resource = resource;
        ElementCount = elementCount;
    }

    /// <summary>
    ///     Gets the native buffer resource.
    /// </summary>
    internal SilkD3D12Resource Resource { get; }

    /// <summary>
    ///     Gets the number of live elements.
    /// </summary>
    internal uint ElementCount { get; private set; }

    /// <summary>
    ///     Gets the unmanaged element stride.
    /// </summary>
    internal static uint StrideInBytes => checked((uint) System.Runtime.CompilerServices.Unsafe.SizeOf<T>());

    /// <summary>
    ///     Gets whether the native buffer has been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Creates a Direct3D 12 vertex stream from an existing element buffer model.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing element buffer model.</param>
    /// <returns>The created Direct3D 12 element buffer.</returns>
    internal static SilkD3D12ElementsBuffer<T> Create(
        SilkD3D12Device device,
        IElementsBufferModel<T> source
    ) {
        device.AssertArgumentNotNull();
        var values = Prepare(source);
        var resource = device.CreateBuffer(checked((ulong) values.Length * StrideInBytes), HeapType.Upload);
        try {
            resource.Write(MemoryMarshal.AsBytes(values.AsSpan()));
            return new SilkD3D12ElementsBuffer<T>(resource, checked((uint) values.Length));
        } catch {
            resource.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Updates the stream without replacing its native allocation.
    /// </summary>
    /// <param name="source">The existing element buffer model.</param>
    /// <exception cref="InvalidOperationException">The elements exceed the existing capacity.</exception>
    internal void Update(IElementsBufferModel<T> source) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var values = Prepare(source);
        var bytes = MemoryMarshal.AsBytes(values.AsSpan());
        if ((ulong) bytes.Length > Resource.SizeInBytes)
            throw new InvalidOperationException("The elements exceed the existing buffer capacity.");
        Resource.Write(bytes);
        ElementCount = checked((uint) values.Length);
    }

    /// <summary>
    ///     Creates a larger replacement before disposing this buffer.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="source">The existing element buffer model.</param>
    /// <returns>The fully initialized replacement.</returns>
    internal SilkD3D12ElementsBuffer<T> Recreate(
        SilkD3D12Device device,
        IElementsBufferModel<T> source
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var replacement = Create(device, source);
        Dispose();
        return replacement;
    }

    /// <summary>
    ///     Binds the elements as one input-assembler vertex stream.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="slot">The vertex input slot.</param>
    internal void Bind(SilkD3D12CommandContext context, uint slot) {
        context.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetVertexBuffer(slot, Resource, StrideInBytes);
    }

    /// <summary>
    ///     Releases the native element buffer.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        Resource.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Validates and copies the live unmanaged elements from a buffer model.
    /// </summary>
    /// <param name="source">The existing element buffer model.</param>
    /// <returns>The contiguous element data.</returns>
    private static T[] Prepare(IElementsBufferModel<T> source) {
        source.AssertArgumentNotNull();
        if (source.Elements is not {Count: > 0} elements)
            throw new InvalidOperationException("The element buffer model has no elements.");
        return [.. elements];
    }
}
