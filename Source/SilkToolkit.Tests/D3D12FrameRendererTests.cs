using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Matrix3X2 = HelixToolkit.SharpDX.Core.Native.Matrix3X2;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Exercises the productive shared recorder against caller-owned offscreen WARP color textures.</summary>
[Collection(WpfCollection.Name)]
public sealed partial class D3D12FrameRendererTests {
    /// <summary>Draws the preparer's own retained candidates without an intermediate classification copy.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersDirectPreparationCandidates() {
        using var frame = new OffscreenFrame();
        using var mesh = CreateGreenQuad();
        var camera = CreateCamera();
        using var update = new RenderContext(camera, 32, 24);
        var candidates = frame.Renderer.SceneRenderer.PrepareScene([mesh], update);
        var target = frame.Target;
        var transforms = D3D12FrameRenderer.CreateViewportTransforms(
            camera, target.Width, target.Height, 1, 0,
            out var frustum
        );
        frame.Context.Reset();
        var draws = frame.Renderer.RecordSceneFrame(
            frame.Context, in target, [0, 0, 1, 1],
            candidates, in transforms, false, ref frustum
        );
        var pixels = frame.Readback(in target);
        Assert.Equal(1, draws);
        Assert.Same(mesh, Assert.Single(candidates));
        AssertPixel(pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
    }

    /// <summary>Records the default viewport pass sequence and reads every pixel without a presentation host.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRecordsDefaultViewportClearIntoOffscreenRgbaTarget() {
        using var frame = new OffscreenFrame();
        var camera = CreateCamera();
        var target = frame.Target;
        frame.Context.Reset();
        var draws = frame.Renderer.RecordViewportFrame(
            frame.Context, in target, 1,
            [0, 0, 1, 1], Array.Empty<SceneNode>(), camera, false
        );
        var pixels = frame.Readback(in target);

