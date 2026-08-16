// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RotateHandler.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Handles rotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.Wpf.SharpDX.Extensions;

namespace HelixToolkit.Wpf.SharpDX.Controls.MouseHandlers;

/// <summary>
///     Handles rotation.
/// </summary>
internal class RotateHandler : MouseGestureHandler {
    /// <summary>
    ///     The change look at.
    /// </summary>
    private readonly bool changeLookAt;

    private bool invertUpDir;

    /// <summary>
    ///     The x rotation axis.
    /// </summary>
    private Vector3 rotationAxisX;

    /// <summary>
    ///     The y rotation axis.
    /// </summary>
    private Vector3 rotationAxisY;

    /// <summary>
    ///     The rotation point.
    /// </summary>
    private Point rotationPoint;

    /// <summary>
    ///     The 3D rotation point.
    /// </summary>
    private Vector3 rotationPoint3D;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RotateHandler" /> class.
    /// </summary>
    /// <param name="controller">
    ///     The controller.
    /// </param>
    /// <param name="changeLookAt">
    ///     The change look at.
    /// </param>
    public RotateHandler(CameraController controller, bool changeLookAt = false)
        : base(controller) {
        this.changeLookAt = changeLookAt;
    }

    /// <summary>
    ///     Gets the camera rotation mode.
    /// </summary>
    /// <value>
    ///     The camera rotation mode.
    /// </value>
    protected CameraRotationMode CameraRotationMode => Controller.CameraRotationMode;

