// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DraggableGeometryModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Example class how to implement mouse dragging for objects.
//   Probably it should be moved to a "Dragging Demo."
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.Media3D;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Example class how to implement mouse dragging for objects.
///     Probably it should be moved to a "Dragging Demo."
/// </summary>
public class DraggableGeometryModel3D : MeshGeometryModel3D, ISelectable {
    public static readonly DependencyProperty DragXProperty =
        DependencyProperty.Register("DragX",
                                    typeof(bool),
                                    typeof(DraggableGeometryModel3D),
                                    new PropertyMetadata(true));

    public static readonly DependencyProperty DragYProperty =
        DependencyProperty.Register("DragY",
                                    typeof(bool),
                                    typeof(DraggableGeometryModel3D),
                                    new PropertyMetadata(true));

    public static readonly DependencyProperty DragZProperty =
        DependencyProperty.Register("DragZ",
                                    typeof(bool),
                                    typeof(DraggableGeometryModel3D),
                                    new PropertyMetadata(true));

    protected Camera? Camera;
    protected bool IsCaptured;
    protected Point3D LastHitPos;
    protected Viewport3DX? Viewport;


    public bool DragX {
        get => (bool)GetValue(DragXProperty);
        set => SetValue(DragXProperty, value);
    }

    public bool DragY {
        get => (bool)GetValue(DragYProperty);
        set => SetValue(DragYProperty, value);
    }

    public bool DragZ {
        get => (bool)GetValue(DragZProperty);
        set => SetValue(DragZProperty, value);
    }

    public Point3D LastHitPosition => LastHitPos;

    protected override void OnMouse3DDown(object sender, RoutedEventArgs e) {
        base.OnMouse3DDown(sender, e);

        var args = e as Mouse3DEventArgs;
        if (args == null)
            return;
        if (args.Viewport is not { } viewport || args.HitTestResult is not { } hitTestResult)
            return;

        IsCaptured = true;
        Viewport = viewport;
        Camera = viewport.Camera;
        LastHitPos = hitTestResult.PointHit.ToPoint3D();
    }

    protected override void OnMouse3DUp(object sender, RoutedEventArgs e) {
        base.OnMouse3DUp(sender, e);
        if (IsCaptured) {
            IsCaptured = false;
            Camera = null;
            Viewport = null;
        }
    }

    protected override void OnMouse3DMove(object sender, RoutedEventArgs e) {
        base.OnMouse3DMove(sender, e);
        if (IsCaptured && Camera is { } camera && Viewport is { } viewport && e is Mouse3DEventArgs args) {
            // move dragmodel                         
            var normal = camera.LookDirection;

            // hit position                        
            var newHit = viewport.UnProjectOnPlane(args.Position, LastHitPos, normal);
            if (newHit.HasValue) {
                var delta = newHit.Value - LastHitPos;
                var offset = new Vector3D(DragX ? delta.X : 0,
                                          DragY ? delta.Y : 0,
                                          DragZ ? delta.Z : 0);

                LastHitPos = newHit.Value;
                if (Transform == null)
                    Transform = new TranslateTransform3D(offset);
                else
                    Transform = new MatrixTransform3D(Transform.AppendTransform(new TranslateTransform3D(offset))
                                                               .Value);
            }
        }
    }
}
