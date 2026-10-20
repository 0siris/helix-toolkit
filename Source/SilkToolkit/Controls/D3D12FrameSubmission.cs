using System;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Protects one reusable frame allocation without waiting on the dispatcher during ordinary rendering.</summary>
internal sealed class D3D12FrameSubmission {
    /// The maximum lifetime of one submitted frame before rendering reports a stalled device.
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// The monotonic timestamp captured when the pending fence was submitted.
    private TimeSpan _submittedAt;

    /// The number of completed frames already delivered to presentation listeners.
    private ulong _notifiedFrames;

    /// Gets the fence protecting reusable frame resources, or zero when no frame is pending.
    internal ulong PendingFenceValue { get; private set; }

    /// Gets whether the previous submission still owns reusable frame resources.
    internal bool IsPending => PendingFenceValue != 0;

    /// Gets the number of submissions whose completion has actually been observed.
    internal ulong CompletedFrames { get; private set; }

    /// <summary>Records the only submission permitted to own the reusable command context and bindings.</summary>
    /// <param name="fenceValue">The nonzero fence signaled after submission.</param>
    /// <param name="now">The current monotonic timestamp.</param>
    /// <exception cref="ArgumentOutOfRangeException">The fence value is zero.</exception>
    /// <exception cref="InvalidOperationException">The previous submission is still pending.</exception>
    internal void Submit(ulong fenceValue, TimeSpan now) {
        ArgumentOutOfRangeException.ThrowIfZero(fenceValue);
        if (IsPending) throw new InvalidOperationException("The preceding Direct3D 12 frame is still pending");
        PendingFenceValue = fenceValue;
        _submittedAt = now;
    }

    /// <summary>Observes completion before any resource reuse, returning immediately while the GPU is busy.</summary>
    /// <param name="completedFenceValue">The actual fence completion value reported by the device.</param>
    /// <param name="now">The current monotonic timestamp.</param>
    /// <returns>Whether frame resources may be reused.</returns>
    /// <exception cref="TimeoutException">The pending frame exceeded the finite completion deadline.</exception>
    internal bool TryComplete(ulong completedFenceValue, TimeSpan now) {
        if (!IsPending) return true;
        if (completedFenceValue >= PendingFenceValue) {
            PendingFenceValue = 0;
            CompletedFrames++;
            return true;
        }
        _ = RemainingWait(now);
        return false;
    }

    /// <summary>Returns the remaining deadline for an explicit synchronous capture or resource drain.</summary>
    /// <param name="now">The current monotonic timestamp.</param>
    /// <returns>The maximum remaining wait, or zero when no frame is pending.</returns>
    /// <exception cref="TimeoutException">The pending frame exceeded the finite completion deadline.</exception>
    internal TimeSpan RemainingWait(TimeSpan now) {
        if (!IsPending) return TimeSpan.Zero;
        var remaining = CompletionTimeout - (now - _submittedAt);
        if (remaining <= TimeSpan.Zero)
            throw new TimeoutException("The Direct3D 12 frame did not complete within five seconds");
        return remaining;
    }

    /// <summary>Claims one completed-frame notification, including frames that contained no scene draws.</summary>
    /// <returns>Whether exactly one previously unreported completion was claimed.</returns>
    internal bool TryTakeCompletion() {
        if (_notifiedFrames == CompletedFrames) return false;
        _notifiedFrames++;
        return true;
    }

    /// <summary>Discards notifications belonging to a detached presentation lifetime after its GPU work is drained.</summary>
    internal void DiscardCompletions() => _notifiedFrames = CompletedFrames;

    /// <summary>Releases an outstanding submission only after confirmed device removal, without inventing completion.</summary>
    internal void AbandonRemovedDeviceSubmission() {
        PendingFenceValue = 0;
        _submittedAt = TimeSpan.Zero;
    }
}
