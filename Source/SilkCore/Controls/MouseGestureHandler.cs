/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Controls;

public abstract class MouseGestureHandler {
    private List<HitTestResult> hits = [];

    /// <summary>
    ///     Use to invert the left handed system
    /// </summary>
    /// <value>
    ///     The inv.
    /// </value>
    protected int Inv = 1;

    /// <summary>
    ///     Gets or sets the last point (in 2D screen coordinates).
    /// </summary>
    protected Vector2 LastPoint;

    /// <summary>
    ///     Gets or sets the last point (in 3D world coordinates).
    /// </summary>
    protected Vector3? LastPoint3D;

    /// <summary>
    ///     Gets or sets the mouse down Vector2 at the nearest hit element (3D world coordinates).
    /// </summary>
    protected Vector3? MouseDownNearestPoint3D;

    /// <summary>
    ///     Gets or sets the mouse down Vector2 (2D screen coordinates).
    /// </summary>
    protected Vector2 MouseDownPoint;

    /// <summary>
    ///     Gets or sets the mouse down Vector2 (3D world coordinates).
    /// </summary>
    protected Vector3? MouseDownPoint3D;

    private long startTick;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseGestureHandler" /> class.
    /// </summary>
    /// <param name="cameraController">
    ///     The viewport.
    /// </param>
    protected MouseGestureHandler(CameraController cameraController)
        => Controller = cameraController;

    /// <summary>
    ///     Gets the origin.
    /// </summary>
    public Vector3 Origin {
        get {
            if (Controller.RotateAroundMouseDownPoint && MouseDownNearestPoint3D.HasValue)
                return MouseDownNearestPoint3D.Value;

            return MouseDownPoint3D ?? new Vector3();
        }
    }

    /// <summary>
    ///     Gets the camera.
    /// </summary>
    /// <value>The camera.</value>
    protected ProjectionCameraCore Camera
        => Controller.ActualCamera as ProjectionCameraCore ?? throw new InvalidOperationException(); //TODO is there an assert thas this is allways a projection camera?

    /// <summary>
    ///     Gets the camera mode.
    /// </summary>
    /// <value>The camera mode.</value>
    protected CameraMode CameraMode => Controller.CameraMode;

    /// <summary>
    ///     Gets the model up direction.
    /// </summary>
    /// <value>The model up direction.</value>
    protected Vector3 ModelUpDirection => Controller.ModelUpDirection;

    /// <summary>
    ///     Gets or sets the mouse down the nearest hit model bounding box center.
    /// </summary>
    /// <value>
    ///     The mouse down the nearest model bound center.
    /// </value>
    protected Vector3? MouseDownNearestModelBoundCenter { get; set; }

    /// <summary>
    ///     Gets the rotation sensitivity.
    /// </summary>
    /// <value>The rotation sensitivity.</value>
    protected double RotationSensitivity => Controller.RotationSensitivity;

    /// <summary>
    ///     Gets the viewport.
    /// </summary>
    /// <value>The viewport.</value>
    protected CameraController Controller { get; }

    /// <summary>
    ///     Gets the zoom sensitivity.
    /// </summary>
    /// <value>The zoom sensitivity.</value>
    protected float ZoomSensitivity => Controller.ZoomSensitivity;

    public event EventHandler? MouseCaptureRequested;
    public event EventHandler? MouseReleaseRequested;


    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="Vector2" /> instance containing the event data.
    /// </param>
    public virtual void Delta(Vector2 e) { }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="Vector2" /> instance containing the event data.
    /// </param>
    protected virtual void Started(Vector2 e) {
        SetMouseDownPoint(e);
        LastPoint = MouseDownPoint;
        LastPoint3D = MouseDownPoint3D;
        startTick = Stopwatch.GetTimestamp();
        Inv = Camera.CreateLeftHandSystem ? -1 : 1;
        Controller.StopAnimations();
        Controller.Viewport.InvalidateRender();
    }

