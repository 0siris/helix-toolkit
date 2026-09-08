using System.Windows;
using System.Windows.Input;
using ValidSphere;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Extensions;

namespace CrossSectionDemo;

/// <summary>
/// Viewport which does hit test on mouse over
/// </summary>
public class CustomViewport3DX : Viewport3DX {
    /// <inheritdoc />
    protected override void OnPreviewMouseMove(MouseEventArgs e) {
        e.AsGuardNotNull();
        base.OnPreviewMouseMove(e);

        var hits = this.FindHits(e.GetPosition(this));
        ModelAtCursor = hits.Count > 0
            ? hits[0].ModelHit
            : null;
    }

    public static readonly DependencyProperty ModelAtCursorProperty = DependencyProperty.Register(
        nameof(ModelAtCursor),
        typeof(object),
        typeof(CustomViewport3DX),
        new PropertyMetadata(default(object)));

    /// <summary>
    /// The model that the mouse hovers over
    /// </summary>
    public object? ModelAtCursor {
        get => GetValue(ModelAtCursorProperty);
        set => SetValue(ModelAtCursorProperty, value);
    }
}
