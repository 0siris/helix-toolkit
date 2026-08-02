// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MeshSimplification;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public string Name { get; set; }

    public MainViewModel ViewModel {
        get { return this; }
    }

    public MeshGeometry3D Model {
        get { return field; }
        private set {
            if (SetValue(ref field, value)) {
                NumberOfTriangles = field.Indices.Count / 3;
                NumberOfVertices = field.Positions.Count;
            }
        }
    }

    public PhongMaterial ModelMaterial { get; set; }
    public PhongMaterial LightModelMaterial { get; set; }

    public Transform3D ModelTransform { private set; get; }

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
        get { return field; }
    } = new Vector3D(-100, -100, -100);

    public ICommand SimplifyCommand { private set; get; }
    public ICommand ResetCommand { private set; get; }

    private HelixToolkit.SharpDX.Core.MeshSimplification simHelper;

    public bool Busy { set; get; } = false;

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                FillMode = value ? FillMode.Wireframe : FillMode.Solid;
            }
        }
        get { return field; }
    } = true;

    public FillMode FillMode { set; get; } = FillMode.Wireframe;

    public int NumberOfTriangles { set; get; } = 0;
    public int NumberOfVertices { set; get; } = 0;

    private MeshGeometry3D OrgMesh;

    public bool Lossless { set; get; } = false;

    public long CalculationTime { set; get; } = 0;

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        // ----------------------------------------------
        // titles
        this.Title = "Mesh Simplification Demo";
        this.SubTitle = "WPF & SharpDX";

        // ----------------------------------------------
        // camera setup
        this.Camera = new PerspectiveCamera {
            Position = new Point3D(100, 100, 100), LookDirection = new Vector3D(-100, -100, -100),
            UpDirection = new Vector3D(0, 1, 0)
        };
        // ----------------------------------------------
        // setup scene
        this.AmbientLightColor = Colors.DimGray;
        this.Light1Color = Colors.Gray;


        this.Light1Direction = new Vector3D(-100, -100, -100);
        SetupCameraBindings(Camera);
        // ----------------------------------------------
        // ----------------------------------------------
        // scene model3d
        this.ModelMaterial = PhongMaterials.Silver;

        var models = Load3ds("wall12.obj").Select(x => x.Geometry as MeshGeometry3D).ToArray();
        //var scale = new Vector3(1f);

        //foreach (var item in caritems)
        //{
        //    for (int i = 0; i < item.Positions.Count; ++i)
        //    {
        //        item.Positions[i] = item.Positions[i] * scale;
        //    }

        //}
        Model = models[0];
        OrgMesh = Model;

        //ModelTransform = new Media3D.RotateTransform3D() { Rotation = new Media3D.AxisAngleRotation3D(new Vector3D(1, 0, 0), -90) };

        SimplifyCommand = new RelayCommand(Simplify, CanSimplify);
        ResetCommand = new RelayCommand((o) => {
            Model = OrgMesh;
            simHelper = new HelixToolkit.SharpDX.Core.MeshSimplification(Model);
        },
                                        CanSimplify);
        simHelper = new HelixToolkit.SharpDX.Core.MeshSimplification(Model);
    }

    [Obsolete]
    public List<Object3D> Load3ds(string path) {
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

    private bool CanSimplify(object obj) {
        return !Busy;
    }

    private void Simplify(object obj) {
        if (!CanSimplify(null)) {
            return;
        }

        Busy = true;
        int size = Model.Indices.Count / 3 / 2;
        CalculationTime = 0;
        Task.Factory.StartNew(() => {
            var sw = Stopwatch.StartNew();
            var model = simHelper.Simplify(size, 7, true, Lossless);
            sw.Stop();
            CalculationTime = sw.ElapsedMilliseconds;
            model.Normals = model.CalculateNormals();
            return model;
        }).ContinueWith(x => {
            Busy = false;
            Model = x.Result;
            CommandManager.InvalidateRequerySuggested();
        },
                        TaskScheduler.FromCurrentSynchronizationContext());
    }
}
