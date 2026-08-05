namespace HelixToolkit.SharpDX.Core.Cameras;

public static class CameraMath {
    /// <summary>
    ///     Rotates the trackball.
    /// </summary>
    /// <param name="cameraMode">The camera mode.</param>
    /// <param name="p1">The p1.</param>
    /// <param name="p2">The p2.</param>
    /// <param name="rotateAround">The rotate around.</param>
    /// <param name="sensitivity">The sensitivity.</param>
    /// <param name="viewportWidth">Width of the viewport.</param>
    /// <param name="viewportHeight">Height of the viewport.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="invertFactor">The invert factor. Right Handed System = 1; LeftHandedSystem = -1;</param>
    /// <param name="newPosition">The new position.</param>
    /// <param name="newLookDirection">The new look direction.</param>
    /// <param name="newUpDirection">The new up direction.</param>
    public static void RotateTrackball(
        CameraMode cameraMode,
        ref Vector2 p1,
        ref Vector2 p2,
        ref Vector3 rotateAround,
        float sensitivity,
        int viewportWidth,
        int viewportHeight,
        CameraCore camera,
        int invertFactor,
        out Vector3 newPosition,
        out Vector3 newLookDirection,
        out Vector3 newUpDirection
    ) {
        // http://viewport3d.com/trackball.htm
        // http://www.codeplex.com/3DTools/Thread/View.aspx?ThreadId=22310
        var v1 = ProjectToTrackball(p1, viewportWidth, viewportHeight);
        var v2 = ProjectToTrackball(p2, viewportWidth, viewportHeight);
        var cUP = SilkMath.Normalize(camera.UpDirection);
        // transform the trackball coordinates to view space
        var viewZ = SilkMath.Normalize(camera.LookDirection * invertFactor);
        var viewX = SilkMath.Normalize(SilkMath.Cross(cUP, viewZ)) * invertFactor;
        var viewY = SilkMath.Cross(viewX, viewZ);
        var u1 = viewZ * v1.Z + viewX * v1.X + viewY * v1.Y;
        var u2 = viewZ * v2.Z + viewX * v2.X + viewY * v2.Y;

        // Could also use the Camera ViewMatrix
        // var vm = Viewport3DHelper.GetViewMatrix(this.ActualCamera);
        // vm.Invert();
        // var ct = new MatrixTransform3D(vm);
        // var u1 = ct.Transform(v1);
        // var u2 = ct.Transform(v2);

        // Find the rotation axis and angle
        var axis = SilkMath.Cross(u1, u2);
        if (axis.LengthSquared() < 1e-8) {
            newPosition = camera.Position;
            newLookDirection = camera.LookDirection;
            newUpDirection = camera.UpDirection;
            return;
        }

        var angle = u1.AngleBetween(u2);

        // Create the transform
        var rotate = SilkMath.RotationAxis(SilkMath.Normalize(axis), -angle * sensitivity * 5);

        // Find vectors relative to the rotate-around point
        var relativeTarget = rotateAround - camera.Target;
        var relativePosition = rotateAround - camera.Position;

        // Rotate the relative vectors
        var newRelativeTarget = SilkMath.TransformCoordinate(relativeTarget, rotate);
        var newRelativePosition = SilkMath.TransformCoordinate(relativePosition, rotate);
        newUpDirection = SilkMath.TransformNormal(cUP, rotate);

        // Find new camera position
        var newTarget = rotateAround - newRelativeTarget;
        newPosition = rotateAround - newRelativePosition;

        newLookDirection = newTarget - newPosition;
        if (cameraMode != CameraMode.Inspect) newPosition = camera.Position;
    }

    /// <summary>
    ///     Projects a screen position to the trackball unit sphere.
    /// </summary>
    /// <param name="point">
    ///     The screen position.
    /// </param>
    /// <param name="w">
    ///     The width of the viewport.
    /// </param>
    /// <param name="h">
    ///     The height of the viewport.
    /// </param>
    /// <returns>
    ///     A trackball coordinate.
    /// </returns>
    private static Vector3 ProjectToTrackball(Vector2 point, double w, double h) {
        // Use the diagonal for scaling, making sure that the whole client area is inside the trackball
        var r = Math.Sqrt(w * w + h * h) / 2;
        var x = (point.X - w / 2) / r;
        var y = (h / 2 - point.Y) / r;
        var z2 = 1 - x * x - y * y;
        var z = z2 > 0 ? Math.Sqrt(z2) : 0;

        return new Vector3((float) x, (float) y, (float) z);
    }

