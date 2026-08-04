/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Core2D;
/// <summary>
///     <see href="https://jeremiahmorrill.wordpress.com/2013/02/06/direct2d-gui-librarygraphucks/" />
/// </summary>
public class Figure {
    /// <summary>
    ///     Initializes a new instance of the <see cref="Figure" /> class.
    /// </summary>
    /// <param name="startPoint">The start point.</param>
    /// <param name="filled">if set to <c>true</c> [filled].</param>
    /// <param name="closed">if set to <c>true</c> [closed].</param>
    public Figure(Vector2 startPoint, bool filled, bool closed) {
        StartPoint = startPoint;
        Filled = filled;
        Closed = closed;
    }

    private List<SegmentData> Segments { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="Figure" /> is closed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if closed; otherwise, <c>false</c>.
    /// </value>
    public bool Closed { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="Figure" /> is filled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if filled; otherwise, <c>false</c>.
    /// </value>
    public bool Filled { get; }

    /// <summary>
    ///     Gets or sets the start point.
    /// </summary>
    /// <value>
    ///     The start point.
    /// </value>
    public Vector2 StartPoint { get; }

    /// <summary>
    ///     Adds the segment.
    /// </summary>
    /// <param name="segment">The segment.</param>
    /// <param name="isStroked">if set to <c>true</c> [is stroked].</param>
    /// <param name="isSmoothJoined">if set to <c>true</c> [is smooth joined].</param>
    public void AddSegment(ISegment segment, bool isStroked = true, bool isSmoothJoined = true) {
        Segments.Add(new SegmentData(segment, isStroked, isSmoothJoined));
    }

    /// <summary>
    ///     Creates the specified sink.
    /// </summary>
    /// <param name="sink">The sink.</param>
    public void Create(GeometrySink sink) {
        sink.BeginFigure(StartPoint, Filled ? FigureBegin.Filled : FigureBegin.Hollow);
        for (var i = 0; i < Segments.Count; ++i) {
            var flag = PathSegment.None;
            var segment = Segments[i];
            if (!segment.IsStroked) flag |= PathSegment.ForceUnstroked;
            if (segment.IsSmoothJoined) flag |= PathSegment.ForceRoundLineJoin;
            sink.SetSegmentFlags(flag);
            segment.Segment.Create(sink);
        }

        sink.EndFigure(Closed ? FigureEnd.Closed : FigureEnd.Open);
    }

    /// <summary>
    /// </summary>
    private struct SegmentData {
        public readonly ISegment Segment;
        public readonly bool IsStroked;
        public readonly bool IsSmoothJoined;

        public SegmentData(ISegment segment, bool isStroked, bool isSmoothJoined) {
            Segment = segment;
            IsStroked = isStroked;
            IsSmoothJoined = isSmoothJoined;
        }
    }
}
