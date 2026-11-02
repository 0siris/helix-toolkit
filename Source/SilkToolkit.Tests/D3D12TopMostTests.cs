using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

public sealed partial class D3D12FrameRendererTests {
    /// <summary>Overlay ownership crosses ordinary groups but respects disabling and nested state boundaries.</summary>
    [Fact]
    public void TopMostOwnershipFollowsAncestorsAndEnableFlag() {
        using var top = new TopMostGroupNode();
        using var ordinary = new GroupNode();
        using var nested = new TopMostGroupNode { EnableTopMost = false };
        using var mesh = CreateGreenQuad();
        top.AddChildNode(ordinary);
        ordinary.AddChildNode(nested);
        nested.AddChildNode(mesh);
        Assert.True(D3D12FrameRenderer.IsOverlayRoot(top));
        Assert.False(D3D12FrameRenderer.IsOverlayRoot(nested));
        Assert.Same(top, D3D12FrameRenderer.FindOverlayAncestor(mesh));
        nested.EnableTopMost = true;
        Assert.Same(nested, D3D12FrameRenderer.FindOverlayAncestor(mesh));
        nested.EnableTopMost = false;
        top.EnableTopMost = false;
        Assert.Null(D3D12FrameRenderer.FindOverlayAncestor(mesh));
        Assert.Equal(RenderType.Opaque, mesh.RenderType);
    }

    /// <summary>Uses actual glyph pixels behind an occluder, including camera/scissor restoration after ScreenSpace.</summary>
    /// <returns>The isolated STA text-rasterization and WARP recording check.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarpTopMostTextSurvivesOcclusionAndRestoresMainViewport() => StaThread.RunAsync(() => {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration { EnableSoftwareRendering = true });
        using var frame = new OffscreenFrame(effects);
        VerifyTopMostWarp(frame.Device);
        using var occluder = CreateGreenQuad();
        using var top = new TopMostGroupNode { EnableTopMost = false };
        using var group = new GroupNode();
        using var text = new BillboardNode {
            Material = new BillboardMaterialCore { FixedSize = true },
            Geometry = new BillboardSingleText3D {
                TextInfo = new TextInfo { Text = "8", Origin = new Vector3(0, 0, -0.5f) },
                FontSize = 12,
                FontColor = new Vector4(1, 0, 0, 1),
                BackgroundColor = new Vector4(0, 0, 0, 0)
            }
        };
        group.AddChildNode(text);
        top.AddChildNode(group);
        var camera = CreateCamera();
        // The label is listed first deliberately; normal-scene ordering must not cover an enabled overlay.
        var disabled = frame.Render(camera, [top, occluder]);
        Assert.Equal(0, CountRed(disabled.Pixels));
        top.EnableTopMost = true;
        var enabled = frame.Render(camera, [top, occluder]);
        Assert.True(CountRed(enabled.Pixels) > 0, "Actual red glyphs must survive the nearer green mesh");
        var opaque = Field<FastList<SceneNode>>(frame.Renderer, "opaqueNodes");
        var overlays = Field<FastList<SceneNode>>(frame.Renderer, "screenSpacedNodes");
        Assert.DoesNotContain(opaque, node => ReferenceEquals(node, text));
        Assert.Collection(overlays,
            node => Assert.Same(top, node), node => Assert.Same(group, node), node => Assert.Same(text, node));

        using var screen = new ScreenSpacedNode { SizeScale = 0.1f, RelativeScreenLocationX = -0.8f };
        var afterScreen = frame.Render(camera, [occluder, screen, top]);
        Assert.Equal(enabled.Pixels, afterScreen.Pixels);
        top.Visible = false;
        var hidden = frame.Render(camera, [occluder, top]);
        Assert.Equal(0, CountRed(hidden.Pixels));
        Assert.Empty(overlays);
    });

    /// <summary>Counts red glyph pixels independently of green mesh and blue clear pixels.</summary>
    /// <param name="pixels">The completed RGBA readback.</param>
    /// <returns>The number of red-dominant pixels.</returns>
    private static int CountRed(byte[] pixels) {
        var count = 0;
        for (var index = 0; index < pixels.Length; index += 4)
            if (pixels[index] > 40 && pixels[index] > pixels[index + 1] * 2 &&
                pixels[index] > pixels[index + 2] * 2) count++;
        return count;
    }

    /// <summary>Preserves local occlusion and transparent material selection, resetting depth at each TopMost boundary.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpTopMostKeepsLocalDepthAndOrderedIndependentGroups() {
        using var frame = new OffscreenFrame();
        VerifyTopMostWarp(frame.Device);
        using var first = new TopMostGroupNode();
        using var second = new TopMostGroupNode();
        using var near = CreateGreenQuad();
        using var far = CreateGreenQuad();
        far.ModelMatrix = SilkMath.Translation(new Vector3(0, 0, -0.5f));
        far.IsTransparent = true;
        ((MeshGeometry3D) far.Geometry!).Colors = new Color4Collection([
            new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1),
            new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1)
        ]);
        first.AddChildNode(near);
        first.AddChildNode(far);
        var camera = CreateCamera();
        var local = frame.Render(camera, [first]);
        AssertPixel(local.Pixels, frame.Target.Width, 16, 12, 0, 255, 0);
        first.RemoveChildNode(far);
        second.AddChildNode(far);
        var later = frame.Render(camera, [first, second]);
        AssertPixel(later.Pixels, frame.Target.Width, 16, 12, 255, 0, 0);
        var reverse = frame.Render(camera, [second, first]);
        AssertPixel(reverse.Pixels, frame.Target.Width, 16, 12, 0, 255, 0);
        first.AddChildNode(second);
        var nested = frame.Render(camera, [first]);
        AssertPixel(nested.Pixels, frame.Target.Width, 16, 12, 255, 0, 0);
    }

    /// <summary>Rejects every adapter except verified Microsoft WARP before recording any submission.</summary>
    /// <param name="device">The device explicitly created with the WARP driver, without fallback.</param>
    private static unsafe void VerifyTopMostWarp(SilkD3D12Device device) {
        Silk.NET.Core.Native.ComPtr<IDXGIFactory4> factory = default;
        Silk.NET.Core.Native.ComPtr<IDXGIAdapter1> adapter = default;
        try {
            Silk.NET.Core.Native.SilkMarshal.ThrowHResult(
                SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(0, out factory));
            var luid = device.NativeDevice.GetAdapterLuid();
            Silk.NET.Core.Native.SilkMarshal.ThrowHResult(factory.EnumAdapterByLuid(luid, out adapter));
            AdapterDesc1 description;
            Silk.NET.Core.Native.SilkMarshal.ThrowHResult(adapter.GetDesc1(&description));
            Assert.NotEqual(0u, description.Flags & (uint) AdapterFlag.Software);
            Assert.Equal(0x1414u, description.VendorId);
            Assert.Equal(0x8cu, description.DeviceId);
        } finally {
            adapter.Dispose();
            factory.Dispose();
        }
    }
}
