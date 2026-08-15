// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace XRayDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using DemoCore;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public string Name { get; set; } = string.Empty;

    public MainViewModel ViewModel => this;

    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D Floor { get; private set; }

    public MeshGeometry3D? CarModel { private set; get; }

    public PhongMaterial ModelMaterial { get; set; }
    public PhongMaterial FloorMaterial { get; set; }
    public PhongMaterial? LightModelMaterial { get; set; }

    public Transform3D ModelTransform { private set; get; }

    public Transform3D ScreenSpacedScale { private set; get; } = new Media3D.ScaleTransform3D(0.1, 0.1, 0.1);

    public Vector3D Light1Direction { get; set; }
    public Color Light1Color { get; set; }
    public Color AmbientLightColor { get; set; }

    public Vector3D CamLookDir {
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
                Light1Direction = value;
            }
        }
        get;
    } = new(-100, -100, -100);

    public Matrix[] Instances { private set; get; }
    public Matrix[] OutlineInstances { private set; get; }
    public BlendStateDescription BlendDescription { set; get; }

    public DepthStencilStateDescription DepthStencilDescription { set; get; }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        // ----------------------------------------------
        // titles
        Title = "Lighting Demo";
        SubTitle = "WPF & SharpDX";

        // ----------------------------------------------
        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(100, 100, 100),
            LookDirection = new Vector3D(-100, -100, -100),
            UpDirection = new Vector3D(0, 1, 0)
        };
        // ----------------------------------------------
        // setup scene
        AmbientLightColor = Colors.DimGray;
        Light1Color = Colors.LightGray;


        Light1Direction = new Vector3D(-100, -100, -100);
        SetupCameraBindings(Camera);
        // ----------------------------------------------
        // ----------------------------------------------
        // scene model3d
        ModelMaterial = PhongMaterials.Silver;

        // ----------------------------------------------
        // floor model3d
        var b2 = new MeshBuilder(true, true, true);
        b2.AddBox(new Vector3(0.0f, 0, 0.0f), 150, 1, 150, BoxFaces.All);
        b2.AddBox(new Vector3(0, 25, 70), 150, 50, 20);
        b2.AddBox(new Vector3(0, 25, -70), 150, 50, 20);
        Floor = b2.ToMeshGeometry3D();
        FloorMaterial = PhongMaterials.Bisque;
        FloorMaterial.DiffuseMap =
            TextureModel.Create(
                new Uri(@"TextureCheckerboard2.jpg", UriKind.RelativeOrAbsolute).ToString());
        FloorMaterial.NormalMap =
            TextureModel.Create(new Uri(@"TextureCheckerboard2_dot3.jpg", UriKind.RelativeOrAbsolute)
                .ToString());

        var caritems = Load3Ds("leone.3DBuilder.obj")
            .Select(x => x.Geometry)
            .OfType<MeshGeometry3D>()
            .ToArray();
        if (caritems.Length == 0) {
            throw new InvalidOperationException("The car model did not contain mesh geometry.");
        }

        var scale = new Vector3(1f, 1f, 1f);

        foreach (var item in caritems) {
            var positions = item.Positions
                            ?? throw new InvalidOperationException("The car mesh did not contain positions.");
            for (int i = 0; i < positions.Count; ++i) {
                positions[i] = positions[i] * scale;
            }
        }

        Model = MeshGeometry3D.Merge(caritems);

        ModelTransform = new Media3D.RotateTransform3D() {
            Rotation = new Media3D.AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)
        };

        Instances = new Matrix[6];
        for (int i = 0; i < Instances.Length; ++i) {
            Instances[i] = Translation(new Vector3(15 * i - 30, 15 * (i % 2) - 30, 0));
        }

        OutlineInstances = new Matrix[6];
        for (int i = 0; i < Instances.Length; ++i) {
            OutlineInstances[i] = Translation(new Vector3(15 * i - 30, 15 * (i % 2), 0));
        }

        var blendDesc = new BlendStateDescription();
        blendDesc.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            BlendOperation = BlendOperation.Add,
            AlphaBlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.One,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };
        BlendDescription = blendDesc;
        DepthStencilDescription = new DepthStencilStateDescription() {
            IsDepthEnabled = true,
            DepthComparison = Comparison.LessEqual,
            DepthWriteMask = DepthWriteMask.Zero
        };
    }

    [Obsolete]
    public List<Object3D> Load3Ds(string path) {
        var reader = new ObjReader();
        var list = reader.Read(path);
        return list;
    }

    public void SetupCameraBindings(Camera camera) {
        if (camera is ProjectionCamera) {
            SetBinding("CamLookDir", camera, ProjectionCamera.LookDirectionProperty, this);
        }
    }

    private static void SetBinding(
        string path,
        DependencyObject dobj,
        DependencyProperty property,
        object viewModel,
        BindingMode mode = BindingMode.TwoWay
    ) {
        var binding = new Binding(path) {
            Source = viewModel,
            Mode = mode
        };
        BindingOperations.SetBinding(dobj, property, binding);
    }

    private static Matrix Translation(Vector3 value) {
        var matrix = Matrix.Identity;
        matrix.M41 = value.X;
        matrix.M42 = value.Y;
        matrix.M43 = value.Z;
        return matrix;
    }
}