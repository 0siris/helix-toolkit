// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace PolygonTriangulationDemo;

using System;
using DemoCore;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Media = System.Windows.Media;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    /// <summary>
    /// Thickness of the Lines of the Grid
    /// </summary>
    public double LineThickness { get; set; }

    /// <summary>
    /// Thickness of the Lines of the Triangulated Polygon
    /// </summary>
    public double TriangulationThickness { get; set; }

    /// <summary>
    /// The Grid
    /// </summary>
    public LineGeometry3D Grid { get; private set; }

    /// <summary>
    /// Color of the Gridlines
    /// </summary>
    public Media.Color GridColor { get; private set; }

    /// <summary>
    /// Color of the Triangle Lines
    /// </summary>
    public Media.Color TriangulationColor { get; private set; }

    /// <summary>
    /// Transform of the Grid
    /// </summary>
    public Transform3D GridTransform { get; private set; }

    /// <summary>
    /// Direction of the directional Light
    /// </summary>
    public Vector3 DirectionalLightDirection { get; private set; }

    /// <summary>
    /// Color of the directional Light
    /// </summary>
    public Color4 DirectionalLightColor { get; private set; }

    /// <summary>
    /// Color of the ambient Light
    /// </summary>
    public Color4 AmbientLightColor { get; private set; }

    /// <summary>
    /// Accessor to the Polygon-Material
    /// </summary>
    public PhongMaterial Material {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Transform of the Polygon
    /// </summary>
    public Transform3D ModelTransform { get; private set; }

    /// <summary>
    /// Transform of the Polygon Triangle Lines
    /// </summary>
    public Transform3D ModelLineTransform { get; private set; }

    /// <summary>
    /// Collection of Materials to chose from
    /// </summary>
    public PhongMaterialCollection Materials => PhongMaterials.Materials;

    /// <summary>
    /// Accessor to the Boolean
    /// </summary>
    public Boolean ShowTriangleLines {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The Point Count
    /// </summary>
    private int mPointCount;

    /// <summary>
    /// Access to the Point Count (restricted to the Range 3 - 10.000)
    /// </summary>
    public int PointCount {
        get => mPointCount;
        set {
            if (value < 3)
                mPointCount = 3;
            else if (value > 10000)
                mPointCount = 10000;
            else
                mPointCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Text representing the current PointCount
    /// </summary>
    public string PointCountText => "Number of Points: " + mPointCount;

    /// <summary>
    /// The Geometry for the Triangle Lines
    /// </summary>
    public LineGeometry3D? LineGeometry;

    /// <summary>
    /// Constructor of the MainViewModel
    /// Sets up allProperties
    /// </summary>
    public MainViewModel() {
        // Render Setup
        EffectsManager = new DefaultEffectsManager();
        // Window Setup
        Title = "Polygon Triangulation Demo";
        SubTitle = string.Empty;

        // Camera Setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(0, 5, 9),
            LookDirection = new Vector3D(0, -5, -4),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // Lines Setup
        LineThickness = 1;
        TriangulationThickness = .5;
        ShowTriangleLines = true;

        // Count Setup
        PointCount = 1000;

        // Lighting Setup
        AmbientLightColor = new Color4(.1f, .1f, .1f, 1.0f);
        DirectionalLightColor = new Color4(1, 1, 1, 1);
        DirectionalLightDirection = new Vector3(0, -1, 0);

        // Model Transformations
        ModelTransform = new TranslateTransform3D(0, 0, 0);
        ModelLineTransform = new TranslateTransform3D(0, 0.001, 0);

        // Model Materials and Colors
        Material = PhongMaterials.PolishedBronze;
        TriangulationColor = Media.Colors.Black;

        // Grid Setup
        Grid = LineBuilder.GenerateGrid(Vector3.UnitY, -5, 5, 0);
        GridColor = Media.Colors.DarkGray;
        GridTransform = new TranslateTransform3D(0, -0.01, 0);
    }
}