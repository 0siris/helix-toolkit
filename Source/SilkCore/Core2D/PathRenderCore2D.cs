using HelixToolkit.SharpDX.Core;
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

namespace HelixToolkit.SharpDX.Core.Core2D;

/// <summary>Caches the existing Figure commands as native geometry for the existing 2D render transports.</summary>
/// <remarks>Replace the Figures list after changing a figure; in-place list edits do not invalidate geometry.</remarks>
public class PathRenderCore2D : ShapeRenderCore2DBase {
    /// <summary>The successfully closed native path cached for the most recent figure publication.</summary>
    protected PathGeometry? Geometry;

    /// <summary>Whether the next consumer must rebuild native geometry before drawing.</summary>
    protected bool IsGeometryChanged = true;

    /// <summary>
    ///     Gets or replaces the derived figures, invalidating native geometry before notifying render observers.
    /// </summary>
    /// <value>
    ///     The derived command list, or null for no path. In-place list or Figure edits are not observed.
    /// </value>
    public List<Figure>? Figures {
        get;
        set {
            if (ReferenceEquals(field, value)) return;
            IsGeometryChanged = true;
            SetAffectsRender(ref field, value);
        }
    } = [];

    /// <summary>
    ///     Gets or sets the native geometry fill rule, invalidating cached geometry before render notification.
    /// </summary>
    /// <value>
    ///     The native fill rule used when the figure commands are rebuilt.
    /// </value>
    public D2DFillMode FillMode {
        get;
        set {
            if (field == value) return;
            IsGeometryChanged = true;
            SetAffectsRender(ref field, value);
        }
    } = D2DFillMode.Alternate;

    /// <summary>
    ///     Marks cached geometry dirty before reattaching the existing shape transport.
    /// </summary>
    /// <returns>Whether the existing shape transport attached successfully.</returns>
    protected override bool OnAttach() {
        IsGeometryChanged = true;
        return base.OnAttach();
    }

    /// <summary>
    ///     Passes successfully rebuilt geometry to the existing device-context transport hooks.
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnRender(RenderContext2D context) {
        var geometry = EnsureGeometry(context.DeviceResources.Factory2D);
        if (geometry is null) return;
        if (StrokeBrush != null && StrokeWidth > 0 && StrokeStyle != null)
            context.DeviceContext.DrawGeometry(geometry, StrokeBrush, StrokeWidth, StrokeStyle);
        if (FillBrush != null) context.DeviceContext.FillGeometry(geometry, FillBrush);
    }

    /// <summary>Builds the current figures once, publishing only a successfully closed native path.</summary>
    /// <param name="factory">The borrowed native geometry factory for this consumer.</param>
    /// <returns>The cached closed path, or null when the current figures are absent or empty.</returns>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    /// <exception cref="ObjectDisposedException">This core or the factory is disposed.</exception>
    /// <remarks>A failed rebuild discards the prior geometry and retries only on a later consumer call.</remarks>
    internal PathGeometry? EnsureGeometry(D2DFactory factory) {
        ArgumentNullException.ThrowIfNull(factory);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ObjectDisposedException.ThrowIf(factory.IsDisposed, factory);
        if (!IsGeometryChanged && Geometry is { IsDisposed: false } existing
            && ReferenceEquals(existing.Factory, factory)) return existing;

        RemoveAndDispose(ref Geometry);
        IsGeometryChanged = true;
        if (Figures is null || Figures.Count == 0) {
            IsGeometryChanged = false;
            return null;
        }

        var geometry = new PathGeometry(factory);
        try {
            using var sink = geometry.Open();
            sink.SetFillMode(FillMode);
            foreach (var figure in Figures) figure.Create(sink);
            sink.Close();
            Geometry = geometry;
            IsGeometryChanged = false;
            return geometry;
        } catch {
            geometry.Dispose();
            throw;
        }
    }

    /// <summary>Discards native geometry and the existing owned shape resources on detach.</summary>
    protected override void OnDetach() {
        RemoveAndDispose(ref Geometry);
        IsGeometryChanged = true;
        base.OnDetach();
    }

    /// <summary>Releases cached geometry and shape resources even if this direct core was never attached.</summary>
    /// <param name="disposeManagedResources">Whether the managed disposal path is active.</param>
    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref Geometry);
        base.OnDispose(disposeManagedResources);
        FillBrush = null;
        StrokeBrush = null;
        StrokeStyle = null;
    }
}
