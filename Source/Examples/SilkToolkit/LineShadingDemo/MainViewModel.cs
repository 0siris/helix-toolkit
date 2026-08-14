// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LineShadingDemo;

using System;
using System.Linq;
using DemoCore;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Media = System.Windows.Media;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public LineGeometry3D Lines { get; private set; }
    public LineGeometry3D Grid { get; private set; }
    public double LineThickness { get; set; }
    public double LineThicknessMaximum => FixedSize ? 10 : 0.05;
    public double LineThicknessTickFrequency => FixedSize ? 1 : 0.005;
    public double LineSmoothness { get; set; }
    public bool LinesEnabled { get; set; }
    public bool GridEnabled { get; set; }

    public bool FixedSize {
        get;
        set {
            field = value;
            LineThickness = FixedSize ? 2 : 0.005;
        }
    } = true;

    public PhongMaterial Material1 { get; private set; }
    public PhongMaterial Material2 { get; private set; }
    public PhongMaterial Material3 { get; private set; }
    public LineMaterial LineMaterial { get; private set; }
    public LineMaterial GridMaterial { private set; get; }
    public Color GridColor { get; private set; }

    public Transform3D Model1Transform { get; private set; }
    public Transform3D Model2Transform { get; private set; }
    public Transform3D Model3Transform { get; private set; }
    public Transform3D Model4Transform { get; private set; }
    public Transform3D GridTransform { get; private set; }

    public Vector3D DirectionalLightDirection { get; private set; }
    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }

    public bool EnableArrowHeadTail {
        set {
            if (SetValue(ref field, value)) {
                var texture = LineMaterial.Texture;
                var tscale = LineMaterial.TextureScale;
                LineMaterial = value
                                   ? new LineArrowHeadTailMaterial() {
                                       ArrowSize = 0.04, Color = Colors.White, Texture = texture, TextureScale = tscale
                                   }
                                   : new LineArrowHeadMaterial() {
                                       ArrowSize = 0.04, Color = Colors.White, Texture = texture, TextureScale = tscale
                                   };
                OnPropertyChanged(nameof(LineMaterial));
            }
        }
        get => field;
    } = false;

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        Title = "Line Shading Demo (HelixToolkitDX)";
        SubTitle = null;

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(0, 5, 5), LookDirection = new Vector3D(-0, -5, -5),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // setup lighting            
        AmbientLightColor = Colors.DimGray;
        DirectionalLightColor = Colors.White;
        DirectionalLightDirection = new Vector3D(-2, -5, -2);

        // floor plane grid
        Grid = LineBuilder.GenerateGrid();
        GridColor = Colors.Black;
        GridTransform = new TranslateTransform3D(-5, -1, -5);

        // scene model3d
        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0, 0, 0), 0.5);
        b1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 2, BoxFaces.All);
        Model = b1.ToMeshGeometry3D();

        // lines model3d
        var e1 = new LineBuilder();
        e1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 2);
        //this.Lines = e1.ToLineGeometry3D().ToUnshared();
        Lines = e1.ToLineGeometry3D(true);
        Lines.Colors = [];
        var linesCount = Lines.Indices.Count;
        var rnd = new Random();
        while (linesCount-- > 0) {
            Lines.Colors.Add(new Color4((float)rnd.NextDouble(),
                                             (float)rnd.NextDouble(),
                                             (float)rnd.NextDouble(),
                                             1f));
        }

        // lines params
        LineThickness = 2;
        LineSmoothness = 2.0;
        LinesEnabled = true;
        GridEnabled = true;

        // model trafos
        Model1Transform = new TranslateTransform3D(0, 0, 0);
        Model2Transform = new TranslateTransform3D(-2, 0, 0);
        Model3Transform = new TranslateTransform3D(+2, 0, 0);
        Model4Transform = new TranslateTransform3D(0, 2, 0);
        // model materials
        Material1 = PhongMaterials.PolishedGold;
        Material2 = PhongMaterials.Copper;
        Material3 = PhongMaterials.Glass;
        LineMaterial = new LineArrowHeadMaterial() { ArrowSize = 0.04, Color = Colors.White, TextureScale = 0.4 };
        GridMaterial = new LineMaterial() { Color = Colors.Red, TextureScale = 0.4 };
        var dash = TextureModel.Create("Dash.png");
        var dotLine = TextureModel.Create("DotLine.png");
        GridMaterial.Texture = dotLine;
        LineMaterial.Texture = dash;
    }
}
