using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Animation;
using HelixToolkit.Wpf.SharpDX;
using Media3D = System.Windows.Media.Media3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace OctreeDemo;

public class DataModel : DemoCore.ObservableObject {
    [field: AllowNull, MaybeNull]
    public MeshGeometry3D Model {
        set => SetValue<MeshGeometry3D>(ref field, value, nameof(Model));
        get;
    } = null;

    public readonly Media3D.ScaleTransform3D ScaleTransform = new();
    public readonly Media3D.TranslateTransform3D TranslateTransform = new();
    public Media3D.Transform3DGroup DynamicTransform { get; private set; } = new();


    public PhongMaterial Material {
        set => SetValue<PhongMaterial>(ref field, value, nameof(Material));
        get;
    }

    public bool Highlight {
        set {
            if (field == value) {
                return;
            }

            field = value;
            if (field) {
                //orgMaterial = material;
                Material.EmissiveColor = Color.Yellow;
            } else {
                Material.EmissiveColor = Color.Transparent;
                //Material = orgMaterial;
            }
        }
        get;
    } = false;

    public DataModel() {
        DynamicTransform.Children.Add(ScaleTransform);
        DynamicTransform.Children.Add(TranslateTransform);
        Material = PhongMaterials.Red;
    }
}

public class SphereModel : DataModel {
    private static MeshGeometry3D _sphere;
    private static MeshGeometry3D _box;
    private static MeshGeometry3D _pyramid;
    private static MeshGeometry3D _pipe;

    static SphereModel() {
        var builder = new MeshBuilder(true, false, false);
        var center = new Vector3();
        builder.AddSphere(center, 1, 12, 12);
        _sphere = builder.ToMeshGeometry3D();
        builder = new MeshBuilder(true, false, false);
        builder.AddBox(center, 1, 1, 1);
        _box = builder.ToMeshGeometry3D();
        builder = new MeshBuilder(true, false, false);
        builder.AddPyramid(center, 1, 1, true);
        _pyramid = builder.ToMeshGeometry3D();
        builder = new MeshBuilder(true, false, false);
        builder.AddPipe(center, center + new Vector3(0, 1, 0), 0, 2, 12);
        _pipe = builder.ToMeshGeometry3D();
    }

    private static readonly Random rnd = new();

    public SphereModel(Vector3 center, double radius, bool enableTransform = true)
        : base() {
        Center = center;
        Radius = radius;
        CreateModel();
        if (enableTransform) {
            CreateAnimatedTransform1(DynamicTransform,
                                     center.ToVector3D(),
                                     new Media3D.Vector3D(rnd.Next(-1, 1), rnd.Next(-1, 1), rnd.Next(-1, 1)),
                                     rnd.Next(10, 100));
        }

        var color = rnd.NextColor();
        Material = new PhongMaterial() { DiffuseColor = color.ToColor4() };
    }

    public Vector3 Center {
        set {
            if (SetValue<Vector3>(ref field, value, nameof(Center))) {
                TranslateTransform.OffsetX = TranslateTransform.OffsetY = TranslateTransform.OffsetZ = value.X;
            }
        }
        get;
    }

    public double Radius {
        set {
            if (SetValue<double>(ref field, value, nameof(Radius))) {
                ScaleTransform.ScaleX = ScaleTransform.ScaleY = ScaleTransform.ScaleZ = value;
            }
        }
        get;
    } = 1;

    private void CreateModel() {
        int type = rnd.Next(0, 3);
        switch (type) {
            case 0:
                Model = _sphere;
                break;
            case 1:
                Model = _box;
                break;
            case 2:
                Model = _pyramid;
                break;
            case 3:
                Model = _pipe;
                break;
        }
    }

    private static Media3D.Transform3D CreateAnimatedTransform1(
        Media3D.Transform3DGroup transformGroup,
        Media3D.Vector3D center,
        Media3D.Vector3D axis,
        double speed = 4
    ) {
        var rotateAnimation = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            By = new Media3D.AxisAngleRotation3D(axis, 90),
            Duration = TimeSpan.FromSeconds(speed / 2),
            IsCumulative = true,
        };

        var rotateTransform = new Media3D.RotateTransform3D();
        rotateTransform.BeginAnimation(Media3D.RotateTransform3D.RotationProperty, rotateAnimation);

        transformGroup.Children.Add(rotateTransform);

        var rotateAnimation1 = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            By = new Media3D.AxisAngleRotation3D(axis, 240),
            Duration = TimeSpan.FromSeconds(speed / 4),
            IsCumulative = true,
        };

        var rotateTransform1 = new Media3D.RotateTransform3D {
            CenterX = center.X,
            CenterY = center.Y,
            CenterZ = center.Z
        };
        rotateTransform1.BeginAnimation(Media3D.RotateTransform3D.RotationProperty, rotateAnimation1);

        transformGroup.Children.Add(rotateTransform1);

        return transformGroup;
    }
}

internal static class RandomExtensions {
    public static double NextDouble(this Random random, double min, double max) => min + random.NextDouble() * (max - min);

    public static System.Windows.Media.Color NextColor(this Random random) => System.Windows.Media.Color.FromArgb(255,
        (byte)random.Next(256),
        (byte)random.Next(256),
        (byte)random.Next(256));
}
