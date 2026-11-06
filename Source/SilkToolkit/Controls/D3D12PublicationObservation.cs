using System;
using System.Diagnostics;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Observes one input-triggered publication window without scheduling any renderer work.</summary>
internal sealed class D3D12PublicationObservation {
    /// <summary>Caps naturally requested scene frames per viewport session.</summary>
    internal const int MaxFrames = 48;
    /// <summary>Caps all emitted events, including the final budget marker.</summary>
    internal const int MaxEvents = 800;
    /// <summary>Bounds observation after the input, without a timer or dispatcher callback.</summary>
    internal static readonly TimeSpan Window = TimeSpan.FromSeconds(15);
    /// <summary>Correlates the viewport and its replacement native bridge generations.</summary>
    internal Guid Owner { get; } = Guid.NewGuid();
    /// <summary>Gets whether an input has consumed this session's one-shot arm.</summary>
    internal bool WasArmed { get; private set; }
    /// <summary>Gets the accepted metadata event count.</summary>
    internal int Events { get; private set; }
    /// <summary>Gets the observed scene frame count.</summary>
    internal int Frames { get; private set; }
    /// <summary>Gets the native generation of the current serial.</summary>
    internal ulong Generation { get; private set; }
    /// <summary>Gets the current publication serial, including a frame already in transport at arm time.</summary>
    internal ulong Frame { get; private set; }
    /// <summary>Retains monotonic input time.</summary>
    private long started;
    /// <summary>Retains the terminal reason and prevents rearming.</summary>
    internal string? EndReason { get; private set; }
    /// <summary>Gets whether observation remains within its finite budget.</summary>
    internal bool IsActive => WasArmed && EndReason is null;

    /// <summary>Arms once from accepted application input; startup and repeated changes cannot reset the budget.</summary>
    internal void Arm() {
        if (WasArmed) return;
        WasArmed = true;
        started = Stopwatch.GetTimestamp();
        if (Record("armed", write: false)) {
            var package = System.Reflection.Assembly.GetEntryAssembly();
            var toolkit = typeof(D3D12PublicationObservation).Assembly;
            var core = typeof(HelixToolkit.SharpDX.Core.Core.MeshRenderCore).Assembly;
            LoggerLib.Logger.Info("WPF publication observation armed owner {Owner} package {Package} packageModule {PackageModule} packagePath {PackagePath} toolkitModule {ToolkitModule} toolkitPath {ToolkitPath} coreModule {CoreModule} corePath {CorePath} maxFrames {MaxFrames} maxEvents {MaxEvents} windowSeconds {WindowSeconds}",
                propertyValues: [Owner, package?.GetName().Version, package?.ManifestModule.ModuleVersionId, package?.Location,
                toolkit.ManifestModule.ModuleVersionId, toolkit.Location, core.ManifestModule.ModuleVersionId, core.Location, MaxFrames, MaxEvents, Window.TotalSeconds]);
        }
    }

    /// <summary>Sets the actual owner serial without inventing a new submission.</summary>
    internal void Correlate(ulong generation, ulong frame) {
        Generation = generation;
        Frame = frame;
    }

    /// <summary>Accounts for a naturally requested frame before its recording can block.</summary>
    internal void BeginFrame(ulong generation, ulong frame) {
        Correlate(generation, frame);
        if (!IsActive) return;
        if (Frames == MaxFrames) { End("frames"); return; }
        Frames++;
        Record("record.begin");
    }

    /// <summary>Closes the final observed serial only after its publication phases have been observed.</summary>
    internal void Published() {
        if (IsActive && Frames == MaxFrames) End("frames");
    }

    /// <summary>Emits metadata only, with failure HRESULTs rather than potentially sensitive exception messages.</summary>
    /// <returns>Whether this phase was within the window.</returns>
    internal bool Record(string phase, int status = 0, bool write = true) {
        if (!IsActive) return false;
        if (Stopwatch.GetElapsedTime(started) >= Window) { End("time"); return false; }
        if (Events >= MaxEvents - 1) { End("events"); return false; }
        Events++;
        if (write) LoggerLib.Logger.Info("WPF publication observation owner {Owner} generation {Generation} frame {Frame} event {Event} phase {Phase} status {Status}",
            propertyValues: [Owner, Generation, Frame, Events, phase, status]);
        return true;
    }

    /// <summary>Emits exactly one terminal marker; no timer or render request is needed to expire the window.</summary>
    private void End(string reason) {
        EndReason = reason;
        Events++;
        LoggerLib.Logger.Info("WPF publication observation owner {Owner} generation {Generation} frame {Frame} end {Reason} frames {Frames} events {Events}",
            propertyValues: [Owner, Generation, Frame, reason, Frames, Events]);
    }
}
