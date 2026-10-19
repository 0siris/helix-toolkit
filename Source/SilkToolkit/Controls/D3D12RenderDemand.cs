using System;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Identifies independent preparation requirements that may be combined without losing priority.</summary>
[Flags]
internal enum D3D12RenderInvalidation {
    /// <summary>No scene change has been requested.</summary>
    None = 0,

    /// <summary>Redraws the current scene.</summary>
    Render = 1,

    /// <summary>Refreshes the per-frame renderable selection before drawing.</summary>
    PerFrameRenderables = 2,

    /// <summary>Rebuilds the scene graph before selecting renderables and drawing.</summary>
    SceneGraph = 4
}

/// <summary>Coordinates render reservations and independent presentation work without per-operation allocations.</summary>
/// <remarks>
/// Requests may arrive on any thread. One frame consumer must pair each successful <see cref="TryBeginFrame"/>
/// with <see cref="CompleteFrame"/> before beginning another frame. Reserve before updates and complete after
/// submission is accepted or canceled; GPU completion remains the presentation owner's responsibility.
/// Publication and recovery requests coalesce independently of each other and of the render budget. Claim
/// each before attempting external work; an unsuccessful attempt must explicitly request its own retry.
/// </remarks>
internal sealed class D3D12RenderDemand {
    /// <summary>Protects the small value-only state transitions; no external work runs while held.</summary>
    private readonly object _gate = new();

    /// <summary>Stores the nonnegative budget setting used by the next invalidation.</summary>
    private int _minimumUpdateCount;

    /// <summary>Counts requested frames that have not been reserved.</summary>
    private int _remainingFrames;

    /// <summary>Retains all preparation bits requested since the last reservation.</summary>
    private D3D12RenderInvalidation _invalidation;

    /// <summary>Retains preparation bits owned by the active reservation until submission succeeds.</summary>
    private D3D12RenderInvalidation _reservedInvalidation;

    /// <summary>Prevents overlapping reservations from consuming the same consumer's budget.</summary>
    private bool _frameActive;

    /// <summary>Records whether a newer request replaced the budget during the active reservation.</summary>
    private bool _reservationSuperseded;

    /// <summary>Retains publication work independently of scene render demand.</summary>
    private bool _publicationRequested;

    /// <summary>Retains presentation recovery work independently of scene render demand.</summary>
    private bool _recoveryRequested;

    /// <summary>Starts with a scene-graph invalidation and the configured total submitted-frame budget.</summary>
    /// <param name="minimumUpdateCount">The total frames per isolated request; negative values clamp to zero.</param>
    internal D3D12RenderDemand(int minimumUpdateCount = 6) {
        _minimumUpdateCount = Math.Max(0, minimumUpdateCount);
        Request(D3D12RenderInvalidation.SceneGraph);
    }

    /// <summary>Gets or sets the total frame budget applied to subsequent requests.</summary>
    /// <remarks>
    /// Negative values clamp to zero. Zero still permits one requested frame. Changing this setting neither
    /// wakes an idle scene nor changes an existing budget or reservation; the next nonempty request uses it.
    /// </remarks>
    internal int MinimumUpdateCount {
        get {
            lock (_gate) return _minimumUpdateCount;
        }
        set {
            lock (_gate) _minimumUpdateCount = Math.Max(0, value);
        }
    }

    /// <summary>Gets whether unreserved frames remain, including requests received during an active frame.</summary>
    /// <remarks>This does not report a frame already reserved or still pending on the GPU.</remarks>
    internal bool HasRenderDemand {
        get {
            lock (_gate) return _remainingFrames > 0;
        }
    }

    /// <summary>Gets whether publication work remains to be claimed, even with a clean scene.</summary>
    internal bool HasPublicationDemand {
        get {
            lock (_gate) return _publicationRequested;
        }
    }

    /// <summary>Gets whether presentation recovery remains to be claimed, even with a clean scene.</summary>
    internal bool HasRecoveryDemand {
        get {
            lock (_gate) return _recoveryRequested;
        }
    }

    /// <summary>Coalesces preparation flags and resets the following frame budget without consuming a frame.</summary>
    /// <param name="kind">The combined scene changes; <see cref="D3D12RenderInvalidation.None"/> does nothing.</param>
    internal void Request(D3D12RenderInvalidation kind) {
        if (kind == D3D12RenderInvalidation.None) return;
        lock (_gate) {
            _invalidation |= kind | D3D12RenderInvalidation.Render;
            _remainingFrames = Math.Max(1, _minimumUpdateCount);
            if (_frameActive) _reservationSuperseded = true;
        }
    }

    /// <summary>Reserves one existing frame and its flags before scene updates can request further work.</summary>
    /// <param name="invalidation">The reserved preparation flags, or none when idle or already reserved.</param>
    /// <returns>Whether this consumer acquired the sole active reservation.</returns>
    internal bool TryBeginFrame(out D3D12RenderInvalidation invalidation) {
        lock (_gate) {
            invalidation = D3D12RenderInvalidation.None;
            if (_frameActive || _remainingFrames == 0) return false;
            _remainingFrames--;
            invalidation = _invalidation | D3D12RenderInvalidation.Render;
            _reservedInvalidation = invalidation;
            _invalidation = D3D12RenderInvalidation.None;
            _frameActive = true;
            _reservationSuperseded = false;
            return true;
        }
    }

    /// <summary>Commits an actual submission or restores a canceled reservation without erasing newer requests.</summary>
    /// <param name="submitted">Whether commands were actually submitted, including an empty-scene frame.</param>
    /// <remarks>
    /// Cancellation restores preparation bits. It refunds the reserved frame only if no newer request reset
    /// the budget, so retries cannot enlarge that newer budget. Without an active reservation this is a no-op.
    /// </remarks>
    internal void CompleteFrame(bool submitted) {
        lock (_gate) {
            if (!_frameActive) return;
            if (!submitted) {
                _invalidation |= _reservedInvalidation;
                if (!_reservationSuperseded) _remainingFrames++;
            }

            _reservedInvalidation = D3D12RenderInvalidation.None;
            _frameActive = false;
        }
    }

    /// <summary>Requests publication without creating or consuming scene frames.</summary>
    internal void RequestPublication() {
        lock (_gate) _publicationRequested = true;
    }

    /// <summary>Requests presentation recovery without creating or consuming scene frames.</summary>
    internal void RequestRecovery() {
        lock (_gate) _recoveryRequested = true;
    }

    /// <summary>Claims publication before attempting it, preserving requests received during the attempt.</summary>
    /// <returns>Whether publication was requested; request it again if the attempt must be retried.</returns>
    internal bool TryTakePublicationRequest() {
        lock (_gate) {
            var requested = _publicationRequested;
            _publicationRequested = false;
            return requested;
        }
    }

    /// <summary>Claims recovery before attempting it, preserving requests received during the attempt.</summary>
    /// <returns>Whether recovery was requested; request it again if the attempt must be retried.</returns>
    internal bool TryTakeRecoveryRequest() {
        lock (_gate) {
            var requested = _recoveryRequested;
            _recoveryRequested = false;
            return requested;
        }
    }
}