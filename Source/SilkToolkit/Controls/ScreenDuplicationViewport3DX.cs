using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Render.RenderBuffers;
using HelixToolkit.SharpDX.Core.Render.RenderHost;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Color = System.Windows.Media.Color;

namespace HelixToolkit.Wpf.SharpDX.Controls;

[DefaultProperty("Children")]
[ContentProperty("Items")]
[TemplatePart(Name = "PART_Canvas", Type = typeof(ContentPresenter))]
public class ScreenDuplicationViewport3DX : ItemsControl, IViewport3DX {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    /// <summary>
    ///     The EffectsManager property.
    /// </summary>
    public static readonly DependencyProperty EffectsManagerProperty = DependencyProperty.Register("EffectsManager",
        typeof(IEffectsManager),
        typeof(ScreenDuplicationViewport3DX),
        new PropertyMetadata(null,
                             (s, _) => ((ScreenDuplicationViewport3DX)s).EffectsManagerPropertyChanged()));

    /// <summary>
    ///     The Render Technique property
    /// </summary>
    public static readonly DependencyProperty RenderTechniqueProperty = DependencyProperty.Register("RenderTechnique",
        typeof(IRenderTechnique),
        typeof(ScreenDuplicationViewport3DX),
        new PropertyMetadata(null,
                             (s, _) => ((ScreenDuplicationViewport3DX)s).RenderTechniquePropertyChanged()));

