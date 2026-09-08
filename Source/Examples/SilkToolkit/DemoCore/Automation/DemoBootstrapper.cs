// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoBootstrapper.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Diagnostics;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     One-line wiring for demo automation: attach in the window constructor, detach on close.
/// </summary>
public static class DemoBootstrapper {
    /// <summary>
    ///     Attaches the automation host and starts REST/MCP. Never throws: on failure the demo runs
    ///     without automation and an inactive host is returned.
    /// </summary>
    /// <param name="viewport">The viewport to automate.</param>
    /// <param name="effectsManager">The effects manager.</param>
    /// <param name="options">The options. Defaults to <see cref="DemoAutomationOptions.FromEnvironment" />.</param>
    /// <param name="sceneHost">The optional scene adapter for model and scene operations.</param>
    /// <param name="demoName">The demo name. Defaults to the entry assembly name.</param>
    /// <returns>The host (check <see cref="DemoAutomationHost.IsActive" />).</returns>
    public static DemoAutomationHost Attach(
        Viewport3DX viewport,
        IEffectsManager effectsManager,
        DemoAutomationOptions? options = null,
        IDemoSceneHost? sceneHost = null,
        string? demoName = null) {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(effectsManager);
        var resolved = options ?? DemoAutomationOptions.FromEnvironment();
        var host = new DemoAutomationHost(viewport, effectsManager, resolved, sceneHost, demoName);
        if (!resolved.EnableHttp && !resolved.EnableMcp) {
            return host;
        }

        try {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            host.StartAsync(timeout.Token).GetAwaiter().GetResult();
        } catch (Exception exception) {
            Debug.WriteLine($"demo-automation: disabled ({exception.Message})");
            Console.WriteLine($"demo-automation: disabled ({exception.Message})");
        }

        return host;
    }

    /// <summary>
    ///     Stops the host. Never throws.
    /// </summary>
    /// <param name="host">The host.</param>
    public static void Detach(DemoAutomationHost? host) {
        if (host is null) {
            return;
        }

        try {
            host.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
        } catch (Exception exception) {
            Debug.WriteLine($"demo-automation: detach failed ({exception.Message})");
        }
    }
}
