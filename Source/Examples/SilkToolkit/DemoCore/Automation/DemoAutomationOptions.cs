// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoAutomationOptions.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

/// <summary>
///     Startoptionen für den Demo-Automatisierungshost (REST und MCP, nur Loopback).
/// </summary>
public sealed class DemoAutomationOptions {
    /// <summary>
    ///     Gets or sets a value indicating whether the HTTP/REST interface is started.
    /// </summary>
    public bool EnableHttp { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the MCP endpoint is started.
    /// </summary>
    public bool EnableMcp { get; set; } = true;

    /// <summary>
    ///     Gets or sets the bind host. Loopback only; never wildcard.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    ///     Gets or sets the HTTP port. Zero selects a free port dynamically.
    /// </summary>
    public int HttpPort { get; set; }

    /// <summary>
    ///     Reads options from the environment (<c>HELIX_DEMO_PORT</c>, <c>HELIX_DEMO_AUTOMATION</c>).
    /// </summary>
    /// <returns>The resolved options.</returns>
    public static DemoAutomationOptions FromEnvironment() {
        var options = new DemoAutomationOptions();
        var portText = Environment.GetEnvironmentVariable("HELIX_DEMO_PORT");
        if (int.TryParse(portText, out var port) && port is >= 0 and <= 65535) {
            options.HttpPort = port;
        }

        var enabledText = Environment.GetEnvironmentVariable("HELIX_DEMO_AUTOMATION");
        if (enabledText is "0" or "false" or "no" or "off"
            or "False" or "No" or "Off" or "FALSE" or "NO" or "OFF") {
            options.EnableHttp = false;
            options.EnableMcp = false;
        }

        return options;
    }
}
