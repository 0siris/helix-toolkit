using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using Color = HelixToolkit.SharpDX.Core.Color;

namespace HelixToolkit.Wpf.SharpDX;

public class CameraModel3D : CompositeModel3D {
    public static readonly DependencyProperty CameraProperty =
        DependencyProperty.Register("Camera",
                                    typeof(ProjectionCamera),
                                    typeof(CameraModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                              ((CameraModel3D)d).camera =
                                                                  e.NewValue as ProjectionCamera;
                                                          }));

    protected bool isCaptured;
    protected Vector3 lastHitPos;
    protected Viewport3DX? viewport;
    protected CameraCore? viewportCamera;

    public CameraModel3D() {
        var b1 = new MeshBuilder();
        b1.AddBox(new Vector3(), 1f, 1f, 1.2f, BoxFaces.All);
        var body = new MeshGeometryModel3D {
            CullMode = CullMode.Back, Geometry = b1.ToMeshGeometry3D(),
            Material = new DiffuseMaterial { DiffuseColor = Color.Gray }
        };
        Children.Add(body);
        b1 = new MeshBuilder();
        b1.AddCone(new Vector3(0, 0, -1.2f), new Vector3(0, 0f, 0), 0.4f, true, 12);
        var lens = new MeshGeometryModel3D {
            CullMode = CullMode.Back, Geometry = b1.ToMeshGeometry3D(),
            Material = new DiffuseMaterial { DiffuseColor = Color.Yellow }
        };
        Children.Add(lens);

        var builder = new LineBuilder();
        builder.AddLine(Vector3.Zero, new Vector3(2, 0, 0));
        builder.AddLine(Vector3.Zero, new Vector3(0, 2, 0));
        builder.AddLine(Vector3.Zero, new Vector3(0, 0, -2));

        var mesh = builder.ToLineGeometry3D();
        var arrowMeshModel = new LineGeometryModel3D {
            Geometry = mesh,
            Color = Colors.White,
            IsHitTestVisible = false
        };
        var segment = mesh.Positions.Count / 3;
        var colors = new Color4Collection(Enumerable.Repeat<Color4>(Color.Black, mesh.Positions.Count));
        var i = 0;
        for (; i < segment; ++i) colors[i] = Color.Red;
        for (; i < segment * 2; ++i) colors[i] = Color.Green;
        for (; i < segment * 3; ++i) colors[i] = Color.Blue;
        mesh.Colors = colors;
        Children.Add(arrowMeshModel);
        SceneNode.TransformChanged += SceneNode_OnTransformChanged;
    }

    /// <summary>
    ///     Distance of the directional light from origin
    /// </summary>
    public ProjectionCamera? Camera {
        get => (ProjectionCamera?)GetValue(CameraProperty);
        set => SetValue(CameraProperty, value);
    }

    protected ProjectionCamera? camera {
        get;
        private set {
            if (field == value) return;
            field = value;
            Transform = field is null ? null : new MatrixTransform3D(field.GetInversedViewMatrix());
        }
    }

    protected override void OnMouse3DDown(object sender, RoutedEventArgs e) {
        base.OnMouse3DDown(sender, e);

        if (!(e is Mouse3DEventArgs args))
            return;
        if (args.Viewport == null)
            return;

        isCaptured = true;
        viewport = args.Viewport;
        viewportCamera = args.Viewport.Camera;
        lastHitPos = args.HitTestResult.PointHit;
    }

    protected override void OnMouse3DUp(object sender, RoutedEventArgs e) {
        base.OnMouse3DUp(sender, e);
        if (isCaptured) {
            isCaptured = false;
            viewportCamera = null;
            viewport = null;
        }
    }

    protected override void OnMouse3DMove(object sender, RoutedEventArgs e) {
        base.OnMouse3DMove(sender, e);
        if (isCaptured) {
            var args = (Mouse3DEventArgs)e;

            // move dragmodel
            var normal = viewportCamera!.LookDirection;

            // hit position
            var newHit = viewport!.UnProjectOnPlane(args.Position, lastHitPos.ToPoint3D(), normal.ToVector3D());
            if (newHit.HasValue) {
                var offset = newHit.Value - lastHitPos.ToPoint3D();
                lastHitPos = newHit.Value.ToVector3();
                if (Transform == null)
                    Transform = new TranslateTransform3D(offset);
                else
                    Transform = new MatrixTransform3D(Transform.AppendTransform(new TranslateTransform3D(offset))
                                                               .Value);
            }
        }
    }

    private void SceneNode_OnTransformChanged(object? sender, TransformArgs e) {
        if (camera != null) {
            var m = e.Transform;
            camera.Position = new Point3D(m.M41, m.M42, m.M43);
            camera.LookDirection = new Vector3D(-m.M31, -m.M32, -m.M33);
            camera.UpDirection = new Vector3D(m.M21, m.M22, m.M23);
        }
    }
}
