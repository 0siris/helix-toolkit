/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Records existing Scene2D shapes, images, and DirectWrite-shaped text through the repository Sprite2D pass.
/// </summary>
internal sealed class D3D12Scene2DRenderer : IDisposable {
    /// <summary>
    ///     Segment count used by ellipse and rounded-rectangle geometry.
    /// </summary>
    private const int CurveSegments = 32;

    /// <summary>
    ///     Existing sprite texture register.
    /// </summary>
    private const int TextureRegister = 50;

    /// <summary>
    ///     Existing sprite sampler register.
    /// </summary>
    private const int SamplerRegister = 8;

    /// <summary>
    ///     Native allocation device.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     Borrowed host-lifetime resource manager.
    /// </summary>
    private readonly SilkD3D12ResourceManager resources;

    /// <summary>
    ///     Shared shader-visible resource heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     Shared shader-visible sampler heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// <summary>
    ///     DirectWrite-shaped text atlas, also containing the solid white texel.
    /// </summary>
    private readonly D3D12GlyphAtlas atlas = new();

    /// <summary>
    ///     Frame-stable descriptor and upload-buffer slots.
    /// </summary>
    private readonly List<BindingSlot> bindingSlots = [];

    /// <summary>
    ///     Texture models keyed by the existing image stream identity.
    /// </summary>
    private readonly Dictionary<Stream, TextureModel> images = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     Reused frame draw list in exact preorder.
    /// </summary>
    private readonly List<D3D12Scene2DDraw> draws = [];

    /// <summary>
    ///     Next binding slot consumed in this frame.
    /// </summary>
    private int bindingSlotIndex;

    /// <summary>
    ///     Initializes one native Scene2D renderer over host-owned resources.
    /// </summary>
    /// <param name="device">The native device.</param>
    /// <param name="resources">The shared geometry and texture manager.</param>
    /// <param name="resourceHeap">The shared CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The shared sampler heap.</param>
    internal D3D12Scene2DRenderer(
        SilkD3D12Device device,
        SilkD3D12ResourceManager resources,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        this.device = device;
        this.resources = resources;
        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
    }

    /// <summary>
    ///     Gets the prepared draws for focused ordering and clipping tests.
    /// </summary>
    internal IReadOnlyList<D3D12Scene2DDraw> Draws => draws;

    /// <summary>
    ///     Gets the shared glyph atlas for focused cache and growth tests.
    /// </summary>
    internal D3D12GlyphAtlas Atlas => atlas;

