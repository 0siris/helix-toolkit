// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MouseGestureHandler.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   An abstract base class for the mouse gesture handlers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     An abstract base class for the mouse gesture handlers.
/// </summary>
internal abstract class MouseGestureHandler {
    protected List<HitTestResult> Hits = [];

    private long startTick;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseGestureHandler" /> class.
    /// </summary>
    /// <param name="controller">
    ///     The camera controller.
    /// </param>
    protected MouseGestureHandler(CameraController controller) {
        Controller = controller;
    }

    /// <summary>
    ///     Gets the origin.
    /// </summary>
    public Vector3 Origin {
        get {
            if (MouseDownNearestPoint3D != null) return MouseDownNearestPoint3D.Value;

            if (MouseDownPoint3D != null) return MouseDownPoint3D.Value;

            return new Vector3();
        }
    }

    /// <summary>
    ///     Gets the camera.
    /// </summary>
    /// <value>The camera.</value>
    protected Camera Camera => Controller.ActualCamera;

    /// <summary>
    ///     Gets the camera mode.
    /// </summary>
    /// <value>The camera mode.</value>
    protected CameraMode CameraMode => Controller.CameraMode;

    /// <summary>
    ///     Gets or sets the last point (in 2D screen coordinates).
    /// </summary>
    protected Point LastPoint { get; set; }

    /// <summary>
    ///     Gets or sets the last point (in 3D world coordinates).
    /// </summary>
    protected Vector3? LastPoint3D { get; set; }

    /// <summary>
    ///     Use to invert the left handed system
    /// </summary>
    /// <value>
    ///     The inv.
    /// </value>
    protected int Inv { get; private set; } = 1;

    /// <summary>
    ///     Gets the model up direction.
    /// </summary>
    /// <value>The model up direction.</value>
    protected Vector3 ModelUpDirection => Controller.ModelUpDirection;

    /// <summary>
    ///     Gets or sets the mouse down point at the nearest hit element (3D world coordinates).
    /// </summary>
    protected Vector3? MouseDownNearestPoint3D { get; set; }

    /// <summary>
    ///     Gets or sets the mouse down nearest hit model bounding box center.
    /// </summary>
    /// <value>
    ///     The mouse down nearest model bound center.
    /// </value>
    protected Vector3? MouseDownNearestModelBoundCenter { get; set; }

    /// <summary>
    ///     Gets or sets the mouse down point (2D screen coordinates).
    /// </summary>
    protected Point MouseDownPoint { get; set; }

    /// <summary>
    ///     Gets or sets the mouse down point (3D world coordinates).
    /// </summary>
    protected Vector3? MouseDownPoint3D { get; set; }

    /// <summary>
    ///     Gets the rotation sensitivity.
    /// </summary>
    /// <value>The rotation sensitivity.</value>
    protected double RotationSensitivity => Controller.RotationSensitivity;

    /// <summary>
    ///     Gets the viewport.
    /// </summary>
    /// <value>The viewport.</value>
    public Viewport3DX Viewport => Controller.Viewport;

    public CameraController Controller { get; }

    /// <summary>
    ///     Gets the zoom sensitivity.
    /// </summary>
    /// <value>The zoom sensitivity.</value>
    protected double ZoomSensitivity => Controller.ZoomSensitivity;

    public bool IsActive { get; private set; }

    /// <summary>
    ///     Occurs when the manipulation is completed.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="Point" /> instance containing the event data.
    /// </param>
    public virtual void Completed(Point e) {
        var elapsed =
            (double)(Stopwatch.GetTimestamp() - startTick) / Stopwatch.Frequency *
            1000; //this.ManipulationWatch.ElapsedMilliseconds;
        if (elapsed > 0 && elapsed < Controller.SpinReleaseTime) OnInertiaStarting(elapsed);
        startTick = Stopwatch.GetTimestamp();
        IsActive = false;
    }

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="Point" /> instance containing the event data.
    /// </param>
    public virtual void Delta(Point e) { }

    /// <summary>
    ///     Executes the mouse gesture command.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    public void Execute(object sender, ExecutedRoutedEventArgs e) {
        if (!CanExecute()) return;


        Viewport.MouseMove -= OnMouseMove;
        Viewport.MouseUp -= OnMouseUp;
        Viewport.MouseUp += OnMouseUp;
        Viewport.Focus();
        Viewport.CaptureMouse();
        OnMouseDown(sender, null);
        Viewport.MouseMove += OnMouseMove;
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="Point" /> instance containing the event data.
    /// </param>
    public virtual void Started(Point e) {
        SetMouseDownPoint(e);
        LastPoint = MouseDownPoint;
        LastPoint3D = MouseDownPoint3D;
        //this.ManipulationWatch.Restart();
        startTick = Stopwatch.GetTimestamp();
        Inv = Camera.CreateLeftHandSystem ? -1 : 1;
        Controller.StopAnimations();
        Controller.PushCameraSetting();
        IsActive = true;
    }

