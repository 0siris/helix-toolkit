// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PanHandler.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Handles panning.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Handles panning.
/// </summary>
internal class PanHandler : MouseGestureHandler
{
    /// <summary>
    ///     The 3D pan origin.
    /// </summary>
    private Vector3 panPoint3D;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PanHandler" /> class.
    /// </summary>
    /// <param name="controller">
    ///     The camera controller.
    /// </param>
    public PanHandler(CameraController controller)
        : base(controller)
    {
    }

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="Point" /> instance containing the event data.</param>
    public override void Delta(Point e)
    {
        base.Delta(e);
        var thisPoint3D = UnProject(e, panPoint3D, Camera.CameraInternal.LookDirection);
        if (Camera.CameraInternal.LookDirection.LengthSquared() < 1e-5f) return;
        if (LastPoint3D == null || thisPoint3D == null) return;

        var delta3D = LastPoint3D.Value - thisPoint3D.Value;
        Pan(delta3D);

        LastPoint = e;
        LastPoint3D = UnProject(e, panPoint3D, Camera.CameraInternal.LookDirection);
    }

    /// <summary>
    ///     Pans the camera by the specified 3D vector (world coordinates).
    /// </summary>
    /// <param name="delta">
    ///     The panning vector.
    /// </param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void Pan(Vector3 delta, bool stopOther = true)
    {
        if (!Controller.IsPanEnabled) return;
        if (stopOther)
        {
            Controller.StopSpin();
            Controller.StopZooming();
        }

        if (CameraMode == CameraMode.FixedPosition) return;

        Camera.Position += delta.ToVector3D();
    }

    /// <summary>
    ///     Pans the camera by the specified 2D vector (screen coordinates).
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void Pan(Vector2 delta, bool stopOther = true)
    {
        if (stopOther)
        {
            Controller.StopSpin();
            Controller.StopZooming();
        }

        var mousePoint = new Point(LastPoint.X + delta.X, LastPoint.Y + delta.Y);

        var thisPoint3D = UnProject(mousePoint, panPoint3D, Camera.CameraInternal.LookDirection);

        if (LastPoint3D == null || thisPoint3D == null) return;

        var delta3D = LastPoint3D.Value - thisPoint3D.Value;
        Pan(delta3D);

        LastPoint3D = UnProject(mousePoint, panPoint3D, Camera.CameraInternal.LookDirection);

        LastPoint = mousePoint;
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">The <see cref="Point" /> instance containing the event data.</param>
    public override void Started(Point e)
    {
        base.Started(e);
        panPoint3D = Camera.CameraInternal.Target;
        if (MouseDownNearestPoint3D.HasValue) panPoint3D = MouseDownNearestPoint3D.Value;
        LastPoint3D = UnProject(MouseDownPoint, panPoint3D, Camera.CameraInternal.LookDirection);
    }

    /// <summary>
    ///     Occurs when the command associated with this handler initiates a check to determine whether the command can be
    ///     executed on the command target.
    /// </summary>
    /// <returns>
    ///     True if the execution can continue.
    /// </returns>
    protected override bool CanExecute()
    {
        return Controller.IsPanEnabled && Controller.CameraMode != CameraMode.FixedPosition;
    }

    /// <summary>
    ///     Gets the cursor for the gesture.
    /// </summary>
    /// <returns>
    ///     A cursor.
    /// </returns>
    protected override Cursor GetCursor()
    {
        return Controller.PanCursor;
    }

    /// <summary>
    ///     Called when inertia is starting.
    /// </summary>
    /// <param name="elapsedTime">
    ///     The elapsed time (milliseconds).
    /// </param>
    protected override void OnInertiaStarting(double elapsedTime)
    {
        var speed = (LastPoint - MouseDownPoint) * (40.0 / elapsedTime);
        Controller.AddPanForce((float) speed.X, (float) speed.Y);
    }
}