// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CameraController.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a control that manipulates the camera by mouse and keyboard gestures.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Provides a control that manipulates the camera by mouse and keyboard gestures.
/// </summary>
public class CameraController {
    private static readonly Point PointZero = new(0, 0);
    private static readonly Vector2 VectorZero = new();
    private static readonly Vector3 Vector3DZero = new();

    /// <summary>
    ///     The camera history stack.
    /// </summary>
    /// <remarks>
    ///     Implemented as a list since we want to remove items at the bottom of the stack.
    /// </remarks>
    private readonly SimpleRingBuffer<CameraSetting> cameraHistory = new(100);

    private Camera actualCamera;

    /// <summary>
    ///     Decides if combined manipulation is allowed.
    /// </summary>
    private bool allowCombinedManipulation;

    /// <summary>
    ///     Gets or sets CameraMode.
    /// </summary>
    public CameraMode CameraMode = CameraMode.Inspect;

    /// <summary>
    ///     Gets or sets CameraRotationMode.
    /// </summary>
    public CameraRotationMode CameraRotationMode = CameraRotationMode.Turntable;

    /// <summary>
    ///     Gets or sets the change field of view cursor.
    /// </summary>
    /// <value> The change field of view cursor. </value>
    public Cursor ChangeFieldOfViewCursor = Cursors.ScrollNS;

    /// <summary>
    ///     The change field of view event handler.
    /// </summary>
    internal ZoomHandler ChangeFieldOfViewHandler;

    /// <summary>
    ///     The change look at event handler.
    /// </summary>
    internal RotateHandler ChangeLookAtHandler;

    /// <summary>
    ///     Gets or sets the default camera (used when resetting the view).
    /// </summary>
    /// <value> The default camera. </value>
    public ProjectionCamera? DefaultCamera;

    internal int Height;

    /// <summary>
    ///     Gets or sets InertiaFactor.
    /// </summary>
    public double InertiaFactor = 0.93;

    /// <summary>
    ///     Gets or sets a value indicating whether InfiniteSpin.
    /// </summary>
    public bool InfiniteSpin = false;

    /// <summary>
    ///     Gets or sets a value indicating whether field of view can be changed.
    /// </summary>
    public bool IsChangeFieldOfViewEnabled = true;

    /// <summary>
    ///     Gets or sets a value indicating whether inertia is enabled for the camera manipulations.
    /// </summary>
    /// <value><c>true</c> if inertia is enabled; otherwise, <c>false</c>.</value>
    public bool IsInertiaEnabled = true;

    /// <summary>
    ///     Gets or sets a value indicating whether move is enabled.
    /// </summary>
    /// <value> <c>true</c> if move is enabled; otherwise, <c>false</c> . </value>
    public bool IsMoveEnabled = true;

    /// <summary>
    ///     Gets or sets a value indicating whether pan is enabled.
    /// </summary>
    public bool IsPanEnabled = true;

    /// <summary>
    ///     Gets or sets a value indicating whether IsRotationEnabled.
    /// </summary>
    public bool IsRotationEnabled = true;

    /// <summary>
    ///     The is spinning flag.
    /// </summary>
    private bool isSpinning;

    /// <summary>
    ///     Gets or sets a value indicating whether IsZoomEnabled.
    /// </summary>
    public bool IsZoomEnabled = true;

    /// <summary>
    ///     The last tick.
    /// </summary>
    private long lastTick;

    /// <summary>
    ///     Gets or sets the sensitivity for pan by the left and right keys.
    /// </summary>
    /// <value> The pan sensitivity. </value>
    /// <remarks>
    ///     Use -1 to invert the pan direction.
    /// </remarks>
    public double LeftRightPanSensitivity = 1.0;

    /// <summary>
    ///     Gets or sets the sensitivity for rotation by the left and right keys.
    /// </summary>
    /// <value> The rotation sensitivity. </value>
    /// <remarks>
    ///     Use -1 to invert the rotation direction.
    /// </remarks>
    public double LeftRightRotationSensitivity = 1.0;

    /// <summary>
    ///     The number of touch manipulators (fingers) in the last touch delta event
    /// </summary>
    private int manipulatorCount;

    /// <summary>
    ///     Gets or sets the maximum field of view.
    /// </summary>
    /// <value> The maximum field of view. </value>
    public double MaximumFieldOfView = 120.0;

    /// <summary>
    ///     Gets or sets the minimum field of view.
    /// </summary>
    /// <value> The minimum field of view. </value>
    public double MinimumFieldOfView = 10.0;

    /// <summary>
    ///     Gets or sets the model up direction.
    /// </summary>
    public Vector3 ModelUpDirection = new(0, 1, 0);

    /// <summary>
    ///     Gets or sets the move sensitivity.
    /// </summary>
    /// <value> The move sensitivity. </value>
    public double MoveSensitivity = 1.0;

