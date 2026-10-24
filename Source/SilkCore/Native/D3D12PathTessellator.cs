/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct2D;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Adapts native Direct2D stroke triangles to the existing Scene2D sprite transport.</summary>
/// <remarks>No path parsing, line widening or triangulation algorithm is implemented in this adapter.</remarks>
internal static unsafe class D3D12PathTessellator {
    /// <summary>The local-coordinate tolerance; the Screen binding uses physical coordinates and identity scale.</summary>
    private const float FlatteningTolerance = 0.25f;

    /// <summary>The IUnknown interface supported by the synchronous collector.</summary>
    private static readonly Guid UnknownGuid = new("00000000-0000-0000-c000-000000000046");

    /// <summary>The installed Silk tessellation sink interface identifier.</summary>
    private static readonly Guid SinkGuid = ID2D1TessellationSink.Guid;

    /// <summary>The native pointer error reported without throwing through an unmanaged callback.</summary>
    private const int PointerError = unchecked((int) 0x80004003);

    /// <summary>The native interface error reported for unsupported QueryInterface requests.</summary>
    private const int InterfaceError = unchecked((int) 0x80004002);

    /// <summary>The deferred native callback failure reported by Close.</summary>
    private const int CallbackError = unchecked((int) 0x80004005);

    /// <summary>Widens one connected native path and collects its filled outline triangles.</summary>
    /// <param name="geometry">The successfully closed source path.</param>
    /// <param name="width">The full positive stroke width in local coordinates.</param>
    /// <param name="style">The explicit stroke policy, or null for Direct2D's default policy.</param>
    /// <returns>Triangle points in native triangle order, three consecutive points per triangle.</returns>
    /// <exception cref="ArgumentNullException">The source path is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The width is nonpositive or nonfinite.</exception>
    /// <exception cref="ArgumentException">The style belongs to a different factory.</exception>
    /// <remarks>Every HRESULT is checked after the synchronous collector has returned to managed code.</remarks>
    internal static Vector2[] TessellateStroke(PathGeometry geometry, float width, StrokeStyle? style) {
        ArgumentNullException.ThrowIfNull(geometry);
        if (!(width > 0) || !float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width));
        if (style is not null && !ReferenceEquals(style.Factory, geometry.Factory))
            throw new ArgumentException("The stroke style and geometry must use the same factory", nameof(style));

        using var outline = new PathGeometry(geometry.Factory);
        using (var sink = outline.Open()) {
            var nativeStyle = style is null ? null : style.Handle;
            SilkMarshal.ThrowHResult(((ID2D1Geometry*) geometry.Handle)->Widen(width,
                nativeStyle, null, FlatteningTolerance, (ID2D1SimplifiedGeometrySink*) sink.Handle));
            sink.Close();
        }

