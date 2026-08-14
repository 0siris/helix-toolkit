//#define TESTADDREMOVE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Controls;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using ImGuiNET;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using DrawingColor = System.Drawing.Color;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace WinFormsTest;

public static class DpiHelper {
    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

    public enum DeviceCap {
        VERTRES = 10,
        DESKTOPVERTRES = 117
    }

    public static double GetWindowsScreenScalingFactor(bool percentage = true) {
        //Create Graphics object from the current windows handle
        var GraphicsObject = Graphics.FromHwnd(IntPtr.Zero);
        //Get Handle to the device context associated with this Graphics object
        IntPtr DeviceContextHandle = GraphicsObject.GetHdc();
        //Call GetDeviceCaps with the Handle to retrieve the Screen Height
        int LogicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.VERTRES);
        int PhysicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.DESKTOPVERTRES);
        //Divide the Screen Heights to get the scaling factor and round it to two decimals
        double ScreenScalingFactor = Math.Round((double)PhysicalScreenHeight / (double)LogicalScreenHeight, 2);
        //If requested as percentage - convert it
        if (percentage) {
            ScreenScalingFactor *= 100.0;
        }

        //Release the Handle and Dispose of the GraphicsObject object
        GraphicsObject.ReleaseHdc(DeviceContextHandle);
        GraphicsObject.Dispose();
        //Return the Scaling Factor
        return ScreenScalingFactor;
    }
}

public class CoreTestApp {
    public ViewportCore Viewport => viewport;

    private readonly ViewportCore viewport;
    private readonly Form window;
    private readonly EffectsManager effectsManager;
    private CameraCore camera;
    private Geometry3D box, sphere, points, lines;
    private GroupNode groupSphere, groupBox, groupPoints, groupLines, groupModel, groupEffects;
    private EnvironmentMapNode environmentMap;
    private DirectionalLightNode directionalLight;
    private AmbientLightNode ambientLight;
    private const int NumItems = 400;
    private Random rnd = new((int)Stopwatch.GetTimestamp());
    private List<Tuple<bool, MaterialCore>> materials = [];
    private long previousTime;
    private bool resizeRequested = false;
    private CameraController cameraController;
    private Stack<IEnumerator<SceneNode>> stackCache = new();
    private IApplyPostEffect? currentHighlight = null;
    private double dpiScale = 1;
    private SynchronizationContext context;

    private ViewportOptions options = new() {
        AmbientLightIntensity = 0.2f,
        BackgroundColor = new System.Numerics.Vector3(0.4f, 0.4f, 0.4f),
        DirectionalLightFollowCamera = true,
        DirectionLightIntensity = 0.8f,
        EnableFrustum = true,
        EnableFXAA = true,
        EnableSSAO = true,
        WalkAround = false,
        ShowRenderDetail = false,
        ShowEnvironmentMap = false,
        EnableDpiScale = true
    };

    public CoreTestApp(Form window, SynchronizationContext context) {
        this.context = context;
        dpiScale = DpiHelper.GetWindowsScreenScalingFactor(false);

        var logger = HelixToolkit.Logger.LogManager.Create<CoreTestApp>();

        viewport = new ViewportCore(window.Handle, true) {
            DpiScale = dpiScale
        };
        cameraController = new CameraController(viewport) {
            CameraMode = CameraMode.Inspect,
            CameraRotationMode = CameraRotationMode.Trackball
        };
        this.window = window;
        window.ResizeEnd += Window_ResizeEnd;
        window.Load += Window_Load;
        window.FormClosing += Window_FormClosing;
        window.MouseMove += Window_MouseMove;
        window.MouseDown += Window_MouseDown;
        window.MouseUp += Window_MouseUp;
        window.MouseWheel += Window_MouseWheel;
        window.KeyDown += Window_KeyDown;
        window.KeyUp += Window_KeyUp;
        window.KeyPress += Window_KeyPress;
        effectsManager = new DefaultEffectsManager();
        effectsManager.AddTechnique(ImGuiNode.RenderTechnique);
        viewport.EffectsManager = effectsManager;
        viewport.StartRendering += Viewport_OnStartRendering;
        viewport.StopRendering += Viewport_OnStopRendering;
        viewport.ErrorOccurred += Viewport_OnErrorOccurred;
        viewport.CoordinateSystemLabelColor = new Color4(1, 1, 0, 1);
        options.Viewport = viewport;
        AssignViewportOption();
        InitializeScene();
    }

