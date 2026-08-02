// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoomHandler.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Handles zooming.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Handles zooming.
/// </summary>
internal class ZoomHandler : MouseGestureHandler {
    /// <summary>
    ///     The change field of view.
    /// </summary>
    private readonly bool changeFieldOfView;

    /// <summary>
    ///     The zoom point.
    /// </summary>
    private Point zoomPoint;

    /// <summary>
    ///     The 3D zoom point.
    /// </summary>
    private Vector3 zoomPoint3D;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ZoomHandler" /> class.
    /// </summary>
    /// <param name="controller">
    ///     The camera controller.
    /// </param>
    /// <param name="changeFieldOfView">
    ///     if set to <c>true</c> [change field of view].
    /// </param>
    public ZoomHandler(CameraController controller, bool changeFieldOfView = false)
        : base(controller) {
        this.changeFieldOfView = changeFieldOfView;
    }

    /// <summary>
    ///     Occurs when the manipulation is completed.
    /// </summary>
    /// <param name="e">The <see cref="Point" /> instance containing the event data.</param>
    public override void Completed(Point e) {
        base.Completed(e);
        Viewport.HideTargetAdorner();
    }

    /// <summary>
    ///     Occurs when the position is changed during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="Point" /> instance containing the event data.</param>
    public override void Delta(Point e) {
        var delta = e - LastPoint;
        LastPoint = e;
        Zoom(delta.Y * 0.01, zoomPoint3D);
    }

    /// <summary>
    ///     Occurs when the manipulation is started.
    /// </summary>
    /// <param name="e">The <see cref="Point" /> instance containing the event data.</param>
    public override void Started(Point e) {
        base.Started(e);
        zoomPoint = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        zoomPoint3D = Camera.CameraInternal.Target;

        if (Controller.ZoomAroundMouseDownPoint && MouseDownNearestPoint3D != null) {
            zoomPoint = MouseDownPoint;
            zoomPoint3D = MouseDownNearestPoint3D.Value;
        }

        if (!changeFieldOfView) Viewport.ShowTargetAdorner(zoomPoint);
    }

