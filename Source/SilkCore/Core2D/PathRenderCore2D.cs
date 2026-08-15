/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Core2D.Models;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core2D;

/// <summary>
/// </summary>
public class PathRenderCore2D : ShapeRenderCore2DBase {
    /// <summary>
    ///     The geometry
    /// </summary>
    protected PathGeometry? Geometry;

    /// <summary>
    ///     The is geometry changed
    /// </summary>
    protected bool IsGeometryChanged = true;

    /// <summary>
    ///     Gets or sets the figures.
    /// </summary>
    /// <value>
    ///     The figures.
    /// </value>
    public List<Figure>? Figures {
        get;
        set {
            if (SetAffectsRender(ref field, value)) IsGeometryChanged = true;
        }
    } = [];

    /// <summary>
    ///     Gets or sets the fill mode.
    /// </summary>
    /// <value>
    ///     The fill mode.
    /// </value>
    public D2DFillMode FillMode {
        get;
        set {
            if (SetAffectsRender(ref field, value)) IsGeometryChanged = true;
        }
    } = D2DFillMode.Alternate;

    /// <summary>
    ///     Called when [attach].
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderHost host) {
        IsGeometryChanged = true;
        return base.OnAttach(host);
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    [SuppressMessage("Microsoft.Usage",
        "CA2202: Do not dispose objects multiple times",
        Justification = "False positive.")]
    protected override void OnRender(RenderContext2D context) {
        if (IsGeometryChanged) {
            RemoveAndDispose(ref Geometry);
            if (Figures == null || Figures.Count == 0) return;
            Geometry = new PathGeometry(context.DeviceResources.Factory2D);
            using (var sink = Geometry.Open()) {
                sink.SetFillMode(FillMode);
                foreach (var figure in Figures) figure.Create(sink);
                sink.Close();
            }

            IsGeometryChanged = false;
        }

        if (Geometry is not { } geometry) return;
        if (StrokeBrush != null && StrokeWidth > 0 && StrokeStyle != null)
            context.DeviceContext.DrawGeometry(geometry, StrokeBrush, StrokeWidth, StrokeStyle);
        if (FillBrush != null) context.DeviceContext.FillGeometry(geometry, FillBrush);
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref Geometry);
        base.OnDetach();
    }
}
