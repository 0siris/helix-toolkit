/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public abstract class DX11RenderHostBase : DisposeObject, IRenderHost {
    private const int DxgiErrorDeviceRemoved = unchecked((int)0x887A0005);
    private const int DxgiErrorDeviceHung = unchecked((int)0x887A0006);
    private const int DxgiErrorDeviceReset = unchecked((int)0x887A0007);
    private const int DxgiErrorAccessLost = unchecked((int)0x887A0026);
    private const int MinWidth = 10;
    private const int MinHeight = 10;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11RenderHostBase" /> class.
    /// </summary>
    public DX11RenderHostBase() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11RenderHostBase" /> class.
    /// </summary>
    /// <param name="createRenderer">The create renderer.</param>
    public DX11RenderHostBase(Func<IDevice3DResources, IRenderer> createRenderer) 
        => createRendererFunction = createRenderer;

    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    /// <summary>
    ///     Invalidates the render.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateRender() {
        UpdateRequested = true;
        updateCounter = 0;
    }

    /// <summary>
    ///     Invalidates the scene graph, request a complete scene graph traverse during next frame.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateSceneGraph() {
        updateSceneGraphRequested = true;
        InvalidatePerFrameRenderables();
    }

    /// <summary>
    ///     Invalidates the per frame renderables.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidatePerFrameRenderables() {
        updatePerFrameRenderableRequested = true;
        InvalidateRender();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate(InvalidateTypes type) {
        switch (type) {
            case InvalidateTypes.SceneGraph:
                InvalidateSceneGraph();
                break;
            case InvalidateTypes.Render:
                InvalidateRender();
                break;
            case InvalidateTypes.PerFrameRenderables:
                InvalidatePerFrameRenderables();
                break;
        }
    }

    /// <summary>
    ///     Updates the and render.
    /// </summary>
    public bool UpdateAndRender() {
        if (!CanRender())
            return false;

        if (RenderBuffer is not { } renderBuffer
            || Renderer is not { } renderer
            || EffectsManager is not { } effectsManager
            || Viewport is not { } viewport
            || viewport.CameraCore is not { } camera)
            return false;

        if (RenderContext is not { } renderContext)
            return false;
        
        if (EnableSharingModelMode && SharedModelContainer != null)
            SharedModelContainer.CurrentRenderHost = this;
        
        IsBusy = true;
        
        var t0 = TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        renderStatistics.FpsStatistics.Push((t0 - lastRenderTime).TotalMilliseconds);
        renderStatistics.Camera = camera;
        
        lastRenderTime = t0;
        
        UpdateRequested = false;
        ++updateCounter;
        
        renderContext.AutoUpdateOctree = RenderConfiguration.AutoUpdateOctree;
        renderContext.EnableBoundingFrustum = EnableRenderFrustum;
        
        if (RenderConfiguration.UpdatePerFrameData) {
            viewport.Update(t0);
            renderContext.TimeStamp = t0;
            renderContext.Camera = camera;
            renderContext.OitWeightPower = RenderConfiguration.OitWeightPower;
            renderContext.OitWeightDepthSlope = RenderConfiguration.OitWeightDepthSlope;
            renderContext.OitWeightMode = RenderConfiguration.OitWeightMode;
            renderContext.SsaoEnabled = RenderConfiguration.EnableSsao;
            renderContext.SsaoBias = RenderConfiguration.SsaoBias;
            renderContext.SsaoIntensity = RenderConfiguration.SsaoIntensity;
            renderContext.OitDepthPeelingIteration = RenderConfiguration.OitDepthPeelingIteration;
        }

        renderBuffer.VSyncInterval = RenderConfiguration.EnableVSync
                                         ? 1 
                                         : 0;
        
        var updateSceneGraph = updateSceneGraphRequested;
        var updatePerFrameRenderable = updatePerFrameRenderableRequested;
        RenderContext.UpdateSceneGraphRequested = updateSceneGraphRequested;
        RenderContext.UpdatePerFrameRenderableRequested = updatePerFrameRenderableRequested;
        updateSceneGraphRequested = false;
        updatePerFrameRenderableRequested = false;
        
        PreRender(updateSceneGraph, updatePerFrameRenderable);
        try {
            if (renderBuffer.BeginDraw()) {
                OnRender(t0);
                renderBuffer.EndDraw();
                renderStatistics.NumDrawCalls = renderer.ImmediateContext.ResetDrawCalls() +
                                                effectsManager.DeviceContextPool.ResetDrawCalls();
            }

            if (RenderConfiguration.RenderD2D && D2DTarget?.D2DTarget is not null)
                OnRender2D(t0);
            
            renderBuffer.Present();
        } catch (COMException ex) {
            if (IsDeviceLost(ex.HResult)) {
                Logger.Warn("Device Lost, code = {Value0}", ex.HResult);
                RenderBuffer_OnDeviceLost(renderBuffer, EventArgs.Empty);
            } else {
                Logger.Error(ex, "DirectX Error during rendering");
                EndD3D();
                ExceptionOccurred?.Invoke(this, new RelayExceptionEventArgs(ex));
            }
        } catch (Exception ex) {
            Logger.Error(ex, "Error during rendering");
            EndD3D();
            ExceptionOccurred?.Invoke(this, new RelayExceptionEventArgs(ex));
        } finally {
            PostRender();
            IsBusy = false;
        }

        lastRenderingDuration = TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency) - t0;
        RenderStatistics.LatencyStatistics.Push(lastRenderingDuration.TotalMilliseconds);
        Rendered?.Invoke(this, EventArgs.Empty);
        
        return true;

    }

    /// <summary>
    ///     Clears the render target.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="clearBackBuffer">if set to <c>true</c> [clear back buffer].</param>
    /// <param name="clearDepthStencilBuffer">if set to <c>true</c> [clear depth stencil buffer].</param>
    public void ClearRenderTarget(
        DeviceContextProxy context,
        bool clearBackBuffer,
        bool clearDepthStencilBuffer
    ) =>
        RenderBuffer?.ClearRenderTarget(context, ClearColor, clearBackBuffer, clearDepthStencilBuffer);

    /// <summary>
    /// </summary>
    public void StartD3D(int width, int height) {
        lock (lockObj) {
            Logger.Info("Starting D3D. Width = {Value0}; Height = {Value1};", width, height);
            if (IsInitialized) {
                Logger.Info("RenderHost already Initialized");
                StartRendering();
                return;
            }

            ActualWidth = width * DpiScale;
            ActualHeight = height * DpiScale;
            isLoaded = true;
            var effectsManager = EffectsManager;
            if (effectsManager?.NativeDeviceResources.Device is not { } device || device.IsDisposed) {
                Logger.Info("EffectsManager is not valid");
                return;
            }

            ImmediateDeviceContext = new DeviceContextProxy(effectsManager.NativeDeviceResources.ImmediateContext,
                                                            device);
            
            RenderTechnique = effectsManager[DefaultRenderTechniqueNames.Mesh];
            CreateAndBindBuffers();
            IsInitialized = true;
            Logger.Info("Initialized");
            AttachRenderable(effectsManager);
            OnStartD3D();
            StartRendering();
        }
    }

    /// <summary>
    ///     Starts the rendering.
    /// </summary>
    public virtual void StartRendering() {
        lock (lockObj) {
            Logger.Info("Start rendering");
            renderStatistics.Reset();
            lastRenderingDuration = TimeSpan.Zero;
            lastRenderTime = TimeSpan.Zero;
            InvalidateSceneGraph();
        }

        StartRenderLoop?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// </summary>
    public void EndD3D() {
        lock (lockObj) {
            Logger.Info("Ending D3D");
            StopRendering();
            IsInitialized = false;
            ImmediateDeviceContext = null;
            

            OnEndingD3D();
            DetachRenderable();
            DisposeBuffers();
        }
    }

    /// <summary>
    ///     Stops the rendering.
    /// </summary>
    public virtual void StopRendering() {
        Logger.Info("Stop rendering");
        StopRenderLoop?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Resizes
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void Resize(int width, int height) 
        => Resize(width, height, false);

    /// <summary>
    ///     Sets the default render targets.
    /// </summary>
    /// <param name="clear">if set to <c>true</c> [clear].</param>
    public virtual void SetDefaultRenderTargets(bool clear) 
        => SetDefaultRenderTargets(ImmediateDeviceContext, clear);

    /// <summary>
    ///     Creates the render buffer.
    /// </summary>
    /// <returns></returns>
    protected abstract DX11RenderBufferProxyBase CreateRenderBuffer();

    /// <summary>
    ///     Determines whether this instance can render.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if this instance can render; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool CanRender() =>
        IsInitialized && IsRendering &&
        (UpdateRequested || updateCounter < RenderConfiguration.MinimumUpdateCount)
        && Viewport is {CameraCore: not null} && ActualWidth > 10 && ActualHeight > 10;

    /// <summary>
    ///     Called before OnRender.
    /// </summary>
    protected virtual void PreRender(bool invalidateSceneGraph, bool invalidatePerFrameRenderables) 
        => SetDefaultRenderTargets(ImmediateDeviceContext, RenderConfiguration.ClearEachFrame);

    /// <summary>
    ///     Called after OnRender.
    /// </summary>
    protected abstract void PostRender();

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="time">The time.</param>
    protected abstract void OnRender(TimeSpan time);

    /// <summary>
    ///     Called when [render2d].
    /// </summary>
    /// <param name="time">The time.</param>
    protected abstract void OnRender2D(TimeSpan time);

    /// <summary>
    ///     Set default render target to specify context.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="clear"></param>
    /// <returns>Set successful?</returns>
    public bool SetDefaultRenderTargets(DeviceContextProxy? context, bool clear = true) {
        if (!IsInitialized || context is null || RenderBuffer is not { } renderBuffer)
            return false;
        
        renderBuffer.SetDefaultRenderTargets(context);
        if (clear) 
            renderBuffer.ClearRenderTarget(context, ClearColor);
        
        return true;
    }

    /// <summary>
    ///     Restarts the render host.
    ///     <para>If HotRestart = true, only recreate buffers, otherwise dispose all resources and call StartD3D.</para>
    /// </summary>
    /// <param name="hotRestart">if set to <c>true</c> [hotRestart].</param>
    protected void Restart(bool hotRestart) {
        Logger.Info("Restart. IsInitialized = {Value0}; HotRestart = {Value1};", IsInitialized, hotRestart);
        if (!IsInitialized) return;
        if (hotRestart) {
            StopRendering();
            DisposeBuffers();
            CreateAndBindBuffers();
            StartRendering();
        } else {
            EndD3D();
            StartD3D((int)Math.Floor(ActualWidth / DpiScale), (int)Math.Floor(ActualHeight / DpiScale));
        }
    }

    protected virtual void OnStartD3D() { }

    /// <summary>
    ///     Creates the and bind buffers.
    /// </summary>
    protected void CreateAndBindBuffers() {
        Logger.Info("CreateAndBindBuffers");
        
        RenderBuffer = CreateRenderBuffer();
        Renderer?.Detach();
        Renderer = null;
        Renderer = CreateRenderer();
        Renderer.Attach(this);
        OnInitializeBuffers(RenderBuffer, Renderer);
    }

    private void RenderBuffer_OnDeviceLost(object? sender, EventArgs e) {
        EndD3D();
        EffectsManager?.DisposeAllResources();
        EffectsManager?.Reinitialize();
    }

    /// <summary>
    ///     Creates the renderer.
    /// </summary>
    /// <returns></returns>
    private IRenderer CreateRenderer() {
        var effectsManager = EffectsManager
                             ?? throw new InvalidOperationException("Effects manager is required to create a renderer.");
        return createRendererFunction?.Invoke(effectsManager) ?? new ImmediateContextRenderer(effectsManager);
    }

    private void RenderBuffer_OnNewBufferCreated(object? sender, Texture2DArgs e) 
        => OnNewRenderTargetTexture?.Invoke(this, e);

    /// <summary>
    ///     Called when [initialize buffers].
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="renderer">The renderer.</param>
    protected virtual void OnInitializeBuffers(DX11RenderBufferProxyBase buffer, IRenderer renderer) 
        => buffer.Initialize((int)ActualWidth, (int)ActualHeight, Msaa);

    /// <summary>
    ///     Attaches the renderable.
    /// </summary>
    /// <param name="deviceResources">The device resources.</param>
    protected virtual void AttachRenderable(IDeviceResources deviceResources) {
        if (!IsInitialized || Viewport == null) return;
        Logger.Info("Attaching renderable");
        if (EnableSharingModelMode && SharedModelContainer != null)
            SharedModelContainer.CurrentRenderHost = this;
        Viewport.Attach(this);
        RenderContext = CreateRenderContext();

        RenderContext2D = CreateRenderContext2D(deviceResources.DeviceContext2D);
    }

    /// <summary>
    ///     Creates the render context.
    /// </summary>
    /// <returns></returns>
    protected virtual RenderContext CreateRenderContext() 
        => new(this);

    /// <summary>
    ///     Creates the render context2 d.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual RenderContext2D CreateRenderContext2D(D2DDeviceContext context) 
        => new(context, this);

    /// <summary>
    ///     Called when [ending d3 d].
    /// </summary>
    protected virtual void OnEndingD3D() { }

    private void OnManagerDisposed(object? sender, EventArgs args) {
        Logger.Trace();
        EndD3D();
    }

    /// <summary>
    ///     Disposes the buffers.
    /// </summary>
    protected virtual void DisposeBuffers() {
        Logger.Trace();
        Renderer?.Detach();
        Renderer = null;
        RenderBuffer = null;
    }

    /// <summary>
    ///     Detaches the renderable.
    /// </summary>
    protected virtual void DetachRenderable() {
        Logger.Info("Detaching renderable");
        RenderContext = null;
        RenderContext2D = null;
        Viewport?.Detach();
    }

    private void Resize(int width, int height, bool dpiChanged) {
        if (Math.Abs(ActualWidth - width * DpiScale) < 1e-6f &&
            Math.Abs(ActualHeight - height * DpiScale) < 1e-6f) 
            return;
        
        ActualWidth = Math.Max(2, width * DpiScale);
        ActualHeight = Math.Max(2, height * DpiScale);
        Logger.Info("Resizing. Width = {Value0}; Height = {Value1};", width, height);
        lock (lockObj) {
            if (IsInitialized) {
                StopRendering();
                if (RenderBuffer is not { } renderBuffer)
                    return;

                var texture = renderBuffer.Resize((int)Math.Floor(ActualWidth),
                                                  (int)Math.Floor(ActualHeight));
                OnNewRenderTargetTexture?.Invoke(this, new Texture2DArgs(texture));
                if (Viewport != null) {
                    var overlay = Viewport.D2DRenderables.FirstOrDefault();
                    if (overlay != null) {
                        if (dpiChanged) {
                            overlay.Detach();
                            overlay.Attach(this);
                        }

                        overlay.InvalidateAll();
                    }
                }

                StartRendering();
            }
        }
    }

    private static bool IsDeviceLost(int hresult) => hresult is 
                                                         DxgiErrorDeviceRemoved or
                                                         DxgiErrorDeviceReset or 
                                                         DxgiErrorDeviceHung or
                                                         DxgiErrorAccessLost;

    private void EffectsManager_DeviceCreated(object? sender, EventArgs e) {
        if (isLoaded && !IsInitialized)
            StartD3D((int)Math.Floor(ActualWidth), (int)Math.Floor(ActualHeight));
    }

    private void EffectsManager_OnInvalidateRenderer(object? sender, EventArgs e) => InvalidateRender();

    public void ReinitializeEffectsManager() {
        lock (lockObj) {
            EffectsManager?.DisposeAllResources();
            EffectsManager?.Reinitialize();
        }
    }

    protected void TriggerSceneGraphUpdated() {
        SceneGraphUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// 
    /// <param name="disposeManagedResources">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to
    ///     release only unmanaged resources.
    /// </param>
    protected override void OnDispose(bool disposeManagedResources) {
        Logger.Info("Disposing");
        if (disposeManagedResources) {
            EffectsManager = null;
            isLoaded = false;
            IsInitialized = false;
            OnNewRenderTargetTexture = null;
            ExceptionOccurred = null;
            StartRenderLoop = null;
            StopRenderLoop = null;
            Rendered = null;
        }

        RenderContext = null;
        RenderContext2D = null;
        DisposeBuffers();
        base.OnDispose(disposeManagedResources);
    }

#region Properties

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    /// <value>
    ///     The unique identifier.
    /// </value>
    public Guid Guid { get; } = Guid.NewGuid();


    /// <summary>
    ///     Gets the render buffer.
    /// </summary>
    /// <value>
    ///     The render buffer.
    /// </value>
    public DX11RenderBufferProxyBase? RenderBuffer {
        get;
        private set {
            var newBuffer = Disposer.SetDispose(ref field, value,
                                              buffer => {
                                                  buffer.OnNewBufferCreated -= RenderBuffer_OnNewBufferCreated;
                                                  buffer.DeviceLost -= RenderBuffer_OnDeviceLost;
                                              });
            
            if (newBuffer && field is not null) {
                field.OnNewBufferCreated += RenderBuffer_OnNewBufferCreated;
                field.DeviceLost += RenderBuffer_OnDeviceLost;
            }
        }
    }

    /// <summary>
    ///     Gets the device.
    /// </summary>
    /// <value>
    ///     The device.
    /// </value>
    public SilkD3DDevice? Device => EffectsManager?.NativeDeviceResources.Device;

    /// <summary>
    ///     Gets the immediate device context.
    /// </summary>
    /// <value>
    ///     The immediate device context.
    /// </value>
    public DeviceContextProxy? ImmediateDeviceContext {
        get;
        private set {
            if(field != value)
                field?.Dispose();
            field = value;
        }
    }

    /// <summary>
    ///     Gets the device2d.
    /// </summary>
    /// <value>
    ///     The device2d.
    /// </value>
    public D2DDevice? Device2D => EffectsManager?.Device2D;

    /// <summary>
    ///     Gets or sets the color of the clear.
    /// </summary>
    /// <value>
    ///     The color of the clear.
    /// </value>
    public Color4 ClearColor {
        get;
        set {
            field = value;
            InvalidateRender();
        }
    } = Color.White;

    /// <summary>
    ///     Gets or sets a value indicating whether shadow map enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is shadow map enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsShadowMapEnabled {
        get;
        set {
            field = value;
            InvalidateRender();
        }
    }

    /// <summary>
    ///     Gets or sets the Multi-Sampling-Anti-Alias.
    /// </summary>
    /// <value>
    ///     The msaa.
    /// </value>
    public MsaaLevel Msaa {
        get;
        set {
            if (Set(ref field, value)) Restart(true);
        }
    } = MsaaLevel.Disable;

    /// <summary>
    ///     <see cref="IRenderHost.Viewport" />
    /// </summary>
    public IViewport3DX? Viewport {
        get;
        set {
            if (field == value) 
                return;
            
            Logger.Info("Set Viewport, Initialized = {Value0}", IsInitialized);
            DetachRenderable();
            field = value;
            
            if (IsInitialized && EffectsManager is { } effectsManager)
                AttachRenderable(effectsManager);
        }
    }


    /// <summary>
    /// </summary>
    public RenderContext? RenderContext { 
        get;
        private set => Disposer.SetDispose(ref field, value);
    }

    /// <summary>
    ///     Gets the render context2d.
    /// </summary>
    /// <value>
    ///     The render context2d.
    /// </value>
    public RenderContext2D? RenderContext2D {
        get;
        set => Disposer.SetDispose(ref field, value);
    }

    private IEffectsManager? effectsManager;

    /// <summary>
    ///     Gets or sets the effects manager.
    /// </summary>
    /// <value>
    ///     The effects manager.
    /// </value>
    public IEffectsManager? EffectsManager {
        get => effectsManager;
        set {
            var currentManager = effectsManager;
            if (Set(ref effectsManager, value)) {
                EffectsManagerChanged?.Invoke(this, value);
                Logger.Info("Set new EffectsManager");
                if (currentManager != null) {
                    currentManager.DisposingResources -= OnManagerDisposed;
                    currentManager.Reinitialized -= EffectsManager_DeviceCreated;
                    currentManager.InvalidateRender -= EffectsManager_OnInvalidateRenderer;
                }

                ImmediateDeviceContext = null;
                if (effectsManager != null) {
                    effectsManager.DisposingResources += OnManagerDisposed;
                    effectsManager.InvalidateRender += EffectsManager_OnInvalidateRenderer;
                    effectsManager.Reinitialized += EffectsManager_DeviceCreated;
                    FeatureLevel = effectsManager.NativeDeviceResources.FeatureLevel.ToFeatureLevel();
                    if (IsInitialized)
                        Restart(false);
                    else if (isLoaded) 
                        StartD3D((int)Math.Floor(ActualWidth), (int)Math.Floor(ActualHeight));
                } else {
                    RenderTechnique = null;
                    EndD3D();
                }
            }
        }
    }

    /// <summary>
    ///     Gets or sets the render technique.
    /// </summary>
    /// <value>
    ///     The render technique.
    /// </value>
    public IRenderTechnique? RenderTechnique {
        get;
        set {
            if (Set(ref field, value) && IsInitialized) 
                Restart(false);
        }
    }

    /// <summary>
    ///     Gets a value indicating whether this instance is deferred lighting.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is deferred lighting; otherwise, <c>false</c>.
    /// </value>
    public bool IsDeferredLighting => false;

    /// <summary>
    ///     Gets or sets the actual height.
    /// </summary>
    /// <value>
    ///     The actual height.
    /// </value>
    public float ActualHeight {
        get;
        private set => field = Math.Max(MinHeight, value);
    } = MinHeight;

    /// <summary>
    ///     Gets or sets the actual width.
    /// </summary>
    /// <value>
    ///     The actual width.
    /// </value>
    public float ActualWidth {
        get;
        private set => field = Math.Max(MinWidth, value);
    } = MinWidth;

    public float DpiScale {
        get;
        set {
            var oldDpiScale = field;
            if (Set(ref field, value))
                Resize((int) (ActualWidth / oldDpiScale), (int) (ActualHeight / oldDpiScale), true);
        }
    } = 1;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is busy.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is busy; otherwise, <c>false</c>.
    /// </value>
    public bool IsBusy { get; private set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable render frustum].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable render frustum]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableRenderFrustum {
        get;
        set {
            if (field == value) return;
            field = value;
            FrustumEnabledChanged?.Invoke(this, value ? BoolArgs.TrueArgs : BoolArgs.FalseArgs);
        }
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [enable sharing model mode].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable sharing model mode]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableSharingModelMode { get; set; }

    /// <summary>
    ///     Gets or sets the shared model container.
    /// </summary>
    /// <value>
    ///     The shared model container.
    /// </value>
    public IModelContainer? SharedModelContainer { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is rendering.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is rendering; otherwise, <c>false</c>.
    /// </value>
    public bool IsRendering { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is initialized.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is initialized; otherwise, <c>false</c>.
    /// </value>
    public bool IsInitialized { get; private set; }

    private bool isLoaded;

    /// <summary>
    ///     Gets the color buffer view.
    /// </summary>
    /// <value>
    ///     The color buffer view.
    /// </value>
    public RenderTargetView? RenderTargetBufferView => RenderBuffer?.ColorBuffer;

    /// <summary>
    ///     Gets the depth stencil buffer view.
    /// </summary>
    /// <value>
    ///     The depth stencil buffer view.
    /// </value>
    public DepthStencilView? DepthStencilBufferView => RenderBuffer?.DepthStencilBuffer;

    /// <summary>
    ///     Gets the d2d controls.
    /// </summary>
    /// <value>
    ///     The d2 d controls.
    /// </value>
    public D2DTargetProxy? D2DTarget => RenderBuffer?.D2DTarget;

    /// <summary>
    ///     Gets the render statistics.
    /// </summary>
    /// <value>
    ///     The render statistics.
    /// </value>
    public IRenderStatistics RenderStatistics => renderStatistics;

    protected readonly RenderStatistics renderStatistics = new();

#region Perframe renderables

    /// <summary>
    ///     Gets the current frame renderables for rendering.
    /// </summary>
    /// <value>
    ///     The per frame renderable.
    /// </value>
    public abstract FastList<(int Key, SceneNode Value)> PerFrameFlattenedScene { get; }

    /// <summary>
    ///     Gets the per frame lights.
    /// </summary>
    /// <value>
    ///     The per frame lights.
    /// </value>
    public abstract IEnumerable<LightNode> PerFrameLights { get; }

    /// <summary>
    ///     Gets the post effects render cores for this frame
    /// </summary>
    /// <value>
    ///     The post effects render cores.
    /// </value>
    public abstract FastList<SceneNode> PerFrameNodesWithPostEffect { get; }

    /// <summary>
    ///     Gets the per frame render cores.
    /// </summary>
    /// <value>
    ///     The per frame render cores.
    /// </value>
    public abstract FastList<SceneNode> PerFrameOpaqueNodes { get; }

    /// <summary>
    ///     Gets the per frame opaque nodes in frustum.
    /// </summary>
    /// <value>
    ///     The per frame opaque nodes in frustum.
    /// </value>
    public abstract FastList<SceneNode> PerFrameOpaqueNodesInFrustum { get; }

    /// <summary>
    ///     Gets the per frame transparent node in frustum.
    /// </summary>
    /// <value>
    ///     The per frame transparent node in frustum.
    /// </value>
    public abstract FastList<SceneNode> PerFrameTransparentNodesInFrustum { get; }

    /// <summary>
    ///     Gets the per frame transparent nodes.
    /// </summary>
    /// <value>
    ///     The per frame transparent nodes.
    /// </value>
    public abstract FastList<SceneNode> PerFrameParticleNodes { get; }

    /// <summary>
    ///     Gets the per frame transparent nodes.
    /// </summary>
    /// <value>
    ///     The per frame transparent nodes.
    /// </value>
    public abstract FastList<SceneNode> PerFrameTransparentNodes { get; }

#endregion

#region Configuration

    /// <summary>
    ///     Gets or sets a value indicating whether [show render statistics].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [show render statistics]; otherwise, <c>false</c>.
    /// </value>
    public RenderDetail ShowRenderDetail {
        get => RenderStatistics.FrameDetail;
        set {
            if (RenderStatistics.FrameDetail != value) {
                RenderStatistics.FrameDetail = value;
                InvalidateRender();
            }
        }
    }

    public DX11RenderHostConfiguration RenderConfiguration { get; set; } = new() {
        UpdatePerFrameData = true,
        RenderD2D = true,
        RenderLights = true,
        ClearEachFrame = true,
        OitRenderType = OitRenderType.DepthPeeling
    };

    /// <summary>
    ///     Gets the feature level.
    /// </summary>
    /// <value>
    ///     The feature level.
    /// </value>
    public FeatureLevel FeatureLevel { get; private set; } = FeatureLevel.Level110;

    public bool EnableParallelProcessing { get; set; } = true;

#endregion

#endregion

#region Events

    /// <summary>
    ///     Occurs when [exception occurred].
    /// </summary>
    public event EventHandler<RelayExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    ///     Occurs when [start render loop].
    /// </summary>
    public event EventHandler<EventArgs>? StartRenderLoop;

    /// <summary>
    ///     Occurs when [stop render loop].
    /// </summary>
    public event EventHandler<EventArgs>? StopRenderLoop;

    /// <summary>
    ///     Occurs when [on new render target texture].
    /// </summary>
    public event EventHandler<Texture2DArgs>? OnNewRenderTargetTexture;

    /// <summary>
    ///     Occurs when each render frame finished rendering.
    /// </summary>
    public event EventHandler? Rendered;

    private readonly Func<IDevice3DResources, IRenderer>? createRendererFunction;

    public event EventHandler<BoolArgs>? FrustumEnabledChanged;

    public event EventHandler? SceneGraphUpdated;

    public event EventHandler<IEffectsManager?>? EffectsManagerChanged;

#endregion

#region Private variables



    /// <summary>
    ///     The renderer
    /// </summary>
    public IRenderer? Renderer {
        get;
        private set => Disposer.SetDispose(ref field, value);
    }

    /// <summary>
    ///     The update requested
    /// </summary>
    protected volatile bool UpdateRequested = true;

    private TimeSpan lastRenderingDuration = TimeSpan.Zero;

    private TimeSpan lastRenderTime = TimeSpan.Zero;

    /// Used to render at least twice. D3DImage sometimes not getting refresh if only render once.
    private uint updateCounter;

    private volatile bool updateSceneGraphRequested = true;

    private volatile bool updatePerFrameRenderableRequested = true;

    private readonly Lock lockObj = new();

#endregion
}
