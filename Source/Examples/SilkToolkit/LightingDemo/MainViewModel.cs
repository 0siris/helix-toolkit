// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LightingDemo;

using System;
using System.Windows.Media.Animation;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public string Name { get; set; }

    public MainViewModel ViewModel => this;

    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D Floor { get; private set; }
    public MeshGeometry3D Sphere { get; private set; }
    public MeshGeometry3D FlyingObject { get; private set; }
    public LineGeometry3D CubeEdges { get; private set; }
    public Transform3D ModelTransform { get; private set; }
    public Transform3D Model1Transform { get; private set; }
    public Transform3D FloorTransform { get; private set; }
    public Transform3D Light1Transform { get; private set; }
    public Transform3D Light2Transform { get; private set; }
    public Transform3D Light3Transform { get; private set; }
    public Transform3D Light4Transform { get; private set; }
    public Transform3D Light1DirectionTransform { get; private set; }
    public Transform3D Light4DirectionTransform { get; private set; }

    public Transform3D Object1Transform { get; private set; }
    public Transform3D Object2Transform { get; private set; }
    public Transform3D Object3Transform { get; private set; }
    public Transform3D Object4Transform { get; private set; }

    public Transform3D Object5Transform { get; private set; }
    public Transform3D Object6Transform { get; private set; }
    public Transform3D Object7Transform { get; private set; }
    public Transform3D Object8Transform { get; private set; }

    public PhongMaterial ModelMaterial { get; set; }
    public PhongMaterial ReflectMaterial { get; set; }
    public PhongMaterial FloorMaterial { get; set; }
    public PhongMaterial LightModelMaterial { get; set; }
    public PhongMaterial ObjectMaterial { set; get; } = PhongMaterials.Red;

    public Vector3D Light1Direction { get; set; }
    public Vector3D Light4Direction { get; set; }
    public Vector3D LightDirection4 { get; set; }
    public Color Light1Color { get; set; }
    public Color Light2Color { get; set; }
    public Color Light3Color { get; set; }
    public Color Light4Color { get; set; }
    public Color AmbientLightColor { get; set; }
    public Vector3D Light2Attenuation { get; set; }
    public Vector3D Light3Attenuation { get; set; }
    public Vector3D Light4Attenuation { get; set; }
    public bool RenderLight1 { get; set; }
    public bool RenderLight2 { get; set; }
    public bool RenderLight3 { get; set; }
    public bool RenderLight4 { get; set; }

    public bool RenderDiffuseMap {
        set {
            if (SetValue(ref field, value)) {
                ModelMaterial.RenderDiffuseMap = FloorMaterial.RenderDiffuseMap = value;
            }
        }
        get => field;
    } = true;

    public bool RenderNormalMap {
        set {
            if (SetValue(ref field, value)) {
                ModelMaterial.RenderNormalMap = FloorMaterial.RenderNormalMap = value;
            }
        }
        get => field;
    } = true;

    public string[] TextureFiles { get; } = [
        @"TextureCheckerboard2.jpg", @"TextureCheckerboard3.jpg", @"TextureNoise1.jpg", @"TextureNoise1_dot3.jpg",
        @"TextureCheckerboard2_dot3.jpg"
    ];

    public string SelectedDiffuseTexture {
        set {
            if (SetValue(ref field, value)) {
                ModelMaterial.DiffuseMap =
                    TextureModel.Create(new Uri(value, UriKind.RelativeOrAbsolute).ToString());
                FloorMaterial.DiffuseMap = ModelMaterial.DiffuseMap;
            }
        }
        get => field;
    } = @"TextureCheckerboard2.jpg";

    public string SelectedNormalTexture {
        set {
            if (SetValue(ref field, value)) {
                ModelMaterial.NormalMap =
                    TextureModel.Create(new Uri(value, UriKind.RelativeOrAbsolute).ToString());
                FloorMaterial.NormalMap = ModelMaterial.NormalMap;
            }
        }
        get => field;
    } = @"TextureCheckerboard2_dot3.jpg";

    public Color DiffuseColor {
        set => FloorMaterial.DiffuseColor = ModelMaterial.DiffuseColor = value.ToColor4();
        get => ModelMaterial.DiffuseColor.ToColor();
    }


    public Color ReflectiveColor {
        set => FloorMaterial.ReflectiveColor = ModelMaterial.ReflectiveColor = value.ToColor4();
        get => ModelMaterial.ReflectiveColor.ToColor();
    }

    public Color EmissiveColor {
        set => FloorMaterial.EmissiveColor = ModelMaterial.EmissiveColor = value.ToColor4();
        get => ModelMaterial.EmissiveColor.ToColor();
    }

    public MsaaLevel MSAA { set; get; } = MsaaLevel.Disable;

    public MsaaLevel[] MSAAs { get; } = [MsaaLevel.Disable, MsaaLevel.Two, MsaaLevel.Four, MsaaLevel.Eight, MsaaLevel.Maximum];

    public FxaaLevel FXAA { set; get; } = FxaaLevel.None;

    public FxaaLevel[] FXAAs { get; } = [FxaaLevel.None, FxaaLevel.Low, FxaaLevel.Medium, FxaaLevel.High, FxaaLevel.Ultra];

    public Camera Camera2 { get; } = new PerspectiveCamera {
        Position = new Point3D(8, 9, 7), LookDirection = new Vector3D(-5, -12, -5), UpDirection = new Vector3D(0, 1, 0)
    };

    public Camera Camera3 { get; } = new PerspectiveCamera {
        Position = new Point3D(8, 9, 7), LookDirection = new Vector3D(-5, -12, -5), UpDirection = new Vector3D(0, 1, 0)
    };

    public Camera Camera4 { get; } = new PerspectiveCamera {
        Position = new Point3D(8, 9, 7), LookDirection = new Vector3D(-5, -12, -5), UpDirection = new Vector3D(0, 1, 0)
    };

    public MainViewModel() {
        //    RenderTechniquesManager = new DefaultRenderTechniquesManager();
        EffectsManager = new DefaultEffectsManager();
        // ----------------------------------------------
        // titles
        Title = "Lighting Demo";
        SubTitle = "WPF & SharpDX";

        // ----------------------------------------------
        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(8, 9, 7), LookDirection = new Vector3D(-5, -12, -5),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // ----------------------------------------------
        // setup scene
        AmbientLightColor = Colors.DarkGray;

        RenderLight1 = true;
        RenderLight2 = true;
        RenderLight3 = true;
        RenderLight4 = true;

        Light1Color = Colors.White;
        Light2Color = Colors.Red;
        Light3Color = Colors.LightYellow;
        Light4Color = Colors.LightBlue;

        Light2Attenuation = new Vector3D(1.0f, 0.5f, 0.10f);
        Light3Attenuation = new Vector3D(1.0f, 0.1f, 0.05f);
        Light4Attenuation = new Vector3D(0.1f, 0.1f, 0.0f);

        Light1Direction = new Vector3D(0, -10, 0);
        Light1Transform = CreateAnimatedTransform1(-Light1Direction, new Vector3D(1, 0, 0), 24);
        Light1DirectionTransform = CreateAnimatedTransform2(-Light1Direction, new Vector3D(0, 1, -1), 24);

        Light2Transform = CreateAnimatedTransform1(new Vector3D(-4, 0, 0), new Vector3D(0, 0, 1), 3);
        Light3Transform = CreateAnimatedTransform1(new Vector3D(0, 0, 4), new Vector3D(0, 1, 0), 5);

        Light4Direction = new Vector3D(0, -5, -1);
        Light4Transform = CreateAnimatedTransform2(-Light4Direction * 2, new Vector3D(0, 1, 0), 24);
        Light4DirectionTransform = CreateAnimatedTransform2(-Light4Direction, new Vector3D(1, 0, 0), 12);

        var transformGroup = new Media3D.Transform3DGroup();
        transformGroup.Children.Add(new Media3D.ScaleTransform3D(10, 10, 10));
        transformGroup.Children.Add(new Media3D.TranslateTransform3D(2, -4, 2));
        Model1Transform = transformGroup;
        // ----------------------------------------------
        // light model3d
        var sphere = new MeshBuilder();
        sphere.AddSphere(new Vector3(0, 0, 0), 0.2);
        Sphere = sphere.ToMeshGeometry3D();
        LightModelMaterial = new PhongMaterial {
            AmbientColor = Colors.Gray.ToColor4(),
            DiffuseColor = Colors.Gray.ToColor4(),
            EmissiveColor = Colors.Yellow.ToColor4(),
            SpecularColor = Colors.Black.ToColor4(),
        };

        // ----------------------------------------------
        // scene model3d
        var b1 = new MeshBuilder(true, true, true);
        b1.AddSphere(new Vector3(0.25f, 0.25f, 0.25f), 0.75, 24, 24);
        b1.AddBox(-new Vector3(0.25f, 0.25f, 0.25f), 1, 1, 1, BoxFaces.All);
        b1.AddBox(-new Vector3(5.0f, 0.0f, 0.0f), 1, 1, 1, BoxFaces.All);
        b1.AddSphere(new Vector3(5f, 0f, 0f), 0.75, 24, 24);
        b1.AddCylinder(new Vector3(0f, -3f, -5f), new Vector3(0f, 3f, -5f), 1.2, 24);
        b1.AddSphere(new Vector3(-5.0f, -5.0f, 5.0f), 4, 24, 64);
        b1.AddCone(new Vector3(6f, -9f, -6f), new Vector3(6f, -1f, -6f), 4f, true, 64);
        Model = b1.ToMeshGeometry3D();
        ModelTransform = new Media3D.TranslateTransform3D(0, 0, 0);
        ModelMaterial = PhongMaterials.Chrome;

        ModelMaterial.NormalMap =
            TextureModel.Create(new Uri(SelectedNormalTexture, UriKind.RelativeOrAbsolute).ToString());

        // ----------------------------------------------
        // floor model3d
        var b2 = new MeshBuilder(true, true, true);
        //b2.AddRectangularMesh(BoxFaces.Left, 10, 10, 10, 10);
        b2.AddBox(new Vector3(0.0f, -5.0f, 0.0f), 15, 1, 15, BoxFaces.All);
        //b2.AddSphere(new Vector3(-5.0f, -5.0f, 5.0f), 4, 24, 64);
        //b2.AddCone(new Vector3(6f, -9f, -6f), new Vector3(6f, -1f, -6f), 4f, true, 64);
        Floor = b2.ToMeshGeometry3D();
        FloorTransform = new Media3D.TranslateTransform3D(0, 0, 0);
        FloorMaterial = new PhongMaterial {
            AmbientColor = Colors.Gray.ToColor4(),
            DiffuseColor = new Color4(0.75f, 0.75f, 0.75f, 1.0f),
            SpecularColor = Colors.White.ToColor4(),
            SpecularShininess = 100f,
            DiffuseMap =
                TextureModel.Create(
                    new Uri(SelectedDiffuseTexture, UriKind.RelativeOrAbsolute).ToString()),
            NormalMap = ModelMaterial.NormalMap,
            RenderShadowMap = true
        };
        ModelMaterial.DiffuseMap = FloorMaterial.DiffuseMap;

        ReflectMaterial = PhongMaterials.PolishedSilver;
        ReflectMaterial.ReflectiveColor = Colors.Silver.ToColor4();
        ReflectMaterial.RenderEnvironmentMap = true;
        InitialObjectTransforms();
    }

    private void InitialObjectTransforms() {
        var b = new MeshBuilder(true);
        b.AddTorus(1, 0.5);
        b.AddTetrahedron(new Vector3(), new Vector3(1, 0, 0), new Vector3(0, 1, 0), 1.1);
        FlyingObject = b.ToMesh();
        var random = new Random();
        Object1Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-5, 5)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-5, 5)),
            random.NextDouble(2, 10));
        Object2Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object3Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object4Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object5Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object6Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object7Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
        Object8Transform = CreateAnimatedTransform1(
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-10, 10), random.NextDouble(-10, 10)),
            new Vector3D(random.NextDouble(-5, 5), random.NextDouble(-5, 5), random.NextDouble(-10, 10)),
            random.NextDouble(2, 10));
    }

    private Transform3D CreateAnimatedTransform1(Vector3D translate, Vector3D axis, double speed = 4) {
        var lightTrafo = new Media3D.Transform3DGroup();
        lightTrafo.Children.Add(new Media3D.TranslateTransform3D(translate));

        var rotateAnimation = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            By = new Media3D.AxisAngleRotation3D(axis, 90),
            Duration = TimeSpan.FromSeconds(speed / 4),
            IsCumulative = true,
        };

        var rotateTransform = new Media3D.RotateTransform3D();
        rotateTransform.BeginAnimation(Media3D.RotateTransform3D.RotationProperty, rotateAnimation);
        lightTrafo.Children.Add(rotateTransform);

        return lightTrafo;
    }

    private Transform3D CreateAnimatedTransform2(Vector3D translate, Vector3D axis, double speed = 4) {
        var lightTrafo = new Media3D.Transform3DGroup();
        lightTrafo.Children.Add(new Media3D.TranslateTransform3D(translate));

        var rotateAnimation = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            //By = new Media3D.AxisAngleRotation3D(axis, 180),
            From = new Media3D.AxisAngleRotation3D(axis, 135),
            To = new Media3D.AxisAngleRotation3D(axis, 225),
            AutoReverse = true,
            Duration = TimeSpan.FromSeconds(speed / 4),
            //IsCumulative = true,
        };

        var rotateTransform = new Media3D.RotateTransform3D();
        rotateTransform.BeginAnimation(Media3D.RotateTransform3D.RotationProperty, rotateAnimation);
        lightTrafo.Children.Add(rotateTransform);
        return lightTrafo;
    }
}

internal static class RandomExtensions {
    public static double NextDouble(this Random random, double min, double max) => min + (max - min) * random.NextDouble();
}
