/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DirectWrite;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using DWriteMatrix = Silk.NET.DirectWrite.Matrix;
using DWriteFontStyle = Silk.NET.DirectWrite.FontStyle;
using DWriteFontWeight = Silk.NET.DirectWrite.FontWeight;
using DWriteMeasuringMode = Silk.NET.DirectWrite.MeasuringMode;
using DWriteTextAlignment = Silk.NET.DirectWrite.TextAlignment;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Caches DirectWrite-shaped text runs in one growable RGBA glyph atlas and uploads it for Direct3D 12.
/// </summary>
internal sealed unsafe class D3D12GlyphAtlas : IDisposable {
    /// <summary>
    ///     IUnknown interface identifier accepted by the callback object.
    /// </summary>
    private static readonly Guid IUnknownGuid = new("00000000-0000-0000-c000-000000000046");

    /// <summary>
    ///     IDWritePixelSnapping interface identifier accepted by the callback object.
    /// </summary>
    private static readonly Guid PixelSnappingGuid = new("eaf3a2da-ecf4-4d24-b644-b34f6842024b");

    /// <summary>
    ///     IDWriteTextRenderer interface identifier accepted by the callback object.
    /// </summary>
    private static readonly Guid TextRendererGuid = new("ef8a8135-5cc6-45fe-8825-c5a0724eb819");

    /// <summary>
    ///     Padding retained between cached run images.
    /// </summary>
    private const int Padding = 1;

    /// <summary>
    ///     Native DirectWrite factory used for shaping and glyph analysis.
    /// </summary>
    private readonly DirectWriteFactory factory = new();

    /// <summary>
    ///     Cached atlas entries keyed by every layout-affecting text property.
    /// </summary>
    private readonly Dictionary<GlyphKey, D3D12GlyphAtlasEntry> entries = [];

    /// <summary>
    ///     CPU-side tightly packed RGBA atlas pixels.
    /// </summary>
    private byte[] pixels;

    /// <summary>
    ///     Current atlas width.
    /// </summary>
    private int width;

    /// <summary>
    ///     Current atlas height.
    /// </summary>
    private int height;

    /// <summary>
    ///     Next horizontal shelf position.
    /// </summary>
    private int cursorX = Padding + 1;

    /// <summary>
    ///     Current shelf top.
    /// </summary>
    private int cursorY = Padding;

    /// <summary>
    ///     Tallest item in the current shelf.
    /// </summary>
    private int rowHeight = 1;

    /// <summary>
    ///     Current GPU atlas texture.
    /// </summary>
    private SilkD3D12Resource? texture;

    /// <summary>
    ///     Upload allocation retained until the synchronous presentation fence has completed.
    /// </summary>
    private SilkD3D12Resource? upload;

    /// <summary>
    ///     Whether CPU pixels differ from the current GPU texture.
    /// </summary>
    private bool dirty = true;

    /// <summary>
    ///     Initializes an atlas with a power-of-two testable initial extent.
    /// </summary>
    /// <param name="initialSize">The positive initial width and height.</param>
    internal D3D12GlyphAtlas(int initialSize = 128) {
        if (initialSize < 2) throw new ArgumentOutOfRangeException(nameof(initialSize));
        width = initialSize;
        height = initialSize;
        pixels = new byte[checked(width * height * 4)];
        SetPixel(0, 0, 255);
        White = new D3D12GlyphAtlasEntry(0, 0, 1, 1, 0, 0, 0);
    }

    /// <summary>
    ///     Gets the permanent white texel used by solid-color geometry.
    /// </summary>
    internal D3D12GlyphAtlasEntry White { get; }

    /// <summary>
    ///     Gets the current atlas width.
    /// </summary>
    internal int Width => width;

    /// <summary>
    ///     Gets the current atlas height.
    /// </summary>
    internal int Height => height;

    /// <summary>
    ///     Gets the number of cached shaped text entries.
    /// </summary>
    internal int Count => entries.Count;

    /// <summary>
    ///     Gets the uploaded texture after <see cref="Upload" /> has completed.
    /// </summary>
    internal SilkD3D12Resource Texture => texture
        ?? throw new InvalidOperationException("The glyph atlas has not been uploaded.");

