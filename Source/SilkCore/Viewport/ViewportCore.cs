/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core;

public partial class ViewportCore : DisposeObject, IViewport3DX {
    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewportCore" /> class.
    /// </summary>
    /// <param name="nativeWindowPointer">The native window pointer.</param>
    /// <param name="deferred">if set to <c>true</c> [deferred].</param>
    public ViewportCore(nint nativeWindowPointer, bool deferred = false)
        : this(deferred
                   ? new SwapChainRenderHost(nativeWindowPointer, device => new DeferredContextRenderer(device, new AutoRenderTaskScheduler()))
                   : new SwapChainRenderHost(nativeWindowPointer)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewportCore" /> class.
    /// </summary>
    public ViewportCore() : this(new DefaultRenderHost()) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewportCore" /> class.
    /// </summary>
    /// <param name="renderHost"></param>
    public ViewportCore(IRenderHost renderHost) {
        RenderHost = renderHost;
        RenderHost.Viewport = this;
        RenderHost.DpiScale = (float)DpiScale;
        BackgroundColor = Color.Black;
        RenderHost.StartRenderLoop += RenderHost_StartRenderLoop;
        RenderHost.StopRenderLoop += RenderHost_StopRenderLoop;
        RenderHost.ExceptionOccurred += (s, e) => HandleExceptionOccured(e.Exception);
        Items2D.ItemsInternal.Add(frameStatisticsNode);
    }

    /// <summary>
    ///     Attaches the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    public void Attach(IRenderHost host) {
        Items.Attach(host.EffectsManager);
        Items.Invalidated += Items_Invalidated;

        if (ViewCube.Visible) {
            ViewCube.Attach(host.EffectsManager);
            ViewCube.Invalidated += Items_Invalidated;
            ViewCube.RenderHost = host;
        }
        
        CoordinateSystem.Attach(host.EffectsManager);
        CoordinateSystem.Invalidated += Items_Invalidated;

        Items2D.Attach(host);

        Items.RenderHost = host;
        CoordinateSystem.RenderHost = host;
    }

    /// <summary>
    ///     Detaches this instance.
    /// </summary>
    public void Detach() {
        Items.Invalidated -= Items_Invalidated;
        Items.Detach();

        if (ViewCube.IsAttached) {
            ViewCube.Invalidated -= Items_Invalidated;
            ViewCube.Detach();
        }

        CoordinateSystem.Invalidated -= Items_Invalidated;
        CoordinateSystem.Detach();

        Items2D.Detach();

        Items.RenderHost = null;
        ViewCube.RenderHost = null;
        CoordinateSystem.RenderHost = null;
    }

    /// <summary>
    ///     Invalidates the render.
    /// </summary>
    public void InvalidateRender() 
        => RenderHost.InvalidateRender();

    /// <summary>
    ///     Invalidates the scene graph.
    /// </summary>
    public void InvalidateSceneGraph() 
        => RenderHost.InvalidateSceneGraph();

    /// <summary>
    /// </summary>
    /// <param name="timeStamp"></param>
    public void Update(TimeSpan timeStamp) { }

    /// <summary>
    ///     Renders this instance.
    /// </summary>
    public void Render() 
        => RenderHost.UpdateAndRender();

    /// <summary>
    ///     Starts the d3 d.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void StartD3D(int width, int height) 
        => RenderHost.StartD3D(width, height);

    /// <summary>
    ///     Ends the d3 d.
    /// </summary>
    public void EndD3D() 
        => RenderHost.EndD3D();

    private void Items_Invalidated(object? sender, InvalidateTypes e) 
        => RenderHost.Invalidate(e);

    /// <summary>
    ///     Mouses down.
    /// </summary>
    /// <param name="position">The position.</param>
    public void MouseDown(Vector2 position) {
        currentNode = null;
        hits.Clear();
        if (!this.UnProject(position, out var ray)) { } 
        else if (ViewCubeHitTest(ref ray, ref position)) { } 
        else if (this.FindHits(position, ref hits) && hits.Count > 0 && hits[0].ModelHit is SceneNode node) {
            currentNode = node;
            currentNode.RaiseMouseDownEvent(this, position, hits[0]);
            NodeHitOnMouseDown?.Invoke(this, new SceneNodeMouseDownArgs(this, position, currentNode, hits[0]));
        }
    }

    /// <summary>
    ///     Mouses the move.
    /// </summary>
    /// <param name="position">The position.</param>
    public void MouseMove(Vector2 position) {
        if (currentNode != null && hits.Count > 0) {
            currentNode.RaiseMouseMoveEvent(this, position, hits[0]);
            NodeHitOnMouseMove?.Invoke(this, new SceneNodeMouseMoveArgs(this, position, currentNode, hits[0]));
        }
    }

    /// <summary>
    ///     Mouses up.
    /// </summary>
    /// <param name="position">The position.</param>
    public void MouseUp(Vector2 position) {
        if (currentNode != null && hits.Count > 0) {
            currentNode.RaiseMouseUpEvent(this, position, hits[0]);
            NodeHitOnMouseUp?.Invoke(this, new SceneNodeMouseUpArgs(this, position, currentNode, hits[0]));
        }

        hits.Clear();
        currentNode = null;
    }

    /// <summary>
    ///     Resizes the specified width.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void Resize(int width, int height) {
        ActualWidth = width;
        ActualHeight = height;
        RenderHost.Resize(width, height);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        Detach();
        if (disposeManagedResources) 
            Items.Dispose();
        base.OnDispose(disposeManagedResources);
    }

    #region Private Methods

    private void HandleExceptionOccured(Exception exception) 
        => ErrorOccurred?.Invoke(this, exception);

    private void RenderHost_StopRenderLoop(object? sender, EventArgs e) 
        => StopRendering?.Invoke(this, EventArgs.Empty);

    private void RenderHost_StartRenderLoop(object? sender, EventArgs e) 
        => StartRendering?.Invoke(this, EventArgs.Empty);

    private bool ViewCubeHitTest(ref Ray ray, ref Vector2 position) {
        if (RenderContext is not { } renderContext)
            return false;

        var hitContext = new HitTestContext(renderContext, ray, position);
        if (ViewCube.HitTest(hitContext, ref hits)) {
            ViewCube.RaiseMouseDownEvent(this, position, hits[0]);
            var normal = hits[0].NormalAtHit;
            if (SilkMath.Cross(normal, ModelUpDirection).LengthSquared() < 1e-5) {
                var vecLeft = new Vector3(-normal.Y, -normal.Z, -normal.X);
                ViewCubeClicked(hits[0].NormalAtHit, vecLeft);
            } else {
                ViewCubeClicked(hits[0].NormalAtHit, ModelUpDirection);
            }

            return true;
        }

        return false;
    }

    private void ViewCubeClicked(Vector3 lookDirection, Vector3 upDirection) {
        if (CameraCore is not { } camera)
            return;

        var target = camera.Position + camera.LookDirection;
        var distance = camera.LookDirection.Length;
        lookDirection *= distance;
        var newPosition = target - lookDirection;
        camera.AnimateTo(newPosition, lookDirection, upDirection, 500);
    }

    #endregion
}
