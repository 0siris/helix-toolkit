// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Helper;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace CustomShaderDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Materials;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D SphereModel { get; private set; }
    public LineGeometry3D AxisModel { get; private set; }
    public BillboardText3D AxisLabel { private set; get; }
    public ColorStripeMaterial ModelMaterial { get; private set; } = new();
    public PhongMaterial SphereMaterial { private set; get; } = PhongMaterials.Copper;

    public PointGeometry3D PointModel { private set; get; }

    public CustomPointMaterial CustomPointMaterial { get; }

    public Transform3D PointTransform { get; } = new TranslateTransform3D(10, 0, 0);

    private Color startColor;

    /// <summary>
    /// Gets or sets the StartColor.
    /// </summary>
    /// <value>
    /// StartColor
    /// </value>
    public Color StartColor {
        set {
            if (SetValue(ref startColor, value)) {
                ColorGradient = [
                    .. GetGradients(startColor.ToColor4(),
                        midColor.ToColor4(),
                        endColor.ToColor4(),
                        100)
                ];
            }
        }
        get => startColor;
    }

    private Color midColor;

    /// <summary>
    /// Gets or sets the StartColor.
    /// </summary>
    /// <value>
    /// StartColor
    /// </value>
    public Color MidColor {
        set {
            if (SetValue(ref midColor, value)) {
                ColorGradient = [
                    .. GetGradients(startColor.ToColor4(),
                        midColor.ToColor4(),
                        endColor.ToColor4(),
                        100)
                ];
            }
        }
        get => midColor;
    }

    private Color endColor;

    /// <summary>
    /// Gets or sets the StartColor.
    /// </summary>
    /// <value>
    /// StartColor
    /// </value>
    public Color EndColor {
        set {
            if (SetValue(ref endColor, value)) {
                ColorGradient = [
                    .. GetGradients(startColor.ToColor4(),
                        midColor.ToColor4(),
                        endColor.ToColor4(),
                        100)
                ];
            }
        }
        get => endColor;
    }

    public Color4Collection ColorGradient {
        private set {
            if (SetValue(ref field, value)) {
                ModelMaterial.ColorStripeX = value;
            }
        }
        get;
    } = [];

    public FillMode FillMode {
        set => SetValue(ref field, value);
        get;
    } = FillMode.Solid;

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                FillMode = value
                    ? FillMode.Wireframe
                    : FillMode.Solid;
            }
        }
        get;
    } = false;

    private int width = 100;
    private int height = 100;

    public ICommand GenerateNoiseCommand { private set; get; }

    public MainViewModel() {
        // titles
        Title = "Simple Demo";
        SubTitle = "WPF & SharpDX";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(-6, 8, 23),
            LookDirection = new Vector3D(11, -4, -23),
            UpDirection = new Vector3D(0, 1, 0),
            FarPlaneDistance = 5000
        };

        EffectsManager = new CustomEffectsManager();


        var builder = new MeshBuilder(true);
        Vector3[] points = new Vector3[width * height];
        for (int i = 0; i < width; ++i) {
            for (int j = 0; j < height; ++j) {
                points[i * width + j] = new Vector3(i / 10f, 0, j / 10f);
            }
        }

        builder.AddRectangularMesh(points, width);
        var model = builder.ToMesh();
        var normals = model.Normals
                      ?? throw new InvalidOperationException("The generated mesh has no normals.");
        Model = model;
        for (int i = 0; i < normals.Count; ++i) {
            normals[i] = new Vector3(0, Math.Abs(normals[i].Y), 0);
        }

        StartColor = Colors.Blue;
        MidColor = Colors.Green;
        EndColor = Colors.Red;

        var lineBuilder = new LineBuilder();
        lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(10, 0, 0));
        lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 10, 0));
        lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, 10));

        var axisModel = lineBuilder.ToLineGeometry3D();
        var positions = axisModel.Positions
                        ?? throw new InvalidOperationException("The generated axis has no positions.");
        axisModel.Colors = new Color4Collection(positions.Count) {
            Colors.Red.ToColor4(),
            Colors.Red.ToColor4(),
            Colors.Green.ToColor4(),
            Colors.Green.ToColor4(),
            Colors.Blue.ToColor4(),
            Colors.Blue.ToColor4()
        };
        AxisModel = axisModel;

        AxisLabel = new BillboardText3D();
        AxisLabel.TextInfo.Add(new TextInfo() {
            Origin = new Vector3(11, 0, 0),
            Text = "X",
            Foreground = Colors.Red.ToColor4()
        });
        AxisLabel.TextInfo.Add(new TextInfo() {
            Origin = new Vector3(0, 11, 0),
            Text = "Y",
            Foreground = Colors.Green.ToColor4()
        });
        AxisLabel.TextInfo.Add(new TextInfo() {
            Origin = new Vector3(0, 0, 11),
            Text = "Z",
            Foreground = Colors.Blue.ToColor4()
        });

        builder = new MeshBuilder(true);
        builder.AddSphere(new Vector3(-15, 0, 0), 5);
        var sphereModel = builder.ToMesh();
        SphereModel = sphereModel;

        GenerateNoiseCommand = new RelayCommand((_) => { CreatePerlinNoise(); });
        CreatePerlinNoise();

        PointModel = new PointGeometry3D() {
            Positions = sphereModel.Positions
                        ?? throw new InvalidOperationException("The generated sphere has no positions.")
        };
        CustomPointMaterial = new CustomPointMaterial() {
            Color = Colors.White
        };
    }

    public static IEnumerable<Color4> GetGradients(Color4 start, Color4 mid, Color4 end, int steps)
        => GetGradients(start, mid, steps / 2)
            .Concat(GetGradients(mid, end, steps / 2));

    public static IEnumerable<Color4> GetGradients(Color4 start, Color4 end, int steps) {
        float stepA = ((end.W - start.W) / (steps - 1));
        float stepR = ((end.X - start.X) / (steps - 1));
        float stepG = ((end.Y - start.Y) / (steps - 1));
        float stepB = ((end.Z - start.Z) / (steps - 1));

        for (int i = 0; i < steps; i++) {
            yield return new Color4((start.X + (stepR * i)),
                (start.Y + (stepG * i)),
                (start.Z + (stepB * i)),
                (start.W + (stepA * i)));
        }
    }

    private void CreatePerlinNoise() {
        float[] noise;
        MathHelper.GenerateNoiseMap(width, height, 8, out noise);
        Vector2Collection collection = new Vector2Collection(width * height);
        for (int i = 0; i < width; ++i) {
            for (int j = 0; j < height; ++j) {
                collection.Add(new Vector2(Math.Abs(noise[width * i + j]), 0));
            }
        }

        Model.TextureCoordinates = collection;
    }
}