    /// <summary>
    ///     Shapes, rasterizes, packs, or reuses one text run.
    /// </summary>
    /// <param name="text">The Unicode text.</param>
    /// <param name="fontFamily">The DirectWrite font family.</param>
    /// <param name="fontWeight">The font weight.</param>
    /// <param name="fontStyle">The font style.</param>
    /// <param name="fontSize">The physical font size.</param>
    /// <param name="maxWidth">The layout width.</param>
    /// <param name="maxHeight">The layout height.</param>
    /// <param name="alignment">The paragraph alignment.</param>
    /// <param name="flowDirection">The text reading direction.</param>
    /// <returns>The stable atlas entry.</returns>
    internal D3D12GlyphAtlasEntry GetOrCreate(
        string text,
        string fontFamily,
        FontWeight fontWeight,
        FontStyle fontStyle,
        float fontSize,
        float maxWidth,
        float maxHeight,
        TextAlignment alignment,
        FlowDirection flowDirection
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        text.AssertArgumentNotNull();
        fontFamily.AssertArgumentNotNull();
        if (!(fontSize > 0) || !float.IsFinite(fontSize)) throw new ArgumentOutOfRangeException(nameof(fontSize));
        var key = new GlyphKey(text,
            fontFamily,
            fontWeight,
            fontStyle,
            fontSize,
            NormalizeExtent(maxWidth),
            NormalizeExtent(maxHeight),
            alignment,
            flowDirection);
        if (entries.TryGetValue(key, out var cached)) return cached;

        using var format = new TextFormat(factory, fontFamily, fontWeight, fontStyle, fontSize);
        using var layout = new TextLayout(factory, text, format, maxWidth, maxHeight) {
            TextAlignment = alignment,
            FlowDirection = flowDirection
        };
        var collector = new GlyphRunCollector(factory.Handle);
        DrawLayout(layout.Handle, collector);
        var raster = collector.Combine();
        var position = Allocate(raster.Width, raster.Height);
        CopyRaster(raster, position.X, position.Y);
        var entry = new D3D12GlyphAtlasEntry(position.X,
            position.Y,
            raster.Width,
            raster.Height,
            raster.OffsetX,
            raster.OffsetY,
            collector.GlyphCount);
        entries.Add(key, entry);
        return entry;
    }