    /// <summary>
    ///     Occurs when the manipulation is completed.
    /// </summary>
    /// <param name="e">The <see cref="ManipulationEventArgs" /> instance containing the event data.</param>
    public override void Completed(Point e) {
        base.Completed(e);
        Viewport.HideTargetAdorner();
    }

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="ManipulationEventArgs" /> instance containing the event data.</param>
    public override void Delta(Point e) {
        base.Delta(e);
        Rotate(LastPoint, e, rotationPoint3D);
        LastPoint = e;
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
    public void LookAt(Vector3 target, double animationTime) {
        if (!Controller.IsPanEnabled) return;

        Camera.LookAt(target.ToPoint3D(), animationTime);
    }

    /// <summary>
    ///     Rotate the camera around the specified point.
    /// </summary>
    /// <param name="p0">
    ///     The p 0.
    /// </param>
    /// <param name="p1">
    ///     The p 1.
    /// </param>
    /// <param name="rotateAround">
    ///     The rotate around.
    /// </param>
    public void Rotate(Point p0, Point p1, Vector3 rotateAround) {
        Rotate(p0.ToVector2(), p1.ToVector2(), rotateAround);
    }

    /// <summary>
    ///     Rotates the specified p0.
    /// </summary>
    /// <param name="p0">The p0.</param>
    /// <param name="p1">The p1.</param>
    /// <param name="rotateAround">The rotate around.</param>
    /// <param name="stopOther">if set to <c>true</c> [stop other].</param>
    public void Rotate(Vector2 p0, Vector2 p1, Vector3 rotateAround, bool stopOther = true) {
        if (!Controller.IsRotationEnabled) return;
        if (stopOther) {
            Controller.StopZooming();
            Controller.StopPanning();
        }

        p0 = SilkMath.Multiply(p0, Controller.AllowRotateXy);
        p1 = SilkMath.Multiply(p1, Controller.AllowRotateXy);
        var camera = Camera.CameraInternal;
        var newPos = camera.Position;
        var newLook = camera.LookDirection;
        var newUp = SilkMath.Normalize(camera.UpDirection);
        switch (Controller.CameraRotationMode) {
            case CameraRotationMode.Trackball:
                CameraMath.RotateTrackball(CameraMode,
                                           ref p0,
                                           ref p1,
                                           ref rotateAround,
                                           (float)RotationSensitivity,
                                           Controller.Width,
                                           Controller.Height,
                                           camera,
                                           Inv,
                                           out newPos,
                                           out newLook,
                                           out newUp);
                break;
            case CameraRotationMode.Turntable:
                var p = p1 - p0;
                CameraMath.RotateTurntable(CameraMode,
                                           ref p,
                                           ref rotateAround,
                                           (float)RotationSensitivity,
                                           Controller.Width,
                                           Controller.Height,
                                           camera,
                                           Inv,
                                           invertUpDir ? -ModelUpDirection : ModelUpDirection,
                                           out newPos,
                                           out newLook,
                                           out newUp);
                break;
            case CameraRotationMode.Turnball:
                CameraMath.RotateTurnball(CameraMode,
                                          ref p0,
                                          ref p1,
                                          ref rotateAround,
                                          (float)RotationSensitivity,
                                          Controller.Width,
                                          Controller.Height,
                                           camera,
                                          Inv,
                                          out newPos,
                                          out newLook,
                                          out newUp);
                break;
        }

        Camera.LookDirection = newLook.ToVector3D();
        Camera.Position = newPos.ToPoint3D();
        Camera.UpDirection = newUp.ToVector3D();
    }

    /// <summary>
    ///     The rotate.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    public void Rotate(Vector2 delta) {
        var p0 = LastPoint.ToVector2();
        var p1 = p0 + delta;
        if (MouseDownPoint3D != null) Rotate(p0, p1, MouseDownPoint3D.Value);
        LastPoint = new Point(p0.X, p0.Y);
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">The <see cref="ManipulationEventArgs" /> instance containing the event data.</param>
    public override void Started(Point e) {
        base.Started(e);
        rotationPoint = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        rotationPoint3D = Camera.CameraInternal.Target;
        invertUpDir = SilkMath.Dot(Controller.CameraUpDirection, ModelUpDirection) < 0;

        switch (CameraMode) {
            case CameraMode.WalkAround:
                rotationPoint = MouseDownPoint;
                rotationPoint3D = Camera.CameraInternal.Position;
                break;
            default:
                if (Controller.FixedRotationPointEnabled) {
                    rotationPoint3D = Controller.FixedRotationPoint;
                } else if (changeLookAt && MouseDownNearestPoint3D != null) {
                    LookAt(MouseDownNearestPoint3D.Value, 0);
                    rotationPoint3D = Camera.CameraInternal.Target;
                } else if (Controller.RotateAroundMouseDownPoint && MouseDownNearestPoint3D != null) {
                    rotationPoint = MouseDownPoint;
                    rotationPoint3D = MouseDownNearestPoint3D.Value;
                }

                break;
        }

        if (CameraMode == CameraMode.Inspect) 
            Viewport.ShowTargetAdorner(rotationPoint);

        switch (CameraRotationMode) {
            case CameraRotationMode.Trackball:
                break;
            case CameraRotationMode.Turntable:
                break;
            case CameraRotationMode.Turnball:
                var camera = Camera.CameraInternal;
                CameraMath.InitTurnballRotationAxes(e.ToVector2(),
                                                    (int)Viewport.ActualWidth,
                                                    (int)Viewport.ActualHeight,
                                                    camera,
                                                    out rotationAxisX,
                                                    out rotationAxisY);
                break;
        }

        Controller.StopSpin();
    }

    /// <summary>
    ///     The can execute.
    /// </summary>
    /// <returns>
    ///     True if the execution can continue.
    /// </returns>
    protected override bool CanExecute() {
        if (changeLookAt) return CameraMode != CameraMode.FixedPosition && Controller.IsPanEnabled;

        return Controller.IsRotationEnabled;
    }

    /// <summary>
    ///     Gets the cursor.
    /// </summary>
    /// <returns>
    ///     A cursor.
    /// </returns>
    protected override Cursor GetCursor() => Controller.RotateCursor;

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time.
    /// </param>
    protected override void OnInertiaStarting(double elapsedTime) {
        var delta = LastPoint - MouseDownPoint;
        var deltaV = new Vector2((float)delta.X, (float)delta.Y);
        // Debug.WriteLine("SpinInertiaStarting: " + elapsedTime + "ms " + delta.Length + "px");
        Controller.StartSpin(4 * deltaV * (float)(Controller.SpinReleaseTime / elapsedTime),
                             MouseDownPoint,
                             rotationPoint3D);
    }
}
