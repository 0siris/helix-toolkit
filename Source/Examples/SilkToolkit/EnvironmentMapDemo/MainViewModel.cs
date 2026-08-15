// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace EnvironmentMapDemo;

using System.Collections.Generic;
using System;
using System.Linq;
using DemoCore;
using Color = Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D Model1 { get; private set; }
    public PhongMaterial ModelMaterial { get; set; }
    public Media3D.Transform3D ModelTransform { get; private set; }
    public Vector3 DirectionalLightDirection { get; private set; }
    public Color4 DirectionalLightColor { get; private set; }
    public Color4 AmbientLightColor { get; private set; }

    public List<Matrix> Instances1 { private set; get; } = [];
    public List<Matrix> Instances2 { private set; get; } = [];
    public List<Matrix> Instances3 { private set; get; } = [];
    public PhongMaterial ModelMaterial1 { get; set; }
    public PhongMaterial ModelMaterial2 { get; set; }
    public PhongMaterial ModelMaterial3 { get; set; }
    public TextureModel SkyboxTexture { private set; get; }

    public MainViewModel() {
        Title = "Environment Mapping Demo";
        SubTitle = "HelixToolkitDX";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(10, 0, 0),
            LookDirection = new Vector3D(-10, 0, 0),
            UpDirection = new Vector3D(0, 1, 0)
        };
        //this.Camera = new OrthographicCamera { Position = new Point3D(3, 3, 5), LookDirection = new Vector3D(-3, -3, -5), UpDirection = new Vector3D(0, 1, 0) };

        // lighting setup
        AmbientLightColor = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
        DirectionalLightColor = Color.White;
        DirectionalLightDirection = new Vector3(-2, -1, 1);

        // scene model3d
        Model = LoadModel("teapot_quads_tex.obj", MeshFaces.Default);
        ModelTransform = new Media3D.TranslateTransform3D();
        ModelMaterial = PhongMaterials.PolishedSilver;
        ModelMaterial.ReflectiveColor = Color.Silver;
        ModelMaterial.RenderEnvironmentMap = true;
        var b1 = new MeshBuilder(true);
        b1.AddSphere(new Vector3(0, 0, 0), 1.0, 64, 64);
        b1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 3, BoxFaces.All);
        Model1 = b1.ToMeshGeometry3D();

        EffectsManager = new DefaultEffectsManager();

        SkyboxTexture = TextureModel.Create("Cubemap_Grandcanyon.dds")
                        ?? throw new InvalidOperationException("The skybox texture could not be loaded.");
        int t = 5;
        for (int i = 0; i < 10; ++i) {
            Instances1.Add(Translation(t, t, (i - 5) * t));
        }

        for (int i = 0; i < 10; ++i) {
            Instances2.Add(Translation(t, (i - 5) * t, t));
        }

        for (int i = 0; i < 10; ++i) {
            Instances3.Add(Translation(-(i - 5) * t, t, (i - 5) * t));
        }

        //int t = 5;
        //Instances.Add(Matrix.Translation(new Vector3(t, t, t)));
        //Instances.Add(Matrix.Translation(new Vector3(-t, t, t)));
        //Instances.Add(Matrix.Translation(new Vector3(-t, -t, t)));
        //Instances.Add(Matrix.Translation(new Vector3(-t, -t, -t)));
        //Instances.Add(Matrix.Translation(new Vector3(t, -t, t)));
        //Instances.Add(Matrix.Translation(new Vector3(t, -t, -t)));
        //Instances.Add(Matrix.Translation(new Vector3(-t, t, -t)));
        //Instances.Add(Matrix.Translation(new Vector3(t, t, -t)));
        ModelMaterial1 = PhongMaterials.Red;
        ModelMaterial1.AmbientColor = Color.Red;
        ModelMaterial1.RenderEnvironmentMap = true;
        ModelMaterial2 = PhongMaterials.Green;
        ModelMaterial2.AmbientColor = Color.Green;
        ModelMaterial2.RenderEnvironmentMap = true;
        ModelMaterial3 = PhongMaterials.Blue;
        ModelMaterial3.AmbientColor = Color.Blue;
        ModelMaterial3.RenderEnvironmentMap = true;
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
        return objModel.Select(x => x.Geometry)
                   .OfType<MeshGeometry3D>()
                   .FirstOrDefault()
               ?? throw new InvalidOperationException("The model did not contain mesh geometry.");
    }

    private static Matrix Translation(float x, float y, float z) {
        var m = System.Numerics.Matrix4x4.CreateTranslation(x, y, z);
        return new Matrix(m.M11,
            m.M12,
            m.M13,
            m.M14,
            m.M21,
            m.M22,
            m.M23,
            m.M24,
            m.M31,
            m.M32,
            m.M33,
            m.M34,
            m.M41,
            m.M42,
            m.M43,
            m.M44);
    }
}