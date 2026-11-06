using System.Diagnostics;
using System.Reflection;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies finite input windows and passive integration without a device or native window.</summary>
public sealed class D3D12PublicationObservationTests {
    /// <summary>Startup cannot consume or arm the input-triggered window.</summary>
    [Fact]
    public void StartupIsSilentAndRepeatedInputDoesNotResetBudget() {
        var observation = new D3D12PublicationObservation();
        for (ulong frame = 1; frame <= 100; frame++) {
            observation.BeginFrame(1, frame);
            observation.Record("startup");
        }
        Assert.False(observation.WasArmed);
        Assert.Equal(0, observation.Events);
        observation.Arm();
        Assert.Equal(1UL, observation.Generation);
        Assert.Equal(100UL, observation.Frame);
        observation.BeginFrame(4, 101);
        observation.Arm();
        Assert.Equal(1, observation.Frames);
        Assert.Equal(2, observation.Events);
        Assert.Equal(4UL, observation.Generation);
        Assert.Equal(101UL, observation.Frame);
    }

    /// <summary>Even a permanently missed lock has a fixed event budget and one final marker.</summary>
    [Fact]
    public void LockMissFloodEndsAtEventBudgetAndCannotRearm() {
        var observation = new D3D12PublicationObservation();
        observation.Arm();
        observation.Correlate(2, 81);
        for (var attempt = 0; attempt < 2000; attempt++) observation.Record("lock.miss");
        observation.Arm();
        Assert.Equal(D3D12PublicationObservation.MaxEvents, observation.Events);
        Assert.Equal("events", observation.EndReason);
        Assert.Equal(81UL, observation.Frame);
        Assert.False(observation.IsActive);
    }

    /// <summary>The last permitted frame retains its unlock phases before closing.</summary>
    [Fact]
    public void FrameBudgetClosesAfterPublication() {
        var observation = new D3D12PublicationObservation();
        observation.Arm();
        for (ulong frame = 1; frame <= D3D12PublicationObservation.MaxFrames; frame++) {
            observation.BeginFrame(3, frame);
            Assert.True(observation.Record("unlock.done"));
            observation.Published();
        }
        var events = observation.Events;
        observation.BeginFrame(3, 49);
        observation.Record("extra");
        Assert.Equal("frames", observation.EndReason);
        Assert.Equal(48, observation.Frames);
        Assert.Equal(events, observation.Events);
    }

    /// <summary>A nonpublishing frame cannot open an unlimited scene observation.</summary>
    [Fact]
    public void NextFrameCannotExceedBudgetWithoutPublication() {
        var observation = new D3D12PublicationObservation();
        observation.Arm();
        for (ulong frame = 1; frame <= 49; frame++) observation.BeginFrame(7, frame);
        Assert.Equal(48, observation.Frames);
        Assert.Equal("frames", observation.EndReason);
    }

    /// <summary>Monotonic expiry does not require a timer, extra frame or actual test sleep.</summary>
    [Fact]
    public void ElapsedWindowEndsOnNextObservationWithoutRearm() {
        var observation = new D3D12PublicationObservation();
        observation.Arm();
        var expired = Stopwatch.GetTimestamp() - 16 * Stopwatch.Frequency;
        typeof(D3D12PublicationObservation).GetField("started", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(observation, expired);
        var accepted = observation.Record("late");
        observation.Arm();
        Assert.False(accepted);
        Assert.Equal("time", observation.EndReason);
        Assert.Equal(2, observation.Events);
    }

    /// <summary>Arming and native generation correlation leave the productive demand and counters untouched.</summary>
    [Fact]
    public Task PassiveOwnerObservationDoesNotSubmitOrChangeScene() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();
        using var canvas = new DPFCanvas();
        var owner = viewport.InitializeWpfPresentation(canvas);
        viewport.StopRendering();
        var demand = viewport.RenderDemand;
        while (demand.TryBeginFrame(out _)) demand.CompleteFrame(true);
        viewport.ObserveNextWpfPublications();
        viewport.PublicationObservation.Correlate(2, 100);
        canvas.ImageSource.ObservePublication!("lock.begin", 0);
        canvas.ImageSource.ObservePublication!("lock.miss", 0);
        canvas.ImageSource.ObservePublication!("unlock.done", 0);
        typeof(D3D12WpfPresentation).GetMethod("ObserveSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(owner, ["test.snapshot"]);
        Assert.False(demand.HasRenderDemand);
        Assert.False(owner.IsInitialized);
        Assert.False(owner.HasPendingTransport);
        Assert.Equal((0UL, 0UL, 0UL, 0UL, 0UL),
            (owner.SubmittedFrames, owner.CompletedFrames, owner.CopiedFrames, owner.PublishedFrames, owner.ReboundFrames));
        Assert.Equal(2UL, viewport.PublicationObservation.Generation);
        Assert.Equal(100UL, viewport.PublicationObservation.Frame);
        Assert.Empty(viewport.Items);
    });
}
