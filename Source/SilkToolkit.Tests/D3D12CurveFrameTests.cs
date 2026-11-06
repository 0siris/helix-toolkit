using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using Xunit;
using SilkMath = HelixToolkit.SharpDX.Core.SilkMath;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks whole-mesh frame continuity while dynamic annotations invalidate the productive recorder.</summary>
public sealed partial class D3D12FrameRendererTests {
    /// <summary>Checks every completed frame during curve, marker and TopMost glyph changes with real host invalidation.</summary>
    /// <param name="invalidation">The actual viewport invalidation API exercised before every frame.</param>
    /// <returns>The bounded STA software rendering check without a window or presentation bridge.</returns>
    [Theory]
    [InlineData(InvalidateTypes.Render)]
    [InlineData(InvalidateTypes.SceneGraph)]
    [InlineData(InvalidateTypes.PerFrameRenderables)]
    [Trait("Category", "Warp")]
    public Task WarpCurveEditsRetainMeshInEveryInvalidatedFrame(InvalidateTypes invalidation) {
        var output = TestContext.Current.TestOutputHelper!;
        return StaThread.RunAsync(() => {
            using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
            using var frame = new OffscreenFrame(effects, 32, 32);
            VerifyTopMostWarp(frame.Device);
            using var viewport = new Viewport3DX { MinimumUpdateCount = 1 };
            using var root = new InvalidatingCurveGroup();
            using var mesh = CreateGreenQuad();
            using var curve = new LineNode {
                Geometry = CurveGeometry(0),
                Material = new LineMaterialCore {
                    LineColor = new Vector4(1, 0, 0, 1), Thickness = 2, Smoothness = 0,
                    FixedSize = true, EnableDistanceFading = false
                }
            };
            using var replacement = new LineNode { Geometry = CurveGeometry(-.1f), Material = curve.Material };
            using var marker = new PointNode {
                Geometry = new PointGeometry3D { Positions = new Vector3Collection([new(0, 0, .1f)]) },
                Material = new PointMaterialCore { PointColor = new Vector4(1, 0, 0, 1), Width = 3, Height = 3 }
            };
            using var top = new TopMostGroupNode();
            using var text = new BillboardNode {
                Material = new BillboardMaterialCore { FixedSize = true }, Geometry = Label("8")
            };
            top.AddChildNode(text);
            root.AddChildNode(mesh);
            var roots = new SceneNode[] { root };
            var camera = CreateCamera();
            var frameId = 0;
            object? meshBuffer = null;
            var geometry = mesh.Geometry;
            var target = frame.Target;

            Render("baseline", 1);
            root.AddChildNode(curve);
            Render("add curve", 2);
            curve.Geometry = CurveGeometry(.1f);
            Render("replace curve geometry", 2);
            curve.Geometry.Positions = CurveGeometry(-.1f).Positions;
            Render("edit attached curve positions", 2);
            root.AddChildNode(marker);
            Render("add marker", 3);
            marker.ModelMatrix = SilkMath.Translation(new Vector3(.1f, .1f, 0));
            Render("move marker", 3);
            root.AddChildNode(top);
            Render("add TopMost glyph", 4);
            text.Geometry = Label("3");
            Render("replace glyph geometry", 4);
            ((BillboardSingleText3D)text.Geometry).TextInfo = new TextInfo {
                Text = "6", Origin = new Vector3(.75f, 0, -.5f)
            };
            Render("edit attached glyph", 4);
            root.RemoveChildNode(curve);
            Render("remove curve", 3);
            root.AddChildNode(replacement);
            Render("add replacement curve", 4);
            root.RemoveChildNode(marker);
            Render("remove marker", 3);
            top.EnableTopMost = false;
            Render("disable TopMost", 3);
            root.RemoveChildNode(top);
            Render("remove TopMost", 2);
            replacement.Geometry = null;
            Render("curve temporarily null", 1);
            replacement.Geometry = new LineGeometry3D();
            Render("curve temporarily empty", 1);
            replacement.Geometry = CurveGeometry(0);
            Render("curve ready again", 2);
            root.RemoveChildNode(replacement);
            Render("mesh alone again", 1);
            var callbacks = 0;
            root.DuringNextUpdate = () => {
                callbacks++;
                viewport.InvalidateSceneGraph();
                mesh.InvalidateRender();
                Assert.False(viewport.RenderDemand.TryBeginFrame(out _));
            };
            Render("invalidate during update", 1);
            Assert.Equal(1, callbacks);
            Assert.True(viewport.RenderDemand.HasRenderDemand);
            Render("consume deferred invalidation", 1, request: false);
            Assert.False(viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(20, frameId);

            /// <summary>Invokes a public host API and records exactly one production frame before checking native pixels.</summary>
            /// <param name="change">The mutation preceding this frame.</param>
            /// <param name="expectedDraws">The exact count of native geometry draws.</param>
            /// <param name="request">Whether to make a new request rather than consume retained demand.</param>
            void Render(string change, int expectedDraws, bool request = true) {
                if (request) {
                    switch (invalidation) {
                        case InvalidateTypes.Render: viewport.InvalidateRender(); break;
                        case InvalidateTypes.SceneGraph: viewport.InvalidateSceneGraph(); break;
                        case InvalidateTypes.PerFrameRenderables: viewport.InvalidatePerFrameRenderables(); break;
                    }
                }
                var reserved = viewport.RenderDemand.TryBeginFrame(out var flags);
                Assert.True(reserved);
                frame.Context.Reset();
                var draws = frame.Renderer.RecordViewportFrame(frame.Context, in target, 1,
                    [0, 0, 1, 1], roots, camera, false, autoUpdateOctree: false, renderHost: viewport);
                var pixels = frame.Readback(in target);
                viewport.RenderDemand.CompleteFrame(true);
                var deferred = viewport.RenderDemand.HasRenderDemand;
                frameId++;
                Assert.Equal(expectedDraws, draws);
                Assert.Contains(frame.Renderer.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, mesh));
                Assert.Contains(mesh, Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes"));
                Assert.True(mesh.Visible);
                Assert.True(mesh.RenderCore.IsAttached);
                Assert.Same(geometry, mesh.Geometry);
                var currentBuffer = ((GeometryRenderCore)mesh.RenderCore).GeometryBuffer;
                meshBuffer ??= currentBuffer;
                Assert.Same(meshBuffer, currentBuffer);
                var greenInterior = 0;
                // Inspect the whole 14x14 interior, including pixels legitimately covered by opaque red geometry.
                for (var y = 9; y <= 22; y++) {
                    for (var x = 9; x <= 22; x++) {
                        var offset = (y * 32 + x) * 4;
                        Assert.Equal(0, pixels[offset + 2]);
                        Assert.Equal(255, pixels[offset + 3]);
                        Assert.InRange(pixels[offset] + pixels[offset + 1], 254, 256);
                        if (pixels[offset + 1] == 255) greenInterior++;
                    }
                }
                // Four fixed interior samples avoid the curve, marker and glyph; also check the independent background.
                foreach (var (x, y) in new (uint X, uint Y)[] { (10, 10), (21, 10), (10, 21), (21, 21) })
                    AssertPixel(pixels, 32, x, y, 0, 255, 0);
                AssertPixel(pixels, 32, 0, 0, 0, 0, 255);
                var red = CountRed(pixels);
                if (expectedDraws > 1) Assert.True(red > 0, "Dynamic geometry must produce actual red pixels");
                else Assert.Equal(0, red);
                output.WriteLine($"{invalidation} frame={frameId:D2} change={change} flags={flags} " +
                    $"draws={draws} nodes={frame.Renderer.PerFrameFlattenedScene.Count} " +
                    $"interior=196/196 greenInterior={greenInterior} meshSamples=4x(0,255,0,255) " +
                    $"background=(0,0,255,255) redPixels={red} deferred={deferred}");
            }
        });
    }

    /// <summary>Creates a two-vertex source curve in front of the ordinary mesh.</summary>
    /// <param name="y">The line height used to replace or dirty attached geometry.</param>
    /// <returns>The minimal indexed curve geometry.</returns>
    private static LineGeometry3D CurveGeometry(float y) => new() {
        Positions = new Vector3Collection([new(-.35f, y, .01f), new(.35f, y, .01f)]),
        Indices = new IntCollection([0, 1])
    };

    /// <summary>Creates a real rasterized glyph outside the four mesh sample locations.</summary>
    /// <param name="value">The text for this geometry generation.</param>
    /// <returns>The productive billboard text geometry.</returns>
    private static BillboardSingleText3D Label(string value) => new() {
        TextInfo = new TextInfo { Text = value, Origin = new Vector3(.75f, 0, -.5f) },
        FontSize = 8, FontColor = new Vector4(1, 0, 0, 1), BackgroundColor = new Vector4(0, 0, 0, 0)
    };

    /// <summary>Raises one real invalidation from inside productive scene preparation.</summary>
    private sealed class InvalidatingCurveGroup : GroupNode {
        /// <summary>Gets or sets the single callback consumed by the next actual update.</summary>
        internal Action? DuringNextUpdate { get; set; }

        /// <inheritdoc />
        public override void Update(RenderContext context) {
            base.Update(context);
            var callback = DuringNextUpdate;
            DuringNextUpdate = null;
            callback?.Invoke();
        }
    }
}
