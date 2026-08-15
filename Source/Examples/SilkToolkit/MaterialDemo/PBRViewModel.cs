using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace MaterialDemo;

public class PbrViewModel : BaseViewModel {
    public Geometry3D SphereModel { get; }
    private const int Row = 5;
    private const int Col = 5;
    private const int Size = 5 * 5;
    public required TextureModel EnvironmentMap { set; get; }
    public ObservableElement3DCollection Models { get; } = [];
    private List<PbrMaterial> materials = [];
    public Geometry3D Model { get; }
    public Geometry3D FloorModel { get; }
    public Transform3D ModelTransform { get; }
    public PbrMaterial Material { get; }
    public PbrMaterial FloorMaterial { get; }
    public Transform3D FloorModelTransform { get; }
    private Color albedoColor = Colors.Gold;

    public Color AlbedoColor {
        set {
            if (SetValue(ref albedoColor, value)) {
                foreach (var m in materials) {
                    m.AlbedoColor = value.ToColor4();
                }

                Material.AlbedoColor = value.ToColor4();
            }
        }
        get => albedoColor;
    }

    public bool RenderEnvironment {
        set {
            if (SetValue(ref field, value)) {
                foreach (var m in materials) {
                    m.RenderEnvironmentMap = value;
                }
            }
        }
        get;
    } = true;

    public bool RenderNormalMap {
        set {
            if (SetValue(ref field, value)) {
                foreach (var m in materials) {
                    m.RenderNormalMap = value;
                }
            }
        }
        get;
    } = true;

    public PbrViewModel(IEffectsManager manager) {
        EffectsManager = manager;
        Camera = new PerspectiveCamera {
            Position = new Point3D(0, 60, 60),
            LookDirection = new Vector3D(0, -60, -60),
            UpDirection = new Vector3D(0, 1, 0)
        };
        var builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero, 2);
        SphereModel = builder.ToMesh();
        var normalMap =
            TextureModel.Create(new Uri("TextureNoise1_dot3.dds", UriKind.RelativeOrAbsolute).ToString());
        for (int i = -Row; i < Row; ++i) {
            for (int j = -Col; j < Col; ++j) {
                var m = new PbrMaterial() {
                    AlbedoColor = albedoColor.ToColor4(),
                    RoughnessFactor = 1.0 / (2 * Row) * Math.Abs(i + Row),
                    MetallicFactor = 1.0 / (2 * Col) * Math.Abs(j + Col),
                    RenderEnvironmentMap = true,
                    EnableAutoTangent = true,
                    NormalMap = normalMap,
                    RenderShadowMap = true
                };
                materials.Add(m);
                Models.Add(new MeshGeometryModel3D() {
                    CullMode = CullMode.Back,
                    Geometry = SphereModel,
                    Material = m,
                    IsThrowingShadow = true,
                    Transform = new Media3D.TranslateTransform3D(new Vector3D(i * 6, 0, j * 6))
                });
            }
        }

        builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero, 8, 12, 12);
        Model = builder.ToMesh();
        Material = new PbrMaterial() {
            AlbedoColor = albedoColor.ToColor4(),
            RenderEnvironmentMap = true,
            AlbedoMap = TextureModel.Create("Engraved_Metal_COLOR.jpg"),
            NormalMap = TextureModel.Create("Engraved_Metal_NORM.jpg"),
            DisplacementMap = TextureModel.Create("Engraved_Metal_DISP.png"),
            RoughnessMetallicMap = TextureModel.Create("Engraved_Metal_RMC.png"),
            DisplacementMapScaleMask = new Vector4(0.1f, 0.1f, 0.1f, 0),
            EnableAutoTangent = true,
            EnableTessellation = true,
            MaxDistanceTessellationFactor = 2,
            MinDistanceTessellationFactor = 4
        };
        ModelTransform = new Media3D.MatrixTransform3D(Translation(0, 30, 0)
            .ToMatrix3D());

        builder = new MeshBuilder();
        builder.AddBox(Vector3.Zero, 100, 0.5, 100);
        var floorGeo = builder.ToMesh();
        var textureCoordinates = floorGeo.TextureCoordinates
                                 ?? throw new InvalidOperationException(
                                     "The floor geometry has no texture coordinates.");
        for (int i = 0; i < textureCoordinates.Count; ++i) {
            textureCoordinates[i] *= 5;
        }

        FloorModel = floorGeo;
        FloorMaterial = new PbrMaterial() {
            AlbedoMap = TextureModel.Create("Wood_Planks_COLOR.jpg"),
            NormalMap = TextureModel.Create("Wood_Planks_NORM.jpg"),
            DisplacementMap = TextureModel.Create("Wood_Planks_DISP.png"),
            RoughnessMetallicMap = TextureModel.Create("Wood_Planks_RMA.png"),
            AmbientOcculsionMap = TextureModel.Create("Wood_Planks_RMA.png"),
            DisplacementMapScaleMask = new Vector4(1f, 1f, 1f, 0),
            RoughnessFactor = 0.8,
            MetallicFactor = 0.2,
            RenderShadowMap = true,
            EnableAutoTangent = true,
        };
        FloorModelTransform = new Media3D.MatrixTransform3D(Translation(0, -5, 0)
            .ToMatrix3D());
    }

    private static Matrix Translation(float x, float y, float z) {
        var matrix = Matrix.Identity;
        matrix.M41 = x;
        matrix.M42 = y;
        matrix.M43 = z;
        return matrix;
    }
}