    /// <summary>
    ///     Recreates and uploads the atlas only after cached pixels change.
    /// </summary>
    /// <param name="device">The native Direct3D 12 device.</param>
    /// <param name="context">The open copy-capable command context.</param>
    internal void Upload(SilkD3D12Device device, SilkD3D12CommandContext context) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!dirty && texture is not null) return;
        var replacement = device.CreateTexture2D(checked((uint) width),
            checked((uint) height),
            Format.FormatR8G8B8A8Unorm);
        SilkD3D12Resource? replacementUpload = null;
        try {
            replacementUpload = device.CreateTextureUploadBuffer(replacement,
                pixels,
                checked((uint) width * 4),
                out var footprint);
            context.CopyBufferToTexture(replacement, replacementUpload, footprint);
            context.Transition(replacement, ResourceStates.PixelShaderResource);
        } catch {
            replacementUpload?.Dispose();
            replacement.Dispose();
            throw;
        }

        upload?.Dispose();
        texture?.Dispose();
        upload = replacementUpload;
        texture = replacement;
        dirty = false;
    }

    /// <summary>
    ///     Releases DirectWrite and Direct3D 12 atlas resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        upload?.Dispose();
        texture?.Dispose();
        factory.Dispose();
        entries.Clear();
        IsDisposed = true;
    }

    /// <summary>
    ///     Gets whether this atlas has released its native resources.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Executes DirectWrite layout drawing through the minimal glyph-run collector COM object.
    /// </summary>
    /// <param name="layout">The shaped native layout.</param>
    /// <param name="collector">The synchronous callback target.</param>
    private static void DrawLayout(IDWriteTextLayout* layout, GlyphRunCollector collector) {
        if (layout == null) return;
        var handle = GCHandle.Alloc(collector);
        try {
            var vtable = new TextRendererVTable {
                QueryInterface = &QueryInterface,
                AddRef = &AddRef,
                Release = &Release,
                IsPixelSnappingDisabled = &IsPixelSnappingDisabled,
                GetCurrentTransform = &GetCurrentTransform,
                GetPixelsPerDip = &GetPixelsPerDip,
                DrawGlyphRun = &DrawGlyphRun,
                DrawUnderline = &DrawUnderline,
                DrawStrikethrough = &DrawStrikethrough,
                DrawInlineObject = &DrawInlineObject
            };
            var renderer = new TextRendererCom {
                VTable = &vtable,
                Context = GCHandle.ToIntPtr(handle),
                ReferenceCount = 1
            };
            SilkMarshal.ThrowHResult(layout->Draw(null, (IDWriteTextRenderer*) &renderer, 0, 0));
        } finally {
            handle.Free();
        }
    }

    /// <summary>
    ///     Allocates one shelf rectangle, growing the atlas without moving existing entries.
    /// </summary>
    /// <param name="entryWidth">The required width.</param>
    /// <param name="entryHeight">The required height.</param>
    /// <returns>The allocated top-left position.</returns>
    private (int X, int Y) Allocate(int entryWidth, int entryHeight) {
        if (entryWidth <= 0 || entryHeight <= 0) return (0, 0);
        EnsureSize(entryWidth + Padding * 2, entryHeight + Padding * 2);
        if (cursorX + entryWidth + Padding > width) {
            cursorX = Padding;
            cursorY += rowHeight + Padding;
            rowHeight = 0;
        }
        EnsureSize(width, cursorY + entryHeight + Padding);
        var result = (cursorX, cursorY);
        cursorX += entryWidth + Padding;
        rowHeight = Math.Max(rowHeight, entryHeight);
        return result;
    }

    /// <summary>
    ///     Doubles atlas dimensions until one extent fits while preserving pixel coordinates.
    /// </summary>
    /// <param name="requiredWidth">The required width.</param>
    /// <param name="requiredHeight">The required height.</param>
    private void EnsureSize(int requiredWidth, int requiredHeight) {
        var nextWidth = width;
        var nextHeight = height;
        while (nextWidth < requiredWidth) nextWidth = checked(nextWidth * 2);
        while (nextHeight < requiredHeight) nextHeight = checked(nextHeight * 2);
        if (nextWidth == width && nextHeight == height) return;
        var replacement = new byte[checked(nextWidth * nextHeight * 4)];
        for (var row = 0; row < height; row++)
            pixels.AsSpan(row * width * 4, width * 4).CopyTo(replacement.AsSpan(row * nextWidth * 4));
        pixels = replacement;
        width = nextWidth;
        height = nextHeight;
        dirty = true;
    }

    /// <summary>
    ///     Copies one grayscale raster into white RGBA atlas texels.
    /// </summary>
    /// <param name="raster">The combined raster.</param>
    /// <param name="destinationX">The atlas left position.</param>
    /// <param name="destinationY">The atlas top position.</param>
    private void CopyRaster(GlyphRaster raster, int destinationX, int destinationY) {
        if (raster.Width == 0 || raster.Height == 0) return;
        for (var y = 0; y < raster.Height; y++)
        for (var x = 0; x < raster.Width; x++)
            SetPixel(destinationX + x,
                destinationY + y,
                raster.Alpha[y * raster.Width + x]);
        dirty = true;
    }

    /// <summary>
    ///     Writes one white texel with the supplied alpha.
    /// </summary>
    /// <param name="x">The pixel x coordinate.</param>
    /// <param name="y">The pixel y coordinate.</param>
    /// <param name="alpha">The alpha coverage.</param>
    private void SetPixel(int x, int y, byte alpha) {
        var offset = checked((y * width + x) * 4);
        pixels[offset] = 255;
        pixels[offset + 1] = 255;
        pixels[offset + 2] = 255;
        pixels[offset + 3] = alpha;
    }

    /// <summary>
    ///     Normalizes unbounded layout extents for stable dictionary equality.
    /// </summary>
    /// <param name="value">The source extent.</param>
    /// <returns>The normalized extent.</returns>
    private static float NormalizeExtent(float value) => float.IsInfinity(value) || value <= 0
        ? float.MaxValue
        : value;

    /// <summary>
    ///     Returns the managed collector associated with one callback object.
    /// </summary>
    /// <param name="self">The callback object.</param>
    /// <returns>The collector.</returns>
    private static GlyphRunCollector GetCollector(TextRendererCom* self) =>
        (GlyphRunCollector) (GCHandle.FromIntPtr(self->Context).Target
                             ?? throw new InvalidOperationException("The glyph collector was released."));

    /// <summary>
    ///     Supports the IUnknown and text-renderer interface queries made by DirectWrite.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(TextRendererCom* self, Guid* iid, void** result) {
        if (iid == null || result == null) return unchecked((int) 0x80004003);
        if (*iid != IUnknownGuid && *iid != PixelSnappingGuid && *iid != TextRendererGuid) {
            *result = null;
            return unchecked((int) 0x80004002);
        }
        *result = self;
        self->ReferenceCount++;
        return 0;
    }

    /// <summary>
    ///     Increments the synchronous callback reference count.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(TextRendererCom* self) => ++self->ReferenceCount;

    /// <summary>
    ///     Decrements the synchronous callback reference count.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(TextRendererCom* self) => self->ReferenceCount == 0 ? 0 : --self->ReferenceCount;

    /// <summary>
    ///     Enables DirectWrite pixel snapping.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IsPixelSnappingDisabled(TextRendererCom* self, void* context, int* disabled) {
        if (disabled == null) return unchecked((int) 0x80004003);
        *disabled = 0;
        return 0;
    }

    /// <summary>
    ///     Returns the identity transform used for atlas rasterization.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetCurrentTransform(TextRendererCom* self, void* context, DWriteMatrix* transform) {
        if (transform == null) return unchecked((int) 0x80004003);
        *transform = default;
        transform->M11 = 1;
        transform->M22 = 1;
        return 0;
    }

    /// <summary>
    ///     Returns one physical pixel per device-independent pixel.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetPixelsPerDip(TextRendererCom* self, void* context, float* pixelsPerDip) {
        if (pixelsPerDip == null) return unchecked((int) 0x80004003);
        *pixelsPerDip = 1;
        return 0;
    }

    /// <summary>
    ///     Rasterizes one already-shaped DirectWrite glyph run.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawGlyphRun(
        TextRendererCom* self,
        void* context,
        float baselineOriginX,
        float baselineOriginY,
        DWriteMeasuringMode measuringMode,
        GlyphRun* glyphRun,
        GlyphRunDescription* description,
        IUnknown* drawingEffect
    ) {
        try {
            GetCollector(self).Add(glyphRun, baselineOriginX, baselineOriginY, measuringMode);
            return 0;
        } catch (Exception exception) {
            GetCollector(self).Failure = exception;
            return unchecked((int) 0x80004005);
        }
    }

    /// <summary>
    ///     Accepts underline callbacks; underline geometry is outside the glyph atlas contract.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawUnderline(TextRendererCom* self,
        void* context,
        float baselineOriginX,
        float baselineOriginY,
        Underline* underline,
        IUnknown* drawingEffect) => 0;

    /// <summary>
    ///     Accepts strikethrough callbacks; decoration geometry is outside the glyph atlas contract.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawStrikethrough(TextRendererCom* self,
        void* context,
        float baselineOriginX,
        float baselineOriginY,
        Strikethrough* strikethrough,
        IUnknown* drawingEffect) => 0;

    /// <summary>
    ///     Accepts inline-object callbacks that contain no glyph data.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawInlineObject(TextRendererCom* self,
        void* context,
        float originX,
        float originY,
        IDWriteInlineObject* inlineObject,
        Bool32 isSideways,
        Bool32 isRightToLeft,
        IUnknown* drawingEffect) => 0;

    /// <summary>
    ///     Identifies a cached shaped text layout.
    /// </summary>
    /// <param name="Text">The Unicode text.</param>
    /// <param name="FontFamily">The font family.</param>
    /// <param name="FontWeight">The font weight.</param>
    /// <param name="FontStyle">The font style.</param>
    /// <param name="FontSize">The physical font size.</param>
    /// <param name="MaxWidth">The normalized layout width.</param>
    /// <param name="MaxHeight">The normalized layout height.</param>
    /// <param name="Alignment">The text alignment.</param>
    /// <param name="FlowDirection">The flow direction.</param>
    private readonly record struct GlyphKey(
        string Text,
        string FontFamily,
        FontWeight FontWeight,
        FontStyle FontStyle,
        float FontSize,
        float MaxWidth,
        float MaxHeight,
        TextAlignment Alignment,
        FlowDirection FlowDirection
    );

    /// <summary>
    ///     Managed target for DirectWrite glyph-run callbacks.
    /// </summary>
    private sealed class GlyphRunCollector {
        /// <summary>
        ///     Native factory borrowed for the synchronous draw callback.
        /// </summary>
        private readonly IDWriteFactory* factory;

        /// <summary>
        ///     Rasterized runs emitted by the shaped text layout.
        /// </summary>
        private readonly List<GlyphRaster> runs = [];

        /// <summary>
        ///     Initializes one collector.
        /// </summary>
        /// <param name="factory">The borrowed DirectWrite factory.</param>
        internal GlyphRunCollector(IDWriteFactory* factory) => this.factory = factory;

        /// <summary>
        ///     Gets the total shaped glyph count.
        /// </summary>
        internal int GlyphCount { get; private set; }

        /// <summary>
        ///     Gets or sets a callback exception that could not cross the COM boundary.
        /// </summary>
        internal Exception? Failure { get; set; }

        /// <summary>
        ///     Rasterizes one shaped run with native glyph analysis.
        /// </summary>
        /// <param name="glyphRun">The shaped glyph run.</param>
        /// <param name="originX">The baseline x origin.</param>
        /// <param name="originY">The baseline y origin.</param>
        /// <param name="measuringMode">The shaped measuring mode.</param>
        internal void Add(GlyphRun* glyphRun, float originX, float originY, DWriteMeasuringMode measuringMode) {
            if (glyphRun == null || glyphRun->GlyphCount == 0) return;
            IDWriteGlyphRunAnalysis* analysis = null;
            SilkMarshal.ThrowHResult(factory->CreateGlyphRunAnalysis(glyphRun,
                1,
                (DWriteMatrix*) null,
                RenderingMode.NaturalSymmetric,
                measuringMode,
                originX,
                originY,
                &analysis));
            try {
                Box2D<int> bounds = default;
                SilkMarshal.ThrowHResult(analysis->GetAlphaTextureBounds(TextureType.Cleartype3x1, &bounds));
                var rasterWidth = Math.Max(0, bounds.Max.X - bounds.Min.X);
                var rasterHeight = Math.Max(0, bounds.Max.Y - bounds.Min.Y);
                var clearType = new byte[checked(rasterWidth * rasterHeight * 3)];
                if (clearType.Length > 0)
                    fixed (byte* destination = clearType)
                        SilkMarshal.ThrowHResult(analysis->CreateAlphaTexture(TextureType.Cleartype3x1,
                            &bounds,
                            destination,
                            checked((uint) clearType.Length)));
                var alpha = new byte[checked(rasterWidth * rasterHeight)];
                for (var index = 0; index < alpha.Length; index++)
                    alpha[index] = Math.Max(clearType[index * 3],
                        Math.Max(clearType[index * 3 + 1], clearType[index * 3 + 2]));
                runs.Add(new GlyphRaster(bounds.Min.X, bounds.Min.Y, rasterWidth, rasterHeight, alpha));
                GlyphCount = checked(GlyphCount + (int) glyphRun->GlyphCount);
            } finally {
                analysis->Release();
            }
        }

        /// <summary>
        ///     Merges all shaped run rasters into one cacheable image.
        /// </summary>
        /// <returns>The combined run raster.</returns>
        internal GlyphRaster Combine() {
            if (Failure is not null) throw new InvalidOperationException("DirectWrite glyph rasterization failed.", Failure);
            if (runs.Count == 0) return new GlyphRaster(0, 0, 0, 0, []);
            var left = runs.Min(run => run.OffsetX);
            var top = runs.Min(run => run.OffsetY);
            var right = runs.Max(run => run.OffsetX + run.Width);
            var bottom = runs.Max(run => run.OffsetY + run.Height);
            var combinedWidth = right - left;
            var combinedHeight = bottom - top;
            var combined = new byte[checked(combinedWidth * combinedHeight)];
            foreach (var run in runs)
                for (var y = 0; y < run.Height; y++)
                for (var x = 0; x < run.Width; x++) {
                    var destination = (run.OffsetY - top + y) * combinedWidth + run.OffsetX - left + x;
                    combined[destination] = Math.Max(combined[destination], run.Alpha[y * run.Width + x]);
                }
            return new GlyphRaster(left, top, combinedWidth, combinedHeight, combined);
        }
    }

    /// <summary>
    ///     Stores one positioned grayscale run image.
    /// </summary>
    /// <param name="OffsetX">The layout-relative left bound.</param>
    /// <param name="OffsetY">The layout-relative top bound.</param>
    /// <param name="Width">The raster width.</param>
    /// <param name="Height">The raster height.</param>
    /// <param name="Alpha">The grayscale coverage.</param>
    private readonly record struct GlyphRaster(int OffsetX, int OffsetY, int Width, int Height, byte[] Alpha);

    /// <summary>
    ///     Memory layout of the synchronous IDWriteTextRenderer callback object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct TextRendererCom {
        /// <summary>The callback vtable.</summary>
        internal TextRendererVTable* VTable;

        /// <summary>The managed collector handle.</summary>
        internal nint Context;

        /// <summary>The callback reference count.</summary>
        internal uint ReferenceCount;
    }

    /// <summary>
    ///     Native IDWriteTextRenderer vtable used only during one layout draw.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct TextRendererVTable {
        /// <summary>The QueryInterface callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, Guid*, void**, int> QueryInterface;

        /// <summary>The AddRef callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, uint> AddRef;

        /// <summary>The Release callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, uint> Release;

        /// <summary>The pixel-snapping callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, int*, int> IsPixelSnappingDisabled;

        /// <summary>The current-transform callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, DWriteMatrix*, int> GetCurrentTransform;

        /// <summary>The pixel-density callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, float*, int> GetPixelsPerDip;

        /// <summary>The shaped glyph-run callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, float, float, DWriteMeasuringMode, GlyphRun*,
            GlyphRunDescription*, IUnknown*, int> DrawGlyphRun;

        /// <summary>The underline callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, float, float, Underline*, IUnknown*, int>
            DrawUnderline;

        /// <summary>The strikethrough callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, float, float, Strikethrough*, IUnknown*, int>
            DrawStrikethrough;

        /// <summary>The inline-object callback.</summary>
        internal delegate* unmanaged[Stdcall]<TextRendererCom*, void*, float, float, IDWriteInlineObject*, Bool32,
            Bool32, IUnknown*, int> DrawInlineObject;
    }
}

