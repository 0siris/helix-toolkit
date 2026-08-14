/*
Model: Sphere Bot Rusty Version. Author: 3DHaupt. Source : https://sketchfab.com/models/d18753fe3e494ddbbc52a8a2e58be7a4
Model: Character. Source : https://github.com/spazzarama/Direct3D-Rendering-Cookbook
*/

using System.Diagnostics;
using DemoCore;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Media3D = System.Windows.Media.Media3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace BoneSkinDemo;

public class MainViewModel : BaseViewModel {
    public SceneNodeGroupModel3D ModelGroup { get; } = new();

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                foreach (var m in boneSkinNodes) {
                    m.RenderWireframe = value;
                }
            }
        }
        get;
    } = false;

    public bool ShowSkeleton {
        set {
            if (SetValue(ref field, value)) {
                foreach (var m in skeletonNodes) {
                    m.Visible = value;
                }
            }
        }
    } = false;

    public bool EnableAnimation {
        set {
            field = value;
            OnPropertyChanged();
            if (field) {
                compositeHelper.Rendering += CompositeHelper_Rendering;
            } else {
                compositeHelper.Rendering -= CompositeHelper_Rendering;
            }
        }
        get;
    } = true;

    public string SelectedAnimation {
        set {
            if (SetValue(ref field, value)) {
                reset = true;
                var curr = scene.Animations.Where(x => x.Name == value).FirstOrDefault();
                animationUpdater = new NodeAnimationUpdater(curr) {
                    RepeatMode = selectedRepeatMode
                };
            }
        }
        get;
    }

    private AnimationRepeatMode selectedRepeatMode = AnimationRepeatMode.Loop;

    public AnimationRepeatMode SelectedRepeatMode {
        set {
            if (SetValue(ref selectedRepeatMode, value)) {
                reset = true;
                animationUpdater?.RepeatMode = value;
            }
        }
        get => selectedRepeatMode;
    }

    public Media3D.Transform3D ModelTransform { private set; get; }

    public LineGeometry3D HitLineGeometry { get; } = new() { IsDynamic = true };

    public string[] Animations { set; get; }

    public GridPattern[] GridTypes { get; } = [GridPattern.Tile, GridPattern.Grid];

    public AnimationRepeatMode[] RepeatModes { get; } = [AnimationRepeatMode.Loop, AnimationRepeatMode.PlayOnce, AnimationRepeatMode.PlayOnceHold];

    private const int NumSegments = 100;
    private const int Theta = 24;
    private long startAniTime = 0;
    private CancellationTokenSource cts = new();
    private SynchronizationContext context = SynchronizationContext.Current;

    private bool reset = true;
    private HelixToolkitScene scene;
    private NodeAnimationUpdater animationUpdater;
    private List<BoneSkinMeshNode> boneSkinNodes = [];
    private List<BoneSkinMeshNode> skeletonNodes = [];
    private CompositionTargetEx compositeHelper = new();

    public MainViewModel() {
        Title = "BoneSkin Demo";
        SubTitle = "WPF & SharpDX";
        EffectsManager = new DefaultEffectsManager();

        Camera = new PerspectiveCamera {
            Position = new Media3D.Point3D(50, 50, 50),
            LookDirection = new Media3D.Vector3D(-50, -50, -50),
            UpDirection = new Media3D.Vector3D(0, 1, 0),
            NearPlaneDistance = 1,
            FarPlaneDistance = 2000
        };
        HitLineGeometry.Positions = [Vector3.Zero, Vector3.Zero];
        HitLineGeometry.Indices = [0, 1];
        LoadFile();
        compositeHelper.Rendering += CompositeHelper_Rendering;
    }

    private void LoadFile() {
        var importer = new Importer();
        importer.Configuration.CreateSkeletonForBoneSkinningMesh = true;
        importer.Configuration.SkeletonSizeScale = 0.04f;
        importer.Configuration.GlobalScale = 0.1f;
        scene = importer.Load("Solus The Knight\\Solus_The_Knight.fbx");
        ModelGroup.AddNode(scene.Root);
        Animations = [.. scene.Animations.Select(x => x.Name)];
        foreach (var node in scene.Root.Items.Traverse(false)) {
            if (node is BoneSkinMeshNode m) {
                if (!m.IsSkeletonNode) {
                    m.IsThrowingShadow = true;
                    m.WireframeColor = new Color4(0, 0, 1, 1);
                    boneSkinNodes.Add(m);
                    m.MouseDown += HandleMouseDown;
                } else {
                    skeletonNodes.Add(m);
                    m.Visible = false;
                }
            }
        }
    }

    private void HandleMouseDown(object sender, SceneNodeMouseDownArgs e) {
        var result = e.HitResult;
        HitLineGeometry.Positions[0] = result.PointHit - result.NormalAtHit * 0.5f;
        HitLineGeometry.Positions[1] = result.PointHit + result.NormalAtHit * 0.5f;
        HitLineGeometry.UpdateVertices();
    }

    private void CompositeHelper_Rendering(object sender, System.Windows.Media.RenderingEventArgs e) {
        if (animationUpdater != null) {
            if (reset) {
                animationUpdater.Reset();
                animationUpdater.RepeatMode = SelectedRepeatMode;
                reset = false;
                startAniTime = 0;
            } else {
                if (startAniTime == 0) {
                    startAniTime = Stopwatch.GetTimestamp();
                }

                var elapsed = Stopwatch.GetTimestamp() - startAniTime;
                animationUpdater.Update(elapsed, Stopwatch.Frequency);
            }
        }
    }

    protected override void Dispose(bool disposing) {
        cts.Cancel(true);
        compositeHelper.Dispose();
        base.Dispose(disposing);
    }
}
