using System.Diagnostics;
using HelixToolkit.SharpDX.Core.Assimp;
using ImGuiNET;

namespace WinFormsTest;

public static class SceneUi {
    private static bool _showImGuiDemo = false;
    private static string _exception = "";
    private static bool _loading = false;
    private static string _modelName = "";
    private static long _currentTime = 0;
    public static string SomeTextFromOutside = "";

    public static HelixToolkitScene? Scene;
    public static IList<IAnimationUpdater>? AnimationUpdaters;

    private static bool[] _animationSelection = [];
    private static string[] _animationNames = [];
    private static int _currentSelectedAnimation = -1;
    private const int FrameDataLength = 128;
    private static float[] _fps = new float[FrameDataLength];
    private static float[] _frustumTest = new float[FrameDataLength];
    private static float[] _latency = new float[FrameDataLength];
    private static int _currFpsIndex = 0;

    public static void DrawUi(int width, int height, ref ViewportOptions options, GroupNode rootNode) {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 350));
        bool opened = true;
        ImGui.Begin("Model Loader Window",
            ref opened,
            ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);
        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("Load Model", !_loading)) {
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

            if (!_showImGuiDemo && ImGui.BeginMenu("ImGui Demo")) {
                if (ImGui.MenuItem("Show")) {
                    _showImGuiDemo = true;
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
            ref _fps[0],
            _fps.Length,
            0,
            $"{_fps[_currFpsIndex]}",
            30,
            70,
            new System.Numerics.Vector2(200, 50));
        ImGui.Text("Rendering Latency Ms");
        ImGui.PlotLines("",
            ref _latency[0],
            _latency.Length,
            0,
            $"{_latency[_currFpsIndex]}ms",
            0,
            5,
            new System.Numerics.Vector2(200, 50));
        if (options.Viewport.RenderHost is {RenderStatistics: { } statistics}) {
            _fps[_currFpsIndex] = 1000f / (float) statistics.LatencyStatistics.AverageValue;
            _latency[_currFpsIndex] = (float) statistics.LatencyStatistics.AverageValue;
            _frustumTest[_currFpsIndex] = (float) statistics.FrustumTestTime * 1000;
        }

        ImGui.Text("Frustum Test Ms");
        ImGui.PlotLines("",
            ref _frustumTest[0],
            _frustumTest.Length,
            0,
            $"{_frustumTest[_currFpsIndex]}ms",
            0,
            5,
            new System.Numerics.Vector2(200, 50));
        _currFpsIndex = (_currFpsIndex + 1) % FrameDataLength;

        if (!_loading && ImGui.CollapsingHeader("Scene Graph", ImGuiTreeNodeFlags.DefaultOpen)) {
            DrawSceneGraph(rootNode);
        }

        if (!_loading && Scene is not null && Scene.Animations.Count > 0) {
            DrawAnimations(ref options);
        }

        if (!_loading && !string.IsNullOrEmpty(_exception)) {
            ImGui.Separator();
            ImGui.Text(_exception);
        }

        if (_loading) {
            ImGui.Text($"Loading: {_modelName}");
            var progress = ((float) (Stopwatch.GetTimestamp() - _currentTime) / Stopwatch.Frequency) * 100 % 100;
            ImGui.ProgressBar(progress / 100, new System.Numerics.Vector2(width, 20), "");
        }

        ImGui.End();
        if (_showImGuiDemo) {
            opened = false;
            ImGui.ShowDemoWindow(ref _showImGuiDemo);
        }
    }

    private static void LoadModel(GroupNode node, bool renderEnvironmentMap) {
        OpenFileDialog dialog = new OpenFileDialog {
            Filter = Importer.SupportedFormatsString
        };
        if (dialog.ShowDialog() == DialogResult.OK) {
            var path = dialog.FileName;
            _exception = "";
            _currentTime = Stopwatch.GetTimestamp();
            _loading = true;
            _modelName = Path.GetFileName(path);
            Task.Run(() => {
                    var importer = new Importer();
                    return importer.Load(path);
                })
                .ContinueWith((x) => {
                        _loading = false;
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
                                _animationSelection = new bool[updaters.Length];
                                _animationNames = [.. updaters.Select(ani => ani.Name)];
                                _currentSelectedAnimation = -1;
                            }
                        } else if (x.Exception != null) {
                            _exception = x.Exception.Message;
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
        if (_animationNames.Length > 0 && AnimationUpdaters is { } animationUpdaters) {
            ImGui.Text($"Animations: {_animationNames.Length}");
            if (ImGui.Combo(" ", ref _currentSelectedAnimation, _animationNames, _animationNames.Length)) {
                options.InitTimeStamp = 0;
                if (_currentSelectedAnimation >= 0 && _currentSelectedAnimation < _animationNames.Length) {
                    if (animationUpdaters[_currentSelectedAnimation] is { } animationUpdater) {
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