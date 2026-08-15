/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using JetBrains.Annotations;

namespace HelixToolkit.SharpDX.Core.Core;

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


    /// <summary>
    ///     Called when [create index buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    protected override void OnCreateIndexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    ) {
        if (geometry is {Indices.Count: > 0})
            buffer.UploadDataToBuffer(context,
                                      geometry.Indices,
                                      geometry.Indices.Count,
                                      0,
                                      geometry.PreDefinedIndexCount);
        else
            buffer.UploadDataToBuffer(context, Array.Empty<int>(), 0);
        //buffer.DisposeAndClear();
    }
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
    ///     Called when [create vertex buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="bufferIndex">Index of the buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    protected override void OnCreateVertexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        int bufferIndex,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    ) {
        if (geometry is not MeshGeometry3D mesh)
            return;
                
        switch (bufferIndex) {
            case 0:
                // -- set geometry if given
                if (mesh.Positions is { Count: > 0 } positions) {
                    // --- get geometry
                    var data = BuildVertexArray(mesh);
                    buffer.UploadDataToBuffer(context,
                                              data,
                                              positions.Count,
                                              0,
                                              geometry.PreDefinedVertexCount);
                } else {
                    //buffer.DisposeAndClear();
                    buffer.UploadDataToBuffer(context, Array.Empty<DefaultVertex>(), 0);
                }

                break;
            case 1:
                if (mesh.TextureCoordinates is {Count: > 0})
                    buffer.UploadDataToBuffer(context,
                                              mesh.TextureCoordinates,
                                              mesh.TextureCoordinates.Count,
                                              0,
                                              geometry.PreDefinedVertexCount);
                else
                    buffer.UploadDataToBuffer(context, Array.Empty<Vector2>(), 0);
                break;
            case 2:
                if (geometry.Colors is {Count: > 0})
                    buffer.UploadDataToBuffer(context,
                                              geometry.Colors,
                                              geometry.Colors.Count,
                                              0,
                                              geometry.PreDefinedVertexCount);
                else
                    buffer.UploadDataToBuffer(context, Array.Empty<Vector4>(), 0);
                break;
        }
    }

    /// <summary>
    ///     Builds the vertex array.
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    /// <returns></returns>
    private DefaultVertex[] BuildVertexArray(MeshGeometry3D geometry) {
        //var geometry = this.geometryInternal as MeshGeometry3D;
        var positionsCollection = geometry.Positions
            ?? throw new System.InvalidOperationException("Mesh geometry positions are required.");
        var vertexCount = positionsCollection.Count;
        using var positions = positionsCollection.GetEnumerator();
                
        using var normals = GetEnumerator(geometry.Normals);
        using var tangents = GetEnumerator(geometry.Tangents);
        using var bitangents = GetEnumerator(geometry.BiTangents);

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
        IEnumerator<Color3> GetEnumerator(Vector3Collection? vec3Collection) =>
            vec3Collection?.GetEnumerator()
            ?? Enumerable.Repeat(Vector3.Zero, vertexCount).GetEnumerator(); //zero if collection is null
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
