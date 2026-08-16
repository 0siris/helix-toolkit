// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Viewport3DX.Properties.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides the dependency properties for Viewport3DX.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using static HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D.Element3D;
using ProjectionCamera = HelixToolkit.Wpf.SharpDX.Camera.ProjectionCamera;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning control and scene graph.

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Provides the dependency properties for Viewport3DX.
/// </summary>
public partial class Viewport3DX {
    /// <summary>
    ///     Background WpfColor property.this.RenderHost
    /// </summary>
    public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register("BackgroundColor",
        typeof(WpfColor),
        typeof(Viewport3DX),
        new PropertyMetadata(Colors.White,
                             (s, e) => {
                                 ((Viewport3DX)s).RenderHostInternal?.ClearColor =
                                         ((WpfColor)e.NewValue).ToColor4();
                             }));

    public static readonly DependencyProperty RenderTechniqueProperty = DependencyProperty.Register("RenderTechnique",
        typeof(IRenderTechnique),
        typeof(Viewport3DX),
        new PropertyMetadata(null,
                             (s, e) => {
                                 ((Viewport3DX)s).RenderTechniquePropertyChanged((IRenderTechnique)e.NewValue);
                             }));

    /// <summary>
    ///     The camera changed event.
    /// </summary>
    public static readonly RoutedEvent CameraChangedEvent = EventManager.RegisterRoutedEvent(
        "CameraChanged",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(Viewport3DX));

    /// <summary>
    ///     The camera inertia factor property.
    /// </summary>
    public static readonly DependencyProperty CameraInertiaFactorProperty = DependencyProperty.Register(
        "CameraInertiaFactor",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(0.93,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.InertiaFactor = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The camera mode property
    /// </summary>
    public static readonly DependencyProperty CameraModeProperty = DependencyProperty.Register("CameraMode",
        typeof(CameraMode),
        typeof(Viewport3DX),
        new PropertyMetadata(CameraMode.Inspect,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.CameraMode = (CameraMode)e.NewValue;
                             }));

    /// <summary>
    ///     The camera property
    /// </summary>
    public static readonly DependencyProperty CameraProperty = DependencyProperty.Register("Camera",
        typeof(Camera.Camera),
        typeof(Viewport3DX),
        new PropertyMetadata(null, (s, e) => { (s as Viewport3DX).CameraPropertyChanged(e); }));

    /// <summary>
    ///     The camera rotation mode property
    /// </summary>
    public static readonly DependencyProperty CameraRotationModeProperty = DependencyProperty.Register(
        "CameraRotationMode",
        typeof(CameraRotationMode),
        typeof(Viewport3DX),
        new PropertyMetadata(CameraRotationMode.Turntable,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.CameraRotationMode = (CameraRotationMode)e.NewValue;
                             }));

