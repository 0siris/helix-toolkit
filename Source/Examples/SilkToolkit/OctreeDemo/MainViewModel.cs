using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DemoCore;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using HelixToolkit.Wpf.SharpDX.Model.Materials;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace OctreeDemo;

public class BindingProxy : Freezable {
    #region Overrides of Freezable

    protected override Freezable CreateInstanceCore() => new BindingProxy();

    #endregion

    public object Data {
        get => (object) GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
}

public class MainViewModel : BaseViewModel {
    public Vector3D Light1Direction {
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
            }
        }
        get;
    } = new();

    public FillMode FillMode {
        set {
            field = value;
            OnPropertyChanged();
        }
        get;
    } = FillMode.Solid;

    public bool ShowWireframe {
        set {
            field = value;
            OnPropertyChanged();
            if (field) {
                FillMode = FillMode.Wireframe;
            } else {
                FillMode = FillMode.Solid;
            }
        }
        get;
    } = false;

    public bool Visibility {
        set {
            field = value;
            OnPropertyChanged();
        }
        get;
    } = true;

    public Color Light1Color { get; set; }

    //public MeshGeometry3D Other { get; private set; }
    public Color AmbientLightColor { get; set; }

    public Color PointColor => Colors.Green;

    public Color PointHitColor => Colors.Red;

    public Color LineColor { set; get; }

    public PhongMaterial Material {
        private set => SetValue<PhongMaterial>(ref field, value);
        get;
    }

    public MeshGeometry3D DefaultModel { private set; get; }
    public PointGeometry3D PointsModel { private set; get; }

    public PointGeometry3D PointsHitModel {
        set => SetValue(ref field, value);
        get;
    }

    public LineGeometry3D LinesModel { private set; get; }
    public ObservableCollection<DataModel> Items { set; get; }
    public List<DataModel> LanderItems { private set; get; } = [];

    public Vector3D CamLookDir {
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
                Light1Direction = value;
            }
        }
        get;
    } = new(-10, -10, -10);

    public bool HitThrough { set; get; }

    private readonly IList<DataModel> highlightItems = [];

    public int SphereSize {
        set {
            if (SetValue<int>(ref field, value)) {
                if (highlightItems.Count > 0) {
                    foreach (SphereModel item in highlightItems) {
                        item.Radius = value;
                    }
                }
            }
        }
        get;
    } = 1;

    public bool AutoDeleteEmptyNode {
        set {
            field = value;
            OnPropertyChanged();
        }
        get;
    } = true;

    public bool OctreeFrameVisible {
        set {
            field = value;
            OnPropertyChanged();
        }
        get;
    } = false;

    public ICommand AddModelCommand { private set; get; }
    public ICommand RemoveModelCommand { private set; get; }
    public ICommand ClearModelCommand { private set; get; }
    public ICommand AutoTestCommand { private set; get; }

    public ICommand MultiViewportCommand { private set; get; }

    public MainViewModel() {
        // titles
        Title = "DynamicTexture Demo";
        SubTitle = "WPF & SharpDX";
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera {
            Position = new Point3D(30, 30, 30),
            LookDirection = new Vector3D(-30, -30, -30),
            UpDirection = new Vector3D(0, 1, 0)
        };
        Light1Color = Colors.White;
        Light1Direction = new Vector3D(-10, -10, -10);
        AmbientLightColor = Colors.DimGray;
        SetupCameraBindings(Camera);
        LineColor = Colors.Blue;
        Items = [];
        var sw = Stopwatch.StartNew();
        CreateDefaultModels();
        sw.Stop();
        Console.WriteLine("Create Models total time =" + sw.ElapsedMilliseconds + " ms");
        timer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Tick += Timer_Tick;
        AddModelCommand = new RelayCommand(AddModel);
        RemoveModelCommand = new RelayCommand(RemoveModel);
        ClearModelCommand = new RelayCommand(ClearModel);
        AutoTestCommand = new RelayCommand(AutoTestAddRemove);
        MultiViewportCommand = new RelayCommand((_) => {
            var win = new MultiviewportWin() {
                DataContext = this
            };
            win.Show();
        });
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Material), nameof(DefaultModel), nameof(PointsModel),
        nameof(PointsHitModel), nameof(LinesModel))]
    private void CreateDefaultModels() {
        Material = PhongMaterials.White;
        var b2 = new MeshBuilder(true, true, true);
        b2.AddSphere(new Vector3(15f, 0f, 0f), 4, 64, 64);
        b2.AddSphere(new Vector3(25f, 0f, 0f), 2);
        b2.AddTube([new Vector3(10f, 5f, 0f), new Vector3(10f, 7f, 0f)], 2, 12, false, true, true);
        DefaultModel = b2.ToMeshGeometry3D();
        DefaultModel.OctreeParameter.RecordHitPathBoundingBoxes = true;

        PointsModel = new PointGeometry3D();
        var offset = new Vector3(1, 1, 1);

        var defaultPositions = DefaultModel.Positions
                               ?? throw new InvalidOperationException("The default model positions are required.");
        PointsModel.Positions = [.. defaultPositions.Select(x => x + offset)];
        PointsModel.Indices = [.. Enumerable.Range(0, PointsModel.Positions.Count)];
        PointsModel.OctreeParameter.RecordHitPathBoundingBoxes = true;
        for (var i = 0; i < 50; ++i) {
            for (var j = 0; j < 10; ++j) {
                Items.Add(new SphereModel(new Vector3(i - 50, j - 25, i + j - 75), rnd.NextDouble(1, 3)));
            }
        }

        var b3 = new LineBuilder();
        for (var i = 0; i < 10; ++i) {
            for (var j = 0; j < 5; ++j) {
                for (var k = 0; k < 5; ++k) {
                    b3.AddBox(new Vector3(-10 - i * 5, j * 5, k * 5), 5, 5, 5);
                }
            }
        }

        LinesModel = b3.ToLineGeometry3D();
        LinesModel.OctreeParameter.RecordHitPathBoundingBoxes = true;
        PointsHitModel = new PointGeometry3D() {
            Positions = [],
            Indices = []
        };
        //var landerItems = Load3ds("Car.3ds").Select(x => new DataModel() { Model = x.Geometry as MeshGeometry3D, Material = PhongMaterials.Copper }).ToList();
        //var scale = new Vector3(0.007f);
        //var offset = new Vector3(15, 15, 15);
        //foreach (var item in landerItems)
        //{
        //    for (int i = 0; i < item.Model.Positions.Count; ++i)
        //    {
        //        item.Model.Positions[i] = item.Model.Positions[i] * scale + offset;
        //    }

        //    item.Model.UpdateOctree();
        //}
        //LanderItems = landerItems;
    }

    [Obsolete]
    public List<Object3D> Load3Ds(string path) {
        var reader = new StudioReader();
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

    public void OnMouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e) {
        foreach (var item in highlightItems) {
            item.Highlight = false;
        }

        highlightItems.Clear();
        Material = PhongMaterials.White;
        var viewport = sender as Viewport3DX;
        if (viewport == null) {
            return;
        }

        var point = e.GetPosition(viewport);
        var hitTests = viewport.FindHits(point);
        if (hitTests.Count > 0) {
            if (HitThrough) {
                foreach (var hit in hitTests) {
                    if (hit.ModelHit is Element3D {DataContext: DataModel model}) {
                        model.Highlight = true;
                        highlightItems.Add(model);
                    } else if (hit.ModelHit is Element3D {DataContext: var dataContext}
                               && ReferenceEquals(dataContext, this)) {
                        if (hit.TriangleIndices != null) {
                            Material = PhongMaterials.Yellow;
                        } else {
                            var v = new Vector3Collection {
                                hit.PointHit
                            };
                            PointsHitModel.Positions = v;
                            var idx = new IntCollection {
                                0
                            };
                            PointsHitModel = new PointGeometry3D() {
                                Positions = v,
                                Indices = idx
                            };
                        }
                    }
                }
            } else {
                var hit = hitTests[0];
                if (hit.ModelHit is Element3D elem) {
                    if (elem.DataContext is DataModel model) {
                        model.Highlight = true;
                        highlightItems.Add(model);
                    } else if (elem.DataContext == this) {
                        if (hit.TriangleIndices != null) {
                            Material = PhongMaterials.Yellow;
                        } else {
                            var v = new Vector3Collection {
                                hit.PointHit
                            };
                            PointsHitModel.Positions = v;
                            var idx = new IntCollection {
                                0
                            };
                            PointsHitModel = new PointGeometry3D() {
                                Positions = v,
                                Indices = idx
                            };
                        }
                    }
                }
            }
        }
    }

    private double theta;
    private double newModelZ = -5;

    private void AddModel(object? o) {
        var x = 10 * (float) Math.Sin(theta);
        var y = 10 * (float) Math.Cos(theta);
        theta += 0.3;
        newModelZ += 0.5;
        var z = (float) (newModelZ);
        Items.Add(new SphereModel(new Vector3(x, y + 20, z + 14), 1));
    }

    private void RemoveModel(object? o) {
        if (Items.Count > 0) {
            Items.RemoveAt(Items.Count - 1);
            newModelZ = newModelZ > -5
                ? newModelZ - 0.5
                : 0;
        }
    }

    private void ClearModel(object? o) {
        Items.Clear();
        highlightItems.Clear();
    }

    private DispatcherTimer timer;
    private int counter;

    public bool AutoTesting {
        set {
            if (SetValue<bool>(ref field, value)) {
                Enabled = !value;
            }
        }
        get;
    } = false;

    public bool Enabled {
        set => SetValue<bool>(ref field, value);
        get;
    } = true;

    private Random rnd = new();

    private void AutoTestAddRemove(object? o) {
        if (!timer.IsEnabled) {
            AutoTesting = true;
            timer.Start();
        } else {
            timer.Stop();
            AutoTesting = false;
            counter = 0;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e) {
        if (counter > 499) {
            counter = -500;
        }

        if (counter < 0) {
            RemoveModel(null);
        } else {
            AddModel(null);
        }

        if (counter % 2 == 0) {
            var k = rnd.Next(0, Items.Count - 1);
            var radius = rnd.Next(1, 5);
            if (Items[k] is SphereModel sphere)
                sphere.Radius = radius;
        }

        ++counter;
    }

    protected override void Dispose(bool disposing) {
        timer.Stop();
        timer.Tick -= Timer_Tick;

        base.Dispose(disposing);
    }
}