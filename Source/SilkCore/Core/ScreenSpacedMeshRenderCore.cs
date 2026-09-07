/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public interface IScreenSpacedRenderParams {
    /// <summary>
    ///     Relative position X of the center of viewport
    /// </summary>
    float RelativeScreenLocationX { get; set; }

    /// <summary>
    ///     Relative position Y of the center of viewport
    /// </summary>
    float RelativeScreenLocationY { get; set; }

    /// <summary>
    /// </summary>
    float SizeScale { get; set; }

    /// <summary>
    ///     Only being used when <see cref="Mode" /> is RelativeScreenSpaced
    /// </summary>
    ScreenSpacedCameraType CameraType { get; set; }

    bool IsPerspective { get; }

    /// <summary>
    /// </summary>
    float Width { get; }

    /// <summary>
    /// </summary>
    float Height { get; }

    /// <summary>
    /// </summary>
    float Size { get; }

    /// <summary>
    /// </summary>
    float ScreenRatio { get; }

    /// <summary>
    /// </summary>
    float Fov { get; }

    /// <summary>
    /// </summary>
    float CameraDistance { get; }

    /// <summary>
    /// </summary>
    GlobalTransformStruct GlobalTransform { get; }

    /// <summary>
    ///     Gets or sets the mode.
    /// </summary>
    /// <value>
    ///     The mode.
    /// </value>
    ScreenSpacedMode Mode { get; set; }

    /// <summary>
    ///     Gets or sets the absolute position. Used in <see cref="ScreenSpacedMode.AbsolutePosition3D" />
    /// </summary>
    /// <value>
    ///     The absolute position.
    /// </value>
    Vector3 AbsolutePosition3D { get; set; }

    /// <summary>
    ///     Gets or sets the far plane for screen spaced camera rendering.
    /// </summary>
    /// <value>
    ///     The far plane.
    /// </value>
    float FarPlane { get; set; }

    /// <summary>
    ///     Gets or sets the near plane for screen spaced camera rendering.
    /// </summary>
    /// <value>
    ///     The near plane.
    /// </value>
    float NearPlane { get; set; }

    event EventHandler<BoolArgs>? OnCoordinateSystemChanged;
}

/// <summary>
///     Used to change view matrix and projection matrix to screen spaced coordinate system.
///     <para>
///         The Direct3D 12 renderer applies screen-space coordinates around the nested model draw.
///     </para>
/// </summary>
public class ScreenSpacedMeshRenderCore : RenderCore, IScreenSpacedRenderParams {
    private Vector3 absolutePosition;

    private bool isMainCameraPerspective;

    private ScreenSpacedMode mode = ScreenSpacedMode.RelativeScreenSpaced;
    private Matrix projectionMatrix;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScreenSpacedMeshRenderCore" /> class.
    /// </summary>
    public ScreenSpacedMeshRenderCore() : base(RenderType.ScreenSpaced) { }

    /// <summary>
    /// </summary>
    public bool IsRightHand {
        get;
        private set {
            if (Set(ref field, value))
                OnCoordinateSystemChanged?.Invoke(this, value ? BoolArgs.TrueArgs : BoolArgs.FalseArgs);
        }
    } = true;

    public event EventHandler<BoolArgs>? OnCoordinateSystemChanged;

    public GlobalTransformStruct GlobalTransform { get; private set; }

    public float ScreenRatio { get; private set; } = 1f;

    /// <summary>
    ///     Relative position X of the center of viewport
    /// </summary>
    public float RelativeScreenLocationX {
        get;
        set => SetAffectsRender(ref field, value);
    } = -0.8f;

    /// <summary>
    ///     Relative position Y of the center of viewport
    /// </summary>
    public float RelativeScreenLocationY {
        get;
        set => SetAffectsRender(ref field, value);
    } = -0.8f;

    public ScreenSpacedMode Mode {
        get => mode;
        set => SetAffectsRender(ref mode, value);
    }

    public Vector3 AbsolutePosition3D {
        get => absolutePosition;
        set => SetAffectsRender(ref absolutePosition, value);
    }

    /// <summary>
    ///     Size scaling
    /// </summary>
    public float SizeScale {
        get;
        set => SetAffectsRender(ref field, value);
    } = 1;

    /// <summary>
    ///     Only being used when <see cref="Mode" /> is RelativeScreenSpaced
    /// </summary>
    public ScreenSpacedCameraType CameraType {
        get;
        set => SetAffectsRender(ref field, value);
    } = ScreenSpacedCameraType.Auto;

