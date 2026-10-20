using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using CullMode = HelixToolkit.SharpDX.Core.Native.CullMode;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>Checks additive effect RGB parity and premultiplied coverage through real WARP render passes.</summary>
public sealed class P5AdditiveEffectParityTests {
    /// <summary>The physical image extent, including untouched background and effect boundaries.</summary>
    private const uint Size = 32;

    /// <summary>Preserves legacy Bloom RGB over green coverage, including actual downsampled blur.</summary>
    /// <param name="backgroundAlpha">The initial premultiplied green background coverage.</param>
    /// <param name="blurPasses">The number of productive Bloom blur pairs.</param>
    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.5f, 0)]
    [InlineData(1f, 0)]
    [InlineData(0f, 1)]
    [InlineData(0.5f, 1)]
    [InlineData(1f, 1)]
    [Trait("Category", "Warp")]
    public void WarpBloomPreservesAdditiveRgbAndCoverage(float backgroundAlpha, int blurPasses) {
        var legacy = RenderPixels(D3D12ColorOutputMode.Legacy, true, backgroundAlpha, blurPasses);
        var profile = RenderPixels(D3D12ColorOutputMode.Premultiplied, true, backgroundAlpha, blurPasses);
        AssertRgbParityAndCoverage(legacy, profile);
        var center = checked((int) ((Size / 2 * Size + Size / 2) * 4));
        Assert.True(profile[center + 2] > 128);
        if (blurPasses == 0) {
            Assert.InRange(profile[center + 2], 254, 255);
            Assert.InRange(
                profile[center + 1], Math.Max(0, (int) (backgroundAlpha * 255) - 2),
                Math.Min(255, (int) (backgroundAlpha * 255) + 2)
            );
            Assert.InRange(profile[center + 3], 254, 255);
        }
    }

    /// <summary>Retains underlying green when red XRay uses opaque or faded coverage in either output profile.</summary>
    /// <param name="backgroundAlpha">The initial premultiplied green background coverage.</param>
    /// <param name="fading">The original normal-dependent XRay fading factor.</param>
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 0f)]
    [InlineData(1f, 0f)]
    [InlineData(0f, 0.5f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1f, 0.5f)]
    [Trait("Category", "Warp")]
    public void WarpXRayPreservesAdditiveRgbAndCoverage(float backgroundAlpha, float fading) {
        var legacy = RenderPixels(D3D12ColorOutputMode.Legacy, false, backgroundAlpha, fading: fading);
        var profile = RenderPixels(D3D12ColorOutputMode.Premultiplied, false, backgroundAlpha, fading: fading);
        AssertRgbParityAndCoverage(legacy, profile);
        var center = checked((int) ((Size / 2 * Size + Size / 2) * 4));
        Assert.InRange(
            profile[center + 1], Math.Max(0, (int) (backgroundAlpha * 255) - 1),
            Math.Min(255, (int) (backgroundAlpha * 255) + 1)
        );
        Assert.InRange(
            profile[center + 2], fading == 0
                ? 255
                : 127, fading == 0
                ? 255
                : 130
        );
        var backgroundByte = (int) MathF.Round(backgroundAlpha * 255);
        for (var index = 0; index < profile.Length; index += 4) {
            Assert.Equal(backgroundByte, legacy[index + 3]);
            var expectedAlpha = Math.Min(255, profile[index + 2] + backgroundByte);
            Assert.InRange(Math.Abs(profile[index + 3] - expectedAlpha), 0, 1);
        }

        if (backgroundAlpha == 0) Assert.Equal(new byte[4], profile[..4]);
    }

    /// <summary>Applies fractional XRay effect alpha once, adds coverage, and preserves every background pixel.</summary>
    /// <param name="effectAlpha">The profile's straight XRay effect alpha.</param>
    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [Trait("Category", "Warp")]
    public void WarpXRayEffectAlphaIsAppliedOnce(float effectAlpha) {
        var profile = RenderPixels(D3D12ColorOutputMode.Premultiplied, false, 0.5f, effectAlpha: effectAlpha);
        // Legacy XRay ignores effect alpha, so match its emitted intensity explicitly for the RGB comparison.
        var legacy = RenderPixels(D3D12ColorOutputMode.Legacy, false, 0.5f, effectIntensity: effectAlpha);
        AssertRgbParityAndCoverage(legacy, profile);
        var expectedRed = (int) MathF.Round(effectAlpha * 255);
        var expectedAlpha = Math.Min(255, 128 + expectedRed);
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++) {
            var index = checked((int) ((y * Size + x) * 4));
            var inside = x >= Size / 4 && x < Size * 3 / 4 && y >= Size / 4 && y < Size * 3 / 4;
            Assert.Equal(0, profile[index]);
            Assert.Equal(128, profile[index + 1]);
            Assert.InRange(
                profile[index + 2], Math.Max(
                    0, (inside
                        ? expectedRed
                        : 0) - 1
                ),
                Math.Min(
                    255, (inside
                        ? expectedRed
                        : 0) + 1
                )
            );
            Assert.InRange(
                profile[index + 3], (inside
                    ? expectedAlpha
                    : 128) - 1,
                Math.Min(
                    255, (inside
                        ? expectedAlpha
                        : 128) + 1
                )
            );
            for (var channel = 0; channel < 3; channel++)
                Assert.InRange(profile[index + channel], 0, profile[index + 3]);
        }
    }

    /// <summary>Compares every emitted RGB channel and checks coverage at backgrounds, interiors and boundaries.</summary>
    /// <param name="legacy">The completed legacy BGRA image.</param>
    /// <param name="profile">The completed premultiplied BGRA image.</param>
    private static void AssertRgbParityAndCoverage(byte[] legacy, byte[] profile) {
        Assert.Equal(legacy.Length, profile.Length);
        for (var index = 0; index < profile.Length; index += 4)
        for (var channel = 0; channel < 3; channel++) {
            Assert.InRange(Math.Abs(profile[index + channel] - legacy[index + channel]), 0, 1);
            Assert.InRange(profile[index + channel], 0, profile[index + 3]);
        }
    }

    /// <summary>Runs the existing native renderer, catalog and Bloom or depth-tested XRay path into BGRA readback.</summary>
    /// <param name="outputMode">The immutable shader and blend profile.</param>
    /// <param name="bloom">Whether to add Bloom to a red mesh instead of drawing occluded red XRay.</param>
    /// <param name="backgroundAlpha">The initial premultiplied green background coverage.</param>
    /// <param name="blurPasses">The Bloom blur-pair count.</param>
    /// <param name="effectAlpha">The straight XRay effect alpha, ignored by the legacy XRay shader.</param>
    /// <param name="fading">The XRay normal-dependent fading factor.</param>
    /// <param name="effectIntensity">The straight red XRay intensity, independent of effect alpha.</param>
    /// <returns>The fence-completed, tightly packed BGRA image.</returns>
    private static byte[] RenderPixels(
        D3D12ColorOutputMode outputMode,
        bool bloom,
        float backgroundAlpha,
        int blurPasses = 0,
        float effectAlpha = 1,
        float fading = 0,
        float effectIntensity = 1
    ) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2048, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(
            device, resources, samplers,
            Format.FormatB8G8R8A8Unorm, outputMode
        );
        using var catalog = new D3D12ScenePassCatalog(
            device, Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint, outputMode: outputMode
        );
        using var rtvs = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var rtv = rtvs.Allocate();
        using var target = device.CreateRenderTargetTexture2D(Size, Size, Format.FormatB8G8R8A8Unorm);
        using var dsvs = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var dsv = dsvs.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(Size, Size, Format.FormatD32FloatS8X24Uint);
        device.CreateRenderTargetView(target, rtv);
        device.CreateDepthStencilView(depth, dsv);
        using var node = new MeshNode {
            IsTransparent = true,
            CullMode = CullMode.None,
            FrontCcw = false,
            Material = new DiffuseMaterialCore { EnableUnLit = true, DiffuseColor = new Vector4(1, 0, 0, 0.5f) },
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection(
                    [
                        new(-0.5f, 0.5f, 0.6f), new(0.5f, 0.5f, 0.6f),
                        new(-0.5f, -0.5f, 0.6f), new(0.5f, -0.5f, 0.6f)
                    ]
                ),
                Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        var nodes = new FastList<SceneNode> { node };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(Size, Size, 1f / Size, 1f / Size),
            Resolution = new Vector4(Size, Size, 1f / Size, 1f / Size),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        renderer.BeginFrame();
        context.Reset();
        context.ClearRenderTarget(target, rtv, [0, backgroundAlpha, 0, backgroundAlpha]);
        context.ClearDepthStencil(
            depth, dsv, bloom
                ? 1f
                : 0.1f, 0
        );
        context.SetRenderTargets(rtv, dsv);
        context.SetViewport(Size, Size);
        if (bloom) {
            Assert.Equal(1, renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum));
            // Zero thresholds and unit saturation/intensity preserve equal input RGB across both shader profiles.
            var effect = new BorderEffectStruct {
                Color = default, Param = new Matrix { M11 = 1, M12 = 1, M13 = 1, M14 = 1 }, ViewportScale = 1
            };
            renderer.RenderBloom(
                context, target, rtv,
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.ScreenQuad),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.EffectBlurVertical),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.EffectBlurHorizontal),
                catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.MeshOutline),
                in effect, blurPasses, in transforms, Size, Size
            );
        } else {
            var effect = new BorderEffectStruct {
                Color = new Vector4(effectIntensity, 0, 0, effectAlpha), Param = new Matrix { M11 = fading }
            };
            Func<SceneNode, ShaderPass?>[] selectors = [
                selected => catalog.ResolvePostEffectGeometry(
                    selected, DefaultPassNames.EffectMeshXRayP2,
                    Format.FormatD32FloatS8X24Uint, 0
                )
            ];
            Assert.Equal(
                1, renderer.RenderXRay(
                    context, nodes, selectors, in effect, in transforms,
                    false, ref frustum, rtv, dsv, Size, Size
                )
            );
        }

        var footprint = device.GetCopyableFootprint(target, out var bytes);
        using var readback = device.CreateBuffer(bytes, HeapType.Readback);
        context.Transition(target, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, target, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
        device.ThrowIfDeviceRemoved();
        var pixels = new byte[Size * Size * 4];
        for (uint y = 0; y < Size; y++)
            readback.Read(checked((int) Size * 4), footprint.Offset + y * footprint.Footprint.RowPitch)
                .CopyTo(pixels, checked((int) (y * Size * 4)));
        return pixels;
    }
}