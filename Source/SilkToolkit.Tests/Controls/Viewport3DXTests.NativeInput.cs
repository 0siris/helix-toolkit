using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SilkToolkit.Tests;
using Xunit;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Verifies button observation fallbacks without a native surface, physical input or device replacement.</summary>
[Collection(WpfCollection.Name)]
public sealed class Viewport3DXTests {
    /// <summary>Retains the ordinary associated-device and primary-device values for every supported button.</summary>
    /// <param name="button">The supported logical button.</param>
    /// <exception cref="ArgumentOutOfRangeException">The supplied test button is unsupported.</exception>
    /// <returns>The completion of the owning dispatcher assertions.</returns>
    [Theory]
    [InlineData(MouseButton.Left)]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    [InlineData(MouseButton.XButton1)]
    [InlineData(MouseButton.XButton2)]
    [Trait("Category", "Wpf")]
    public Task GetMouseInputButtonStateFallbackTest(MouseButton button) => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();
        var args = new MouseEventArgs(Mouse.PrimaryDevice, 0);
        var expected = button switch {
            MouseButton.Left => args.LeftButton,
            MouseButton.Right => args.RightButton,
            MouseButton.Middle => args.MiddleButton,
            MouseButton.XButton1 => args.XButton1,
            MouseButton.XButton2 => args.XButton2,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported test button")
        };

        var fromEvent = viewport.GetMouseInputButtonState(button, args);
        var fromPrimary = viewport.GetMouseInputButtonState(button);

        Assert.Null(args.MouseDevice.ActiveSource);
        Assert.Equal(expected, fromEvent);
        Assert.Equal(expected, fromPrimary);
    });

    /// <summary>Retains the WPF fallback after real visual detachment without creating or showing a native host.</summary>
    /// <returns>The completion of the owning dispatcher assertions.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task GetMouseInputButtonStateDetachedTest() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();
        var parent = new Grid();
        parent.Children.Add(viewport);
        var attachedParent = VisualTreeHelper.GetParent(viewport);
        parent.Children.Remove(viewport);
        var detachedParent = VisualTreeHelper.GetParent(viewport);
        var args = new MouseEventArgs(Mouse.PrimaryDevice, 0);
        var expected = args.RightButton;

        var actual = viewport.GetMouseInputButtonState(MouseButton.Right, args);

        Assert.Same(parent, attachedParent);
        Assert.Null(detachedParent);
        Assert.False(viewport.IsLoaded);
        Assert.Null(args.MouseDevice.ActiveSource);
        Assert.Equal(expected, actual);
    });

    /// <summary>Rejects unsupported buttons even when the viewport has no native input authority.</summary>
    /// <param name="button">The unsupported logical button value.</param>
    /// <returns>The completion of the owning dispatcher assertions.</returns>
    [Theory]
    [InlineData((MouseButton) (-1))]
    [InlineData((MouseButton) 5)]
    [Trait("Category", "Wpf")]
    public Task GetMouseInputButtonStateInvalidButtonTest(MouseButton button) => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => viewport.GetMouseInputButtonState(button));

        Assert.Equal("button", error.ParamName);
        Assert.Equal(button, error.ActualValue);
    });
}