    /// <summary>
    ///     Un-projects a point from the screen (2D) to a point on plane (3D)
    /// </summary>
    /// <param name="p">
    ///     The 2D point.
    /// </param>
    /// <param name="position">
    ///     plane position
    /// </param>
    /// <param name="normal">
    ///     plane normal
    /// </param>
    /// <returns>
    ///     A 3D point.
    /// </returns>
    public Vector3? UnProject(Point p, Vector3 position, Vector3 normal) {
        var ray = GetRay(p);
        var plane = new Plane(position, normal);
        if (plane.Intersects(ref ray, out var distance)) return ray.Position + ray.Direction * distance;

        return null;
        //return ray.PlaneIntersection(position, normal);
    }

    /// <summary>
    ///     Un-projects a point from the screen (2D) to a point on the plane trough the camera target point.
    /// </summary>
    /// <param name="p">
    ///     The 2D point.
    /// </param>
    /// <returns>
    ///     A 3D point.
    /// </returns>
    public Vector3? UnProject(Point p) => UnProject(p, Camera.CameraInternal.Target, Camera.CameraInternal.LookDirection);

    /// <summary>
    ///     Occurs when the command associated with this handler initiates a check to determine whether the command can be
    ///     executed on the command target.
    /// </summary>
    /// <returns>
    ///     True if the execution can continue.
    /// </returns>
    protected virtual bool CanExecute() => true;

    /// <summary>
    ///     Gets the cursor for the gesture.
    /// </summary>
    /// <returns>
    ///     A cursor.
    /// </returns>
    protected abstract Cursor GetCursor();

    /// <summary>
    ///     Get the ray into the view volume given by the position in 2D (screen coordinates)
    /// </summary>
    /// <param name="position">
    ///     A 2D point.
    /// </param>
    /// <returns>
    ///     A ray
    /// </returns>
    protected Ray GetRay(Point position) => Viewport.UnProject(position);

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time (milliseconds).
    /// </param>
    protected virtual void OnInertiaStarting(double elapsedTime) { }

    /// <summary>
    ///     Called when the mouse button is pressed down.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The <see cref="System.Windows.Input.MouseEventArgs" /> instance containing the event data.
    /// </param>
    protected virtual void OnMouseDown(object sender, MouseEventArgs e) {
        Started(Mouse.GetPosition(Viewport));

        Controller.CursorHistory.Push(Viewport.Cursor);
        Viewport.Cursor = GetCursor();
    }

    /// <summary>
    ///     The on mouse move.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    protected virtual void OnMouseMove(object sender, MouseEventArgs e) {
        if (e.Handled)
            return;
        Delta(Mouse.GetPosition(Viewport));
    }

    /// <summary>
    ///     The on mouse up.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e) {
        Viewport.MouseMove -= OnMouseMove;
        Viewport.MouseUp -= OnMouseUp;
        Viewport.ReleaseMouseCapture();
        Viewport.Cursor = Controller.CursorHistory.Count > 0 ? Controller.CursorHistory.Pop() : Cursors.Arrow;
        Completed(Mouse.GetPosition(Viewport));
        CheckCursorHistory();
    }

    private void CheckCursorHistory() {
        if (Controller.CursorHistory.Count == 0) return;
        foreach (var handler in Controller.MouseHandlers)
            if (handler.IsActive)
                return;

        Cursor cur = null;
        while (Controller.CursorHistory.Count > 0) cur = Controller.CursorHistory.Pop();
        Viewport.Cursor = cur;
    }

    /// <summary>
    ///     Calculate the screen position of a 3D point.
    /// </summary>
    /// <param name="p">
    ///     The 3D point.
    /// </param>
    /// <returns>
    ///     The 2D point.
    /// </returns>
    protected Point Project(Vector3 p) => Viewport.Project(p).ToPoint();

    /// <summary>
    ///     Sets mouse down point.
    /// </summary>
    /// <param name="position">
    ///     The position.
    /// </param>
    private void SetMouseDownPoint(Point position) {
        MouseDownPoint = position;

        if (!Viewport.FixedRotationPointEnabled && Viewport.FindHitsInFrustum(MouseDownPoint.ToVector2(), ref Hits)) {
            if (Hits.Count > 0) {
                MouseDownNearestPoint3D = Hits[0].PointHit;
                if (Hits[0].ModelHit is Element3D ele)
                    MouseDownNearestModelBoundCenter = ele.BoundsWithTransform.Center();
                else if (Hits[0].ModelHit is SceneNode node)
                    MouseDownNearestModelBoundCenter = node.BoundsWithTransform.Center();
            }
        } else {
            MouseDownNearestModelBoundCenter = null;
            MouseDownNearestPoint3D = null;
        }

        MouseDownPoint3D = UnProject(MouseDownPoint);
    }
}