/// <summary>
///     Describes one shaped text image in the current glyph atlas.
/// </summary>
internal sealed class D3D12GlyphAtlasEntry {
    /// <summary>
    ///     Initializes one immutable atlas entry.
    /// </summary>
    /// <param name="x">The atlas x coordinate.</param>
    /// <param name="y">The atlas y coordinate.</param>
    /// <param name="width">The raster width.</param>
    /// <param name="height">The raster height.</param>
    /// <param name="offsetX">The layout-relative raster x offset.</param>
    /// <param name="offsetY">The layout-relative raster y offset.</param>
    /// <param name="glyphCount">The shaped glyph count.</param>
    internal D3D12GlyphAtlasEntry(int x,
        int y,
        int width,
        int height,
        int offsetX,
        int offsetY,
        int glyphCount) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        OffsetX = offsetX;
        OffsetY = offsetY;
        GlyphCount = glyphCount;
    }

    /// <summary>Gets the atlas x coordinate.</summary>
    internal int X { get; }

    /// <summary>Gets the atlas y coordinate.</summary>
    internal int Y { get; }

    /// <summary>Gets the raster width.</summary>
    internal int Width { get; }

    /// <summary>Gets the raster height.</summary>
    internal int Height { get; }

    /// <summary>Gets the layout-relative raster x offset.</summary>
    internal int OffsetX { get; }

    /// <summary>Gets the layout-relative raster y offset.</summary>
    internal int OffsetY { get; }

    /// <summary>Gets the number of shaped glyphs represented by this entry.</summary>
    internal int GlyphCount { get; }
}