    /// <summary>
    ///     The move speed.
    /// </summary>
    private Vector3 moveSpeed;

    /// <summary>
    ///     Gets or sets the sensitivity for zoom by the page up and page down keys.
    /// </summary>
    /// <value> The zoom sensitivity. </value>
    /// <remarks>
    ///     Use -1 to invert the zoom direction.
    /// </remarks>
    public double PageUpDownZoomSensitivity = 1.0;

    /// <summary>
    ///     Gets or sets the pan cursor.
    /// </summary>
    /// <value> The pan cursor. </value>
    public Cursor PanCursor = Cursors.Hand;

    /// <summary>
    ///     The number of fingers used for panning.
    /// </summary>
    private int panFingerCount;

    /// <summary>
    ///     The pan event handler.
    /// </summary>
    internal PanHandler PanHandler;

    /// <summary>
    ///     The pan speed.
    /// </summary>
    private Vector3 panSpeed;

    private double prevScale = 1;

    /// <summary>
    ///     Gets or sets a value indicating whether to rotate around the mouse down point.
    /// </summary>
    /// <value> <c>true</c> if rotation around the mouse down point is enabled; otherwise, <c>false</c> . </value>
    public bool RotateAroundMouseDownPoint = false;

    /// <summary>
    ///     Gets or sets the rotate cursor.
    /// </summary>
    /// <value> The rotate cursor. </value>
    public Cursor RotateCursor = Cursors.SizeAll;

    /// <summary>
    ///     The number of fingers used for rotating.
    /// </summary>
    private int rotateFingerCount;

    /// <summary>
    ///     The rotation event handler.
    /// </summary>
    internal RotateHandler RotateHandler;

    /// <summary>
    ///     The 3D rotation point.
    /// </summary>
    private Vector3 rotationPoint3D;

    /// <summary>
    ///     The rotation position.
    /// </summary>
    private Vector2 rotationPosition;

    /// <summary>
    ///     Gets or sets the rotation sensitivity (degrees/pixel).
    /// </summary>
    /// <value> The rotation sensitivity. </value>
    public double RotationSensitivity = 1.0;

    /// <summary>
    ///     The rotation speed.
    /// </summary>
    private Vector2 rotationSpeed;

    /// <summary>
    ///     The set target handler
    /// </summary>
    internal RotateHandler SetTargetHandler;

    /// <summary>
    ///     Gets or sets a value indicating whether to show a target adorner when manipulating the camera.
    /// </summary>
    public bool ShowCameraTarget = true;

    /// <summary>
    ///     The 3D point to spin around.
    /// </summary>
    private Vector3 spinningPoint3D;

    /// <summary>
    ///     The spinning position.
    /// </summary>
    private Vector2 spinningPosition;

    /// <summary>
    ///     The spinning speed.
    /// </summary>
    private Vector2 spinningSpeed;

    /// <summary>
    ///     Gets or sets the max duration of mouse drag to activate spin.
    /// </summary>
    /// <remarks>
    ///     If the time between mouse down and mouse up is less than this value, spin is activated.
    /// </remarks>
    public int SpinReleaseTime = 200;

    /// <summary>
    ///     The touch point in the last touch delta event
    /// </summary>
    private Point touchPreviousPoint;

    /// <summary>
    ///     Gets or sets the sensitivity for pan by the up and down keys.
    /// </summary>
    /// <value> The pan sensitivity. </value>
    /// <remarks>
    ///     Use -1 to invert the pan direction.
    /// </remarks>
    public double UpDownPanSensitivity = 1.0;

    /// <summary>
    ///     Gets or sets the sensitivity for rotation by the up and down keys.
    /// </summary>
    /// <value> The rotation sensitivity. </value>
    /// <remarks>
    ///     Use -1 to invert the rotation direction.
    /// </remarks>
    public double UpDownRotationSensitivity = 1.0;

    internal int Width;

    /// <summary>
    ///     Gets or sets a value indicating whether to zoom around mouse down point.
    /// </summary>
    /// <value> <c>true</c> if zooming around the mouse down point is enabled; otherwise, <c>false</c> . </value>
    public bool ZoomAroundMouseDownPoint = false;

    /// <summary>
    ///     Gets or sets the zoom cursor.
    /// </summary>
    /// <value> The zoom cursor. </value>
    public Cursor ZoomCursor = Cursors.SizeNS;

    /// <summary>
    ///     Gets or sets the zoom distance limit far.
    /// </summary>
    /// <value>
    ///     The zoom distance limit far.
    /// </value>
    public double ZoomDistanceLimitFar = double.PositiveInfinity;

    /// <summary>
    ///     Gets or sets the zoom distance limit near.
    /// </summary>
    /// <value>
    ///     The zoom distance limit near.
    /// </value>
    public double ZoomDistanceLimitNear = 0.001;

    /// <summary>
    ///     The number of fingers used for zooming.
    /// </summary>
    private int zoomFingerCount;

