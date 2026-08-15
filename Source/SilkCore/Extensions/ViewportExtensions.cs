using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Controls;

namespace HelixToolkit.SharpDX.Core;

public static class ViewportExtensions {
    /// <summary>
    ///     Changes the field of view and tries to keep the scale fixed.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="delta">The delta.</param>
    public static void ZoomByChangingFieldOfView(this CameraController controller, float delta) {
        var viewport = controller.Viewport;
        if (viewport.CameraCore is PerspectiveCameraCore pcamera) {
            var fov = pcamera.FieldOfView;
            var d = pcamera.LookDirection.Length;
            var r = d * (float)Math.Tan(0.5f * fov / 180 * Math.PI);

            fov *= 1f + delta * 0.5f;
            if (fov < controller.MinimumFieldOfView) fov = controller.MinimumFieldOfView;

            if (fov > controller.MaximumFieldOfView) fov = controller.MaximumFieldOfView;

            pcamera.FieldOfView = fov;
            var d2 = r / (float)Math.Tan(0.5f * fov / 180 * Math.PI);
            var newLookDirection = pcamera.LookDirection;
            newLookDirection = newLookDirection.Normalized();
            newLookDirection *= d2;
            var target = pcamera.Position + pcamera.LookDirection;
            pcamera.Position = target - newLookDirection;
            pcamera.LookDirection = newLookDirection;
        }
    }

    /// <summary>
    ///     Zooms the viewport to the specified rectangle.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="rectangle">The rectangle.</param>
    public static void ZoomToRectangle(this ViewportCore viewport, RectangleF rectangle) {
        var camera = viewport.CameraCore.AssertNotNull("Camera must be initialized.");
        camera.ZoomToRectangle(viewport, rectangle);
    }

    /// <summary>
    ///     Zooms to the extents of the specified viewport.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="animationTime">The animation time.</param>
    public static void ZoomExtents(this ViewportCore viewport, float animationTime = 0) {
        var bounds = viewport.FindBounds();
        var diagonal = bounds.Maximum - bounds.Minimum;

        if (diagonal.LengthSquared == 0) return;
        var camera = viewport.CameraCore.AssertNotNull("Camera must be initialized.");
        camera.ZoomExtents(viewport, bounds, animationTime);
    }

    /// <summary>
    ///     Zooms to the extents of the specified bounding box.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="bounds">The bounding rectangle.</param>
    /// <param name="animationTime">The animation time.</param>
    public static void ZoomExtents(this ViewportCore viewport, BoundingBox bounds, float animationTime = 0) {
        var camera = viewport.CameraCore.AssertNotNull("Camera must be initialized.");
        camera.ZoomExtents(viewport, bounds, animationTime);
    }

    /// <summary>
    ///     Zooms to the extents of the specified bounding sphere.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="animationTime">The animation time.</param>
    public static void ZoomExtents(this ViewportCore viewport, Vector3 center, float radius, float animationTime = 0) {
        var camera = viewport.CameraCore.AssertNotNull("Camera must be initialized.");
        camera.ZoomExtents(viewport, center, radius, animationTime);
    }
}
