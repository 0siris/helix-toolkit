/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
///     Line Geometry Buffer Model. Used for line rendering
/// </summary>
/// <typeparam name="VertexStruct"></typeparam>
public abstract class LineGeometryBufferModel<VertexStruct> 
    : GeometryBufferModel where VertexStruct : struct 
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LineGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="structSize">Size of the structure.</param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public LineGeometryBufferModel(int structSize, bool dynamic = false)
        : base(PrimitiveTopology.LineList,
               dynamic
                   ? new DynamicBufferProxy(structSize, BindFlags.VertexBuffer)
                   : new ImmutableBufferProxy(structSize, BindFlags.VertexBuffer),
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LineGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public LineGeometryBufferModel(IElementsBufferProxy vertexBuffer, bool dynamic = false)
        : base(PrimitiveTopology.LineList,
               vertexBuffer,
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LineGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    /// <param name="dynamic">Create dynamic buffer or immutable buffer</param>
    public LineGeometryBufferModel(IElementsBufferProxy[] vertexBuffer, bool dynamic = false)
        : base(PrimitiveTopology.LineList,
               vertexBuffer,
               dynamic
                   ? new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer)
                   : new ImmutableBufferProxy(sizeof(int), BindFlags.IndexBuffer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LineGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    /// <param name="indexBuffer"></param>
    public LineGeometryBufferModel(IElementsBufferProxy vertexBuffer, IElementsBufferProxy indexBuffer)
        : base(PrimitiveTopology.LineList,
               vertexBuffer,
               indexBuffer) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LineGeometryBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="vertexBuffer"></param>
    /// <param name="indexBuffer"></param>
    public LineGeometryBufferModel(IElementsBufferProxy[] vertexBuffer, IElementsBufferProxy indexBuffer)
        : base(PrimitiveTopology.LineList,
               vertexBuffer,
               indexBuffer) { }
}

/// <summary>
/// </summary>
public class DefaultLineGeometryBufferModel : LineGeometryBufferModel<LinesVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultLineGeometryBufferModel" /> class.
    /// </summary>
    public DefaultLineGeometryBufferModel() : base(LinesVertex.SizeInBytes) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultLineGeometryBufferModel" /> class.
    /// </summary>
    /// <param name="isDynamic"></param>
    public DefaultLineGeometryBufferModel(bool isDynamic) : base(LinesVertex.SizeInBytes, isDynamic) { }

    /// <summary>
    ///     Called when [create vertex buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    /// <param name="bufferIndex"></param>
    protected override void OnCreateVertexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        int bufferIndex,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    ) {
        // -- set geometry if given
        if (geometry is {Positions.Count: > 0}) {
            // --- get geometry
            var data = OnBuildVertexArray(geometry);
            buffer.UploadDataToBuffer(context,
                                      data,
                                      geometry.Positions.Count,
                                      0,
                                      geometry.PreDefinedVertexCount);
        } else {
            //buffer.DisposeAndClear();
            buffer.UploadDataToBuffer(context, Array.Empty<LinesVertex>(), 0);
        }
    }

    protected override bool IsVertexBufferChanged(string? propertyName, int vertexBufferIndex) =>
        base.IsVertexBufferChanged(propertyName, vertexBufferIndex) ||
        propertyName?.Equals(nameof(Geometry3D.Colors), StringComparison.Ordinal) == true;

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
    }

    /// <summary>
    ///     Called when [build vertex array].
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    /// <returns></returns>
    private LinesVertex[] OnBuildVertexArray(Geometry3D geometry) {
        var positions = geometry.Positions
            ?? throw new InvalidOperationException("Line geometry requires positions.");
        var vertexCount = positions.Count;
        var array = ThreadBufferManager<LinesVertex>.GetBuffer(vertexCount);
        var colors = geometry.Colors?.GetEnumerator() ?? 
                     Enumerable.Repeat<Color4>(Color.White, vertexCount).GetEnumerator();

        for (var i = 0; i < vertexCount; i++) {
            colors.MoveNext();
            array[i].Position = new Vector4(positions[i], 1f);
            array[i].Color = colors.Current;
        }

        colors.Dispose();
        return array;
    }
}

/// <summary>
/// </summary>
public sealed class DynamicLineGeometryBufferModel : DefaultLineGeometryBufferModel {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicLineGeometryBufferModel" /> class.
    /// </summary>
    public DynamicLineGeometryBufferModel() : base(true) { }
}
