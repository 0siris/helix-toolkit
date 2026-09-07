/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using JetBrains.Annotations;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
///     Mesh Geometry Buffer Model.
/// </summary>
/// <typeparam name="TVertexStruct"></typeparam>
public abstract class MeshGeometryBufferModel<TVertexStruct> : GeometryBufferModel where TVertexStruct : struct {
    protected static readonly TVertexStruct[] EmptyVerts = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="structSize">Size of the structure.</param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public MeshGeometryBufferModel(int structSize, bool dynamic = false)
        : base(PrimitiveTopology.TriangleList,
               dynamic
                   ? new DynamicBufferProxy(structSize, BindFlags.VertexBuffer)
                   : new ImmutableBufferProxy(structSize, BindFlags.VertexBuffer),
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="structSize">Size of the structure.</param>
    /// <param name="topology">The topology.</param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public MeshGeometryBufferModel(int structSize, PrimitiveTopology topology, bool dynamic = false)
        : base(topology,
               dynamic
                   ? new DynamicBufferProxy(structSize, BindFlags.VertexBuffer)
                   : new ImmutableBufferProxy(structSize, BindFlags.VertexBuffer),
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="topology">The topology.</param>
    /// <param name="vertexBuffers"></param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public MeshGeometryBufferModel(
        PrimitiveTopology topology,
        IElementsBufferProxy[] vertexBuffers,
        bool dynamic = false
    )
        : base(topology,
               vertexBuffers,
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="topology">The topology.</param>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    /// <param name="indexBuffer">The index buffer.</param>
    protected MeshGeometryBufferModel(
        PrimitiveTopology topology,
        IElementsBufferProxy vertexBuffer,
        IElementsBufferProxy indexBuffer
    )
        : base(topology, vertexBuffer, indexBuffer) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="topology">The topology.</param>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    /// <param name="indexBuffer">The index buffer.</param>
    protected MeshGeometryBufferModel(
        PrimitiveTopology topology,
        IElementsBufferProxy[] vertexBuffer,
        IElementsBufferProxy indexBuffer
    )
        : base(topology, vertexBuffer, indexBuffer) { }
}

/// <summary>
/// </summary>
public class DefaultMeshGeometryBufferModel : MeshGeometryBufferModel<DefaultVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultMeshGeometryBufferModel" /> class.
    /// </summary>
    public DefaultMeshGeometryBufferModel()
        : base(PrimitiveTopology.TriangleList,
        [
            new ImmutableBufferProxy(DefaultVertex.SizeInBytes, BindFlags.VertexBuffer),
            new ImmutableBufferProxy(SilkMath.Vector2SizeInBytes, BindFlags.VertexBuffer),
            new ImmutableBufferProxy(SilkMath.Vector4SizeInBytes, BindFlags.VertexBuffer)
        ]) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultMeshGeometryBufferModel" /> class.
    /// </summary>
    /// <param name="buffers">The buffers.</param>
    /// <param name="isDynamic"></param>
    public DefaultMeshGeometryBufferModel(IElementsBufferProxy[] buffers, bool isDynamic)
        : base(PrimitiveTopology.TriangleList, buffers, isDynamic) { }

    /// <summary>
    ///     Determines whether [is vertex buffer changed] [the specified property name].
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="bufferIndex"></param>
    /// <returns>
    ///     <c>true</c> if [is vertex buffer changed] [the specified property name]; otherwise, <c>false</c>.
    /// </returns>
    protected override bool IsVertexBufferChanged(string? propertyName, int bufferIndex) {
        switch (bufferIndex) {
            case 0:
                return base.IsVertexBufferChanged(propertyName, bufferIndex);
            case 1:
                return propertyName?.Equals(nameof(MeshGeometry3D.TextureCoordinates), StringComparison.Ordinal) == true;
            case 2:
                return propertyName?.Equals(nameof(MeshGeometry3D.Colors), StringComparison.Ordinal) == true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Builds the vertex array.
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    /// <returns></returns>
    internal static DefaultVertex[] BuildVertexArray(MeshGeometry3D geometry) {
        //var geometry = this.geometryInternal as MeshGeometry3D;
        var positionsCollection = geometry.Positions
            ?? throw new InvalidOperationException("Mesh geometry positions are required.");
        var vertexCount = positionsCollection.Count;
        using var positions = positionsCollection.GetEnumerator();
                
        using var normals = GetEnumerator(geometry.Normals, nameof(geometry.Normals));
        using var tangents = GetEnumerator(geometry.Tangents, nameof(geometry.Tangents));
        using var bitangents = GetEnumerator(geometry.BiTangents, nameof(geometry.BiTangents));

        var array = ThreadBufferManager<DefaultVertex>.GetBuffer(vertexCount);
        for (var i = 0; i < vertexCount; i++) {
            positions.MoveNext();
            normals.MoveNext();
            tangents.MoveNext();
            bitangents.MoveNext();
            array[i].Position = new Vector4(positions.Current, 1f);
            array[i].Normal = normals.Current;
            array[i].Tangent = tangents.Current;
            array[i].BiTangent = bitangents.Current;
        }
                
        return array;

        [MustDisposeResource]
        IEnumerator<Color3> GetEnumerator(Vector3Collection? collection, string name) {
            if (collection is not null && collection.Count != vertexCount)
                throw new ArgumentException($"{name} must contain one value per position.", nameof(geometry));

            return collection?.GetEnumerator()
                   ?? Enumerable.Repeat(Vector3.Zero, vertexCount).GetEnumerator();
        }
    }
}

/// <summary>
/// </summary>
public sealed class DynamicMeshGeometryBufferModel : DefaultMeshGeometryBufferModel {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicMeshGeometryBufferModel" /> class.
    /// </summary>
    public DynamicMeshGeometryBufferModel()
        : base([
                   new DynamicBufferProxy(DefaultVertex.SizeInBytes, BindFlags.VertexBuffer),
                   new DynamicBufferProxy(SilkMath.Vector2SizeInBytes, BindFlags.VertexBuffer),
                   new DynamicBufferProxy(SilkMath.Vector4SizeInBytes, BindFlags.VertexBuffer)
               ],
               true) { }
}
