// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InteractionHandle3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MouseDragDemo;

using System.Linq;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using MatrixTransform3D = System.Windows.Media.Media3D.MatrixTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

public sealed class InteractionHandle3D : GroupModel3D, IHitable, ISelectable {
    // 3 --- 2
    // |     |
    // 0 --- 1
    private Vector3[] positions = [
        new(-1, -1, 0),
        new(+1, -1, 0),
        new(+1, +1, 0),
        new(-1, +1, 0),
    ];

    private DraggableGeometryModel3D[] cornerHandles = new DraggableGeometryModel3D[4];
    private DraggableGeometryModel3D[] midpointHandles = new DraggableGeometryModel3D[4];
    private MeshGeometryModel3D[] edgeHandles = new MeshGeometryModel3D[4];
    private bool isCaptured;
    private Viewport3DX viewport;
    private CameraCore camera;
    private System.Windows.Media.Media3D.Point3D lastHitPos;
    private MatrixTransform3D dragTransform;
    //private Material selectionMaterial;


    private static Geometry3D NodeGeometry;
    private static Geometry3D EdgeHGeometry, EdgeVGeometry;
    private static Geometry3D BoxGeometry;

    static InteractionHandle3D() {
        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0.0f, 0.0f, 0), 0.135);
        NodeGeometry = b1.ToMeshGeometry3D();

        var b2 = new MeshBuilder();
        b2.AddCylinder(new Vector3(0, 0, 0), new Vector3(1, 0, 0), 0.05, 32, true, true);
        EdgeHGeometry = b2.ToMeshGeometry3D();

        var b3 = new MeshBuilder();
        b3.AddCylinder(new Vector3(0, 0, 0), new Vector3(0, 1, 0), 0.05, 32, true, true);
        EdgeVGeometry = b3.ToMeshGeometry3D();

        var b4 = new MeshBuilder();
        b4.AddBox(new Vector3(0, 0, 0), 0.175, 0.175, 0.175);
        BoxGeometry = b4.ToMeshGeometry3D();
    }

    /// <summary>
    ///
    /// </summary>
    public InteractionHandle3D() {
        Material = PhongMaterials.Orange;
        //selectionColor.EmissiveColor = Color.Blue;
        //selectionColor.SpecularColor = Color.Black;
        //selectionColor.ReflectiveColor = Color.Black;

        for (int i = 0; i < 4; i++) {
            var translate = Matrix3DExtensions.Translate3D(positions[i].ToVector3D());
            cornerHandles[i] = new DraggableGeometryModel3D() {
                DragZ = false,
                Visibility = Visibility.Visible,
                Material = Material,
                Geometry = NodeGeometry,
                Transform = new MatrixTransform3D(translate),
            };
            cornerHandles[i].MouseMove3D += OnNodeMouse3DMove;
            cornerHandles[i].MouseUp3D += OnNodeMouse3DUp;
            cornerHandles[i].MouseDown3D += OnNodeMouse3DDown;

            edgeHandles[i] = new MeshGeometryModel3D() {
                Geometry = (i % 2 == 0) ? EdgeHGeometry : EdgeVGeometry,
                Material = Material,
                Visibility = Visibility.Visible,
                Transform = new MatrixTransform3D(translate),
            };
            edgeHandles[i].MouseMove3D += OnEdgeMouse3DMove;
            edgeHandles[i].MouseUp3D += OnEdgeMouse3DUp;
            edgeHandles[i].MouseDown3D += OnEdgeMouse3DDown;


            translate = Matrix3DExtensions.Translate3D(0.5 * (positions[i] + positions[(i + 1) % 4]).ToVector3D());
            midpointHandles[i] = new DraggableGeometryModel3D() {
                DragZ = false,
                DragX = (i % 2 == 1),
                DragY = (i % 2 == 0),
                Material = Material,
                Geometry = BoxGeometry,
                Transform = new MatrixTransform3D(translate),
            };
            midpointHandles[i].MouseMove3D += OnNodeMouse3DMove;
            midpointHandles[i].MouseUp3D += OnNodeMouse3DUp;
            midpointHandles[i].MouseDown3D += OnNodeMouse3DDown;

            Children.Add(cornerHandles[i]);
            Children.Add(edgeHandles[i]);
            Children.Add(midpointHandles[i]);
        }

        // 3 --- 2
        // |     |
        // 0 --- 1
        var m0 = Scaling(+2, 1, 1) * Translation(positions[0]);
        edgeHandles[0].Transform = new MatrixTransform3D(m0.ToMatrix3D());
        var m2 = Scaling(+2, 1, 1) * Translation(positions[3]);
        edgeHandles[2].Transform = new MatrixTransform3D(m2.ToMatrix3D());

        var m1 = Scaling(1, +2, 1) * Translation(positions[1]);
        edgeHandles[1].Transform = new MatrixTransform3D(m1.ToMatrix3D());
        var m3 = Scaling(1, +2, 1) * Translation(positions[0]);
        edgeHandles[3].Transform = new MatrixTransform3D(m3.ToMatrix3D());

        dragTransform = new MatrixTransform3D(Transform.Value);
    }


    private void OnNodeMouse3DDown(object sender, RoutedEventArgs e) {
        var args = e as Mouse3DEventArgs;
        if (args == null) return;
        if (args.Viewport == null) return;

        isCaptured = true;
    }

    private void OnNodeMouse3DUp(object sender, RoutedEventArgs e) {
        if (isCaptured) {
            Application.Current.MainWindow.Cursor = Cursors.Arrow;
            //UpdateTransforms(sender);
        }
    }

    private void OnNodeMouse3DMove(object sender, RoutedEventArgs e) {
        if (isCaptured) {
            UpdateTransforms(sender);
        }
    }

    private void OnEdgeMouse3DDown(object sender, RoutedEventArgs e) {
        var args = e as Mouse3DEventArgs;
        if (args == null) return;
        if (args.Viewport == null) return;

        isCaptured = true;
        viewport = args.Viewport;
        camera = args.Viewport.Camera;
        lastHitPos = args.HitTestResult.PointHit.ToPoint3D();
    }

    private void OnEdgeMouse3DUp(object sender, RoutedEventArgs e) {
        if (isCaptured) {
            Application.Current.MainWindow.Cursor = Cursors.Arrow;
            isCaptured = false;
            camera = null;
            viewport = null;
        }
    }

    private void OnEdgeMouse3DMove(object sender, RoutedEventArgs e) {
        if (isCaptured) {
            Application.Current.MainWindow.Cursor = Cursors.SizeAll;
            var args = e as Mouse3DEventArgs;

            // move dragmodel
            var normal = camera.LookDirection;

            // hit position
            var newHit = viewport.UnProjectOnPlane(args.Position, lastHitPos, normal.ToVector3D());
            if (newHit.HasValue) {
                var offset = (newHit.Value - lastHitPos);
                var trafo = Transform.Value;

                if (DragX)
                    trafo.OffsetX += offset.X;

                if (DragY)
                    trafo.OffsetY += offset.Y;

                if (!DragZ)
                    trafo.OffsetZ += offset.Z;

                dragTransform.Matrix = trafo;
                Transform = dragTransform;
                lastHitPos = newHit.Value;
            }
        }
    }

    private void UpdateTransforms(object sender) {
        var cornerTrafos = cornerHandles.Select(x => (x.Transform as MatrixTransform3D)).ToArray();
        var cornerMatrix = cornerTrafos.Select(x => (x).Value).ToArray();
        positions = [.. cornerMatrix.Select(x => TranslationVector(x.ToMatrix()))];

        BoundingBox bb;
        if (sender == cornerHandles[0] || sender == cornerHandles[2]) {
            Application.Current.MainWindow.Cursor = Cursors.SizeNESW;
            bb = BoundingBoxExtensions.FromPoints([positions[0], positions[2]]);
        } else if (sender == cornerHandles[1] || sender == cornerHandles[3]) {
            Application.Current.MainWindow.Cursor = Cursors.SizeNWSE;
            bb = BoundingBoxExtensions.FromPoints([positions[1], positions[3]]);
        } else {
            if (sender == midpointHandles[0] || sender == midpointHandles[2]) {
                Application.Current.MainWindow.Cursor = Cursors.SizeNS;
            } else {
                Application.Current.MainWindow.Cursor = Cursors.SizeWE;
            }

            positions = [.. midpointHandles.Select(x => TranslationVector(x.Transform.Value.ToMatrix()))];
            bb = BoundingBoxExtensions.FromPoints(positions);
        }

        // 3 --- 2
        // |     |
        // 0 --- 1
        positions[0].X = bb.Minimum.X;
        positions[1].X = bb.Maximum.X;
        positions[2].X = bb.Maximum.X;
        positions[3].X = bb.Minimum.X;

        positions[0].Y = bb.Minimum.Y;
        positions[1].Y = bb.Minimum.Y;
        positions[2].Y = bb.Maximum.Y;
        positions[3].Y = bb.Maximum.Y;

        for (int i = 0; i < 4; i++) {
            if (sender != cornerHandles[i]) {
                cornerTrafos[i].Matrix = Matrix3DExtensions.Translate3D(positions[i].ToVector3D());
            }

            var m = Matrix3DExtensions.Translate3D(0.5 * (positions[i] + positions[(i + 1) % 4]).ToVector3D());
            ((MatrixTransform3D)midpointHandles[i].Transform).Matrix = m;
        }

        // 3 --- 2
        // |     |
        // 0 --- 1
        var m0 = Scaling(positions[1].X - positions[0].X, 1, 1) * Translation(positions[0]);
        ((MatrixTransform3D)edgeHandles[0].Transform).Matrix = (m0.ToMatrix3D());
        var m2 = Scaling(positions[1].X - positions[0].X, 1, 1) * Translation(positions[3]);
        ((MatrixTransform3D)edgeHandles[2].Transform).Matrix = (m2.ToMatrix3D());

        var m1 = Scaling(1, positions[2].Y - positions[1].Y, 1) * Translation(positions[1]);
        ((MatrixTransform3D)edgeHandles[1].Transform).Matrix = (m1.ToMatrix3D());
        var m3 = Scaling(1, positions[2].Y - positions[1].Y, 1) * Translation(positions[0]);
        ((MatrixTransform3D)edgeHandles[3].Transform).Matrix = (m3.ToMatrix3D());
    }


    public static readonly DependencyProperty DragXProperty =
        DependencyProperty.Register("DragX", typeof(bool), typeof(InteractionHandle3D), new UIPropertyMetadata(true));

    public static readonly DependencyProperty DragYProperty =
        DependencyProperty.Register("DragY", typeof(bool), typeof(InteractionHandle3D), new UIPropertyMetadata(true));

    public static readonly DependencyProperty DragZProperty =
        DependencyProperty.Register("DragZ", typeof(bool), typeof(InteractionHandle3D), new UIPropertyMetadata(true));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected",
                                    typeof(bool),
                                    typeof(InteractionHandle3D),
                                    new UIPropertyMetadata(false));


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

    public bool IsSelected {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public Material Material {
        get => (Material)GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty MaterialProperty =
        DependencyProperty.Register("Material",
                                    typeof(Material),
                                    typeof(InteractionHandle3D),
                                    new UIPropertyMetadata(MaterialChanged));

    /// <summary>
    ///
    /// </summary>
    private static void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (e.NewValue is PhongMaterial) {
            foreach (var item in ((GroupModel3D)d).Children) {
                var model = item as MaterialGeometryModel3D;
                model?.Material = e.NewValue as PhongMaterial;
            }
        }
    }

    private static Matrix Scaling(float x, float y, float z) => new(x,
        0,
        0,
        0,
        0,
        y,
        0,
        0,
        0,
        0,
        z,
        0,
        0,
        0,
        0,
        1);

    private static Matrix Translation(Vector3 value) {
        var result = Matrix.Identity;
        result.M41 = value.X;
        result.M42 = value.Y;
        result.M43 = value.Z;
        return result;
    }

    private static Vector3 TranslationVector(Matrix value) => new(value.M41, value.M42, value.M43);
}
