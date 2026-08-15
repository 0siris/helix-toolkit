// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   load the model from obj-file
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace TessellationDemo;

using System.Collections.Generic;
using System;
using System.Linq;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public Geometry3D DefaultModel { get; private set; }
    public Geometry3D Grid { get; private set; }
    public Geometry3D FloorModel { private set; get; }
    public PhongMaterial DefaultMaterial { get; private set; }
    public PhongMaterial FloorMaterial { get; } = PhongMaterials.Silver;
    public Color GridColor { get; private set; }

    public Transform3D DefaultTransform { get; private set; }
    public Transform3D GridTransform { get; private set; }

    public Vector3D DirectionalLightDirection1 { get; private set; }
    public Vector3D DirectionalLightDirection2 { get; private set; }
    public Vector3D DirectionalLightDirection3 { get; private set; }
    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }

    public FillMode FillMode {
        set => SetValue(ref field, value);
        get;
    } = FillMode.Solid;

    public bool Wireframe {
        set {
            if (SetValue(ref field, value)) {
                if (value) {
                    FillMode = FillMode.Wireframe;
                } else {
                    FillMode = FillMode.Solid;
                }
            }
        }
        get;
    } = false;

    private MeshTopologyEnum meshTopology = MeshTopologyEnum.PnTriangles;

    public MeshTopologyEnum MeshTopology {
        get => meshTopology;
        set {
            /// if topology is changes, reload the model with proper type of faces
            meshTopology = value;
            DefaultModel = LoadModel(@"./Media/teapot_quads_tex.obj",
                meshTopology == MeshTopologyEnum.PnTriangles
                    ? MeshFaces.Default
                    : MeshFaces.QuadPatches);
        }
    }

    public IList<Matrix> Instances { private set; get; }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        // ----------------------------------------------
        // titles
        Title = "Hardware Tessellation Demo";
        SubTitle = "WPF & SharpDX";

        // ---------------------------------------------
        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(7, 10, 12),
            LookDirection = new Vector3D(-7, -10, -12),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // ---------------------------------------------
        // setup lighting
        AmbientLightColor = Color.FromArgb(1, 12, 12, 12);
        DirectionalLightColor = Colors.White;
        DirectionalLightDirection1 = new Vector3D(-0, -20, -20);
        DirectionalLightDirection2 = new Vector3D(-0, -1, +50);
        DirectionalLightDirection3 = new Vector3D(0, +1, 0);

        // ---------------------------------------------
        // model trafo
        DefaultTransform = new Media3D.TranslateTransform3D(0, -0, 0);

        // ---------------------------------------------
        // model material
        DefaultMaterial = new PhongMaterial {
            AmbientColor = Colors.Gray.ToColor4(),
            DiffuseColor = Colors.Red.ToColor4(), // Colors.LightGray,
            SpecularColor = Colors.White.ToColor4(),
            SpecularShininess = 100f,
            DiffuseMap =
                TextureModel.Create(
                    new System.Uri(@"./Media/TextureCheckerboard2.dds", System.UriKind.RelativeOrAbsolute).ToString()),
            NormalMap = TextureModel.Create(
                new System.Uri(@"./Media/TextureCheckerboard2_dot3.dds", System.UriKind.RelativeOrAbsolute).ToString()),
            EnableTessellation = true,
            RenderShadowMap = true
        };
        FloorMaterial.RenderShadowMap = true;
        // ---------------------------------------------
        // init model
        DefaultModel = LoadModel(@"./Media/teapot_quads_tex.obj",
            meshTopology == MeshTopologyEnum.PnTriangles
                ? MeshFaces.Default
                : MeshFaces.QuadPatches);
        // ---------------------------------------------
        // floor plane grid
        Grid = LineBuilder.GenerateGrid(10);
        GridColor = Colors.Black;
        GridTransform = new Media3D.TranslateTransform3D(-5, -4, -5);

        var builder = new MeshBuilder(true, true, true);
        builder.AddBox(new Vector3(0, -5, 0), 60, 0.5, 60, BoxFaces.All);
        FloorModel = builder.ToMesh();

        Instances = [
            Matrix.Identity, Translation(10, 0, 10), Translation(-10, 0, 10), Translation(10, 0, -10),
            Translation(-10, 0, -10),
        ];
    }

    /// <summary>
    /// load the model from obj-file
    /// </summary>
    /// <param name="filename">filename</param>
    /// <param name="faces">Determines if facades should be treated as triangles (Default) or as quads (Quads)</param>
    [System.Obsolete]
    private MeshGeometry3D LoadModel(string filename, MeshFaces faces) {
        // load model
        var reader = new ObjReader();
        var objModel = reader.Read(filename, new ModelInfo() {
            Faces = faces
        });
        var model = objModel.Select(x => x.Geometry)
                        .OfType<MeshGeometry3D>()
                        .FirstOrDefault()
                    ?? throw new InvalidOperationException("The model did not contain mesh geometry.");
        var positions = model.Positions
                        ?? throw new InvalidOperationException("The model did not contain positions.");
        model.Colors = [.. positions.Select(_ => new Color4(1, 0, 0, 1))];
        return model;
    }

    private static Matrix Translation(float x, float y, float z) {
        var matrix = Matrix.Identity;
        matrix.M41 = x;
        matrix.M42 = y;
        matrix.M43 = z;
        return matrix;
    }
}