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
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core;

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

    private CameraCore camera;

    private ConstantBufferProxy cbuffer;

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

    private Light3DSceneShared lightScene;

    private volatile bool needsUpdate = true;

    /// <summary>
    ///     Gets or sets a value indicating order independent transparent pass stage.
    /// </summary>
    /// <value>
    ///     <c>None</c> It is not using oit pass <c>false</c>.
    /// </value>
    public OitRenderStage OitRenderStage = OitRenderStage.None;

    private ContextSharedResource sharedResource;

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
    /// <param name="renderHost">The render host.</param>
    public RenderContext(IRenderHost renderHost) {
        RenderHost = renderHost;
        IsDeferredPass = false;
        cbuffer = renderHost.EffectsManager.ConstantBufferPool.Register(DefaultBufferNames.GlobalTransformCb,
                                                                        GlobalTransformStruct.SizeInBytes);
        lightScene = new Light3DSceneShared(renderHost.EffectsManager.ConstantBufferPool);
        sharedResource = new ContextSharedResource();
        OitWeightPower = 3;
        OitWeightDepthSlope = 1;
        OitWeightMode = OitWeightMode.Linear2;
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
    public CameraCore Camera {
        get => camera;
        set {
            if (camera == value) {
                needsUpdate = true;
                Update();
                return;
            }

            camera?.PropertyChanged -= Camera_PropertyChanged;
            camera = value;
            camera?.PropertyChanged += Camera_PropertyChanged;
            Update();
        }
    }

    /// <summary>
    ///     Gets or sets the light scene.
    /// </summary>
    /// <value>
    ///     The light scene.
    /// </value>
    public Light3DSceneShared LightScene => lightScene;

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
    public ContextSharedResource SharedResource => sharedResource;

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
    public bool IsShadowMapEnabled => RenderHost.IsShadowMapEnabled;

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
    public float ActualWidth => RenderHost.ActualWidth;

    /// <summary>
    ///     Gets the actual height.
    /// </summary>
    /// <value>
    ///     The actual height.
    /// </value>
    public float ActualHeight => RenderHost.ActualHeight;

    /// <summary>
    ///     Gets the dpi scale.
    /// </summary>
    /// <value>
    ///     The dpi scale.
    /// </value>
    public float DpiScale => RenderHost.DpiScale;

    /// <summary>
    ///     Gets the render host.
    /// </summary>
    /// <value>
    ///     The render host.
    /// </value>
    public IRenderHost RenderHost { get; }

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

    private void Camera_PropertyChanged(object sender, PropertyChangedEventArgs e) {
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
    internal Matrix GetScreenViewProjectionMatrix() {
        return ScreenViewProjectionMatrix;
    }

    /// <summary>
    ///     Call to update constant buffer for per frame
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePerFrameData(DeviceContextProxy deviceContext) {
        UpdatePerFrameData(true, true, deviceContext);
    }

    /// <summary>
    ///     Call to update constant buffer for per frame
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePerFrameData(bool updateGlobalTransform, bool updateLights, DeviceContextProxy deviceContext) {
        if (updateGlobalTransform) cbuffer.UploadDataToBuffer(deviceContext, ref globalTransform);
        if (updateLights) {
            LightScene.LightModels.HasEnvironmentMap = SharedResource.EnvironementMap != null;
            LightScene.LightModels.EnvironmentMapMipLevels = SharedResource.EnvironmentMapMipLevels;
            LightScene.UploadToBuffer(deviceContext);
        }
    }

    /// <summary>
    ///     Gets the off screen texture. Same as <see cref="GetOffScreenRt" /> or
    ///     <see cref="GetOffScreenDs" />
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="size">The size.</param>
    /// <param name="format">The format.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetOffScreenTexture(
        OffScreenTextureType type,
        OffScreenTextureSize size,
        Format format
    ) {
        switch (type) {
            case OffScreenTextureType.RenderTarget:
                return GetOffScreenRt(size, format);
            case OffScreenTextureType.DepthStencil:
                return GetOffScreenDs(size, format);
            default:
                return ShaderResourceViewProxy.Empty;
        }
    }

    /// <summary>
    ///     Gets the off screen render target.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="format">The format.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetOffScreenRt(OffScreenTextureSize size, Format format) {
        switch (size) {
            case OffScreenTextureSize.Full:
                return RenderHost.RenderBuffer.FullResRenderTargetPool.Get(format);
            case OffScreenTextureSize.Half:
                return RenderHost.RenderBuffer.HalfResRenderTargetPool.Get(format);
            case OffScreenTextureSize.Quarter:
                return RenderHost.RenderBuffer.QuarterResRenderTargetPool.Get(format);
            default:
                return ShaderResourceViewProxy.Empty;
        }
    }

    /// <summary>
    ///     Gets the off screen rt.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="format">The format.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetOffScreenRt(
        OffScreenTextureSize size,
        Format format,
        out int width,
        out int height
    ) {
        switch (size) {
            case OffScreenTextureSize.Full:
                width = RenderHost.RenderBuffer.FullResRenderTargetPool.Width;
                height = RenderHost.RenderBuffer.FullResRenderTargetPool.Height;
                return RenderHost.RenderBuffer.FullResRenderTargetPool.Get(format);
            case OffScreenTextureSize.Half:
                width = RenderHost.RenderBuffer.HalfResRenderTargetPool.Width;
                height = RenderHost.RenderBuffer.HalfResRenderTargetPool.Height;
                return RenderHost.RenderBuffer.HalfResRenderTargetPool.Get(format);
            case OffScreenTextureSize.Quarter:
                width = RenderHost.RenderBuffer.QuarterResRenderTargetPool.Width;
                height = RenderHost.RenderBuffer.QuarterResRenderTargetPool.Height;
                return RenderHost.RenderBuffer.QuarterResRenderTargetPool.Get(format);
            default:
                width = height = 0;
                return ShaderResourceViewProxy.Empty;
        }
    }

    /// <summary>
    ///     Gets the off screen depth stencil.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="format">The format.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetOffScreenDs(OffScreenTextureSize size, Format format) {
        switch (size) {
            case OffScreenTextureSize.Full:
                return RenderHost.RenderBuffer.FullResDepthStencilPool.Get(format);
            case OffScreenTextureSize.Half:
                return RenderHost.RenderBuffer.HalfResDepthStencilPool.Get(format);
            case OffScreenTextureSize.Quarter:
                return RenderHost.RenderBuffer.QuarterResDepthStencilPool.Get(format);
            default:
                return ShaderResourceViewProxy.Empty;
        }
    }

    /// <summary>
    ///     Gets the off screen ds.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="format">The format.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetOffScreenDs(
        OffScreenTextureSize size,
        Format format,
        out int width,
        out int height
    ) {
        switch (size) {
            case OffScreenTextureSize.Full:
                width = RenderHost.RenderBuffer.FullResDepthStencilPool.Width;
                height = RenderHost.RenderBuffer.FullResDepthStencilPool.Height;
                return RenderHost.RenderBuffer.FullResDepthStencilPool.Get(format);
            case OffScreenTextureSize.Half:
                width = RenderHost.RenderBuffer.HalfResDepthStencilPool.Width;
                height = RenderHost.RenderBuffer.HalfResDepthStencilPool.Height;
                return RenderHost.RenderBuffer.HalfResDepthStencilPool.Get(format);
            case OffScreenTextureSize.Quarter:
                width = RenderHost.RenderBuffer.QuarterResDepthStencilPool.Width;
                height = RenderHost.RenderBuffer.QuarterResDepthStencilPool.Height;
                return RenderHost.RenderBuffer.QuarterResDepthStencilPool.Get(format);
            default:
                width = height = 0;
                return ShaderResourceViewProxy.Empty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetPingPongBufferNextRtv() {
        return RenderHost.RenderBuffer.FullResPpBuffer.NextRtv;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceViewProxy GetPingPongBufferCurrentRtv() {
        return RenderHost.RenderBuffer.FullResPpBuffer.CurrentRtv;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        Camera = null;
        RemoveAndDispose(ref lightScene);
        RemoveAndDispose(ref sharedResource);
        RemoveAndDispose(ref cbuffer);
        base.OnDispose(disposeManagedResources);
    }
}