    /// <summary>
    ///     Rotates around three axes.
    /// </summary>
    /// <param name="cameraMode">The camera mode.</param>
    /// <param name="p1">The p1.</param>
    /// <param name="p2">The p2.</param>
    /// <param name="rotateAround">The rotate around.</param>
    /// <param name="sensitivity">The sensitivity.</param>
    /// <param name="viewportWidth">Width of the viewport.</param>
    /// <param name="viewportHeight">Height of the viewport.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="invertFactor">The invert factor. Right Handed = 1; Left Handed = -1</param>
    /// <param name="newPosition">The new position.</param>
    /// <param name="newLookDirection">The new look direction.</param>
    /// <param name="newUpDirection">The new up direction.</param>
    public static void RotateTurnball(
        CameraMode cameraMode,
        ref Vector2 p1,
        ref Vector2 p2,
        ref Vector3 rotateAround,
        float sensitivity,
        int viewportWidth,
        int viewportHeight,
        CameraCore camera,
        int invertFactor,
        out Vector3 newPosition,
        out Vector3 newLookDirection,
        out Vector3 newUpDirection
    ) {
        InitTurnballRotationAxes(p1,
                                 viewportWidth,
                                 viewportHeight,
                                 camera,
                                 out var rotationAxisX,
                                 out var rotationAxisY);

        var delta = p2 - p1;

        var relativeTarget = rotateAround - camera.Target;
        var relativePosition = rotateAround - camera.Position;

        float d = -1;
        if (cameraMode != CameraMode.Inspect) d = 0.2f;

        d *= sensitivity;

        var q1 = SilkMath.QuaternionRotationAxis(rotationAxisX,
                                                 d * invertFactor * delta.X / 180 * (float) Math.PI);
        var q2 = SilkMath.QuaternionRotationAxis(rotationAxisY, d * delta.Y / 180 * (float) Math.PI);
        var q = q1 * q2;

        var m = SilkMath.RotationQuaternion(q);
        var newLookDir = SilkMath.TransformNormal(SilkMath.Normalize(camera.LookDirection), m);
        newUpDirection = SilkMath.TransformNormal(SilkMath.Normalize(camera.UpDirection), m);

        var newRelativeTarget = SilkMath.TransformCoordinate(relativeTarget, m);
        var newRelativePosition = SilkMath.TransformCoordinate(relativePosition, m);

        var newRightVector = SilkMath.Normalize(SilkMath.Cross(newLookDir, newUpDirection));
        var modUpDir = SilkMath.Cross(newRightVector, newLookDir);
        if ((newUpDirection - modUpDir).Length > 1e-8) newUpDirection = modUpDir;

        var newTarget = rotateAround - newRelativeTarget;
        newPosition = rotateAround - newRelativePosition;
        newLookDirection = newTarget - newPosition;

        if (cameraMode != CameraMode.Inspect) newPosition = camera.Position;
    }

    /// <summary>
    ///     Initializes the 'turn-ball' rotation axes from the specified point.
    /// </summary>
    /// <param name="p1">
    ///     The point.
    /// </param>
    /// <param name="camera"></param>
    /// <param name="rotationAxisX"></param>
    /// <param name="rotationAxisY"></param>
    /// <param name="viewportHeight"></param>
    /// <param name="viewportWidth"></param>
    public static void InitTurnballRotationAxes(
        Vector2 p1,
        int viewportWidth,
        int viewportHeight,
        CameraCore camera,
        out Vector3 rotationAxisX,
        out Vector3 rotationAxisY
    ) {
        double fx = p1.X / viewportWidth;
        double fy = p1.Y / viewportHeight;

        var up = SilkMath.Normalize(camera.UpDirection);
        var dir = SilkMath.Normalize(camera.LookDirection);

        var right = SilkMath.Normalize(SilkMath.Cross(dir, up));

        rotationAxisX = up;
        rotationAxisY = right;

        if (fx > 0.8)
            // delta.X = 0;
            rotationAxisY = dir;

        if (fx < 0.2)
            // delta.X = 0;
            rotationAxisY = -dir;
    }

    /// <summary>
    ///     Rotates using turntable.
    /// </summary>
    /// <param name="cameraMode">The camera mode.</param>
    /// <param name="delta">The delta.</param>
    /// <param name="rotateAround">The rotate around.</param>
    /// <param name="sensitivity">The sensitivity.</param>
    /// <param name="viewportWidth">Width of the viewport.</param>
    /// <param name="viewportHeight">Height of the viewport.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="invertFactor">The invert factor. Right Handed = 1; Left Handed = -1;</param>
    /// <param name="modelUpDirection">The model up direction.</param>
    /// <param name="newPosition">The new position.</param>
    /// <param name="newLookDirection">The new look direction.</param>
    /// <param name="newUpDirection">The new up direction.</param>
    public static void RotateTurntable(
        CameraMode cameraMode,
        ref Vector2 delta,
        ref Vector3 rotateAround,
        float sensitivity,
        int viewportWidth,
        int viewportHeight,
        CameraCore camera,
        int invertFactor,
        Vector3 modelUpDirection,
        out Vector3 newPosition,
        out Vector3 newLookDirection,
        out Vector3 newUpDirection
    ) {
        var relativeTarget = rotateAround - camera.Target;
        var relativePosition = rotateAround - camera.Position;
        var cUp = SilkMath.Normalize(camera.UpDirection);
        var up = modelUpDirection;
        var dir = SilkMath.Normalize(camera.LookDirection);
        var right = SilkMath.Normalize(SilkMath.Cross(dir, cUp));

        var d = -0.5f;
        if (cameraMode != CameraMode.Inspect) d *= -0.2f;

        d *= sensitivity;

        var q1 = SilkMath.QuaternionRotationAxis(up, d * invertFactor * delta.X / 180 * (float) Math.PI);
        var q2 = SilkMath.QuaternionRotationAxis(right, d * delta.Y / 180 * (float) Math.PI);
        var q = q1 * q2;

        var m = SilkMath.RotationQuaternion(q);

        newUpDirection = SilkMath.TransformNormal(cUp, m);

        var newRelativeTarget = SilkMath.TransformCoordinate(relativeTarget, m);
        var newRelativePosition = SilkMath.TransformCoordinate(relativePosition, m);

        var newTarget = rotateAround - newRelativeTarget;
        newPosition = rotateAround - newRelativePosition;

        newLookDirection = newTarget - newPosition;
        if (cameraMode != CameraMode.Inspect) newPosition = camera.Position;
    }
}
