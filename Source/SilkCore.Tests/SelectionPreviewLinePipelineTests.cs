using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Exercises the existing point/line pipeline and included arrow shaders through actual WARP readback.</summary>
/// <remarks>These authored draft cases are unrun and do not establish native Meshmixer optical equivalence.</remarks>
public sealed class SelectionPreviewLinePipelineTests {
    /// <summary>Verifies the ordinary fixed-size GS emits no endpoint glyph for zero projected direction.</summary>
    /// <param name="distinctDepth">Whether the endpoints differ only along the view direction.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpZeroProjectedDirectionProducesNoPixelsTest(bool distinctDepth) {
        var a = new Vector3(0, 0, 0.25f);
        var b = distinctDepth ? new Vector3(0, 0, 0.75f) : a;
        var pixels = RenderLine(a, b, true, 1, 1.5f, DefaultRenderTechniqueNames.Lines);
        var redCount = Enumerable.Range(0, pixels.Length / 4).Count(index => pixels[index * 4] > 200);

        Assert.Equal(0, redCount);
    }

    /// <summary>Verifies visible oblique geometry still records ribbons through ordinary and perspective cameras.</summary>
    /// <param name="perspective">Whether the existing perspective camera builds the host matrices.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpOrdinaryLineSurvivesProjectedDirectionGuardTest(bool perspective) {
        var z = perspective ? -2 : 0.5f;
        var pixels = RenderLine(new Vector3(-0.5f, -0.25f, z), new Vector3(0.5f, 0.25f, z),
            true, 1, 1.5f, DefaultRenderTechniqueNames.Lines, perspective);
        var redCount = Enumerable.Range(0, pixels.Length / 4).Count(index => pixels[index * 4] > 200);

        Assert.True(redCount > 0);
    }

    /// <summary>Verifies the existing w=0 clipping singularity emits no nonfinite fixed-size ribbon.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpEyePlaneCrossingSingularityProducesNoPixelsTest() {
        var pixels = RenderLine(new Vector3(-0.5f, -0.25f, 1), new Vector3(0.5f, 0.25f, -1),
            true, 1, 1.5f, DefaultRenderTechniqueNames.Lines, true);
        var redCount = Enumerable.Range(0, pixels.Length / 4).Count(index => pixels[index * 4] > 200);

        Assert.Equal(0, redCount);
    }

    /// <summary>Verifies actual existing GS expansion retains the requested full physical width across DPI.</summary>
    /// <param name="width">The desired full physical width, independent of logical DPI.</param>
    /// <param name="dpi">The actual DPI carried in the ordinary global transform payload.</param>
    [Theory]
    [InlineData(3, 1f)]
    [InlineData(3, 1.25f)]
    [InlineData(3, 1.5f)]
    [InlineData(3, 2f)]
    [InlineData(5, 1f)]
    [InlineData(5, 1.25f)]
    [InlineData(5, 1.5f)]
    [InlineData(5, 2f)]
    [Trait("Category", "Warp")]
    public void WarpExistingLineThicknessIsHalfWidthTimesDpiTest(int width, float dpi) {
        var pixels = RenderLine(new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f),
            true, dpi, (width * 0.5f) / dpi, DefaultRenderTechniqueNames.Lines);
        var rows = Enumerable.Range(0, 32).Count(row => pixels[(row * 32 + 16) * 4] > 200);

        Assert.Equal(width, rows);
    }

    /// <summary>Verifies the unchanged nonfixed path and both included arrow entry points still create real PSOs.</summary>
    /// <param name="technique">The actual existing line or arrow technique.</param>
    /// <param name="fixedSize">Whether the caller uses fixed-pixel or ordinary nonfixed line construction.</param>
    [Theory]
    [InlineData(DefaultRenderTechniqueNames.Lines, false)]
    [InlineData(DefaultRenderTechniqueNames.LinesArrowHead, true)]
    [InlineData(DefaultRenderTechniqueNames.LinesArrowHead, false)]
    [InlineData(DefaultRenderTechniqueNames.LinesArrowHeadTail, true)]
    [InlineData(DefaultRenderTechniqueNames.LinesArrowHeadTail, false)]
    [Trait("Category", "Warp")]
    public void WarpExistingArrowIncludesAndNonfixedPathRemainUsableTest(string technique, bool fixedSize) {
        var thickness = fixedSize ? 1.5f : 0.1f;
        var pixels = RenderLine(new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f),
            fixedSize, 1, thickness, technique);
        var redCount = Enumerable.Range(0, pixels.Length / 4).Count(index => pixels[index * 4] > 200);

        Assert.True(redCount > 0);
    }

    /// <summary>Records one existing PointLineRenderCore through its real shader stages and returns fenced pixels.</summary>
    /// <param name="a">The first model-space endpoint.</param>
    /// <param name="b">The second model-space endpoint.</param>
    /// <param name="fixedSize">The existing material fixed-size flag.</param>
    /// <param name="dpi">The actual global DPI value.</param>
    /// <param name="thickness">The unchanged material half-width parameter.</param>
    /// <param name="technique">The existing registered technique to resolve.</param>
    /// <param name="perspective">Whether to use the existing host perspective camera core.</param>
    /// <returns>Tightly packed 32 by 32 RGBA pixels.</returns>
    private static byte[] RenderLine(Vector3 a, Vector3 b, bool fixedSize, float dpi,
        float thickness, string technique, bool perspective = false) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var signature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var description = DefaultEffectsManager.LoadTechniqueDescriptions().Single(item => item.Name == technique);
        var passDescription = description.PassDescriptions!.Single(item => item.Name == DefaultPassNames.Default);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device, signature, cache,
            description.InputLayoutDescription, PrimitiveTopology.LineList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var bindings = new SilkD3D12PointLineBindings(device, resourceHeap, samplerHeap);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        using var geometry = new DefaultLineGeometryBufferModel {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([a, b]),
                Indices = new IntCollection([0, 1])
            }
        };
        var material = new LineMaterialCore {
            LineColor = new Vector4(1, 0, 0, 1), Thickness = thickness,
            FixedSize = fixedSize, Smoothness = 0, EnableDistanceFading = false
        };
        using var core = new PointLineRenderCore { GeometryBuffer = geometry, D3D12Material = material };
        var view = Matrix.Identity;
        var projection = Matrix.Identity;
        if (perspective) {
            var camera = new HelixToolkit.SharpDX.Core.Cameras.PerspectiveCameraCore {
                Position = Vector3.Zero, LookDirection = new Vector3(0, 0, -1), UpDirection = Vector3.UnitY,
                FieldOfView = 60, NearPlaneDistance = 0.01f, FarPlaneDistance = 10
            };
            view = camera.CreateViewMatrix();
            projection = camera.CreateProjectionMatrix(1);
        }
        var transforms = new GlobalTransformStruct {
            View = view, Projection = projection, ViewProjection = view * projection,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32),
            Resolution = new Vector4(32, 32, 1f / 32, 1f / 32),
            EyePos = new Vector3(0, 0, 2), DpiScale = dpi, IsPerspective = perspective
        };
        device.CreateRenderTargetView(target, targetView);
        var footprint = device.GetCopyableFootprint(target, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.Attach();
        context.Reset();
        context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
        context.SetRenderTarget(targetView);
        context.SetViewport(32, 32);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        var recorded = core.TryRenderD3D12(context, resources, pass, bindings, in transforms);
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, target, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
        device.ThrowIfDeviceRemoved();
        var pixels = new byte[32 * 32 * 4];
        for (uint row = 0; row < 32; row++) {
            var bytes = readback.Read(32 * 4, footprint.Offset + (ulong) row * footprint.Footprint.RowPitch);
            bytes.CopyTo(pixels, checked((int) row * 32 * 4));
        }
        core.Detach();
        Assert.True(recorded);
        return pixels;
    }
}