    /// <summary>
    ///     The render exception property.
    /// </summary>
    public static DependencyProperty RenderExceptionProperty = DependencyProperty.Register(
        "RenderException",
        typeof(Exception),
        typeof(ScreenDuplicationViewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The message text property.
    /// </summary>
    public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register(
        "MessageText",
        typeof(string),
        typeof(ScreenDuplicationViewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     Background Color property.this.RenderHost
    /// </summary>
    public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register("BackgroundColor",
        typeof(Color),
        typeof(ScreenDuplicationViewport3DX),
        new PropertyMetadata(Colors.White,
                             (s, e) => {
                                 ((ScreenDuplicationViewport3DX)s).RenderHost?.ClearColor =
                                         ((Color)e.NewValue).ToColor4();
                             }));

    private bool disposedValue;

    private bool isAttached;

    static ScreenDuplicationViewport3DX() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ScreenDuplicationViewport3DX),
                                                 new FrameworkPropertyMetadata(typeof(ScreenDuplicationViewport3DX)));
    }

    /// <summary>
    ///     Gets or sets value for the shading model shading is used
    /// </summary>
    /// <value>
    ///     <c>true</c> if deferred shading is enabled; otherwise, <c>false</c>.
    /// </value>
    public IRenderTechnique? RenderTechnique {
        get => (IRenderTechnique?)GetValue(RenderTechniqueProperty);
        set => SetValue(RenderTechniqueProperty, value);
    }

    /// <summary>
    ///     Gets or sets the <see cref="System.Exception" /> that occured at rendering subsystem.
    /// </summary>
    public Exception? RenderException {
        get => (Exception?)GetValue(RenderExceptionProperty);
        set => SetValue(RenderExceptionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the message text.
    /// </summary>
    /// <value>
    ///     The message text.
    /// </value>
    public string? MessageText {
        get => (string?)GetValue(MessageTextProperty);

        set => SetValue(MessageTextProperty, value);
    }

    /// <summary>
    ///     Background Color
    /// </summary>
    public Color BackgroundColor {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public Matrix WorldMatrix { get; } = Matrix.Identity;

    public static bool IsInDesignMode {
        get {
            var prop = DesignerProperties.IsInDesignModeProperty;
            return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata
                                                      .DefaultValue;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="IEffectsManager" />.
    /// </summary>
    public IEffectsManager? EffectsManager {
        get => (IEffectsManager?)GetValue(EffectsManagerProperty);
        set => SetValue(EffectsManagerProperty, value);
    }


    public CameraCore CameraCore { get; } = new PerspectiveCameraCore();

    public IEnumerable<SceneNode> Renderables {
        get {
            if (RenderHost != null)
                foreach (Model.Elements3D.AbstractElements3D.Element3D item in Items)
                    yield return item.SceneNode;
        }
    }

    public IEnumerable<SceneNode2D> D2DRenderables => [];

    public IRenderHost? RenderHost { get; private set; }

    public bool IsShadowMappingEnabled => false;

    public Rectangle ViewportRectangle => new();

    public void Attach(IRenderHost host) {
        if (!isAttached) {
            foreach (var e in Renderables) e.Attach(EffectsManager);
            isAttached = true;
        }
    }

    public void Detach() {
        if (isAttached) {
            isAttached = false;
            foreach (var e in Renderables) e.Detach();
        }
    }

    public void InvalidateRender() {
        RenderHost?.InvalidateRender();
    }

    public void InvalidateSceneGraph() {
        RenderHost?.InvalidateSceneGraph();
    }

    public void Update(TimeSpan timeStamp) { }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Fired whenever an exception occurred at rendering subsystem.
    /// </summary>
    public event EventHandler<RelayExceptionEventArgs> RenderExceptionOccurred = delegate { };

    /// <summary>
    ///     Handles the change of the effects manager.
    /// </summary>
    private void EffectsManagerPropertyChanged() {
        RenderHost?.EffectsManager = EffectsManager;
    }

    /// <summary>
    ///     Handles the change of the render technique
    /// </summary>
    private void RenderTechniquePropertyChanged() {
        if (RenderHost != null) {
            // remove the scene
            RenderHost.Viewport = null;

            // if new rendertechnique set, attach the scene
            if (RenderTechnique != null) RenderHost.Viewport = this;
        }
    }

    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        if (IsInDesignMode) return;
        RenderHost?.ExceptionOccurred -= HandleRenderException;
        if (GetTemplateChild("PART_Canvas") is not ContentPresenter hostPresenter) return;
        hostPresenter.Content = new DPFSurfaceSwapChain(surface => new ScreenCloneRenderHost(surface));
        if (hostPresenter.Content is not IRenderCanvas { RenderHost: { } renderHost }) return;
        RenderHost = renderHost;
        renderHost.ExceptionOccurred += HandleRenderException;
        renderHost.Viewport = this;
        renderHost.EffectsManager = EffectsManager;
        renderHost.ClearColor = BackgroundColor.ToColor4();
    }

    /// <summary>
    ///     Handles a rendering exception.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void HandleRenderException(object? sender, RelayExceptionEventArgs e) {
        var bindingExpression = GetBindingExpression(RenderExceptionProperty);
        if (bindingExpression != null) {
            // If RenderExceptionProperty is bound, we assume the exception will be handled.
            RenderException = e.Exception;
            e.Handled = true;
        }

        // Fire RenderExceptionOccurred event
        RenderExceptionOccurred(sender, e);

        // If the Exception is still unhandled...
        if (!e.Handled) {
            // ... prevent a MessageBox.Show().
            MessageText = e.Exception.ToString();
            e.Handled = true;
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                EffectsManager = null;
                RenderHost?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    private class ScreenCloneRenderHost : SwapChainRenderHost {
        public ScreenCloneRenderHost(IntPtr surface) : base(surface) {
            RenderConfiguration = new DX11RenderHostConfiguration {
                ClearEachFrame = false,
                RenderD2D = false,
                RenderLights = false,
                UpdatePerFrameData = false
            };
        }

        protected override DX11RenderBufferProxyBase CreateRenderBuffer() {
            Logger.Info("DX11SwapChainRenderBufferProxy");
            return new DX11SwapChainRenderBufferProxy(
                Surface,
                EffectsManager.AssertNotNull("Effects manager is not initialized."),
                false);
        }
    }
}
