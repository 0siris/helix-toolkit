// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace ManipulatorDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D Model2 { private set; get; }
    public LineGeometry3D Lines { get; private set; }
    public LineGeometry3D Grid { get; private set; }

    public PhongMaterial Material1 { get; private set; }
    public PhongMaterial Material2 { get; private set; }
    public PhongMaterial Material3 { get; private set; }
    public Color GridColor { get; private set; }

    public Transform3D Model1Transform { get; set; }
    public Transform3D Model2Transform { get; set; }
    public Transform3D Model3Transform { get; set; }
    public Transform3D GridTransform { get; set; }

    public Vector3D DirectionalLightDirection { get; private set; }
    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }

    public Element3D? Target { set; get; }
    public Vector3 CenterOffset { set; get; }

    public ICommand ResetTransformsCommand { private set; get; }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        Title = "Manipulator Demo";
        SubTitle = string.Empty;

        // camera setup
        Camera = new OrthographicCamera {
            Position = new Point3D(0, 0, 5),
            LookDirection = new Vector3D(0, 0, -5),
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
        b1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 1.5, BoxFaces.All);
        Model = b1.ToMeshGeometry3D();
        var m1 = Load3Ds("suzanne.3ds");
        Model2 = m1.Select(x => x.Geometry)
                     .OfType<MeshGeometry3D>()
                     .FirstOrDefault()
                 ?? throw new InvalidOperationException("The manipulator sample mesh could not be loaded.");
        //Manully set an offset for test
        if (Model2.Positions is not { } model2Positions)
            throw new InvalidOperationException("The manipulator sample positions are required.");
        for (int i = 0; i < model2Positions.Count; ++i) {
            model2Positions[i] = model2Positions[i] + new Vector3(2, 3, 4);
        }

        Model2.UpdateBounds();

        // lines model3d
        var e1 = new LineBuilder();
        e1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 1.5);
        Lines = e1.ToLineGeometry3D();

        // model trafos
        Model1Transform = new TranslateTransform3D(0, 0, 0);
        Model2Transform = new TranslateTransform3D(-3, 0, 0);
        Model3Transform = new TranslateTransform3D(+3, 0, 0);

        // model materials
        Material1 = PhongMaterials.Orange;
        Material2 = PhongMaterials.Orange;
        Material3 = PhongMaterials.Red;

        var dr = Colors.DarkRed;
        Console.WriteLine(dr);
        ResetTransformsCommand = new RelayCommand((_) => {
            Model1Transform = new TranslateTransform3D(0, 0, 0);
            Model2Transform = new TranslateTransform3D(-3, 0, 0);
            Model3Transform = new TranslateTransform3D(+3, 0, 0);
        });
    }

    public void OnMouseDown3DHandler(object sender, MouseDown3DEventArgs e) {
        if (e.HitTestResult != null && e.HitTestResult.ModelHit is MeshGeometryModel3D m &&
            (m.Geometry == Model || m.Geometry == Model2)) {
            if (m.Geometry is not { } geometry)
                return;
            Target = null;
            CenterOffset = BoundingBoxExtensions.Center(geometry.Bound); // Must update this before updating target
            Target = m;
        }
    }

    [Obsolete]
    public List<Object3D> Load3Ds(string path) {
        if (path.EndsWith(".obj", StringComparison.CurrentCultureIgnoreCase)) {
            var reader = new ObjReader();
            var list = reader.Read(path);
            return list;
        } else if (path.EndsWith(".3ds", StringComparison.CurrentCultureIgnoreCase)) {
            var reader = new StudioReader();
            var list = reader.Read(path);
            return list;
        } else {
            return [];
        }
    }
}