// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Constructor of the MainViewModel
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DeferredShadingDemo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Animation;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using RotateTransform3D = System.Windows.Media.Media3D.RotateTransform3D;
using ScaleTransform3D = System.Windows.Media.Media3D.ScaleTransform3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Transform3DGroup = System.Windows.Media.Media3D.Transform3DGroup;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D Plane { get; private set; }
    public LineGeometry3D? Lines { get; private set; }
    public LineGeometry3D? Grid { get; private set; }

    public PhongMaterial RedMaterial { get; private set; }
    public PhongMaterial GreenMaterial { get; private set; }
    public PhongMaterial BlueMaterial { get; private set; }
    public PhongMaterial PlaneMaterial { get; private set; }
    public IRenderTechnique RenderTechnique { get; private set; }

    public Transform3D Model1Transform { get; private set; }
    public Transform3D Model2Transform { get; private set; }
    public Transform3D Model3Transform { get; private set; }
    public Transform3D PlaneTransform { get; private set; }

    public Color AmbientLightColor { get; private set; }
    public Color DirectionalLightColor { get; set; }
    public Vector3D DirectionalLightDirection { get; set; }

    public Transform3D PointLightTransform1 { get; private set; }
    public Transform3D PointLightTransform2 { get; private set; }
    public Transform3D PointLightTransform3 { get; private set; }

    public ObservableElement3DCollection PointLightCollection { get; set; }

    public Color PointLightColor {
        get;
        set {
            field = value;
            UpdatePointLightCollection();
        }
    }

    public Vector3D PointLightAttenuation {
        get;
        set {
            field = value;
            UpdatePointLightCollection();
        }
    }

    public int PointLightCount {
        get => PointLightCollection.Count;
        set => InitPointLightCollection(value);
    }

    public int PointLightSpread {
        get;
        set {
            field = value;
            InitPointLightCollection(PointLightCount);
        }
    }

    public ObservableElement3DCollection SpotLightCollection { get; set; }

    public Color SpotLightColor {
        get;
        set {
            field = value;
            UpdateSpotLightCollection();
        }
    }

    public Vector3D SpotLightAttenuation {
        get;
        set {
            field = value;
            UpdateSpotLightCollection();
        }
    }

    public int SpotLightCount {
        get => SpotLightCollection.Count;
        set => InitSpotLightCollection(value);
    }

    public double SpotLightSpread {
        get;
        set {
            field = value;
            InitSpotLightCollection(SpotLightCount);
        }
    }

    public IEnumerable<int> SamplesMsaa {
        get {
            yield return 1;
            yield return 2;
            yield return 4;
            yield return 8;
        }
    }

    /// <summary>
    /// Constructor of the MainViewModel
    /// </summary>
    [Obsolete]
    public MainViewModel() {
        // titles
        Title = "Deferred Shading Demo";
        SubTitle = "WPF & SharpDX";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(18, 64, 30),
            LookDirection = new Vector3D(-18, -64, -30),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // deferred render technique

        EffectsManager = new DefaultEffectsManager();
        RenderTechnique = EffectsManager[DeferredRenderTechniqueNames.Deferred];
        //load model
        var reader = new ObjReader();
        var objModel = reader.Read(@"./Media/bunny.obj");
        Model = objModel.Select(x => x.Geometry)
                    .OfType<MeshGeometry3D>()
                    .FirstOrDefault()
                ?? throw new InvalidOperationException("The deferred shading model could not be loaded.");
        var scale = 2.0;

        // model trafos
        var transf1 = new Transform3DGroup();
        transf1.Children.Add(new ScaleTransform3D(scale, scale, scale));
        transf1.Children.Add(new RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 1, 0), 40),
            0.0,
            0.0,
            0.0));
        transf1.Children.Add(new TranslateTransform3D(0, -2, 3));
        Model1Transform = transf1;

        var transf2 = new Transform3DGroup();
        transf2.Children.Add(new ScaleTransform3D(scale, scale, scale));
        transf2.Children.Add(
            new RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 1, 0), 66), 0.0, 0.0, 0.0));
        transf2.Children.Add(new TranslateTransform3D(-3.0, -2, -2.5));
        Model2Transform = transf2;

        var transf3 = new Transform3DGroup();
        transf3.Children.Add(new ScaleTransform3D(scale, scale, scale));
        transf3.Children.Add(new TranslateTransform3D(+3.5, -2, -1.0));
        Model3Transform = transf3;

        // floor plane
        var meshBuilder = new MeshBuilder();
        meshBuilder.AddBox(new Vector3(0, 0, 0), 100, 0.0, 100, BoxFaces.PositiveY);
        Plane = meshBuilder.ToMeshGeometry3D();
        PlaneTransform = new TranslateTransform3D(0, -1.05, 0);

        // model materials
        RedMaterial = PhongMaterials.Red;
        GreenMaterial = PhongMaterials.Green;
        BlueMaterial = PhongMaterials.Blue;
        PlaneMaterial = PhongMaterials.DefaultVrml;
        PlaneMaterial.DiffuseMap =
            LoadFileToMemory(new Uri(@"./Media/TextureCheckerboard2.jpg", UriKind.RelativeOrAbsolute)
                .ToString());
        PlaneMaterial.NormalMap =
            LoadFileToMemory(new Uri(@"./Media/TextureCheckerboard2_dot3.jpg", UriKind.RelativeOrAbsolute)
                .ToString());

        // setup lighting            
        AmbientLightColor = Colors.DarkGray;
        DirectionalLightColor = Colors.Gray;
        DirectionalLightDirection = new Vector3D(-2, -5, -2);

        PointLightColor = Colors.White;
        PointLightAttenuation = new Vector3D(0.0f, 0.0f, 0.18f); //1/0/0 ; 0.1, 0.2, 0.3
        PointLightTransform1 = new TranslateTransform3D(new Vector3D(0, 1, 0));
        PointLightTransform2 = new TranslateTransform3D(new Vector3D(6, 1, 3));
        PointLightTransform3 = new TranslateTransform3D(new Vector3D(-3, 1, -6));

        SpotLightColor = Colors.AntiqueWhite;
        SpotLightAttenuation = new Vector3D(1.0, 0.1, 0.01);

        // light collection
        PointLightCollection = [];
        PointLightCount = 7;
        PointLightSpread = 100;

        // spotlight collection
        SpotLightCollection = [];
        SpotLightCount = 7;
        SpotLightSpread = 100;
    }

    /// <summary>
    /// Init the PointLight Collection
    /// </summary>
    /// <param name="numberLights"></param>
    private void InitPointLightCollection(int numberLights) {
        // store the current technique
        //  var technique = this.RenderTechnique;

        // detouch the renderer
        //   this.RenderTechnique = null;

        // random            
        var rndx = new Random();
        var rndy = new Random(rndx.Next());
        var rndz = new Random(rndy.Next());
        var spread = PointLightSpread;

        // re-generate the lights
        PointLightCollection.Clear();
        for (int i = 0; i < numberLights; i++) {
            var pointLight = new PointLight3D() {
                Color = PointLightColor,
                Attenuation = PointLightAttenuation,
                Transform = CreateAnimatedTransform(
                    new Vector3D(rndx.NextDouble() * spread - spread / 2.0, 1, rndz.NextDouble() * spread - spread / 2),
                    new Vector3D(0, 1, 0),
                    rndx.Next(10) + 4),
            };
            PointLightCollection.Add(pointLight);
        }

        // attach the renderer
        //    this.RenderTechnique = technique;
    }

    /// <summary>
    /// Update Pointlights
    /// </summary>
    private void UpdatePointLightCollection() {
        for (int i = 0; i < PointLightCollection.Count; i++) {
            if (PointLightCollection[i] is PointLight3D pointLight) {
                pointLight.Attenuation = PointLightAttenuation;
                pointLight.Color = PointLightColor;
            }
        }
    }

    /// <summary>
    /// Init the Spotlight Collection
    /// </summary>
    /// <param name="numberLights"></param>
    private void InitSpotLightCollection(int numberLights) {
        // store the current technique
        //var technique = this.RenderTechnique;

        // detouch the renderer
        //this.RenderTechnique = null;

        // random            
        var rndx = new Random();
        var rndy = new Random(rndx.Next());
        var rndz = new Random(rndy.Next());
        var spread = SpotLightSpread;

        // re-generate the lights
        SpotLightCollection.Clear();
        for (int i = 0; i < numberLights; i++) {
            var spotLight = new SpotLight3D() {
                Color = SpotLightColor,
                Attenuation = SpotLightAttenuation,
                //OuterAngle = 90,
                //InnerAngle = 88,
                Position = new Point3D(0, 20, 0),
                Direction = new Vector3D(0, -1, 0),
                Transform = CreateAnimatedDirection(-new Vector3D(0, -1, 0),
                    (2 * rndx.NextDouble() - 1) * new Vector3D(1, 0, 0) +
                    (2 * rndz.NextDouble() - 1) * new Vector3D(0, 0, 1),
                    rndx.Next(10) + 8),
            };
            SpotLightCollection.Add(spotLight);
        }

        // attach the renderer
        //this.RenderTechnique = technique;
    }

    /// <summary>
    /// Update Spotlights
    /// </summary>
    private void UpdateSpotLightCollection() {
        for (int i = 0; i < SpotLightCollection.Count; i++) {
            if (SpotLightCollection[i] is SpotLight3D spotLight) {
                spotLight.Attenuation = SpotLightAttenuation;
                spotLight.Color = SpotLightColor;
            }
        }
    }

    /// <summary>
    /// Create animation for positions
    /// </summary>
    private Transform3D CreateAnimatedTransform(Vector3D translate, Vector3D axis, double speed = 4) {
        var lightTrafo = new Transform3DGroup();
        lightTrafo.Children.Add(new TranslateTransform3D(translate));

        var rotateAnimation = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            By = new Media3D.AxisAngleRotation3D(axis, 90),
            Duration = TimeSpan.FromSeconds(speed / 4),
            IsCumulative = true,
        };

        var rotateTransform = new RotateTransform3D();
        rotateTransform.BeginAnimation(RotateTransform3D.RotationProperty, rotateAnimation);
        lightTrafo.Children.Add(rotateTransform);

        return lightTrafo;
    }

    /// <summary>
    /// Create animation for directions
    /// </summary>
    private Transform3D CreateAnimatedDirection(Vector3D translate, Vector3D axis, double speed = 4) {
        var lightTrafo = new Transform3DGroup();
        lightTrafo.Children.Add(new TranslateTransform3D(translate));

        var rotateAnimation = new Rotation3DAnimation {
            RepeatBehavior = RepeatBehavior.Forever,
            From = new Media3D.AxisAngleRotation3D(axis, 90),
            To = new Media3D.AxisAngleRotation3D(axis, 270),
            AutoReverse = true,
            Duration = TimeSpan.FromSeconds(speed / 4),
            //IsCumulative = true,                  
        };

        var rotateTransform = new RotateTransform3D();
        rotateTransform.BeginAnimation(RotateTransform3D.RotationProperty, rotateAnimation);
        lightTrafo.Children.Add(rotateTransform);

        return lightTrafo;
    }

    /// <summary>
    /// Load OBJ file
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="faces"></param>
    [Obsolete]
    private void LoadModel(string filename, MeshFaces faces) {
        // load model
        var reader = new ObjReader();
        var objModel = reader.Read(filename, new ModelInfo() {
            Faces = MeshFaces.Default
        });
        //this.Model = objModel[0].Geometry as MeshGeometry3D;
        //this.Model.Colors = this.Model.Positions.Select(x => new Color4(1, 0, 0, 1)).ToArray();
    }

    private static MemoryStream LoadFileToMemory(string filePath) {
        using var file = new FileStream(filePath, FileMode.Open);
        var memory = new MemoryStream();
        file.CopyTo(memory);
        memory.Position = 0;
        return memory;
    }


    private string meshTopology = MeshFaces.Default.ToString();
}

public class ColorVectorConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is Color4 color
            ? color.ToColor()
            : value;

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        System.Globalization.CultureInfo culture
    )
        => targetType == typeof(Color) ? value :
            value is Color color ? color.ToColor4() : null;
}