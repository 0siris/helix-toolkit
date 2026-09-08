// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoAutomationHost.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using Rect3D = System.Windows.Media.Media3D.Rect3D;

/// <summary>
///     In-process automation host for a single demo viewport. All viewport access runs on the UI dispatcher;
///     REST and MCP arrive on Kestrel thread-pool threads.
/// </summary>
public sealed class DemoAutomationHost : IAsyncDisposable {
    /// <summary>
    ///     The automated viewport. Lifetime equals the demo window lifetime.
    /// </summary>
    private readonly Viewport3DX viewport;

    /// <summary>
    ///     The effects manager used to attach loaded scenes.
    /// </summary>
    private readonly IEffectsManager effectsManager;

    /// <summary>
    ///     The optional scene adapter. Null disables model and scene operations.
    /// </summary>
    private readonly IDemoSceneHost? sceneHost;

    /// <summary>
    ///     The running HTTP server. Null before <see cref="StartAsync" />.
    /// </summary>
    private DemoHttpServer? server;

    /// <summary>
    ///     Frame-rate accumulator fed by WPF rendering events.
    /// </summary>
    private readonly FrameRateMeter frameMeter = new();

    /// <summary>
    ///     Captured log buffer installed as the process logger.
    /// </summary>
    private readonly RingBufferLogger logBuffer;

    /// <summary>
    ///     Previous process logger, restored on dispose.
    /// </summary>
    private readonly LoggerLib.ILog previousLogger;

    /// <summary>
    ///     Whether <see cref="DisposeAsync" /> already ran.
    /// </summary>
    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DemoAutomationHost" /> class.
    /// </summary>
    /// <param name="viewport">The viewport to automate.</param>
    /// <param name="effectsManager">The effects manager.</param>
    /// <param name="options">The options.</param>
    /// <param name="sceneHost">The optional scene adapter.</param>
    /// <param name="demoName">The demo name. Defaults to the entry assembly name.</param>
    public DemoAutomationHost(
        Viewport3DX viewport,
        IEffectsManager effectsManager,
        DemoAutomationOptions? options = null,
        IDemoSceneHost? sceneHost = null,
        string? demoName = null) {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(effectsManager);
        this.viewport = viewport;
        this.effectsManager = effectsManager;
        Options = options ?? new DemoAutomationOptions();
        this.sceneHost = sceneHost;
        DemoName = demoName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "Demo";
        previousLogger = LoggerLib.Logger.Current;
        logBuffer = new RingBufferLogger(previousLogger);
        LoggerLib.Logger.Use(logBuffer);
        CompositionTarget.Rendering += OnRendering;
    }

    /// <summary>
    ///     Gets the host options.
    /// </summary>
    public DemoAutomationOptions Options { get; }

    /// <summary>
    ///     Gets the demo name reported by status endpoints.
    /// </summary>
    public string DemoName { get; }

    /// <summary>
    ///     Gets whether the HTTP server is running.
    /// </summary>
    public bool IsActive => server is not null;

    /// <summary>
    ///     Gets the server URL, or null before <see cref="StartAsync" />.
    /// </summary>
    public string? Url => server?.Url;

    /// <summary>
    ///     Gets whether this host exposes model and scene operations.
    /// </summary>
    public bool HasSceneHost => sceneHost is not null;

