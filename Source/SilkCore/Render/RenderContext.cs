// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderContext.cs" company="Helix Toolkit">
//   Copyright (c) 2018 Helix Toolkit contributors
// </copyright>
// <summary>
//   The render-context is currently generated per frame
//   Optimizations might be possible
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Render;

public enum OitRenderType {
    None,

    /// <summary>
    ///     Use weighted order independent transparent rendering. This OIT is the fastest but not color accurate in many cases.
    /// </summary>
    SinglePassWeighted,

    /// <summary>
    ///     Use classic depth peeling method for independent transparent rendering.
    /// </summary>
    DepthPeeling
}

public enum OitRenderStage {
    None,
    SinglePassWeighted,
    DepthPeelingInitMinMaxZ,
    DepthPeeling
}

/// <summary>
///     The render-context is currently generated per frame
///     Optimizations might be possible
/// </summary>
public sealed class RenderContext : DisposeObject, IRenderMatrices {
    private readonly Stack<GlobalTransformStruct> transformHistory = new();
    private readonly float actualWidth;
    private readonly float actualHeight;
    private readonly float dpiScale;

    /// <summary>
    ///     Gets or sets a value indicating whether [update octree] automatically.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [update octree]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoUpdateOctree = true;

    /// <summary>
    ///     Gets or sets the bounding frustum.
    /// </summary>
    /// <value>
    ///     The bounding frustum.
    /// </value>
    public BoundingFrustum BoundingFrustum;

    private CameraCore? camera;

