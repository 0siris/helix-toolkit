// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Viewport3DX.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a Viewport control.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Controls.MouseHandlers;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using HitTestResult = HelixToolkit.SharpDX.Core.Utilities.HitTestResult;
using IViewportExtensions = HelixToolkit.SharpDX.Core.Extensions.IViewportExtensions;
using MouseButtons = System.Windows.Forms.MouseButtons;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.Camera.OrthographicCamera;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.Camera.PerspectiveCamera;
using Visibility = System.Windows.Visibility;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Provides a Viewport control.
/// </summary>
[ContentProperty("Items")]
[TemplatePart(Name = "PART_Canvas", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_AdornerLayer", Type = typeof(AdornerDecorator))]
[TemplatePart(Name = "PART_CoordinateView", Type = typeof(Viewport3D))]
[TemplatePart(Name = "PART_ViewCube", Type = typeof(Viewport3D))]
[TemplatePart(Name = "PART_FrameStatisticView", Type = typeof(Viewport3D))]
[TemplatePart(Name = "PART_TitleView", Type = typeof(StackPanel2D))]
[TemplatePart(Name = "PART_Items", Type = typeof(ItemsControl))]
[Localizability(LocalizationCategory.NeverLocalize)]
public partial class Viewport3DX : Control, IViewport3DX, IDisposable {
    /// <summary>
    ///     The adorner layer part name.
    /// </summary>
    private const string PartAdornerLayer = "PART_AdornerLayer";

    /// <summary>
    ///     The coordinate view part name.
    /// </summary>
    private const string PartCoordinateView = "PART_CoordinateView";

    /// <summary>
    ///     The view cube part name.
    /// </summary>
    private const string PartViewCube = "PART_ViewCube";

    /// <summary>
    ///     The frame statistic view part name
    /// </summary>
    private const string PartFrameStatisticView = "PART_FrameStatisticView";

    /// <summary>
    ///     The part title view
    /// </summary>
    private const string PartTitleView = "PART_TitleView";

    /// <summary>
    ///     The part items used to inherit datacontext for children
    /// </summary>
    private const string PartItems = "PART_Items";

    /// <summary>
    ///     The camera controller.
    /// </summary>
    private readonly CameraController cameraController;

    /// <summary>
    ///     The orthographic camera.
    /// </summary>
    private readonly Camera.Camera orthographicCamera;

    /// <summary>
    ///     The perspective camera.
    /// </summary>
    private readonly Camera.Camera perspectiveCamera;

    /// <summary>
    ///     Coalesces WPF composition callbacks for the Direct3D 12 presentation path.
    /// </summary>
    private readonly CompositionTargetEx d3d12CompositionTarget = new();

    /// <summary>
    ///     The coordinate view.
    /// </summary>
    private ScreenSpacedElement3D? coordinateView;

    /// <summary>
    ///     The nearest valid result during a hit test.
    /// </summary>
    private HitTestResult? currentHit;

    /// <summary>
    ///     The current two-dimensional model under the pointer.
    /// </summary>
    private Model.Elements2D.Abstract.Element2D? mouseOverModel2D;

    /// <summary>
    ///     Current 2D model hit
    /// </summary>
    private HitTest2DResult? currentHit2D;

    private bool enableMouseButtonHitTest = true;

    private FrameStatisticsModel2D? frameStatisticModel;

    /// <summary>
    ///     The "control has been loaded before" flag.
    /// </summary>
    private bool hasBeenLoadedBefore;

    private List<HitTestResult> hits = [];
    private ContentPresenter? hostPresenter;

    /// <summary>
    ///     The active Direct3D 12 presentation surface when swap-chain rendering is selected.
    /// </summary>
    private D3D12PresentationSurface? d3d12Surface;

    /// <summary>
    ///     The existing camera gesture currently driven by the native Direct3D 12 child window.
    /// </summary>
    private MouseGestureHandler? d3d12PointerGesture;

    /// <summary>
    ///     The first native touch contact routed to the existing viewport hit-test contract.
    /// </summary>
    private uint? d3d12TouchPointerId;

    private bool isAttached;

    private Window? parentWindow;

    private ItemsControl? partItemsControl;

    /// <summary>
    ///     The rectangle adorner.
    /// </summary>
    private RectangleAdorner? rectangleAdorner;

    /// <summary>
    ///     The target adorner.
    /// </summary>
    private Adorner? targetAdorner;

    /// <summary>
    ///     The <see cref="TouchDevice" /> of the first TouchDown.
    /// </summary>
    private TouchDevice? touchDownDevice;

    /// <summary>
    ///     The view cube.
    /// </summary>
    private ScreenSpacedElement3D? viewCube;

    /// <summary>
    ///     Initializes static members of the <see cref="Viewport3DX" /> class.
    /// </summary>
    static Viewport3DX() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Viewport3DX),
            new FrameworkPropertyMetadata(typeof(Viewport3DX)));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Viewport3DX" /> class.
    /// </summary>
    public Viewport3DX() {
        Items.CollectionChanged += Items_CollectionChanged;
        perspectiveCamera = new PerspectiveCamera();
        orthographicCamera = new OrthographicCamera();
        perspectiveCamera.Reset();
        orthographicCamera.Reset();
        cameraController = new CameraController(this);

        Camera = Orthographic
            ? orthographicCamera
            : perspectiveCamera;

        InitCameraController();
        CommandBindings.Add(new CommandBinding(ViewportCommands.ZoomExtents, ZoomExtentsHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.SetTarget, cameraController.SetTargetHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.Reset, ResetHandler));

        CommandBindings.Add(new CommandBinding(ViewportCommands.Zoom, cameraController.ZoomHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.Pan, cameraController.PanHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.Rotate, cameraController.RotateHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.ChangeFieldOfView,
            cameraController.ChangeFieldOfViewHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.ZoomRectangle,
            cameraController.ZoomRectangleHandler.Execute));
        CommandBindings.Add(new CommandBinding(ViewportCommands.BottomView, BottomViewHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.TopView, TopViewHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.FrontView, FrontViewHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.BackView, BackViewHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.LeftView, LeftViewHandler));
        CommandBindings.Add(new CommandBinding(ViewportCommands.RightView, RightViewHandler));

        SetDefaultGestures();

        Loaded += ControlLoaded;
        Unloaded += ControlUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    /// <summary>
    ///     Mirrors WPF visibility to the remaining render-host path.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The visibility change.</param>
    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs eventArgs) {
        if (RenderHostInternal is { } host) host.IsRendering = (bool) eventArgs.NewValue;
    }

    public Model.Elements2D.Abstract.Element2D? MouseOverModel2D {
        get => mouseOverModel2D;
        private set {
            if (mouseOverModel2D == value) return;
            mouseOverModel2D?.RaiseEvent(new Mouse2DEventArgs(Model.Elements2D.Abstract.Element2D.MouseLeave2DEvent,
                mouseOverModel2D,
                this));
            mouseOverModel2D = value;
            mouseOverModel2D?.RaiseEvent(new Mouse2DEventArgs(Model.Elements2D.Abstract.Element2D.MouseEnter2DEvent,
                mouseOverModel2D,
                this));
        }
    }

    /// <summary>
    ///     Get current render context
    /// </summary>
    public RenderContext? RenderContext => RenderHostInternal?.RenderContext;

    public ObservableElement3DCollection Items { get; } = [];

    private IEnumerable<SceneNode> OwnedRenderables {
        get {
            foreach (var item in Items) yield return item.SceneNode;
            if (viewCube is { } cube) yield return cube.SceneNode;
            if (coordinateView is { } coordinate) yield return coordinate.SceneNode;
        }
    }

    private Overlay Overlay2D { get; } = new() {
        EnableBitmapCache = true
    };

    public static bool IsInDesignMode {
        get {
            var prop = DesignerProperties.IsInDesignModeProperty;
            return (bool) DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement))
                .Metadata
                .DefaultValue;
        }
    }

    /// <summary>
    ///     <para>Return enumerable of all the rederable elements</para>
    ///     <para>If enabled shared model mode, the returned rederables are current viewport renderable plus shared models</para>
    /// </summary>
    public IEnumerable<SceneNode> Renderables {
        get {
            foreach (var item in Items) yield return item.SceneNode;
            if (RenderHostInternal is {EnableSharingModelMode: true, SharedModelContainer: not null})
                foreach (var item in RenderHostInternal.SharedModelContainer.Renderables)
                    yield return item;

            if (viewCube is { } cube) yield return cube.SceneNode;
            if (coordinateView is { } coordinate) yield return coordinate.SceneNode;
        }
    }

    /// <summary>
    ///     Gets or sets the Direct3D 12 device type used by the opt-in swap-chain path.
    /// </summary>
    internal SilkDriverType D3D12DriverType { get; set; } = SilkDriverType.Hardware;

    /// <summary>
    ///     Gets the active Direct3D 12 surface for lifecycle verification.
    /// </summary>
    internal D3D12PresentationSurface? D3D12Surface => d3d12Surface;

    public IEnumerable<SceneNode2D> D2DRenderables {
        get {
            yield return Overlay2D.SceneNode;
            if (frameStatisticModel is { } statistics) yield return statistics.SceneNode;
        }
    }

    public CameraCore? CameraCore => CameraController.ActualCamera;

    public IRenderHost? RenderHost => RenderHostInternal;

    public Rectangle ViewportRectangle => new(0, 0, (int) ActualWidth, (int) ActualHeight);

    /// <summary>
    ///     Tries to invalidate the current render.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateRender() {
        RenderHostInternal?.InvalidateRender();
    }

    /// <summary>
    ///     Invalidates the scene graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateSceneGraph() {
        RenderHostInternal?.InvalidateSceneGraph();
    }

    /// <summary>
    ///     Attaches the elements to the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    public void Attach(IRenderHost host) {
        if (!isAttached) {
            foreach (var e in OwnedRenderables) {
                e.Attach(EffectsManager);
                e.RenderHost = host;
                e.Invalidated += NodeInvalidated;
            }

            SharedModelContainerInternal?.Attach(host);
            foreach (var e in D2DRenderables) e.Attach(host);
            isAttached = true;
        }
    }

    /// <summary>
    ///     Detaches the elements.
    /// </summary>
    public void Detach() {
        if (isAttached) {
            isAttached = false;
            foreach (var e in OwnedRenderables) {
                e.Invalidated -= NodeInvalidated;
                e.RenderHost = null;
                e.Detach();
            }

            if (RenderHostInternal is { } host) SharedModelContainerInternal?.Detach(host);
            foreach (var e in D2DRenderables) e.Detach();
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="timeStamp"></param>
    public void Update(TimeSpan timeStamp) {
        OnCompositionTargetRendering();
        cameraController.OnCompositionTargetRendering(timeStamp.Ticks);
    }

    /// <summary>
    ///     Fired whenever an exception occurred at rendering subsystem.
    /// </summary>
    public event EventHandler<RelayExceptionEventArgs> RenderExceptionOccurred = delegate { };

    /// <summary>
    ///     Occurs when each render frame finished rendering. Called directly from RenderHost after each frame.
    ///     Use this event carefully. Unsubscrible this event when not used. Otherwise may cause performance issue.
    /// </summary>
    public event EventHandler? OnRendered;

    private void InitCameraController() {
        #region Assign Defaults

        cameraController.ActualCamera = Camera;
        cameraController.DefaultCamera = DefaultCamera;
        cameraController.CameraMode = CameraMode;
        cameraController.CameraRotationMode = CameraRotationMode;
        cameraController.PageUpDownZoomSensitivity = PageUpDownZoomSensitivity;
        cameraController.PanCursor = PanCursor;
        cameraController.RotateAroundMouseDownPoint = RotateAroundMouseDownPoint;
        cameraController.RotateCursor = RotateCursor;
        cameraController.RotationSensitivity = RotationSensitivity;
        cameraController.ShowCameraTarget = ShowCameraTarget;
        cameraController.SpinReleaseTime = SpinReleaseTime;
        cameraController.UpDownPanSensitivity = UpDownPanSensitivity;
        cameraController.UpDownRotationSensitivity = UpDownRotationSensitivity;
        cameraController.ZoomAroundMouseDownPoint = ZoomAroundMouseDownPoint;
        cameraController.ZoomCursor = ZoomCursor;
        cameraController.ZoomRectangleCursor = ZoomRectangleCursor;
        cameraController.ZoomSensitivity = ZoomSensitivity;
        cameraController.ChangeFieldOfViewCursor = ChangeFieldOfViewCursor;
        cameraController.InertiaFactor = CameraInertiaFactor;
        cameraController.InfiniteSpin = InfiniteSpin;
        cameraController.IsChangeFieldOfViewEnabled = IsChangeFieldOfViewEnabled;
        cameraController.IsInertiaEnabled = IsInertiaEnabled;
        cameraController.IsMoveEnabled = IsMoveEnabled;
        cameraController.IsPanEnabled = IsPanEnabled;
        cameraController.IsRotationEnabled = IsRotationEnabled;
        cameraController.EnableTouchRotate = IsTouchRotateEnabled;
        cameraController.EnablePinchZoom = IsPinchZoomEnabled;
        cameraController.PinchZoomAtCenter = PinchZoomAtCenter;
        cameraController.EnableThreeFingerPan = IsThreeFingerPanningEnabled;
        cameraController.LeftRightPanSensitivity = LeftRightPanSensitivity;
        cameraController.LeftRightRotationSensitivity = LeftRightRotationSensitivity;
        cameraController.MaximumFieldOfView = MaximumFieldOfView;
        cameraController.MinimumFieldOfView = MinimumFieldOfView;
        cameraController.ModelUpDirection = ModelUpDirection.ToVector3();
        cameraController.ZoomDistanceLimitFar = ZoomDistanceLimitFar;
        cameraController.ZoomDistanceLimitNear = ZoomDistanceLimitNear;
        cameraController.FixedRotationPoint = FixedRotationPoint.ToVector3();
        cameraController.FixedRotationPointEnabled = FixedRotationPointEnabled;

        #endregion
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (e.OldItems != null)
            foreach (var item in e.OldItems) {
                partItemsControl?.Items.Remove(item);
                if (item is Model.Elements3D.AbstractElements3D.Element3D element) {
                    element.SceneNode.Invalidated -= NodeInvalidated;
                    element.SceneNode.Detach();
                    element.SceneNode.RenderHost = null;
                }
            }

        if (e.NewItems != null)
            foreach (var item in e.NewItems) {
                partItemsControl?.Items.Add(item);
                if (isAttached && item is Model.Elements3D.AbstractElements3D.Element3D element) {
                    element.SceneNode.RenderHost = RenderHostInternal;
                    element.SceneNode.Invalidated += NodeInvalidated;
                    element.SceneNode.Attach(EffectsManager);
                }
            }

        //Fix for ORL-1571: We need to invalidate the graph here. Otherwise, detached elements will
        //continue to be attempted to render. (cached in flatten scene and so on ...)
        InvalidateSceneGraph();
    }

    /// <summary>
    ///     Adds the specified move force.
    /// </summary>
    /// <param name="dx">
    ///     The delta x.
    /// </param>
    /// <param name="dy">
    ///     The delta y.
    /// </param>
    /// <param name="dz">
    ///     The delta z.
    /// </param>
    public void AddMoveForce(double dx, double dy, double dz) {
        cameraController.AddMoveForce(new Vector3((float) dx, (float) dy, (float) dz));
    }

    /// <summary>
    ///     Adds the specified move force.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    public void AddMoveForce(Vector3D delta) {
        cameraController.AddMoveForce(delta.ToVector3());
    }

    /// <summary>
    ///     Adds the specified pan force.
    /// </summary>
    /// <param name="dx">
    ///     The delta x.
    /// </param>
    /// <param name="dy">
    ///     The delta y.
    /// </param>
    public void AddPanForce(double dx, double dy) {
        cameraController.AddPanForce((float) dx, (float) dy);
    }

    /// <summary>
    ///     The add pan force.
    /// </summary>
    /// <param name="pan">
    ///     The pan.
    /// </param>
    public void AddPanForce(Vector3D pan) {
        cameraController.AddPanForce(pan.ToVector3());
    }

    /// <summary>
    ///     The add rotate force.
    /// </summary>
    /// <param name="dx">
    ///     The delta x.
    /// </param>
    /// <param name="dy">
    ///     The delta y.
    /// </param>
    public void AddRotateForce(double dx, double dy) {
        cameraController.AddRotateForce((float) dx, (float) dy);
    }

    /// <summary>
    ///     Adds the zoom force.
    /// </summary>
    /// <param name="dx">
    ///     The delta.
    /// </param>
    public void AddZoomForce(double dx) {
        cameraController.AddZoomForce((float) dx);
    }

    /// <summary>
    ///     Adds the zoom force.
    /// </summary>
    /// <param name="dx">
    ///     The delta.
    /// </param>
    /// <param name="zoomOrigin">
    ///     The zoom origin.
    /// </param>
    public void AddZoomForce(double dx, Point3D zoomOrigin) {
        cameraController.AddZoomForce((float) dx, zoomOrigin.ToVector3());
    }

    /// <summary>
    ///     Changes the direction of the camera.
    /// </summary>
    /// <param name="lookDir">
    ///     The look direction.
    /// </param>
    /// <param name="upDir">
    ///     The up direction.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void ChangeDirection(Vector3D lookDir, Vector3D upDir, double animationTime = 500) {
        cameraController.ChangeDirection(lookDir, upDir, animationTime);
    }

    /// <summary>
    ///     Finds the nearest 3D point in the scene.
    /// </summary>
    /// <param name="pt">
    ///     The screen point (2D).
    /// </param>
    /// <returns>
    ///     A Point3D or null.
    /// </returns>
    public Point3D? FindNearestPoint(Point pt) => ViewportExtensions.FindNearestPoint(this, pt);

    /// <summary>
    ///     Hides the target adorner.
    /// </summary>
    public void HideTargetAdorner() {
        if (hostPresenter is not Visual visual) return;

        var myAdornerLayer = AdornerLayer.GetAdornerLayer(visual);
        if (myAdornerLayer == null) return;
        if (targetAdorner != null) myAdornerLayer.Remove(targetAdorner);

        targetAdorner = null;

        InvalidateRender();
    }

    /// <summary>
    ///     Hides the zoom rectangle.
    /// </summary>
    public void HideZoomRectangle() {
        if (hostPresenter is not Visual visual) return;

        var myAdornerLayer = AdornerLayer.GetAdornerLayer(visual);
        if (myAdornerLayer == null) return;
        if (rectangleAdorner != null) myAdornerLayer.Remove(rectangleAdorner);

        rectangleAdorner = null;

        InvalidateRender();
    }

    /// <summary>
    ///     Change the camera to look at the specified point.
    /// </summary>
    /// <param name="p">
    ///     The point.
    /// </param>
    public void LookAt(Point3D p) {
        cameraController.ActualCamera.LookAt(p, 0);
    }

    /// <summary>
    ///     Change the camera to look at the specified point.
    /// </summary>
    /// <param name="p">
    ///     The point.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void LookAt(Point3D p, double animationTime) {
        cameraController.ActualCamera.LookAt(p, animationTime);
    }

    /// <summary>
    ///     Change the camera to look at the specified point.
    /// </summary>
    /// <param name="p">
    ///     The point.
    /// </param>
    /// <param name="distance">
    ///     The distance.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void LookAt(Point3D p, double distance, double animationTime) {
        cameraController.ActualCamera.LookAt(p, distance, animationTime);
    }

    /// <summary>
    ///     Change the camera to look at the specified point.
    /// </summary>
    /// <param name="p">
    ///     The point.
    /// </param>
    /// <param name="direction">
    ///     The direction.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void LookAt(Point3D p, Vector3D direction, double animationTime) {
        cameraController.ActualCamera.LookAt(p, direction, animationTime);
    }


    /// <summary>
    ///     When overridden in a derived class, is invoked whenever application code or internal processes call
    ///     <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
    /// </summary>
    /// <exception cref="HelixToolkitException">{0} is missing from the template.</exception>
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        if (IsInDesignMode && !EnableDesignModeRendering) return;
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        if (d3d12Surface is { } previousSurface)
            previousSurface.WindowHost.PointerInput -= D3D12PointerInput;
        d3d12Surface?.Dispose();
        d3d12Surface = null;
        Disposer.RemoveAndDispose(ref RenderHostInternal);
        var presenter = GetTemplateChild("PART_Canvas") as ContentPresenter ??
                        throw new HelixToolkitException("{0} is missing from the template.", "PART_Canvas");
        hostPresenter = presenter;

        if (presenter.Content is IRenderCanvas renderCanvas)
            renderCanvas.ExceptionOccurred -= HandleRenderException;
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is { } compositionTarget)
            DpiScale = Math.Max(DpiScale,
                Math.Max(compositionTarget.TransformToDevice.M11,
                    compositionTarget.TransformToDevice.M22));
        if (EnableSwapChainRendering) {
            d3d12Surface = new D3D12PresentationSurface(D3D12DriverType);
            d3d12Surface.WindowHost.PointerInput += D3D12PointerInput;
            presenter.Content = d3d12Surface;
            d3d12CompositionTarget.Rendering += D3D12CompositionTargetRendering;
        } else {
            presenter.Content = new DPFCanvas(EnableDeferredRendering, BelongsToParentWindow) {
                DpiScale = DpiScale
            };
            renderCanvas = (IRenderCanvas) presenter.Content;
            renderCanvas.EnableDpiScale = EnableDpiScale;
            RenderHostInternal = renderCanvas.RenderHost;
            renderCanvas.ExceptionOccurred += HandleRenderException;
        }
        if (RenderHostInternal != null) {
            RenderHostInternal.Rendered += RaiseRenderHostRendered;
            RenderHostInternal.ExceptionOccurred += HandleRenderException;
            RenderHostInternal.ClearColor = BackgroundColor.ToColor4();
            RenderHostInternal.IsShadowMapEnabled = IsShadowMappingEnabled;
            RenderHostInternal.Msaa = Msaa;
            RenderHostInternal.EnableRenderFrustum = EnableRenderFrustum;
            RenderHostInternal.EnableSharingModelMode = EnableSharedModelMode;
            RenderHostInternal.SharedModelContainer = SharedModelContainer;
            RenderHostInternal.Viewport = this;
            RenderHostInternal.EffectsManager = EffectsManager;
            RenderHostInternal.IsRendering = Visibility == Visibility.Visible;
            RenderHostInternal.RenderConfiguration.RenderD2D = EnableD2DRendering;
            RenderHostInternal.RenderConfiguration.AutoUpdateOctree = EnableAutoOctreeUpdate;
            RenderHostInternal.RenderConfiguration.OitRenderType = OitRenderMode;
            RenderHostInternal.RenderConfiguration.OitWeightPower = (float) OitWeightPower;
            RenderHostInternal.RenderConfiguration.OitWeightDepthSlope = (float) OitWeightDepthSlope;
            RenderHostInternal.RenderConfiguration.OitWeightMode = OitWeightMode;
            RenderHostInternal.RenderConfiguration.OitDepthPeelingIteration = OitDepthPeelingIteration;
            RenderHostInternal.RenderConfiguration.FxaaLevel = FxaaLevel;
            RenderHostInternal.RenderConfiguration.EnableRenderOrder = EnableRenderOrder;
            RenderHostInternal.RenderConfiguration.EnableSsao = EnableSsao;
            RenderHostInternal.RenderConfiguration.SsaoRadius = (float) SsaoSamplingRadius;
            RenderHostInternal.RenderConfiguration.SsaoIntensity = (float) SsaoIntensity;
            RenderHostInternal.RenderConfiguration.SsaoQuality = SsaoQuality;
            RenderHostInternal.RenderConfiguration.MinimumUpdateCount = (uint) Math.Max(0, MinimumUpdateCount);
            if (ShowFrameRate)
                RenderHostInternal.ShowRenderDetail |= RenderDetail.Fps;
            else
                RenderHostInternal.ShowRenderDetail &= ~RenderDetail.Fps;
            if (ShowFrameDetails)
                RenderHostInternal.ShowRenderDetail |= RenderDetail.Statistics;
            else
                RenderHostInternal.ShowRenderDetail &= ~RenderDetail.Statistics;
            if (ShowTriangleCountInfo)
                RenderHostInternal.ShowRenderDetail |= RenderDetail.TriangleInfo;
            else
                RenderHostInternal.ShowRenderDetail &= ~RenderDetail.TriangleInfo;
            if (ShowCameraInfo)
                RenderHostInternal.ShowRenderDetail |= RenderDetail.Camera;
            else
                RenderHostInternal.ShowRenderDetail &= ~RenderDetail.Camera;
        }

        coordinateView ??= Template.FindName(PartCoordinateView, this) as ScreenSpacedElement3D;
        if (coordinateView == null)
            throw new HelixToolkitException("{0} is missing from the template.", PartCoordinateView);
        viewCube ??= Template.FindName(PartViewCube, this) as ScreenSpacedElement3D;
        if (viewCube == null) throw new HelixToolkitException("{0} is missing from the template.", PartViewCube);
        frameStatisticModel ??= Template.FindName(PartFrameStatisticView, this) as FrameStatisticsModel2D;
        if (frameStatisticModel == null)
            throw new HelixToolkitException("{0} is missing from the template.", PartFrameStatisticView);
        partItemsControl ??= Template.FindName(PartItems, this) as ItemsControl;
        if (partItemsControl == null) throw new HelixToolkitException("{0} is missing from the template.", PartItems);

        foreach (var item in Items) partItemsControl.Items.Remove(item);
        foreach (var item in Items) partItemsControl.Items.Add(item);
        Overlay2D.Children.Clear();
        RemoveLogicalChild(Overlay2D);
        AddLogicalChild(Overlay2D);
        var titleView = Template.FindName(PartTitleView, this);
        if (titleView is Model.Elements2D.Abstract.Element2D element) Overlay2D.Children.Add(element);
        Overlay2D.Children.Add(viewCube.MoverCanvas);
        Overlay2D.Children.Add(coordinateView.MoverCanvas);
        if (Content2D != null) Overlay2D.Children.Add(Content2D);
    }

    /// <summary>
    ///     Resets the view.
    /// </summary>
    public void Reset() {
        if (!IsZoomEnabled || !IsRotationEnabled || !IsPanEnabled) return;

        if (DefaultCamera != null) {
            DefaultCamera.CopyTo(Camera);
        } else {
            Camera.Reset();
            ZoomExtents();
        }
    }

    /// <summary>
    ///     Change the camera position and directions.
    /// </summary>
    /// <param name="newPosition">
    ///     The new camera position.
    /// </param>
    /// <param name="newDirection">
    ///     The new camera look direction.
    /// </param>
    /// <param name="newUpDirection">
    ///     The new camera up direction.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void SetView(Point3D newPosition, Vector3D newDirection, Vector3D newUpDirection, double animationTime) {
        Camera.AnimateTo(newPosition, newDirection, newUpDirection, animationTime);
    }

    /// <summary>
    ///     Shows the target adorner.
    /// </summary>
    /// <param name="position">The position.</param>
    public void ShowTargetAdorner(Point position) {
        if (!ShowCameraTarget) return;

        if (targetAdorner != null) return;

        if (hostPresenter is not UIElement visual) return;

        var myAdornerLayer = AdornerLayer.GetAdornerLayer(visual);
        if (myAdornerLayer == null) return;
        targetAdorner = new TargetSymbolAdorner(visual, position);
        myAdornerLayer.Add(targetAdorner);
    }

    /// <summary>
    ///     Shows the zoom rectangle.
    /// </summary>
    /// <param name="rect">The zoom rectangle.</param>
    public void ShowZoomRectangle(Rect rect) {
        if (rectangleAdorner != null) {
            rectangleAdorner.Rectangle = rect;
            return;
        }

        if (hostPresenter is not UIElement visual) return;

        var myAdornerLayer = AdornerLayer.GetAdornerLayer(visual);
        if (myAdornerLayer == null) return;
        rectangleAdorner = new RectangleAdorner(
            visual,
            rect,
            Colors.LightGray,
            Colors.Black,
            3,
            1,
            10,
            DashStyles.Solid);
        myAdornerLayer.Add(rectangleAdorner);
    }

    /// <summary>
    ///     Starts spinning.
    /// </summary>
    /// <param name="speed">The speed.</param>
    /// <param name="position">The position.</param>
    /// <param name="aroundPoint">The point to spin around.</param>
    public void StartSpin(Vector speed, Point position, Point3D aroundPoint) {
        cameraController.StartSpin(speed.ToVector2(), position, aroundPoint.ToVector3());
    }

    /// <summary>
    ///     Stops the spinning.
    /// </summary>
    public void StopSpin() {
        cameraController.StopSpin();
    }

    /// <summary>
    ///     Zooms to the extents of the specified bounding box.
    /// </summary>
    /// <param name="bounds">
    ///     The bounding box.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void ZoomExtents(Rect3D bounds, double animationTime = 0) {
        ViewportExtensions.ZoomExtents(this, bounds, animationTime);
    }

    /// <summary>
    ///     Zooms to the extents of the model.
    /// </summary>
    /// <param name="animationTime">
    ///     The animation time (milliseconds).
    /// </param>
    public void ZoomExtents(double animationTime = 200) {
        if (!IsZoomEnabled) return;

        ViewportExtensions.ZoomExtents(this, animationTime);
    }

    private void NodeInvalidated(object? sender, InvalidateTypes e) {
        RenderHostInternal?.Invalidate(e);
    }

    /// <summary>
    ///     Gets the pressed mouse buttons as flags of <see cref="MouseButtons" />.
    ///     If no button is pressed (result is zero), then it was a touch down.
    /// </summary>
    /// <returns>
    ///     The pressed mouse buttons as flags of <see cref="MouseButtons" />.
    /// </returns>
    public static MouseButtons GetPressedMouseButtons() {
        var flags = 0;
        flags |= (int) Mouse.LeftButton << 20;
        flags |= (int) Mouse.RightButton << 21;
        flags |= (int) Mouse.MiddleButton << 22;
        flags |= (int) Mouse.XButton1 << 23;
        flags |= (int) Mouse.XButton2 << 24;
        return (MouseButtons) flags;
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
        base.OnPreviewMouseDown(e);
        if (touchDownDevice == null) {
            Focus();
            MouseDownHitTest(e.GetPosition(this), e);
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e) {
        cameraController.OnMouseDown(e);
        base.OnMouseDown(e);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     This makes selection via Touch work without disabling the CameraController which uses Manipulation.
    /// </remarks>
    /// >
    protected override void OnTouchDown(TouchEventArgs e) {
        base.OnTouchDown(e);
        if (touchDownDevice == null) {
            touchDownDevice = e.TouchDevice;
            Focus();
            MouseDownHitTest(e.GetTouchPoint(this)
                .Position, e);
        }
    }

    public void EmulateMouseDownByTouch(MouseButtonEventArgs e) {
        Focus();
        MouseDownHitTest(e.GetPosition(this), e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (touchDownDevice == null) {
            var pt = e.GetPosition(this);
            MouseMoveHitTest(pt, e);
            UpdateCurrentPosition(pt);
        }
    }

    private void Viewport3DX_FormMouseMove(object sender, WinformHostExtend.FormMouseMoveEventArgs e) {
        if (touchDownDevice == null) {
            var pt = e.Location;
            MouseMoveHitTest(pt);
            UpdateCurrentPosition(pt);
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewTouchMove(TouchEventArgs e) {
        base.OnPreviewTouchMove(e);
        if (touchDownDevice == e.TouchDevice) {
            var tp = e.GetTouchPoint(this);
            var pt = tp.Position;
            MouseMoveHitTest(pt, e);
            UpdateCurrentPosition(pt);
        }
    }

    /// <summary>
    ///     Emulates the mouse move by touch.
    /// </summary>
    /// <param name="pt">The pt.</param>
    public void EmulateMouseMoveByTouch(Point pt) {
        MouseMoveHitTest(pt);
        UpdateCurrentPosition(pt);
    }

    /// <summary>
    ///     Updates the property <see cref="CurrentPosition" />.
    /// </summary>
    /// <param name="pt">The current mouse hit point.</param>
    [Obsolete]
    private void UpdateCurrentPosition(Point pt) {
        if (EnableCursorPosition) {
            CursorOnElementPosition = FindNearestPoint(pt);
            CursorPosition = this.UnProjectOnPlane(pt);
        }

        if (EnableCurrentPosition) {
            var pos = FindNearestPoint(pt);
            if (pos != null) {
                CurrentPosition = pos.Value;
            } else {
                var p = this.UnProjectOnPlane(pt);
                if (p != null) CurrentPosition = p.Value;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e) {
        base.OnMouseUp(e);
        MouseUpHitTest(e.GetPosition(this), e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e) {
        cameraController.OnMouseWheel(this, e);
        base.OnMouseWheel(e);
    }

    private void Viewport3DX_FormMouseWheel(object sender, WinformHostExtend.FormMouseWheelEventArgs e) {
        cameraController.OnMouseWheel(this, e);
        base.OnMouseWheel(e);
    }

    /// <summary>
    ///     Routes renderer-neutral native child-window input through the existing hit-test and camera contracts.
    /// </summary>
    /// <param name="sender">The native child-window host.</param>
    /// <param name="eventArgs">The decoded pointer event in device-independent coordinates.</param>
    private void D3D12PointerInput(object? sender, HwndPointerEventArgs eventArgs) =>
        ProcessD3D12Pointer(eventArgs, Keyboard.Modifiers);

    /// <summary>
    ///     Processes one Direct3D 12 child-window pointer event.
    /// </summary>
    /// <param name="eventArgs">The decoded pointer event.</param>
    /// <param name="modifiers">The current keyboard modifiers used to resolve existing mouse bindings.</param>
    internal void ProcessD3D12Pointer(HwndPointerEventArgs eventArgs, ModifierKeys modifiers) {
        eventArgs.AssertArgumentNotNull();
        var point = new Point(eventArgs.X, eventArgs.Y);
        if (eventArgs.Device == HwndPointerDevice.Touch) {
            if (eventArgs.Action == HwndPointerAction.Pressed && d3d12TouchPointerId is null)
                d3d12TouchPointerId = eventArgs.PointerId;
            if (d3d12TouchPointerId != eventArgs.PointerId) return;
        }

        switch (eventArgs.Action) {
            case HwndPointerAction.Pressed:
                Focus();
                MouseDownHitTest(point);
                if (eventArgs.Device == HwndPointerDevice.Mouse) {
                    d3d12PointerGesture?.Completed(point);
                    d3d12PointerGesture = ResolveD3D12PointerGesture(eventArgs.Button, modifiers);
                    d3d12PointerGesture?.Started(point);
                }
                break;
            case HwndPointerAction.Move:
                MouseMoveHitTest(point);
                UpdateCurrentPosition(point);
                d3d12PointerGesture?.Delta(point);
                break;
            case HwndPointerAction.Released:
                MouseUpHitTest(point);
                d3d12PointerGesture?.Completed(point);
                d3d12PointerGesture = null;
                if (eventArgs.Device == HwndPointerDevice.Touch) d3d12TouchPointerId = null;
                break;
            case HwndPointerAction.Wheel:
                cameraController.OnD3D12MouseWheel(eventArgs.WheelDelta, point);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventArgs));
        }
    }

    /// <summary>
    ///     Resolves a native mouse press to the existing configurable viewport gesture binding.
    /// </summary>
    /// <param name="button">The pressed native mouse button.</param>
    /// <param name="modifiers">The current keyboard modifiers.</param>
    /// <returns>The matching existing camera handler, or <see langword="null" />.</returns>
    internal MouseGestureHandler? ResolveD3D12PointerGesture(HwndPointerButton button, ModifierKeys modifiers) {
        var action = button switch {
            HwndPointerButton.Left => MouseAction.LeftClick,
            HwndPointerButton.Right => MouseAction.RightClick,
            HwndPointerButton.Middle => MouseAction.MiddleClick,
            _ => MouseAction.None
        };
        var command = InputBindings.OfType<MouseBinding>()
            .FirstOrDefault(binding => binding.Gesture is MouseGesture gesture
                                       && gesture.MouseAction == action
                                       && gesture.Modifiers == modifiers)
            ?.Command;
        if (command == ViewportCommands.Rotate) return cameraController.RotateHandler;
        if (command == ViewportCommands.Zoom) return cameraController.ZoomHandler;
        if (command == ViewportCommands.Pan) return cameraController.PanHandler;
        if (command == ViewportCommands.ChangeFieldOfView) return cameraController.ChangeFieldOfViewHandler;
        if (command == ViewportCommands.ZoomRectangle) return cameraController.ZoomRectangleHandler;
        return null;
    }

    /// <inheritdoc />
    protected override void OnTouchUp(TouchEventArgs e) {
        base.OnTouchUp(e);
        if (touchDownDevice == e.TouchDevice) {
            touchDownDevice = null;
            MouseUpHitTest(e.GetTouchPoint(this)
                .Position, e);
        }
    }

    public void EmulateMouseUpByTouch(MouseButtonEventArgs e) {
        MouseUpHitTest(e.GetPosition(this), e);
    }

    protected override void OnManipulationStarted(ManipulationStartedEventArgs e) {
        cameraController.OnManipulationStarted(e);
        base.OnManipulationStarted(e);
    }

    protected override void OnManipulationDelta(ManipulationDeltaEventArgs e) {
        cameraController.OnManipulationDelta(e);
        base.OnManipulationDelta(e);
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        cameraController.OnKeyDown(e);
        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e) {
        cameraController.OnManipulationCompleted(e);
        base.OnManipulationCompleted(e);
        if (touchDownDevice != null) {
            // If this.touchDownDevice has not come up for some reason, do it now.
            touchDownDevice = null;
            var pt = e.ManipulationOrigin + e.TotalManipulation.Translation;
            MouseUpHitTest(pt, e);
        }
    }

    /// <summary>
    ///     Raises the camera changed event.
    /// </summary>
    protected virtual void RaiseCameraChangedEvent() {
        // e.Handled = true;
        var args = new RoutedEventArgs(CameraChangedEvent);
        RaiseEvent(args);
    }

    /// <summary>
    ///     Animates the opacity of the specified object.
    /// </summary>
    /// <param name="obj">
    ///     The object to animate.
    /// </param>
    /// <param name="toOpacity">
    ///     The to opacity.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    private static void AnimateOpacity(UIElement obj, double toOpacity, double animationTime) {
        var a = new DoubleAnimation(toOpacity, new Duration(TimeSpan.FromMilliseconds(animationTime))) {
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.5
        };
        obj.BeginAnimation(OpacityProperty, a);
    }

    /// <summary>
    ///     The back view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void BackViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
        else
            ChangeDirection(new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
    }

    /// <summary>
    ///     The bottom view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void BottomViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
        else
            ChangeDirection(new Vector3D(0, 0, 1), new Vector3D(0, -1, 0));
    }

    /// <summary>
    ///     Determines whether the model up direction is (0,1,0).
    /// </summary>
    /// <returns>
    ///     <c>true</c> if the up direction is (0,1,0); otherwise, <c>false</c>.
    /// </returns>
    public bool IsModelUpDirectionY() => ModelUpDirection.Y.Equals(1);


    /// <summary>
    ///     Handles the change of the effects manager.
    /// </summary>
    private void EffectsManagerPropertyChanged() {
        if (RenderHostInternal is { } host) host.EffectsManager = EffectsManager;
    }

    /// <summary>
    ///     Handles the change of the render technique
    /// </summary>
    private void RenderTechniquePropertyChanged(IRenderTechnique technique) {
        if (RenderHostInternal is { } host) host.RenderTechnique = technique;
    }

    /// <summary>
    ///     Handles changes in the camera properties.
    /// </summary>
    private void CameraPropertyChanged(DependencyPropertyChangedEventArgs e) {
        if (ReferenceEquals(e.NewValue, e.OldValue))
            return;

        CameraController.ActualCamera.CameraInternal.PropertyChanged -= CameraInternal_PropertyChanged;
        CameraController.ActualCamera = e.NewValue as Camera.Camera ??
                                        (Orthographic
                                            ? orthographicCamera
                                            : perspectiveCamera);
        CameraController.ActualCamera.CameraInternal.PropertyChanged += CameraInternal_PropertyChanged;
    }

    private void CameraInternal_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        InvalidateRender();
        // Raise notification
        RaiseCameraChangedEvent();
    }

    /// <summary>
    ///     Clamps the specified value between the limits.
    /// </summary>
    /// <param name="value">
    ///     The value.
    /// </param>
    /// <param name="min">
    ///     The min.
    /// </param>
    /// <param name="max">
    ///     The max.
    /// </param>
    /// <returns>
    ///     The clamp.
    /// </returns>
    private double Clamp(double value, double min, double max) {
        if (value < min) return min;

        if (value > max) return max;

        return value;
    }

    /// <summary>
    ///     Called when the control is loaded.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void ControlLoaded(object? sender, RoutedEventArgs e) {
        if (!hasBeenLoadedBefore) {
            if (DefaultCamera != null) {
                DefaultCamera.CopyTo(perspectiveCamera);
                DefaultCamera.CopyTo(orthographicCamera);
            }

            hasBeenLoadedBefore = true;
        }

        if (BelongsToParentWindow) {
            parentWindow = FindVisualAncestor<Window>(this);
            parentWindow?.Closed += ParentWindow_Closed;
        }

        if (ZoomExtentsWhenLoaded)
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { ZoomExtents(); }));
        if (EnableSwapChainRendering) {
            d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
            d3d12CompositionTarget.Rendering += D3D12CompositionTargetRendering;
        }
    }

    private void ParentWindow_Closed(object? sender, EventArgs e) {
        ControlUnloaded(sender, new RoutedEventArgs());

        if (hostPresenter?.Content is IDisposable d) {
            hostPresenter.Content = null;
            d.Dispose();
        }
    }

    /// <summary>
    ///     Called when the control is unloaded.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void ControlUnloaded(object? sender, RoutedEventArgs e) {
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        FormMouseMove -= Viewport3DX_FormMouseMove;
        FormMouseWheel -= Viewport3DX_FormMouseWheel;
        if (BelongsToParentWindow && parentWindow != null) parentWindow.Closed -= ParentWindow_Closed;
    }

    /// <summary>
    ///     The front view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void FrontViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
        else
            ChangeDirection(new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));
    }

    /// <summary>
    ///     The left view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void LeftViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
        else
            ChangeDirection(new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
    }

    /// <summary>
    ///     The rendering event handler.
    /// </summary>
    private void OnCompositionTargetRendering() {
        var statistics = RenderHostInternal?.RenderStatistics;
        if (statistics is null) return;
        FrameRate = Math.Round(statistics.FpsStatistics.AverageFrequency, 2);
        FrameRateText = FrameRate + " FPS";
    }

    /// <summary>
    ///     Updates the existing camera and records one Direct3D 12 frame from the current viewport items.
    /// </summary>
    /// <param name="sender">The composition event source.</param>
    /// <param name="eventArgs">The WPF rendering timestamp.</param>
    private void D3D12CompositionTargetRendering(object? sender, RenderingEventArgs eventArgs) {
        var surface = d3d12Surface;
        try {
            _ = RenderD3D12Frame(eventArgs.RenderingTime);
        } catch (Exception exception) {
            d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
            HandleRenderException(surface, new RelayExceptionEventArgs(exception));
        }
    }

    /// <summary>
    ///     Records one Direct3D 12 viewport frame when the opt-in surface is ready and visible.
    /// </summary>
    /// <param name="renderingTime">The WPF rendering timestamp.</param>
    /// <returns>The number of recorded scene draws, or zero when no frame is ready.</returns>
    internal int RenderD3D12Frame(TimeSpan renderingTime) {
        var surface = d3d12Surface;
        var camera = CameraCore;
        if (surface is not {IsInitialized: true} || camera is null || Visibility != Visibility.Visible) return 0;
        cameraController.OnCompositionTargetRendering(renderingTime.Ticks);
        var clearColor = BackgroundColor.ToColor4();
        var recorded = surface.RenderViewportOnce([clearColor.X, clearColor.Y, clearColor.Z, clearColor.W],
            Items.Select(item => item.SceneNode),
            camera,
            EnableRenderFrustum,
            IsShadowMappingEnabled,
            OitRenderMode,
            OitDepthPeelingIteration,
            (float) OitWeightPower,
            (float) OitWeightDepthSlope,
            OitWeightMode,
            FxaaLevel,
            EnableSsao,
            (float) SsaoSamplingRadius,
            (float) SsaoIntensity,
            SsaoQuality,
            D2DRenderables);
        OnRendered?.Invoke(surface, EventArgs.Empty);
        return recorded;
    }

    /// <summary>
    ///     Called when the camera type is changed.
    /// </summary>
    private void OrthographicChanged() {
        var oldCamera = Camera;
        if (Orthographic)
            Camera = orthographicCamera;
        else
            Camera = perspectiveCamera;

        if (oldCamera is IProjectionCameraModel projectionCamera) projectionCamera.CopyTo(Camera);
    }

    /// <summary>
    ///     Handles a rendering exception.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    internal void HandleRenderException(object? sender, RelayExceptionEventArgs e) {
        LoggerLib.Logger.Error(e.Exception, "Viewport rendering failed");
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

        if (hostPresenter is { } presenter) presenter.Content = null;
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        if (d3d12Surface is { } surface) surface.WindowHost.PointerInput -= D3D12PointerInput;
        d3d12Surface?.Dispose();
        d3d12Surface = null;
        Disposer.RemoveAndDispose(ref RenderHostInternal);
    }

    /// <summary>
    ///     Handles the reset command.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ExecutedRoutedEventArgs" /> instance containing the event data.</param>
    private void ResetHandler(object sender, ExecutedRoutedEventArgs e) {
        Reset();
    }

    /// <summary>
    ///     The right view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void RightViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(-1, 0, 0), new Vector3D(0, 1, 0));
        else
            ChangeDirection(new Vector3D(0, -1, 0), new Vector3D(0, 0, 1));
    }

    /// <summary>
    ///     Sets the default gestures.
    /// </summary>
    private void SetDefaultGestures() {
        InputBindings.Clear();

        // Set Default Key Gestures:
        // this.InputBindings.Add(new KeyBinding(ViewportCommands.TopView, Key.U, ModifierKeys.None));
        // will not work, because the KeyBinding constructor creates a KeyGesture implictly.
        // The problem: Gestures with printable keys and the ModifierKeys "None" or "Shift" are not supported.
        // "None + U" or "Shift + U" can not be used as gesture. So we have to create a KeyBinding
        // without a gesture. For this we have to use the KeyBinding default constructor.
        var kb = new[] {
            new KeyBinding {
                Command = ViewportCommands.TopView,
                Key = Key.U
            },
            new KeyBinding {
                Command = ViewportCommands.BottomView,
                Key = Key.D
            },
            new KeyBinding {
                Command = ViewportCommands.FrontView,
                Key = Key.F
            },
            new KeyBinding {
                Command = ViewportCommands.BackView,
                Key = Key.B
            },
            new KeyBinding {
                Command = ViewportCommands.LeftView,
                Key = Key.L
            },
            new KeyBinding {
                Command = ViewportCommands.RightView,
                Key = Key.R
            }
        };
        InputBindings.AddRange(kb);

        InputBindings.Add(new KeyBinding(ViewportCommands.ZoomExtents, Key.E, ModifierKeys.Control));
        InputBindings.Add(new MouseBinding(ViewportCommands.ZoomExtents,
            new MouseGesture(MouseAction.LeftDoubleClick, ModifierKeys.Control)));
        InputBindings.Add(new MouseBinding(ViewportCommands.Rotate,
            new MouseGesture(MouseAction.RightClick, ModifierKeys.None)));
        InputBindings.Add(new MouseBinding(ViewportCommands.Zoom,
            new MouseGesture(MouseAction.RightClick, ModifierKeys.Control)));
        InputBindings.Add(new MouseBinding(ViewportCommands.Pan,
            new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift)));
        InputBindings.Add(new MouseBinding(ViewportCommands.ChangeFieldOfView,
            new MouseGesture(MouseAction.RightClick, ModifierKeys.Alt)));
        InputBindings.Add(new MouseBinding(ViewportCommands.ZoomRectangle,
            new MouseGesture(MouseAction.RightClick,
                ModifierKeys.Control | ModifierKeys.Shift)));
        InputBindings.Add(new MouseBinding(ViewportCommands.SetTarget,
            new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.Control)));
        InputBindings.Add(new MouseBinding(ViewportCommands.Reset,
            new MouseGesture(MouseAction.MiddleDoubleClick, ModifierKeys.Control)));
    }

    /// <summary>
    ///     The top view event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void TopViewHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsModelUpDirectionY())
            ChangeDirection(new Vector3D(0, -1, 0), new Vector3D(0, 0, 1));
        else
            ChangeDirection(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
    }

    /// <summary>
    ///     The UseDefaultGestures property changed.
    /// </summary>
    private void UseDefaultGesturesChanged() {
        if (UseDefaultGestures)
            SetDefaultGestures();
        else
            InputBindings.Clear();
    }

    private void ViewCubeClicked(Vector3D lookDirection, Vector3D upDirection) {
        var target = cameraController.ActualCamera.Position + cameraController.ActualCamera.LookDirection;
        var distance = cameraController.ActualCamera.LookDirection.Length;
        lookDirection *= distance;
        var newPosition = target - lookDirection;
        cameraController.ActualCamera.AnimateTo(newPosition, lookDirection, upDirection, 500);
    }

    /// <summary>
    ///     Handles the zoom extents command.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ExecutedRoutedEventArgs" /> instance containing the event data.</param>
    private void ZoomExtentsHandler(object sender, ExecutedRoutedEventArgs e) {
        ZoomExtents();
    }

    public bool HittedSomething(MouseEventArgs e) => IViewportExtensions.FindHitsInFrustum(this, e.GetPosition(this)
        .ToVector2(), ref hits);

    /// <summary>
    ///     Handles hit testing on mouse down.
    /// </summary>
    /// <param name="pt">The hit point.</param>
    /// <param name="originalInputEventArgs">
    ///     The original input event (which mouse button pressed?)
    /// </param>
    private void MouseDownHitTest(Point pt, InputEventArgs? originalInputEventArgs = null) {
        if (Overlay2D.HitTest(pt.ToVector2(), out currentHit2D)
            && currentHit2D is {ModelHit: Model.Elements2D.Abstract.Element2D e} hit2D) {
            e.RaiseEvent(new Mouse2DEventArgs(Model.Elements2D.Abstract.Element2D.MouseDown2DEvent,
                hit2D.ModelHit,
                hit2D,
                pt,
                this,
                originalInputEventArgs));
            if (originalInputEventArgs is { } inputEventArgs) inputEventArgs.Handled = true;

            return;
        }

        if (ViewBoxHitTest(pt, originalInputEventArgs)) {
            if (originalInputEventArgs is { } inputEventArgs) inputEventArgs.Handled = true;
            return;
        }

        if (!enableMouseButtonHitTest) return;

        if (IViewportExtensions.FindHits(this, pt.ToVector2(), ref hits)) {
            // We can't capture Touch because that would disable the CameraController which uses Manipulation,
            // but since Manipulation captures touch, we can be quite sure to get every relevant touch event.
            if (touchDownDevice == null) Mouse.Capture(this, CaptureMode.SubTree);

            currentHit = hits.FirstOrDefault(x => x.IsValid);
            if (currentHit != null) {
                if (currentHit.ModelHit is Model.Elements3D.AbstractElements3D.Element3D ele) {
                    ele.RaiseEvent(new MouseDown3DEventArgs(currentHit.ModelHit,
                        currentHit,
                        pt,
                        this,
                        originalInputEventArgs));
                } else if (currentHit.ModelHit is SceneNode sceneNode) {
                    sceneNode.RaiseMouseDownEvent(this, pt.ToVector2(), currentHit, originalInputEventArgs);
                    RaiseEvent(new MouseDown3DEventArgs(currentHit.ModelHit,
                        currentHit,
                        pt,
                        this,
                        originalInputEventArgs));
                }
            }
        } else {
            currentHit = null;
            // Raise event from Viewport3DX if there's no hit
            RaiseEvent(new MouseDown3DEventArgs(this, null, pt, this, originalInputEventArgs));
        }
    }

    private bool ViewBoxHitTest(Point p, InputEventArgs? originalInputEventArgs = null) {
        if (RenderContext is not { } renderContext || viewCube is not { } cube) return false;
        var ray = this.UnProject(p.ToVector2());
        var hits = new List<HitTestResult>();
        var hitContext = new HitTestContext(renderContext, ray, p.ToVector2());
        if (cube.HitTest(hitContext, ref hits)) {
            cube.RaiseEvent(new MouseDown3DEventArgs(cube, currentHit, p, this, originalInputEventArgs));
            var normal = hits[0].NormalAtHit;
            if (SilkMath.Cross(normal, ModelUpDirection.ToVector3())
                    .LengthSquared() < 1e-5) {
                var vecLeft = new Vector3(-normal.Y, -normal.Z, -normal.X);
                ViewCubeClicked(hits[0]
                    .NormalAtHit.ToVector3D(), vecLeft.ToVector3D());
            } else {
                ViewCubeClicked(hits[0]
                    .NormalAtHit.ToVector3D(), ModelUpDirection);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Handles hit testing on mouse move.
    /// </summary>
    /// <param name="pt">The hit point.</param>
    /// <param name="originalInputEventArgs">
    ///     The original input event (which mouse button pressed?)
    /// </param>
    private void MouseMoveHitTest(Point pt, InputEventArgs? originalInputEventArgs = null) {
        if (Overlay2D.HitTest(pt.ToVector2(), out var hit2D)
            && hit2D is {ModelHit: Model.Elements2D.Abstract.Element2D e} actualHit2D) {
            MouseOverModel2D = e;
            e.RaiseEvent(new Mouse2DEventArgs(Model.Elements2D.Abstract.Element2D.MouseMove2DEvent,
                actualHit2D.ModelHit,
                actualHit2D,
                pt,
                this,
                originalInputEventArgs));
            //Debug.WriteLine("hit 2D, name="+e.Name);

            return;
        }

        MouseOverModel2D = null;
        if (enableMouseButtonHitTest) {
            if (currentHit != null) {
                if (currentHit.ModelHit is Model.Elements3D.AbstractElements3D.Element3D ele) {
                    ele.RaiseEvent(new MouseMove3DEventArgs(currentHit.ModelHit,
                        currentHit,
                        pt,
                        this,
                        originalInputEventArgs));
                } else if (currentHit.ModelHit is SceneNode sceneNode) {
                    sceneNode.RaiseMouseMoveEvent(this, pt.ToVector2(), currentHit, originalInputEventArgs);
                    RaiseEvent(new MouseMove3DEventArgs(currentHit.ModelHit,
                        currentHit,
                        pt,
                        this,
                        originalInputEventArgs));
                }
            } else {
                // Raise event from Viewport3DX if there's no hit
                RaiseEvent(new MouseMove3DEventArgs(this, null, pt, this, originalInputEventArgs));
            }
        }
    }

    /// <summary>
    ///     Handles hit testing on mouse up.
    /// </summary>
    /// <param name="pt">The hit point.</param>
    /// <param name="originalInputEventArgs">
    ///     The original input event (which mouse button pressed?)
    /// </param>
    private void MouseUpHitTest(Point pt, InputEventArgs? originalInputEventArgs = null) {
        if (currentHit2D != null) {
            if (currentHit2D.ModelHit is Model.Elements2D.Abstract.Element2D element)
                element.RaiseEvent(new Mouse2DEventArgs(Model.Elements2D.Abstract.Element2D.MouseUp2DEvent,
                    currentHit2D.ModelHit,
                    currentHit2D,
                    pt,
                    this,
                    originalInputEventArgs));
            currentHit2D = null;
        }

        if (enableMouseButtonHitTest) {
            if (currentHit != null) {
                if (currentHit.ModelHit is Model.Elements3D.AbstractElements3D.Element3D ele) {
                    ele.RaiseEvent(new MouseUp3DEventArgs(currentHit.ModelHit,
                        currentHit,
                        pt,
                        this,
                        originalInputEventArgs));
                } else if (currentHit.ModelHit is SceneNode sceneNode) {
                    sceneNode.RaiseMouseUpEvent(this, pt.ToVector2(), currentHit, originalInputEventArgs);
                    RaiseEvent(
                        new MouseUp3DEventArgs(currentHit.ModelHit, currentHit, pt, this, originalInputEventArgs));
                }

                currentHit = null;
                Mouse.Capture(this, CaptureMode.None);
            } else {
                // Raise event from Viewport3DX if there's no hit
                RaiseEvent(new MouseUp3DEventArgs(this, null, pt, this, originalInputEventArgs));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RaiseRenderHostRendered(object? sender, EventArgs e) {
        OnRendered?.Invoke(sender, e);
    }

    public static T? FindVisualAncestor<T>(DependencyObject obj) where T : DependencyObject {
        var parent = VisualTreeHelper.GetParent(obj);
        while (parent != null) {
            if (parent is T typed) return typed;

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    protected override Size MeasureOverride(Size constraint) {
        if (double.IsInfinity(constraint.Width) && double.IsInfinity(constraint.Height))
            if ((_ = FindVisualAncestor<Viewbox>(this)) != null) {
                MessageText = "Must specify Width and Height for Viewport3DX in a ViewBox";
                return base.MeasureOverride(new Size(600, 400));
            }

        return base.MeasureOverride(constraint);
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
                d3d12CompositionTarget.Dispose();
                if (d3d12Surface is { } surface) surface.WindowHost.PointerInput -= D3D12PointerInput;
                d3d12Surface?.Dispose();
                d3d12Surface = null;
                if (!BelongsToParentWindow) {
                    if (hostPresenter?.Content is IDisposable d) {
                        hostPresenter.Content = null;
                        d.Dispose();
                    }

                    ClearValue(CameraProperty);
                    EffectsManager = null;
                    foreach (var item in Items) item.Dispose();
                    viewCube?.Dispose();
                    coordinateView?.Dispose();
                    Items.Clear();
                }
            }
            // TODO: dispose managed state (managed objects).
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Viewport3DX() {
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