    /// <summary>
    ///     Starts the HTTP server with REST routes and, if enabled, the MCP endpoint.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task tracking the start.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (server is not null) {
            return;
        }

        if (Application.Current is null) {
            throw new InvalidOperationException("Demo automation requires a running WPF application.");
        }

        server = await DemoHttpServer.StartAsync(this, Options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Stops the HTTP server. Viewport automation stays usable in-process.
    /// </summary>
    /// <returns>A task tracking the stop.</returns>
    public async Task StopAsync() {
        if (server is { } running) {
            server = null;
            await running.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (disposed) {
            return;
        }

        disposed = true;
        CompositionTarget.Rendering -= OnRendering;
        if (ReferenceEquals(LoggerLib.Logger.Current, logBuffer)) {
            LoggerLib.Logger.Use(previousLogger);
        }

        await StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Feeds render timestamps to the frame-rate meter.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args.</param>
    private void OnRendering(object? sender, EventArgs e) => frameMeter.Tick(Stopwatch.GetTimestamp());

    /// <summary>
    ///     Gets the current camera state on the UI thread.
    /// </summary>
    /// <returns>The camera state.</returns>
    public Task<CameraState> GetCameraAsync() => OnUiAsync(() => CameraState.FromCamera(viewport.Camera));

    /// <summary>
    ///     Applies a camera patch on the UI thread and returns the resulting state.
    /// </summary>
    /// <param name="patch">The patch. Null fields keep current values.</param>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>The resulting camera state.</returns>
    public Task<CameraState> SetCameraAsync(CameraState patch, double animationTimeMs = 0) =>
        OnUiAsync(() => {
            ArgumentNullException.ThrowIfNull(patch);
            var applied = patch.ApplyTo(viewport.Camera, animationTimeMs);
            if (!ReferenceEquals(applied, viewport.Camera)) {
                viewport.Camera = applied;
            }

            return CameraState.FromCamera(viewport.Camera);
        });

    /// <summary>
    ///     Zooms to the scene extents on the UI thread.
    /// </summary>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>A task tracking the operation.</returns>
    public Task ZoomExtentsAsync(double animationTimeMs = 0) =>
        OnUiAsync(() => viewport.ZoomExtents(animationTimeMs));

    /// <summary>
    ///     Resets the view on the UI thread (default camera, else camera reset plus zoom extents).
    /// </summary>
    /// <returns>A task tracking the operation.</returns>
    public Task ResetCameraAsync() => OnUiAsync(() => viewport.Reset());

    /// <summary>
    ///     Renders the viewport to a PNG on the UI thread.
    /// </summary>
    /// <param name="width">Optional explicit width. Null keeps the window size.</param>
    /// <param name="height">Optional explicit height. Null keeps the window size.</param>
    /// <returns>The rendered image.</returns>
    public Task<RenderedImage> RenderPngAsync(int? width, int? height) =>
        OnUiAsync(() => {
            var resized = width.HasValue || height.HasValue;
            var originalWidth = viewport.Width;
            var originalHeight = viewport.Height;
            if (width.HasValue ^ height.HasValue) {
                throw new ArgumentException("Width and height must be specified together.");
            }

            try {
                if (width.HasValue && height.HasValue) {
                    viewport.ResizeAndArrange(width.Value, height.Value);
                }

                var bitmap = viewport.RenderBitmap()
                    ?? throw new InvalidOperationException("The Direct3D 12 presentation surface is unavailable.");
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                return new RenderedImage(stream.ToArray(), bitmap.PixelWidth, bitmap.PixelHeight);
            } finally {
                if (resized) {
                    viewport.Width = originalWidth;
                    viewport.Height = originalHeight;
                    viewport.UpdateLayout();
                }
            }
        });

    /// <summary>
    ///     Gets scene statistics on the UI thread.
    /// </summary>
    /// <returns>The statistics.</returns>
    public Task<SceneStats> GetStatsAsync() =>
        OnUiAsync(() => {
            var bounds = viewport.FindBounds3D();
            return new SceneStats(
                viewport.GetTotalNumberOfTriangles(),
                [bounds.X, bounds.Y, bounds.Z],
                [bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, bounds.Z + bounds.SizeZ],
                viewport.Renderables.Count());
        });

    /// <summary>
    ///     Gets the demo status on the UI thread.
    /// </summary>
    /// <returns>The status.</returns>
    public Task<DemoStatus> GetStatusAsync() =>
        OnUiAsync(() => new DemoStatus(
            DemoName,
            viewport.ActualWidth,
            viewport.ActualHeight,
            viewport.Camera is HelixToolkit.Wpf.SharpDX.Camera.OrthographicCamera
                ? CameraState.Orthographic
                : CameraState.Perspective,
            viewport.GetTotalNumberOfTriangles(),
            Options.EnableMcp));

    /// <summary>
    ///     Clears the scene. Requires a scene host.
    /// </summary>
    /// <returns>A task tracking the operation.</returns>
    public Task ClearSceneAsync() {
        if (sceneHost is null) {
            throw new NoSceneHostException();
        }

        return OnUiAsync(sceneHost.Clear);
    }

    /// <summary>
    ///     Gets render info on the UI thread.
    /// </summary>
    /// <returns>The render info.</returns>
    public Task<RenderInfo> GetRenderInfoAsync() =>
        OnUiAsync(() => new RenderInfo(
            frameMeter.Fps,
            frameMeter.FrameMs,
            viewport.GetTotalNumberOfTriangles(),
            viewport.RenderTechnique?.Name,
            [.. effectsManager.RenderTechniques]));

    /// <summary>
    ///     Sets the viewport render technique by name on the UI thread.
    /// </summary>
    /// <param name="name">The technique name.</param>
    /// <returns>The resulting render info.</returns>
    public Task<RenderInfo> SetRenderTechniqueAsync(string name) =>
        OnUiAsync(() => {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!effectsManager.HasTechnique(name)) {
                throw new ArgumentException($"unknown-technique: '{name}'.", nameof(name));
            }

            viewport.RenderTechnique = effectsManager[name];
            return new RenderInfo(
                frameMeter.Fps,
                frameMeter.FrameMs,
                viewport.GetTotalNumberOfTriangles(),
                viewport.RenderTechnique?.Name,
                [.. effectsManager.RenderTechniques]);
        });

    /// <summary>
    ///     Gets captured log entries, newest first.
    /// </summary>
    /// <param name="level">Optional minimum level string.</param>
    /// <param name="limit">Maximum entry count (1-1000).</param>
    /// <returns>The entries.</returns>
    public Task<IReadOnlyList<LogEntry>> GetLogsAsync(string? level = null, int limit = 100) {
        if (limit is < 1 or > 1000) {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be within [1, 1000].");
        }

        return Task.FromResult(LogQuery.Apply(logBuffer.Snapshot(), level, limit));
    }

    /// <summary>
    ///     Sets the process log level.
    /// </summary>
    /// <param name="level">verbose|debug|info|warn|error|fatal.</param>
    /// <returns>The normalized level.</returns>
    public Task<string> SetLogLevelAsync(string level) {
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        LoggerLib.Logger.SwitchMinimumLevel(LogQuery.ParseLevel(level));
        return Task.FromResult(level.ToLowerInvariant());
    }


    /// <summary>
    ///     Gets the scene tree on the UI thread.
    /// </summary>
    /// <param name="depth">Maximum nesting depth (1-12).</param>
    /// <param name="limit">Maximum node count (1-2000).</param>
    /// <returns>The root infos.</returns>
    public Task<List<SceneNodeInfo>> GetSceneTreeAsync(int depth = 6, int limit = 500) {
        if (depth is < 1 or > 12) {
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be within [1, 12].");
        }

        if (limit is < 1 or > 2000) {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be within [1, 2000].");
        }

        return OnUiAsync(() => SceneTreeBuilder.Build(Roots, depth, limit));
    }

    /// <summary>
    ///     Sets node visibility on the UI thread.
    /// </summary>
    /// <param name="id">The node id.</param>
    /// <param name="visible">The visibility.</param>
    /// <returns>The node summary.</returns>
    public Task<SceneNodeInfo> SetNodeVisibilityAsync(Guid id, bool visible) =>
        OnUiAsync(() => {
            var node = FindNode(id);
            node.Visible = visible;
            return SceneTreeBuilder.Summarize(node);
        });

    /// <summary>
    ///     Zooms to a node subtree on the UI thread.
    /// </summary>
    /// <param name="id">The node id.</param>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>The node summary.</returns>
    public Task<SceneNodeInfo> FocusNodeAsync(Guid id, double animationTimeMs = 0) =>
        OnUiAsync(() => {
            var node = FindNode(id);
            if (!SceneNodeExtensions.TryGetBound(node, out var box)) {
                throw new NoNodeBoundException(id);
            }

            var size = box.Maximum - box.Minimum;
            viewport.ZoomExtents(
                new Rect3D(box.Minimum.X, box.Minimum.Y, box.Minimum.Z, size.X, size.Y, size.Z),
                animationTimeMs);
            return SceneTreeBuilder.Summarize(node);
        });

    /// <summary>
    ///     Picks the scene at viewport coordinates on the UI thread.
    /// </summary>
    /// <param name="x">Viewport-relative x in device-independent units.</param>
    /// <param name="y">Viewport-relative y in device-independent units.</param>
    /// <param name="maxHits">Maximum hit count (1-20).</param>
    /// <returns>Nearest-first hits.</returns>
    public Task<List<PickHit>> PickAsync(double x, double y, int maxHits = 5) {
        if (maxHits is < 1 or > 20) {
            throw new ArgumentOutOfRangeException(nameof(maxHits), "MaxHits must be within [1, 20].");
        }

        return OnUiAsync(() =>
            viewport.FindHits(new Point(x, y)).Take(maxHits).Select(hit => {
                var (nodeId, nodeName, modelType) = Identify(hit.ModelHit);
                return new PickHit(
                    nodeId,
                    nodeName,
                    modelType,
                    hit.Distance,
                    [hit.PointHit.X, hit.PointHit.Y, hit.PointHit.Z],
                    [hit.NormalAtHit.X, hit.NormalAtHit.Y, hit.NormalAtHit.Z],
                    hit.TriangleIndices is { } triangle
                        ? [triangle.Item1, triangle.Item2, triangle.Item3]
                        : null);
            }).ToList());
    }

    /// <summary>
    ///     Loads a model file on a worker thread and swaps it into the scene on the UI thread.
    ///     Requires a scene host.
    /// </summary>
    /// <param name="path">Absolute or demo-relative model file path.</param>
    /// <returns>The load result.</returns>
    public async Task<ModelLoadResult> LoadModelAsync(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (sceneHost is null) {
            throw new NoSceneHostException();
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Model file '{fullPath}' was not found.", fullPath);
        }

        var scene = await Task.Run(() => new Importer().Load(fullPath)).ConfigureAwait(false);
        if (scene is null) {
            throw new ModelLoadException($"The file '{fullPath}' did not contain a scene.");
        }

        scene.Root.Attach(effectsManager);
        SceneNodeExtensions.UpdateAllTransformMatrix(scene.Root);
        await OnUiAsync(() => {
            sceneHost.Clear();
            sceneHost.Add(scene.Root);
        }).ConfigureAwait(false);
        var stats = await GetStatsAsync().ConfigureAwait(false);
        return new ModelLoadResult(System.IO.Path.GetFileName(fullPath), stats.TriangleCount, stats.BoundsMin, stats.BoundsMax);
    }

    /// <summary>
    ///     Gets the traversal roots: the scene host group when present, else the viewport renderables.
    /// </summary>
    private IEnumerable<SceneNode> Roots => sceneHost?.GetRoots() ?? viewport.Renderables;

    /// <summary>
    ///     Finds a node by id. Runs on the UI thread.
    /// </summary>
    /// <param name="id">The node id.</param>
    /// <returns>The node.</returns>
    private SceneNode FindNode(Guid id) =>
        SceneTreeBuilder.Find(Roots, id) ?? throw new KeyNotFoundException($"Scene node '{id}' was not found.");

    /// <summary>
    ///     Unwraps a pick hit to node identity.
    /// </summary>
    /// <param name="modelHit">The hit model.</param>
    /// <returns>Node id, name, and model type.</returns>
    private static (Guid? Id, string? Name, string? Type) Identify(object? modelHit) =>
        modelHit switch {
            Element3D element when element.SceneNode is { } node => (node.Guid, node.Name, node.GetType().Name),
            SceneNode node => (node.Guid, node.Name, node.GetType().Name),
            _ => (null, null, null),
        };

    /// <summary>
    ///     Runs a function on the viewport dispatcher.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function.</param>
    /// <returns>The function result.</returns>
    private Task<T> OnUiAsync<T>(Func<T> func) => viewport.Dispatcher.InvokeAsync(func).Task;

    /// <summary>
    ///     Runs an action on the viewport dispatcher.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>A task tracking the action.</returns>
    private Task OnUiAsync(Action action) => viewport.Dispatcher.InvokeAsync(action).Task;
}
