// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoMcpTools.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.ComponentModel;
using System.IO;
using ModelContextProtocol.Server;

/// <summary>
///     MCP tools over the automation host. Known failures surface as <c>{ error }</c> payloads.
/// </summary>
[McpServerToolType]
public sealed class DemoMcpTools {
    /// <summary>
    ///     The automation host.
    /// </summary>
    private readonly DemoAutomationHost host;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DemoMcpTools" /> class.
    /// </summary>
    /// <param name="host">The automation host.</param>
    public DemoMcpTools(DemoAutomationHost host) {
        ArgumentNullException.ThrowIfNull(host);
        this.host = host;
    }

    /// <summary>
    ///     Gets the demo status.
    /// </summary>
    /// <returns>The status or an error payload.</returns>
    [McpServerTool, Description("Live demo status: viewport size, camera type, triangle count.")]
    public async Task<object> DemoStatus() => await GuardAsync(host.GetStatusAsync).ConfigureAwait(false);

    /// <summary>
    ///     Gets the camera state.
    /// </summary>
    /// <returns>The camera state or an error payload.</returns>
    [McpServerTool(Name = "camera_get"), Description("Current viewport camera state.")]
    public async Task<object> CameraGet() => await GuardAsync(host.GetCameraAsync).ConfigureAwait(false);

    /// <summary>
    ///     Sets the camera. Null arguments keep current values.
    /// </summary>
    /// <param name="position">Position as [x, y, z].</param>
    /// <param name="lookDirection">Look direction as [x, y, z].</param>
    /// <param name="upDirection">Up direction as [x, y, z].</param>
    /// <param name="fieldOfView">Field of view in degrees (perspective).</param>
    /// <param name="width">Viewport width (orthographic).</param>
    /// <param name="nearPlane">Near plane distance.</param>
    /// <param name="farPlane">Far plane distance.</param>
    /// <param name="cameraType">perspective or orthographic; switches the camera.</param>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>The resulting camera state or an error payload.</returns>
    [McpServerTool(Name = "camera_set"), Description("Move the viewport camera; omitted fields keep current values.")]
    public async Task<object> CameraSet(
        double[]? position = null,
        double[]? lookDirection = null,
        double[]? upDirection = null,
        double? fieldOfView = null,
        double? width = null,
        double? nearPlane = null,
        double? farPlane = null,
        string? cameraType = null,
        double animationTimeMs = 0) =>
        await GuardAsync(() => host.SetCameraAsync(
            new CameraState {
                Position = position,
                LookDirection = lookDirection,
                UpDirection = upDirection,
                FieldOfView = fieldOfView,
                Width = width,
                NearPlane = nearPlane,
                FarPlane = farPlane,
                CameraType = cameraType,
            },
            animationTimeMs)).ConfigureAwait(false);

    /// <summary>
    ///     Zooms to the scene extents.
    /// </summary>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>ok or an error payload.</returns>
    [McpServerTool(Name = "camera_zoom_extents"), Description("Zoom the viewport camera to the scene extents.")]
    public async Task<object> CameraZoomExtents(double animationTimeMs = 0) =>
        await GuardAsync<object>(async () => {
            await host.ZoomExtentsAsync(animationTimeMs).ConfigureAwait(false);
            return new { ok = true };
        }).ConfigureAwait(false);

    /// <summary>
    ///     Resets the view.
    /// </summary>
    /// <returns>ok or an error payload.</returns>
    [McpServerTool(Name = "camera_reset"), Description("Reset the viewport to its default camera.")]
    public async Task<object> CameraReset() =>
        await GuardAsync<object>(async () => {
            await host.ResetCameraAsync().ConfigureAwait(false);
            return new { ok = true };
        }).ConfigureAwait(false);

    /// <summary>
    ///     Gets scene statistics.
    /// </summary>
    /// <returns>The statistics or an error payload.</returns>
    [McpServerTool(Name = "scene_stats"), Description("Scene statistics: triangles, bounds, renderable count.")]
    public async Task<object> SceneStats() => await GuardAsync(host.GetStatsAsync).ConfigureAwait(false);

    /// <summary>
    ///     Clears the scene.
    /// </summary>
    /// <returns>ok or an error payload.</returns>
    [McpServerTool(Name = "scene_clear"), Description("Clear the scene (demos with a scene host only).")]
    public async Task<object> SceneClear() =>
        await GuardAsync<object>(async () => {
            await host.ClearSceneAsync().ConfigureAwait(false);
            return new { ok = true };
        }).ConfigureAwait(false);

    /// <summary>
    ///     Loads a model file into the scene.
    /// </summary>
    /// <param name="path">Absolute or demo-relative model file path.</param>
    /// <returns>The load result or an error payload.</returns>
    [McpServerTool(Name = "model_load"), Description("Load a 3D model file into the demo scene.")]
    public async Task<object> ModelLoad(string path) =>
        await GuardAsync(() => host.LoadModelAsync(path)).ConfigureAwait(false);

    /// <summary>
    ///     Gets the scene tree.
    /// </summary>
    /// <param name="depth">Maximum nesting depth (1-12).</param>
    /// <param name="limit">Maximum node count (1-2000).</param>
    /// <returns>The root infos or an error payload.</returns>
    [McpServerTool(Name = "scene_tree"), Description("Scene tree with node ids, names, visibility, and bounds.")]
    public async Task<object> SceneTree(int depth = 6, int limit = 500) =>
        await GuardAsync(() => host.GetSceneTreeAsync(depth, limit)).ConfigureAwait(false);

