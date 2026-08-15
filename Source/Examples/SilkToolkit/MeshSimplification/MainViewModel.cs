// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace MeshSimplification;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DemoCore;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public string Name { get; set; } = string.Empty;

    public MainViewModel ViewModel => this;

    public MeshGeometry3D Model {
        get;
        private set {
            if (SetValue(ref field, value)) {
                var indices = field.Indices ?? throw new InvalidOperationException("Model indices are required.");
                var positions = field.Positions ?? throw new InvalidOperationException("Model positions are required.");
                NumberOfTriangles = indices.Count / 3;
                NumberOfVertices = positions.Count;
            }
        }
    }

    public PhongMaterial ModelMaterial { get; set; }
    public PhongMaterial? LightModelMaterial { get; set; }

    public Transform3D ModelTransform { private set; get; } = Transform3D.Identity;

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

    public ICommand SimplifyCommand { private set; get; }
    public ICommand ResetCommand { private set; get; }

    private HelixToolkit.SharpDX.Core.Geometry.MeshSimplification simHelper;

    public bool Busy { set; get; }

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                FillMode = value
                    ? FillMode.Wireframe
                    : FillMode.Solid;
            }
        }
        get;
    } = true;

    public FillMode FillMode { set; get; } = FillMode.Wireframe;

    public int NumberOfTriangles { set; get; }
    public int NumberOfVertices { set; get; }

    private readonly MeshGeometry3D orgMesh;

    public bool Lossless { set; get; } = false;

    public long CalculationTime { set; get; }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        // ----------------------------------------------
        // titles
        Title = "Mesh Simplification Demo";
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
        Light1Color = Colors.Gray;


        Light1Direction = new Vector3D(-100, -100, -100);
        SetupCameraBindings(Camera);
        // ----------------------------------------------
        // ----------------------------------------------
        // scene model3d
        ModelMaterial = PhongMaterials.Silver;

        var model = Load3Ds("wall12.obj")
                        .Select(x => x.Geometry)
                        .OfType<MeshGeometry3D>()
                        .FirstOrDefault()
                    ?? throw new InvalidOperationException("The sample mesh could not be loaded.");
        //var scale = new Vector3(1f);

        //foreach (var item in caritems)
        //{
        //    for (int i = 0; i < item.Positions.Count; ++i)
        //    {
        //        item.Positions[i] = item.Positions[i] * scale;
        //    }

        //}
        Model = model;
        orgMesh = Model;

        //ModelTransform = new Media3D.RotateTransform3D() { Rotation = new Media3D.AxisAngleRotation3D(new Vector3D(1, 0, 0), -90) };

        SimplifyCommand = new RelayCommand(Simplify, CanSimplify);
        ResetCommand = new RelayCommand((_) => {
                Model = orgMesh;
                simHelper = new HelixToolkit.SharpDX.Core.Geometry.MeshSimplification(Model);
            },
            CanSimplify);
        simHelper = new HelixToolkit.SharpDX.Core.Geometry.MeshSimplification(Model);
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

    private bool CanSimplify(object? obj) => !Busy;

    private void Simplify(object? obj) {
        if (!CanSimplify(null)) {
            return;
        }

        Busy = true;
        var indices = Model.Indices ?? throw new InvalidOperationException("Model indices are required.");
        var size = indices.Count / 3 / 2;
        CalculationTime = 0;
        Task.Factory.StartNew(() => {
                var sw = Stopwatch.StartNew();
                var model = simHelper.Simplify(size, 7, true, Lossless);
                sw.Stop();
                CalculationTime = sw.ElapsedMilliseconds;
                model.Normals = model.CalculateNormals();
                return model;
            })
            .ContinueWith(x => {
                    Busy = false;
                    Model = x.Result;
                    CommandManager.InvalidateRequerySuggested();
                },
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}