    private static Color4 ToColor4(DrawingColor color) {
        const float scale = 1f / 255f;
        return new Color4(color.R * scale, color.G * scale, color.B * scale, color.A * scale);
    }

    private static Matrix Translation(Vector3 value) {
        var result = Matrix.Identity;
        result.M41 = value.X;
        result.M42 = value.Y;
        result.M43 = value.Z;
        return result;
    }

    private void RunRenderLoop(Action render) {
        while (!window.IsDisposed && window.Visible) {
            Application.DoEvents();
            render();
        }
    }

    private void AssignViewportOption() {
        viewport.FxaaLevel = options.EnableFXAA ? FxaaLevel.Low : FxaaLevel.None;
        viewport.EnableRenderFrustum = options.EnableFrustum;
        viewport.BackgroundColor =
            new Color4(options.BackgroundColor.X, options.BackgroundColor.Y, options.BackgroundColor.Z, 1);
        viewport.EnableSsao = options.EnableSSAO;
        viewport.ShowRenderDetail = options.ShowRenderDetail;
        viewport.DpiScale = options.EnableDpiScale ? dpiScale : 1;
        if (options.ShowWireframeChanged) {
            options.ShowWireframeChanged = false;
            foreach (var node in groupModel.Items.Traverse(true, stackCache)) {
                if (node is MeshNode m) {
                    m.RenderWireframe = options.ShowWireframe;
                }
            }
        }
    }


    private void InitializeScene() {
        camera = new PerspectiveCameraCore() {
            LookDirection = new Vector3(0, 0, 50),
            Position = new Vector3(0, 0, -50),
            FarPlaneDistance = 5000,
            NearPlaneDistance = 1f,
            FieldOfView = 45,
            UpDirection = new Vector3(0, 1, 0)
        };
        viewport.CameraCore = camera;
        directionalLight = new DirectionalLightNode() {
            Direction = new Vector3(0, -1, 1),
            Color = ToColor4(DrawingColor.White).ChangeIntensity(options.DirectionLightIntensity)
        };
        viewport.Items.AddChildNode(directionalLight);

        ambientLight = new AmbientLightNode() {
            Color = ToColor4(DrawingColor.White).ChangeIntensity(options.AmbientLightIntensity)
        };
        viewport.Items.AddChildNode(ambientLight);

        groupModel = new GroupNode();
        viewport.Items.AddChildNode(groupModel);
        var builder = new MeshBuilder(true, true, true);
        builder.AddSphere(Vector3.Zero, 1, 12, 12);
        sphere = builder.ToMesh();
        builder = new MeshBuilder(true, true, true);
        builder.AddBox(Vector3.Zero, 1, 1, 1);
        box = builder.ToMesh();
        points = new PointGeometry3D() { Positions = sphere.Positions };
        var lineBuilder = new LineBuilder();
        lineBuilder.AddBox(Vector3.Zero, 2, 2, 2);
        lines = lineBuilder.ToLineGeometry3D();
        groupSphere = new GroupNode();
        groupBox = new GroupNode();
        groupLines = new GroupNode();
        groupPoints = new GroupNode();
        groupEffects = new GroupNode();
        InitializeMaterials();
        var materialCount = materials.Count;
        Task.Run(() => {
            var builder = new MeshBuilder(true, true, true);
            builder.AddSphere(Vector3.Zero, 1);
            for (int i = 0; i < NumItems; ++i) {
                var sphere1 = builder.ToMesh();
                var transform =
                    Translation(new Vector3(rnd.NextFloat(-20, 20), rnd.NextFloat(-20, 20), rnd.NextFloat(-20, 20)));
                var material = materials[i % materialCount];
                var node = new MeshNode() {
                    Geometry = sphere1,
                    IsTransparent = material.Item1,
                    Material = material.Item2,
                    ModelMatrix = transform,
                    CullMode = CullMode.Back
                };
                node.Attach(effectsManager);
                context.Post((o) => { groupSphere.AddChildNode(node); }, null);
                Task.Delay(1).Wait();
            }
        });

        Task.Run(() => {
            for (int i = 0; i < NumItems; ++i) {
                var transform =
                    Translation(new Vector3(rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50)));
                var material = materials[i % materialCount];
                var node = new MeshNode() {
                    Geometry = box,
                    IsTransparent = material.Item1,
                    Material = material.Item2,
                    ModelMatrix = transform,
                    CullMode = CullMode.Back
                };
                node.Attach(effectsManager);
                context.Post((o) => { groupBox.AddChildNode(node); }, null);
                Task.Delay(1).Wait();
            }
        });

