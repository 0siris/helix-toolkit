using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     The VisualWrapper simply integrates a raw Visual child into a tree
///     of FrameworkElements.
///     https://blogs.msdn.microsoft.com/dwayneneed/2007/04/26/multithreaded-ui-hostvisual/
/// </summary>
[ContentProperty("Child")]
public class VisualWrapper<T> : FrameworkElement where T : Visual {
    private T? _child;

    public T? Child {
        get => _child;

        set {
            if (_child is { } oldChild) RemoveVisualChild(oldChild);

            _child = value;

            if (_child is { } newChild) AddVisualChild(newChild);
        }
    }

    protected override int VisualChildrenCount => _child is null ? 0 : 1;

    protected override Visual GetVisualChild(int index) {
        if (_child is { } child && index == 0) return child;

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}

/// <summary>
///     The VisualWrapper simply integrates a raw Visual child into a tree
///     of FrameworkElements.
/// </summary>
public class VisualWrapper : VisualWrapper<Visual> { }
