using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises the actual presentation effect dispatcher with original parsed attributes and offscreen GPU readback.
public sealed class NativePostEffectDispatchTests {
    /// <summary>
    ///     Checks both draw modes, both node colors, transparent participation and live attribute replacement.
    /// </summary>
    /// <param name="separated">Whether each object's interior is excluded independently.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpBorderAttributesAndDrawModeTest(bool separated) => VerifyBorder(SilkDriverType.Warp, separated);

    /// <summary>
    ///     Repeats both draw-mode and live-attribute physical oracles on hardware.
    /// </summary>
    /// <param name="separated">Whether each object's interior is excluded independently.</param>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    public void HardwareBorderAttributesAndDrawModeTest(bool separated) => VerifyBorder(SilkDriverType.Hardware, separated);

    /// <summary>
    ///     Checks border/glow identity, half-resolution six-pair glow and the literal source highlight color.
    /// </summary>
    /// <param name="geometryKind">The ordinary, registered slot-four or GPU-skinned geometry variant.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Category", "Warp")]
    public void WarpGlowAndBorderIdentityTest(int geometryKind) => VerifyGlow(SilkDriverType.Warp, geometryKind);

    /// <summary>
    ///     Checks simultaneous effect identity and the literal highlight producer on hardware.
    /// </summary>
    /// <param name="geometryKind">The original geometry/layout variant.</param>
    [Theory(Explicit = true)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Category", "Hardware")]
    public void HardwareGlowAndBorderIdentityTest(int geometryKind) => VerifyGlow(SilkDriverType.Hardware, geometryKind);

    /// Checks original indexed line masks, live effect colors and source single-pass XRay line-material behavior.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpLinePostEffectsTest() => VerifyLine(SilkDriverType.Warp);

    /// Repeats indexed line effect coverage and depth checks on hardware.
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public void HardwareLinePostEffectsTest() => VerifyLine(SilkDriverType.Hardware);

    /// <summary>
    ///     Checks true depth occlusion, single/double-pass stencil, attributes and original registered/skinned geometry.
    /// </summary>
    /// <param name="doublePass">Whether the source stencil overlap prepass is enabled.</param>
    /// <param name="geometryKind">Zero for ordinary, one for registered slot-four instances, two for bone skinning.</param>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [Trait("Category", "Warp")]
    public void WarpXRayOcclusionAndGeometryTest(bool doublePass, int geometryKind) =>
        VerifyXRay(SilkDriverType.Warp, doublePass, geometryKind);

    /// <summary>
    ///     Repeats the actual XRay depth and original geometry assertions on hardware.
    /// </summary>
    /// <param name="doublePass">Whether the source stencil overlap prepass is enabled.</param>
    /// <param name="geometryKind">The original geometry/layout variant.</param>
    [Theory(Explicit = true)]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [Trait("Category", "Hardware")]
    public void HardwareXRayOcclusionAndGeometryTest(bool doublePass, int geometryKind) =>
        VerifyXRay(SilkDriverType.Hardware, doublePass, geometryKind);

    /// <summary>
    ///     Checks preserved manipulator grid density, dimming, blending and depth-occlusion policy.
    /// </summary>
    /// <param name="occlusion">Whether the unoccluded half of the selected mesh must be removed.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpManipulatorGridReadbackTest(bool occlusion) => VerifyGrid(SilkDriverType.Warp, occlusion);

    /// <summary>
    ///     Repeats the exact manipulator grid equations on hardware.
    /// </summary>
    /// <param name="occlusion">Whether visible geometry is removed from the stencil.</param>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    public void HardwareManipulatorGridReadbackTest(bool occlusion) => VerifyGrid(SilkDriverType.Hardware, occlusion);

    /// <summary>
    ///     Uses two overlapping indexed rectangles to discriminate merged and separated stencil interiors.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    /// <param name="separated">Whether each object's border is composited independently.</param>
    private static void VerifyBorder(SilkDriverType driver, bool separated) {
        using var fixture = new Fixture(driver);
        using var left = Rectangle(8, 16, 36, 48);
        using var right = Rectangle(28, 16, 56, 48);
        left.IsTransparent = true;
        left.PostEffects = "border[color:#00FF00]";
        right.PostEffects = "border[color:#0000FF]";
        using var border = new NodePostEffectBorderHighlight {
            EffectName = "border", Color = new Vector4(1, 0, 0, 1), NumberOfBlurPass = 1,
            DrawMode = separated ? OutlineMode.Separated : OutlineMode.Merged
        };
        var candidates = new FastList<SceneNode> { left, right };
        var effects = new FastList<SceneNode> { border };
        var first = fixture.Render(candidates, effects);
        left.PostEffects = "border[color:#0000FF]";
        right.PostEffects = "border[color:#00FF00]";
        var updated = fixture.Render(candidates, effects);
        var internalEnergy = 0;
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) {
                var index = (y * 64 + x) * 4;
                Assert.Equal(0, first[index]);
                Assert.Equal(0, updated[index]);
                var interior = x >= 8 && x < 56 && y >= 16 && y < 48;
                var far = x < 6 || x >= 58 || y < 14 || y >= 50;
                if (far || interior && !separated) {
                    Assert.Equal(0, first[index + 1]);
                    Assert.Equal(0, first[index + 2]);
                }
                if (x is 7 or 56 && y >= 18 && y < 46) {
                    var channel = x == 7 ? 1 : 2;
                    Assert.InRange(first[index + channel], 100, 255);
                    Assert.Equal(0, first[index + 3 - channel]);
                    Assert.Equal(first[index + channel], updated[index + 3 - channel]);
                }
                if (interior) internalEnergy += first[index + 1] + first[index + 2];
            }
        if (separated) Assert.True(internalEnergy > 5000);
        else Assert.Equal(0, internalEnergy);
        border.NumberOfBlurPass = 0;
        var zeroBlur = fixture.Render(candidates, effects);
        for (var pixel = 0; pixel < 64 * 64; pixel++) {
            Assert.Equal(0, zeroBlur[pixel * 4]);
            Assert.Equal(0, zeroBlur[pixel * 4 + 1]);
            Assert.Equal(0, zeroBlur[pixel * 4 + 2]);
        }
    }

