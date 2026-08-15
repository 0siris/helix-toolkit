using System.Diagnostics;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using ImGuiNET;

namespace WinFormsTest;

public static class SceneUi {
    private static bool showImGuiDemo;
    private static string exception = "";
    private static bool loading;
    private static string modelName = "";
    private static long currentTime;
    public static string SomeTextFromOutside = "";

    public static HelixToolkitScene? Scene;
    public static IList<IAnimationUpdater>? AnimationUpdaters;

    private static bool[] animationSelection = [];
    private static string[] animationNames = [];
    private static int currentSelectedAnimation = -1;
    private const int FrameDataLength = 128;
    private static float[] fps = new float[FrameDataLength];
    private static float[] frustumTest = new float[FrameDataLength];
    private static float[] latency = new float[FrameDataLength];
    private static int currFpsIndex;

    public static void DrawUi(int width, int height, ref ViewportOptions options, GroupNode rootNode) {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 350));
        var opened = true;
        ImGui.Begin("Model Loader Window",
            ref opened,
            ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("Load Model", !loading)) {
                if (ImGui.MenuItem("Open")) {
                    LoadModel(rootNode, options.ShowEnvironmentMap);
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Options")) {
                ImGui.Checkbox("Dir Light Follow Camera", ref options.DirectionalLightFollowCamera);
                ImGui.SliderFloat("Dir Light Intensity",
                    ref options.DirectionLightIntensity,
                    0,
                    1,
                    "",
                    ImGuiSliderFlags.AlwaysClamp);
                ImGui.SliderFloat("Ambient Light Intensity",
                    ref options.AmbientLightIntensity,
                    0,
                    1,
                    "",
                    ImGuiSliderFlags.AlwaysClamp);
                ImGui.Separator();
                ImGui.Checkbox("Show EnvironmentMap", ref options.ShowEnvironmentMap);
                ImGui.Checkbox("Enable SSAO", ref options.EnableSsao);
                ImGui.Checkbox("Enable FXAA", ref options.EnableFxaa);
                ImGui.Checkbox("Enable Frustum", ref options.EnableFrustum);
                ImGui.Checkbox("Enable DpiScale", ref options.EnableDpiScale);
                if (ImGui.Checkbox("Show Wireframe", ref options.ShowWireframe)) {
                    options.ShowWireframeChanged = true;
                }

                ImGui.Separator();
                ImGui.ColorPicker3("Background Color", ref options.BackgroundColor);
                ImGui.EndMenu();
            }

            if (!showImGuiDemo && ImGui.BeginMenu("ImGui Demo")) {
                if (ImGui.MenuItem("Show")) {
                    showImGuiDemo = true;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        if (ImGui.CollapsingHeader("Mouse Gestures", ImGuiTreeNodeFlags.DefaultOpen)) {
            ImGui.Text("Mouse Right: Rotate");
            ImGui.Text("Mouse Middle: Pan");
            ImGui.Separator();
            if (!string.IsNullOrEmpty(SomeTextFromOutside)) {
                ImGui.Text(SomeTextFromOutside);
            }
        }

        ImGui.Separator();
        ImGui.Text("FPS");
        ImGui.PlotLines("",
            ref fps[0],
            fps.Length,
            0,
            $"{fps[currFpsIndex]}",
            30,
            70,
            new System.Numerics.Vector2(200, 50));
        ImGui.Text("Rendering Latency Ms");
        ImGui.PlotLines("",
            ref latency[0],
            latency.Length,
            0,
            $"{latency[currFpsIndex]}ms",
            0,
            5,
            new System.Numerics.Vector2(200, 50));
        if (options.Viewport.RenderHost is {RenderStatistics: { } statistics}) {
            fps[currFpsIndex] = 1000f / (float) statistics.LatencyStatistics.AverageValue;
            latency[currFpsIndex] = (float) statistics.LatencyStatistics.AverageValue;
            frustumTest[currFpsIndex] = statistics.FrustumTestTime * 1000;
        }

        ImGui.Text("Frustum Test Ms");
        ImGui.PlotLines("",
            ref frustumTest[0],
            frustumTest.Length,
            0,
            $"{frustumTest[currFpsIndex]}ms",
            0,
            5,
            new System.Numerics.Vector2(200, 50));
        currFpsIndex = (currFpsIndex + 1) % FrameDataLength;

        if (!loading && ImGui.CollapsingHeader("Scene Graph", ImGuiTreeNodeFlags.DefaultOpen)) {
            DrawSceneGraph(rootNode);
        }

        if (!loading && Scene is not null && Scene.Animations.Count > 0) {
            DrawAnimations(ref options);
        }

        if (!loading && !string.IsNullOrEmpty(exception)) {
            ImGui.Separator();
            ImGui.Text(exception);
        }

        if (loading) {
            ImGui.Text($"Loading: {modelName}");
            var progress = ((float) (Stopwatch.GetTimestamp() - currentTime) / Stopwatch.Frequency) * 100 % 100;
            ImGui.ProgressBar(progress / 100, new System.Numerics.Vector2(width, 20), "");
        }

        ImGui.End();
        if (showImGuiDemo) {
            opened = false;
            ImGui.ShowDemoWindow(ref showImGuiDemo);
        }
    }

    private static void LoadModel(GroupNode node, bool renderEnvironmentMap) {
        var dialog = new OpenFileDialog {
            Filter = Importer.SupportedFormatsString
        };
        if (dialog.ShowDialog() == DialogResult.OK) {
            var path = dialog.FileName;
            exception = "";
            currentTime = Stopwatch.GetTimestamp();
            loading = true;
            modelName = Path.GetFileName(path);
            Task.Run(() => {
                    var importer = new Importer();
                    return importer.Load(path);
                })
                .ContinueWith((x) => {
                        loading = false;
                        if (x.Status == TaskStatus.RanToCompletion && x.Result is { } loadedScene) {
                            node.Clear();
                            foreach (var model in loadedScene.Root.Traverse()) {
                                if (model is MeshNode mesh) {
                                    if (mesh.Material is PbrMaterialCore pbr) {
                                        pbr.RenderEnvironmentMap = renderEnvironmentMap;
                                    } else if (mesh.Material is PhongMaterialCore phong) {
                                        phong.RenderEnvironmentMap = renderEnvironmentMap;
                                    }
                                }
                            }

                            node.AddChildNode(loadedScene.Root);
                            Scene = loadedScene;
                            if (loadedScene.Animations.Count > 0) {
                                var updaters = loadedScene.Animations.CreateAnimationUpdaters()
                                    .Values.ToArray();
                                AnimationUpdaters = updaters;
                                animationSelection = new bool[updaters.Length];
                                animationNames = [.. updaters.Select(ani => ani.Name)];
                                currentSelectedAnimation = -1;
                            }
                        } else if (x.Exception != null) {
                            exception = x.Exception.Message;
                        }
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    private static void DrawSceneGraph(SceneNode node) {
        if (node.Items.Count > 0) {
            if (node.IsAnimationNode) {
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0, 1, 1, 1));
            }

            if (ImGui.TreeNode(node.Name)) {
                if (node.IsAnimationNode) {
                    ImGui.PopStyleColor();
                }

                foreach (var n in node.Items) {
                    DrawSceneGraph(n);
                }

                ImGui.TreePop();
            } else if (node.IsAnimationNode) {
                ImGui.PopStyleColor();
            }
        } else {
            ImGui.Text(node.Name);
        }
    }

    private static void DrawAnimations(ref ViewportOptions options) {
        if (animationNames.Length > 0 && AnimationUpdaters is { } animationUpdaters) {
            ImGui.Text($"Animations: {animationNames.Length}");
            if (ImGui.Combo(" ", ref currentSelectedAnimation, animationNames, animationNames.Length)) {
                options.InitTimeStamp = 0;
                if (currentSelectedAnimation >= 0 && currentSelectedAnimation < animationNames.Length) {
                    if (animationUpdaters[currentSelectedAnimation] is { } animationUpdater) {
                        options.AnimationUpdater = animationUpdater;
                        animationUpdater.Reset();
                        animationUpdater.RepeatMode = AnimationRepeatMode.Loop;
                    }

                    options.PlayAnimation = true;
                } else {
                    options.PlayAnimation = false;
                    options.AnimationUpdater = null;
                }
            }
        }
    }
}