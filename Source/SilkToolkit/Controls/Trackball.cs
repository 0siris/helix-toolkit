// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Trackball.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Trackball is a utility class which observes the mouse events
//   on a specified FrameworkElement and produces a Transform3D
//   with the resultant rotation and scale.
//   ///     Example Usage:
//   ///         Trackball trackball = new Trackball();
//   trackball.EventSource = myElement;
//   myViewport3D.Camera.Transform = trackball.Transform;
//   ///     Because Viewport3Ds only raise events when the mouse is over the
//   rendered 3D geometry (as opposed to not when the mouse is within
//   the layout bounds) you usually want to use another element as 
//   your EventSource.  For example, a transparent border placed on
//   top of your Viewport3D works well:
//   ///         <Grid>
//   <ColumnDefinition />
//   <RowDefinition />
//   <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
//   <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
//   </Grid>
//   ///     NOTE: The Transform property may be shared by multiple Cameras
//   if you want to have auxiliary views following the trackball.
//   ///           It can also be useful to share the Transform property with
//   models in the scene that you want to move with the camera.
//   (For example, the Trackport3D's headlight is implemented
//   this way.)
//   ///           You may also use a Transform3DGroup to combine the
//   Transform property with additional Transforms.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Trackball is a utility class which observes the mouse events
///     on a specified FrameworkElement and produces a Transform3D
///     with the resultant rotation and scale.
///     Example Usage:
///     Trackball trackball = new Trackball();
///     trackball.EventSource = myElement;
///     myViewport3D.Camera.Transform = trackball.Transform;
///     Because Viewport3Ds only raise events when the mouse is over the
///     rendered 3D geometry (as opposed to not when the mouse is within
///     the layout bounds) you usually want to use another element as
///     your EventSource.  For example, a transparent border placed on
///     top of your Viewport3D works well:
///     <Grid>
///         <ColumnDefinition />
///         <RowDefinition />
///         <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
///         <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
///     </Grid>
///     NOTE: The Transform property may be shared by multiple Cameras
///     if you want to have auxiliary views following the trackball.
///     It can also be useful to share the Transform property with
///     models in the scene that you want to move with the camera.
///     (For example, the Trackport3D's headlight is implemented
///     this way.)
///     You may also use a Transform3DGroup to combine the
///     Transform property with additional Transforms.
/// </summary>
public class Trackball {
    private readonly RotateTransform3D rotateTransform;
    private readonly AxisAngleRotation3D rotation = new();

    private readonly double rotationFactor;
    private readonly ScaleTransform3D scale = new();

    private readonly Transform3DGroup transform;
    private readonly TranslateTransform3D translate = new();
    private readonly double zoomFactor;
    private FrameworkElement? eventSource;
    private Point previousPosition2D;
    private Vector3D previousPosition3D = new(0, 0, 1);

    public Trackball(double rotationFactor = 4.0, double zoomFacfor = 1.0) {
        this.rotationFactor = rotationFactor;
        zoomFactor = zoomFacfor;
        transform = new Transform3DGroup();
        transform.Children.Add(scale);
        rotateTransform = new RotateTransform3D(rotation);
        transform.Children.Add(rotateTransform);
        transform.Children.Add(translate);
    }

    /// <summary>
    ///     A transform to move the camera or scene to the trackball's
    ///     current orientation and scale.
    /// </summary>
    public Transform3D Transform => transform;

    /// <summary>
    ///     Rotation component of the transform
    /// </summary>
    public Transform3D RotateTransform => rotateTransform;

    /// <summary>
    ///     The FrameworkElement we listen to for mouse events.
    /// </summary>
    public FrameworkElement EventSource {
        get => eventSource ?? throw new InvalidOperationException("Event source is not initialized.");

        set {
            if (eventSource is not null) {
                //_eventSource.MouseDown -= this.OnMouseDown;
                //_eventSource.MouseUp -= this.OnMouseUp;
                //_eventSource.MouseMove -= this.OnMouseMove;

                eventSource.PreviewMouseDown -= OnMouseDown;
                eventSource.PreviewMouseUp -= OnMouseUp;
                eventSource.PreviewMouseMove -= OnMouseMove;
            }

            eventSource = value;

            eventSource.PreviewMouseDown += OnMouseDown;
            eventSource.PreviewMouseUp += OnMouseUp;
            eventSource.PreviewMouseMove += OnMouseMove;
        }
    }

    /// <summary>
    /// </summary>
    private void OnMouseDown(object sender, MouseEventArgs e) {
        Mouse.Capture(EventSource, CaptureMode.SubTree);
        previousPosition2D = e.GetPosition(EventSource);
        previousPosition3D = ProjectToTrackball(EventSource.ActualWidth,
                                                 EventSource.ActualHeight,
                                                 previousPosition2D);
    }

    /// <summary>
    /// </summary>
    private void OnMouseUp(object sender, MouseEventArgs e) {
        Mouse.Capture(EventSource, CaptureMode.None);
    }

    /// <summary>
    /// </summary>
    private void OnMouseMove(object sender, MouseEventArgs e) {
        var currentPosition = e.GetPosition(EventSource);

        if (e.LeftButton == MouseButtonState.Pressed &&
            (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            Pan(currentPosition);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Look(currentPosition);
        else if (e.RightButton == MouseButtonState.Pressed) Zoom(currentPosition);

        previousPosition2D = currentPosition;
    }

    /// <summary>
    /// </summary>
    private void Look(Point currentPosition) {
        var currentPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

        if (previousPosition3D.Equals(currentPosition3D))
            return;

        var axis = Vector3D.CrossProduct(previousPosition3D, currentPosition3D);

        var angle = rotationFactor * Vector3D.AngleBetween(previousPosition3D, currentPosition3D);
        var delta = new System.Windows.Media.Media3D.Quaternion(axis, -angle);

        // Get the current orientation from the RotateTransform3D
        var q = new System.Windows.Media.Media3D.Quaternion(rotation.Axis, rotation.Angle);

        // Compose the delta with the previous orientation
        q *= delta;

        // Write the new orientation back to the Rotation3D
        rotation.Axis = q.Axis;
        rotation.Angle = q.Angle;

        previousPosition3D = currentPosition3D;
    }

    /// <summary>
    /// </summary>
    private void Pan(Point currentPosition) {
        var currentPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

        var change = Point.Subtract(previousPosition2D, currentPosition);

        var changeVector = new Vector3D(change.X, change.Y, 0);

        translate.OffsetX += changeVector.X * .1;
        translate.OffsetY -= changeVector.Y * .1;
        translate.OffsetZ += changeVector.Z * .1;

        previousPosition3D = currentPosition3D;
    }

    /// <summary>
    /// </summary>
    private Vector3D ProjectToTrackball(double width, double height, Point point) {
        var x = point.X / (width / 2); // Scale so bounds map to [0,0] - [2,2]
        var y = point.Y / (height / 2);

        x--; // Translate 0,0 to the center
        y = 1 - y; // Flip so +Y is up instead of down

        var z2 = 1 - x * x - y * y; // z^2 = 1 - x^2 - y^2
        var z = z2 > 0 ? Math.Sqrt(z2) : 0;

        return new Vector3D(x, y, z);
    }

    /// <summary>
    /// </summary>
    private void Zoom(Point currentPosition) {
        var yDelta = currentPosition.Y - previousPosition2D.Y;

        var s = zoomFactor * Math.Exp(-yDelta / 100); // e^(yDelta/100) is fairly arbitrary.

        scale.ScaleX *= s;
        scale.ScaleY *= s;
        scale.ScaleZ *= s;
    }
}
