// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoDiscovery.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Diagnostics;
using System.IO;
/// <summary>
///     A running automated demo discovered via its URL file.
/// </summary>
/// <param name="Pid">Process id.</param>
/// <param name="Url">Automation base URL. Query <c>/api/status</c> for the demo name.</param>
public sealed record DiscoveredDemo(int Pid, string Url);

/// <summary>
///     Client-side discovery of running automated demos. Reads the same URL files the host writes.
/// </summary>
public static class DemoDiscovery {
    /// <summary>
    ///     Lists running automated demos, pruning stale URL files.
    /// </summary>
    /// <param name="directory">Discovery directory. Defaults to the host directory.</param>
    /// <returns>Live demos.</returns>
    public static IReadOnlyList<DiscoveredDemo> ListRunning(string? directory = null) {
        var resolved = directory ?? DefaultDirectory();
        if (!Directory.Exists(resolved)) {
            return [];
        }

        var result = new List<DiscoveredDemo>();
        foreach (var file in Directory.GetFiles(resolved, "*.url")) {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(file), out var pid)) {
                continue;
            }

            if (!IsAlive(pid)) {
                try {
                    File.Delete(file);
                } catch (Exception exception) {
                    Debug.WriteLine($"demo-automation: stale discovery cleanup failed ({exception.Message})");
                }

                continue;
            }

            try {
                result.Add(new DiscoveredDemo(pid, File.ReadAllText(file)));
            } catch (Exception exception) {
                Debug.WriteLine($"demo-automation: discovery read failed ({exception.Message})");
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets the default discovery directory.
    /// </summary>
    /// <returns>The directory path.</returns>
    internal static string DefaultDirectory() => Path.Combine(Path.GetTempPath(), "helix-demo-automation");

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
}
