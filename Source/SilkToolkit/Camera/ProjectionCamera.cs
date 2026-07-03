/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Cameras;

namespace HelixToolkit.Wpf.SharpDX;

public interface IProjectionCameraModel : ICameraModel {
    double FarPlaneDistance { get; set; }

    double NearPlaneDistance { get; set; }
}

/// <summary>
///     An abstract base class for perspective and orthographic projection cameras.
/// </summary>
public abstract class ProjectionCamera : Camera, IProjectionCameraModel {
    /// <summary>
    ///     The create left hand system property
    /// </summary>
    public static readonly DependencyProperty CreateLeftHandSystemProperty =
        DependencyProperty.Register("CreateLeftHandSystem",
                                    typeof(bool),
                                    typeof(ProjectionCamera),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Camera).CameraInternal as ProjectionCameraCore)
                                                                 .CreateLeftHandSystem = (bool) e.NewValue;
                                                         }));

    /// <summary>
    ///     The far plane distance property.
    /// </summary>
    public static readonly DependencyProperty FarPlaneDistanceProperty =
        DependencyProperty.Register("FarPlaneDistance",
                                    typeof(double),
                                    typeof(ProjectionCamera),
                                    new PropertyMetadata(1e3,
                                                         (d, e) => {
                                                             ((d as Camera).CameraInternal as ProjectionCameraCore)
                                                                 .FarPlaneDistance =
                                                                 (float) (double) e.NewValue;
                                                         }));

    /// <summary>
    ///     The look direction property
    /// </summary>
    public static readonly DependencyProperty LookDirectionProperty = DependencyProperty.Register("LookDirection",
        typeof(Vector3D),
        typeof(ProjectionCamera),
        new PropertyMetadata(new Vector3D(0, 0, -5),
                             (d, e) => {
#if NETFX_CORE|| WINUI
                ((d as Camera).CameraInternal as ProjectionCameraCore).LookDirection = (Vector3D)e.NewValue;
#else
                                 ((d as Camera).CameraInternal as ProjectionCameraCore).LookDirection =
                                     ((Vector3D) e.NewValue).ToVector3();
#endif
                             }));

    /// <summary>
    ///     The near plane distance property
    /// </summary>
    public static readonly DependencyProperty NearPlaneDistanceProperty =
        DependencyProperty.Register("NearPlaneDistance",
                                    typeof(double),
                                    typeof(ProjectionCamera),
                                    new PropertyMetadata(0.01,
                                                         (d, e) => {
                                                             ((d as Camera).CameraInternal as ProjectionCameraCore)
                                                                 .NearPlaneDistance =
                                                                 (float) (double) e.NewValue;
                                                         }));

    /// <summary>
    ///     The position property
    /// </summary>
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position",
        typeof(Point3D),
        typeof(ProjectionCamera),
        new PropertyMetadata(new Point3D(0, 0, +5),
                             (d, e) => {
#if NETFX_CORE|| WINUI
                ((d as Camera).CameraInternal as ProjectionCameraCore).Position = (Point3D)e.NewValue;
#else
                                 ((d as Camera).CameraInternal as ProjectionCameraCore).Position =
                                     ((Point3D) e.NewValue).ToVector3();
#endif
                             }));

    /// <summary>
    ///     Up direction property
    /// </summary>
    public static readonly DependencyProperty UpDirectionProperty = DependencyProperty.Register("UpDirection",
        typeof(Vector3D),
        typeof(ProjectionCamera),
        new PropertyMetadata(new Vector3D(0, 1, 0),
                             (d, e) => {
#if NETFX_CORE|| WINUI
                ((d as Camera).CameraInternal as ProjectionCameraCore).UpDirection = (Vector3D)e.NewValue;
#else
                                 ((d as Camera).CameraInternal as ProjectionCameraCore).UpDirection =
                                     ((Vector3D) e.NewValue).ToVector3();
#endif
                             }));

    /// <summary>
    ///     Gets the target position.
    /// </summary>
    /// <value>
    ///     The target.
    /// </value>
    public Point3D Target => Position + LookDirection;

    /// <summary>
    ///     Gets or sets a value indicating whether to create a left hand system.
    /// </summary>
    /// <value>
    ///     <c>true</c> if creating a left hand system; otherwise, <c>false</c>.
    /// </value>
    public override bool CreateLeftHandSystem {
        get => (bool) GetValue(CreateLeftHandSystemProperty);
        set => SetValue(CreateLeftHandSystemProperty, value);
    }

    /// <summary>
    ///     Gets or sets the far plane distance.
    /// </summary>
    /// <value>
    ///     The far plane distance.
    /// </value>
    public double FarPlaneDistance {
        get => (double) GetValue(FarPlaneDistanceProperty);
        set => SetValue(FarPlaneDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the look direction.
    /// </summary>
    /// <value>
    ///     The look direction.
    /// </value>
    public override Vector3D LookDirection {
        get => (Vector3D) GetValue(LookDirectionProperty);
        set => SetValue(LookDirectionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the near plane distance.
    /// </summary>
    /// <value>
    ///     The near plane distance.
    /// </value>
    public double NearPlaneDistance {
        get => (double) GetValue(NearPlaneDistanceProperty);
        set => SetValue(NearPlaneDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the position.
    /// </summary>
    /// <value>
    ///     The position.
    /// </value>
    public override Point3D Position {
        get => (Point3D) GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>
    ///     Gets or sets up direction.
    /// </summary>
    /// <value>
    ///     Up direction.
    /// </value>
    public override Vector3D UpDirection {
        get => (Vector3D) GetValue(UpDirectionProperty);
        set => SetValue(UpDirectionProperty, value);
    }
}