    /// <summary>
    ///     The zoom event handler.
    /// </summary>
    internal ZoomHandler ZoomHandler;

    /// <summary>
    ///     The point to zoom around.
    /// </summary>
    private Vector3 zoomPoint3D;

    /// <summary>
    ///     Gets or sets the zoom rectangle cursor.
    /// </summary>
    /// <value> The zoom rectangle cursor. </value>
    public Cursor ZoomRectangleCursor = Cursors.SizeNWSE;

    /// <summary>
    ///     The zoom rectangle event handler.
    /// </summary>
    internal ZoomRectangleHandler ZoomRectangleHandler;

    /// <summary>
    ///     Gets or sets ZoomSensitivity.
    /// </summary>
    public double ZoomSensitivity = 1.0;

    /// <summary>
    ///     The zoom speed.
    /// </summary>
    private double zoomSpeed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraController" /> class.
    /// </summary>
    public CameraController(Viewport3DX viewport) {
        Viewport = viewport;
        actualCamera = viewport.Camera;
        ChangeLookAtHandler = new RotateHandler(this, true);
        RotateHandler = new RotateHandler(this);
        ZoomRectangleHandler = new ZoomRectangleHandler(this);
        ZoomHandler = new ZoomHandler(this);
        PanHandler = new PanHandler(this);
        ChangeFieldOfViewHandler = new ZoomHandler(this, true);
        SetTargetHandler = new RotateHandler(this, true);
        MouseHandlers.Add(ChangeLookAtHandler);
        MouseHandlers.Add(RotateHandler);
        MouseHandlers.Add(ZoomRectangleHandler);
        MouseHandlers.Add(ZoomHandler);
        MouseHandlers.Add(PanHandler);
        MouseHandlers.Add(ChangeFieldOfViewHandler);
        Viewport.SizeChanged += (s, e) => {
            Width = (int)e.NewSize.Width;
            Height = (int)e.NewSize.Height;
        };
        Width = (int)viewport.Width;
        Height = (int)viewport.Height;
    }

    /// <summary>
    ///     Records series of mouse down cursor changes. And play back during mouse up.
    /// </summary>
    internal Stack<Cursor> CursorHistory { get; } = new();

    internal List<MouseGestureHandler> MouseHandlers { get; } = [];

