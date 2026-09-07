/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
///     Point Geometry Buffer Model. Use for point rendering
/// </summary>
/// <typeparam name="VertexStruct"></typeparam>
public abstract class PointGeometryBufferModel<VertexStruct> : GeometryBufferModel where VertexStruct : struct {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PointGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="structSize">Size of the structure.</param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public PointGeometryBufferModel(int structSize, bool dynamic = false)
        : base(PrimitiveTopology.PointList,
               dynamic
                   ? new DynamicBufferProxy(structSize, BindFlags.VertexBuffer)
                   : new ImmutableBufferProxy(structSize, BindFlags.VertexBuffer),
               null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PointGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    public PointGeometryBufferModel(IElementsBufferProxy vertexBuffer)
        : base(PrimitiveTopology.PointList,
               vertexBuffer,
               null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PointGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    public PointGeometryBufferModel(IElementsBufferProxy[] vertexBuffer) 
        : base(PrimitiveTopology.PointList, vertexBuffer, null) 
    { }
}

/// <summary>
/// </summary>
public class DefaultPointGeometryBufferModel : PointGeometryBufferModel<PointsVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultPointGeometryBufferModel" /> class.
    /// </summary>
    public DefaultPointGeometryBufferModel() : base(PointsVertex.SizeInBytes) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultPointGeometryBufferModel" /> class.
    /// </summary>
    /// <param name="isDynamic"></param>
    public DefaultPointGeometryBufferModel(bool isDynamic) : base(PointsVertex.SizeInBytes, isDynamic) { }


    protected override bool IsVertexBufferChanged(string? propertyName, int vertexBufferIndex) =>
        base.IsVertexBufferChanged(propertyName, vertexBufferIndex) ||
        propertyName?.Equals(nameof(Geometry3D.Colors), StringComparison.Ordinal) == true;

    /// <summary>
    ///     Called when [build vertex array].
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    /// <returns></returns>
    internal static PointsVertex[] BuildVertexArray(Geometry3D geometry) {
        var positions = geometry.Positions
            ?? throw new InvalidOperationException("Point geometry requires positions.");
        var vertexCount = positions.Count;
        if (geometry.Colors is { } geometryColors && geometryColors.Count != vertexCount)
            throw new ArgumentException("Point colors must contain one value per position.", nameof(geometry));
        var array = new PointsVertex[vertexCount];

        for (var i = 0; i < vertexCount; i++) {
            array[i].Position = new Vector4(positions[i], 1f);
            array[i].Color = geometry.Colors?[i] ?? Color.White;
        }

        return array;
    }
}

/// <summary>
/// </summary>
public sealed class DynamicPointGeometryBufferModel : DefaultPointGeometryBufferModel {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicPointGeometryBufferModel" /> class.
    /// </summary>
    public DynamicPointGeometryBufferModel() : base(true) { }
}
