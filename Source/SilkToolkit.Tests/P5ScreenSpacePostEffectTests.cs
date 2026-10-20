using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks screen-space selection effects through both productive viewport templates and actual readback.</summary>
[Collection(WpfCollection.Name)]
public sealed class P5ScreenSpacePostEffectTests {
    /// <summary>Applies the registered outline to a tagged corner mesh using its local camera and viewport.</summary>
    /// <param name="hwnd">Whether the shipped template explicitly selects HWND presentation.</param>
    /// <returns>The dispatcher-owned hardware capture verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task TaggedScreenSpaceMeshChangesBackgroundPixels(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        driver.Viewport.Width = 240;
        driver.Viewport.Height = 180;
        using var mesh = P4PresentationDriver.CreateMesh();
        mesh.Transform = new ScaleTransform3D(8, 8, 8);
        using var screen = new ScreenSpacedGroup3D {
            RelativeScreenLocationX = 0.5, RelativeScreenLocationY = 0, EnableMover = false
        };
        screen.Children.Add(mesh);
        using var outline = new PostEffectMeshOutlineBlur {
            EffectName = "screen-outline", Color = Colors.Red, NumberOfBlurPass = 1
        };
        driver.Viewport.Items.Add(screen);
        driver.Viewport.Items.Add(outline);
        driver.Root.UpdateLayout();
        await driver.WarmAsync();

        var withoutEffect = await driver.CaptureAsync();
        Assert.Equal(240u, withoutEffect.Width);
        Assert.Equal(180u, withoutEffect.Height);
        P4PresentationDriver.AssertPixel(withoutEffect, 180, 90, 0, 255, 0, 255);
        P4PresentationDriver.AssertPixel(withoutEffect, 60, 90, 255, 0, 0, 255);

        mesh.PostEffects = "screen-outline";
        await driver.DrainAsync();
        var withEffect = await driver.CaptureAsync();
        Assert.Equal(withoutEffect.Width, withEffect.Width);
        Assert.Equal(withoutEffect.Height, withEffect.Height);
        P4PresentationDriver.AssertPixel(withEffect, 180, 90, 0, 255, 0, 255);
        var outlinedBackgroundPixels = 0;
        for (var y = 0; y < 180; y++)
            for (var x = 0; x < 240; x++) {
                var offset = (y * 240 + x) * 4;
                Assert.Equal((byte) 0, withoutEffect.Pixels[offset + 2]);
                Assert.Equal((byte) 255, withEffect.Pixels[offset + 3]);
                if (x < 140 || x >= 220 || y < 50 || y >= 130) {
                    for (var channel = 0; channel < 4; channel++)
                        Assert.Equal(withoutEffect.Pixels[offset + channel], withEffect.Pixels[offset + channel]);
                }

                if (withoutEffect.Pixels[offset] < 253 || withoutEffect.Pixels[offset + 1] > 2 ||
                    withEffect.Pixels[offset + 2] < 16)
                    continue;

                // The blurred red mask has R = A. One/InverseSourceAlpha over blue gives R + B = 1.
                Assert.InRange(withEffect.Pixels[offset] + withEffect.Pixels[offset + 2], 254, 256);
                Assert.InRange((int) withEffect.Pixels[offset + 1], 0, 2);
                outlinedBackgroundPixels++;
            }

        Assert.True(outlinedBackgroundPixels >= 16, $"Only {outlinedBackgroundPixels} background pixels gained an outline.");
        mesh.PostEffects = string.Empty;
        await driver.DrainAsync();
        var restored = await driver.CaptureAsync();
        Assert.Equal(withoutEffect.Pixels, restored.Pixels);
    });
}
