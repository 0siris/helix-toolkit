using System;
using System.Collections.Generic;
using System.Windows;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Locks presentation selection and connects image properties to bounded render demand.</summary>
public partial class Viewport3DX {
    /// <summary>Retains the first successfully initialized backend across unload and template replacement.</summary>
    private bool? selectedSwapChainRendering;

    /// <summary>Retains the bounded set of image properties; lookups never allocate on status updates.</summary>
    private static readonly HashSet<DependencyProperty> imageProperties;

    /// <summary>Identifies the initial backend choice; false selects WPF composition.</summary>
    public static readonly DependencyProperty EnableSwapChainRenderingProperty = DependencyProperty.Register(
        nameof(EnableSwapChainRendering), typeof(bool), typeof(Viewport3DX),
        new PropertyMetadata(false, PresentationBackendChanged, CoercePresentationBackend)
    );

    /// <summary>Identifies whether physical render dimensions follow monitor DPI, independently of DIP input.</summary>
    public static readonly DependencyProperty EnableDpiScaleProperty = DependencyProperty.Register(
        nameof(EnableDpiScale), typeof(bool), typeof(Viewport3DX),
        new PropertyMetadata(true, EnableDpiScaleChanged)
    );

    /// <summary>Gets or sets the backend before native initialization; true selects continuous HWND presentation.</summary>
    /// <exception cref="InvalidOperationException">A different backend has already initialized successfully.</exception>
    public bool EnableSwapChainRendering {
        get => (bool) GetValue(EnableSwapChainRenderingProperty);
        set {
            if (selectedSwapChainRendering is { } selected && selected != value)
                throw new InvalidOperationException("The presentation backend cannot change after successful native initialization.");
            SetValue(EnableSwapChainRenderingProperty, value);
        }
    }

    /// <summary>Gets or sets whether rendering uses physical monitor resolution while input remains in DIPs.</summary>
    public bool EnableDpiScale {
        get => (bool) GetValue(EnableDpiScaleProperty);
        set => SetValue(EnableDpiScaleProperty, value);
    }

    /// <summary>Preserves the initialized backend for all dependency-property and binding value sources.</summary>
    /// <param name="owner">The viewport receiving the request.</param>
    /// <param name="value">The requested backend.</param>
    /// <returns>The unchanged compatible backend choice.</returns>
    private static object CoercePresentationBackend(DependencyObject owner, object value) {
        var viewport = (Viewport3DX) owner;
        ObjectDisposedException.ThrowIf(viewport.IsDisposed && (bool) value != viewport.EnableSwapChainRendering, viewport);
        if (viewport.wpfPresentation is { Generation: > 0 } || viewport.d3d12Surface?.IsInitialized == true)
            viewport.MarkPresentationInitialized();
        if (viewport.selectedSwapChainRendering is { } selected && selected != (bool) value)
            return selected;
        if ((bool) value != viewport.EnableSwapChainRendering)
            viewport.RetirePresentation();
        return value;
    }

    /// <summary>Remembers native initialization permanently without treating WPF template initialization as success.</summary>
    internal void MarkPresentationInitialized() => selectedSwapChainRendering ??= EnableSwapChainRendering;

