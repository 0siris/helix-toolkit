using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace GenericMaterialDemo;

public class MainWindowViewModel : DemoCore.BaseViewModel {
    public SceneNodeGroupModel3D ModelGroup { get; } = new();

    private Geometry3D Sphere { get; }

    public GenericMaterialCore PhongMaterial { private set; get; }


    public MainWindowViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera() {
            Position = new System.Windows.Media.Media3D.Point3D(0, 0, -5),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 5),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0)
        };

        var builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero);
        Sphere = builder.ToMesh();
        CreateGenericPhongMaterial();
        InitializeScene();
    }


    private void CreateGenericPhongMaterial() {
        PhongMaterial =
            new GenericMeshMaterialCore(EffectsManager[DefaultRenderTechniqueNames.Mesh][DefaultPassNames.Default],
                                        "cbMesh");
        PhongMaterial.SetProperty(PhongPbrMaterialStruct.DiffuseStr, Color.Red.ToColor4());
        PhongMaterial.SetProperty(PhongPbrMaterialStruct.ReflectStr, Color.Black.ToColor4());
        PhongMaterial.SetProperty(PhongPbrMaterialStruct.UvTransformR1Str, new Vector4(1, 0, 0, 0));
        PhongMaterial.SetProperty(PhongPbrMaterialStruct.UvTransformR2Str, new Vector4(0, 1, 0, 0));
    }

    private void InitializeScene() {
        var node = new MeshNode() { Geometry = Sphere, Material = PhongMaterial };
        ModelGroup.AddNode(node);
    }
}
