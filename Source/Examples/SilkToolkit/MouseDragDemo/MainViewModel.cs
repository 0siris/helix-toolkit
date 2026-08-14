// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MouseDragDemo;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DemoCore;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D MeshGeometry { get; private set; }
    public LineGeometry3D Lines { get; private set; }
    public LineGeometry3D Grid { get; private set; }

    public PhongMaterial RedMaterial { get; private set; }
    public PhongMaterial GreenMaterial { get; private set; }
    public PhongMaterial BlueMaterial { get; private set; }
    public Color GridColor { get; private set; }

    public List<Matrix> Model1Instances { get; private set; }

    public Transform3D Model1Transform { get; private set; }
    public Transform3D Model2Transform { get; private set; }
    public Transform3D Model3Transform { get; private set; }
    public Transform3D GridTransform { get; private set; }


    public Vector3D DirectionalLightDirection { get; private set; }
    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }

    public RelayCommand AddCmd { get; set; }
    public RelayCommand DelCmd { get; set; }


    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        // titles
        Title = "Mouse Drag Demo";
        SubTitle = "WPF & SharpDX";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(0, 0, 9), LookDirection = new Vector3D(-0, -0, -9),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // setup lighting
        AmbientLightColor = Colors.DimGray;
        DirectionalLightColor = Colors.White;
        DirectionalLightDirection = new Vector3D(-2, -5, -2);

        // floor plane grid
        Grid = LineBuilder.GenerateGrid(Vector3.UnitZ, -5, 5);
        GridColor = Colors.Black;
        GridTransform = new Media3D.TranslateTransform3D(-0, -0, -0);

        // scene model3d
        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0, 0, 0), 0.65);
        b1.AddBox(new Vector3(0, 0, 0), 1, 1, 1);
        var meshGeometry = b1.ToMeshGeometry3D();
        meshGeometry.Colors = [.. meshGeometry.TextureCoordinates.Select(x => x.ToColor4())];
        MeshGeometry = meshGeometry;
        Model1Instances = [];
        for (int i = 0; i < 5; i++) {
            Model1Instances.Add(Translation(0, i, 0));
        }

        // lines model3d
        var e1 = new LineBuilder();
        e1.AddBox(new Vector3(0, 0, 0), 1, 0.5, 2);
        Lines = e1.ToLineGeometry3D();

        // model trafos
        Model1Transform = new Media3D.TranslateTransform3D(0, 0, 0.0);
        Model2Transform = new Media3D.TranslateTransform3D(-2, 0, 0);
        Model3Transform = new Media3D.TranslateTransform3D(+2, 0, 0);

        // model materials
        RedMaterial = PhongMaterials.Red;
        GreenMaterial = PhongMaterials.Green;
        BlueMaterial = PhongMaterials.Blue;

        // ---
        Shape3DCollection = new ObservableCollection<Shape3D> {
            new() {
                Geometry = MeshGeometry,
                Material = BlueMaterial,
                Transform = Model3Transform,
                Instances = [Matrix.Identity],
                DragZ = false,
            },
            new() {
                Geometry = MeshGeometry,
                Material = RedMaterial,
                Transform = Model1Transform,
                Instances = [Matrix.Identity],
                DragZ = true,
            },
        };

        Element3DCollection = new ObservableCollection<Element3D>() {
            new DraggableGeometryModel3D() {
                Geometry = MeshGeometry,
                Material = BlueMaterial,
                Transform = Model3Transform,
            },

            new DraggableGeometryModel3D() {
                Geometry = MeshGeometry,
                Material = RedMaterial,
                Transform = Model1Transform,
            },
        };

        AddCmd = new RelayCommand((o) => AddShape());
        DelCmd = new RelayCommand((o) => DelShape());
    }


    public void AddShape() {
        Element3DCollection.Add(new DraggableGeometryModel3D() {
            Geometry = MeshGeometry,
            Material = GreenMaterial,
            Transform = Model2Transform,
            Instances = [
                Translation(-1, 0, 0), Translation(+1, 0, 0),
                Translation(0, -1, 0), Translation(0, +1, 0),
                Translation(0, 0, -1), Translation(0, 0, +1),
            ],
        });

        var shape = new Shape3D() {
            Geometry = MeshGeometry,
            Material = GreenMaterial,
            Transform = Model2Transform,
        };
        Shape3DCollection.Add(shape);
    }

    public void DelShape() {
        //this.Element3DCollection = null;
        //this.Element3DCollection = new ObservableCollection<Element3D>();
        Element3DCollection.Remove((Element3D)SelectedItem);

        //this.Shape3DCollection = null;
        //this.Shape3DCollection = new ObservableCollection<Shape3D>();
        Shape3DCollection.Remove((Shape3D)SelectedItem);
    }


    public class Shape3D : BaseViewModel {
        public Geometry3D Geometry { get; set; }
        public Transform3D Transform { get; set; }
        public Material Material { get; set; }
        public IList<Matrix> Instances { get; set; }
        public bool IsSelected { get; set; }
        public bool DragZ { get; set; }
    }

    public IList<Shape3D> Shape3DCollection { get; set; }
    public IList<Element3D> Element3DCollection { get; set; }
    public object SelectedItem { get; set; }

    private static Matrix Translation(float x, float y, float z) {
        var result = Matrix.Identity;
        result.M41 = x;
        result.M42 = y;
        result.M43 = z;
        return result;
    }
}
