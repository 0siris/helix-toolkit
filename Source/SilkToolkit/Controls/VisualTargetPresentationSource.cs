using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     The VisualTargetPresentationSource represents the root
///     of a visual subtree owned by a different thread that the
///     visual tree in which is is displayed.
///     https://blogs.msdn.microsoft.com/dwayneneed/2007/04/26/multithreaded-ui-hostvisual/
/// </summary>
/// <remarks>
///     A HostVisual belongs to the same UI thread that owns the
///     visual tree in which it resides.
///     A HostVisual can reference a VisualTarget owned by another
///     thread.
///     A VisualTarget has a root visual.
///     VisualTargetPresentationSource wraps the VisualTarget and
///     enables basic functionality like Loaded, which depends on
///     a PresentationSource being available.
/// </remarks>
public class VisualTargetPresentationSource : PresentationSource, IDisposable {
    private readonly VisualTarget visualTarget;
    private object dataContext;
    private string propertyName;

    public VisualTargetPresentationSource(HostVisual hostVisual) {
        visualTarget = new VisualTarget(hostVisual);
    }

    public override Visual RootVisual {
        get => visualTarget.RootVisual;

        set {
            var oldRoot = visualTarget.RootVisual;


            // Set the root visual of the VisualTarget.  This visual will
            // now be used to visually compose the scene.
            visualTarget.RootVisual = value;

            // Hook the SizeChanged event on framework elements for all
            // future changed to the layout size of our root, and manually
            // trigger a size change.
            var rootFe = value as FrameworkElement;
            if (rootFe != null) {
                rootFe.SizeChanged += root_SizeChanged;
                rootFe.DataContext = dataContext;

                // HACK!
                if (propertyName != null) {
                    var myBinding = new Binding(propertyName) {
                        Source = dataContext
                    };
                    rootFe.SetBinding(TextBlock.TextProperty, myBinding);
                }
            }

            // Tell the PresentationSource that the root visual has
            // changed.  This kicks off a bunch of stuff like the
            // Loaded event.
            RootChanged(oldRoot, value);

            // Kickoff layout...
            var rootElement = value as UIElement;
            if (rootElement != null) {
                rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rootElement.Arrange(new Rect(rootElement.DesiredSize));
            }
        }
    }

    public object DataContext {
        get => dataContext;
        set {
            dataContext = value;
            var rootElement = visualTarget.RootVisual as FrameworkElement;
            rootElement?.DataContext = dataContext;
        }
    }

    // HACK!
    public string PropertyName {
        get => propertyName;
        set {
            propertyName = value;

            var rootElement = visualTarget.RootVisual as TextBlock;
            if (rootElement != null) {
                if (!rootElement.CheckAccess()) throw new InvalidOperationException("What?");

                var myBinding = new Binding(propertyName) {
                    Source = dataContext
                };
                rootElement.SetBinding(TextBlock.TextProperty, myBinding);
            }
        }
    }

    public override bool IsDisposed =>
        // We don't support disposing this object.
        false;

    public event SizeChangedEventHandler SizeChanged;

    protected override CompositionTarget GetCompositionTargetCore() => visualTarget;

    private void root_SizeChanged(object sender, SizeChangedEventArgs e) {
        var handler = SizeChanged;
        if (handler != null) handler(this, e);
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    [SuppressMessage("Microsoft.Usage", "CA2213", Justification = "False positive.")]
    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) visualTarget?.Dispose();
            // TODO: dispose managed state (managed objects).
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~VisualTargetPresentationSource() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
