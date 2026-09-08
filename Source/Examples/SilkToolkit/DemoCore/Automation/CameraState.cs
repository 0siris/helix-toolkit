// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CameraState.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Windows.Media.Media3D;
using HelixToolkit.Wpf.SharpDX.Camera;
using Camera = HelixToolkit.Wpf.SharpDX.Camera.Camera;
using ProjectionCamera = HelixToolkit.Wpf.SharpDX.Camera.ProjectionCamera;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.Camera.PerspectiveCamera;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.Camera.OrthographicCamera;

/// <summary>
///     JSON-serialisierbarer Kamerazustand für REST und MCP. Null-Felder bedeuten "behalten".
/// </summary>
public sealed record CameraState {
    /// <summary>
    ///     Camera type literal for <see cref="PerspectiveCamera" />.
    /// </summary>
    public const string Perspective = "perspective";

    /// <summary>
    ///     Camera type literal for <see cref="OrthographicCamera" />.
    /// </summary>
    public const string Orthographic = "orthographic";

    /// <summary>
    ///     Gets the camera position as [x, y, z].
    /// </summary>
    public double[]? Position { get; init; }

    /// <summary>
    ///     Gets the look direction as [x, y, z].
    /// </summary>
    public double[]? LookDirection { get; init; }

    /// <summary>
    ///     Gets the up direction as [x, y, z].
    /// </summary>
    public double[]? UpDirection { get; init; }

    /// <summary>
    ///     Gets the near plane distance.
    /// </summary>
    public double? NearPlane { get; init; }

    /// <summary>
    ///     Gets the far plane distance.
    /// </summary>
    public double? FarPlane { get; init; }

    /// <summary>
    ///     Gets the field of view in degrees (perspective only).
    /// </summary>
    public double? FieldOfView { get; init; }

    /// <summary>
    ///     Gets the viewport width (orthographic only).
    /// </summary>
    public double? Width { get; init; }

    /// <summary>
    ///     Gets the camera type (<c>perspective</c> or <c>orthographic</c>).
    /// </summary>
    public string? CameraType { get; init; }

    /// <summary>
    ///     Captures the state of a live camera.
    /// </summary>
    /// <param name="camera">The camera to capture.</param>
    /// <returns>The captured state.</returns>
    public static CameraState FromCamera(Camera camera) {
        ArgumentNullException.ThrowIfNull(camera);
        return new CameraState {
            Position = [camera.Position.X, camera.Position.Y, camera.Position.Z],
            LookDirection = [camera.LookDirection.X, camera.LookDirection.Y, camera.LookDirection.Z],
            UpDirection = [camera.UpDirection.X, camera.UpDirection.Y, camera.UpDirection.Z],
            NearPlane = camera is ProjectionCamera projection ? projection.NearPlaneDistance : null,
            FarPlane = camera is ProjectionCamera far ? far.FarPlaneDistance : null,
            FieldOfView = camera is PerspectiveCamera perspective ? perspective.FieldOfView : null,
            Width = camera is OrthographicCamera orthographic ? orthographic.Width : null,
            CameraType = camera is OrthographicCamera ? Orthographic : Perspective,
        };
    }

    /// <summary>
    ///     Applies this patch to a camera. A differing <see cref="CameraType" /> creates a new camera instance.
    /// </summary>
    /// <param name="current">The current camera.</param>
    /// <param name="animationTimeMs">Animation time in milliseconds; zero sets values directly.</param>
    /// <returns>The camera to use (either <paramref name="current" /> or a new instance).</returns>
    public Camera ApplyTo(Camera current, double animationTimeMs = 0) {
        ArgumentNullException.ThrowIfNull(current);
        if (CameraType is { } requested && !IsType(current, requested)) {
            var created = requested switch {
                Perspective => (Camera)new PerspectiveCamera(),
                Orthographic => (Camera)new OrthographicCamera(),
                _ => throw new ArgumentException($"Unknown camera type '{requested}'.", nameof(CameraType)),
            };
            ApplyValues(created, 0);
            return created;
        }

        ApplyValues(current, animationTimeMs);
        return current;
    }

    /// <summary>
    ///     Checks whether a camera matches a type literal.
    /// </summary>
    /// <param name="camera">The camera.</param>
    /// <param name="cameraType">The literal.</param>
    /// <returns>True for a match.</returns>
    public static bool IsType(Camera camera, string cameraType) =>
        cameraType switch {
            Perspective => camera is PerspectiveCamera,
            Orthographic => camera is OrthographicCamera,
            _ => throw new ArgumentException($"Unknown camera type '{cameraType}'.", nameof(cameraType)),
        };

    /// <summary>
    ///     Applies the set fields to a camera of matching type.
    /// </summary>
    /// <param name="target">The target camera.</param>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    private void ApplyValues(Camera target, double animationTimeMs) {
        var position = ToPoint(Position, nameof(Position)) ?? target.Position;
        var direction = ToVector(LookDirection, nameof(LookDirection)) ?? target.LookDirection;
        var up = ToVector(UpDirection, nameof(UpDirection)) ?? target.UpDirection;
        if (animationTimeMs > 0) {
            target.AnimateTo(position, direction, up, animationTimeMs);
        } else {
            target.Position = position;
            target.LookDirection = direction;
            target.UpDirection = up;
        }

        if (target is ProjectionCamera projection) {
            if (NearPlane.HasValue) {
                projection.NearPlaneDistance = NearPlane.Value;
            }

            if (FarPlane.HasValue) {
                projection.FarPlaneDistance = FarPlane.Value;
            }
        }

        if (target is PerspectiveCamera perspective && FieldOfView.HasValue) {
            if (FieldOfView.Value <= 0 || FieldOfView.Value >= 180) {
                throw new ArgumentOutOfRangeException(nameof(FieldOfView), "Field of view must be within (0, 180).");
            }

            perspective.FieldOfView = FieldOfView.Value;
        }

        if (target is OrthographicCamera orthographic && Width.HasValue) {
            if (Width.Value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(Width), "Width must be positive.");
            }

            orthographic.Width = Width.Value;
        }
    }

    /// <summary>
    ///     Converts an optional triple to a point.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <param name="name">The field name for error reporting.</param>
    /// <returns>The point or null.</returns>
    private static Point3D? ToPoint(double[]? values, string name) {
        var vector = ToVector(values, name);
        return vector.HasValue ? new Point3D(vector.Value.X, vector.Value.Y, vector.Value.Z) : null;
    }

    /// <summary>
    ///     Converts an optional triple to a vector.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <param name="name">The field name for error reporting.</param>
    /// <returns>The vector or null.</returns>
    private static Vector3D? ToVector(double[]? values, string name) {
        if (values is null) {
            return null;
        }

        if (values.Length != 3) {
            throw new ArgumentException($"{name} must be a triple [x, y, z].", name);
        }

        return new Vector3D(values[0], values[1], values[2]);
    }
}
