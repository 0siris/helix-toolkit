using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Coordinates generation retirement for productive owners of the same native D3D12 singleton.</summary>
internal sealed class D3D12PresentationDeviceRecovery {
    /// <summary>Protects generation membership only; owner callbacks never execute under this lock.</summary>
    private static readonly object Gate = new();

    /// <summary>Contains weak generation callbacks, without keeping abandoned viewports alive.</summary>
    private static readonly List<D3D12PresentationDeviceRecovery> Owners = [];

    /// <summary>The exact native device identity, independent of manager and adapter configuration.</summary>
    private readonly nint devicePointer;

    /// <summary>The weak native-state probe and retirement callback retained by the presentation owner.</summary>
    private readonly WeakReference<Action> retire;

    /// <summary>The weak owner wrapper that reports queued failures through its rendering error boundary.</summary>
    private readonly WeakReference<Action> queuedRetire;

    /// <summary>The dispatcher on which this generation's native objects may be retired.</summary>
    private readonly Dispatcher dispatcher;

    /// <summary>Reuses the dispatcher probe action without retaining the presentation owner strongly.</summary>
    private readonly Action probeCallback;

    /// <summary>Coalesces creation-failure discovery while one dispatcher probe is queued or executing.</summary>
    private int probeRequested;

    /// <summary>Coalesces the sole removal notification for this generation across threads.</summary>
    private int removalRequested;

    /// <summary>Gets whether this generation must stop accepting productive work.</summary>
    internal bool IsRemovalRequested => Volatile.Read(ref removalRequested) != 0;

    /// <summary>Registers one native generation; ordinary frame polling does not modify the registry.</summary>
    /// <param name="devicePointer">The actual native device pointer owned by the generation.</param>
    /// <param name="dispatcher">The native owner's dispatcher.</param>
    /// <param name="retire">The owner's retained native-state probe and retirement callback.</param>
    /// <param name="queuedRetire">The retained queued error boundary, or the callback for managed registry-only owners.</param>
    internal D3D12PresentationDeviceRecovery(
        nint devicePointer, Dispatcher dispatcher, Action retire, Action? queuedRetire = null
    ) {
        this.devicePointer = devicePointer;
        this.dispatcher = dispatcher;
        this.retire = new WeakReference<Action>(retire);
        this.queuedRetire = new WeakReference<Action>(queuedRetire ?? retire);
        probeCallback = ProbeOwner;
        lock (Gate) Owners.Add(this);
    }

    /// <summary>Asks registered owners to check their actual native device after a creation-time removal failure.</summary>
    /// <remarks>Probes never mark an owner removed; only its own dispatcher may confirm native removal.</remarks>
    internal static void ProbeForRemoval() {
        D3D12PresentationDeviceRecovery[] owners;
        lock (Gate) owners = Owners.ToArray();
        foreach (var owner in owners) {
            if (owner.IsRemovalRequested || !owner.retire.TryGetTarget(out _) || owner.dispatcher.HasShutdownStarted ||
                Interlocked.CompareExchange(ref owner.probeRequested, 1, 0) != 0) continue;
            _ = owner.dispatcher.BeginInvoke(DispatcherPriority.Background, owner.probeCallback);
        }
    }

    /// <summary>Runs one retained owner probe and permits another exceptional discovery request after it returns.</summary>
    private void ProbeOwner() {
        try {
            if (queuedRetire.TryGetTarget(out var callback)) callback();
        } finally {
            Volatile.Write(ref probeRequested, 0);
        }
    }

    /// <summary>Broadcasts confirmed removal once, posting across dispatchers without a synchronous wait.</summary>
    internal void RequestRemoval() {
        if (IsRemovalRequested) return;
        List<D3D12PresentationDeviceRecovery> affected = [];
        lock (Gate) {
            for (var index = Owners.Count - 1; index >= 0; index--) {
                var owner = Owners[index];
                if (!owner.retire.TryGetTarget(out _)) {
                    Owners.RemoveAt(index);
                    continue;
                }

                if (owner.devicePointer == devicePointer &&
                    Interlocked.Exchange(ref owner.removalRequested, 1) == 0) affected.Add(owner);
            }
        }

        ExceptionDispatchInfo? failure = null;
        foreach (var owner in affected) {
            if (!owner.retire.TryGetTarget(out var callback) || owner.dispatcher.HasShutdownStarted) continue;
            try {
                if (owner.dispatcher.CheckAccess()) callback();
                else if (owner.queuedRetire.TryGetTarget(out var queuedCallback))
                    _ = owner.dispatcher.BeginInvoke(DispatcherPriority.Background, queuedCallback);
            } catch (Exception exception) {
                // A failed owner retains its native ownership, but must not prevent peers receiving removal.
                failure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }

        failure?.Throw();
    }

    /// <summary>Recognizes explicit creation failure for a removed singleton, including external native retainers.</summary>
    /// <param name="exception">The native device-creation failure.</param>
    /// <returns>Whether creation should retain recovery demand rather than change adapter policy.</returns>
    internal static bool IsRemovalFailure(COMException exception) => exception.HResult is
        unchecked((int) 0x887A0005) or unchecked((int) 0x887A0006) or unchecked((int) 0x887A0007);

    /// <summary>Removes membership only after the generation has released its final native device reference.</summary>
    internal void Unregister() {
        lock (Gate) Owners.Remove(this);
    }

    /// <summary>Gets whether every registered owner of this removed native singleton has released its generation.</summary>
    internal bool CanRecreate {
        get {
            lock (Gate) {
                for (var index = Owners.Count - 1; index >= 0; index--) {
                    var owner = Owners[index];
                    if (!owner.retire.TryGetTarget(out _)) Owners.RemoveAt(index);
                    else if (owner.devicePointer == devicePointer && owner.IsRemovalRequested) return false;
                }

                return true;
            }
        }
    }
}