    /// <summary>
    ///     Recycles frame-local slots after the preceding presentation fence has completed.
    /// </summary>
    internal void BeginFrame() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        bindingSlotIndex = 0;
        draws.Clear();
    }

    /// <summary>
    ///     Prepares visible Scene2D nodes in exact parent-before-child order.
    /// </summary>
    /// <param name="roots">The existing Scene2D roots.</param>
    /// <param name="width">The physical viewport width.</param>
    /// <param name="height">The physical viewport height.</param>
    /// <param name="dpiScale">The physical-pixel scale used by text.</param>
    /// <returns>The number of prepared draws.</returns>
    internal int Prepare(IEnumerable<SceneNode2D> roots, uint width, uint height, float dpiScale) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        roots.AsGuardNotNull();
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        if (!(dpiScale > 0) || !float.IsFinite(dpiScale)) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        draws.Clear();
        var viewport = new RectangleF(0, 0, width, height);
        foreach (var root in roots) Traverse(root, viewport, viewport, dpiScale);
        return draws.Count;
    }

    /// <summary>
    ///     Uploads prepared text and records all prepared draws into one presentation target.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The existing Sprite2D pass.</param>
    /// <param name="target">The presentation render-target descriptor.</param>
    /// <param name="width">The physical target width.</param>
    /// <param name="height">The physical target height.</param>
    /// <returns>The number of recorded draws.</returns>
    internal int RenderPrepared(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        SilkD3D12Descriptor target,
        uint width,
        uint height
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (draws.Count == 0) return 0;
        atlas.Upload(device, context);
        var transforms = CreateTransforms(width, height);
        context.SetRenderTarget(target);
        context.SetViewport(width, height);
        var recorded = 0;
        foreach (var draw in draws) {
            var texture = draw.Image is { } image
                ? resources.GetOrCreate(context, image).Resource
                : atlas.Texture;
            context.Transition(texture, ResourceStates.PixelShaderResource);
            UpdateAtlasUvs(draw);
            var slot = NextBindingSlot();
            slot.Update(draw.Vertices, draw.Indices, in transforms);
            device.CreateShaderResourceView(texture, slot.Bindings.ShaderResource(TextureRegister));
            var clip = draw.Clip;
            context.SetScissorRectangle(checked((int) clip.Left),
                checked((int) clip.Top),
                checked((int) clip.Right),
                checked((int) clip.Bottom));
            pass.BindShader(context);
            slot.Bindings.BindGraphics(context);
            context.SetVertexBuffer(0,
                slot.VertexBuffer,
                SpriteStruct.SizeInBytes,
                checked((uint) draw.Vertices.Length * SpriteStruct.SizeInBytes));
            context.SetIndexBuffer(slot.IndexBuffer,
                Format.FormatR32Uint,
                checked((uint) draw.Indices.Length * sizeof(uint)));
            context.DrawIndexedInstanced(checked((uint) draw.Indices.Length));
            recorded++;
        }
        return recorded;
    }

    /// <summary>
    ///     Releases atlas and frame-local upload resources.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var slot in bindingSlots) slot.Dispose();
        bindingSlots.Clear();
        images.Clear();
        draws.Clear();
        atlas.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Gets whether all owned native resources have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Traverses one node, inheriting and intersecting axis-aligned physical clipping.
    /// </summary>
    /// <param name="node">The current node.</param>
    /// <param name="inheritedClip">The parent clip.</param>
    /// <param name="viewport">The full viewport.</param>
    /// <param name="dpiScale">The physical text scale.</param>
    private void Traverse(SceneNode2D node, RectangleF inheritedClip, RectangleF viewport, float dpiScale) {
        if (node.Visibility != Visibility.Visible) return;
        node.UpdateTransform();
        var clip = inheritedClip;
        if (node.ClipEnabled || node.ClipToBound)
            clip = Intersect(clip, TransformBounds(node.LayoutClipBound, node.TotalModelMatrix));
        clip = Intersect(clip, viewport);
        if (clip.Width <= 0 || clip.Height <= 0) return;
        AddNode(node, clip, dpiScale);
        foreach (var child in node.Items) Traverse(child, clip, viewport, dpiScale);
    }

    /// <summary>
    ///     Converts one supported render core into one or more ordered sprite draws.
    /// </summary>
    /// <param name="node">The source node.</param>
    /// <param name="clip">The inherited physical clip.</param>
    /// <param name="dpiScale">The physical text scale.</param>
    private void AddNode(SceneNode2D node, RectangleF clip, float dpiScale) {
        var core = node.RenderCore;
        switch (core) {
            case RectangleRenderCore2D rectangle:
                AddRectangle(node, rectangle, clip);
                break;
            case EllipseRenderCore2D ellipse:
                AddEllipse(node, ellipse, clip);
                break;
            case BorderRenderCore2D border:
                AddBorder(node, border, clip);
                break;
            case TextRenderCore2D text:
                AddText(node, text, clip, dpiScale);
                break;
            case ImageRenderCore2D when node is ImageNode2D image:
                AddImage(image, clip);
                break;
        }
    }

    /// <summary>
    ///     Adds rectangle fill and stroke geometry.
    /// </summary>
    private void AddRectangle(SceneNode2D node, ShapeRenderCore2DBase core, RectangleF clip) {
        if (TryGetColor(core.FillBrush, out var fill))
            AddQuad(node, node.LayoutBound, fill, clip, atlas.White);
        if (core.StrokeWidth > 0 && TryGetColor(core.StrokeBrush, out var stroke))
            AddRectangleRing(node, node.LayoutBound, core.StrokeWidth, stroke, clip);
    }

    /// <summary>
    ///     Adds ellipse fill and stroke geometry.
    /// </summary>
    private void AddEllipse(SceneNode2D node, ShapeRenderCore2DBase core, RectangleF clip) {
        if (TryGetColor(core.FillBrush, out var fill))
            AddEllipseGeometry(node, node.LayoutBound, 0, fill, clip);
        if (core.StrokeWidth > 0 && TryGetColor(core.StrokeBrush, out var stroke))
            AddEllipseGeometry(node, node.LayoutBound, core.StrokeWidth, stroke, clip);
    }

    /// <summary>
    ///     Adds rounded border background and per-side ring geometry.
    /// </summary>
    private void AddBorder(SceneNode2D node, BorderRenderCore2D core, RectangleF clip) {
        if (TryGetColor(core.Background, out var background))
            AddRoundedGeometry(node, node.LayoutBound, core.CornerRadius, default, background, clip, false);
        if (TryGetColor(core.StrokeBrush, out var stroke) && core.BorderThickness.LengthSquared() > 0)
            AddRoundedGeometry(node,
                node.LayoutBound,
                core.CornerRadius,
                core.BorderThickness,
                stroke,
                clip,
                true);
    }

    /// <summary>
    ///     Adds optional text background followed by its shaped glyph image.
    /// </summary>
    private void AddText(SceneNode2D node, TextRenderCore2D core, RectangleF clip, float dpiScale) {
        if (TryGetColor(core.Background, out var background))
            AddQuad(node, node.LayoutBound, background, clip, atlas.White);
        if (string.IsNullOrEmpty(core.Text) || !TryGetColor(core.Foreground, out var foreground)) return;
        var entry = atlas.GetOrCreate(core.Text,
            core.FontFamily,
            core.FontWeight,
            core.FontStyle,
            core.FontSize * dpiScale,
            core.MaxWidth,
            core.MaxHeight,
            core.TextAlignment,
            core.FlowDirection);
        if (entry.Width == 0 || entry.Height == 0) return;
        var bounds = new RectangleF(node.LayoutBound.Left + entry.OffsetX,
            node.LayoutBound.Top + entry.OffsetY,
            entry.Width,
            entry.Height);
        AddQuad(node, bounds, foreground, clip, entry);
    }

    /// <summary>
    ///     Adds one existing image stream as a textured quad.
    /// </summary>
    private void AddImage(ImageNode2D node, RectangleF clip) {
        if (node.ImageStream is not { } stream || node.Opacity <= 0) return;
        if (!images.TryGetValue(stream, out var image)) {
            image = TextureModel.Create(stream)
                    ?? throw new InvalidOperationException("The Scene2D image stream could not be loaded.");
            images.Add(stream, image);
        }
        AddQuad(node,
            node.LayoutBound,
            new Color4(1, 1, 1, Math.Clamp(node.Opacity, 0, 1)),
            clip,
            null,
            image);
    }

    /// <summary>
    ///     Adds one transformed quad using either an atlas entry or a standalone image.
    /// </summary>
    private void AddQuad(SceneNode2D node,
        RectangleF bounds,
        Color4 color,
        RectangleF clip,
        D3D12GlyphAtlasEntry? entry,
        TextureModel? image = null) {
        var uv = entry is null ? new RectangleF(0, 0, 1, 1) : default;
        var vertices = CreateQuadVertices(bounds, node.TotalModelMatrix, color, uv);
        draws.Add(new D3D12Scene2DDraw(node,
            vertices,
            [0, 1, 2, 0, 2, 3],
            ToScissor(clip),
            image,
            entry));
    }

    /// <summary>
    ///     Adds a four-sided rectangle stroke without overlapping the fill center.
    /// </summary>
    private void AddRectangleRing(SceneNode2D node,
        RectangleF bounds,
        float thickness,
        Color4 color,
        RectangleF clip) {
        var width = Math.Min(thickness, bounds.Width / 2);
        var height = Math.Min(thickness, bounds.Height / 2);
        List<SpriteStruct> vertices = [];
        List<uint> indices = [];
        AppendQuad(vertices, indices, new RectangleF(bounds.Left, bounds.Top, bounds.Width, height), node, color);
        AppendQuad(vertices,
            indices,
            new RectangleF(bounds.Left, bounds.Bottom - height, bounds.Width, height),
            node,
            color);
        AppendQuad(vertices,
            indices,
            new RectangleF(bounds.Left, bounds.Top + height, width, bounds.Height - height * 2),
            node,
            color);
        AppendQuad(vertices,
            indices,
            new RectangleF(bounds.Right - width, bounds.Top + height, width, bounds.Height - height * 2),
            node,
            color);
        draws.Add(new D3D12Scene2DDraw(node, [.. vertices], [.. indices], ToScissor(clip), null, atlas.White));
    }

    /// <summary>
    ///     Adds a filled ellipse or an ellipse ring.
    /// </summary>
    private void AddEllipseGeometry(SceneNode2D node,
        RectangleF bounds,
        float strokeWidth,
        Color4 color,
        RectangleF clip) {
        List<Vector2> outer = [];
        List<Vector2> inner = [];
        var center = bounds.Center;
        var radiusX = bounds.Width / 2;
        var radiusY = bounds.Height / 2;
        for (var index = 0; index < CurveSegments; index++) {
            var angle = MathF.Tau * index / CurveSegments;
            outer.Add(new Vector2(center.X + MathF.Cos(angle) * radiusX,
                center.Y + MathF.Sin(angle) * radiusY));
            if (strokeWidth > 0)
                inner.Add(new Vector2(center.X + MathF.Cos(angle) * Math.Max(0, radiusX - strokeWidth),
                    center.Y + MathF.Sin(angle) * Math.Max(0, radiusY - strokeWidth)));
        }
        AddConvexOrRing(node, outer, inner, color, clip);
    }

    /// <summary>
    ///     Adds a filled rounded rectangle or a rounded border ring.
    /// </summary>
    private void AddRoundedGeometry(SceneNode2D node,
        RectangleF bounds,
        float radius,
        Vector4 thickness,
        Color4 color,
        RectangleF clip,
        bool ring) {
        var outer = CreateRoundedPerimeter(bounds, radius);
        List<Vector2> inner = [];
        if (ring) {
            var innerBounds = new RectangleF(bounds.Left + thickness.W,
                bounds.Top + thickness.X,
                Math.Max(0, bounds.Width - thickness.W - thickness.Y),
                Math.Max(0, bounds.Height - thickness.X - thickness.Z));
            var innerRadius = Math.Max(0, radius - Math.Max(Math.Max(thickness.X, thickness.Y),
                Math.Max(thickness.Z, thickness.W)));
            inner = CreateRoundedPerimeter(innerBounds, innerRadius);
        }
        AddConvexOrRing(node, outer, inner, color, clip);
    }

    /// <summary>
    ///     Adds one convex fan or a matching outer/inner ring.
    /// </summary>
    private void AddConvexOrRing(SceneNode2D node,
        IReadOnlyList<Vector2> outer,
        IReadOnlyList<Vector2> inner,
        Color4 color,
        RectangleF clip) {
        if (outer.Count < 3) return;
        var atlasUv = new Vector2((atlas.White.X + 0.5f) / atlas.Width, (atlas.White.Y + 0.5f) / atlas.Height);
        var vertexColor = ToVector(color);
        List<SpriteStruct> vertices = [];
        List<uint> indices = [];
        if (inner.Count == outer.Count) {
            for (var index = 0; index < outer.Count; index++) {
                vertices.Add(CreateVertex(outer[index], node.TotalModelMatrix, atlasUv, vertexColor));
                vertices.Add(CreateVertex(inner[index], node.TotalModelMatrix, atlasUv, vertexColor));
            }
            for (var index = 0; index < outer.Count; index++) {
                var next = (index + 1) % outer.Count;
                var outerCurrent = checked((uint) index * 2);
                var innerCurrent = outerCurrent + 1;
                var outerNext = checked((uint) next * 2);
                var innerNext = outerNext + 1;
                indices.AddRange([outerCurrent, outerNext, innerNext, outerCurrent, innerNext, innerCurrent]);
            }
        } else {
            var center = new Vector2(outer.Average(point => point.X), outer.Average(point => point.Y));
            vertices.Add(CreateVertex(center, node.TotalModelMatrix, atlasUv, vertexColor));
            foreach (var point in outer)
                vertices.Add(CreateVertex(point, node.TotalModelMatrix, atlasUv, vertexColor));
            for (var index = 0; index < outer.Count; index++)
                indices.AddRange([0, checked((uint) index + 1), checked((uint) ((index + 1) % outer.Count) + 1)]);
        }
        draws.Add(new D3D12Scene2DDraw(node, [.. vertices], [.. indices], ToScissor(clip), null, atlas.White));
    }

    /// <summary>
    ///     Creates a clockwise rounded-rectangle perimeter with equal samples per corner.
    /// </summary>
    private static List<Vector2> CreateRoundedPerimeter(RectangleF bounds, float radius) {
        var clamped = Math.Clamp(radius, 0, Math.Min(bounds.Width, bounds.Height) / 2);
        if (clamped == 0)
            return [bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft];
        List<Vector2> result = [];
        var segmentsPerCorner = CurveSegments / 4;
        var centers = new[] {
            new Vector2(bounds.Right - clamped, bounds.Top + clamped),
            new Vector2(bounds.Right - clamped, bounds.Bottom - clamped),
            new Vector2(bounds.Left + clamped, bounds.Bottom - clamped),
            new Vector2(bounds.Left + clamped, bounds.Top + clamped)
        };
        for (var corner = 0; corner < centers.Length; corner++)
            for (var segment = 0; segment < segmentsPerCorner; segment++) {
                var angle = -MathF.PI / 2 + corner * MathF.PI / 2 + segment * MathF.PI / 2 / segmentsPerCorner;
                result.Add(centers[corner] + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * clamped);
            }
        return result;
    }

    /// <summary>
    ///     Appends one solid-color quad to shared geometry arrays.
    /// </summary>
    private void AppendQuad(List<SpriteStruct> vertices,
        List<uint> indices,
        RectangleF bounds,
        SceneNode2D node,
        Color4 color) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var start = checked((uint) vertices.Count);
        var uv = new RectangleF((float) atlas.White.X / atlas.Width,
            (float) atlas.White.Y / atlas.Height,
            (float) atlas.White.Width / atlas.Width,
            (float) atlas.White.Height / atlas.Height);
        vertices.AddRange(CreateQuadVertices(bounds, node.TotalModelMatrix, color, uv));
        indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
    }

    /// <summary>
    ///     Creates four transformed vertices in clockwise screen order.
    /// </summary>
    internal static SpriteStruct[] CreateQuadVertices(RectangleF bounds,
        Matrix3X2 transform,
        Color4 color,
        RectangleF uv) {
        var vertexColor = ToVector(color);
        return [
            CreateVertex(bounds.TopLeft, transform, uv.TopLeft, vertexColor),
            CreateVertex(bounds.TopRight, transform, uv.TopRight, vertexColor),
            CreateVertex(bounds.BottomRight, transform, uv.BottomRight, vertexColor),
            CreateVertex(bounds.BottomLeft, transform, uv.BottomLeft, vertexColor)
        ];
    }

    /// <summary>
    ///     Creates one transformed sprite vertex.
    /// </summary>
    private static SpriteStruct CreateVertex(Vector2 position,
        Matrix3X2 transform,
        Vector2 uv,
        Vector4 color) => new() {
        Position = Matrix3X2.TransformPoint(transform, position),
        UV = uv,
        Color = color
    };

    /// <summary>
    ///     Converts one solid Direct2D brush to its renderer color.
    /// </summary>
    private static bool TryGetColor(Brush? brush, out Color4 color) {
        if (brush is SolidColorBrush solid) {
            color = solid.Color;
            return color.W > 0;
        }
        color = default;
        return false;
    }

    /// <summary>
    ///     Converts a color into the sprite vertex layout.
    /// </summary>
    private static Vector4 ToVector(Color4 color) => new(color.X, color.Y, color.Z, color.W);

    /// <summary>
    ///     Resolves atlas coordinates after all text for the frame has had a chance to grow the atlas.
    /// </summary>
    /// <param name="draw">The prepared draw to update.</param>
    private void UpdateAtlasUvs(D3D12Scene2DDraw draw) {
        if (draw.AtlasEntry is not { } entry) return;
        if (ReferenceEquals(entry, atlas.White)) {
            var white = new Vector2((entry.X + 0.5f) / atlas.Width, (entry.Y + 0.5f) / atlas.Height);
            foreach (ref var vertex in draw.Vertices.AsSpan()) vertex.UV = white;
            return;
        }
        var left = (float) entry.X / atlas.Width;
        var top = (float) entry.Y / atlas.Height;
        var right = (float) (entry.X + entry.Width) / atlas.Width;
        var bottom = (float) (entry.Y + entry.Height) / atlas.Height;
        draw.Vertices[0].UV = new Vector2(left, top);
        draw.Vertices[1].UV = new Vector2(right, top);
        draw.Vertices[2].UV = new Vector2(right, bottom);
        draw.Vertices[3].UV = new Vector2(left, bottom);
    }

    /// <summary>
    ///     Transforms a local rectangle and returns its axis-aligned physical bounds.
    /// </summary>
    internal static RectangleF TransformBounds(RectangleF bounds, Matrix3X2 transform) {
        var points = new[] {
            Matrix3X2.TransformPoint(transform, bounds.TopLeft),
            Matrix3X2.TransformPoint(transform, bounds.TopRight),
            Matrix3X2.TransformPoint(transform, bounds.BottomRight),
            Matrix3X2.TransformPoint(transform, bounds.BottomLeft)
        };
        var left = points.Min(point => point.X);
        var top = points.Min(point => point.Y);
        var right = points.Max(point => point.X);
        var bottom = points.Max(point => point.Y);
        return new RectangleF(left, top, right - left, bottom - top);
    }

    /// <summary>
    ///     Intersects two axis-aligned rectangles.
    /// </summary>
    internal static RectangleF Intersect(RectangleF left, RectangleF right) {
        var x = Math.Max(left.Left, right.Left);
        var y = Math.Max(left.Top, right.Top);
        var edgeX = Math.Min(left.Right, right.Right);
        var edgeY = Math.Min(left.Bottom, right.Bottom);
        return new RectangleF(x, y, Math.Max(0, edgeX - x), Math.Max(0, edgeY - y));
    }

    /// <summary>
    ///     Rounds a non-empty floating clip inward to the valid D3D12 scissor contract.
    /// </summary>
    private static RectangleF ToScissor(RectangleF clip) {
        var left = MathF.Max(0, MathF.Floor(clip.Left));
        var top = MathF.Max(0, MathF.Floor(clip.Top));
        var right = MathF.Max(left + 1, MathF.Ceiling(clip.Right));
        var bottom = MathF.Max(top + 1, MathF.Ceiling(clip.Bottom));
        return new RectangleF(left, top, right - left, bottom - top);
    }

    /// <summary>
    ///     Creates the pixel-space orthographic projection consumed by vsSprite.
    /// </summary>
    internal static GlobalTransformStruct CreateTransforms(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        var projection = Matrix.Identity;
        projection.M11 = 2f / width;
        projection.M22 = -2f / height;
        projection.M41 = -1;
        projection.M42 = 1;
        return new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = projection,
            ViewProjection = projection,
            Viewport = new Vector4(width, height, 1f / width, 1f / height),
            Resolution = new Vector4(width, height, 1f / width, 1f / height)
        };
    }

    /// <summary>
    ///     Returns one immutable descriptor and upload-buffer slot for the current recorded draw.
    /// </summary>
    private BindingSlot NextBindingSlot() {
        if (bindingSlotIndex == bindingSlots.Count)
            bindingSlots.Add(new BindingSlot(device, resourceHeap, samplerHeap));
        return bindingSlots[bindingSlotIndex++];
    }

    /// <summary>
    ///     Owns immutable descriptors and reusable upload buffers for one recorded frame draw.
    /// </summary>
    private sealed class BindingSlot : IDisposable {
        /// <summary>
        ///     Native device used for buffer growth.
        /// </summary>
        private readonly SilkD3D12Device device;

        /// <summary>
        ///     Current vertex upload allocation.
        /// </summary>
        private SilkD3D12Resource vertexBuffer;

        /// <summary>
        ///     Current index upload allocation.
        /// </summary>
        private SilkD3D12Resource indexBuffer;

        /// <summary>
        ///     Initializes one complete Sprite2D root table.
        /// </summary>
        internal BindingSlot(SilkD3D12Device device,
            SilkD3D12DescriptorHeap resourceHeap,
            SilkD3D12DescriptorHeap samplerHeap) {
            this.device = device;
            Bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
            Transforms = new SilkD3D12ConstantBuffer(device,
                Bindings.ConstantBuffer(0),
                GlobalTransformStruct.SizeInBytes);
            FallbackConstants = device.CreateBuffer(256, HeapType.Upload);
            FallbackConstants.Write(new byte[256]);
            Bindings.InitializeFallbackDescriptors(device, FallbackConstants, 0);
            device.CreateSampler(Bindings.Sampler(SamplerRegister),
                Filter.MinMagMipLinear,
                TextureAddressMode.Clamp);
            vertexBuffer = device.CreateBuffer(SpriteStruct.SizeInBytes, HeapType.Upload);
            indexBuffer = device.CreateBuffer(sizeof(uint), HeapType.Upload);
        }

        /// <summary>Gets the complete graphics root table.</summary>
        internal SilkD3D12GraphicsBindings Bindings { get; }

        /// <summary>Gets the per-draw b0 constants.</summary>
        private SilkD3D12ConstantBuffer Transforms { get; }

        /// <summary>Gets the zero fallback allocation.</summary>
        private SilkD3D12Resource FallbackConstants { get; }

        /// <summary>Gets the current vertex upload buffer.</summary>
        internal SilkD3D12Resource VertexBuffer => vertexBuffer;

        /// <summary>Gets the current index upload buffer.</summary>
        internal SilkD3D12Resource IndexBuffer => indexBuffer;

        /// <summary>
        ///     Grows and fills this slot without changing data referenced by another draw.
        /// </summary>
        internal void Update(SpriteStruct[] vertices, uint[] indices, in GlobalTransformStruct transforms) {
            var vertexBytes = MemoryMarshal.AsBytes(vertices.AsSpan());
            var indexBytes = MemoryMarshal.AsBytes(indices.AsSpan());
            EnsureCapacity(ref vertexBuffer, vertexBytes.Length);
            EnsureCapacity(ref indexBuffer, indexBytes.Length);
            vertexBuffer.Write(vertexBytes);
            indexBuffer.Write(indexBytes);
            Transforms.Write(in transforms);
        }

        /// <summary>
        ///     Replaces one upload buffer when its current allocation is too small.
        /// </summary>
        private void EnsureCapacity(ref SilkD3D12Resource buffer, int requiredBytes) {
            if ((ulong) requiredBytes <= buffer.SizeInBytes) return;
            var replacement = device.CreateBuffer(checked((ulong) requiredBytes), HeapType.Upload);
            buffer.Dispose();
            buffer = replacement;
        }

        /// <summary>
        ///     Releases descriptors and upload allocations.
        /// </summary>
        public void Dispose() {
            indexBuffer.Dispose();
            vertexBuffer.Dispose();
            FallbackConstants.Dispose();
            Transforms.Dispose();
            Bindings.Dispose();
        }
    }
}

