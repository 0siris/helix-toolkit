using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace BatchedMeshDemo;

public class MainViewModel : BaseViewModel {
    public IList<BatchedMeshGeometryConfig>? BatchedMeshes {
        set => SetValue(ref field, value);
        get;
    }

    public IList<Material>? BatchedMaterials {
        set => SetValue(ref field, value);
        get;
    }

    public Media3D.Transform3D BatchedTransform { get; } = new Media3D.ScaleTransform3D(0.1, 0.1, 0.1);

    public Geometry3D? SelectedGeometry {
        set {
            if (SetValue(ref field, value) && value is not null && BatchedMeshes is { } batchedMeshes
                && batchedMeshes.FirstOrDefault(x => x.Geometry == value) is { } selected) {
                SelectedTransform = new Media3D.MatrixTransform3D(
                    selected.ModelTransform.ToMatrix3D() * BatchedTransform.Value);
            }
        }
        get;
    }

    public Media3D.Transform3D? SelectedTransform {
        set => SetValue(ref field, value);
        get;
    }

    public Material MainMaterial { get; } = PhongMaterials.White;

    public Material SelectedMaterial { get; } = new PhongMaterial() {
        EmissiveColor = Color.Yellow
    };

    public Geometry3D FloorModel { private set; get; }

    public Material FloorMaterial { private set; get; } = PhongMaterials.Pearl;

    private readonly SynchronizationContext context = SynchronizationContext.Current
                                                      ?? throw new InvalidOperationException(
                                                          "The batched mesh demo requires a synchronization context.");

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera() {
            Position = new Point3D(0, 0, 200),
            LookDirection = new Vector3D(0, 0, -200),
            UpDirection = new Vector3D(0, 1, 0),
            FarPlaneDistance = 1000
        };
        Task.Run(LoadModels);
        var builder = new MeshBuilder(true);
        builder.AddBox(new Vector3(0, -65, 0), 600, 1, 600);
        FloorModel = builder.ToMesh();
        if (MainMaterial is PhongMaterial mainMaterial)
            mainMaterial.NormalMap = new TextureModel("TextureNoise1_dot3.jpg");
        if (MainMaterial is PhongMaterial mainMaterialWithShadows)
            mainMaterialWithShadows.RenderShadowMap = true;
        if (FloorMaterial is PhongMaterial floorMaterial)
            floorMaterial.RenderShadowMap = true;
    }

    private void LoadModels() {
        var models = Load3Ds("Car.3DS");
        int count = 0;
        Dictionary<MaterialCore, int> materialDict = [];
        //materialDict.Add(new PhongMaterialCore() { DiffuseColor = new Color4(1, 0, 0, 1) }, count);
        foreach (var model in models) {
            if (model.Geometry is not { } geometry || model.Material is not { } material)
                continue;
            if (materialDict.ContainsKey(material)) {
                continue;
            }

            materialDict.Add(material, count++);
        }

        var modelList = new List<BatchedMeshGeometryConfig>(models.Count);
        foreach (var model in models) {
            if (model.Geometry is not { } geometry || model.Material is not { } material)
                continue;
            geometry.UpdateOctree();
            if (model.Transform != null) {
                foreach (var transform in model.Transform) {
                    modelList.Add(
                        new BatchedMeshGeometryConfig(geometry, transform, materialDict[material]));
                    //modelList.Add(new BatchedMeshGeometryConfig(model.Geometry, transform, 0));
                }
            } else {
                modelList.Add(
                    new BatchedMeshGeometryConfig(geometry, Matrix.Identity, materialDict[material]));
                //modelList.Add(new BatchedMeshGeometryConfig(model.Geometry, Matrix.Identity, 0));
            }
        }

        Material[] materials = new Material[materialDict.Count];
        foreach (var m in materialDict.Keys) {
            materials[materialDict[m]] = m.ConvertToMaterial();
        }

        context.Post((o) => {
                BatchedMeshes = modelList;
                BatchedMaterials = materials;
            },
            null);
    }

    [Obsolete]
    public List<Object3D> Load3Ds(string path) {
        if (path.EndsWith(".obj", StringComparison.CurrentCultureIgnoreCase)) {
            var reader = new ObjReader();
            var list = reader.Read(path);
            return list;
        } else if (path.EndsWith(".3ds", StringComparison.CurrentCultureIgnoreCase)) {
            var reader = new StudioReader();
            var list = reader.Read(path);
            return list;
        } else {
            return [];
        }
    }
}