    /// <summary>
    ///     Viewport Width
    /// </summary>
    public float Width { get; private set; }

    /// <summary>
    ///     Viewport Height
    /// </summary>
    public float Height { get; private set; }

    /// <summary>
    ///     Default size. To scale, use <see cref="SizeScale" />
    /// </summary>
    public float Size { get; } = 100;

    /// <summary>
    /// </summary>
    public float CameraDistance { get; } = 20;

    /// <summary>
    ///     Fov in radian
    /// </summary>
    public float Fov { get; } = (float)(45 * Math.PI / 180);

    /// <summary>
    ///     Gets the near plane.
    /// </summary>
    /// <value>
    ///     The near plane.
    /// </value>
    public float NearPlane { get; set; } = 1e-2f;

    /// <summary>
    ///     Gets the far plane.
    /// </summary>
    /// <value>
    ///     The far plane.
    /// </value>
    public float FarPlane { get; set; } = 1e3f;

    public bool IsPerspective { get; private set; }

    /// <summary>
    ///     Creates the view matrix.
    /// </summary>
    /// <param name="renderContext">The render context.</param>
    /// <param name="eye">The eye.</param>
    /// <returns></returns>
    protected Matrix CreateViewMatrix(RenderContext renderContext, out Vector3 eye) {
        if (renderContext.Camera is not { } camera) {
            eye = Vector3.Zero;
            return Matrix.Identity;
        }

        eye = -camera.LookDirection.Normalized() * CameraDistance;
        if (IsRightHand) return SilkMath.LookAtRh(eye, Vector3.Zero, camera.UpDirection);

        return SilkMath.LookAtLh(eye, Vector3.Zero, camera.UpDirection);
    }

    /// <summary>
    ///     Called when [create projection matrix].
    /// </summary>
    protected virtual void OnCreateProjectionMatrix(RenderContext context) {
        var isPerspective = false;
        switch (CameraType) {
            case ScreenSpacedCameraType.Auto:
                isPerspective = context.IsPerspective;
                break;
            case ScreenSpacedCameraType.Perspective:
                isPerspective = true;
                break;
            case ScreenSpacedCameraType.Orthographic:
                break;
        }

        IsPerspective = isPerspective;
        switch (mode) {
            case ScreenSpacedMode.AbsolutePosition3D:
                if (IsPerspective)
                    if (context.Camera is { } camera)
                        projectionMatrix = camera.CreateProjectionMatrix(context.ActualWidth / context.ActualHeight,
                                                                         NearPlane,
                                                                         FarPlane);
                    else
                        return;
                else
                    //projectionMatrix = context.ProjectionMatrix;
                    projectionMatrix = CreateProjectionMatrix(context.IsPerspective,
                                                              IsRightHand,
                                                              Fov,
                                                              NearPlane,
                                                              FarPlane,
                                                              CameraDistance,
                                                              CameraDistance);
                break;
            case ScreenSpacedMode.RelativeScreenSpaced:
                projectionMatrix = CreateProjectionMatrix(isPerspective,
                                                          IsRightHand,
                                                          Fov,
                                                          NearPlane,
                                                          FarPlane,
                                                          CameraDistance,
                                                          CameraDistance);
                break;
        }
    }

    private static Matrix CreateProjectionMatrix(
        bool isPerspective,
        bool isRightHand,
        float fov,
        float near,
        float far,
        float w,
        float h
    ) {
        if (isPerspective)
            return isRightHand
                       ? SilkMath.PerspectiveFovRh(fov, w / h, near, far)
                       : SilkMath.PerspectiveFovLh(fov, w / h, near, far);

        return isRightHand ? SilkMath.OrthoRh(w, h, near, far) : SilkMath.OrthoLh(w, h, near, far);
    }

    protected void UpdateParameters(RenderContext context, float width, float height) {
        var ratio = width / height;
        if (ScreenRatio != ratio || Width != width || Height != height ||
            isMainCameraPerspective != context.IsPerspective) {
            ScreenRatio = ratio;
            Width = width;
            Height = height;
            isMainCameraPerspective = context.IsPerspective;
            OnCreateProjectionMatrix(context);
        }
    }