    /// <summary>
    ///     Discriminates two effect identities sharing one mesh and checks source literal parsing without correction.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    /// <param name="geometryKind">The original geometry/layout variant.</param>
    private static void VerifyGlow(SilkDriverType driver, int geometryKind) {
        using var registry = geometryKind == 1 ? SlotFourRegistry() : null;
        using var fixture = new Fixture(driver, registry);
        using var node = Rectangle(20, 20, 44, 44, geometryKind: geometryKind);
        node.IsTransparent = true;
        node.PostEffects = "border[color:#00FF00]; glow[color:#0000FF]";
        using var border = new NodePostEffectBorderHighlight { EffectName = "border", NumberOfBlurPass = 1 };
        using var glow = new NodePostEffectMeshOutlineBlur { EffectName = "glow", NumberOfBlurPass = 6 };
        var candidates = new FastList<SceneNode> { node };
        var blue = fixture.Render(candidates, new FastList<SceneNode> { glow });
        var both = fixture.Render(candidates, new FastList<SceneNode> { glow, border });
        var distantBlue = 0;
        var greenBorder = 0;
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) {
                var index = (y * 64 + x) * 4;
                var interior = x >= 20 && x < 44 && y >= 20 && y < 44;
                Assert.Equal(0, both[index]);
                if (interior) {
                    Assert.Equal(0, both[index + 1]);
                    Assert.Equal(0, both[index + 2]);
                }
                if (x < 18 || x >= 46 || y < 18 || y >= 46) {
                    Assert.Equal(0, both[index + 1]);
                    Assert.Equal(blue[index + 2], both[index + 2]);
                    distantBlue += both[index + 2];
                }
                greenBorder += both[index + 1];
            }
        Assert.True(distantBlue > 1000);
        Assert.True(greenBorder > 5000);
        node.PostEffects = "border[color:#004CF]; glow[color:#0000FF];";
        var sourceLiteral = fixture.Render(candidates, new FastList<SceneNode> { glow, border });
        for (var pixel = 0; pixel < 64 * 64; pixel++) {
            Assert.Equal(blue[pixel * 4], sourceLiteral[pixel * 4]);
            Assert.Equal(blue[pixel * 4 + 1], sourceLiteral[pixel * 4 + 1]);
            Assert.Equal(blue[pixel * 4 + 2], sourceLiteral[pixel * 4 + 2]);
        }
        var parsed = "#004CF".ToColor4();
        var expectedColor = new Vector4(0, 4f / 255, 207f / 255, 0);
        Assert.Equal(expectedColor, parsed);
    }

    /// <summary>
    ///     Checks all pixels against rectangle visibility through an actual half-screen scene occluder.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    /// <param name="doublePass">Whether overlapping back-facing fragments are reduced by a stencil prepass.</param>
    /// <param name="geometryKind">The registered mesh/layout or skinning variant.</param>
    private static void VerifyXRay(SilkDriverType driver, bool doublePass, int geometryKind) {
        using var registry = geometryKind == 1 ? SlotFourRegistry() : null;
        using var fixture = new Fixture(driver, registry);
        using var node = Rectangle(16, 16, 48, 48, geometryKind: geometryKind);
        node.PostEffects = "xrayLine[color:#FF0000]";
        node.IsTransparent = true;
        using var occluder = Rectangle(0, 0, 32, 64, 0.25f);
        occluder.Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(0, 1, 0, 1), EnableUnLit = true };
        using var xray = new NodePostEffectXRay {
            EffectName = "xrayLine", Color = new Vector4(0, 0, 1, 1), OutlineFadingFactor = 0,
            EnableDoublePass = doublePass
        };
        var candidates = new FastList<SceneNode> { node };
        var scene = new FastList<SceneNode> { occluder };
        var first = fixture.Render(candidates, new FastList<SceneNode> { xray }, scene);
        node.PostEffects = "xrayLine[color:#0000FF]";
        var updated = fixture.Render(candidates, new FastList<SceneNode> { xray }, scene);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) {
                var index = (y * 64 + x) * 4;
                var selectedBehind = x >= 16 && x < 32 && y >= 16 && y < 48;
                Assert.Equal(selectedBehind ? 255 : 0, first[index]);
                Assert.Equal(x < 32 ? 255 : 0, first[index + 1]);
                Assert.Equal(0, first[index + 2]);
                Assert.Equal(0, updated[index]);
                Assert.Equal(first[index + 1], updated[index + 1]);
                Assert.Equal(selectedBehind ? 255 : 0, updated[index + 2]);
            }
    }

    /// <summary>
    ///     Verifies the original diffuse grid equation at every pixel, including unoccluded and background regions.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    /// <param name="occlusion">Whether visible selected fragments are excluded.</param>
    private static void VerifyGrid(SilkDriverType driver, bool occlusion) {
        using var fixture = new Fixture(driver);
        using var node = Rectangle(16, 16, 48, 48);
        node.Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(0.8f, 0.4f, 0.2f, 1), EnableUnLit = true };
        node.PostEffects = "ManipulatorXRayGrid[color:#0000FF]";
        node.IsTransparent = true;
        using var occluder = Rectangle(0, 0, 32, 64, 0.25f);
        occluder.Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(0, 1, 0, 1), EnableUnLit = true };
        using var grid = new NodePostEffectXRayGrid {
            EffectName = "ManipulatorXRayGrid", GridDensity = 8, DimmingFactor = 0.5f, BlendingFactor = 0.25f,
            XRayDrawingPassName = DefaultPassNames.EffectMeshDiffuseXRayGridP3, UseDepthOcclusion = occlusion
        };
        var candidates = new FastList<SceneNode> { node };
        var effects = new FastList<SceneNode> { grid };
        foreach (var density in new[] { 8, 5 }) {
            grid.GridDensity = density;
            var pixels = fixture.Render(candidates, effects, new FastList<SceneNode> { occluder });
            for (var y = 0; y < 64; y++)
                for (var x = 0; x < 64; x++) {
                    var index = (y * 64 + x) * 4;
                    var selected = x >= 16 && x < (occlusion ? 32 : 48) && y >= 16 && y < 48;
                    var expected = x < 32 ? new Vector3(0, 1, 0) : Vector3.Zero;
                    if (selected) {
                        expected = new Vector3(0.4f, 0.2f, 0.1f);
                        var gridPixel = Math.Abs(x - y) % density == 0 || (x + y) % density == 0;
                        if (gridPixel) expected = expected * 0.75f + new Vector3(0, 0, 0.25f);
                    }
                    Assert.InRange(Math.Abs(pixels[index] - expected.X * 255), 0, 1.1f);
                    Assert.InRange(Math.Abs(pixels[index + 1] - expected.Y * 255), 0, 1.1f);
                    Assert.InRange(Math.Abs(pixels[index + 2] - expected.Z * 255), 0, 1.1f);
                }
        }
    }

    /// <summary>
    ///     Checks every line-effect pixel and compares XRay against independently rendered original line coverage.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    private static void VerifyLine(SilkDriverType driver) {
        using var fixture = new Fixture(driver);
        using var line = new LineNode {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([new(-0.5f, 0, 0.75f), new(0.5f, 0, 0.75f)]),
                Indices = new IntCollection([0, 1])
            },
            Material = new LineMaterialCore { LineColor = new Vector4(1, 0, 0, 1), Thickness = 6, Smoothness = 0 },
            IsTransparent = true, PostEffects = "border[color:#00FF00]"
        };
        using var border = new NodePostEffectBorderHighlight { EffectName = "border", NumberOfBlurPass = 1 };
        var candidates = new FastList<SceneNode> { line };
        var green = fixture.Render(candidates, new FastList<SceneNode> { border });
        line.PostEffects = "border[color:#0000FF]";
        var blue = fixture.Render(candidates, new FastList<SceneNode> { border });
        var energy = 0;
        for (var pixel = 0; pixel < 4096; pixel++) {
            Assert.Equal(0, green[pixel * 4]);
            Assert.Equal(0, green[pixel * 4 + 2]);
            Assert.Equal(0, blue[pixel * 4]);
            Assert.Equal(0, blue[pixel * 4 + 1]);
            Assert.Equal(green[pixel * 4 + 1], blue[pixel * 4 + 2]);
            energy += green[pixel * 4 + 1];
        }
        Assert.True(energy > 1000);
        var lineOnly = fixture.Render([], [], candidates);
        line.PostEffects = "xrayLine[color:#00FF00]";
        using var xray = new NodePostEffectXRay { EffectName = "xrayLine", EnableDoublePass = false };
        using var occluder = Rectangle(0, 0, 32, 64, 0.25f);
        occluder.Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(0, 1, 0, 1), EnableUnLit = true };
        var occluded = fixture.Render(candidates, new FastList<SceneNode> { xray }, new FastList<SceneNode> { occluder });
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) {
                var pixel = (y * 64 + x) * 4;
                Assert.Equal(x < 32 ? lineOnly[pixel] : 0, occluded[pixel]);
                Assert.Equal(x < 32 ? 255 : 0, occluded[pixel + 1]);
                Assert.Equal(0, occluded[pixel + 2]);
            }
    }

    /// <summary>
    ///     Constructs indexed triangles at exact pixel boundaries; optional skinning transforms original rest vertices.
    /// </summary>
    /// <param name="left">The left pixel boundary.</param>
    /// <param name="top">The top pixel boundary.</param>
    /// <param name="right">The right pixel boundary.</param>
    /// <param name="bottom">The bottom pixel boundary.</param>
    /// <param name="depth">The analytic projected depth.</param>
    /// <param name="geometryKind">The ordinary, registered slot-four or GPU-skinned variant.</param>
    /// <returns>The owned original geometry node.</returns>
    private static MeshNode Rectangle(int left, int top, int right, int bottom, float depth = 0.75f,
        int geometryKind = 0) {
        var positions = new Vector3Collection([new(left / 32f - 1, 1 - top / 32f, depth),
            new(right / 32f - 1, 1 - top / 32f, depth), new(left / 32f - 1, 1 - bottom / 32f, depth),
            new(right / 32f - 1, 1 - bottom / 32f, depth)]);
        MeshGeometry3D geometry = new() {
            Positions = positions,
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        MeshNode node;
        if (geometryKind == 2) {
            var bone = Matrix.Identity;
            bone.M41 = 0.125f;
            geometry = new BoneSkinnedMeshGeometry3D {
                Positions = new Vector3Collection(positions.Select(position => position - new Vector3(0.125f, 0, 0))),
                Normals = geometry.Normals, Indices = geometry.Indices,
                VertexBoneIds = Enumerable.Repeat(new BoneIds { Bone1 = 0, Weights = Vector4.UnitX }, 4).ToList()
            };
            node = new BoneSkinMeshNode { BoneMatrices = [bone] };
        } else node = new MeshNode();
        node.Geometry = geometry;
        node.Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = Vector4.One };
        node.CullMode = CullMode.None;
        node.FrontCcw = false;
        if (geometryKind == 1) {
            node.OnSetRenderTechnique = manager => manager["PostEffectSlotFour"];
            var instance = Matrix.Identity;
            instance.M41 = 0.125f;
            var model = Matrix.Identity;
            model.M41 = -0.125f;
            node.ModelMatrix = model;
            node.Instances = [instance];
        }
        return node;
    }

    /// <summary>
    ///     Registers original mesh passes with their instance matrix moved to the source custom-layout slot four.
    /// </summary>
    /// <returns>The owned technique registry.</returns>
    private static DefaultEffectsManager SlotFourRegistry() {
        var manager = new DefaultEffectsManager();
        var source = manager[DefaultRenderTechniqueNames.Mesh].Description;
        /// <summary>
        ///     Retains original vertex semantics while moving only instance inputs.
        /// </summary>
        /// <param name="layout">The original pass layout.</param>
        /// <returns>The independent registered layout.</returns>
        InputLayoutDescription Move(InputLayoutDescription? layout) {
            var original = layout ?? source.InputLayoutDescription!;
            var elements = original.InputElements!.Select(element => {
                if (element.Classification == InputClassification.PerInstanceData) element.Slot = 4;
                return element;
            }).ToArray();
            return new InputLayoutDescription(original.ShaderByteCode!, elements);
        }
        var passes = source.PassDescriptions!.Select(pass => new HelixToolkit.SharpDX.Core.Shaders.ShaderPassDescription(pass.Name!) {
            ShaderList = pass.ShaderList, InputLayoutDescription = Move(pass.InputLayoutDescription),
            BlendStateDescription = pass.BlendStateDescription, DepthStencilStateDescription = pass.DepthStencilStateDescription,
            RasterStateDescription = pass.RasterStateDescription, BlendFactor = pass.BlendFactor,
            SampleMask = pass.SampleMask, StencilRef = pass.StencilRef, Topology = pass.Topology
        }).ToList();
        manager.AddTechnique(new HelixToolkit.SharpDX.Core.Shaders.TechniqueDescription("PostEffectSlotFour") {
            InputLayoutDescription = Move(null), PassDescriptions = passes
        });
        return manager;
    }

    /// Owns one true offscreen native scene and compares both scene depth/stencil planes around every effect draw.
    private sealed class Fixture : IDisposable {
        /// The physical device.
        private readonly SilkD3D12Device device;
        /// The submission queue.
        private readonly SilkD3D12CommandQueue queue;
        /// The reusable command context.
        private readonly SilkD3D12CommandContext context;
        /// The completion fence.
        private readonly SilkD3D12Fence fence;
        /// The original registered pass catalog.
        private readonly D3D12ScenePassCatalog catalog;
        /// Shader-visible resources shared by the actual renderer.
        private readonly SilkD3D12DescriptorHeap resources;
        /// Shader-visible sampler owner.
        private readonly SilkD3D12DescriptorHeap samplers;
        /// The productive native renderer used by presentation.
        private readonly SilkD3D12SceneRenderer renderer;
        /// Color descriptor owner.
        private readonly SilkD3D12DescriptorHeap targets;
        /// Depth descriptor owner.
        private readonly SilkD3D12DescriptorHeap depths;
        /// Actual offscreen RGBA color.
        private readonly SilkD3D12Resource target;
        /// Original scene depth and stencil.
        private readonly SilkD3D12Resource depth;
        /// The color view.
        private readonly SilkD3D12Descriptor targetView;
        /// The original scene depth/stencil view.
        private readonly SilkD3D12Descriptor depthView;

        /// <summary>
        ///     Allocates an offscreen renderer without any window, swap chain or application.
        /// </summary>
        /// <param name="driver">The requested physical adapter.</param>
        /// <param name="registry">The optional existing custom technique registry.</param>
        internal Fixture(SilkDriverType driver, DefaultEffectsManager? registry = null) {
            device = SilkD3D12DeviceFactory.CreateDefault(driverType: driver);
            queue = device.CreateCommandQueue();
            context = device.CreateCommandContext();
            fence = device.CreateFence();
            catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
                Format.FormatD32FloatS8X24Uint, registry?.TechniqueDescriptions, registry);
            resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 16384, true);
            samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 2048, true);
            renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
            targets = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
            depths = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
            targetView = targets.Allocate();
            depthView = depths.Allocate();
            target = device.CreateRenderTargetTexture2D(64, 64, Format.FormatR8G8B8A8Unorm);
            depth = device.CreateDepthStencilTexture2D(64, 64, Format.FormatD32FloatS8X24Uint);
            device.CreateRenderTargetView(target, targetView);
            device.CreateDepthStencilView(depth, depthView);
        }

        /// <summary>
        ///     Renders actual scene occluders followed by the same effect dispatch method called by presentation.
        /// </summary>
        /// <param name="candidates">Original opaque and transparent geometry carrying parsed post-effect attributes.</param>
        /// <param name="effects">Registered effect nodes in source order.</param>
        /// <param name="scene">The optional ordinary scene occluders.</param>
        /// <returns>All 4096 fence-completed RGBA pixels, excluding only native row padding.</returns>
        internal byte[] Render(FastList<SceneNode> candidates, FastList<SceneNode> effects,
            FastList<SceneNode>? scene = null) {
            renderer.BeginFrame();
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.Transition(depth, ResourceStates.DepthWrite);
            context.ClearRenderTarget(target, targetView, [0, 0, 0, 1]);
            context.ClearDepthStencil(depth, depthView, 1, 7);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(64, 64);
            var transforms = new GlobalTransformStruct {
                View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
                Viewport = new Vector4(64, 64, 1f / 64, 1f / 64),
                Resolution = new Vector4(64, 64, 1f / 64, 1f / 64), EyePos = new Vector3(0, 0, 2), DpiScale = 1
            };
            var frustum = new BoundingFrustum(Matrix.Identity);
            if (scene is not null)
                renderer.RenderVisible(context, scene, catalog.Resolve, in transforms, false, ref frustum);
            var colorFootprint = device.GetCopyableFootprint(target, out var colorBytes);
            var depthFootprint = device.GetCopyableFootprint(depth, 0, out var depthBytes);
            var stencilFootprint = device.GetCopyableFootprint(depth, 1, out var stencilBytes);
            using var color = device.CreateBuffer(colorBytes, HeapType.Readback);
            using var depthBefore = device.CreateBuffer(depthBytes, HeapType.Readback);
            using var depthAfter = device.CreateBuffer(depthBytes, HeapType.Readback);
            using var stencilBefore = device.CreateBuffer(stencilBytes, HeapType.Readback);
            using var stencilAfter = device.CreateBuffer(stencilBytes, HeapType.Readback);
            context.Transition(depth, ResourceStates.CopySource);
            context.CopyTextureToBuffer(depthBefore, depth, in depthFootprint, 0);
            context.CopyTextureToBuffer(stencilBefore, depth, in stencilFootprint, 1);
            context.Transition(depth, ResourceStates.DepthWrite);
            renderer.RenderPostEffects(context, candidates, effects, catalog, in transforms, false, ref frustum,
                target, targetView, depth, depthView, 64, 64);
            Assert.Equal(ResourceStates.DepthWrite, depth.State);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(color, target, in colorFootprint);
            context.Transition(depth, ResourceStates.CopySource);
            context.CopyTextureToBuffer(depthAfter, depth, in depthFootprint, 0);
            context.CopyTextureToBuffer(stencilAfter, depth, in stencilFootprint, 1);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            var originalDepth = ReadRows(depthBefore, in depthFootprint, 256);
            var preservedDepth = ReadRows(depthAfter, in depthFootprint, 256);
            var originalStencil = ReadRows(stencilBefore, in stencilFootprint, 64);
            var preservedStencil = ReadRows(stencilAfter, in stencilFootprint, 64);
            Assert.Equal(originalDepth, preservedDepth);
            Assert.Equal(originalStencil, preservedStencil);
            device.ThrowIfDeviceRemoved();
            return ReadRows(color, in colorFootprint, 256);
        }

        /// <summary>
        ///     Packs every active byte while excluding uninitialized D3D12 row padding.
        /// </summary>
        /// <param name="buffer">The fence-completed native readback.</param>
        /// <param name="footprint">The device-provided planar footprint.</param>
        /// <param name="rowBytes">The active bytes per row.</param>
        /// <returns>The complete packed texture plane.</returns>
        private static byte[] ReadRows(SilkD3D12Resource buffer, in PlacedSubresourceFootprint footprint, int rowBytes) {
            var bytes = new byte[rowBytes * 64];
            for (var y = 0; y < 64; y++) {
                var row = buffer.Read(rowBytes, footprint.Offset + (ulong)y * footprint.Footprint.RowPitch);
                row.CopyTo(bytes, y * rowBytes);
            }
            return bytes;
        }

        /// Completes cleanup after all submitted commands and verifies that every shared descriptor is returned.
        public void Dispose() {
            renderer.Dispose();
            Assert.Equal(0, resources.Count);
            Assert.Equal(0, samplers.Count);
            depth.Dispose();
            target.Dispose();
            depthView.Dispose();
            targetView.Dispose();
            depths.Dispose();
            targets.Dispose();
            resources.Dispose();
            samplers.Dispose();
            catalog.Dispose();
            fence.Dispose();
            context.Dispose();
            queue.Dispose();
            device.Dispose();
        }
    }
}
