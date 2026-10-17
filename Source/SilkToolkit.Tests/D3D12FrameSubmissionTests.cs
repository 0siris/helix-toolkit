using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies single-flight ownership, finite deadlines, and exactly-once completed-frame notifications.</summary>
public sealed class D3D12FrameSubmissionTests {
    /// <summary>Verifies polling an incomplete fence cannot release resources or manufacture frame completion.</summary>
    [Fact]
    public void PendingFenceRetainsOwnershipWithoutCompletion() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(12, TimeSpan.FromSeconds(10));

        var available = frame.TryComplete(11, TimeSpan.FromSeconds(11));
        var notified = frame.TryTakeCompletion();

        Assert.False(available);
        Assert.True(frame.IsPending);
        Assert.Equal(12UL, frame.PendingFenceValue);
        Assert.Equal(0UL, frame.CompletedFrames);
        Assert.False(notified);
    }

    /// <summary>Verifies a completed empty frame is observable exactly once and permits the next submission.</summary>
    [Fact]
    public void CompletionReleasesOwnershipAndNotifiesExactlyOnce() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(4, TimeSpan.Zero);

        var available = frame.TryComplete(4, TimeSpan.FromMilliseconds(1));
        var firstNotification = frame.TryTakeCompletion();
        var repeatedPoll = frame.TryComplete(4, TimeSpan.FromMilliseconds(2));
        var repeatedNotification = frame.TryTakeCompletion();
        frame.Submit(5, TimeSpan.FromMilliseconds(3));

        Assert.True(available);
        Assert.True(firstNotification);
        Assert.True(repeatedPoll);
        Assert.False(repeatedNotification);
        Assert.True(frame.IsPending);
        Assert.Equal(1UL, frame.CompletedFrames);
    }

    /// <summary>Verifies a pending frame rejects overlapping command-context ownership.</summary>
    [Fact]
    public void PendingSubmissionRejectsOverwrite() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(1, TimeSpan.Zero);

        Assert.Throws<InvalidOperationException>(() => frame.Submit(2, TimeSpan.FromSeconds(1)));
        Assert.Equal(1UL, frame.PendingFenceValue);
    }

    /// <summary>Verifies invalid zero fences cannot masquerade as completed submissions.</summary>
    [Fact]
    public void ZeroSubmissionIsRejected() {
        var frame = new D3D12FrameSubmission();

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Submit(0, TimeSpan.Zero));
        Assert.False(frame.IsPending);
    }

    /// <summary>Verifies repeated polling and explicit consumers share the original finite submission deadline.</summary>
    [Fact]
    public void DeadlineDoesNotRestartOnEachPoll() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(3, TimeSpan.FromSeconds(10));
        var available = frame.TryComplete(2, TimeSpan.FromSeconds(14));
        var remaining = frame.RemainingWait(TimeSpan.FromSeconds(14));

        Assert.False(available);
        Assert.Equal(TimeSpan.FromSeconds(1), remaining);
        Assert.Throws<TimeoutException>(() => frame.TryComplete(2, TimeSpan.FromSeconds(15)));
        Assert.True(frame.IsPending);
    }

    /// <summary>Verifies actual completion wins over a delayed dispatcher poll after the nominal deadline.</summary>
    [Fact]
    public void ObservedCompletionAfterDelayedTickDoesNotReportTimeout() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(3, TimeSpan.Zero);

        var available = frame.TryComplete(3, TimeSpan.FromSeconds(10));

        Assert.True(available);
        Assert.Equal(1UL, frame.CompletedFrames);
    }

    /// <summary>Verifies a detached presentation lifetime cannot notify a later scene about its drained frame.</summary>
    [Fact]
    public void DetachedLifetimeDiscardsOnlyPreviouslyCompletedNotifications() {
        var frame = new D3D12FrameSubmission();
        frame.Submit(1, TimeSpan.Zero);
        _ = frame.TryComplete(1, TimeSpan.FromMilliseconds(1));
        frame.DiscardCompletions();
        var staleNotification = frame.TryTakeCompletion();
        frame.Submit(2, TimeSpan.FromMilliseconds(2));
        _ = frame.TryComplete(2, TimeSpan.FromMilliseconds(3));
        var currentNotification = frame.TryTakeCompletion();

        Assert.False(staleNotification);
        Assert.True(currentNotification);
        Assert.False(frame.TryTakeCompletion());
    }
}