    /// <summary>
    ///     Zooms the view.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    public void Zoom(double delta) {
        Zoom(delta, Camera.CameraInternal.Target);
    }

    /// <summary>
    ///     Zooms the view around the specified point.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    /// <param name="zoomAround">
    ///     The zoom around.
    /// </param>
    /// <param name="isTouch"></param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void Zoom(double delta, Vector3 zoomAround, bool isTouch = false, bool stopOther = true) {
        if (!Controller.IsZoomEnabled) return;
        if (stopOther) {
            Controller.StopSpin();
            Controller.StopPanning();
        }

        if (Camera is IPerspectiveCameraModel) {
            if (!isTouch) {
                if (delta < -0.5) delta = -0.5;
                delta *= ZoomSensitivity;
            }

            if (CameraMode == CameraMode.FixedPosition || changeFieldOfView)
                Viewport.ZoomByChangingFieldOfView(delta);
            else
                switch (CameraMode) {
                    case CameraMode.Inspect:
                        ChangeCameraDistance(ref delta, zoomAround);
                        break;
                    case CameraMode.WalkAround:
                        Camera.Position -= Camera.LookDirection * delta;
                        break;
                }
        } else if (Camera is IOrthographicCameraModel) {
            switch (CameraMode) {
                case CameraMode.WalkAround:
                    Camera.Position -= Camera.LookDirection * delta;
                    break;
                default:
                    ZoomByChangingCameraWidth(delta, zoomAround);
                    break;
            }
        }
    }

    /// <summary>
    ///     Changes the camera position by the specified vector.
    /// </summary>
    /// <param name="delta">
    ///     The translation vector in camera space (z in look direction, y in up direction, and x perpendicular
    ///     to the two others)
    /// </param>
    /// <param name="stopOther">Stop other manipulation</param>
    public void MoveCameraPosition(Vector3 delta, bool stopOther = true) {
        if (stopOther) {
            Controller.StopPanning();
            Controller.StopSpin();
        }

        var z = SilkMath.Normalize(Camera.CameraInternal.LookDirection);
        var x = SilkMath.Cross(Camera.CameraInternal.LookDirection, Camera.CameraInternal.UpDirection);
        var y = SilkMath.Normalize(SilkMath.Cross(x, z));
        x = SilkMath.Cross(z, y);

        // delta *= this.ZoomSensitivity;
        switch (CameraMode) {
            case CameraMode.Inspect:
            case CameraMode.WalkAround:
                Camera.Position += (x * delta.X + y * delta.Y + z * delta.Z).ToVector3D();
                break;
        }
    }

    /// <summary>
    ///     The change camera width.
    /// </summary>
    /// <param name="delta">
    ///     The delta.
    /// </param>
    /// <param name="zoomAround">
    ///     The zoom around.
    /// </param>
    public void ZoomByChangingCameraWidth(double delta, Vector3 zoomAround) {
        if (delta < -0.5) delta = -0.5;

        if (ChangeCameraDistance(ref delta, zoomAround))
            // Modify the camera width
            if (Camera is IOrthographicCameraModel ocamera)
                ocamera.Width *= Math.Pow(2.5, delta);
    }

    /// <summary>
    ///     Occurs when the command associated with this handler initiates a check to determine whether the command can be
    ///     executed on the command target.
    /// </summary>
    /// <returns>
    ///     True if the execution can continue.
    /// </returns>
    protected override bool CanExecute() {
        if (changeFieldOfView) return Controller.IsChangeFieldOfViewEnabled && Camera is PerspectiveCamera;

        return Controller.IsZoomEnabled;
    }

    /// <summary>
    ///     Gets the cursor for the gesture.
    /// </summary>
    /// <returns>
    ///     A cursor.
    /// </returns>
    protected override Cursor GetCursor() {
        return Controller.ZoomCursor;
    }

    /// <summary>
    ///     Changes the camera distance.
    /// </summary>
    /// <param name="delta">The delta.</param>
    /// <param name="zoomAround">The zoom around point.</param>
    private bool ChangeCameraDistance(ref double delta, Vector3 zoomAround) {
        // Handle the 'zoomAround' point
        var target = Camera.CameraInternal.Position + Camera.CameraInternal.LookDirection;
        var relativeTarget = zoomAround - target;
        var relativePosition = zoomAround - Camera.CameraInternal.Position;
        if (relativePosition.LengthSquared() < 1e-5) {
            if (delta > 0) //If Zoom out from very close distance, increase the initial relativePosition
            {
                relativePosition = SilkMath.Normalize(relativePosition) / 10;
                zoomAround = relativePosition + Camera.CameraInternal.Position;
                relativeTarget = zoomAround - target;
            } else //If Zoom in too close, stop it.
            {
                return false;
            }
        }

        var f = (float)Math.Pow(2.5, delta);
        var newRelativePosition = relativePosition * f;
        var newRelativeTarget = relativeTarget * f;
        var newTarget = zoomAround - newRelativeTarget;
        var newPosition = zoomAround - newRelativePosition;

        var newDistance = (newPosition - zoomAround).Length;
        var oldDistance = (Camera.CameraInternal.Position - zoomAround).Length;

        if (newDistance > Controller.ZoomDistanceLimitFar &&
            (oldDistance < Controller.ZoomDistanceLimitFar || newDistance > oldDistance)) {
            var ratio = (newDistance - (float)Controller.ZoomDistanceLimitFar) / newDistance;
            f *= 1 - ratio;
            newRelativePosition = relativePosition * f;
            newRelativeTarget = relativeTarget * f;

            newTarget = zoomAround - newRelativeTarget;
            newPosition = zoomAround - newRelativePosition;
            delta = Math.Log(f) / Math.Log(2.5);
            // return false;
        }

        if (newDistance < Controller.ZoomDistanceLimitNear &&
            (oldDistance > Controller.ZoomDistanceLimitNear || newDistance < oldDistance)) {
            var ratio = ((float)Controller.ZoomDistanceLimitNear - newDistance) / newDistance;
            f *= 1 + ratio;
            newRelativePosition = relativePosition * f;
            newRelativeTarget = relativeTarget * f;

            newTarget = zoomAround - newRelativeTarget;
            newPosition = zoomAround - newRelativePosition;
            delta = Math.Log(f) / Math.Log(2.5);
            // return false;
        }

        var newLookDirection = newTarget - newPosition;
        if (newLookDirection.LengthSquared() < 1e-5) return false;
        Camera.LookDirection = newLookDirection.ToVector3D();
        Camera.Position = newPosition.ToPoint3D();
        return true;
    }
}