/// <summary>
///     Contains one prepared Scene2D draw in stable traversal order.
/// </summary>
internal sealed class D3D12Scene2DDraw {
    /// <summary>
    ///     Initializes one prepared draw.
    /// </summary>
    internal D3D12Scene2DDraw(SceneNode2D node,
        SpriteStruct[] vertices,
        uint[] indices,
        RectangleF clip,
        TextureModel? image,
        D3D12GlyphAtlasEntry? atlasEntry) {
        Node = node;
        Vertices = vertices;
        Indices = indices;
        Clip = clip;
        Image = image;
        AtlasEntry = atlasEntry;
    }

    /// <summary>Gets the source node.</summary>
    internal SceneNode2D Node { get; }

    /// <summary>Gets the transformed sprite vertices.</summary>
    internal SpriteStruct[] Vertices { get; }

    /// <summary>Gets the triangle indices.</summary>
    internal uint[] Indices { get; }

    /// <summary>Gets the physical scissor rectangle.</summary>
    internal RectangleF Clip { get; }

    /// <summary>Gets the optional standalone image texture.</summary>
    internal TextureModel? Image { get; }

    /// <summary>Gets the optional atlas entry whose UVs are resolved after atlas growth.</summary>
    internal D3D12GlyphAtlasEntry? AtlasEntry { get; }
}
