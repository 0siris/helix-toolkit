// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using AnimationExtensions = HelixToolkit.SharpDX.Core.Extensions.AnimationExtensions;
using SceneNodeExtensions = HelixToolkit.SharpDX.Core.Extensions.SceneNodeExtensions;

namespace FileLoadDemo;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DemoCore;
using DemoCore.Automation;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Controls;
using Microsoft.Win32;
using BoundingBox = BoundingBox;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

public class MainViewModel : BaseViewModel {
    private string openFileFilter = $"{Importer.SupportedFormatsString}";
    private string exportFileFilter = $"{Exporter.SupportedFormatsString}";

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                ShowWireframeFunct(value);
            }
        }
        get;
    } = false;

    public bool RenderFlat {
        set {
            if (SetValue(ref field, value)) {
                RenderFlatFunct(value);
            }
        }
        get;
    } = false;

    public bool RenderEnvironmentMap {
        set {
            if (SetValue(ref field, value) && scene?.Root is { } root) {
                foreach (var node in root.Traverse()) {
                    if (node is MaterialGeometryNode {Material: PbrMaterialCore material}) {
                        material.RenderEnvironmentMap = value;
                    }
                }
            }
        }
        get;
    } = true;

    public ICommand OpenFileCommand { get; set; }

    public ICommand ResetCameraCommand { set; get; }

    public ICommand ExportCommand { private set; get; }

    public ICommand CopyAsBitmapCommand { private set; get; }

    public ICommand CopyAsHiresBitmapCommand { private set; get; }

    private bool isLoading;

    public bool IsLoading {
        private set => SetValue(ref isLoading, value);
        get => isLoading;
    }

    public bool IsPlaying {
        private set => SetValue(ref field, value);
        get;
    } = false;

    public float StartTime {
        private set => SetValue(ref field, value);
        get;
    }

    public float EndTime {
        private set => SetValue(ref field, value);
        get;
    }

    public float CurrAnimationTime {
        set {
            if (EndTime == 0) {
                return;
            }

            if (SetValue(ref field, value % EndTime + StartTime)) {
                animationUpdater?.Update(value, 1);
            }
        }
        get;
    } = 0;

    public ObservableCollection<IAnimationUpdater> Animations { get; } = [];
    public SceneNodeGroupModel3D GroupModel { get; } = new();

    /// <summary>
    /// Gets the attached demo automation host.
    /// </summary>
    public DemoAutomationHost? AutomationHost => automationHost;

    private DemoAutomationHost? automationHost;

    public IAnimationUpdater? SelectedAnimation {
        set {
            if (SetValue(ref field, value)) {
                StopAnimation();
                CurrAnimationTime = 0;
                if (value != null) {
                    animationUpdater = value;
                    animationUpdater.Reset();
                    animationUpdater.RepeatMode = AnimationRepeatMode.Loop;
                    StartTime = value.StartTime;
                    EndTime = value.EndTime;
                } else {
                    animationUpdater = null;
                    StartTime = EndTime = 0;
                }
            }
        }
        get;
    } = null;

    private float speed = 1.0f;

    public float Speed {
        set => SetValue(ref speed, value);
        get => speed;
    }

    public Point3D ModelCentroid {
        private set => SetValue(ref field, value);
        get;
    } = default;

    private BoundingBox modelBound;

    public BoundingBox ModelBound {
        private set => SetValue(ref modelBound, value);
        get => modelBound;
    }

    public TextureModel? EnvironmentMap { get; }

    public ICommand PlayCommand { get; }

    private HelixToolkitScene? scene;
    private IAnimationUpdater? animationUpdater;
    private readonly CompositionTargetEx compositeHelper = new();
    private long initTimeStamp;

    public MainViewModel(MainWindow window) {
        OpenFileCommand = new DelegateCommand(OpenFile);
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera() {
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
            Position = new Point3D(0, 10, 10),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            FarPlaneDistance = 5000,
            NearPlaneDistance = 0.1f
        };
        ResetCameraCommand = new DelegateCommand(() => {
            if (Camera is OrthographicCamera camera) {
                camera.Reset();
                camera.FarPlaneDistance = 5000;
                camera.NearPlaneDistance = 0.1f;
            }
        });
        ExportCommand = new DelegateCommand(ExportFile);

        CopyAsBitmapCommand = new DelegateCommand(() => { CopyAsBitmapToClipBoard(window.View); });
        CopyAsHiresBitmapCommand = new DelegateCommand(() => { CopyAsHiResBitmapToClipBoard(window.View); });

        EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");

        PlayCommand = new DelegateCommand(() => {
            if (!IsPlaying && SelectedAnimation != null) {
                StartAnimation();
            } else {
                StopAnimation();
            }
        });
        automationHost = DemoBootstrapper.Attach(
            window.View,
            EffectsManager ?? throw new InvalidOperationException("EffectsManager is not initialized."),
            sceneHost: new ViewportSceneHost(window.View, GroupModel));
    }

    private void CopyAsBitmapToClipBoard(Viewport3DX viewport) {
        var bitmap = viewport.RenderBitmap();
        try {
            Clipboard.Clear();
            if (bitmap is { } actualBitmap)
                Clipboard.SetImage(actualBitmap);
        } catch (Exception e) {
            Debug.WriteLine(e);
        }
    }

    private void CopyAsHiResBitmapToClipBoard(Viewport3DX viewport) {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var bitmap = viewport.RenderBitmap(1920, 1080);
        try {
            Clipboard.Clear();
            if (bitmap is { } actualBitmap)
                Clipboard.SetImage(actualBitmap);
            stopwatch.Stop();
            Debug.WriteLine($"creating bitmap needs {stopwatch.ElapsedMilliseconds} ms");
        } catch (Exception e) {
            Debug.WriteLine(e);
        }
    }

    private void OpenFile() {
        if (isLoading) {
            return;
        }

        var path = OpenFileDialog(openFileFilter);
        if (path is null) {
            return;
        }

        StopAnimation();
        var syncContext = SynchronizationContext.Current
                          ?? throw new InvalidOperationException("The file load must start on the UI thread.");
        IsLoading = true;
        Task.Run(() => {
                var loader = new Importer();
                var loadedScene = loader.Load(path)
                                  ?? throw new InvalidOperationException("The selected file did not contain a scene.");
                loadedScene.Root.Attach(EffectsManager); // Pre attach scene graph
                SceneNodeExtensions.UpdateAllTransformMatrix(loadedScene.Root);
                if (SceneNodeExtensions.TryGetBound(loadedScene.Root, out var bound)) {
                    // Must use UI thread to set value back.
                    syncContext.Post((_) => { ModelBound = bound; }, null);
                }

                if (SceneNodeExtensions.TryGetCentroid(loadedScene.Root, out var centroid)) {
                    // Must use UI thread to set value back.
                    syncContext.Post((_) => { ModelCentroid = centroid.ToPoint3D(); }, null);
                }

                return loadedScene;
            })
            .ContinueWith((result) => {
                    IsLoading = false;
                    if (result is {Status: TaskStatus.RanToCompletion, Result: { } loadedScene}) {
                        scene = loadedScene;
                        Animations.Clear();
                        var oldNode = GroupModel.SceneNode.Items.ToArray();
                        GroupModel.Clear(false);
                        Task.Run(() => {
                            foreach (var node in oldNode) {
                                node.Dispose();
                            }
                        });
                        foreach (var node in loadedScene.Root.Traverse()) {
                            if (node is MaterialGeometryNode m) {
                                //m.Geometry.SetAsTransient();
                                if (m.Material is PbrMaterialCore pbr) {
                                    pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                } else if (m.Material is PhongMaterialCore phong) {
                                    phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                }
                            }
                        }

                        GroupModel.AddNode(loadedScene.Root);
                        if (loadedScene.HasAnimation) {
                            var dict = AnimationExtensions.CreateAnimationUpdaters(loadedScene.Animations);
                            foreach (var ani in dict.Values) {
                                Animations.Add(ani);
                            }
                        }

                        foreach (var n in loadedScene.Root.Traverse()) {
                            n.Tag = new AttachedNodeViewModel(n);
                        }

                        FocusCameraToScene();
                    } else if (result is {IsFaulted: true, Exception: { } exception}) {
                        MessageBox.Show(exception.Message);
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void StartAnimation() {
        initTimeStamp = Stopwatch.GetTimestamp();
        compositeHelper.Rendering += CompositeHelper_Rendering;
        IsPlaying = true;
    }

    public void StopAnimation() {
        IsPlaying = false;
        compositeHelper.Rendering -= CompositeHelper_Rendering;
    }

    private void CompositeHelper_Rendering(object? sender, System.Windows.Media.RenderingEventArgs e) {
        if (animationUpdater != null) {
            var elapsed = (Stopwatch.GetTimestamp() - initTimeStamp) * speed;
            CurrAnimationTime = elapsed / Stopwatch.Frequency;
        }
    }

    private void FocusCameraToScene() {
        var maxWidth = Math.Max(Math.Max(modelBound.Size.X, modelBound.Size.Y), modelBound.Size.Z);
        var center = (modelBound.Minimum + modelBound.Maximum) / 2;
        var pos = center + new Vector3(0, 0, maxWidth);
        Camera.Position = pos.ToPoint3D();
        Camera.LookDirection = (center - pos).ToVector3D();
        Camera.UpDirection = Vector3.UnitY.ToVector3D();
        if (Camera is OrthographicCamera orthCam) {
            orthCam.Width = maxWidth;
        }
    }

    private void ExportFile() {
        var index = SaveFileDialog(exportFileFilter, out var path);
        if (!string.IsNullOrEmpty(path) && index >= 0) {
            var id = Exporter.SupportedFormats[index].FormatId;
            var exporter = new Exporter();
            if (scene is { } loadedScene)
                exporter.ExportToFile(path, loadedScene, id);
        }
    }


    private string? OpenFileDialog(string filter) {
        var d = new OpenFileDialog();
        d.CustomPlaces.Clear();

        d.Filter = filter;

        if (d.ShowDialog() != true) {
            return null;
        }

        return d.FileName;
    }

    private int SaveFileDialog(string filter, out string path) {
        var d = new SaveFileDialog {
            Filter = filter
        };
        if (d.ShowDialog() == true) {
            path = d.FileName;
            return d.FilterIndex - 1; //This is tarting from 1. So must minus 1
        } else {
            path = "";
            return -1;
        }
    }

    private void ShowWireframeFunct(bool show) {
        foreach (var node in GroupModel.GroupNode.Items.PreorderDft((node) => { return node.IsRenderable; })) {
            if (node is MeshNode m) {
                m.RenderWireframe = show;
            }
        }
    }

    private void RenderFlatFunct(bool show) {
        foreach (var node in GroupModel.GroupNode.Items.PreorderDft((node) => { return node.IsRenderable; })) {
            if (node is MeshNode m) {
                if (m.Material is PhongMaterialCore phong) {
                    phong.EnableFlatShading = show;
                } else if (m.Material is PbrMaterialCore pbr) {
                    pbr.EnableFlatShading = show;
                }
            }
        }
    }
}