        var collector = new TriangleCollector();
        var handle = GCHandle.Alloc(collector);
        try {
            var vtable = new SinkVTable {
                QueryInterface = &QueryInterface,
                AddRef = &AddRef,
                Release = &Release,
                AddTriangles = &AddTriangles,
                Close = &Close
            };
            var callback = new SinkCom {
                VTable = &vtable,
                Context = GCHandle.ToIntPtr(handle),
                ReferenceCount = 1
            };
            var sink = (ID2D1TessellationSink*) &callback;
            var tessellateResult = ((ID2D1Geometry*) outline.Handle)->Tessellate(
                null, FlatteningTolerance, sink);
            var closeResult = sink->Close();
            if (collector.Failure is { } failure) ExceptionDispatchInfo.Capture(failure).Throw();
            SilkMarshal.ThrowHResult(tessellateResult);
            SilkMarshal.ThrowHResult(closeResult);
            return [.. collector.Points];
        } finally {
            handle.Free();
        }
    }

    /// <summary>Queries only the two interfaces implemented by this synchronous stack-owned collector.</summary>
    /// <param name="self">The callback object.</param>
    /// <param name="iid">The requested interface identifier.</param>
    /// <param name="result">Receives the same callback pointer on success.</param>
    /// <returns>Success, pointer error or unsupported-interface HRESULT.</returns>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(SinkCom* self, Guid* iid, void** result) {
        if (result == null) return PointerError;
        *result = null;
        if (self == null || iid == null) return PointerError;
        if (*iid != UnknownGuid && *iid != SinkGuid) return InterfaceError;
        *result = self;
        self->ReferenceCount++;
        return 0;
    }

    /// <summary>Retains the callback only for the duration of its synchronous native call.</summary>
    /// <param name="self">The callback object.</param>
    /// <returns>The incremented reference count, or zero for a null pointer.</returns>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(SinkCom* self) => self == null ? 0 : ++self->ReferenceCount;

    /// <summary>Releases a synchronous native reference without freeing stack-owned callback memory.</summary>
    /// <param name="self">The callback object.</param>
    /// <returns>The remaining reference count.</returns>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(SinkCom* self) =>
        self == null || self->ReferenceCount == 0 ? 0 : --self->ReferenceCount;

    /// <summary>Collects native triangles while deferring every managed failure until the native call returns.</summary>
    /// <param name="self">The callback object whose GCHandle retains the collector.</param>
    /// <param name="triangles">The native contiguous triangle array.</param>
    /// <param name="count">The number of native triangles in the array.</param>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void AddTriangles(SinkCom* self, Triangle* triangles, uint count) {
        if (self == null) return;
        if (self->FailureHResult < 0) return;
        TriangleCollector? collector = null;
        try {
            collector = (TriangleCollector?) GCHandle.FromIntPtr(self->Context).Target;
            if (collector is null || (triangles == null && count != 0)) {
                self->FailureHResult = PointerError;
                return;
            }
            for (uint index = 0; index < count; index++) {
                var triangle = triangles[index];
                collector.Add(triangle.Point1);
                collector.Add(triangle.Point2);
                collector.Add(triangle.Point3);
            }
        } catch (Exception exception) {
            self->FailureHResult = CallbackError;
            if (collector is not null) collector.Failure = exception;
        }
    }

    /// <summary>Reports a previously captured callback failure without invoking managed operations.</summary>
    /// <param name="self">The callback object.</param>
    /// <returns>The accumulated callback HRESULT, or a pointer error.</returns>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Close(SinkCom* self) => self == null ? PointerError : self->FailureHResult;

    /// <summary>Retains only triangle points and the first callback failure during one native tessellation.</summary>
    private sealed class TriangleCollector {
        /// <summary>The native triangle points in callback order.</summary>
        internal readonly List<Vector2> Points = [];

        /// <summary>The managed failure to rethrow after returning from native code.</summary>
        internal Exception? Failure;

        /// <summary>Appends a finite native point without changing triangle or figure order.</summary>
        /// <param name="point">The native local-coordinate point.</param>
        /// <exception cref="InvalidOperationException">The native tessellation returned a nonfinite point.</exception>
        internal void Add(Vector2 point) {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                throw new InvalidOperationException("Native path tessellation returned a nonfinite point");
            Points.Add(point);
        }
    }

    /// <summary>The stack-owned COM layout retained through one synchronous native tessellation.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SinkCom {
        /// <summary>The IUnknown and tessellation sink vtable.</summary>
        internal SinkVTable* VTable;

        /// <summary>The GCHandle keeping the managed collector alive.</summary>
        internal nint Context;

        /// <summary>The synchronous native reference count.</summary>
        internal uint ReferenceCount;

        /// <summary>The deferred callback HRESULT returned by Close.</summary>
        internal int FailureHResult;
    }

    /// <summary>The installed ID2D1TessellationSink vtable in native ABI order.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SinkVTable {
        /// <summary>The IUnknown QueryInterface callback.</summary>
        internal delegate* unmanaged[Stdcall]<SinkCom*, Guid*, void**, int> QueryInterface;

        /// <summary>The IUnknown AddRef callback.</summary>
        internal delegate* unmanaged[Stdcall]<SinkCom*, uint> AddRef;

        /// <summary>The IUnknown Release callback.</summary>
        internal delegate* unmanaged[Stdcall]<SinkCom*, uint> Release;

        /// <summary>The native triangle callback.</summary>
        internal delegate* unmanaged[Stdcall]<SinkCom*, Triangle*, uint, void> AddTriangles;

        /// <summary>The native completion callback.</summary>
        internal delegate* unmanaged[Stdcall]<SinkCom*, int> Close;
    }
}