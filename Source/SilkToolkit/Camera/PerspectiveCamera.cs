/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Cameras;

namespace HelixToolkit.Wpf.SharpDX;

public interface IPerspectiveCameraModel {
    double FieldOfView { get; set; }
}

/// <summary>
///     Represents a perspective projection camera.
/// </summary>
public class PerspectiveCamera : ProjectionCamera, IPerspectiveCameraModel {
    /// <summary>
    ///     The field of view property
    /// </summary>
    public static readonly DependencyProperty FieldOfViewProperty = DependencyProperty.Register("FieldOfView",
        typeof(double),
        typeof(PerspectiveCamera),
        new PropertyMetadata(45.0,
                             (d, e) => {
                                 ((d as Camera).CameraInternal as PerspectiveCameraCore).FieldOfView =
                                     (float)(double)e.NewValue;
                             }));

    /// <summary>
    ///     Gets or sets the field of view.
    /// </summary>
    /// <value>
    ///     The field of view.
    /// </value>
    public double FieldOfView {
        get => (double)GetValue(FieldOfViewProperty);
        set => SetValue(FieldOfViewProperty, value);
    }

    protected override CameraCore CreatePortableCameraCore() => new PerspectiveCameraCore();

    protected override void OnCoreCreated(CameraCore core) {
        base.OnCoreCreated(core);
        (core as PerspectiveCameraCore).FarPlaneDistance = (float)FarPlaneDistance;
        (core as PerspectiveCameraCore).FieldOfView = (float)FieldOfView;
        (core as PerspectiveCameraCore).NearPlaneDistance = (float)NearPlaneDistance;
    }

    protected override Freezable CreateInstanceCore() => new PerspectiveCamera();
}
