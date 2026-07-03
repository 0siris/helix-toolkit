namespace HelixToolkit.SharpDX.Core.Controls;

public sealed class PanHandler(CameraController cameraController) : MouseGestureHandler(cameraController) {
    /// <summary>
    ///     The 3D pan origin.
    /// </summary>
    private Vector3 panPoint3D;

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="Vector2" /> instance containing the event data.</param>
    public override void Delta(Vector2 e) {
        base.Delta(e);
        if (Camera.LookDirection.LengthSquared() < 1e-5f) return;
        var thisPoint3D = UnProject(e, panPoint3D, Camera.LookDirection);

        if (LastPoint3D == null || thisPoint3D == null) return;

        var delta3D = LastPoint3D.Value - thisPoint3D.Value;
        Pan(delta3D);

        LastPoint = e;
        LastPoint3D = UnProject(e, panPoint3D, Camera.LookDirection);
    }

    /// <summary>
    ///     Pans the camera by the specified 3D vector (world coordinates).
    /// </summary>
    /// <param name="delta">
    ///     The panning vector.
    /// </param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void Pan(Vector3 delta, bool stopOther = true) {
        if (!Controller.IsPanEnabled) return;
        if (stopOther) {
            Controller.StopSpin();
            Controller.StopZooming();
        }

        if (CameraMode == CameraMode.FixedPosition) return;
        Camera.Position += delta;
    }

    /// <summary>
    ///     Pans the camera by the specified 2D vector (screen coordinates).
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void Pan(Vector2 delta, bool stopOther = true) {
        if (stopOther) {
            Controller.StopSpin();
            Controller.StopZooming();
        }

        var mousePoint = LastPoint + delta;

        var thisPoint3D = UnProject(mousePoint, panPoint3D, Camera.LookDirection);

        if (LastPoint3D == null || thisPoint3D == null) return;

        var delta3D = LastPoint3D.Value - thisPoint3D.Value;
        Pan(delta3D);

        LastPoint3D = UnProject(mousePoint, panPoint3D, Camera.LookDirection);

        LastPoint = mousePoint;
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">The <see cref="Vector2" /> instance containing the event data.</param>
    protected override void Started(Vector2 e) {
        base.Started(e);
        panPoint3D = Camera.Target;
        if (MouseDownNearestPoint3D.HasValue) panPoint3D = MouseDownNearestPoint3D.Value;

        LastPoint3D = UnProject(MouseDownPoint, panPoint3D, Camera.LookDirection);
    }

    /// <summary>
    ///     Occurs when the command associated with this handler initiates a check to determine whether the command can be
    ///     executed on the command target.
    /// </summary>
    /// <returns>
    ///     True if the execution can continue.
    /// </returns>
    protected override bool CanStart() 
        => Controller.IsPanEnabled && Controller.CameraMode != CameraMode.FixedPosition;

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time (milliseconds).
    /// </param>
    protected override void OnInertiaStarting(double elapsedTime) {
        var speed = (LastPoint - MouseDownPoint) * (40.0f / (float) elapsedTime);
        Controller.AddPanForce(speed.X, speed.Y);
    }
}
