using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf.SharpDX;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SSAODemo;

public class MainWindowViewModel : DemoCore.BaseViewModel {
    public Geometry3D FloorModel { get; }
    public Geometry3D SphereModel { get; }
    public Geometry3D TeapotModel { get; }

    public Geometry3D BunnyModel { get; }

    public PhongMaterial FloorMaterial { get; }
    public PhongMaterial SphereMaterial { get; }

    public PhongMaterial BunnyMaterial { get; }
    public Matrix[] SphereInstances { get; }

    public Matrix[] BunnyInstances { get; }

    public SSAOQuality[] SSAOQualities { get; } = [SSAOQuality.High, SSAOQuality.Low];

    [Obsolete]
    public MainWindowViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera() {
            Position = new Media3D.Point3D(0, 10, 10),
            LookDirection = new Media3D.Vector3D(0, -10, -10),
            UpDirection = new Media3D.Vector3D(0, 1, 0),
            FarPlaneDistance = 200,
            NearPlaneDistance = 0.1
        };

        var builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, -0.1f, 0), 20, 0.1f, 20);
        builder.AddBox(new Vector3(-7, 2.5f, 0), 5, 5, 5);
        builder.AddBox(new Vector3(-5, 2.5f, -5), 5, 5, 5);
        FloorModel = builder.ToMesh();

        builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero, 1);
        SphereModel = builder.ToMesh();

        var reader = new ObjReader();

        var models = reader.Read("bunny.obj");
        BunnyModel = models[0].Geometry;
        BunnyMaterial = PhongMaterials.Green;
        BunnyMaterial.AmbientColor = BunnyMaterial.DiffuseColor * 0.5f;
        FloorMaterial = PhongMaterials.PureWhite;
        FloorMaterial.AmbientColor = FloorMaterial.DiffuseColor * 0.5f;
        SphereMaterial = PhongMaterials.Red;
        SphereMaterial.AmbientColor = SphereMaterial.DiffuseColor * 0.5f;
        SphereInstances = [
            Translation(-2.5f, 1, 0),
            Translation(2.5f, 1, 0),
            Translation(0, 1, -2.5f),
            Translation(0, 1, 2.5f)
        ];

        BunnyInstances = [
            Translation(0f, -0.8f, 0),
            Translation(6f, -0.8f, 0),
            Translation(0, -0.8f, -4f),
            Translation(0, -0.8f, 4f)
        ];
    }

    private static Matrix Translation(float x, float y, float z) {
        var m = System.Numerics.Matrix4x4.CreateTranslation(x, y, z);
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