    /// <summary>Recreates an existing template's presentation after a permitted pre-initialization backend change.</summary>
    /// <param name="owner">The viewport whose backend changed.</param>
    /// <param name="args">The committed backend selection.</param>
    private static void PresentationBackendChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) {
        var viewport = (Viewport3DX) owner;
        if (viewport.hostPresenter is not null && viewport.Template is not null) viewport.OnApplyTemplate();
    }

    /// <summary>Rejects reentrant template changes before WPF can replace the visual tree of an active recorder.</summary>
    /// <param name="owner">The viewport receiving the template.</param>
    /// <param name="value">The requested template.</param>
    /// <returns>The template when no scene recording is active.</returns>
    private static object CoercePresentationTemplate(DependencyObject owner, object value) {
        ((Viewport3DX) owner).VerifyPresentationReplacement();
        return value;
    }

    /// <summary>Prevents replacement from destroying commands or root snapshots borrowed by a scene callback.</summary>
    /// <exception cref="InvalidOperationException">A native or composited scene is being recorded.</exception>
    private void VerifyPresentationReplacement() {
        if (isD3D12FrameUpdating || wpfPresentation?.IsRecording == true)
            throw new InvalidOperationException("A viewport template cannot change while a scene is being recorded.");
    }

    /// <summary>Retires native transport before releasing references, retaining the owner when a drain fails.</summary>
    /// <remarks>Application scene nodes and externally supplied managers are never disposed here.</remarks>
    private void RetirePresentation() {
        VerifyPresentationReplacement();
        if (wpfPresentation is { Generation: > 0 } || d3d12Surface?.IsInitialized == true)
            MarkPresentationInitialized();
        if (wpfPresentation is { } composited) {
            composited.PrepareForDisposal();
            composited.Canvas.Dispose();
            composited.Dispose();
            composited.Canvas.PresentationChanged -= CanvasPresentationChanged;
            wpfPresentation = null;
        }

        if (d3d12Surface is { } surface) {
            surface.Dispose();
            surface.WindowHost.PointerInput -= D3D12PointerInput;
            surface.DeferredRenderException -= HandleRenderException;
            d3d12Surface = null;
        }

        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        if (hostPresenter is { } presenter) presenter.Content = null;
    }

    /// <summary>Applies render resolution policy to the selected transport without changing native pointer scaling.</summary>
    /// <param name="owner">The viewport receiving the DPI option.</param>
    /// <param name="args">The effective option change.</param>
    private static void EnableDpiScaleChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) {
        var viewport = (Viewport3DX) owner;
        if (viewport.wpfPresentation is { } composited) composited.Canvas.EnableDpiScale = (bool) args.NewValue;
        if (viewport.d3d12Surface is { } surface) surface.EnableDpiScale = (bool) args.NewValue;
        viewport.RefreshRenderDpi();
        viewport.InvalidateRender();
    }

    /// <summary>Publishes actual render DPI independently of the original routed WPF input coordinates.</summary>
    private void RefreshRenderDpi() {
        var scale = wpfPresentation?.Canvas.DpiScale ?? d3d12Surface?.DpiScale ?? DpiScale;
        if (DpiScale != scale) SetCurrentValue(DpiScaleProperty, scale);
    }

    /// <summary>Forwards the public scale to the active transport without changing DIP input.</summary>
    /// <param name="owner">The viewport receiving the public scale.</param>
    /// <param name="args">The effective requested scale.</param>
    private static void RenderDpiChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) {
        var viewport = (Viewport3DX) owner;
        if (viewport.wpfPresentation is { } composited) composited.Canvas.DpiScale = (double) args.NewValue;
        if (viewport.d3d12Surface is { } surface) surface.DpiScale = (double) args.NewValue;
        viewport.InvalidateRender();
    }

    /// <summary>Retains only finite positive render scales.</summary>
    /// <param name="value">The requested public scale.</param>
    /// <returns>Whether the scale is valid for physical dimensions.</returns>
    private static bool IsValidRenderDpi(object value) => (double) value > 0 && double.IsFinite((double) value);

    /// <summary>Tracks physical canvas changes without requesting scene frames for publication-only notifications.</summary>
    /// <param name="sender">The actual presentation canvas.</param>
    /// <param name="args">The coalesced lifecycle notification.</param>
    private void CanvasPresentationChanged(object? sender, EventArgs args) => RefreshRenderDpi();

    /// <summary>Refreshes physical resolution on monitor changes without scaling WPF input positions.</summary>
    /// <param name="oldDpi">The preceding monitor scale.</param>
    /// <param name="newDpi">The new monitor scale.</param>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
        base.OnDpiChanged(oldDpi, newDpi);
        SetCurrentValue(DpiScaleProperty, newDpi.DpiScaleX);
    }

    /// <summary>Registers only image-producing state; diagnostics and cursor/status updates do not wake the scene.</summary>
    /// <returns>The image properties whose effective value changes request a frame.</returns>
    private static HashSet<DependencyProperty> CreateImagePropertySet() => [
        BackgroundColorProperty, RenderTechniqueProperty, CameraProperty, OrthographicProperty,
        IsShadowMappingEnabledProperty, MsaaProperty, FxaaLevelProperty, EnableRenderFrustumProperty,
        EnableDeferredRenderingProperty, EnableSharedModelModeProperty, SharedModelContainerProperty,
        Content2DProperty, EnableD2DRenderingProperty, EnableAutoOctreeUpdateProperty, EnableRenderOrderProperty,
        OitRenderModeProperty, OitWeightPowerProperty, OitWeightDepthSlopeProperty, OitWeightModeProperty,
        OitDepthPeelingIterationProperty, EnableSsaoProperty, SsaoSamplingRadiusProperty, SsaoIntensityProperty,
        SsaoQualityProperty, ModelUpDirectionProperty, ShowViewCubeProperty, ViewCubeTextureProperty,
        ViewCubeHorizontalPositionProperty, ViewCubeVerticalPositionProperty, ViewCubeSizeProperty,
        IsViewCubeEdgeClicksEnabledProperty, IsViewCubeMoverEnabledProperty, ShowCoordinateSystemProperty,
        CoordinateSystemHorizontalPositionProperty, CoordinateSystemVerticalPositionProperty,
        CoordinateSystemSizeProperty, CoordinateSystemLabelForegroundProperty, CoordinateSystemLabelXProperty,
        CoordinateSystemLabelYProperty, CoordinateSystemLabelZProperty, CoordinateSystemAxisXColorProperty,
        CoordinateSystemAxisYColorProperty, CoordinateSystemAxisZColorProperty, IsCoordinateSystemMoverEnabledProperty,
        ShowFrameRateProperty, ShowFrameDetailsProperty, ShowTriangleCountInfoProperty, ShowCameraInfoProperty,
        ShowCameraTargetProperty, InfoBackgroundProperty, InfoForegroundProperty, TitleProperty,
        TitleSizeProperty, TitleBackgroundProperty, TitleFontFamilyProperty, SubTitleProperty,
        SubTitleSizeProperty, TextBrushProperty
    ];

    /// <summary>Wakes only image-producing state; original aliases flow through their synchronized current property.</summary>
    /// <param name="args">The effective image-property transition.</param>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs args) {
        base.OnPropertyChanged(args);
        if (imageProperties.Contains(args.Property)) InvalidateRender();
    }
}