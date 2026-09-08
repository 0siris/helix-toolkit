// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoHttpServer.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

/// <summary>
///     Loopback-only Kestrel server hosting the demo REST routes and, if enabled, the MCP endpoint.
///     v1 has no authentication; binding to 127.0.0.1 is the entire access control.
/// </summary>
internal sealed class DemoHttpServer : IAsyncDisposable {
    /// <summary>
    ///     The running application.
    /// </summary>
    private readonly WebApplication app;

    /// <summary>
    ///     The URL discovery file. Null when unwritable.
    /// </summary>
    private readonly string? discoveryFile;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DemoHttpServer" /> class.
    /// </summary>
    /// <param name="app">The running application.</param>
    /// <param name="url">The bound URL.</param>
    /// <param name="discoveryFile">The URL discovery file.</param>
    private DemoHttpServer(WebApplication app, string url, string? discoveryFile) {
        this.app = app;
        Url = url;
        this.discoveryFile = discoveryFile;
    }

    /// <summary>
    ///     Gets the bound URL.
    /// </summary>
    internal string Url { get; }

    /// <summary>
    ///     Builds, maps, and starts the server.
    /// </summary>
    /// <param name="host">The automation host serving the routes.</param>
    /// <param name="options">The options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The running server.</returns>
    internal static async Task<DemoHttpServer> StartAsync(
        DemoAutomationHost host,
        DemoAutomationOptions options,
        CancellationToken cancellationToken) {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = [],
            ApplicationName = typeof(DemoHttpServer).Assembly.FullName ?? "DemoCore.Automation",
        });
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseKestrel(serverOptions => serverOptions.Listen(IPAddress.Loopback, options.HttpPort));
        builder.Services.AddSingleton(host);
        if (options.EnableMcp) {
            builder.Services.AddMcpServer().WithHttpTransport(transport => transport.Stateless = true).WithTools<DemoMcpTools>();
        }

        var app = builder.Build();
        if (options.EnableHttp) {
            MapRoutes(app);
        }

        if (options.EnableMcp) {
            app.MapMcp("/mcp");
        }

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
            ?? $"http://{options.Host}:{options.HttpPort}";
        var discoveryFile = WriteDiscoveryFile(url);
        Debug.WriteLine($"demo-automation: {url}");
        Console.WriteLine($"demo-automation: {url}");
        return new DemoHttpServer(app, url, discoveryFile);
    }

    /// <summary>
    ///     Writes the bound URL to a temp file. WinExe demos have no console, so agents discover the port here.
    /// </summary>
    /// <param name="url">The bound URL.</param>
    /// <returns>The discovery file path, or null when unwritable.</returns>
    private static string? WriteDiscoveryFile(string url) {
        try {
            var directory = Path.Combine(Path.GetTempPath(), "helix-demo-automation");
            Directory.CreateDirectory(directory);
            foreach (var stale in Directory.GetFiles(directory, "*.url")) {
                if (int.TryParse(Path.GetFileNameWithoutExtension(stale), out var pid) && !IsAlive(pid)) {
                    try {
                        File.Delete(stale);
                    } catch (Exception exception) {
                        Debug.WriteLine($"demo-automation: stale discovery cleanup failed ({exception.Message})");
                    }
                }
            }

            var file = Path.Combine(directory, $"{Environment.ProcessId}.url");
            File.WriteAllText(file, url);
            return file;
        } catch (Exception exception) {
            Debug.WriteLine($"demo-automation: discovery file failed ({exception.Message})");
            return null;
        }
    }

    /// <summary>
    ///     Checks whether a process id is still running.
    /// </summary>
    /// <param name="pid">The process id.</param>
    /// <returns>True when the process exists.</returns>
    private static bool IsAlive(int pid) {
        try {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        } catch {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await app.DisposeAsync().ConfigureAwait(false);
        if (discoveryFile is not null) {
            try {
                File.Delete(discoveryFile);
            } catch (Exception exception) {
                Debug.WriteLine($"demo-automation: discovery cleanup failed ({exception.Message})");
            }
        }
    }

    /// <summary>
    ///     Maps the REST routes.
    /// </summary>
    /// <param name="app">The application.</param>
    private static void MapRoutes(WebApplication app) {
        app.MapGet("/healthz", () => Results.Text("ok", "text/plain"));
        app.MapGet("/api/status", (DemoAutomationHost host) => GuardAsync(host.GetStatusAsync));
        app.MapGet("/api/camera", (DemoAutomationHost host) => GuardAsync(host.GetCameraAsync));
        app.MapPost("/api/camera", (CameraState patch, DemoAutomationHost host) => GuardAsync(() => host.SetCameraAsync(patch)));
        app.MapPost("/api/camera/zoom-extents",
            (ZoomExtentsRequest? request, DemoAutomationHost host) =>
                GuardAsync<object>(async () => {
                    await host.ZoomExtentsAsync(request?.AnimationTimeMs ?? 0).ConfigureAwait(false);
                    return new { ok = true };
                }));
        app.MapPost("/api/camera/reset",
            (DemoAutomationHost host) =>
                GuardAsync<object>(async () => {
                    await host.ResetCameraAsync().ConfigureAwait(false);
                    return new { ok = true };
                }));
        app.MapGet("/api/scene/stats", (DemoAutomationHost host) => GuardAsync(host.GetStatsAsync));
        app.MapGet("/api/scene/tree",
            (int? depth, int? limit, DemoAutomationHost host) =>
                GuardAsync(() => host.GetSceneTreeAsync(depth ?? 6, limit ?? 500)));
        app.MapPost("/api/scene/nodes/{id}/visibility",
            (string id, VisibilityRequest request, DemoAutomationHost host) =>
                GuardAsync(() => host.SetNodeVisibilityAsync(ParseNodeId(id), request.Visible)));
        app.MapPost("/api/scene/nodes/{id}/focus",
            (string id, ZoomExtentsRequest? request, DemoAutomationHost host) =>
                GuardAsync(() => host.FocusNodeAsync(ParseNodeId(id), request?.AnimationTimeMs ?? 0)));
        app.MapPost("/api/pick",
            (PickRequest request, DemoAutomationHost host) =>
                GuardAsync(() => host.PickAsync(request.X, request.Y, request.MaxHits)));
        app.MapGet("/api/render", (DemoAutomationHost host) => GuardAsync(host.GetRenderInfoAsync));
        app.MapPost("/api/render/technique",
            (TechniqueRequest request, DemoAutomationHost host) =>
                GuardAsync(() => host.SetRenderTechniqueAsync(request.Name)));
        app.MapGet("/api/logs",
            (string? level, int? limit, DemoAutomationHost host) =>
                GuardAsync(() => host.GetLogsAsync(level, limit ?? 100)));
        app.MapPost("/api/logs/level",
            (LogLevelRequest request, DemoAutomationHost host) =>
                GuardAsync<object>(async () => new { level = await host.SetLogLevelAsync(request.Level).ConfigureAwait(false) }));
        app.MapPost("/api/scene/clear",
            (DemoAutomationHost host) =>
                GuardAsync<object>(async () => {
                    await host.ClearSceneAsync().ConfigureAwait(false);
                    return new { ok = true };
                }));
        app.MapPost("/api/model/load",
            (ModelLoadRequest request, DemoAutomationHost host) =>
                GuardAsync(() => host.LoadModelAsync(request.Path)));
        app.MapGet("/api/screenshot",
            async (int? width, int? height, DemoAutomationHost host) => {
                try {
                    var image = await host.RenderPngAsync(width, height).ConfigureAwait(false);
                    return Results.File(image.Png, "image/png");
                } catch (Exception exception) {
                    return MapError(exception);
                }
            });
        app.MapGet("/api/screenshot.json",
            (DemoAutomationHost host) =>
                GuardAsync<object>(async () => {
                    var image = await host.RenderPngAsync(null, null).ConfigureAwait(false);
                    return new {
                        pngBase64 = Convert.ToBase64String(image.Png),
                        width = image.Width,
                        height = image.Height,
                    };
                }));
    }

    /// <summary>
    ///     Runs a host operation and maps known failures to stable error responses.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation.</param>
    /// <returns>The HTTP result.</returns>
    private static async Task<IResult> GuardAsync<T>(Func<Task<T>> operation) {
        try {
            return Results.Json(await operation().ConfigureAwait(false));
        } catch (Exception exception) {
            return MapError(exception);
        }
    }

    /// <summary>
    ///     Maps an exception to a stable error response without stack traces.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The HTTP result.</returns>
    private static IResult MapError(Exception exception) =>
        exception switch {
            FileNotFoundException notFound => Results.NotFound(new { error = notFound.Message }),
            NoSceneHostException noHost => Results.Conflict(new { error = noHost.Message }),
            KeyNotFoundException missing => Results.NotFound(new { error = missing.Message }),
            ModelLoadException load => Results.UnprocessableEntity(new { error = load.Message }),
            NoNodeBoundException bound => Results.UnprocessableEntity(new { error = bound.Message }),
            ArgumentException argument => Results.UnprocessableEntity(new { error = argument.Message }),
            InvalidOperationException invalid => Results.Json(new { error = invalid.Message },
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Json(new { error = "internal: automation operation failed." },
                statusCode: StatusCodes.Status500InternalServerError),
        };

    /// <summary>
    ///     Parses a route node id.
    /// </summary>
    /// <param name="id">The id string.</param>
    /// <returns>The guid.</returns>
    private static Guid ParseNodeId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new ArgumentException($"invalid-node-id: '{id}' is not a guid.", nameof(id));
}