        Assert.Equal(0, draws);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, target.ColorResource.Description.Format);
        for (uint y = 0; y < target.Height; y++)
        for (uint x = 0; x < target.Width; x++)
            AssertPixel(pixels, target.Width, x, y, 0, 0, 255);
        Assert.Equal(ResourceStates.CopySource, target.ColorResource.State);
        var depth = Assert.IsType<SilkD3D12Resource>(frame.Renderer.DepthStencilBuffer);
        Assert.Equal((ulong) target.Width, depth.Description.Width);
        Assert.Equal(target.Height, depth.Description.Height);

        frame.Renderer.Dispose();
        Assert.True(depth.IsDisposed);
        Assert.False(target.ColorResource.IsDisposed);
        Assert.False(target.RenderTargetView.IsDisposed);
        Assert.False(frame.Context.IsDisposed);
        Assert.False(frame.Device.IsDisposed);
    }

    /// <summary>Proves native mesh pixels, repeated depth clears, and actual context, list, and selector reuse.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersMeshAndReusesFrameOwnersAcrossCompletedFrames() {
        using var frame = new OffscreenFrame();
        using var mesh = CreateGreenQuad();
        var camera = CreateCamera();
        var roots = new FastList<SceneNode> { mesh };
        var target = frame.Target;
        var first = frame.Render(camera, roots);
        Assert.Equal(1, first.Draws);
        AssertPixel(first.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
        AssertPixel(first.Pixels, target.Width, 0, 0, 0, 0, 255);
        Assert.True(mesh.RenderCore.IsAttached);
        var context = frame.Renderer.RenderContext;
        var flattened = frame.Renderer.PerFrameFlattenedScene;
        var depth = frame.Renderer.DepthStencilBuffer;
        var catalog = frame.Renderer.PassCatalog;
        var opaque = Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes");
        var transparent = Field<FastList<SceneNode>>(frame.Renderer, "transparentNodes");
        var lights = Field<LightsBufferModel>(frame.Renderer, "lights");
        var selector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass");
        var boneSelector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolveBoneSkinningPass");
        Assert.Same(catalog, selector.Target);
        Assert.Same(catalog, boneSelector.Target);
        Assert.Same(mesh, Assert.Single(opaque));

        // A farther second draw fails if the common recorder retains the preceding frame's depth values.
        mesh.ModelMatrix = SilkMath.Translation(new Vector3(0, 0, -0.5f));
        var second = frame.Render(camera, roots);
        Assert.Equal(1, second.Draws);
        AssertPixel(second.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 255, 0);
        Assert.Same(context, frame.Renderer.RenderContext);
        Assert.Same(flattened, frame.Renderer.PerFrameFlattenedScene);
        Assert.Same(depth, frame.Renderer.DepthStencilBuffer);
        Assert.Same(opaque, Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes"));
        Assert.Same(transparent, Field<FastList<SceneNode>>(frame.Renderer, "transparentNodes"));
        Assert.Same(lights, Field<LightsBufferModel>(frame.Renderer, "lights"));
        Assert.Same(selector, Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass"));
        Assert.Same(boneSelector, Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolveBoneSkinningPass"));

        mesh.Visible = false;
        var hidden = frame.Render(camera, roots);
        Assert.Equal(0, hidden.Draws);
        Assert.Empty(opaque);
        AssertPixel(hidden.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 0, 255);
        Assert.Same(context, frame.Renderer.RenderContext);
    }

    /// <summary>Retains both update contexts until immutable camera, DPI, size, or manager inputs change.</summary>
    /// <returns>The STA context and offscreen rendering verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarpReusesContextsAndReplacesThemAtCompletedResourceBoundaries() => StaThread.RunAsync(() => {
            using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
            using var replacementEffects = new DefaultEffectsManager(
                new EffectsManagerConfiguration { EnableSoftwareRendering = true }
            );
            using var frame = new OffscreenFrame(effects);
            var camera = CreateCamera();
            var roots = Array.Empty<SceneNode>();
            var overlays = Array.Empty<SceneNode2D>();
            _ = frame.Render(camera, roots, roots2D: overlays);
            var context = Assert.IsType<RenderContext>(frame.Renderer.RenderContext);
            var context2D = Assert.IsType<RenderContext2D>(frame.Renderer.RenderContext2D);
            _ = frame.Render(camera, roots, roots2D: overlays);
            Assert.Same(context, frame.Renderer.RenderContext);
            Assert.Same(context2D, frame.Renderer.RenderContext2D);
            Assert.Equal(Matrix3X2.Identity, context2D.RelativeTransform);
            Assert.False(context2D.HasTarget);

            _ = frame.Render(camera, roots, dpiScale: 2, roots2D: overlays);
            Assert.NotSame(context, frame.Renderer.RenderContext);
            Assert.Null(context.Camera);
            Assert.True(context2D.IsDisposed);
            Assert.Equal(2, frame.Renderer.RenderContext!.DpiScale);
            Assert.Equal(2, frame.Renderer.RenderContext2D!.DpiScale);

            var depth = Assert.IsType<SilkD3D12Resource>(frame.Renderer.DepthStencilBuffer);
            var resizedContext = frame.Renderer.RenderContext;
            using var resizedColor = frame.Device.CreateRenderTargetTexture2D(48, 16, Format.FormatR8G8B8A8Unorm);
            using var resizedView = frame.TargetHeap.Allocate();
            frame.Device.CreateRenderTargetView(resizedColor, resizedView);
            var resized = new D3D12FrameTarget(resizedColor, resizedView, 48, 16, Format.FormatR8G8B8A8Unorm);
            frame.Context.Reset();
            _ = frame.Renderer.RecordViewportFrame(
                frame.Context, in resized, 2,
                [1, 0, 0, 1], roots, camera, false, roots2D: overlays
            );
            var pixels = frame.Readback(in resized);
            AssertPixel(pixels, resized.Width, 0, 0, 255, 0, 0);
            Assert.True(depth.IsDisposed);
            Assert.NotSame(resizedContext, frame.Renderer.RenderContext);
            Assert.Equal(48, frame.Renderer.RenderContext!.ActualWidth);
            Assert.Equal(16, frame.Renderer.RenderContext2D!.ActualHeight);

            var oldCatalog = frame.Renderer.PassCatalog;
            var oldContext2D = frame.Renderer.RenderContext2D;
            frame.Renderer.SetEffectsManager(replacementEffects);
            Assert.True(oldCatalog.IsDisposed);
            Assert.True(oldContext2D!.IsDisposed);
            Assert.Null(frame.Renderer.RenderContext2D);
            var selector = Field<Func<SceneNode, ShaderPass?>>(frame.Renderer, "resolvePass");
            Assert.Same(frame.Renderer.PassCatalog, selector.Target);
            _ = frame.Render(camera, roots, roots2D: overlays);
            Assert.Same(replacementEffects, frame.Renderer.RenderContext2D!.DeviceResources);
            frame.Renderer.Dispose();
            Assert.False(effects.IsDisposed);
            Assert.False(replacementEffects.IsDisposed);
        }
    );

    /// <summary>Discards mutable context stacks after a failed original Scene2D update and renders again.</summary>
    /// <returns>The STA exception and recovery verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarpDoesNotReuseFailedScene2DContextState() => StaThread.RunAsync(() => {
            using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
            using var frame = new OffscreenFrame(effects);
            using var overlay = new ThrowingOverlay();
            var target = frame.Target;
            var camera = CreateCamera();
            frame.Context.Reset();
            Assert.Throws<InvalidOperationException>(() => frame.Renderer.RecordViewportFrame(
                    frame.Context,
                    in target, 1, [0, 0, 1, 1], Array.Empty<SceneNode>(), camera, false, roots2D: [overlay]
                )
            );
            var failed = Assert.IsType<RenderContext2D>(overlay.LastContext);
            Assert.True(failed.IsDisposed);
            Assert.Null(frame.Renderer.RenderContext2D);
            // Submission belongs to the caller, including completion of a partially recorded failed frame.
            _ = frame.Readback(in target);
            var recovered = frame.Render(camera, Array.Empty<SceneNode>(), roots2D: Array.Empty<SceneNode2D>());
            var current = Assert.IsType<RenderContext2D>(frame.Renderer.RenderContext2D);
            Assert.NotSame(failed, current);
            Assert.Equal(Matrix3X2.Identity, current.RelativeTransform);
            Assert.False(current.HasTarget);
            AssertPixel(recovered.Pixels, target.Width, 0, 0, 0, 0, 255);
        }
    );

    /// <summary>Rejects format, size, and DPI mismatches before changing the actual retained frame owners.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsIncompatibleBorrowedTargetMetadataBeforeMutation() {
        using var frame = new OffscreenFrame();
        var camera = CreateCamera();
        var roots = Array.Empty<SceneNode>();
        _ = frame.Render(camera, roots);
        var context = frame.Renderer.RenderContext;
        var depth = frame.Renderer.DepthStencilBuffer;
        var target = frame.Target;
        var badFormat = target with { Format = Format.FormatB8G8R8A8Unorm };
        var badSize = target with { Width = target.Width + 1 };
        var zeroSize = target with { Height = 0 };
        frame.Context.Reset();
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(
                frame.Context,
                in badFormat, 1, [0, 0, 0, 1], roots, camera, false
            )
        );
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(
                frame.Context,
                in badSize, 1, [0, 0, 0, 1], roots, camera, false
            )
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Renderer.RecordViewportFrame(
                frame.Context,
                in zeroSize, 1, [0, 0, 0, 1], roots, camera, false
            )
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Renderer.RecordViewportFrame(
                frame.Context,
                in target, float.NaN, [0, 0, 0, 1], roots, camera, false
            )
        );
        using var differentColor = frame.Device.CreateRenderTargetTexture2D(32, 24, Format.FormatB8G8R8A8Unorm);
        using var differentView = frame.TargetHeap.Allocate();
        frame.Device.CreateRenderTargetView(differentColor, differentView);
        var mismatchedResource = new D3D12FrameTarget(
            differentColor, differentView, 32, 24,
            Format.FormatR8G8B8A8Unorm
        );
        Assert.Throws<ArgumentException>(() => frame.Renderer.RecordViewportFrame(
                frame.Context,
                in mismatchedResource, 1, [0, 0, 0, 1], roots, camera, false
            )
        );
        Assert.Same(context, frame.Renderer.RenderContext);
        Assert.Same(depth, frame.Renderer.DepthStencilBuffer);
        Assert.False(depth!.IsDisposed);
        Assert.Equal(ResourceStates.CopySource, target.ColorResource.State);
    }


    /// <summary>Retains scene occlusion, native surface offset, strict region ties and helper ordering.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpInspectorGroupRetainsSceneDepthAndDrawsHelpersAfterRegions() {
        using var effects = new InspectorProbeEffects();
        using var frame = new OffscreenFrame(effects);
        using var scene = InspectorQuad(Vector3.Zero, new Vector4(0, 1, 0, 1));
        scene.DepthBias = 1;
        scene.SlopeScaledDepthBias = 1;
        using var group = new GroupNode { RenderType = RenderType.PostEffect };
        using var region = InspectorQuad(Vector3.Zero, new Vector4(1, 0, 0, 1));
        using var tied = InspectorQuad(Vector3.Zero, new Vector4(0, 0, 1, 1));
        group.AddChildNode(region);
        group.AddChildNode(tied);
        var camera = CreateCamera();
        var target = frame.Target;
        foreach (var aa in new[] { FXAALevel.None, FXAALevel.Low }) {
            var visible = frame.Render(camera, [scene, group], fxaaLevel: aa);
            Assert.Equal(3, visible.Draws);
            Assert.Same(scene, Assert.Single(Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes")));
            Assert.Same(group, Assert.Single(Field<FastList<SceneNode>>(frame.Renderer, "postEffectNodes")));
            AssertPixel(visible.Pixels, target.Width, target.Width / 2, target.Height / 2, 13, 0, 0);
            scene.DepthBias = 0;
            scene.SlopeScaledDepthBias = 0;
            var tiedScene = frame.Render(camera, [scene, group], fxaaLevel: aa);
            AssertPixel(tiedScene.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 13, 0);
            scene.DepthBias = 1;
            scene.SlopeScaledDepthBias = 1;
        }
        using var helper = CreateGreenQuad();
        helper.Geometry!.Colors = new Color4Collection(Enumerable.Repeat(new Vector4(1, 1, 1, 1), 4));
        group.AddChildNode(helper);
        var marker = frame.Render(camera, [scene, group]);
        AssertPixel(marker.Pixels, target.Width, target.Width / 2, target.Height / 2, 255, 255, 255);
        var geometry = Assert.IsType<MeshGeometry3D>(scene.Geometry);
        geometry.Positions = new Vector3Collection(geometry.Positions!.Select(point =>
            new Vector3(point.X, point.Y, -.75f)));
        var occluded = frame.Render(camera, [scene, group]);
        AssertPixel(occluded.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 13, 0);
        Assert.Equal(new SceneNode[] { region, tied, helper }, group.Items.ToArray());
        using var empty = new MeshNode { Geometry = new MeshGeometry3D(), Material = new ColorMaterialCore() };
        group.Clear();
        group.AddChildNode(empty);
        scene.Visible = false;
        var emptyFrame = frame.Render(camera, [scene, group]);
        Assert.Equal(0, emptyFrame.Draws);
        Assert.True(empty.RenderCore.IsAttached);
        AssertPixel(emptyFrame.Pixels, target.Width, target.Width / 2, target.Height / 2, 0, 0, 255);
    }

    /// <summary>Differentiates four native lights, stored normal length, one-sided lighting and unlit substitution.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpInspectorUsesFourLightsAndStoredNormalsBeforeInterpolation() {
        using var effects = new InspectorProbeEffects();
        using var frame = new OffscreenFrame(effects);
        using var group = new GroupNode { RenderType = RenderType.PostEffect };
        using var mesh = InspectorQuad(Vector3.Zero, new Vector4(1, 0, 0, 1));
        group.AddChildNode(mesh);
        var geometry = Assert.IsType<MeshGeometry3D>(mesh.Geometry);
        var camera = CreateCamera();
        var target = frame.Target;
        foreach (var sample in new[] {
                     (Normal: Vector3.Zero, R: (byte)13, G: (byte)0, B: (byte)0),
                     (Normal: new Vector3(0, 0, .5f), R: (byte)223, G: (byte)0, B: (byte)0),
                     (Normal: Vector3.UnitZ, R: (byte)255, G: (byte)153, B: (byte)153),
                     (Normal: -Vector3.UnitZ, R: (byte)13, G: (byte)0, B: (byte)0)
                 }) {
            geometry.Normals = new Vector3Collection(Enumerable.Repeat(sample.Normal, 4));
            foreach (var reverse in new[] { false, true }) {
                geometry.Indices = reverse
                    ? new IntCollection([2, 1, 0, 3, 1, 2])
                    : new IntCollection([0, 1, 2, 2, 1, 3]);
                var result = frame.Render(camera, [group]);
                Assert.Equal(1, result.Draws);
                AssertPixel(result.Pixels, target.Width, target.Width / 2, target.Height / 2,
                    sample.R, sample.G, sample.B);
            }
        }
        geometry.Indices = new IntCollection([0, 1, 2, 2, 1, 3]);
        geometry.Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ,
            -Vector3.UnitZ, -Vector3.UnitZ]);
        var interpolated = frame.Render(camera, [group]);
        var offset = checked((int)((target.Height / 2 * target.Width + target.Width / 2) * 4));
        // At this center sample the post-VS color interpolates bright and dark vertices.
        // Per-fragment normal evaluation would instead produce ambient or saturated red.
        Assert.InRange(interpolated.Pixels[offset], (byte)90, (byte)180);
        Assert.InRange(interpolated.Pixels[offset + 1], (byte)45, (byte)110);
        Assert.Equal(interpolated.Pixels[offset + 1], interpolated.Pixels[offset + 2]);

        mesh.Material = new InspectorProbeMaterial(inspector: false);
        geometry.Normals = new Vector3Collection(Enumerable.Repeat(new Vector3(0, 0, .5f), 4));
        var ordinary = new List<(byte R, byte G, byte B)>();
        foreach (var reverse in new[] { false, true }) {
            geometry.Indices = reverse ? new IntCollection([2, 1, 0, 3, 1, 2])
                : new IntCollection([0, 1, 2, 2, 1, 3]);
            var result = frame.Render(camera, [group]);
            ordinary.Add((result.Pixels[offset], result.Pixels[offset + 1], result.Pixels[offset + 2]));
        }
        Assert.Contains(((byte)252, (byte)51, (byte)51), ordinary);
        Assert.Contains(((byte)13, (byte)13, (byte)13), ordinary);
    }

    /// <summary>Creates independent synthetic shader witnesses; these coordinates are not native runtime observations.</summary>
    /// <param name="normal">The unchanged stored vertex normal.</param>
    /// <param name="color">The explicit native type-color witness.</param>
    /// <returns>The productive node using the shared Meshmixer shader modules.</returns>
    private static MeshNode InspectorQuad(Vector3 normal, Vector4 color) => new() {
        Material = new InspectorProbeMaterial(),
        CullMode = CullMode.None,
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-.5f, .5f, 0), new(.5f, .5f, 0),
                new(-.5f, -.5f, 0), new(.5f, -.5f, 0)]),
            Normals = new Vector3Collection(Enumerable.Repeat(normal, 4)),
            Colors = new Color4Collection(Enumerable.Repeat(color, 4)),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>Registers the existing modules on the existing native frame fixture.</summary>
    private sealed class InspectorProbeEffects : DefaultEffectsManager, ID3D12MeshShaderProvider {
        /// <summary>Creates one explicit LESS/write pass with the production module names and layout.</summary>
        internal InspectorProbeEffects() : base(new EffectsManagerConfiguration { EnableSoftwareRendering = true }) {
            var vertex = D3D12ShaderModule.Load("VS", "vsMeshMixerPhong");
            var pixel = D3D12ShaderModule.Load("PS", "psMeshMixerPhong");
            var vs = new ShaderDescription("vsMeshMixerPhong", ShaderStage.Vertex, vertex.ByteCode.ToArray());
            var ps = new ShaderDescription("psMeshMixerPhong", ShaderStage.Pixel, pixel.ByteCode.ToArray());
            var elements = DefaultInputLayout.VsInput.Select(element => {
                if (element.Classification == HelixToolkit.SharpDX.Core.Native.InputClassification.PerInstanceData) element.Slot = 4;
                return element;
            }).ToArray();
            AddTechnique(new TechniqueDescription("InspectorProbe") {
                InputLayoutDescription = new InputLayoutDescription(vs.ByteCode!,
                    [.. elements, new InputElement("COLOR", 1, Format.FormatR32G32B32A32Float, 0, 3)]),
                PassDescriptions = [new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [vs, ps],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                }]
            });
        }

        /// <summary>Supplies the unused back-color slot required by the shared ordinary/region VS layout.</summary>
        /// <param name="geometry">The actual productive geometry.</param>
        /// <returns>One explicit auxiliary record per position.</returns>
        D3D12MeshVertexData[] ID3D12MeshShaderProvider.GetVertexData(Geometry3D geometry) =>
            geometry is MeshGeometry3D { Normals: not null }
                ? new D3D12MeshVertexData[geometry.Positions?.Count ?? 0] : [];

        /// <summary>Routes only the test material to the registered shared modules.</summary>
        /// <param name="node">The actual scene node.</param>
        /// <param name="selectedTechnique">The ordinary selected technique.</param>
        /// <returns>The probe technique or the unchanged ordinary technique.</returns>
        string ID3D12MeshShaderProvider.GetTechniqueName(SceneNode node, string selectedTechnique) =>
            node is MaterialGeometryNode { Material: InspectorProbeMaterial } ? "InspectorProbe" : selectedTechnique;
    }

    /// <summary>Uploads independent fixed input witnesses into the production 256-byte material union.</summary>
    /// <param name="inspector">Whether the witness selects the Inspector draw or the unchanged ordinary draw.</param>
    private sealed class InspectorProbeMaterial(bool inspector = true) : MaterialCore, ID3D12MeshMaterial {
        /// <summary>Gets the shared module test technique.</summary>
        string ID3D12MeshMaterial.TechniqueName => "InspectorProbe";
        /// <summary>Disables an unrelated shadow input.</summary>
        bool ID3D12MeshMaterial.RenderShadowMap => false;
        /// <summary>Clears the unrelated shared shadow flag.</summary>
        /// <param name="material">The productive shared mesh payload.</param>
        void ID3D12MeshMaterial.ApplyMeshParameters(ref PhongPBRMaterialStruct material) =>
            material.RenderShadowMap = 0;
        /// <summary>Writes matrices, four distant positional lights and the explicit region discriminator.</summary>
        /// <param name="destination">The productive upload payload.</param>
        /// <param name="geometry">The productive geometry.</param>
        /// <param name="node">The productive node.</param>
        void ID3D12MeshMaterial.WriteMaterialConstants(Span<byte> destination, Geometry3D? geometry, SceneNode? node) {
            destination.Clear();
            var identity = System.Numerics.Matrix4x4.Identity;
            var light = new System.Numerics.Vector4(0, 0, 1000000, 1);
            var profile = new System.Numerics.Vector4(1, inspector ? 1 : 0, 0, 0);
            System.Runtime.InteropServices.MemoryMarshal.Write(destination, in identity);
            System.Runtime.InteropServices.MemoryMarshal.Write(destination[64..], in identity);
            System.Runtime.InteropServices.MemoryMarshal.Write(destination[128..], in identity);
            foreach (var offset in new[] { 176, 192, 208, 240 })
                System.Runtime.InteropServices.MemoryMarshal.Write(destination[offset..], in light);
            System.Runtime.InteropServices.MemoryMarshal.Write(destination[224..], in profile);
        }
        /// <summary>Supplies no texture.</summary>
        /// <param name="shaderRegister">The requested texture register.</param>
        /// <returns>No texture.</returns>
        TextureModel? ID3D12MeshMaterial.GetTexture(int shaderRegister) => null;
        /// <summary>Supplies no custom sampler.</summary>
        /// <param name="shaderRegister">The requested sampler register.</param>
        /// <returns>The productive default sampler.</returns>
        SamplerStateDescription? ID3D12MeshMaterial.GetSampler(int shaderRegister) => null;
    }

    /// <summary>Keeps an existing screen-spaced edge inside FXAA when no post-scene group is present.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpScreenSpacedEdgeRemainsInsideFxaaWithoutInspector() {
        using var frame = new OffscreenFrame(width: 128, height: 128);
        using var overlay = new ScreenSpacedNode {
            RelativeScreenLocationX = 0, RelativeScreenLocationY = 0,
            CameraType = ScreenSpacedCameraType.Orthographic
        };
        using var triangle = new MeshNode {
            Material = new ColorMaterialCore(), CullMode = CullMode.None,
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([new(-8, -7, 0), new(8, -3, 0), new(-3, 8, 0)]),
                Colors = new Color4Collection(Enumerable.Repeat(new Vector4(1, 1, 1, 1), 3)),
                Indices = new IntCollection([0, 1, 2])
            }
        };
        overlay.AddChildNode(triangle);
        var camera = CreateCamera();
        var raw = frame.Render(camera, [overlay], fxaaLevel: FXAALevel.None);
        var filtered = frame.Render(camera, [overlay], fxaaLevel: FXAALevel.Low);
        Assert.Equal(1, raw.Draws);
        Assert.Equal(1, filtered.Draws);
        Assert.Empty(Field<FastList<SceneNode>>(frame.Renderer, "postEffectNodes"));
        var overlays = Field<FastList<SceneNode>>(frame.Renderer, "screenSpacedNodes");
        Assert.Collection(overlays, node => Assert.Same(overlay, node), node => Assert.Same(triangle, node));
        var rawRed = Enumerable.Range(0, raw.Pixels.Length / 4).Select(index => raw.Pixels[index * 4]).ToArray();
        var filteredRed = Enumerable.Range(0, filtered.Pixels.Length / 4)
            .Select(index => filtered.Pixels[index * 4]).ToArray();
        Assert.Contains((byte)0, rawRed);
        Assert.Contains((byte)255, rawRed);
        Assert.All(rawRed, value => Assert.True(value is 0 or 255));
        Assert.Contains(filteredRed, value => value > 0 && value < 255);
        Assert.False(raw.Pixels.SequenceEqual(filtered.Pixels));
    }

    /// <summary>Creates a camera whose center and background samples have independent pixel predictions.</summary>
    /// <returns>The fixed orthographic camera.</returns>
    private static OrthographicCameraCore CreateCamera() => new() {
        Position = new Vector3(0, 0, 2),
        LookDirection = new Vector3(0, 0, -2),
        UpDirection = Vector3.UnitY,
        Width = 2,
        NearPlaneDistance = 0.1f,
        FarPlaneDistance = 10
    };

    /// <summary>Creates a centered green mesh with culling disabled for a platform-independent pixel oracle.</summary>
    /// <returns>The original productive mesh node.</returns>
    private static MeshNode CreateGreenQuad() => new() {
        Material = new ColorMaterialCore(),
        CullMode = CullMode.None,
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection(
                [
                    new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0)
                ]
            ),
            Colors = new Color4Collection(
                [
                    new Vector4(0, 1, 0, 1), new Vector4(0, 1, 0, 1),
                    new Vector4(0, 1, 0, 1), new Vector4(0, 1, 0, 1)
                ]
            ),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>Checks a physical RGBA pixel against its independent expected color.</summary>
    /// <param name="pixels">The tightly packed completed readback.</param>
    /// <param name="width">The physical image width.</param>
    /// <param name="x">The physical column.</param>
    /// <param name="y">The physical row.</param>
    /// <param name="red">The expected red channel.</param>
    /// <param name="green">The expected green channel.</param>
    /// <param name="blue">The expected blue channel.</param>
    private static void AssertPixel(byte[] pixels, uint width, uint x, uint y, byte red, byte green, byte blue) {
        var offset = checked((int) ((y * width + x) * 4));
        Assert.Equal(red, pixels[offset]);
        Assert.Equal(green, pixels[offset + 1]);
        Assert.Equal(blue, pixels[offset + 2]);
        Assert.Equal(255, pixels[offset + 3]);
    }

    /// <summary>Reads an actual retained owner to verify that completed frames reuse its identity.</summary>
    /// <typeparam name="T">The expected owner type.</typeparam>
    /// <param name="renderer">The productive shared recorder.</param>
    /// <param name="name">The actual owner field.</param>
    /// <returns>The retained field value.</returns>
    private static T Field<T>(D3D12FrameRenderer renderer, string name) where T : class =>
        Assert.IsType<T>(
            typeof(D3D12FrameRenderer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(renderer)
        );

    /// <summary>Models an application update hook that throws after mutating its frame context.</summary>
    private sealed class ThrowingOverlay : RectangleNode2D {
        /// <summary>Gets the actual context received by the failing update.</summary>
        internal RenderContext2D? LastContext { get; private set; }

        /// <summary>Leaves a relative transform on the context stack before throwing an application failure.</summary>
        /// <param name="context">The retained common Scene2D update context.</param>
        public override void Update(RenderContext2D context) {
            LastContext = context;
            context.PushRelativeTransform(new Matrix3X2(1, 0, 0, 1, 5, 7));
            throw new InvalidOperationException("Injected Scene2D update failure.");
        }
    }

    /// <summary>Owns offscreen test submission and synchronous readback independently of the shared recorder.</summary>
    private sealed class OffscreenFrame : IDisposable {
        /// <summary>The caller-owned queue used only by explicit test submissions.</summary>
        private readonly SilkD3D12CommandQueue queue;

        /// <summary>The caller-owned completion fence.</summary>
        private readonly SilkD3D12Fence fence;

        /// <summary>The borrowed-target RTV owned by this test harness.</summary>
        private readonly SilkD3D12Descriptor view;

        /// <summary>The offscreen RGBA allocation owned by this test harness.</summary>
        private readonly SilkD3D12Resource color;

        /// <summary>Gets the caller-owned WARP device.</summary>
        internal SilkD3D12Device Device { get; }

        /// <summary>Gets the caller-owned recording context.</summary>
        internal SilkD3D12CommandContext Context { get; }

        /// <summary>Gets the caller-owned RTV heap with room for a replacement target.</summary>
        internal SilkD3D12DescriptorHeap TargetHeap { get; }

        /// <summary>Gets the productive common recorder.</summary>
        internal D3D12FrameRenderer Renderer { get; }

        /// <summary>Gets borrowed metadata for the offscreen texture.</summary>
        internal D3D12FrameTarget Target => new(color, view, width, height, Format.FormatR8G8B8A8Unorm);

        /// <summary>The caller-selected physical target width.</summary>
        private readonly uint width;

        /// <summary>The caller-selected physical target height.</summary>
        private readonly uint height;

        /// <summary>Creates a window-independent WARP target and its explicit submission owners.</summary>
        /// <param name="effectsManager">The optional borrowed scene manager.</param>
        /// <param name="width">The physical target width; existing cases retain 32 pixels.</param>
        /// <param name="height">The physical target height; existing cases retain 24 pixels.</param>
        internal OffscreenFrame(HelixToolkit.SharpDX.Core.IEffectsManager? effectsManager = null,
            uint width = 32, uint height = 24) {
            this.width = width;
            this.height = height;
            Device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
            queue = Device.CreateCommandQueue();
            Context = Device.CreateCommandContext();
            fence = Device.CreateFence();
            TargetHeap = Device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 2);
            view = TargetHeap.Allocate();
            color = Device.CreateRenderTargetTexture2D(width, height, Format.FormatR8G8B8A8Unorm);
            Device.CreateRenderTargetView(color, view);
            Renderer = new D3D12FrameRenderer(Device, Format.FormatR8G8B8A8Unorm, effectsManager);
        }

        /// <summary>Records the productive viewport path and completes an explicit test readback.</summary>
        /// <param name="camera">The active camera.</param>
        /// <param name="roots">The stable original scene roots.</param>
        /// <param name="dpiScale">The explicit physical-pixel scale.</param>
        /// <param name="roots2D">The optional stable overlay roots.</param>
        /// <returns>The actual native draw count and tightly packed completed RGBA pixels.</returns>
        internal (int Draws, byte[] Pixels) Render(
            CameraCore camera,
            IEnumerable<SceneNode> roots,
            float dpiScale = 1,
            IEnumerable<SceneNode2D>? roots2D = null,
            FXAALevel fxaaLevel = FXAALevel.None
        ) {
            var target = Target;
            Context.Reset();
            var draws = Renderer.RecordViewportFrame(
                Context, in target, dpiScale,
                [0, 0, 1, 1], roots, camera, false, roots2D: roots2D, autoUpdateOctree: false,
                fxaaLevel: fxaaLevel
            );
            return (draws, Readback(in target));
        }

        /// <summary>Copies an already recorded target and waits for its finite test completion deadline.</summary>
        /// <param name="target">The actual offscreen target used for the current recording.</param>
        /// <returns>Every physical RGBA pixel with native row padding removed.</returns>
        internal byte[] Readback(in D3D12FrameTarget target) {
            var footprint = Device.GetCopyableFootprint(target.ColorResource, out var bytes);
            using var readback = Device.CreateBuffer(bytes, HeapType.Readback);
            Context.Transition(target.ColorResource, ResourceStates.CopySource);
            Context.CopyTextureToBuffer(readback, target.ColorResource, in footprint);
            Context.Close();
            queue.Execute(Context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            Device.ThrowIfDeviceRemoved();
            var rowBytes = checked((int) target.Width * 4);
            var pixels = new byte[checked(rowBytes * (int) target.Height)];
            for (var row = 0; row < target.Height; row++) {
                var source = footprint.Offset + (ulong) row * footprint.Footprint.RowPitch;
                readback.Read(rowBytes, source)
                    .CopyTo(pixels, checked((int) row * rowBytes));
            }

            return pixels;
        }

        /// <summary>Completes test submissions before releasing their native owners in dependency order.</summary>
        public void Dispose() {
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            Renderer.Dispose();
            color.Dispose();
            view.Dispose();
            TargetHeap.Dispose();
            Context.Dispose();
            fence.Dispose();
            queue.Dispose();
            Device.Dispose();
        }
    }
}