        Task.Run(() => {
            for (int i = 0; i < NumItems; ++i) {
                var transform =
                    Translation(new Vector3(rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50)));
                var node = new PointNode() {
                    Geometry = points, ModelMatrix = transform,
                    Material = new PointMaterialCore() { PointColor = ToColor4(DrawingColor.Red) }
                };
                node.Attach(effectsManager);
                context.Post((o) => { groupPoints.AddChildNode(node); }, null);
                Task.Delay(1).Wait();
            }
        });

        Task.Run(() => {
            for (int i = 0; i < NumItems; ++i) {
                var transform =
                    Translation(new Vector3(rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50)));
                var node = new LineNode() {
                    Geometry = lines, ModelMatrix = transform,
                    Material = new LineMaterialCore() { LineColor = ToColor4(DrawingColor.LightBlue) }
                };
                node.Attach(effectsManager);
                context.Post((o) => { groupLines.AddChildNode(node); }, null);
                Task.Delay(1).Wait();
            }
        });

        groupModel.AddChildNode(groupSphere);
        groupSphere.AddChildNode(groupBox);
        groupSphere.AddChildNode(groupPoints);
        groupSphere.AddChildNode(groupLines);

        var imGui = new ImGuiNode();
        viewport.Items.AddChildNode(imGui);
        imGui.UpdatingImGuiUI += ImGui_UpdatingImGuiUI;
        groupEffects.AddChildNode(new NodePostEffectBorderHighlight() { EffectName = "highlightEffect", Color = ToColor4(DrawingColor.Yellow) });
        viewport.Items.AddChildNode(groupEffects);
        environmentMap = new EnvironmentMapNode() { Texture = TextureModel.Create("Cubemap_Grandcanyon.dds") };
        viewport.Items.AddChildNode(environmentMap);
        viewport.NodeHitOnMouseDown += Viewport_NodeHitOnMouseDown;
    }

    private void Viewport_NodeHitOnMouseDown(object sender, SceneNodeMouseDownArgs e) {
        currentHighlight?.PostEffects = "";

        currentHighlight = null;
        if (e.HitResult.ModelHit is IApplyPostEffect s) {
            currentHighlight = s;
            currentHighlight.PostEffects = "highlightEffect";
        }
    }

    private void ImGui_UpdatingImGuiUI(object sender, EventArgs e) {
        SceneUI.DrawUI((int)viewport.ActualWidth, (int)viewport.ActualHeight, ref options, groupModel);
    }

    private void InitializeMaterials() {
        var diffuse = TextureModel.Create("TextureCheckerboard2.jpg");
        var normal = TextureModel.Create("TextureCheckerboard2_dot3.jpg");
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.Red), DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.Green),
                                                        DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.Blue), DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.DodgerBlue),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.Orange),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = ToColor4(DrawingColor.PaleGreen),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false, new NormalMaterialCore()));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = ToColor4(DrawingColor.Beige),
                                                        MetallicFactor = 0.8f, RoughnessFactor = 0.6f
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = ToColor4(DrawingColor.Bisque),
                                                        MetallicFactor = 0.4f, RoughnessFactor = 0.9f
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(false,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = ToColor4(DrawingColor.Chartreuse),
                                                        MetallicFactor = 0.2f, RoughnessFactor = 0.2f
                                                    }));

        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = new Color4(1, 0, 1, 0.6f), DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = new Color4(0, 1, 1, 0.4f), DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new DiffuseMaterialCore() {
                                                        DiffuseColor = new Color4(1, 0, 1, 0.3f), DiffuseMap = diffuse
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = new Color4(1, 1, 0, 0.6f),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = new Color4(0, 1, 1, 0.4f),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PhongMaterialCore() {
                                                        DiffuseColor = new Color4(1, 0, 1, 0.3f),
                                                        ReflectiveColor = ToColor4(DrawingColor.DarkGray),
                                                        SpecularShininess = 10,
                                                        SpecularColor = ToColor4(DrawingColor.Red),
                                                        DiffuseMap = diffuse, NormalMap = normal
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = new Color4(1, 1, 0, 0.6f), MetallicFactor = 0.8f,
                                                        RoughnessFactor = 0.6f
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = new Color4(0, 1, 1, 0.4f), MetallicFactor = 0.4f,
                                                        RoughnessFactor = 0.9f
                                                    }));
        materials.Add(new Tuple<bool, MaterialCore>(true,
                                                    new PbrMaterialCore() {
                                                        AlbedoColor = new Color4(1, 0, 1, 0.6f), MetallicFactor = 0.2f,
                                                        RoughnessFactor = 0.2f
                                                    }));
    }

    private void Viewport_OnErrorOccurred(object sender, Exception e) { }

    private void Viewport_OnStopRendering(object sender, EventArgs e) { }

    private void Viewport_OnStartRendering(object sender, EventArgs e) {
        bool isGoingOut = true;
        bool isAddingNode = false;
        RunRenderLoop(() => {
            if (resizeRequested) {
                viewport.Resize(window.ClientSize.Width, window.ClientSize.Height);
                resizeRequested = false;
                return;
            }

            var pos = camera.Position;
            var t = Stopwatch.GetTimestamp();
            var elapse = t - previousTime;
            previousTime = t;
            cameraController.OnTimeStep();
            if (options.DirectionalLightFollowCamera) {
                directionalLight.Direction = camera.LookDirection.Normalized();
            }

            AssignViewportOption();
            directionalLight.Color = ToColor4(DrawingColor.White).ChangeIntensity(options.DirectionLightIntensity);
            ambientLight.Color = ToColor4(DrawingColor.White).ChangeIntensity(options.AmbientLightIntensity);
            ChangeEnvironmentMapVisibility(options.ShowEnvironmentMap);
            viewport.Render();

            if (options.PlayAnimation && options.AnimationUpdater != null) {
                var elapsed = Stopwatch.GetTimestamp() - options.InitTimeStamp;
                options.AnimationUpdater.Update(elapsed, Stopwatch.Frequency);
            }
#if TESTADDREMOVE
                if (groupSphere.Items.Count > 0 && !isAddingNode)
                {
                    groupSphere.RemoveChildNode(groupSphere.Items.First());
                    if (groupSphere.Items.Count == 0)
                    {
                        isAddingNode = true;
                        Console.WriteLine($"{effectsManager.GetResourceCountSummary()}");
                        groupSphere.AddChildNode(groupBox);
                        groupSphere.AddChildNode(groupPoints);
                        groupPoints.AddChildNode(groupLines);
                    }
                }
                else
                {
                    var materialCount = materialList.Length;
                    var transform =
 Translation(new Vector3(rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50), rnd.NextFloat(-50, 50)));
                    groupSphere.AddChildNode(new MeshNode() { Geometry = box, Material =
 materialList[groupSphere.Items.Count % materialCount], ModelMatrix = transform, CullMode = CullMode.Back });
                    transform =
 Translation(new Vector3(rnd.NextFloat(-20, 20), rnd.NextFloat(-20, 20), rnd.NextFloat(-20, 20)));
                    groupSphere.AddChildNode(new MeshNode() { Geometry = sphere, Material =
 materialList[groupSphere.Items.Count % materialCount], ModelMatrix = transform, CullMode = CullMode.Back });
                    if (groupSphere.Items.Count > NumItems)
                    {
                        isAddingNode = false;
                    }
                }
#endif
        });
    }

    private void ChangeEnvironmentMapVisibility(bool visible) {
        if (environmentMap.Visible != visible) {
            environmentMap.Visible = visible;
            foreach (var model in groupModel.Traverse()) {
                if (model is MeshNode mesh) {
                    if (mesh.Material is PbrMaterialCore pbr) {
                        pbr.RenderEnvironmentMap = visible;
                    } else if (mesh.Material is PhongMaterialCore phong) {
                        phong.RenderEnvironmentMap = visible;
                    }
                }
            }
        }
    }

    private void Window_ResizeEnd(object sender, EventArgs e) {
        resizeRequested = true;
    }

    public void RequestResize() {
        resizeRequested = true;
    }

    private void Window_FormClosing(object sender, FormClosingEventArgs e) {
        viewport.EndD3D();
    }

    private void Window_Load(object sender, EventArgs e) {
        viewport.StartD3D(window.ClientSize.Width, window.ClientSize.Height);
    }

    #region Handle mouse event

    private void Window_MouseMove(object sender, MouseEventArgs e) {
        var io = ImGui.GetIO();
        if (!cameraController.IsMouseCaptured) {
            io.MousePos = new System.Numerics.Vector2(e.X, e.Y);
        } else if (!io.WantCaptureMouse) {
            cameraController.MouseMove(new Vector2(e.X, e.Y));
            viewport.MouseMove(new Vector2(e.X, e.Y));
        }
    }

    private void Window_MouseUp(object sender, MouseEventArgs e) {
        var io = ImGui.GetIO();
        switch (e.Button) {
            case MouseButtons.Left:
                io.MouseDown[0] = false;
                break;
            case MouseButtons.Right:
                io.MouseDown[1] = false;
                break;
            case MouseButtons.Middle:
                io.MouseDown[2] = false;
                break;
        }

        if (cameraController.IsMouseCaptured) {
            switch (e.Button) {
                case MouseButtons.Left:
                    viewport.MouseUp(new Vector2(e.X, e.Y));
                    break;
                case MouseButtons.Right:
                    cameraController.EndRotate(new Vector2(e.X, e.Y));
                    break;
                case MouseButtons.Middle:
                    cameraController.EndPan(new Vector2(e.X, e.Y));
                    break;
            }
        }
    }

    private void Window_MouseDown(object sender, MouseEventArgs e) {
        var io = ImGui.GetIO();
        if (!cameraController.IsMouseCaptured) {
            switch (e.Button) {
                case MouseButtons.Left:
                    io.MouseDown[0] = true;
                    break;
                case MouseButtons.Right:
                    io.MouseDown[1] = true;
                    break;
                case MouseButtons.Middle:
                    io.MouseDown[2] = true;
                    break;
            }

            if (!io.WantCaptureMouse) {
                switch (e.Button) {
                    case MouseButtons.Left:
                        viewport.MouseDown(new Vector2(e.X, e.Y));
                        break;
                    case MouseButtons.Right:
                        cameraController.StartRotate(new Vector2(e.X, e.Y));
                        break;
                    case MouseButtons.Middle:
                        cameraController.StartPan(new Vector2(e.X, e.Y));
                        break;
                }
            }
        } else if (!io.WantCaptureMouse) {
            switch (e.Button) {
                case MouseButtons.Left:
                    break;
                case MouseButtons.Right:
                    cameraController.StartRotate(new Vector2(e.X, e.Y));
                    break;
                case MouseButtons.Middle:
                    cameraController.StartPan(new Vector2(e.X, e.Y));
                    break;
            }
        }
    }

    private void Window_MouseWheel(object sender, MouseEventArgs e) {
        var io = ImGui.GetIO();
        if (!cameraController.IsMouseCaptured) {
            io.MouseWheel = (int)(e.Delta * 0.01f);
        }

        if (!io.WantCaptureMouse) {
            cameraController.MouseWheel(e.Delta, new Vector2(e.X, e.Y));
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) {
        UpdateImGuiKey(e, true);
    }

    private void Window_KeyUp(object sender, KeyEventArgs e) {
        UpdateImGuiKey(e, false);
    }

    /// <summary>
    /// Forwards a Windows Forms keyboard event to Dear ImGui's named-key input queue.
    /// </summary>
    /// <param name="e">The Windows Forms keyboard event.</param>
    /// <param name="isDown">Whether the key was pressed.</param>
    private static void UpdateImGuiKey(KeyEventArgs e, bool isDown) {
        var io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);

        int keyValue = e.KeyValue;
        ImGuiKey key = keyValue switch {
            >= (int)Keys.D0 and <= (int)Keys.D9 => (ImGuiKey)((int)ImGuiKey._0 + keyValue - (int)Keys.D0),
            >= (int)Keys.A and <= (int)Keys.Z => (ImGuiKey)((int)ImGuiKey.A + keyValue - (int)Keys.A),
            >= (int)Keys.F1 and <= (int)Keys.F24 => (ImGuiKey)((int)ImGuiKey.F1 + keyValue - (int)Keys.F1),
            >= (int)Keys.NumPad0 and <= (int)Keys.NumPad9 =>
                (ImGuiKey)((int)ImGuiKey.Keypad0 + keyValue - (int)Keys.NumPad0),
            (int)Keys.Tab => ImGuiKey.Tab,
            (int)Keys.Left => ImGuiKey.LeftArrow,
            (int)Keys.Right => ImGuiKey.RightArrow,
            (int)Keys.Up => ImGuiKey.UpArrow,
            (int)Keys.Down => ImGuiKey.DownArrow,
            (int)Keys.PageUp => ImGuiKey.PageUp,
            (int)Keys.PageDown => ImGuiKey.PageDown,
            (int)Keys.Home => ImGuiKey.Home,
            (int)Keys.End => ImGuiKey.End,
            (int)Keys.Insert => ImGuiKey.Insert,
            (int)Keys.Delete => ImGuiKey.Delete,
            (int)Keys.Back => ImGuiKey.Backspace,
            (int)Keys.Space => ImGuiKey.Space,
            (int)Keys.Enter => ImGuiKey.Enter,
            (int)Keys.Escape => ImGuiKey.Escape,
            (int)Keys.ShiftKey or (int)Keys.LShiftKey => ImGuiKey.LeftShift,
            (int)Keys.RShiftKey => ImGuiKey.RightShift,
            (int)Keys.ControlKey or (int)Keys.LControlKey => ImGuiKey.LeftCtrl,
            (int)Keys.RControlKey => ImGuiKey.RightCtrl,
            (int)Keys.Menu or (int)Keys.LMenu => ImGuiKey.LeftAlt,
            (int)Keys.RMenu => ImGuiKey.RightAlt,
            (int)Keys.LWin => ImGuiKey.LeftSuper,
            (int)Keys.RWin => ImGuiKey.RightSuper,
            (int)Keys.Apps => ImGuiKey.Menu,
            (int)Keys.Oem7 => ImGuiKey.Apostrophe,
            (int)Keys.Oemcomma => ImGuiKey.Comma,
            (int)Keys.OemMinus => ImGuiKey.Minus,
            (int)Keys.OemPeriod => ImGuiKey.Period,
            (int)Keys.OemQuestion => ImGuiKey.Slash,
            (int)Keys.Oem1 => ImGuiKey.Semicolon,
            (int)Keys.Oemplus => ImGuiKey.Equal,
            (int)Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            (int)Keys.Oem5 => ImGuiKey.Backslash,
            (int)Keys.Oem6 => ImGuiKey.RightBracket,
            (int)Keys.Oemtilde => ImGuiKey.GraveAccent,
            (int)Keys.CapsLock => ImGuiKey.CapsLock,
            (int)Keys.Scroll => ImGuiKey.ScrollLock,
            (int)Keys.NumLock => ImGuiKey.NumLock,
            (int)Keys.PrintScreen => ImGuiKey.PrintScreen,
            (int)Keys.Pause => ImGuiKey.Pause,
            (int)Keys.Decimal => ImGuiKey.KeypadDecimal,
            (int)Keys.Divide => ImGuiKey.KeypadDivide,
            (int)Keys.Multiply => ImGuiKey.KeypadMultiply,
            (int)Keys.Subtract => ImGuiKey.KeypadSubtract,
            (int)Keys.Add => ImGuiKey.KeypadAdd,
            (int)Keys.BrowserBack => ImGuiKey.AppBack,
            (int)Keys.BrowserForward => ImGuiKey.AppForward,
            _ => ImGuiKey.None
        };

        if (key == ImGuiKey.None) {
            return;
        }

        io.AddKeyEvent(key, isDown);
        io.SetKeyEventNativeData(key, keyValue, -1);
    }


    private void Window_KeyPress(object sender, KeyPressEventArgs e) {
        var io = ImGui.GetIO();
        io.AddInputCharacter(e.KeyChar);
    }

    #endregion
}

internal static class RandomExtensions {
    public static float NextFloat(this Random random, float minimum, float maximum) => minimum + (float)random.NextDouble() * (maximum - minimum);
}