    /// <summary>
    ///     Un-projects a Vector2 from the screen (2D) to a Vector2 on plane (3D)
    /// </summary>
    /// <param name="p">
    ///     The 2D Vector2.
    /// </param>
    /// <param name="position">
    ///     plane position
    /// </param>
    /// <param name="normal">
    ///     plane normal
    /// </param>
    /// <returns>
    ///     A 3D Vector2.
    /// </returns>
    public Vector3? UnProject(Vector2 p, Vector3 position, Vector3 normal) {
        var ray = GetRay(p);
        var plane = new Plane(position, normal);

        if (Collision.RayIntersectsPlane(ref ray, ref plane, out Vector3 point))
            return point;

        return null;
    }

    /// <summary>
    ///     Un-projects a Vector2 from the screen (2D) to a Vector2 on the plane through the camera target Vector2.
    /// </summary>
    /// <param name="p">
    ///     The 2D Vector2.
    /// </param>
    /// <returns>
    ///     A 3D Vector2.
    /// </returns>
    public Vector3? UnProject(Vector2 p)
        => UnProject(p, Camera.Target, Camera.LookDirection);

    /// <summary>
    ///     Get the ray into the view volume given by the position in 2D (screen coordinates)
    /// </summary>
    /// <param name="position">
    ///     A 2D Vector2.
    /// </param>
    /// <returns>
    ///     A ray
    /// </returns>
    protected Ray GetRay(Vector2 position)
        => Controller.Viewport.UnProject(position, out var ray)
               ? ray
               : new Ray();

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time (milliseconds).
    /// </param>
    protected virtual void OnInertiaStarting(double elapsedTime) { }

    /// <summary>
    ///     Mouses down.
    /// </summary>
    /// <param name="e">The e.</param>
    /// <returns></returns>
    public virtual bool Start(Vector2 e) {
        if (!CanStart())
            return false;

        MouseCaptureRequested?.Invoke(this, EventArgs.Empty);
        Started(e);
        return true;

    }

    protected virtual bool CanStart()
        => true;

    /// <summary>
    ///     Mouses the move.
    /// </summary>
    /// <param name="e">The e.</param>
    public virtual void MouseMove(Vector2 e) {
        Delta(e);
        Controller.Viewport.InvalidateRender();
    }

    /// <summary>
    ///     Mouses up.
    /// </summary>
    /// <param name="e">The e.</param>
    public virtual void End(Vector2 e) {
        MouseReleaseRequested?.Invoke(this, EventArgs.Empty);
        Completed(e);
    }

    protected virtual void Completed(Vector2 e) {
        var elapsed = (double) (Stopwatch.GetTimestamp() - startTick) / Stopwatch.Frequency * 1000; //this.ManipulationWatch.ElapsedMilliseconds;

        if (elapsed > 0 && elapsed < Controller.SpinReleaseTime)
            OnInertiaStarting(elapsed);

        startTick = Stopwatch.GetTimestamp();
    }

    /// <summary>
    ///     Calculate the screen position of a 3D Vector2.
    /// </summary>
    /// <param name="p">
    ///     The 3D Vector2.
    /// </param>
    /// <returns>
    ///     The 2D Vector2.
    /// </returns>
    protected Vector2 Project(Vector3 p)
        => Controller.Viewport.Project(p);

    /// <summary>
    ///     Sets mouse down Vector2.
    /// </summary>
    /// <param name="position">
    ///     The position.
    /// </param>
    private void SetMouseDownPoint(Vector2 position) {
        MouseDownPoint = position;

        if (!Controller.FixedRotationPointEnabled && Controller.Viewport.FindHitsInFrustum(MouseDownPoint, ref hits)) {
            if (hits.Count > 0) {
                MouseDownNearestPoint3D = hits[0].PointHit;
                if (hits[0].ModelHit is SceneNode node)
                    MouseDownNearestModelBoundCenter = BoundingBoxExtensions.Center(node.BoundsWithTransform);
            }
        } else {
            MouseDownNearestModelBoundCenter = null;
            MouseDownNearestPoint3D = null;
        }

        MouseDownPoint3D = UnProject(position);
    }
}
