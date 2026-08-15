/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model.Camera;

namespace HelixToolkit.SharpDX.Core.Controls;

public sealed class RotateHandler(CameraController controller, bool changeLookAt = false)
    : MouseGestureHandler(controller) {
    /// <summary>
    ///     The change look at.
    /// </summary>
    private readonly bool changeLookAt = changeLookAt;

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
    private Vector2 rotationPoint;

    /// <summary>
    ///     The 3D rotation point.
    /// </summary>
    private Vector3 rotationPoint3D;

    /// <summary>
    ///     Gets the camera rotation mode.
    /// </summary>
    /// <value>
    ///     The camera rotation mode.
    /// </value>
    private CameraRotationMode CameraRotationMode => Controller.CameraRotationMode;

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="Vector2" /> instance containing the event data.</param>
    public override void Delta(Vector2 e) {
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
    public void LookAt(Vector3 target, float animationTime) {
        if (!Controller.IsPanEnabled) return;

        Camera.LookAt(target, animationTime);
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
    /// <param name="stopOther">Stop other manipulation</param>
    public void Rotate(Vector2 p0, Vector2 p1, Vector3 rotateAround, bool stopOther = true) {
        if (!Controller.IsRotationEnabled) return;
        if (stopOther) {
            Controller.StopZooming();
            Controller.StopPanning();
        }

        p0 = SilkMath.Multiply(p0, Controller.AllowRotateXy);
        p1 = SilkMath.Multiply(p1, Controller.AllowRotateXy);
        var newPos = Camera.Position;
        var newLook = Camera.LookDirection;
        var newUp = SilkMath.Normalize(Camera.UpDirection);
        switch (Controller.CameraRotationMode) {
            case CameraRotationMode.Trackball:
                CameraMath.RotateTrackball(CameraMode,
                                           ref p0,
                                           ref p1,
                                           ref rotateAround,
                                           (float)RotationSensitivity,
                                           Controller.Width,
                                           Controller.Height,
                                           Camera,
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
                                           Camera,
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
                                          Camera,
                                          Inv,
                                          out newPos,
                                          out newLook,
                                          out newUp);
                break;
        }

        Camera.LookDirection = newLook;
        Camera.Position = newPos;
        Camera.UpDirection = newUp;
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">The <see cref="Vector2" /> instance containing the event data.</param>
    protected override void Started(Vector2 e) {
        base.Started(e);
        rotationPoint = new Vector2(Controller.Width / 2f, Controller.Height / 2f);
        rotationPoint3D = Camera.Target;
        invertUpDir = SilkMath.Dot(Controller.CameraUpDirection, ModelUpDirection) < 0;

        switch (CameraMode) {
            case CameraMode.WalkAround:
                rotationPoint = MouseDownPoint;
                rotationPoint3D = Camera.Position;
                break;
            default:
                if (Controller.FixedRotationPointEnabled) {
                    rotationPoint3D = Controller.FixedRotationPoint;
                } else if (changeLookAt && MouseDownNearestPoint3D is not null) {
                    LookAt(MouseDownNearestPoint3D.Value, 0);
                    rotationPoint3D = Camera.Target;
                } else if (Controller.RotateAroundMouseDownPoint && MouseDownNearestPoint3D is not null) {
                    rotationPoint = MouseDownPoint;
                    rotationPoint3D = MouseDownNearestPoint3D.Value;
                }

                break;
        }

        switch (CameraRotationMode) {
            case CameraRotationMode.Trackball:
            case CameraRotationMode.Turntable:
                break;
            case CameraRotationMode.Turnball:
                CameraMath.InitTurnballRotationAxes(e,
                                                    Controller.Width,
                                                    Controller.Height,
                                                    Camera,
                                                    out rotationAxisX,
                                                    out rotationAxisY);
                break;
        }

        Controller.StopSpin();
    }

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time.
    /// </param>
    protected override void OnInertiaStarting(double elapsedTime) {
        var delta = LastPoint - MouseDownPoint;
        var deltaV = new Vector2(delta.X, delta.Y);
        // Debug.WriteLine("SpinInertiaStarting: " + elapsedTime + "ms " + delta.Length + "px");
        Controller.StartSpin(4 * deltaV * (float)(Controller.SpinReleaseTime / elapsedTime),
                             MouseDownPoint,
                             rotationPoint3D);
    }
}
