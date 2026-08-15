// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Microsoft.Win32;

namespace CoreWpfTest;

using ObservableObject = GalaSoft.MvvmLight.ObservableObject;

public class MainViewModel : ObservableObject {
    private string openFileFilter = $"{Importer.SupportedFormatsString}";
    private string exportFileFilter = $"{HelixToolkit.SharpDX.Core.Assimp.Exporter.SupportedFormatsString}";

    public bool ShowWireframe {
        set {
            if (Set(ref field, value)) {
                ShowWireframeFunct(value);
            }
        }
        get;
    } = false;

    public bool RenderFlat {
        set {
            if (Set(ref field, value)) {
                RenderFlatFunct(value);
            }
        }
        get;
    } = false;

    public bool RenderEnvironmentMap {
        set {
            if (Set(ref field, value) && scene?.Root is { } root) {
                foreach (var node in root.Traverse()) {
                    if (node is MaterialGeometryNode m && m.Material is PbrMaterialCore material) {
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

    private bool isLoading = false;

    public bool IsLoading {
        private set => Set(ref isLoading, value);
        get => isLoading;
    }

    private bool enableAnimation = false;

    public bool EnableAnimation {
        set {
            if (Set(ref enableAnimation, value)) {
                if (value) {
                    StartAnimation();
                } else {
                    StopAnimation();
                }
            }
        }
        get => enableAnimation;
    }

    public ObservableCollection<Animation> Animations { get; } = [];

    public SceneNodeGroupModel3D GroupModel { get; } = new();

    public Animation? SelectedAnimation {
        set {
            if (Set(ref field, value)) {
                StopAnimation();
                if (value != null) {
                    animationUpdater = new NodeAnimationUpdater(value);
                } else {
                    animationUpdater = null;
                }

                if (enableAnimation) {
                    StartAnimation();
                }
            }
        }
        get;
    } = null;

    public TextureModel? EnvironmentMap { get; }
    public EffectsManager EffectsManager { get; }
    public Camera Camera { get; }

    private HelixToolkitScene? scene;
    private NodeAnimationUpdater? animationUpdater;
    private List<BoneSkinMeshNode> boneSkinNodes = [];
    private List<BoneSkinMeshNode> skeletonNodes = [];
    private CompositionTargetEx compositeHelper = new();


    public MainViewModel() {
        OpenFileCommand = new DelegateCommand(OpenFile);
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera() {
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
            Position = new System.Windows.Media.Media3D.Point3D(0, 10, 10),
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
        EnvironmentMap = LoadFileToMemory("Cubemap_Grandcanyon.dds");
    }

    private void OpenFile() {
        if (isLoading) {
            return;
        }

        string? path = OpenFileDialog(openFileFilter);
        if (path is null) {
            return;
        }

        StopAnimation();

        IsLoading = true;
        Task.Run(() => {
                var loader = new Importer();
                return loader.Load(path)
                       ?? throw new InvalidOperationException("The selected file did not contain a scene.");
            })
            .ContinueWith((result) => {
                    IsLoading = false;
                    if (result.Status == TaskStatus.RanToCompletion && result.Result is { } loadedScene) {
                        scene = loadedScene;
                        Animations.Clear();
                        GroupModel.Clear();
                        foreach (var node in loadedScene.Root.Traverse()) {
                            if (node is MaterialGeometryNode m) {
                                if (m.Material is PbrMaterialCore pbr) {
                                    pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                } else if (m.Material is PhongMaterialCore phong) {
                                    phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                }
                            }
                        }

                        GroupModel.AddNode(loadedScene.Root);
                        if (loadedScene.HasAnimation) {
                            foreach (var ani in loadedScene.Animations) {
                                Animations.Add(ani);
                            }
                        }

                        foreach (var n in loadedScene.Root.Traverse()) {
                            n.Tag = new AttachedNodeViewModel(n);
                        }
                    } else if (result.IsFaulted && result.Exception is { } exception) {
                        MessageBox.Show(exception.Message);
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void StartAnimation() {
        compositeHelper.Rendering += CompositeHelper_Rendering;
    }

    public void StopAnimation() {
        compositeHelper.Rendering -= CompositeHelper_Rendering;
    }

    private void CompositeHelper_Rendering(object? sender, System.Windows.Media.RenderingEventArgs e) {
        animationUpdater?.Update(Stopwatch.GetTimestamp(), Stopwatch.Frequency);
    }

    private void ExportFile() {
        var index = SaveFileDialog(exportFileFilter, out var path);
        if (!string.IsNullOrEmpty(path) && index >= 0) {
            var id = HelixToolkit.SharpDX.Core.Assimp.Exporter.SupportedFormats[index].FormatId;
            var exporter = new HelixToolkit.SharpDX.Core.Assimp.Exporter();
            if (scene is { } loadedScene)
                exporter.ExportToFile(path, loadedScene, id);
            return;
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

    public static MemoryStream LoadFileToMemory(string filePath) {
        using var file = new FileStream(filePath, FileMode.Open);
        var memory = new MemoryStream();
        file.CopyTo(memory);
        return memory;
    }
}