using System.Linq;
using HelixToolkit.Wpf.SharpDX;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace CustomViewCubeDemo;

public class MainWindowViewModel : DemoCore.BaseViewModel {
    public Geometry3D Geometry { private set; get; }
    public Geometry3D RectGeometry { private set; get; }
    public Material Material { private set; get; }
    public Geometry3D ViewCubeGeometry1 { private set; get; }
    public Geometry3D ViewCubeGeometry2 { private set; get; }
    public Material ViewCubeMaterial1 { private set; get; }
    public Material ViewCubeMaterial2 { private set; get; }
    public Material ViewCubeMaterial3 { private set; get; }
    public Material ViewCubeMaterial4 { private set; get; }
    public LineGeometry3D Coordinate { private set; get; }
    public BillboardText3D CoordinateText { private set; get; }

    public System.Windows.Media.Media3D.Transform3D ViewCubeTransform3 { private set; get; }

    public MainWindowViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera() {
            Position = new System.Windows.Media.Media3D.Point3D(0, 0, 10),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -10),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0)
        };
        InitializeModels();
        InitializeViewCubes();
        InitializeCoordinates();
    }

    [Obsolete]
    private void InitializeModels() {
        var builder = new MeshBuilder();
        builder.AddBox(Vector3.Zero, 1, 1, 1);
        var reader = new ObjReader();
        var models = reader.Read("bunny.obj");
        Geometry = models[0].Geometry;
        Material = PhongMaterials.Red;

        builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, 0, -4), 2, 2, 6);
        RectGeometry = builder.ToMeshGeometry3D();
    }

    private void InitializeViewCubes() {
        var builder = new MeshBuilder();
        builder.AddPyramid(Vector3.Zero, 10, 10, true);
        ViewCubeGeometry1 = builder.ToMesh();
        ViewCubeMaterial1 = DiffuseMaterials.Orange;

        builder = new MeshBuilder();
        builder.AddDodecahedron(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, 5);
        ViewCubeGeometry2 = builder.ToMesh();
        ViewCubeMaterial2 = DiffuseMaterials.Blue;

        ViewCubeMaterial3 = DiffuseMaterials.Gray;
        ViewCubeMaterial4 = DiffuseMaterials.Pearl;
        //Center the model first and do scaling
        var transform = CreateTransform();
        ViewCubeTransform3 = new System.Windows.Media.Media3D.MatrixTransform3D(transform.ToMatrix3D());
    }

    private void InitializeCoordinates() {
        var builder = new LineBuilder();
        builder.AddLine(Vector3.Zero, Vector3.UnitX * 5);
        builder.AddLine(Vector3.Zero, Vector3.UnitY * 5);
        builder.AddLine(Vector3.Zero, Vector3.UnitZ * 5);
        Coordinate = builder.ToLineGeometry3D();
        Coordinate.Colors = [.. Enumerable.Repeat<Color4>(Color.White, 6)];
        Coordinate.Colors[0] = Coordinate.Colors[1] = Color.Red;
        Coordinate.Colors[2] = Coordinate.Colors[3] = Color.Green;
        Coordinate.Colors[4] = Coordinate.Colors[5] = Color.Blue;

        CoordinateText = new BillboardText3D();
        CoordinateText.TextInfo.Add(new TextInfo("X", Vector3.UnitX * 6));
        CoordinateText.TextInfo.Add(new TextInfo("Y", Vector3.UnitY * 6));
        CoordinateText.TextInfo.Add(new TextInfo("Z", Vector3.UnitZ * 6));
    }

    private static Matrix CreateTransform() {
        var m = System.Numerics.Matrix4x4.CreateTranslation(0, -2, 0) * System.Numerics.Matrix4x4.CreateScale(3.5f);
        return new Matrix(m.M11,
                          m.M12,
                          m.M13,
                          m.M14,
                          m.M21,
                          m.M22,
                          m.M23,
                          m.M24,
                          m.M31,
                          m.M32,
                          m.M33,
                          m.M34,
                          m.M41,
                          m.M42,
                          m.M43,
                          m.M44);
    }
}