    /// <summary>
    ///     Gets or sets the name of the custom pass.
    /// </summary>
    /// <value>
    ///     The name of the custom pass.
    /// </value>
    public string CustomPassName = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether [enable bounding frustum].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable bounding frustum]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableBoundingFrustum = false;

    private GlobalTransformStruct globalTransform;

    /// <summary>
    ///     Gets or sets a value indicating whether is deferred pass.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is deferred pass; otherwise, <c>false</c>.
    /// </value>
    public bool IsDeferredPass;

    /// <summary>
    ///     Gets or sets a value indicating whether this render pass is using inverted cull mode.
    ///     If <see cref="CullMode" />=<see cref="CullMode.None" />, default state is used.
    ///     This is usually used when rendering <see cref="Core.DynamicCubeMapCore" />.
    /// </summary>
    /// <value>
    ///     <c>true</c> Set invert cullmode flag; otherwise, <c>false</c>.
    /// </value>
    public bool IsInvertCullMode = false;

    private Light3DSceneShared? lightScene;

    private volatile bool needsUpdate = true;

    /// <summary>
    ///     Gets or sets a value indicating order independent transparent pass stage.
    /// </summary>
    /// <value>
    ///     <c>None</c> It is not using oit pass <c>false</c>.
    /// </value>
    public OitRenderStage OitRenderStage = OitRenderStage.None;

    private ContextSharedResource? sharedResource;

    /// <summary>
    ///     Gets or sets the time stamp.
    /// </summary>
    /// <value>
    ///     The time stamp.
    /// </value>
    public TimeSpan TimeStamp;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RenderContext" /> class.
    /// </summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="actualWidth">The viewport width.</param>
    /// <param name="actualHeight">The viewport height.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
    public RenderContext(CameraCore camera, float actualWidth, float actualHeight, float dpiScale = 1) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actualWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actualHeight);
        if (!float.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        this.actualWidth = actualWidth;
        this.actualHeight = actualHeight;
        this.dpiScale = dpiScale;
        IsDeferredPass = false;
        sharedResource = new ContextSharedResource();
        OitWeightPower = 3;
        OitWeightDepthSlope = 1;
        OitWeightMode = OitWeightMode.Linear2;
        Camera = camera;
    }

    /// <summary>
    ///     Current viewport setting
    /// </summary>
    public ViewportF Viewport { get; private set; }

    /// <summary>
    ///     Gets or sets the camera.
    /// </summary>
    /// <value>
    ///     The camera.
    /// </value>
    public CameraCore? Camera {
        get => camera;
        set {
            if (camera == value) {
                needsUpdate = true;
                Update();
                return;
            }

            if (camera is not null) camera.PropertyChanged -= Camera_PropertyChanged;
            camera = value;
            if (camera is not null) camera.PropertyChanged += Camera_PropertyChanged;
            Update();
        }
    }

    /// <summary>
    ///     Gets or sets the light scene.
    /// </summary>
    /// <value>
    ///     The light scene.
    /// </value>
    public Light3DSceneShared LightScene => lightScene.AssertNotNull("Light scene has been disposed.");

    /// <summary>
    ///     Gets the global transform.
    /// </summary>
    /// <value>
    ///     The global transform.
    /// </value>
    public GlobalTransformStruct GlobalTransform => globalTransform;

    /// <summary>
    ///     Gets or sets the shared resource.
    /// </summary>
    /// <value>
    ///     The shared resource.
    /// </value>
    public ContextSharedResource SharedResource => sharedResource.AssertNotNull("Shared resource has been disposed.");

    /// <summary>
    ///     Gets or sets the oit weight power used for color weight calculation. Default = 3;
    /// </summary>
    /// <value>
    ///     The oit weight power.
    /// </value>
    public float OitWeightPower {
        get => globalTransform.OITWeightPower;
        set => globalTransform.OITWeightPower = value;
    }

    /// <summary>
    ///     Gets or sets the oit weight depth slope. Used to increase resolution for particular range of depth values.
    ///     <para>
    ///         If value = 2, the depth range from 0-0.5 expands to 0-1 to increase resolution. However, values from 0.5 - 1
    ///         will be pushed to 1
    ///     </para>
    /// </summary>
    /// <value>
    ///     The oit weight depth slope.
    /// </value>
    public float OitWeightDepthSlope {
        get => globalTransform.OITWeightDepthSlope;
        set => globalTransform.OITWeightDepthSlope = value;
    }

    /// <summary>
    ///     Gets or sets the oit weight mode.
    ///     <para>Please refer to http://jcgt.org/published/0002/02/09/ </para>
    ///     <para>Linear0: eq7; Linear1: eq8; Linear2: eq9; NonLinear: eq10</para>
    /// </summary>
    /// <value>
    ///     The oit weight mode.
    /// </value>
    public OitWeightMode OitWeightMode {
        get => (OitWeightMode)globalTransform.OITWeightMode;
        set => globalTransform.OITWeightMode = (int)value;
    }

    /// <summary>
    ///     Depth peeling iteration. Default is 4.
    ///     Iteration depends on estimating number of overlapping semi-transparent layers to be accurately rendered.
    /// </summary>
    public int OitDepthPeelingIteration { get; set; } = 4;

    /// <summary>
    ///     Gets or sets a value indicating whether [ssao enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [ssao enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool SsaoEnabled {
        get => globalTransform.SSAOEnabled == 1u;
        set => globalTransform.SSAOEnabled = value ? 1u : 0;
    }

    /// <summary>
    ///     Gets or sets the ssao bias.
    /// </summary>
    /// <value>
    ///     The ssao bias.
    /// </value>
    public float SsaoBias {
        get => globalTransform.SSAOBias;
        set => globalTransform.SSAOBias = value;
    }

    /// <summary>
    ///     Gets or sets the ssao intensity.
    /// </summary>
    /// <value>
    ///     The ssao intensity.
    /// </value>
    public float SsaoIntensity {
        get => globalTransform.SSAOIntensity;
        set => globalTransform.SSAOIntensity = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [update scene graph requested] in this frame.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [update scene graph requested]; otherwise, <c>false</c>.
    /// </value>
    public bool UpdateSceneGraphRequested { get; internal set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [update per frame renderable requested] in this frame.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [update per frame renderable requested]; otherwise, <c>false</c>.
    /// </value>
    public bool UpdatePerFrameRenderableRequested { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether this instance is shadow map enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is shadow map enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsShadowMapEnabled { get; set; }

    /// <summary>
    ///     Gets the camera parameters.
    /// </summary>
    /// <value>
    ///     The camera parameters.
    /// </value>
    public FrustumCameraParams CameraParams { get; private set; }

    public bool IsPerspective => globalTransform.IsPerspective;

    /// <summary>
    ///     Gets the view matrix.
    /// </summary>
    /// <value>
    ///     The view matrix.
    /// </value>
    public Matrix ViewMatrix => globalTransform.View;

    /// <summary>
    ///     Gets or sets the inversed view matrix.
    /// </summary>
    /// <value>
    ///     The inversed view matrix.
    /// </value>
    public Matrix ViewMatrixInv => globalTransform.View.PsudoInvert();

    /// <summary>
    ///     Gets or sets the projection matrix.
    /// </summary>
    /// <value>
    ///     The projection matrix.
    /// </value>
    public Matrix ProjectionMatrix => globalTransform.Projection;

    /// <summary>
    ///     Gets the viewport matrix.
    /// </summary>
    /// <value>
    ///     The viewport matrix.
    /// </value>
    public Matrix ViewportMatrix =>
        new(globalTransform.Viewport.X / 2,
            0,
            0,
            0,
            0,
            -(globalTransform.Viewport.Y / 2),
            0,
            0,
            0,
            0,
            1,
            0,
            (globalTransform.Viewport.X - 1) / 2,
            (globalTransform.Viewport.Y - 1) / 2,
            0,
            1);

    /// <summary>
    ///     Gets the screen view projection matrix.
    /// </summary>
    /// <value>
    ///     The screen view projection matrix.
    /// </value>
    public Matrix ScreenViewProjectionMatrix { get; private set; } = Matrix.Identity;

    /// <summary>
    ///     Gets the actual width.
    /// </summary>
    /// <value>
    ///     The actual width.
    /// </value>
    public float ActualWidth => actualWidth;

    /// <summary>
    ///     Gets the actual height.
    /// </summary>
    /// <value>
    ///     The actual height.
    /// </value>
    public float ActualHeight => actualHeight;

    /// <summary>
    ///     Gets the dpi scale.
    /// </summary>
    /// <value>
    ///     The dpi scale.
    /// </value>
    public float DpiScale => dpiScale;

    public void Update() {
        if (camera == null || !needsUpdate) return;
        needsUpdate = false;
        globalTransform.View = camera.CreateViewMatrix();
        var aspectRatio = ActualWidth / ActualHeight;
        globalTransform.Projection = camera.CreateProjectionMatrix(aspectRatio);
        globalTransform.ViewProjection = ViewMatrix * ProjectionMatrix;
        // viewport: W,H,1/W,1/H
        globalTransform.Viewport = globalTransform.Resolution
                                       = new Vector4(ActualWidth, ActualHeight, 1f / ActualWidth, 1f / ActualHeight);
        CameraParams = camera.CreateCameraParams(aspectRatio);
        BoundingFrustum = new BoundingFrustum(globalTransform.ViewProjection);
        // frustum: FOV,AR,N,F
        globalTransform.Frustum =
            new Vector4(CameraParams.Fov, CameraParams.AspectRatio, CameraParams.ZNear, CameraParams.ZFar);
        globalTransform.EyePos = CameraParams.Position;
        globalTransform.IsPerspective = !BoundingFrustum.IsOrthographic;
        globalTransform.TimeStamp = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        globalTransform.DpiScale = DpiScale;
        Viewport = new ViewportF(0, 0, ActualWidth, ActualHeight);
        ScreenViewProjectionMatrix = ViewMatrix * ProjectionMatrix * ViewportMatrix;
    }

    private void Camera_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        needsUpdate = true;
    }

    public void Set(ref GlobalTransformStruct transforms, ViewportF viewport) {
        Set(ref transforms, ref viewport);
    }


    public void Set(ref GlobalTransformStruct transforms, ref ViewportF viewport) {
        globalTransform = transforms;
        Viewport = viewport;
        ScreenViewProjectionMatrix = ViewMatrix * ProjectionMatrix * ViewportMatrix;
        needsUpdate = true;
    }

    public void RestoreGlobalTransform() {
        needsUpdate = true;
        Update();
    }

    /// <summary>
    ///     Gets the screen view projection matrix.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Matrix GetScreenViewProjectionMatrix() => ScreenViewProjectionMatrix;

    protected override void OnDispose(bool disposeManagedResources) {
        Camera = null;
        RemoveAndDispose(ref lightScene);
        RemoveAndDispose(ref sharedResource);
        base.OnDispose(disposeManagedResources);
    }
}