    /// <summary>
    ///     Gets ActualCamera.
    /// </summary>
    public Camera ActualCamera {
        get => actualCamera;
        set {
            if (actualCamera != value) {
                actualCamera = value;
                OnCameraChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets CameraLookDirection.
    /// </summary>
    public Vector3 CameraLookDirection {
        get => ActualCamera.CameraInternal.LookDirection;

        set => ActualCamera.LookDirection = value.ToVector3D();
    }

    /// <summary>
    ///     Gets or sets CameraPosition.
    /// </summary>
    public Vector3 CameraPosition {
        get => ActualCamera.CameraInternal.Position;

        set => ActualCamera.Position = value.ToPoint3D();
    }

    /// <summary>
    ///     Gets or sets CameraTarget.
    /// </summary>
    public Vector3 CameraTarget {
        get => CameraPosition + CameraLookDirection;

        set => CameraLookDirection = value - CameraPosition;
    }

    /// <summary>
    ///     Gets or sets CameraUpDirection.
    /// </summary>
    public Vector3 CameraUpDirection {
        get => ActualCamera.CameraInternal.UpDirection;

        set => ActualCamera.UpDirection = value.ToVector3D();
    }

    /// <summary>
    ///     Gets or sets Viewport.
    /// </summary>
    public Viewport3DX Viewport { get; }

    /// <summary>
    ///     Gets a value indicating whether IsOrthographicCamera.
    /// </summary>
    protected bool IsOrthographicCamera => ActualCamera is OrthographicCamera;

    /// <summary>
    ///     Gets a value indicating whether IsPerspectiveCamera.
    /// </summary>
    protected bool IsPerspectiveCamera => ActualCamera is PerspectiveCamera;

    /// <summary>
    ///     Gets OrthographicCamera.
    /// </summary>
    protected OrthographicCamera? OrthographicCamera => ActualCamera as OrthographicCamera;

    /// <summary>
    ///     Gets PerspectiveCamera.
    /// </summary>
    protected PerspectiveCamera? PerspectiveCamera => ActualCamera as PerspectiveCamera;

    /// <summary>
    ///     Gets or sets a value indicating whether [fixed rotation point enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [fixed rotation point enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool FixedRotationPointEnabled { get; set; } = false;

    /// <summary>
    ///     Gets or sets the fixed rotation point.
    /// </summary>
    /// <value>
    ///     The fixed rotation point.
    /// </value>
    public Vector3 FixedRotationPoint { get; set; } = new();

    /// <summary>
    ///     Gets or sets to allow rotate x direction and y direction globally. X, Y is screen space.
    ///     <para>X = 1: Allow left/right rotation. Y = 1: Allow up/down rotation</para>
    ///     <para>Default is (1, 1)</para>
    /// </summary>
    /// <value>
    ///     The allow rotate xy.
    /// </value>
    public Vector2 AllowRotateXy { get; set; } = Vector2.One;

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
    public void AddMoveForce(float dx, float dy, float dz) {
        AddMoveForce(new Vector3(dx, dy, dz));
    }

    /// <summary>
    ///     Adds the specified move force.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    public void AddMoveForce(Vector3 delta) {
        if (!IsMoveEnabled) return;

        PushCameraSetting();
        moveSpeed += delta * 40;
        Viewport.InvalidateRender();
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
    public void AddPanForce(float dx, float dy) {
        AddPanForce(FindPanVector(dx, dy));
    }

    /// <summary>
    ///     The add pan force.
    /// </summary>
    /// <param name="pan">
    ///     The pan.
    /// </param>
    public void AddPanForce(Vector3 pan) {
        if (!IsPanEnabled) return;

        PushCameraSetting();
        if (IsInertiaEnabled)
            panSpeed += pan;
        else
            PanHandler.Pan(pan);
        Viewport.InvalidateRender();
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
    public void AddRotateForce(float dx, float dy) {
        if (!IsRotationEnabled) return;

        PushCameraSetting();
        if (IsInertiaEnabled) {
            rotationPoint3D = CameraTarget;
            rotationPosition = new Vector2((float)Viewport.ActualWidth / 2, (float)Viewport.ActualHeight / 2);
            rotationSpeed.X += dx * 40;
            rotationSpeed.Y += dy * 40;
        } else if (FixedRotationPointEnabled) {
            rotationPosition = new Vector2((float)Viewport.ActualWidth / 2, (float)Viewport.ActualHeight / 2);
            RotateHandler.Rotate(rotationPosition, rotationPosition + new Vector2(dx, dy), FixedRotationPoint);
        } else {
            rotationPosition = new Vector2((float)Viewport.ActualWidth / 2, (float)Viewport.ActualHeight / 2);
            RotateHandler.Rotate(rotationPosition, rotationPosition + new Vector2(dx, dy), CameraTarget);
        }

        Viewport.InvalidateRender();
    }

    /// <summary>
    ///     Adds the zoom force.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    public void AddZoomForce(float delta) {
        AddZoomForce(delta, CameraTarget);
    }

    /// <summary>
    ///     Adds the zoom force.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    /// <param name="zoomOrigin">
    ///     The zoom origin.
    /// </param>
    public void AddZoomForce(float delta, Vector3 zoomOrigin) {
        if (!IsZoomEnabled) return;
        PushCameraSetting();

        if (IsInertiaEnabled) {
            zoomPoint3D = zoomOrigin;
            zoomSpeed += delta * 8;
        } else {
            ZoomHandler.Zoom(delta, zoomOrigin);
        }

        Viewport.InvalidateRender();
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
    public void ChangeDirection(Vector3 lookDir, Vector3 upDir, double animationTime = 500) {
        ChangeDirection(lookDir.ToVector3D(), upDir.ToVector3D(), animationTime);
    }

    /// <summary>
    ///     Changes the direction.
    /// </summary>
    /// <param name="lookDir">The look dir.</param>
    /// <param name="upDir">Up dir.</param>
    /// <param name="animationTime">The animation time.</param>
    public void ChangeDirection(Vector3D lookDir, Vector3D upDir, double animationTime = 500) {
        if (!IsRotationEnabled) return;

        StopAnimations();
        PushCameraSetting();
        ActualCamera.ChangeDirection(lookDir, upDir, animationTime);
    }

    /// <summary>
    ///     Changes the direction of the camera.
    /// </summary>
    /// <param name="lookDir">
    ///     The look direction.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    public void ChangeDirection(Vector3 lookDir, double animationTime = 500) {
        if (!IsRotationEnabled) return;

        StopAnimations();
        PushCameraSetting();
        ActualCamera.ChangeDirection(lookDir.ToVector3D(), ActualCamera.UpDirection, animationTime);
    }

    /// <summary>
    ///     Change the "look-at" point.
    /// </summary>
    /// <param name="target">
    ///     The target.
    /// </param>
    /// <param name="animationTime">
    ///     The animation time.
    /// </param>
    [Obsolete("Use ChangeDirection or the camera's equivalent method.")]
    public void LookAt(Vector3 target, double animationTime) {
        if (!IsPanEnabled) return;

        PushCameraSetting();
        ActualCamera.LookAt(target.ToPoint3D(), animationTime);
    }

    /// <summary>
    ///     Push the current camera settings on an internal stack.
    /// </summary>
    public void PushCameraSetting() {
        cameraHistory.Add(new CameraSetting(ActualCamera));
    }

    /// <summary>
    ///     Resets the camera.
    /// </summary>
    public void ResetCamera() {
        if (!IsZoomEnabled || !IsRotationEnabled || !IsPanEnabled) return;

        PushCameraSetting();
        if (DefaultCamera != null) {
            DefaultCamera.CopyTo(ActualCamera);
        } else {
            ActualCamera.Reset();
            ActualCamera.ZoomExtents(Viewport);
        }
    }

    /// <summary>
    ///     Resets the camera up direction.
    /// </summary>
    public void ResetCameraUpDirection() {
        CameraUpDirection = ModelUpDirection;
    }

    /// <summary>
    ///     Restores the most recent camera setting from the internal stack.
    /// </summary>
    /// <returns> The restore camera setting. </returns>
    public bool RestoreCameraSetting() {
        if (cameraHistory.Count > 0) {
            var cs = cameraHistory.Last;
            cameraHistory.RemoveLast();
            cs.UpdateCamera(ActualCamera);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Starts the spin.
    /// </summary>
    /// <param name="speed">
    ///     The speed.
    /// </param>
    /// <param name="position">
    ///     The position.
    /// </param>
    /// <param name="aroundPoint">
    ///     The spin around point.
    /// </param>
    public void StartSpin(Vector2 speed, Point position, Vector3 aroundPoint) {
        spinningSpeed = speed;
        spinningPosition = position.ToVector2();
        spinningPoint3D = aroundPoint;
        isSpinning = true;
    }

    /// <summary>
    ///     Stops the spin.
    /// </summary>
    public void StopSpin() {
        isSpinning = false;
        spinningSpeed = new Vector2();
    }

    /// <summary>
    ///     Stops the zooming inertia.
    /// </summary>
    public void StopZooming() {
        zoomSpeed = 0;
    }

    /// <summary>
    ///     Stops the panning.
    /// </summary>
    public void StopPanning() {
        panSpeed = Vector3.Zero;
    }

    /// <summary>
    ///     Zooms by the specified delta value.
    /// </summary>
    /// <param name="delta">
    ///     The delta value.
    /// </param>
    public void Zoom(double delta) {
        ZoomHandler.Zoom(delta);
    }

    /// <summary>
    ///     Zooms to the extents of the model.
    /// </summary>
    /// <param name="animationTime">
    ///     The animation time (milliseconds).
    /// </param>
    public void ZoomExtents(double animationTime = 200) {
        if (!IsZoomEnabled) return;

        PushCameraSetting();
        ActualCamera.ZoomExtents(Viewport, animationTime);
    }

    /// <summary>
    ///     Called when the <see cref="E:System.Windows.UIElement.ManipulationCompleted" /> event occurs.
    /// </summary>
    /// <param name="e">
    ///     The data for the event.
    /// </param>
    public void OnManipulationCompleted(ManipulationCompletedEventArgs e) {
        var p = e.ManipulationOrigin + e.TotalManipulation.Translation;

        if (manipulatorCount == rotateFingerCount) RotateHandler.Completed(p);

        if (manipulatorCount == panFingerCount) PanHandler.Completed(p);

        if (manipulatorCount == zoomFingerCount) ZoomHandler.Completed(p);
    }

    /// <summary>
    ///     Called when the <see cref="E:System.Windows.UIElement.ManipulationDelta" /> event occurs.
    /// </summary>
    /// <param name="e">
    ///     The data for the event.
    /// </param>
    public void OnManipulationDelta(ManipulationDeltaEventArgs e) {
        if (!EnablePinchZoom && !EnableThreeFingerPan && !EnableTouchRotate) return;

        // number of manipulators (fingers)
        var n = e.Manipulators.Count();
        var p = e.ManipulationOrigin;
        var position = new Point(touchPreviousPoint.X + e.DeltaManipulation.Translation.X,
                                 touchPreviousPoint.Y + e.DeltaManipulation.Translation.Y);
        touchPreviousPoint = position;

        // http://msdn.microsoft.com/en-us/library/system.windows.uielement.manipulationdelta.aspx

        //// System.Diagnostics.Debug.WriteLine("OnManipulationDelta: T={0}, S={1}, R={2}, O={3}", e.DeltaManipulation.Translation, e.DeltaManipulation.Scale, e.DeltaManipulation.Rotation, e.ManipulationOrigin);
        //// System.Diagnostics.Debug.WriteLine(n + " Delta:" + e.DeltaManipulation.Translation + " Origin:" + e.ManipulationOrigin + " pos:" + position);

        if (manipulatorCount != n) {
            // the number of manipulators has changed

            // cancel old manipulations
            var combine = true;
            if (manipulatorCount == rotateFingerCount) // && combine)
            {
                RotateHandler.Completed(position);
                combine = allowCombinedManipulation;
            }

            if (manipulatorCount == zoomFingerCount && combine) {
                ZoomHandler.Completed(p);
                combine = allowCombinedManipulation;
            }

            if (manipulatorCount == panFingerCount && combine) PanHandler.Completed(position);
            //combine = this.allowCombinedManipulation;
            // start new manipulations
            combine = true;
            if (EnableTouchRotate && n == rotateFingerCount) // && combine)
            {
                RotateHandler.Started(position);
                e.Handled = true;
                combine = allowCombinedManipulation;
            }

            if (EnablePinchZoom && n == zoomFingerCount && combine) {
                ZoomHandler.Started(p);
                e.Handled = true;
                combine = allowCombinedManipulation;
            }

            if (EnableThreeFingerPan && n == panFingerCount && combine) {
                PanHandler.Started(position);
                e.Handled = true;
                //combine = this.allowCombinedManipulation;
            }

            manipulatorCount = n;
            // skip this event, the origin may have changed
        } else {
            if (EnableTouchRotate && n == rotateFingerCount) {
                RotateHandler.Delta(position);
                e.Handled = true;
                if (!allowCombinedManipulation)
                    return;
            }

            if (EnablePinchZoom && n == zoomFingerCount) {
                if (prevScale == 1) {
                    prevScale = e.CumulativeManipulation.Scale.Length;
                } else {
                    if (PinchZoomAtCenter) {
                        var s = e.CumulativeManipulation.Scale.Length;
                        ZoomHandler.Zoom(prevScale - s, CameraPosition + CameraLookDirection, true);
                        prevScale = s;
                    } else {
                        var zoomAroundPoint = ZoomHandler.UnProject(p, ZoomHandler.Origin, CameraLookDirection);
                        if (zoomAroundPoint.HasValue) {
                            var s = e.CumulativeManipulation.Scale.Length;
                            ZoomHandler.Zoom(prevScale - s, zoomAroundPoint.Value, true);
                            prevScale = s;
                        }
                    }
                }

                e.Handled = true;
                if (!allowCombinedManipulation)
                    return;
            }

            if (EnableThreeFingerPan && n == panFingerCount) {
                PanHandler.Delta(position);
                e.Handled = true;
                //if (!this.allowCombinedManipulation) return;
            }
        }
    }

    /// <summary>
    ///     Called when the <see cref="E:System.Windows.UIElement.ManipulationStarted" /> event occurs.
    /// </summary>
    /// <param name="e">
    ///     The data for the event.
    /// </param>
    public void OnManipulationStarted(ManipulationStartedEventArgs e) {
        touchPreviousPoint = e.ManipulationOrigin;
        manipulatorCount = 0;
        prevScale = 1;
        panFingerCount = -1;
        zoomFingerCount = -1;
        rotateFingerCount = -1;
        allowCombinedManipulation = false;

        foreach (var mb in Viewport.InputBindings.OfType<ManipulationBinding>()) {
            allowCombinedManipulation = true;
            if (mb.Command == ViewportCommands.Pan)
                panFingerCount = mb.FingerCount;
            else if (mb.Command == ViewportCommands.Zoom)
                zoomFingerCount = mb.FingerCount;
            else if (mb.Command == ViewportCommands.Rotate) rotateFingerCount = mb.FingerCount;
        }

        if (!allowCombinedManipulation) {
            panFingerCount = 3;
            zoomFingerCount = 2;
            rotateFingerCount = 1;
        }
    }

    /// <summary>
    ///     Invoked when an unhandled MouseDown attached event reaches an element in its route that is derived from this class.
    ///     Implement this method to add class handling for this event.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data
    ///     reports details about the mouse button that was pressed and the handled state.
    /// </param>
    public void OnMouseDown(MouseButtonEventArgs e) {
        if (e.ChangedButton == MouseButton.XButton1) RestoreCameraSetting();
    }

    /// <summary>
    ///     Invoked when an unhandled StylusSystemGesture attached event reaches an element in its route that is derived from
    ///     this class. Implement this method to add class handling for this event.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="T:System.Windows.Input.StylusSystemGestureEventArgs" /> that contains the event data.
    /// </param>
    public void OnStylusSystemGesture(StylusSystemGestureEventArgs e) {
        // Debug.WriteLine("OnStylusSystemGesture: " + e.SystemGesture);
        if (e.SystemGesture == SystemGesture.HoldEnter) {
            var p = e.GetPosition(Viewport);
            ChangeLookAtHandler.Started(p);
            ChangeLookAtHandler.Completed(p);
            e.Handled = true;
        }

        if (e.SystemGesture == SystemGesture.TwoFingerTap) {
            ZoomExtents();
            e.Handled = true;
        }
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
        ChangeDirection(new Vector3(1, 0, 0), new Vector3(0, 0, 1));
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
        ChangeDirection(new Vector3(0, 0, 1), new Vector3(0, -1, 0));
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
    ///     Finds the pan vector.
    /// </summary>
    /// <param name="dx">
    ///     The delta x.
    /// </param>
    /// <param name="dy">
    ///     The delta y.
    /// </param>
    /// <returns>
    ///     The <see cref="Vector3" /> .
    /// </returns>
    private Vector3 FindPanVector(float dx, float dy) {
        var axis1 = SilkMath.Normalize(SilkMath.Cross(CameraLookDirection, CameraUpDirection));
        var axis2 = SilkMath.Normalize(SilkMath.Cross(axis1, CameraLookDirection));
        axis1 *= ActualCamera.CreateLeftHandSystem ? -1 : 1;
        float l = 0;
        if (actualCamera is PerspectiveCamera)
            // this should be dependent on distance to target?
            l = CameraLookDirection.Length;
        else if (actualCamera.CameraInternal is OrthographicCameraCore orth)
            // this should be dependent on width
            l = orth.Width;
        var f = l * 0.001f;
        var move = -axis1 * f * dx + axis2 * f * dy;
        return move;
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
        ChangeDirection(new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
    }

    /// <summary>
    ///     Initializes the input bindings.
    /// </summary>
    private void InitializeBindings() {
        ChangeLookAtHandler = new RotateHandler(this, true);
        RotateHandler = new RotateHandler(this);
        ZoomRectangleHandler = new ZoomRectangleHandler(this);
        ZoomHandler = new ZoomHandler(this);
        PanHandler = new PanHandler(this);
        ChangeFieldOfViewHandler = new ZoomHandler(this, true);
        SetTargetHandler = new RotateHandler(this, true);
        MouseHandlers.Add(ChangeLookAtHandler);
        MouseHandlers.Add(RotateHandler);
        MouseHandlers.Add(ZoomRectangleHandler);
        MouseHandlers.Add(ZoomHandler);
        MouseHandlers.Add(PanHandler);
        MouseHandlers.Add(ChangeFieldOfViewHandler);
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
        ChangeDirection(new Vector3(0, 1, 0), new Vector3(0, 0, 1));
    }

    /// <summary>
    ///     The on camera changed.
    /// </summary>
    private void OnCameraChanged() {
        cameraHistory.Clear();
        PushCameraSetting();
    }

    /// <summary>
    ///     Called when [composition target rendering].
    /// </summary>
    /// <param name="ticks">The ticks.</param>
    public void OnCompositionTargetRendering(long ticks) {
        OnTimeStep(ticks);
    }

    /// <summary>
    ///     Called when a key is pressed.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="System.Windows.Input.KeyEventArgs" /> instance containing the event data.
    /// </param>
    public void OnKeyDown(KeyEventArgs e) {
        var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        var control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        var f = control ? 0.25f : 1;

        if (!shift)
            switch (e.Key) {
                case Key.Left:
                    AddRotateForce(-1 * f * (float)LeftRightRotationSensitivity, 0);
                    e.Handled = true;
                    break;
                case Key.Right:
                    AddRotateForce(1 * f * (float)LeftRightRotationSensitivity, 0);
                    e.Handled = true;
                    break;
                case Key.Up:
                    AddRotateForce(0, -1 * f * (float)UpDownRotationSensitivity);
                    e.Handled = true;
                    break;
                case Key.Down:
                    AddRotateForce(0, 1 * f * (float)UpDownRotationSensitivity);
                    e.Handled = true;
                    break;
            } else
            switch (e.Key) {
                case Key.Left:
                    AddPanForce(-5 * f * (float)LeftRightPanSensitivity, 0);
                    e.Handled = true;
                    break;
                case Key.Right:
                    AddPanForce(5 * f * (float)LeftRightPanSensitivity, 0);
                    e.Handled = true;
                    break;
                case Key.Up:
                    AddPanForce(0, -5 * f * (float)UpDownPanSensitivity);
                    e.Handled = true;
                    break;
                case Key.Down:
                    AddPanForce(0, 5 * f * (float)UpDownPanSensitivity);
                    e.Handled = true;
                    break;
            }

        switch (e.Key) {
            case Key.PageUp:
                AddZoomForce(-0.1f * f * (float)PageUpDownZoomSensitivity);
                e.Handled = true;
                break;
            case Key.PageDown:
                AddZoomForce(0.1f * f * (float)PageUpDownZoomSensitivity);
                e.Handled = true;
                break;
            case Key.Back:
                if (RestoreCameraSetting()) e.Handled = true;

                break;
        }

        switch (e.Key) {
            case Key.W:
                AddMoveForce(0, 0, 0.1f * f * (float)MoveSensitivity);
                break;
            case Key.A:
                AddMoveForce(-0.1f * f * (float)LeftRightPanSensitivity, 0, 0);
                break;
            case Key.S:
                AddMoveForce(0, 0, -0.1f * f * (float)MoveSensitivity);
                break;
            case Key.D:
                AddMoveForce(0.1f * f * (float)LeftRightPanSensitivity, 0, 0);
                break;
            case Key.Z:
                AddMoveForce(0, -0.1f * f * (float)LeftRightPanSensitivity, 0);
                break;
            case Key.Q:
                AddMoveForce(0, 0.1f * f * (float)LeftRightPanSensitivity, 0);
                break;
        }
    }

    /// <summary>
    ///     Called when the mouse wheel is moved.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The <see cref="System.Windows.Input.MouseWheelEventArgs" /> instance containing the event data.
    /// </param>
    public void OnMouseWheel(object sender, MouseWheelEventArgs e) {
        if (!IsZoomEnabled) return;
        if (ZoomAroundMouseDownPoint) {
            var point = e.GetPosition(Viewport);
            if (Viewport.FindNearest(point.ToVector2(), out var nearestPoint, out var normal, out var model)) {
                AddZoomForce(-e.Delta * 0.001f, nearestPoint);
                e.Handled = true;
                return;
            }
        }

        AddZoomForce(-e.Delta * 0.001f);
        e.Handled = true;
    }

    /// <summary>
    ///     The on time step.
    /// </summary>
    /// <param name="ticks">
    ///     The time.
    /// </param>
    private void OnTimeStep(long ticks) {
        if (lastTick == 0) lastTick = ticks;
        var time = (float)(ticks - lastTick) / Stopwatch.Frequency;
        time = time == 0 ? 0.016f : time;
        time = Math.Min(time, 0.05f); // Clamp the maximum time elapse to prevent over shooting
        // should be independent of time
        var factor = IsInertiaEnabled ? (float)Clamp(Math.Pow(InertiaFactor, time / 0.02f), 0.1f, 1) : 0;
        var needUpdate = false;

        if (rotationSpeed.LengthSquared() > 0.1f) {
            RotateHandler.Rotate(rotationPosition, rotationPosition + rotationSpeed * time, rotationPoint3D, false);
            rotationSpeed *= factor;
            needUpdate = true;
            spinningSpeed = VectorZero;
        } else {
            rotationSpeed = VectorZero;
            if (isSpinning && spinningSpeed.LengthSquared() > 0.1f) {
                RotateHandler.Rotate(spinningPosition, spinningPosition + spinningSpeed * time, spinningPoint3D, false);
                if (!InfiniteSpin) spinningSpeed *= factor;
                needUpdate = true;
            } else {
                spinningSpeed = VectorZero;
            }
        }

        if (panSpeed.LengthSquared() > 0.0001f) {
            PanHandler.Pan(panSpeed * time, false);
            panSpeed *= factor;
            needUpdate = true;
        } else {
            panSpeed = Vector3DZero;
        }

        if (moveSpeed.LengthSquared() > 0.0001f) {
            ZoomHandler.MoveCameraPosition(moveSpeed * time, false);
            moveSpeed *= factor;
            needUpdate = true;
        } else {
            moveSpeed = Vector3DZero;
        }

        if (Math.Abs(zoomSpeed) > 0.001f) {
            ZoomHandler.Zoom(zoomSpeed * time, zoomPoint3D, false, false);
            zoomSpeed *= factor;
            needUpdate = true;
        } else {
            zoomSpeed = 0;
        }

        if (ActualCamera.OnTimeStep()) needUpdate = true;
        if (needUpdate) {
            lastTick = ticks;
            Viewport.InvalidateRender();
        } else {
            lastTick = 0;
        }
    }

    /// <summary>
    ///     The on viewport changed.
    /// </summary>
    private void OnViewportChanged() {
        InitializeBindings();
    }

    /// <summary>
    ///     The refresh viewport.
    /// </summary>
    private void RefreshViewport() {
        Viewport.InvalidateRender();
    }

    /// <summary>
    ///     The reset camera event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void ResetCameraHandler(object sender, ExecutedRoutedEventArgs e) {
        if (IsPanEnabled && IsZoomEnabled && CameraMode != CameraMode.FixedPosition) {
            StopAnimations();
            ResetCamera();
        }
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
        ChangeDirection(new Vector3(0, -1, 0), new Vector3(0, 0, 1));
    }

    /// <summary>
    ///     The stop animations.
    /// </summary>
    public void StopAnimations() {
        StopPanning();
        StopZooming();
        StopSpin();
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
        ChangeDirection(new Vector3(0, 0, -1), new Vector3(0, 1, 0));
    }

    /// <summary>
    ///     The Zoom extents event handler.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private void ZoomExtentsHandler(object sender, ExecutedRoutedEventArgs e) {
        StopAnimations();
        ZoomExtents();
    }

    #region TouchGesture

    public bool EnableTouchRotate { get; set; } = true;
    public bool EnablePinchZoom { get; set; } = true;
    public bool EnableThreeFingerPan { get; set; } = true;
    public bool PinchZoomAtCenter { get; set; } = false;

    #endregion
}