    /// <summary>
    ///     Creates the transform and viewport used to draw this screen-spaced group's children through Direct3D 12.
    /// </summary>
    /// <param name="camera">The main viewport camera whose orientation is mirrored by the overlay camera.</param>
    /// <param name="mainTransform">The main viewport transform payload.</param>
    /// <param name="width">The physical render-target width.</param>
    /// <param name="height">The physical render-target height.</param>
    /// <param name="dpiScale">The logical-to-physical pixel scale.</param>
    /// <param name="transform">The resulting overlay transform payload.</param>
    /// <param name="viewport">The resulting overlay viewport.</param>
    /// <returns><see langword="true" /> when the render target is large enough for the overlay.</returns>
    internal bool TryCreateD3D12Transform(CameraCore camera,
        in GlobalTransformStruct mainTransform,
        float width,
        float height,
        float dpiScale,
        out GlobalTransformStruct transform,
        out ViewportF viewport) {
        transform = mainTransform;
        viewport = default;
        if (width < Size || height < Size) return false;

        IsRightHand = !camera.CreateLeftHandSystem;
        Width = width;
        Height = height;
        ScreenRatio = width / height;
        isMainCameraPerspective = mainTransform.IsPerspective;
        IsPerspective = CameraType switch {
            ScreenSpacedCameraType.Perspective => true,
            ScreenSpacedCameraType.Orthographic => false,
            _ => mainTransform.IsPerspective
        };
        projectionMatrix = Mode == ScreenSpacedMode.AbsolutePosition3D && IsPerspective
            ? camera.CreateProjectionMatrix(width / height, NearPlane, FarPlane)
            : CreateProjectionMatrix(IsPerspective,
                IsRightHand,
                Fov,
                NearPlane,
                FarPlane,
                CameraDistance,
                CameraDistance);

        if (Mode == ScreenSpacedMode.AbsolutePosition3D && mainTransform.IsPerspective) {
            var distance = Size / 2 / SizeScale;
            var viewInverse = mainTransform.View.PsudoInvert();
            var direction = SilkMath.Normalize(AbsolutePosition3D - mainTransform.EyePos);
            var position = AbsolutePosition3D - direction * distance - AbsolutePosition3D;
            viewInverse.M41 = position.X;
            viewInverse.M42 = position.Y;
            viewInverse.M43 = position.Z;
            transform.View = viewInverse.PsudoInvert();
            transform.EyePos = position;
            transform.Projection = projectionMatrix;
            transform.ViewProjection = transform.View * transform.Projection;
            viewport = new ViewportF(0, 0, width, height);
        } else {
            transform.View = CreateViewMatrix(camera, out transform.EyePos);
            transform.Projection = projectionMatrix;
            transform.ViewProjection = transform.View * transform.Projection;
            var viewportSize = Size * SizeScale * dpiScale;
            float offsetX;
            float offsetY;
            if (Mode == ScreenSpacedMode.AbsolutePosition3D) {
                var viewportMatrix = new Matrix(width / 2, 0, 0, 0,
                    0, -(height / 2), 0, 0,
                    0, 0, 1, 0,
                    (width - 1) / 2, (height - 1) / 2, 0, 1);
                var screenPoint = SilkMath.TransformCoordinate(AbsolutePosition3D,
                    mainTransform.ViewProjection * viewportMatrix);
                offsetX = screenPoint.X - viewportSize / 2;
                offsetY = screenPoint.Y - viewportSize / 2;
            } else {
                offsetX = Math.Max(0,
                    Math.Min(width / 2 * (1 + RelativeScreenLocationX) - viewportSize / 2,
                        width - viewportSize));
                offsetY = Math.Max(0,
                    Math.Min(height / 2 * (1 - RelativeScreenLocationY) - viewportSize / 2,
                        height - viewportSize));
            }
            transform.Viewport = new Vector4(viewportSize,
                viewportSize,
                1 / viewportSize,
                1 / viewportSize);
            viewport = new ViewportF(offsetX, offsetY, viewportSize, viewportSize);
        }

        GlobalTransform = transform;
        return true;
    }

    /// <summary>
    ///     Creates the fixed overlay camera view while preserving the main camera orientation.
    /// </summary>
    /// <param name="camera">The main viewport camera.</param>
    /// <param name="eye">The resulting overlay eye position.</param>
    /// <returns>The overlay view matrix.</returns>
    private Matrix CreateViewMatrix(CameraCore camera, out Vector3 eye) {
        eye = -camera.LookDirection.Normalized() * CameraDistance;
        return IsRightHand
            ? SilkMath.LookAtRh(eye, Vector3.Zero, camera.UpDirection)
            : SilkMath.LookAtLh(eye, Vector3.Zero, camera.UpDirection);
    }
}
