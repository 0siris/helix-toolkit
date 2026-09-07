using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Extensions;

/// <summary>
/// </summary>
public static class IViewportExtensions {
    public static readonly HitTestResult[] EmptyHits = [];

    /// <summary>
    ///     Stores the traversal stack for the current thread.
    /// </summary>
    [ThreadStatic] private static Stack<IEnumerator<SceneNode>>? stackCache;

    /// <summary>
    ///     Gets the traversal stack for the current thread.
    /// </summary>
    private static Stack<IEnumerator<SceneNode>> StackCache => stackCache ??= new();

    /// <summary>
    ///     Creates the current camera and viewport matrices used by hit testing and projection.
    /// </summary>
    /// <param name="viewport">The source viewport.</param>
    /// <returns>The current render matrices.</returns>
    internal static IRenderMatrices CreateRenderMatrices(IViewport3DX viewport) => new ViewportRenderMatrices(viewport);

    /// <summary>
    ///     Forces to update transform and bounds.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    public static void ForceUpdateTransformsAndBounds(this IViewport3DX viewport) {
        viewport.Renderables.ForceUpdateTransformsAndBounds();
    }

    /// <summary>
    ///     Finds the hits in camera view frustum only.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="pos">The position.</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    public static bool FindHitsInFrustum(this IViewport3DX viewport, Vector2 pos, ref List<HitTestResult> hits) {
        hits.Clear();
        if (viewport.UnProject(pos, out var ray)) {
            var matrices = new ViewportRenderMatrices(viewport);
            var hitContext = new HitTestContext(matrices, ref ray, ref pos);
            foreach (var element in viewport.Renderables) element.HitTest(hitContext, ref hits);
            hits.Sort();
            return hits.Count > 0;
        }

        return false;
    }

    /// <summary>
    ///     Finds the hits for a given 2D viewport position.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    /// <param name="position">
    ///     The position.
    /// </param>
    /// <returns>
    ///     List of hits, sorted with the nearest hit first.
    /// </returns>
    public static IList<HitTestResult> FindHits(this IViewport3DX viewport, Vector2 position) {
        var hits = new List<HitTestResult>();
        if (viewport.FindHits(position, ref hits)) return hits;

        return EmptyHits;
    }

    /// <summary>
    ///     Finds the hits.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="position">The position.</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    public static bool FindHits(this IViewport3DX viewport, Vector2 position, ref List<HitTestResult> hits) {
        hits.Clear();
        if (!viewport.UnProject(position, out var ray)) return false;
        var matrices = new ViewportRenderMatrices(viewport);
        var hitContext = new HitTestContext(matrices, ref ray, ref position);
        foreach (var element in viewport.Renderables) element.HitTest(hitContext, ref hits);
        hits.Sort();

        return hits.Count > 0;
    }

    /// <summary>
    ///     Finds the nearest point and its normal.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    /// <param name="position">
    ///     The position.
    /// </param>
    /// <param name="point">
    ///     The point.
    /// </param>
    /// <param name="normal">
    ///     The normal.
    /// </param>
    /// <param name="model">
    ///     The model.
    /// </param>
    /// <returns>
    ///     The find nearest.
    /// </returns>
    public static bool FindNearest(
        this IViewport3DX viewport,
        Vector2 position,
        out Vector3 point,
        out Vector3 normal,
        out object? model
    ) {
        point = new Vector3();
        normal = new Vector3();
        model = null;
        var hits = new List<HitTestResult>();
        if (viewport.FindHitsInFrustum(position, ref hits) && hits.Count > 0) {
            point = hits[0].PointHit;
            normal = hits[0].NormalAtHit;
            model = hits[0].ModelHit;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Un-project 2D screen point onto 3D space by camera.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="point2D">The point2d.</param>
    /// <param name="ray">The ray.</param>
    /// <returns></returns>
    public static bool UnProject(this IViewport3DX viewport, Vector2 point2D, out Ray ray) {
        var matrices = new ViewportRenderMatrices(viewport);
        return matrices.UnProject(point2D, out ray);
    }

    /// <summary>
    ///     Uns the project 2D point onto a 3D plane.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="p">The p.</param>
    /// <param name="position">The position.</param>
    /// <param name="normal">The normal.</param>
    /// <param name="intersection">The intersection.</param>
    /// <returns></returns>
    public static bool UnProjectOnPlane(
        this IViewport3DX viewport,
        Vector2 p,
        Vector3 position,
        Vector3 normal,
        out Vector3 intersection
    ) {
        if (viewport.UnProject(p, out var ray)) return ray.PlaneIntersection(position, normal, out intersection);

        intersection = Vector3.Zero;
        return false;
    }

    /// <summary>
    ///     Uns the project on plane.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="p">The p.</param>
    /// <param name="position">The position.</param>
    /// <param name="normal">The normal.</param>
    /// <returns></returns>
    public static Vector3? UnProjectOnPlane(this IViewport3DX viewport, Vector2 p, Vector3 position, Vector3 normal) {
        if (viewport.UnProjectOnPlane(p, position, normal, out var intersection)) return intersection;

        return null;
    }

    /// <summary>
    ///     Gets the viewport transform aka the screen-space transform.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    /// <returns>
    ///     The transform.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix GetViewportMatrix(this IViewport3DX viewport) => new(viewport.ViewportRectangle.Width / 2f,
        0,
        0,
        0,
        0,
        -viewport.ViewportRectangle.Height / 2f,
        0,
        0,
        0,
        0,
        1,
        0,
        (viewport.ViewportRectangle.Width - 1) / 2f,
        (viewport.ViewportRectangle.Height - 1) / 2f,
        0,
        1);

    /// <summary>
    ///     Gets the total transform for a ViewportCore.
    ///     Old name of this function: GetTotalTransform
    ///     New name of the function: GetScreenViewProjectionTransform
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <returns>The total transform.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix GetScreenViewProjectionMatrix(this IViewport3DX viewport) => viewport.GetViewProjectionMatrix() * viewport.GetViewportMatrix();

    /// <summary>
    ///     Projects the specified 3D point to a 2D screen point.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="point">The 3D point.</param>
    /// <returns>The point.</returns>
    public static Vector2 Project(this IViewport3DX viewport, Vector3 point) {
        return new ViewportRenderMatrices(viewport).Project(point);
    }

    /// <summary>
    ///     Gets the camera transform.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    /// <returns>
    ///     The camera transform.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix GetViewProjectionMatrix(this IViewport3DX viewport) {
        var matrices = new ViewportRenderMatrices(viewport);
        return matrices.ViewMatrix * matrices.ProjectionMatrix;
    }

    /// <summary>
    ///     Gets the projection matrix.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix GetProjectionMatrix(this IViewport3DX viewport) {
        return new ViewportRenderMatrices(viewport).ProjectionMatrix;
    }

    /// <summary>
    ///     Traverses the Visual3D/Element3D tree and invokes the specified action on each Element3D of the specified type.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport.
    /// </param>
    /// <param name="action">
    ///     The action.
    /// </param>
    public static IEnumerable<SceneNode> Traverse(this IViewport3DX viewport, Action<SceneNode> action) {
        return viewport.Renderables.PreorderDft(node => {
                action(node);
                return true;
            },
            StackCache);
    }

    /// <summary>
    ///     Traverses the specified action.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="function">The function. Return true to continue traverse, otherwise stop at current node</param>
    public static IEnumerable<SceneNode> Traverse(this IViewport3DX viewport, Func<SceneNode, bool> function) 
        => viewport.Renderables.PreorderDft(function, StackCache);

    /// <summary>
    ///     Finds the bounding box of the viewport.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <returns>The bounding box.</returns>
    public static BoundingBox FindBounds(this IViewport3DX viewport) {
        viewport.ForceUpdateTransformsAndBounds();
        return viewport.FindBoundsInternal();
    }

    public static BoundingBox FindBoundsInternal(this IViewport3DX viewport) {
        var maxVector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var firstModel = viewport.Renderables.PreorderDft(r => {
                if (r.Visible && !(r is ScreenSpacedNode)) return true;
                return false;
            })
            .Where(x => {
                if (x is IBoundable b)
                    return b.HasBound && b.BoundsWithTransform.Maximum != b.BoundsWithTransform.Minimum
                                      && b.BoundsWithTransform.Maximum != Vector3.Zero &&
                                      b.BoundsWithTransform.Maximum != maxVector;

                return false;
            })
            .FirstOrDefault();
        if (firstModel == null) return new BoundingBox();
        var bounds = firstModel.BoundsWithTransform;

        foreach (var renderable in viewport.Renderables.PreorderDft(r => {
                     if (r.Visible && !(r is ScreenSpacedNode)) return true;
                     return false;
                 }))
            if (renderable is IBoundable r)
                if (r.HasBound && r.BoundsWithTransform.Maximum != maxVector)
                    bounds = BoundingBox.Merge(bounds, r.BoundsWithTransform);

        return bounds;
    }

    /// <summary>
    /// Supplies camera matrices for viewport projection and hit testing without a legacy render host.
    /// </summary>
    private sealed class ViewportRenderMatrices : IRenderMatrices {
        /// <summary>
        /// Initializes matrices from the current viewport camera and dimensions.
        /// </summary>
        /// <param name="viewport">The source viewport.</param>
        public ViewportRenderMatrices(IViewport3DX viewport) {
            var camera = viewport.CameraCore.AssertNotNull("Camera must be initialized.").Value;
            ActualWidth = Math.Max(1, viewport.ViewportRectangle.Width);
            ActualHeight = Math.Max(1, viewport.ViewportRectangle.Height);
            ViewMatrix = camera.CreateViewMatrix();
            ViewMatrixInv = ViewMatrix.PsudoInvert();
            ProjectionMatrix = camera.CreateProjectionMatrix(ActualWidth / ActualHeight);
            ViewportMatrix = viewport.GetViewportMatrix();
            ScreenViewProjectionMatrix = ViewMatrix * ProjectionMatrix * ViewportMatrix;
            CameraParams = camera.CreateCameraParams(ActualWidth / ActualHeight);
            IsPerspective = !new BoundingFrustum(ViewMatrix * ProjectionMatrix).IsOrthographic;
        }

        /// <inheritdoc />
        public Matrix ViewMatrix { get; }

        /// <inheritdoc />
        public Matrix ViewMatrixInv { get; }

        /// <inheritdoc />
        public Matrix ProjectionMatrix { get; }

        /// <inheritdoc />
        public Matrix ViewportMatrix { get; }

        /// <inheritdoc />
        public Matrix ScreenViewProjectionMatrix { get; }

        /// <inheritdoc />
        public bool IsPerspective { get; }

        /// <inheritdoc />
        public float ActualWidth { get; }

        /// <inheritdoc />
        public float ActualHeight { get; }

        /// <inheritdoc />
        public float DpiScale => 1;

        /// <inheritdoc />
        public FrustumCameraParams CameraParams { get; }

        /// <inheritdoc />
        public void Update() { }
    }
}