    /// <summary>
    ///     Sets node visibility.
    /// </summary>
    /// <param name="id">Node id (guid string).</param>
    /// <param name="visible">The visibility.</param>
    /// <returns>The node summary or an error payload.</returns>
    [McpServerTool(Name = "scene_set_visible"), Description("Show or hide a scene node by id.")]
    public async Task<object> SceneSetVisible(string id, bool visible) =>
        await GuardAsync(() => host.SetNodeVisibilityAsync(ParseNodeId(id), visible)).ConfigureAwait(false);

    /// <summary>
    ///     Zooms to a node subtree.
    /// </summary>
    /// <param name="id">Node id (guid string).</param>
    /// <param name="animationTimeMs">Animation time in milliseconds.</param>
    /// <returns>The node summary or an error payload.</returns>
    [McpServerTool(Name = "scene_focus"), Description("Zoom the camera to a scene node by id.")]
    public async Task<object> SceneFocus(string id, double animationTimeMs = 0) =>
        await GuardAsync(() => host.FocusNodeAsync(ParseNodeId(id), animationTimeMs)).ConfigureAwait(false);

    /// <summary>
    ///     Picks the scene at viewport coordinates.
    /// </summary>
    /// <param name="x">Viewport-relative x.</param>
    /// <param name="y">Viewport-relative y.</param>
    /// <param name="maxHits">Maximum hit count.</param>
    /// <returns>Nearest-first hits or an error payload.</returns>
    [McpServerTool(Name = "pick"), Description("Pick scene nodes at viewport coordinates, nearest first.")]
    public async Task<object> Pick(double x, double y, int maxHits = 5) =>
        await GuardAsync(() => host.PickAsync(x, y, maxHits)).ConfigureAwait(false);

    /// <summary>
    ///     Gets render info.
    /// </summary>
    /// <returns>The render info or an error payload.</returns>
    [McpServerTool(Name = "render_info"), Description("Render info: FPS, triangles, active and known techniques.")]
    public async Task<object> RenderInfo() => await GuardAsync(host.GetRenderInfoAsync).ConfigureAwait(false);

    /// <summary>
    ///     Sets the viewport render technique.
    /// </summary>
    /// <param name="name">Technique name.</param>
    /// <returns>The render info or an error payload.</returns>
    [McpServerTool(Name = "render_set_technique"), Description("Set the viewport render technique by name.")]
    public async Task<object> RenderSetTechnique(string name) =>
        await GuardAsync(() => host.SetRenderTechniqueAsync(name)).ConfigureAwait(false);

    /// <summary>
    ///     Gets captured log entries, newest first.
    /// </summary>
    /// <param name="level">Optional minimum level.</param>
    /// <param name="limit">Maximum entry count.</param>
    /// <returns>The entries or an error payload.</returns>
    [McpServerTool(Name = "logs_get"), Description("Recent library log entries, newest first.")]
    public async Task<object> LogsGet(string? level = null, int limit = 100) =>
        await GuardAsync(() => host.GetLogsAsync(level, limit)).ConfigureAwait(false);

    /// <summary>
    ///     Sets the process log level.
    /// </summary>
    /// <param name="level">verbose|debug|info|warn|error|fatal.</param>
    /// <returns>The level confirmation or an error payload.</returns>
    [McpServerTool(Name = "logs_set_level"), Description("Set the process log level.")]
    public async Task<object> LogsSetLevel(string level) =>
        await GuardAsync<object>(async () => new { level = await host.SetLogLevelAsync(level).ConfigureAwait(false) })
            .ConfigureAwait(false);

    /// <summary>
    ///     Captures a viewport screenshot as base64 PNG.
    /// </summary>
    /// <param name="width">Optional explicit width.</param>
    /// <param name="height">Optional explicit height.</param>
    /// <param name="maxBytes">Byte budget; oversized shots re-render at half resolution (two attempts).</param>
    /// <returns>PNG payload with dimensions or an error payload.</returns>
    [McpServerTool(Name = "viewport_screenshot"), Description("Screenshot of the demo viewport as base64 PNG.")]
    public async Task<object> ViewportScreenshot(int? width = null, int? height = null, int maxBytes = 3_000_000) =>
        await GuardAsync<object>(async () => {
            var image = await host.RenderPngAsync(width, height).ConfigureAwait(false);
            for (var attempt = 0; attempt < 2 && image.Png.Length > maxBytes; attempt++) {
                image = await host.RenderPngAsync(
                    Math.Max(1, image.Width / 2),
                    Math.Max(1, image.Height / 2)).ConfigureAwait(false);
            }

            return new {
                mimeType = "image/png",
                base64 = Convert.ToBase64String(image.Png),
                width = image.Width,
                height = image.Height,
            };
        }).ConfigureAwait(false);

    /// <summary>
    ///     Runs a host operation and converts known failures to error payloads.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation.</param>
    /// <returns>The result or an error payload.</returns>
    private static async Task<object> GuardAsync<T>(Func<Task<T>> operation) {
        try {
            var result = await operation().ConfigureAwait(false);
            return (object?)result ?? new { error = "internal: empty result." };
        } catch (Exception exception) {
            return new { error = StableMessage(exception) };
        }
    }

    /// <summary>
    ///     Maps an exception to a stable message without stack traces.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The message.</returns>
    private static string StableMessage(Exception exception) =>
        exception switch {
            FileNotFoundException => exception.Message,
            NoSceneHostException => exception.Message,
            KeyNotFoundException => exception.Message,
            ModelLoadException => exception.Message,
            NoNodeBoundException => exception.Message,
            ArgumentException => exception.Message,
            InvalidOperationException invalid => invalid.Message,
            _ => "internal: automation operation failed.",
        };

    /// <summary>
    ///     Parses a tool node id.
    /// </summary>
    /// <param name="id">The id string.</param>
    /// <returns>The guid.</returns>
    private static Guid ParseNodeId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new ArgumentException($"invalid-node-id: '{id}' is not a guid.", nameof(id));
}
