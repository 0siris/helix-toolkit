/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Cameras;

namespace HelixToolkit.Wpf.SharpDX;

public interface IOrthographicCameraModel : IProjectionCameraModel {
    double Width { get; set; }

    void AnimateWidth(double newWidth, double animationTime);
}

/// <summary>
///     Represents an orthographic projection camera.
/// </summary>
public class OrthographicCamera : ProjectionCamera, IOrthographicCameraModel {
    /// <summary>
    ///     The width property
    /// </summary>
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width",
        typeof(double),
        typeof(OrthographicCamera),
        new PropertyMetadata(10.0,
                             (d, e) => {
                                 ((d as Camera).CameraInternal as OrthographicCameraCore).Width =
                                     (float)(double)e.NewValue;
                             }));

    private double accumTime;
    private double aniTime;

    private double oldWidth;
    private double targetWidth;

    public OrthographicCamera() {
        // default values for near-far must be different for ortho:
        NearPlaneDistance = 0.001;
        FarPlaneDistance = 100.0;
    }

    /// <summary>
    ///     Gets or sets the width.
    /// </summary>
    /// <value>
    ///     The width.
    /// </value>
    public double Width {
        get => (double)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public void AnimateWidth(double newWidth, double animationTime) {
        if (animationTime == 0) {
            UpdateCameraPositionByWidth(newWidth);
            Width = newWidth;
        } else {
            oldWidth = Width;
            targetWidth = newWidth;
            accumTime = 1;
            aniTime = animationTime;
            OnUpdateAnimation(0);
        }
    }

    protected override CameraCore CreatePortableCameraCore() {
        return new OrthographicCameraCore();
    }

    protected override void OnCoreCreated(CameraCore core) {
        base.OnCoreCreated(core);
        (core as OrthographicCameraCore).FarPlaneDistance = (float)FarPlaneDistance;
        (core as OrthographicCameraCore).NearPlaneDistance = (float)NearPlaneDistance;
        (core as OrthographicCameraCore).Width = (float)Width;
    }

    protected override bool OnUpdateAnimation(float ellapsed) {
        var res = base.OnUpdateAnimation(ellapsed);
        if (aniTime == 0) return res;
        accumTime += ellapsed;
        if (accumTime > aniTime) {
            UpdateCameraPositionByWidth(targetWidth);
            Width = targetWidth;
            aniTime = 0;
            return res;
        }

        var newWidth = oldWidth + (targetWidth - oldWidth) * (accumTime / aniTime);
        UpdateCameraPositionByWidth(newWidth);
        Width = newWidth;
        return true;
    }

    private void UpdateCameraPositionByWidth(double newWidth) {
        var ratio = newWidth / Width;
        var dir = LookDirection.ToVector3();
        var target = Target.ToVector3();
        var dist = dir.Length;
        var newDist = dist * ratio;
        dir.Normalize();
        var position = target - dir * (float)newDist;
        var lookDir = dir * (float)newDist;
        Position = position.ToPoint3D();
        LookDirection = lookDir.ToVector3D();
    }

    protected override Freezable CreateInstanceCore() {
        return new OrthographicCamera();
    }
}