    /// <summary>
    ///     The change fov cursor property.
    /// </summary>
    public static readonly DependencyProperty ChangeFieldOfViewCursorProperty = DependencyProperty.Register(
        "ChangeFieldOfViewCursor",
        typeof(Cursor),
        typeof(Viewport3DX),
        new PropertyMetadata(Cursors.ScrollNS,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ChangeFieldOfViewCursor = (Cursor)e.NewValue;
                             }));

    /// <summary>
    ///     The change field of view gesture property.
    /// </summary>
    public static readonly DependencyProperty ChangeFieldOfViewGestureProperty = DependencyProperty.Register(
        "ChangeFieldOfViewGesture",
        typeof(MouseGesture),
        typeof(Viewport3DX),
        new PropertyMetadata(new MouseGesture(MouseAction.RightClick, ModifierKeys.Alt)));

    /// <summary>
    ///     The change field of view gesture property.
    /// </summary>
    public static readonly DependencyProperty ChangeLookAtGestureProperty = DependencyProperty.Register(
        "ChangeLookAtGesture",
        typeof(MouseGesture),
        typeof(Viewport3DX),
        new PropertyMetadata(new MouseGesture(MouseAction.RightDoubleClick)));

    /// <summary>
    ///     The coordinate system horizontal position property. Relative to viewport center
    ///     <para>Default: -0.8</para>
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemHorizontalPositionProperty = DependencyProperty.Register(
        "CoordinateSystemHorizontalPosition",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(-0.8));

    /// <summary>
    ///     The coordinate system label foreground property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelForegroundProperty = DependencyProperty.Register(
        "CoordinateSystemLabelForeground",
        typeof(WpfColor),
        typeof(Viewport3DX),
        new PropertyMetadata(Colors.DarkGray));

    /// <summary>
    ///     The is coordinate system mover enabled property
    /// </summary>
    public static readonly DependencyProperty IsCoordinateSystemMoverEnabledProperty =
        DependencyProperty.Register("IsCoordinateSystemMoverEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true));


    /// <summary>
    ///     The coordinate system label X property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelXProperty = DependencyProperty.Register(
        "CoordinateSystemLabelX",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata("X"));

    /// <summary>
    ///     The coordinate system label Y property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelYProperty = DependencyProperty.Register(
        "CoordinateSystemLabelY",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata("Y"));

    /// <summary>
    ///     The coordinate system label Z property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelZProperty = DependencyProperty.Register(
        "CoordinateSystemLabelZ",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata("Z"));

    /// <summary>
    ///     The coordinate system color X property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemAxisXColorProperty = DependencyProperty.Register(
        "CoordinateSystemAxisXColor",
        typeof(WpfColor),
        typeof(Viewport3DX),
        new PropertyMetadata(Colors.Red));

    /// <summary>
    ///     The coordinate system WpfColor Y property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemAxisYColorProperty = DependencyProperty.Register(
        "CoordinateSystemAxisYColor",
        typeof(WpfColor),
        typeof(Viewport3DX),
        new PropertyMetadata(Colors.Green));

    /// <summary>
    ///     The coordinate system WpfColor Z property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemAxisZColorProperty = DependencyProperty.Register(
        "CoordinateSystemAxisZColor",
        typeof(WpfColor),
        typeof(Viewport3DX),
        new PropertyMetadata(Colors.Blue));

    /// <summary>
    ///     The coordinate system vertical position property. Relative to viewport center.
    ///     <para>Default: -0.8</para>
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemVerticalPositionProperty = DependencyProperty.Register(
        "CoordinateSystemVerticalPosition",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(-0.8));

    /// <summary>
    ///     The coordinate system size property.
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemSizeProperty = DependencyProperty.Register(
        "CoordinateSystemSize",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0));

    /// <summary>
    ///     The current position property.
    /// </summary>
    public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register("CurrentPosition",
        typeof(Point3D),
        typeof(Viewport3DX),
        new FrameworkPropertyMetadata(new Point3D(0, 0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    ///     Identifies the <see cref="CursorPosition" /> dependency property.
    /// </summary>
    /// <remarks>
    ///     The return value equals ConstructionPlanePosition or CursorModelSnapPosition if CursorSnapToModels is not null.
    /// </remarks>
    public static readonly DependencyProperty CursorPositionProperty =
        DependencyProperty.Register("CursorPosition",
                                    typeof(Point3D?),
                                    typeof(Viewport3DX),
                                    new FrameworkPropertyMetadata(null,
                                                                  FrameworkPropertyMetadataOptions
                                                                      .BindsTwoWayByDefault));

    /// <summary>
    ///     Identifies the <see cref="CursorOnElementPosition" /> dependency property.
    /// </summary>
    /// <remarks>
    ///     This property returns the position of the nearest model.
    /// </remarks>
    public static readonly DependencyProperty CursorOnElementPositionProperty =
        DependencyProperty.Register("CursorOnElementPosition",
                                    typeof(Point3D?),
                                    typeof(Viewport3DX),
                                    new FrameworkPropertyMetadata(null,
                                                                  FrameworkPropertyMetadataOptions
                                                                      .BindsTwoWayByDefault));

    /// <summary>
    ///     The default camera property.
    /// </summary>
    public static readonly DependencyProperty DefaultCameraProperty = DependencyProperty.Register("DefaultCamera",
        typeof(ProjectionCamera),
        typeof(Viewport3DX),
        new PropertyMetadata(null,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.DefaultCamera = e.NewValue as ProjectionCamera;
                             }));

    /// <summary>
    ///     The EnableCurrentPosition property.
    /// </summary>
    public static readonly DependencyProperty EnableCurrentPositionProperty = DependencyProperty.Register(
        "EnableCurrentPosition",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false));

    /// <summary>
    ///     Identifies the <see cref="EnableCursorPosition" /> dependency property.
    ///     It enables (true) or disables (false) the calculation of the cursor position in the 3D Viewport
    /// </summary>
    public static readonly DependencyProperty EnableCursorPositionProperty =
        DependencyProperty.Register("EnableCursorPosition",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new UIPropertyMetadata(false));

    /// <summary>
    ///     The EffectsManager property.
    /// </summary>
    public static readonly DependencyProperty EffectsManagerProperty = DependencyProperty.Register("EffectsManager",
        typeof(IEffectsManager),
        typeof(Viewport3DX),
        new PropertyMetadata(null,
                             (s, _) => ((Viewport3DX)s).EffectsManagerPropertyChanged()));

    /// <summary>
    ///     The field of view text property.
    /// </summary>
    public static readonly DependencyProperty FieldOfViewTextProperty = DependencyProperty.Register(
        "FieldOfViewText",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The frame rate property.
    /// </summary>
    public static readonly DependencyProperty FrameRateProperty =
        DependencyProperty.Register("FrameRate", typeof(double), typeof(Viewport3DX));

    /// <summary>
    ///     The frame rate text property.
    /// </summary>
    public static readonly DependencyProperty FrameRateTextProperty = DependencyProperty.Register(
        "FrameRateText",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The front view gesture property.
    /// </summary>
    public static readonly DependencyProperty FrontViewGestureProperty = DependencyProperty.Register("FrontViewGesture",
        typeof(InputGesture),
        typeof(Viewport3DX),
        new PropertyMetadata(new KeyGesture(Key.F, ModifierKeys.Control)));

    /// <summary>
    ///     The infinite spin property.
    /// </summary>
    public static readonly DependencyProperty InfiniteSpinProperty = DependencyProperty.Register("InfiniteSpin",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.InfiniteSpin = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The info background property.
    /// </summary>
    public static readonly DependencyProperty InfoBackgroundProperty = DependencyProperty.Register("InfoBackground",
        typeof(WpfBrush),
        typeof(Viewport3DX),
        new PropertyMetadata(new WpfSolidColorBrush(WpfColor.FromArgb(0x80, 0x8f, 0x8f, 0x8f))));

    /// <summary>
    ///     The info foreground property.
    /// </summary>
    public static readonly DependencyProperty InfoForegroundProperty = DependencyProperty.Register(
        "InfoForeground",
        typeof(WpfBrush),
        typeof(Viewport3DX),
        new PropertyMetadata(Brushes.Blue));

    /// <summary>
    ///     The message text property.
    /// </summary>
    public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register(
        "MessageText",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The render exception property.
    /// </summary>
    public static DependencyProperty RenderExceptionProperty = DependencyProperty.Register(
        "RenderException",
        typeof(Exception),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    ///// <summary>
    ///// The is deferred shading enabled propery
    ///// </summary>
    //public static readonly DependencyProperty IsDeferredShadingEnabledProperty = DependencyProperty.Register(
    //    "IsDeferredShadingEnabled", typeof(bool), typeof(Viewport3DX), new PropertyMetadata(false, (s, e) => ((Viewport3DX)s).ReAttach()));

    /// <summary>
    ///     The is deferred shading enabled propery
    /// </summary>
    public static readonly DependencyProperty IsShadowMappingEnabledProperty = DependencyProperty.Register(
        "IsShadowMappingEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (s, e) => {
                                 ((Viewport3DX)s).RenderHostInternal?.IsShadowMapEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The is change field of view enabled property
    /// </summary>
    public static readonly DependencyProperty IsChangeFieldOfViewEnabledProperty = DependencyProperty.Register(
        "IsChangeFieldOfViewEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.IsChangeFieldOfViewEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     Identifies the <see cref="IsInertiaEnabled" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsInertiaEnabledProperty =
        DependencyProperty.Register("IsInertiaEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.CameraController.IsInertiaEnabled =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The is pan enabled property
    /// </summary>
    public static readonly DependencyProperty IsPanEnabledProperty = DependencyProperty.Register("IsPanEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.IsPanEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The is rotation enabled property
    /// </summary>
    public static readonly DependencyProperty IsRotationEnabledProperty = DependencyProperty.Register(
        "IsRotationEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.IsRotationEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The enable touch rotate property
    /// </summary>
    public static readonly DependencyProperty IsTouchRotateEnabledProperty =
        DependencyProperty.Register("IsTouchRotateEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.CameraController.EnableTouchRotate =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The IsTouchZoomEnabled property.
    /// </summary>
    public static readonly DependencyProperty IsPinchZoomEnabledProperty = DependencyProperty.Register(
        "IsPinchZoomEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.EnablePinchZoom = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The pinch zoom at center property
    /// </summary>
    public static readonly DependencyProperty PinchZoomAtCenterProperty =
        DependencyProperty.Register("PinchZoomAtCenter",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.CameraController.PinchZoomAtCenter =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable touch rotate property
    /// </summary>
    public static readonly DependencyProperty IsThreeFingerPanningEnabledProperty =
        DependencyProperty.Register("IsThreeFingerPanningEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.CameraController.EnableThreeFingerPan =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The is zoom enabled property
    /// </summary>
    public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register("IsZoomEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.IsZoomEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The left right pan sensitivity property.
    /// </summary>
    public static readonly DependencyProperty LeftRightPanSensitivityProperty = DependencyProperty.Register(
        "LeftRightPanSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.LeftRightPanSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The left right rotation sensitivity property.
    /// </summary>
    public static readonly DependencyProperty LeftRightRotationSensitivityProperty = DependencyProperty.Register(
        "LeftRightRotationSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.LeftRightRotationSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The maximum field of view property
    /// </summary>
    public static readonly DependencyProperty MaximumFieldOfViewProperty = DependencyProperty.Register(
        "MaximumFieldOfView",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(120.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.MaximumFieldOfView = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The minimum field of view property
    /// </summary>
    public static readonly DependencyProperty MinimumFieldOfViewProperty = DependencyProperty.Register(
        "MinimumFieldOfView",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(10.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.MinimumFieldOfView = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The model up direction property
    /// </summary>
    public static readonly DependencyProperty ModelUpDirectionProperty = DependencyProperty.Register("ModelUpDirection",
        typeof(Vector3D),
        typeof(Viewport3DX),
        new PropertyMetadata(new Vector3D(0, 1, 0),
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ModelUpDirection = ((Vector3D)e.NewValue).ToVector3();
                             }));

    /// <summary>
    ///     The orthographic property.
    /// </summary>
    public static readonly DependencyProperty OrthographicProperty = DependencyProperty.Register("Orthographic",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false, (s, _) => ((Viewport3DX)s).OrthographicChanged()));

    /// <summary>
    ///     The orthographic toggle gesture property.
    /// </summary>
    public static readonly DependencyProperty OrthographicToggleGestureProperty = DependencyProperty.Register(
        "OrthographicToggleGesture",
        typeof(InputGesture),
        typeof(Viewport3DX),
        new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)));

    /// <summary>
    ///     The page up down zoom sensitivity property.
    /// </summary>
    public static readonly DependencyProperty PageUpDownZoomSensitivityProperty = DependencyProperty.Register(
        "PageUpDownZoomSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.PageUpDownZoomSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The pan cursor property
    /// </summary>
    public static readonly DependencyProperty PanCursorProperty = DependencyProperty.Register("PanCursor",
        typeof(Cursor),
        typeof(Viewport3DX),
        new PropertyMetadata(Cursors.Hand,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.PanCursor = (Cursor)e.NewValue;
                             }));

    /// <summary>
    ///     The rotate around mouse down point property
    /// </summary>
    public static readonly DependencyProperty RotateAroundMouseDownPointProperty = DependencyProperty.Register(
        "RotateAroundMouseDownPoint",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.RotateAroundMouseDownPoint = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The rotate cursor property
    /// </summary>
    public static readonly DependencyProperty RotateCursorProperty = DependencyProperty.Register("RotateCursor",
        typeof(Cursor),
        typeof(Viewport3DX),
        new PropertyMetadata(Cursors.SizeAll,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.RotateCursor = (Cursor)e.NewValue;
                             }));

    /// <summary>
    ///     The rotation sensitivity property
    /// </summary>
    public static readonly DependencyProperty RotationSensitivityProperty = DependencyProperty.Register(
        "RotationSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.RotationSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The show camera info property.
    /// </summary>
    public static readonly DependencyProperty ShowCameraInfoProperty = DependencyProperty.Register("ShowCameraInfo",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 if ((d as Viewport3DX).RenderHostInternal != null) {
                                     if ((bool)e.NewValue)
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail |= RenderDetail.Camera;
                                     else
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail &= ~RenderDetail.Camera;
                                 }
                             }));

    /// <summary>
    ///     The show camera target property.
    /// </summary>
    public static readonly DependencyProperty ShowCameraTargetProperty = DependencyProperty.Register("ShowCameraTarget",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ShowCameraTarget = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The show coordinate system property.
    /// </summary>
    public static readonly DependencyProperty ShowCoordinateSystemProperty = DependencyProperty.Register(
        "ShowCoordinateSystem",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false));

    /// <summary>
    ///     The show frame rate property.
    /// </summary>
    public static readonly DependencyProperty ShowFrameRateProperty = DependencyProperty.Register("ShowFrameRate",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 if ((d as Viewport3DX).RenderHostInternal != null) {
                                     if ((bool)e.NewValue)
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail |= RenderDetail.Fps;
                                     else
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail &= ~RenderDetail.Fps;
                                 }
                             }));

    /// <summary>
    ///     The show frame rate property.
    /// </summary>
    public static readonly DependencyProperty ShowFrameDetailsProperty = DependencyProperty.Register("ShowFrameDetails",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 if ((d as Viewport3DX).RenderHostInternal != null) {
                                     if ((bool)e.NewValue)
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail |=
                                             RenderDetail.Statistics;
                                     else
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail &=
                                             ~RenderDetail.Statistics;
                                 }
                             }));

    /// <summary>
    ///     The show triangle count info property.
    /// </summary>
    public static readonly DependencyProperty ShowTriangleCountInfoProperty = DependencyProperty.Register(
        "ShowTriangleCountInfo",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 if ((d as Viewport3DX).RenderHostInternal != null) {
                                     if ((bool)e.NewValue)
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail |=
                                             RenderDetail.TriangleInfo;
                                     else
                                         (d as Viewport3DX).RenderHostInternal.ShowRenderDetail &=
                                             ~RenderDetail.TriangleInfo;
                                 }
                             }));

    /// <summary>
    ///     The show view cube property.
    /// </summary>
    public static readonly DependencyProperty ShowViewCubeProperty = DependencyProperty.Register(
        "ShowViewCube",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true));

    /// <summary>
    ///     The spin release time property
    /// </summary>
    public static readonly DependencyProperty SpinReleaseTimeProperty = DependencyProperty.Register("SpinReleaseTime",
        typeof(int),
        typeof(Viewport3DX),
        new PropertyMetadata(200,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.SpinReleaseTime = (int)e.NewValue;
                             }));

    /// <summary>
    ///     The sub title property.
    /// </summary>
    public static readonly DependencyProperty SubTitleProperty = DependencyProperty.Register(
        "SubTitle",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The sub title size property.
    /// </summary>
    public static readonly DependencyProperty SubTitleSizeProperty = DependencyProperty.Register(
        "SubTitleSize",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(12.0));

    /// <summary>
    ///     The text brush property.
    /// </summary>
    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(
        "TextBrush",
        typeof(WpfBrush),
        typeof(Viewport3DX),
        new PropertyMetadata(Brushes.Black));

    /// <summary>
    ///     The title background property.
    /// </summary>
    public static readonly DependencyProperty TitleBackgroundProperty = DependencyProperty.Register(
        "TitleBackground",
        typeof(WpfBrush),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The title font family property.
    /// </summary>
    public static readonly DependencyProperty TitleFontFamilyProperty = DependencyProperty.Register(
        "TitleFontFamily",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The title property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        "Title",
        typeof(string),
        typeof(Viewport3DX),
        new PropertyMetadata(null));

    /// <summary>
    ///     The title size property.
    /// </summary>
    public static readonly DependencyProperty TitleSizeProperty = DependencyProperty.Register(
        "TitleSize",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(12.0));


    /// <summary>
    ///     The up down Pan sensitivity property.
    /// </summary>
    public static readonly DependencyProperty UpDownPanSensitivityProperty = DependencyProperty.Register(
        "UpDownPanSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.UpDownPanSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The up down rotation sensitivity property.
    /// </summary>
    public static readonly DependencyProperty UpDownRotationSensitivityProperty = DependencyProperty.Register(
        "UpDownRotationSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.UpDownRotationSensitivity = (double)e.NewValue;
                             }));

    // Using a DependencyProperty as the backing store for AllowUpDownRotation.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AllowUpDownRotationProperty =
        DependencyProperty.Register("AllowUpDownRotation",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             var allowX = viewport.cameraController.AllowRotateXy.X;
                                                             float allowY = (bool)e.NewValue ? 1 : 0;
                                                             viewport.CameraController.AllowRotateXy =
                                                                 new Vector2(allowX, allowY);
                                                         }));

    // Using a DependencyProperty as the backing store for AllowLeftRightRotation.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AllowLeftRightRotationProperty =
        DependencyProperty.Register("AllowLeftRightRotation",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             float allowX = (bool)e.NewValue ? 1 : 0;
                                                             var allowY = viewport.cameraController.AllowRotateXy.Y;
                                                             viewport.CameraController.AllowRotateXy =
                                                                 new Vector2(allowX, allowY);
                                                         }));


    /// <summary>
    ///     The use default gestures property
    /// </summary>
    public static readonly DependencyProperty UseDefaultGesturesProperty = DependencyProperty.Register(
        "UseDefaultGestures",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true, (s, _) => ((Viewport3DX)s).UseDefaultGesturesChanged()));

    /// <summary>
    ///     The view cube texture. It must be a 6x1 (ex: 600x100) ratio image. You can also use
    ///     BitmapExtension.CreateViewBoxBitmapSource to create
    /// </summary>
    public static readonly DependencyProperty ViewCubeTextureProperty = DependencyProperty.Register(
        "ViewCubeTexture",
        typeof(TextureModel),
        typeof(Viewport3DX),
        new PropertyMetadata());

    /// <summary>
    ///     The view cube horizontal position property. Relative to viewport center.
    ///     <para>Default: 0.8</para>
    /// </summary>
    public static readonly DependencyProperty ViewCubeHorizontalPositionProperty = DependencyProperty.Register(
        "ViewCubeHorizontalPosition",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(0.8));

    /// <summary>
    ///     Identifies the <see cref=" IsViewCubeEdgeClicksEnabled" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsViewCubeEdgeClicksEnabledProperty =
        DependencyProperty.Register("IsViewCubeEdgeClicksEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false));

    /// <summary>
    ///     Identifies the <see cref=" IsViewCubeEdgeClicksEnabled" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsViewCubeMoverEnabledProperty =
        DependencyProperty.Register("IsViewCubeMoverEnabled",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true));

    /// <summary>
    ///     The view cube vertical position property. Relative to viewport center.
    ///     <para>Default: -0.8</para>
    /// </summary>
    public static readonly DependencyProperty ViewCubeVerticalPositionProperty = DependencyProperty.Register(
        "ViewCubeVerticalPosition",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(-0.8));

    /// <summary>
    ///     The view cube size property.
    /// </summary>
    public static readonly DependencyProperty ViewCubeSizeProperty = DependencyProperty.Register(
        "ViewCubeSize",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0));

    /// <summary>
    ///     The zoom around mouse down point property
    /// </summary>
    public static readonly DependencyProperty ZoomAroundMouseDownPointProperty = DependencyProperty.Register(
        "ZoomAroundMouseDownPoint",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomAroundMouseDownPoint = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The zoom cursor property
    /// </summary>
    public static readonly DependencyProperty ZoomCursorProperty = DependencyProperty.Register("ZoomCursor",
        typeof(Cursor),
        typeof(Viewport3DX),
        new PropertyMetadata(Cursors.SizeNS,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomCursor = (Cursor)e.NewValue;
                             }));

    /// <summary>
    ///     The far zoom distance limit property.
    /// </summary>
    public static readonly DependencyProperty ZoomDistanceLimitFarProperty = DependencyProperty.Register(
        "ZoomDistanceLimitFar",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(double.PositiveInfinity,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomDistanceLimitFar = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The near zoom distance limit property.
    /// </summary>
    public static readonly DependencyProperty ZoomDistanceLimitNearProperty = DependencyProperty.Register(
        "ZoomDistanceLimitNear",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(0.001,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomDistanceLimitNear = (double)e.NewValue;
                             }));

    /// <summary>
    ///     The zoom extents when loaded property.
    /// </summary>
    public static readonly DependencyProperty ZoomExtentsWhenLoadedProperty = DependencyProperty.Register(
        "ZoomExtentsWhenLoaded",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false));

    /// <summary>
    ///     The zoom rectangle cursor property
    /// </summary>
    public static readonly DependencyProperty ZoomRectangleCursorProperty = DependencyProperty.Register(
        "ZoomRectangleCursor",
        typeof(Cursor),
        typeof(Viewport3DX),
        new PropertyMetadata(Cursors.SizeNWSE,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomRectangleCursor = (Cursor)e.NewValue;
                             }));

    /// <summary>
    ///     The zoom rectangle gesture property.
    /// </summary>
    public static readonly DependencyProperty ZoomRectangleGestureProperty = DependencyProperty.Register(
        "ZoomRectangleGesture",
        typeof(MouseGesture),
        typeof(Viewport3DX),
        new PropertyMetadata(new MouseGesture(MouseAction.RightClick, ModifierKeys.Control | ModifierKeys.Shift)));

    /// <summary>
    ///     The zoom sensitivity property
    /// </summary>
    public static readonly DependencyProperty ZoomSensitivityProperty = DependencyProperty.Register("ZoomSensitivity",
        typeof(double),
        typeof(Viewport3DX),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.ZoomSensitivity = (double)e.NewValue;
                             }));

    /// <summary>
    ///     Set MSAA Level
    /// </summary>
    public static readonly DependencyProperty MsaaProperty = DependencyProperty.Register("Msaa",
        typeof(MsaaLevel),
        typeof(Viewport3DX),
        new PropertyMetadata(MsaaLevel.Disable,
                             (s, e) => {
                                 var viewport = s as Viewport3DX;
                                 viewport.RenderHostInternal?.Msaa = (MsaaLevel)e.NewValue;
                             }));

    /// <summary>
    ///     The is move enabled property.
    /// </summary>
    public static readonly DependencyProperty IsMoveEnabledProperty = DependencyProperty.Register("IsMoveEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true,
                             (d, e) => {
                                 var viewport = d as Viewport3DX;
                                 viewport.CameraController.IsMoveEnabled = (bool)e.NewValue;
                             }));


    /// <summary>
    ///     Rotate around this fixed rotation point only.<see cref="FixedRotationPointEnabledProperty" />
    /// </summary>
    public static readonly DependencyProperty FixedRotationPointProperty = DependencyProperty.Register(
        "FixedRotationPoint",
        typeof(Point3D),
        typeof(Viewport3DX),
        new PropertyMetadata(new Point3D(),
                             (d, e) => {
                                 (d as Viewport3DX).CameraController.FixedRotationPoint =
                                     ((Point3D)e.NewValue).ToVector3();
                             }));

    /// <summary>
    ///     Enable fixed rotation mode and use FixedRotationPoint for rotation. Only works under CameraMode = Inspect
    /// </summary>
    public static readonly DependencyProperty FixedRotationPointEnabledProperty = DependencyProperty.Register(
        "FixedRotationPointEnabled",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(false,
                             (d, e) => {
                                 (d as Viewport3DX).CameraController.FixedRotationPointEnabled = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     Enable mouse button hit test
    /// </summary>
    public static readonly DependencyProperty EnableMouseButtonHitTestProperty = DependencyProperty.Register(
        "EnableMouseButtonHitTest",
        typeof(bool),
        typeof(Viewport3DX),
        new PropertyMetadata(true, (d, e) => { (d as Viewport3DX).enableMouseButtonHitTest = (bool)e.NewValue; }));

    /// <summary>
    ///     Manually move camera to look at a point in 3D space
    /// </summary>
    public static readonly DependencyProperty ManualLookAtPointProperty = DependencyProperty.Register(
        "ManualLookAtPoint",
        typeof(Point3D),
        typeof(Viewport3DX),
        new FrameworkPropertyMetadata(new Point3D(),
                                      (_, _) => { },
                                      (d, e) => {
                                          (d as Viewport3DX).LookAt((Point3D)e);
                                          return e;
                                      }) {
            BindsTwoWayByDefault = false
        });

    /// <summary>
    ///     Enable render frustum to avoid rendering model if it is out of view frustum
    /// </summary>
    public static readonly DependencyProperty EnableRenderFrustumProperty
        = DependencyProperty.Register("EnableRenderFrustumProperty",
                                      typeof(bool),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(true,
                                                           (s, e) => {
                                                               var viewport = s as Viewport3DX;
                                                               if (viewport.RenderHostInternal != null)
                                                                   viewport.EnableRenderFrustum = (bool)e.NewValue;
                                                           }));

    /// <summary>
    ///     <para>Enable deferred rendering. Use multithreading to call rendering procedure using different Deferred Context.</para>
    ///     <para>Deferred Rendering: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476892.aspx</para>
    ///     <para>https://docs.nvidia.com/gameworks/content/gameworkslibrary/graphicssamples/d3d_samples/d3d11deferredcontextssample.htm</para>
    ///     <para>Note: Only if draw calls > 3000 to be benefit according to the online performance test.</para>
    /// </summary>
    public static readonly DependencyProperty EnableDeferredRenderingProperty
        = DependencyProperty.Register("EnableDeferredRendering",
                                      typeof(bool),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(false));

    /// <summary>
    ///     Used to create multiple viewport with shared models.
    /// </summary>
    public static readonly DependencyProperty EnableSharedModelModeProperty
        = DependencyProperty.Register("EnableSharedModelMode",
                                      typeof(bool),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(false,
                                                           (s, e) => {
                                                               var viewport = s as Viewport3DX;
                                                               viewport.RenderHostInternal?.EnableSharingModelMode =
                                                                       (bool)e.NewValue;
                                                           }));

    /// <summary>
    ///     Binding to the element inherit with <see cref="IModelContainer" />
    /// </summary>
    public static readonly DependencyProperty SharedModelContainerProperty
        = DependencyProperty.Register("SharedModelContainer",
                                      typeof(IModelContainer),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(null,
                                                           (d, e) => {
                                                               var viewport = d as Viewport3DX;
                                                               if (e.OldValue is IModelContainer o)
                                                                   o.DettachViewport3DX(viewport);
                                                               if (e.NewValue is IModelContainer n)
                                                                   n.AttachViewport3DX(viewport);
                                                               viewport.SharedModelContainerInternal =
                                                                   (IModelContainer)e.NewValue;
                                                               viewport.RenderHostInternal?.SharedModelContainer =
                                                                       (IModelContainer)e.NewValue;
                                                           }));

    /// <summary>
    ///     The enable swap chain rendering property
    /// </summary>
    public static readonly DependencyProperty EnableSwapChainRenderingProperty
        = DependencyProperty.Register("EnableSwapChainRendering",
                                      typeof(bool),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(false));

    /// <summary>
    ///     The content2 d property
    /// </summary>
    public static readonly DependencyProperty Content2DProperty
        = DependencyProperty.Register("Content2D",
                                      typeof(Model.Elements2D.Abstract.Element2D),
                                      typeof(Viewport3DX),
                                      new PropertyMetadata(null,
                                                           (d, e) => {
                                                               if (e.OldValue is Model.Elements2D.Abstract.Element2D elementOld)
                                                                   (d as Viewport3DX).Overlay2D.Children.Remove(
                                                                       elementOld);
                                                               if (e.NewValue is Model.Elements2D.Abstract.Element2D elementNew)
                                                                   (d as Viewport3DX).Overlay2D.Children
                                                                       .Add(elementNew);
                                                           }));

    /// <summary>
    ///     The enable d2 d rendering property
    /// </summary>
    public static readonly DependencyProperty EnableD2DRenderingProperty =
        DependencyProperty.Register("EnableD2DRendering",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .RenderD2D = (bool)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));

    /// <summary>
    ///     The enable automatic octree update property
    /// </summary>
    public static readonly DependencyProperty EnableAutoOctreeUpdateProperty =
        DependencyProperty.Register("EnableAutoOctreeUpdate",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.RenderHostInternal?.RenderConfiguration
                                                                         .AutoUpdateOctree = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets a value indicating for Transparent objects render mode.
    ///     <see cref="MaterialGeometryModel3D.IsTransparent" />, <see cref="BillboardTextModel3D.IsTransparent" />
    /// </summary>
    public static readonly DependencyProperty OitRenderModeProperty =
        DependencyProperty.Register("OitRenderMode",
                                    typeof(OitRenderType),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(OitRenderType.DepthPeeling,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .OitRenderType = (OitRenderType)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));

    /// <summary>
    ///     The Order independent transparent rendering color weight power property
    /// </summary>
    public static readonly DependencyProperty OitWeightPowerProperty =
        DependencyProperty.Register("OitWeightPower",
                                    typeof(double),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(3.0,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .OitWeightPower = (float)(double)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));


    /// <summary>
    ///     The oit weight depth slope property
    /// </summary>
    public static readonly DependencyProperty OitWeightDepthSlopeProperty =
        DependencyProperty.Register("OitWeightDepthSlope",
                                    typeof(double),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .OitWeightDepthSlope =
                                                                     (float)(double)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));

    /// <summary>
    ///     The oit weight mode property
    ///     <para>Please refer to http://jcgt.org/published/0002/02/09/ </para>
    ///     <para>Linear0: eq7; Linear1: eq8; Linear2: eq9; NonLinear: eq10</para>
    /// </summary>
    public static readonly DependencyProperty OitWeightModeProperty =
        DependencyProperty.Register("OitWeightMode",
                                    typeof(OitWeightMode),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(OitWeightMode.Linear1,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .OitWeightMode = (OitWeightMode)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));

    public static readonly DependencyProperty OitDepthPeelingIterationProperty =
        DependencyProperty.Register("OitDepthPeelingIteration",
                                    typeof(int),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(4,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .OitDepthPeelingIteration = (int)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));

    /// <summary>
    ///     The fxaa level property
    /// </summary>
    public static readonly DependencyProperty FxaaLevelProperty =
        DependencyProperty.Register("FxaaLevel",
                                    typeof(FxaaLevel),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(FxaaLevel.None,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .FxaaLevel = (FxaaLevel)e.NewValue;
                                                                 viewport.InvalidateRender();
                                                             }
                                                         }));


    /// <summary>
    ///     The enable design time rendering property
    /// </summary>
    public static readonly DependencyProperty EnableDesignModeRenderingProperty =
        DependencyProperty.Register("EnableDesignModeRendering",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false));


    /// <summary>
    ///     The enable render order property. <see cref="EnableRenderOrder" />
    /// </summary>
    public static readonly DependencyProperty EnableRenderOrderProperty =
        DependencyProperty.Register("EnableRenderOrder",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .EnableRenderOrder = (bool)e.NewValue;
                                                                 viewport.RenderHostInternal
                                                                         .InvalidatePerFrameRenderables();
                                                             }
                                                         }));

    /// <summary>
    ///     The enable ssao property
    /// </summary>
    public static readonly DependencyProperty EnableSsaoProperty =
        DependencyProperty.Register("EnableSsao",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .EnableSsao = (bool)e.NewValue;
                                                                 viewport.RenderHostInternal.InvalidateRender();
                                                             }
                                                         }));


    /// <summary>
    ///     The ssao sampling radius property
    /// </summary>
    public static readonly DependencyProperty SsaoSamplingRadiusProperty =
        DependencyProperty.Register("SsaoSamplingRadius",
                                    typeof(double),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(0.5,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .SsaoRadius = (float)(double)e.NewValue;
                                                                 viewport.RenderHostInternal.InvalidateRender();
                                                             }
                                                         }));

    public static readonly DependencyProperty SsaoIntensityProperty =
        DependencyProperty.Register("SsaoIntensity",
                                    typeof(double),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .SsaoIntensity = (float)(double)e.NewValue;
                                                                 viewport.RenderHostInternal.InvalidateRender();
                                                             }
                                                         }));

    /// <summary>
    ///     The ssao quality property
    /// </summary>
    public static readonly DependencyProperty SsaoQualityProperty =
        DependencyProperty.Register("SsaoQuality",
                                    typeof(SsaoQuality),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(SsaoQuality.Low,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.RenderHostInternal != null) {
                                                                 viewport.RenderHostInternal.RenderConfiguration
                                                                         .SsaoQuality = (SsaoQuality)e.NewValue;
                                                                 viewport.RenderHostInternal.InvalidateRender();
                                                             }
                                                         }));


    /// <summary>
    ///     The minimum update count property
    /// </summary>
    public static readonly DependencyProperty MinimumUpdateCountProperty =
        DependencyProperty.Register("MinimumUpdateCount",
                                    typeof(int),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(6,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             viewport.RenderHostInternal?.RenderConfiguration
                                                                         .MinimumUpdateCount =
                                                                     (uint)Math.Max(0, (int)e.NewValue);
                                                         }));

    /// <summary>
    ///     The belongs to parent window property
    /// </summary>
    public static readonly DependencyProperty BelongsToParentWindowProperty =
        DependencyProperty.Register("BelongsToParentWindow",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true));

    /// <summary>
    ///     The dpi scale
    /// </summary>
    public static readonly DependencyProperty DpiScaleProperty =
        DependencyProperty.Register("DpiScale",
                                    typeof(double),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.hostPresenter is {Content: IRenderCanvas canvas})
                                                                 canvas.DpiScale = (double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable dpi scale property
    /// </summary>
    public static readonly DependencyProperty EnableDpiScaleProperty =
        DependencyProperty.Register("EnableDpiScale",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.hostPresenter is {Content: IRenderCanvas canvas})
                                                                 canvas.EnableDpiScale = (bool)e.NewValue;
                                                         }));

    public static readonly DependencyProperty IncreaseSwapchainFpsProperty =
        DependencyProperty.Register("IncreaseSwapchainFps",
                                    typeof(bool),
                                    typeof(Viewport3DX),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var viewport = d as Viewport3DX;
                                                             if (viewport.hostPresenter is {Content: DPFSurfaceSwapChain
                                                                     surface
                                                                 })
                                                                 surface.IncreaseFps = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the render host internal.
    /// </summary>
    /// <value>
    ///     The render host internal.
    /// </value>
    protected IRenderHost? RenderHostInternal;

    /// <summary>
    ///     Background WpfColor
    /// </summary>
    public WpfColor BackgroundColor {
        get => (WpfColor)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the camera.
    /// </summary>
    /// <value>
    ///     The camera.
    /// </value>
    public Camera.Camera Camera {
        get => (Camera.Camera?)GetValue(CameraProperty) ?? (Orthographic ? orthographicCamera : perspectiveCamera);

        set => SetValue(CameraProperty, value);
    }

    /// <summary>
    ///     Gets the camera controller
    /// </summary>
    internal CameraController CameraController => cameraController;

    /// <summary>
    ///     Gets or sets the camera inertia factor.
    /// </summary>
    /// <value>
    ///     The camera inertia factor.
    /// </value>
    public double CameraInertiaFactor {
        get => (double)GetValue(CameraInertiaFactorProperty);

        set => SetValue(CameraInertiaFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the camera mode.
    /// </summary>
    /// <value>
    ///     The camera mode.
    /// </value>
    public CameraMode CameraMode {
        get => (CameraMode)GetValue(CameraModeProperty);

        set => SetValue(CameraModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the camera rotation mode.
    /// </summary>
    /// <value>
    ///     The camera rotation mode.
    /// </value>
    public CameraRotationMode CameraRotationMode {
        get => (CameraRotationMode)GetValue(CameraRotationModeProperty);

        set => SetValue(CameraRotationModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the change field of view cursor.
    /// </summary>
    /// <value>
    ///     The change field of view cursor.
    /// </value>
    public Cursor ChangeFieldOfViewCursor {
        get => (Cursor)GetValue(ChangeFieldOfViewCursorProperty);

        set => SetValue(ChangeFieldOfViewCursorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the horizontal position of the coordinate system viewport. Relative to the viewport center.
    ///     <para>Default: -0.8</para>
    /// </summary>
    /// <value>
    ///     The horizontal position.
    /// </value>
    public double CoordinateSystemHorizontalPosition {
        get => (double)GetValue(CoordinateSystemHorizontalPositionProperty);

        set => SetValue(CoordinateSystemHorizontalPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the color of the coordinate system label.
    /// </summary>
    /// <value>
    ///     The color of the coordinate system label.
    /// </value>
    public WpfColor CoordinateSystemLabelForeground {
        get => (WpfColor)GetValue(CoordinateSystemLabelForegroundProperty);

        set => SetValue(CoordinateSystemLabelForegroundProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system label X.
    /// </summary>
    /// <value>
    ///     The coordinate system label X.
    /// </value>
    public string CoordinateSystemLabelX {
        get => (string)GetValue(CoordinateSystemLabelXProperty);

        set => SetValue(CoordinateSystemLabelXProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system label Y.
    /// </summary>
    /// <value>
    ///     The coordinate system label Y.
    /// </value>
    public string CoordinateSystemLabelY {
        get => (string)GetValue(CoordinateSystemLabelYProperty);

        set => SetValue(CoordinateSystemLabelYProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system label Z.
    /// </summary>
    /// <value>
    ///     The coordinate system label Z.
    /// </value>
    public string CoordinateSystemLabelZ {
        get => (string)GetValue(CoordinateSystemLabelZProperty);

        set => SetValue(CoordinateSystemLabelZProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system color X.
    /// </summary>
    /// <value>
    ///     The coordinate system color X.
    /// </value>
    public WpfColor CoordinateSystemAxisXColor {
        get => (WpfColor)GetValue(CoordinateSystemAxisXColorProperty);

        set => SetValue(CoordinateSystemAxisXColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system color Y.
    /// </summary>
    /// <value>
    ///     The coordinate system color T.
    /// </value>
    public WpfColor CoordinateSystemAxisYColor {
        get => (WpfColor)GetValue(CoordinateSystemAxisYColorProperty);

        set => SetValue(CoordinateSystemAxisYColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the coordinate system color Z.
    /// </summary>
    /// <value>
    ///     The coordinate system color Z.
    /// </value>
    public WpfColor CoordinateSystemAxisZColor {
        get => (WpfColor)GetValue(CoordinateSystemAxisZColorProperty);

        set => SetValue(CoordinateSystemAxisZColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the vertical position of the coordinate system viewport. Relative to the viewport center
    ///     <para>Default: -0.8</para>
    /// </summary>
    /// <value>
    ///     The vertical position.
    /// </value>
    public double CoordinateSystemVerticalPosition {
        get => (double)GetValue(CoordinateSystemVerticalPositionProperty);

        set => SetValue(CoordinateSystemVerticalPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the width of the coordinate system viewport.
    /// </summary>
    /// <value>
    ///     The width of the coordinate system viewport.
    /// </value>
    public double CoordinateSystemSize {
        get => (double)GetValue(CoordinateSystemSizeProperty);

        set => SetValue(CoordinateSystemSizeProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether calculation of the <see cref="CurrentPosition" /> property is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if calculation is enabled; otherwise, <c>false</c> .
    /// </value>
    [Obsolete("EnableCurrentPosition is now obsolete, please use EnableCursorPosition instead", false)]
    public bool EnableCurrentPosition {
        get => (bool)GetValue(EnableCurrentPositionProperty);

        set => SetValue(EnableCurrentPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the current position.
    /// </summary>
    /// <value>
    ///     The current position.
    /// </value>
    /// <remarks>
    ///     The <see cref="EnableCurrentPosition" /> property must be set to true to enable updating of this property.
    /// </remarks>
    [Obsolete("CurrentPosition is now obsolete, please use CursorPosition instead", false)]
    public Point3D CurrentPosition {
        get => (Point3D)GetValue(CurrentPositionProperty);

        set => SetValue(CurrentPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether calculation of the <see cref="CursorPosition" /> properties is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if calculation is enabled; otherwise, <c>false</c> .
    /// </value>
    public bool EnableCursorPosition {
        get => (bool)GetValue(EnableCursorPositionProperty);

        set => SetValue(EnableCursorPositionProperty, value);
    }

    /// <summary>
    ///     Gets the current cursor position.
    /// </summary>
    /// <value>
    ///     The current cursor position.
    /// </value>
    /// <remarks>
    ///     The <see cref="EnableCursorPosition" /> property must be set to true to enable updating of this property.
    /// </remarks>
    public Point3D? CursorPosition {
        get => (Point3D?)GetValue(CursorPositionProperty);

        private set => SetValue(CursorPositionProperty, value);
    }

    /// <summary>
    ///     Gets the current cursor position on the nearest model. If the model is not hit, the position is <c>null</c>.
    /// </summary>
    /// <value>
    ///     The position of the model intersection.
    /// </value>
    /// <remarks>
    ///     The <see cref="EnableCursorPosition" /> property must be set to <c>true</c> to enable updating of this property.
    /// </remarks>
    public Point3D? CursorOnElementPosition {
        get => (Point3D?)GetValue(CursorOnElementPositionProperty);

        private set => SetValue(CursorOnElementPositionProperty, value);
    }


    /// <summary>
    ///     Gets or sets the default camera.
    /// </summary>
    /// <value>
    ///     The default camera.
    /// </value>
    public ProjectionCamera? DefaultCamera {
        get => (ProjectionCamera?)GetValue(DefaultCameraProperty);

        set => SetValue(DefaultCameraProperty, value);
    }

    public IRenderTechnique RenderTechnique {
        get => (IRenderTechnique)GetValue(RenderTechniqueProperty);
        set => SetValue(RenderTechniqueProperty, value);
    }

    /// <summary>
    ///     Gets or sets the field of view text.
    /// </summary>
    /// <value>
    ///     The field of view text.
    /// </value>
    public string FieldOfViewText {
        get => (string)GetValue(FieldOfViewTextProperty);

        set => SetValue(FieldOfViewTextProperty, value);
    }

    /// <summary>
    ///     Gets or sets the frame rate.
    /// </summary>
    /// <value>
    ///     The frame rate.
    /// </value>
    public double FrameRate {
        get => (double)GetValue(FrameRateProperty);

        set => SetValue(FrameRateProperty, value);
    }

    /// <summary>
    ///     Gets or sets the frame rate text.
    /// </summary>
    /// <value>
    ///     The frame rate text.
    /// </value>
    public string FrameRateText {
        get => (string)GetValue(FrameRateTextProperty);

        set => SetValue(FrameRateTextProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether infinite spin is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if infinite spin is enabled; otherwise, <c>false</c> .
    /// </value>
    public bool InfiniteSpin {
        get => (bool)GetValue(InfiniteSpinProperty);

        set => SetValue(InfiniteSpinProperty, value);
    }

    /// <summary>
    ///     Gets or sets the background brush for the CameraInfo and TriangleCount fields.
    /// </summary>
    /// <value>
    ///     The info background.
    /// </value>
    public WpfBrush InfoBackground {
        get => (WpfBrush)GetValue(InfoBackgroundProperty);

        set => SetValue(InfoBackgroundProperty, value);
    }

    /// <summary>
    ///     Gets or sets the foreground brush for informational text.
    /// </summary>
    /// <value>
    ///     The foreground brush.
    /// </value>
    public WpfBrush InfoForeground {
        get => (WpfBrush)GetValue(InfoForegroundProperty);

        set => SetValue(InfoForegroundProperty, value);
    }

    /// <summary>
    ///     Gets or sets the message text.
    /// </summary>
    /// <value>
    ///     The message text.
    /// </value>
    public string MessageText {
        get => (string)GetValue(MessageTextProperty);

        set => SetValue(MessageTextProperty, value);
    }

    /// <summary>
    ///     Gets or sets the <see cref="System.Exception" /> that occured at rendering subsystem.
    /// </summary>
    public Exception RenderException {
        get => (Exception)GetValue(RenderExceptionProperty);
        set => SetValue(RenderExceptionProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether change field of view is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if change field of view is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsChangeFieldOfViewEnabled {
        get => (bool)GetValue(IsChangeFieldOfViewEnabledProperty);

        set => SetValue(IsChangeFieldOfViewEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether inertia is enabled for the camera manipulations.
    /// </summary>
    /// <value><c>true</c> if inertia is enabled; otherwise, <c>false</c>.</value>
    public bool IsInertiaEnabled {
        get => (bool)GetValue(IsInertiaEnabledProperty);

        set => SetValue(IsInertiaEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether pan is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if pan is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsPanEnabled {
        get => (bool)GetValue(IsPanEnabledProperty);

        set => SetValue(IsPanEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether rotation is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if rotation is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsRotationEnabled {
        get => (bool)GetValue(IsRotationEnabledProperty);

        set => SetValue(IsRotationEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable one finger touch rotate].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable touch rotate]; otherwise, <c>false</c>.
    /// </value>
    public bool IsTouchRotateEnabled {
        get => (bool)GetValue(IsTouchRotateEnabledProperty);
        set => SetValue(IsTouchRotateEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether two finger pinch zoom is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if pinch zoom is enabled; otherwise, <c>false</c> .
    /// </value>
    public bool IsPinchZoomEnabled {
        get => (bool)GetValue(IsPinchZoomEnabledProperty);

        set => SetValue(IsPinchZoomEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [pinch zoom at center] instead of at finger down point.
    ///     Default is false.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [pinch zoom at center]; otherwise, <c>false</c>.
    /// </value>
    public bool PinchZoomAtCenter {
        get => (bool)GetValue(PinchZoomAtCenterProperty);
        set => SetValue(PinchZoomAtCenterProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable three finger panning].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable three finger panning]; otherwise, <c>false</c>.
    /// </value>
    public bool IsThreeFingerPanningEnabled {
        get => (bool)GetValue(IsThreeFingerPanningEnabledProperty);
        set => SetValue(IsThreeFingerPanningEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether zoom is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if zoom is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsZoomEnabled {
        get => (bool)GetValue(IsZoomEnabledProperty);

        set => SetValue(IsZoomEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sensitivity for pan by the left and right keys.
    /// </summary>
    /// <value>
    ///     The pan sensitivity.
    /// </value>
    /// <remarks>
    ///     Use -1 to invert the pan direction.
    /// </remarks>
    public double LeftRightPanSensitivity {
        get => (double)GetValue(LeftRightPanSensitivityProperty);

        set => SetValue(LeftRightPanSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sensitivity for rotation by the left and right keys.
    /// </summary>
    /// <value>
    ///     The rotation sensitivity.
    /// </value>
    /// <remarks>
    ///     Use -1 to invert the rotation direction.
    /// </remarks>
    public double LeftRightRotationSensitivity {
        get => (double)GetValue(LeftRightRotationSensitivityProperty);

        set => SetValue(LeftRightRotationSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets the maximum field of view.
    /// </summary>
    /// <value>
    ///     The maximum field of view.
    /// </value>
    public double MaximumFieldOfView {
        get => (double)GetValue(MaximumFieldOfViewProperty);

        set => SetValue(MaximumFieldOfViewProperty, value);
    }

    /// <summary>
    ///     Gets or sets the minimum field of view.
    /// </summary>
    /// <value>
    ///     The minimum field of view.
    /// </value>
    public double MinimumFieldOfView {
        get => (double)GetValue(MinimumFieldOfViewProperty);

        set => SetValue(MinimumFieldOfViewProperty, value);
    }

    /// <summary>
    ///     Gets or sets the model up direction.
    /// </summary>
    /// <value>
    ///     The model up direction.
    /// </value>
    public Vector3D ModelUpDirection {
        get => (Vector3D)GetValue(ModelUpDirectionProperty);

        set => SetValue(ModelUpDirectionProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether an orthographic camera should be used.
    /// </summary>
    /// <value>
    ///     <c>true</c> if orthographic; otherwise, <c>false</c> .
    /// </value>
    public bool Orthographic {
        get => (bool)GetValue(OrthographicProperty);

        set => SetValue(OrthographicProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sensitivity for zoom by the page up and page down keys.
    /// </summary>
    /// <value>
    ///     The zoom sensitivity.
    /// </value>
    /// <remarks>
    ///     Use -1 to invert the zoom direction.
    /// </remarks>
    public double PageUpDownZoomSensitivity {
        get => (double)GetValue(PageUpDownZoomSensitivityProperty);

        set => SetValue(PageUpDownZoomSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets the pan cursor.
    /// </summary>
    /// <value>
    ///     The pan cursor.
    /// </value>
    public Cursor PanCursor {
        get => (Cursor)GetValue(PanCursorProperty);

        set => SetValue(PanCursorProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to rotate around the mouse down point.
    /// </summary>
    /// <value>
    ///     <c>true</c> if rotating around mouse down point; otherwise, <c>false</c>.
    /// </value>
    public bool RotateAroundMouseDownPoint {
        get => (bool)GetValue(RotateAroundMouseDownPointProperty);

        set => SetValue(RotateAroundMouseDownPointProperty, value);
    }

    /// <summary>
    ///     Gets or sets the rotate cursor.
    /// </summary>
    /// <value>
    ///     The rotate cursor.
    /// </value>
    public Cursor RotateCursor {
        get => (Cursor)GetValue(RotateCursorProperty);

        set => SetValue(RotateCursorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the rotation sensitivity.
    /// </summary>
    /// <value>
    ///     The rotation sensitivity.
    /// </value>
    public double RotationSensitivity {
        get => (double)GetValue(RotationSensitivityProperty);

        set => SetValue(RotationSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show camera info.
    /// </summary>
    /// <value>
    ///     <c>true</c> if camera info should be shown; otherwise, <c>false</c> .
    /// </value>
    public bool ShowCameraInfo {
        get => (bool)GetValue(ShowCameraInfoProperty);

        set => SetValue(ShowCameraInfoProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show the camera target adorner.
    /// </summary>
    /// <value>
    ///     <c>true</c> if camera target should be shown; otherwise, <c>false</c> .
    /// </value>
    public bool ShowCameraTarget {
        get => (bool)GetValue(ShowCameraTargetProperty);

        set => SetValue(ShowCameraTargetProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show the coordinate system.
    /// </summary>
    /// <value>
    ///     <c>true</c> if coordinate system should be shown; otherwise, <c>false</c> .
    /// </value>
    public bool ShowCoordinateSystem {
        get => (bool)GetValue(ShowCoordinateSystemProperty);

        set => SetValue(ShowCoordinateSystemProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show frame rate.
    /// </summary>
    /// <value>
    ///     <c>true</c> if frame rate should be shown; otherwise, <c>false</c> .
    /// </value>
    public bool ShowFrameRate {
        get => (bool)GetValue(ShowFrameRateProperty);

        set => SetValue(ShowFrameRateProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show the total number of triangles in the scene.
    /// </summary>
    public bool ShowTriangleCountInfo {
        get => (bool)GetValue(ShowTriangleCountInfoProperty);

        set => SetValue(ShowTriangleCountInfoProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to show the view cube.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the view cube should be shown; otherwise, <c>false</c> .
    /// </value>
    public bool ShowViewCube {
        get => (bool)GetValue(ShowViewCubeProperty);

        set => SetValue(ShowViewCubeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the spin release time in milliseconds (maximum allowed time to start a spin).
    /// </summary>
    /// <value>
    ///     The spin release time (in milliseconds).
    /// </value>
    public int SpinReleaseTime {
        get => (int)GetValue(SpinReleaseTimeProperty);

        set => SetValue(SpinReleaseTimeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sub title.
    /// </summary>
    /// <value>
    ///     The sub title.
    /// </value>
    public string SubTitle {
        get => (string)GetValue(SubTitleProperty);

        set => SetValue(SubTitleProperty, value);
    }

    /// <summary>
    ///     Gets or sets the size of the sub title.
    /// </summary>
    /// <value>
    ///     The size of the sub title.
    /// </value>
    public double SubTitleSize {
        get => (double)GetValue(SubTitleSizeProperty);

        set => SetValue(SubTitleSizeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the text brush.
    /// </summary>
    /// <value>
    ///     The text brush.
    /// </value>
    public WpfBrush TextBrush {
        get => (WpfBrush)GetValue(TextBrushProperty);

        set => SetValue(TextBrushProperty, value);
    }

    /// <summary>
    ///     Gets or sets the title.
    /// </summary>
    /// <value>
    ///     The title.
    /// </value>
    public string Title {
        get => (string)GetValue(TitleProperty);

        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    ///     Gets or sets the title background brush.
    /// </summary>
    /// <value>
    ///     The title background.
    /// </value>
    public WpfBrush TitleBackground {
        get => (WpfBrush)GetValue(TitleBackgroundProperty);

        set => SetValue(TitleBackgroundProperty, value);
    }

    /// <summary>
    ///     Gets or sets the title font family.
    /// </summary>
    /// <value>
    ///     The title font family.
    /// </value>
    public string TitleFontFamily {
        get => (string)GetValue(TitleFontFamilyProperty);

        set => SetValue(TitleFontFamilyProperty, value);
    }

    /// <summary>
    ///     Gets or sets the size of the title.
    /// </summary>
    /// <value>
    ///     The size of the title.
    /// </value>
    public double TitleSize {
        get => (double)GetValue(TitleSizeProperty);

        set => SetValue(TitleSizeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sensitivity for pan by the up and down keys.
    /// </summary>
    /// <value>
    ///     The pan sensitivity.
    /// </value>
    /// <remarks>
    ///     Use -1 to invert the pan direction.
    /// </remarks>
    public double UpDownPanSensitivity {
        get => (double)GetValue(UpDownPanSensitivityProperty);

        set => SetValue(UpDownPanSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sensitivity for rotation by the up and down keys.
    /// </summary>
    /// <value>
    ///     The rotation sensitivity.
    /// </value>
    /// <remarks>
    ///     Use -1 to invert the rotation direction.
    /// </remarks>
    public double UpDownRotationSensitivity {
        get => (double)GetValue(UpDownRotationSensitivityProperty);

        set => SetValue(UpDownRotationSensitivityProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to use default mouse/keyboard gestures.
    /// </summary>
    /// <value>
    ///     <c>true</c> if default gestures should be used; otherwise, <c>false</c>.
    /// </value>
    public bool UseDefaultGestures {
        get => (bool)GetValue(UseDefaultGesturesProperty);

        set => SetValue(UseDefaultGesturesProperty, value);
    }

    /// <summary>
    ///     Gets or sets the view cube texture;
    ///     The view cube texture. It must be a 6x1 (ex: 600x100) ratio image. You can also use
    ///     BitmapExtension.CreateViewBoxBitmapSource to create
    /// </summary>
    /// <value>
    ///     The view cube texture.
    /// </value>
    public TextureModel ViewCubeTexture {
        get => (TextureModel)GetValue(ViewCubeTextureProperty);

        set => SetValue(ViewCubeTextureProperty, value);
    }

    /// <summary>
    ///     Gets or sets the horizontal position of the view cube viewport. Relative to viewport center
    ///     <para>Default: 0.8</para>
    /// </summary>
    /// <value>
    ///     The horizontal position.
    /// </value>
    public double ViewCubeHorizontalPosition {
        get => (double)GetValue(ViewCubeHorizontalPositionProperty);

        set => SetValue(ViewCubeHorizontalPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets if the view cube edge clickable.
    /// </summary>
    /// <value>
    ///     Boolean for enable or disable.
    /// </value>
    public bool IsViewCubeEdgeClicksEnabled {
        get => (bool)GetValue(IsViewCubeEdgeClicksEnabledProperty);
        set => SetValue(IsViewCubeEdgeClicksEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is view cube mover enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is view cube mover enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsViewCubeMoverEnabled {
        get => (bool)GetValue(IsViewCubeMoverEnabledProperty);
        set => SetValue(IsViewCubeMoverEnabledProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether coordinate system mover enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if coordinate system mover enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsCoordinateSystemMoverEnabled {
        get => (bool)GetValue(IsCoordinateSystemMoverEnabledProperty);
        set => SetValue(IsCoordinateSystemMoverEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets the vertical position of view cube viewport. Relative to viewport center
    ///     <para>Default: -0.8</para>
    /// </summary>
    /// <value>
    ///     The vertical position.
    /// </value>
    public double ViewCubeVerticalPosition {
        get => (double)GetValue(ViewCubeVerticalPositionProperty);

        set => SetValue(ViewCubeVerticalPositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the width of the view cube viewport.
    /// </summary>
    /// <value>
    ///     The width of the view cube viewport.
    /// </value>
    public double ViewCubeSize {
        get => (double)GetValue(ViewCubeSizeProperty);

        set => SetValue(ViewCubeSizeProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to zoom around the mouse down point.
    /// </summary>
    /// <value>
    ///     <c>true</c> if zooming around the mouse down point; otherwise, <c>false</c>.
    /// </value>
    public bool ZoomAroundMouseDownPoint {
        get => (bool)GetValue(ZoomAroundMouseDownPointProperty);

        set => SetValue(ZoomAroundMouseDownPointProperty, value);
    }

    /// <summary>
    ///     Gets or sets the zoom cursor.
    /// </summary>
    /// <value>
    ///     The zoom cursor.
    /// </value>
    public Cursor ZoomCursor {
        get => (Cursor)GetValue(ZoomCursorProperty);

        set => SetValue(ZoomCursorProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating the far distance limit for zoom.
    /// </summary>
    public double ZoomDistanceLimitFar {
        get => (double)GetValue(ZoomDistanceLimitFarProperty);

        set => SetValue(ZoomDistanceLimitFarProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating the near distance limit for zoom.
    /// </summary>
    public double ZoomDistanceLimitNear {
        get => (double)GetValue(ZoomDistanceLimitNearProperty);

        set => SetValue(ZoomDistanceLimitNearProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether to Zoom extents when the control has loaded.
    /// </summary>
    public bool ZoomExtentsWhenLoaded {
        get => (bool)GetValue(ZoomExtentsWhenLoadedProperty);

        set => SetValue(ZoomExtentsWhenLoadedProperty, value);
    }

    /// <summary>
    ///     Gets or sets the zoom rectangle cursor.
    /// </summary>
    /// <value>
    ///     The zoom rectangle cursor.
    /// </value>
    public Cursor ZoomRectangleCursor {
        get => (Cursor)GetValue(ZoomRectangleCursorProperty);

        set => SetValue(ZoomRectangleCursorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the zoom sensitivity.
    /// </summary>
    /// <value>
    ///     The zoom sensitivity.
    /// </value>
    public double ZoomSensitivity {
        get => (double)GetValue(ZoomSensitivityProperty);

        set => SetValue(ZoomSensitivityProperty, value);
    }

    /// <summary>
    ///     Set MSAA level. If set to Two/Four/Eight, the actual level is set to minimum between Maximum and Two/Four/Eight
    /// </summary>
    public MsaaLevel Msaa {
        get => (MsaaLevel)GetValue(MsaaProperty);
        set => SetValue(MsaaProperty, value);
    }
    /// <summary>
    ///     Rotate around this fixed rotation point only.<see cref="FixedRotationPointEnabled" />
    /// </summary>
    public Point3D FixedRotationPoint {
        get => (Point3D)GetValue(FixedRotationPointProperty);
        set => SetValue(FixedRotationPointProperty, value);
    }

    /// <summary>
    ///     Enable fixed rotation mode and use <see cref="FixedRotationPoint" />  for rotation. Only works under
    ///     <see cref="CameraMode" /> = Inspect
    /// </summary>
    public bool FixedRotationPointEnabled {
        get => (bool)GetValue(FixedRotationPointEnabledProperty);
        set => SetValue(FixedRotationPointEnabledProperty, value);
    }

    /// <summary>
    ///     Enable mouse button hit test
    /// </summary>
    public bool EnableMouseButtonHitTest {
        get => (bool)GetValue(EnableMouseButtonHitTestProperty);
        set => SetValue(EnableMouseButtonHitTestProperty, value);
    }

    /// <summary>
    ///     Manually move camera to look at a point in 3D space. (Same as calling Viewport3DX.LookAt() function)
    ///     Since camera may have been moved by mouse, the value gets does not reflect the actual point camera currently
    ///     looking at.
    /// </summary>
    public Point3D ManualLookAtPoint {
        get => (Point3D)GetValue(ManualLookAtPointProperty);
        set => SetValue(ManualLookAtPointProperty, value);
    }

    /// <summary>
    ///     Enable render frustum to skip rendering model if model is out of the camera bounding frustum
    /// </summary>
    public bool EnableRenderFrustum {
        get => (bool)GetValue(EnableRenderFrustumProperty);
        set => SetValue(EnableRenderFrustumProperty, value);
    }

    /// <summary>
    ///     <para>Enable deferred rendering. Use multithreading to call rendering procedure using different Deferred Context.</para>
    ///     <para>Deferred Rendering: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476892.aspx</para>
    ///     <para>https://docs.nvidia.com/gameworks/content/gameworkslibrary/graphicssamples/d3d_samples/d3d11deferredcontextssample.htm</para>
    ///     <para>Note: Only if draw calls > 3000 to be benefit according to the online performance test.</para>
    /// </summary>
    public bool EnableDeferredRendering {
        get => (bool)GetValue(EnableDeferredRenderingProperty);
        set => SetValue(EnableDeferredRenderingProperty, value);
    }

    /// <summary>
    ///     Used to create multiple viewport with shared models.
    /// </summary>
    public bool EnableSharedModelMode {
        get => (bool)GetValue(EnableSharedModelModeProperty);
        set => SetValue(EnableSharedModelModeProperty, value);
    }

    /// <summary>
    ///     Binding to the element inherit with <see cref="IModelContainer" />
    /// </summary>
    public IModelContainer SharedModelContainer {
        get => (IModelContainer)GetValue(SharedModelContainerProperty);
        set => SetValue(SharedModelContainerProperty, value);
    }

    /// <summary>
    ///     Gets or sets the shared model container internal.
    /// </summary>
    /// <value>
    ///     The shared model container internal.
    /// </value>
    protected IModelContainer? SharedModelContainerInternal { get; private set; }

    /// <summary>
    ///     <para>Use HwndHost as rendering surface, swapchain for rendering. Much faster than using D3DImage.</para>
    ///     <para>
    ///         Drawbacks: The rendering surface will cover all WPF controls in the same Viewport region. Move controls out
    ///         of viewport region to solve this problem.
    ///     </para>
    ///     <para>
    ///         For displaying ViewCube and CoordinateSystem, separate Model needs to create to render along with the other
    ///         models. WPF viewport will not be visibled.
    ///     </para>
    ///     <para>Note: Enable deferred rendering will use seperate rendering thread or rendering.</para>
    /// </summary>
    public bool EnableSwapChainRendering {
        get => (bool)GetValue(EnableSwapChainRenderingProperty);
        set => SetValue(EnableSwapChainRenderingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the content2d.
    /// </summary>
    /// <value>
    ///     The content2 d.
    /// </value>
    public Model.Elements2D.Abstract.Element2D? Content2D {
        get => (Model.Elements2D.Abstract.Element2D?)GetValue(Content2DProperty);
        set => SetValue(Content2DProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [show frame details].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [show frame details]; otherwise, <c>false</c>.
    /// </value>
    public bool ShowFrameDetails {
        get => (bool)GetValue(ShowFrameDetailsProperty);
        set => SetValue(ShowFrameDetailsProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable direct2D rendering]. Default is On
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render d2d]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableD2DRendering {
        get => (bool)GetValue(EnableD2DRenderingProperty);
        set => SetValue(EnableD2DRenderingProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable automatic update octree for geometry models].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable automatic octree update]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableAutoOctreeUpdate {
        get => (bool)GetValue(EnableAutoOctreeUpdateProperty);
        set => SetValue(EnableAutoOctreeUpdateProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether move is enabled.
    /// </summary>
    /// <value> <c>true</c> if move is enabled; otherwise, <c>false</c> . </value>
    public bool IsMoveEnabled {
        get => (bool)GetValue(IsMoveEnabledProperty);

        set => SetValue(IsMoveEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating the render mode for Transparent objects.
    ///     <see cref="MaterialGeometryModel3D.IsTransparent" />, <see cref="BillboardTextModel3D.IsTransparent" />
    /// </summary>
    public OitRenderType OitRenderMode {
        get => (OitRenderType)GetValue(OitRenderModeProperty);
        set => SetValue(OitRenderModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the oit weight depth slope. Used to increase resolution for particular range of depth values.
    ///     <para>
    ///         If value = 2, the depth range from 0-0.5 expands to 0-1 to increase resolution. However, values from 0.5 - 1
    ///         will be pushed to 1
    ///     </para>
    /// </summary>
    /// <value>
    ///     The oit weight depth slope.
    /// </value>
    public double OitWeightDepthSlope {
        get => (double)GetValue(OitWeightDepthSlopeProperty);
        set => SetValue(OitWeightDepthSlopeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the Order independent transparent rendering color weight power.
    ///     Used for color weight calculation.
    ///     <para>Different near field/far field settings may need different power value for z value based weight calculation.</para>
    /// </summary>
    /// <value>
    ///     The oit weight power.
    /// </value>
    public double OitWeightPower {
        get => (double)GetValue(OitWeightPowerProperty);
        set => SetValue(OitWeightPowerProperty, value);
    }

    /// <summary>
    ///     Gets or sets the oit weight mode.
    ///     <para>Please refer to http://jcgt.org/published/0002/02/09/ </para>
    ///     <para>Linear0: eq7; Linear1: eq8; Linear2: eq9; NonLinear: eq10</para>
    /// </summary>
    /// <value>
    ///     The oit weight mode.
    /// </value>
    public OitWeightMode OitWeightMode {
        get => (OitWeightMode)GetValue(OitWeightModeProperty);
        set => SetValue(OitWeightModeProperty, value);
    }

    public int OitDepthPeelingIteration {
        get => (int)GetValue(OitDepthPeelingIterationProperty);
        set => SetValue(OitDepthPeelingIterationProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fxaa. If MSAA is set, FXAA will be disabled automatically
    /// </summary>
    /// <value>
    ///     The enable fxaa.
    /// </value>
    public FxaaLevel FxaaLevel {
        get => (FxaaLevel)GetValue(FxaaLevelProperty);
        set => SetValue(FxaaLevelProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable design time rendering].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable design time rendering]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDesignModeRendering {
        get => (bool)GetValue(EnableDesignModeRenderingProperty);
        set => SetValue(EnableDesignModeRenderingProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable render order].
    ///     Specify render order in <see cref="Element3D.RenderOrder" />.
    ///     Scene node will be sorted by the <see cref="Element3D.RenderOrder" /> during rendering.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable manual render order]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableRenderOrder {
        get => (bool)GetValue(EnableRenderOrderProperty);
        set => SetValue(EnableRenderOrderProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether [enable ScreenSpaced Ambient Occlusion].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable ssao]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableSsao {
        get => (bool)GetValue(EnableSsaoProperty);
        set => SetValue(EnableSsaoProperty, value);
    }

    /// <summary>
    ///     Gets or sets the ssao sampling radius.
    /// </summary>
    /// <value>
    ///     The ssao sampling radius.
    /// </value>
    public double SsaoSamplingRadius {
        get => (double)GetValue(SsaoSamplingRadiusProperty);
        set => SetValue(SsaoSamplingRadiusProperty, value);
    }

    /// <summary>
    ///     Gets or sets the ssao intensity.
    /// </summary>
    /// <value>
    ///     The ssao intensity.
    /// </value>
    public double SsaoIntensity {
        get => (double)GetValue(SsaoIntensityProperty);
        set => SetValue(SsaoIntensityProperty, value);
    }


    /// <summary>
    ///     Gets or sets the ssao quality.
    /// </summary>
    /// <value>
    ///     The ssao quality.
    /// </value>
    public SsaoQuality SsaoQuality {
        get => (SsaoQuality)GetValue(SsaoQualityProperty);
        set => SetValue(SsaoQualityProperty, value);
    }

    /// <summary>
    ///     The update count. Used to render at least N frames for each InvalidateRenderer.
    ///     D3DImage sometimes not getting refresh if only render once.
    ///     Default = 6.
    /// </summary>
    /// <value>
    ///     The minimum update count.
    /// </value>
    public int MinimumUpdateCount {
        get => (int)GetValue(MinimumUpdateCountProperty);
        set => SetValue(MinimumUpdateCountProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether globally [allow up down rotation].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [allow up down rotation]; otherwise, <c>false</c>.
    /// </value>
    public bool AllowUpDownRotation {
        get => (bool)GetValue(AllowUpDownRotationProperty);
        set => SetValue(AllowUpDownRotationProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether globally [allow left right rotation].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [allow left right rotation]; otherwise, <c>false</c>.
    /// </value>
    public bool AllowLeftRightRotation {
        get => (bool)GetValue(AllowLeftRightRotationProperty);
        set => SetValue(AllowLeftRightRotationProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating if the life cycle of the viewport
    ///     depends on the first parent window found in the actual visual tree.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the viewport belongs to the first parent window; otherwise, <c>false</c>
    /// </value>
    public bool BelongsToParentWindow {
        get => (bool)GetValue(BelongsToParentWindowProperty);
        set => SetValue(BelongsToParentWindowProperty, value);
    }

    /// <summary>
    ///     Gets or sets the dpi scale. For example, if dpi scale is set to 200% in windows, this value must be set to 2.
    /// </summary>
    /// <value>
    ///     The dpi scale.
    /// </value>
    public double DpiScale {
        get => (double)GetValue(DpiScaleProperty);
        set => SetValue(DpiScaleProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable dpi scale].
    ///     Enable this option if you want to render high definition image with using high definition monitor and using dpi
    ///     scaling in windows.
    ///     This option may impact rendering performance due to higher resolution.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable dpi scale]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDpiScale {
        get => (bool)GetValue(EnableDpiScaleProperty);
        set => SetValue(EnableDpiScaleProperty, value);
    }

    /// <summary>
    ///     Increase swapchain fps by speed up the wpf composition target frame rate.
    ///     This may negatively impact the performance on low end graphics card.
    ///     Default is enabled.
    /// </summary>
    public bool IncreaseSwapchainFps {
        get => (bool)GetValue(IncreaseSwapchainFpsProperty);
        set => SetValue(IncreaseSwapchainFpsProperty, value);
    }

    /// <summary>
    ///     Gets or sets the <see cref="IEffectsManager" />.
    /// </summary>
    public IEffectsManager? EffectsManager {
        get => (IEffectsManager?)GetValue(EffectsManagerProperty);
        set => SetValue(EffectsManagerProperty, value);
    }

    ///// <summary>
    ///// Gets or sets a value indicating whether deferred shading is used
    ///// </summary>
    ///// <value>
    ///// <c>true</c> if deferred shading is enabled; otherwise, <c>false</c>.
    ///// </value>
    //public bool IsDeferredShadingEnabled
    //{
    //    get { return (bool)this.GetValue(IsDeferredShadingEnabledProperty); }
    //    set { this.SetValue(IsDeferredShadingEnabledProperty, value); }
    //}

    /// <summary>
    ///     Gets or sets a value indicating whether shadow mapping is enabled
    /// </summary>
    /// <value>
    ///     <c>true</c> if deferred shading is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsShadowMappingEnabled {
        get => (bool)GetValue(IsShadowMappingEnabledProperty);
        set => SetValue(IsShadowMappingEnabledProperty, value);
    }

    /// <summary>
    ///     Provide CLR accessors for the event
    /// </summary>
    public event RoutedEventHandler MouseDown3D {
        add => AddHandler(MouseDown3DEvent, value);
        remove => RemoveHandler(MouseDown3DEvent, value);
    }

    /// <summary>
    ///     Provide CLR accessors for the event
    /// </summary>
    public event RoutedEventHandler MouseUp3D {
        add => AddHandler(MouseUp3DEvent, value);
        remove => RemoveHandler(MouseUp3DEvent, value);
    }

    /// <summary>
    ///     Provide CLR accessors for the event
    /// </summary>
    public event RoutedEventHandler MouseMove3D {
        add => AddHandler(MouseMove3DEvent, value);
        remove => RemoveHandler(MouseMove3DEvent, value);
    }

    /// <summary>
    ///     Occurs when [form mouse move].
    /// </summary>
    public event WinformHostExtend.FormMouseMoveEventHandler FormMouseMove {
        add => AddHandler(WinformHostExtend.FormMouseMoveEvent, value);
        remove => RemoveHandler(WinformHostExtend.FormMouseMoveEvent, value);
    }

    /// <summary>
    ///     Occurs when [form mouse wheel].
    /// </summary>
    public event WinformHostExtend.FormMouseWheelEventHandler FormMouseWheel {
        add => AddHandler(WinformHostExtend.FormMouseWheelEvent, value);
        remove => RemoveHandler(WinformHostExtend.FormMouseWheelEvent, value);
    }

    /// <summary>
    ///     Event when a property has been changed
    /// </summary>
    public event RoutedEventHandler CameraChanged {
        add => AddHandler(CameraChangedEvent, value);

        remove => RemoveHandler(CameraChangedEvent, value);
    }
}
