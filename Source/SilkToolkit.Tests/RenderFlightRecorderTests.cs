using System.Diagnostics;
using System.IO;
using System.Reflection;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Bounded recorder and real publication-hook regression tests without a device or visible window.</summary>
public sealed class RenderFlightRecorderTests {
    [Fact]
    public void CpuOnlyHookBaselineDoesNotAllocateAfterStart() {
        var recorder = new RenderFlightRecorder();
        MeasureHooks(recorder, 100);
        recorder.Start();
        MeasureHooks(recorder, 100);
        recorder.Stop();
        recorder.Clear();
        var disabled = MeasureHooks(recorder, 2000);
        recorder.Start();
        var enabled = MeasureHooks(recorder, 2000);
        recorder.Stop();
        var data = recorder.Snapshot();
        Assert.Equal(2000, data.Frames.Length);
        Assert.Equal(2000, data.Events.Length);
        Assert.Equal(0, disabled.Bytes);
        Assert.Equal(0, enabled.Bytes);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "flightrecorder-cpu-baseline.txt"),
            FormattableString.Invariant($"CPU-only, 2000 frame+event pairs; disabled {disabled.Ms:F4} ms / {disabled.Bytes} bytes; enabled {enabled.Ms:F4} ms / {enabled.Bytes} bytes. No renderer/device/UI.\n") +
            $"Value sizes: frame {System.Runtime.CompilerServices.Unsafe.SizeOf<FlightFrame>()} bytes; event {System.Runtime.CompilerServices.Unsafe.SizeOf<FlightEvent>()} bytes\n");
    }

    private static (double Ms, long Bytes) MeasureHooks(RenderFlightRecorder recorder, int count) {
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++) {
            recorder.RecordFrame(1, (ulong) i, Stopwatch.GetTimestamp(), 1, 1, 2, 3, 50);
            recorder.RecordEvent(1, (ulong) i, "submitted");
        }
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        return (elapsed, GC.GetAllocatedBytesForCurrentThread() - allocated);
    }

    [Fact]
    public void DisabledHooksAllocateNoBuffersAndDoNotCreateData() {
        var recorder = new RenderFlightRecorder();
        for (var i = 0; i < 1000; i++) {
            recorder.RecordEvent(1, 1, "lock.miss");
            recorder.RecordFrame(1, 1, Stopwatch.GetTimestamp(), 1, 1, 2, 3, 4);
        }
        Assert.False(recorder.HasRecording);
        Assert.Equal(0, recorder.ElapsedMs);
        Assert.Equal(0, recorder.Dropped);
    }

    [Fact]
    public void StartStopClearPreserveDataAndAllowExplicitRestart() {
        var recorder = new RenderFlightRecorder();
        Assert.True(recorder.Start());
        recorder.RecordEvent(2, 232, "copy.ready");
        Assert.False(recorder.Start());
        Assert.False(recorder.Clear());
        recorder.Stop();
        recorder.Stop("late");
        var data = recorder.Snapshot();
        Assert.False(recorder.Start());
        Assert.Single(data.Events);
        Assert.Equal("user", data.StopReason);
        Assert.True(recorder.Clear());
        Assert.True(recorder.Start());
        recorder.Stop();
        var restarted = recorder.Snapshot();
        Assert.Empty(restarted.Events);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FixedCapacityStopsAndCountsTheRejectedSample(bool frames) {
        var recorder = new RenderFlightRecorder();
        recorder.Start();
        var capacity = frames ? RenderFlightRecorder.FrameCapacity : RenderFlightRecorder.EventCapacity;
        for (var i = 0; i <= capacity + 20; i++) {
            if (frames) recorder.RecordFrame(1, (ulong) i, Stopwatch.GetTimestamp(), 7, 1, 2, 3, 50);
            else recorder.RecordEvent(1, 1, "lock.miss");
        }
        var data = recorder.Snapshot();
        Assert.False(recorder.IsRecording);
        Assert.Equal(frames ? "frame-budget" : "event-budget", data.StopReason);
        Assert.Equal(capacity, frames ? data.Frames.Length : data.Events.Length);
        Assert.Equal(1, frames ? data.DroppedFrames : data.DroppedEvents);
    }

    [Fact]
    public void MonotonicFramesCorrelateToExistingSerialsAcrossGenerations() {
        var recorder = new RenderFlightRecorder();
        var owner = Guid.NewGuid();
        recorder.Start();
        recorder.RecordFrame(1, 232, Stopwatch.GetTimestamp(), 7, 1.25, 2.5, 0.25, 50, owner);
        recorder.RecordFrame(2, 233, Stopwatch.GetTimestamp(), 1, 0.25, 0.5, 0.1, 51, owner);
        recorder.Stop();
        var data = recorder.Snapshot();
        Assert.Equal(new long[] { 1, 2 }, data.Frames.Select(frame => frame.FrameId));
        Assert.Equal(new ulong[] { 232, 233 }, data.Frames.Select(frame => frame.SubmissionSerial));
        Assert.Null(data.Frames[0].FramePeriodMs);
        Assert.True(data.Frames[1].FramePeriodMs >= 0);
        Assert.True(data.Frames[1].ElapsedMs >= data.Frames[0].ElapsedMs);
        Assert.All(data.Frames, frame => Assert.Equal(owner, frame.PresentationOwner));
        Assert.Equal(TimeSpan.Zero, data.StartedUtc.Offset);
    }

    [Fact]
    public void ExpiredHooksStopWithoutAcceptingLateData() {
        var recorder = new RenderFlightRecorder();
        recorder.Start();
        typeof(RenderFlightRecorder).GetField("started", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(recorder, Stopwatch.GetTimestamp() - 61 * Stopwatch.Frequency);
        recorder.RecordEvent(1, 1, "late");
        var data = recorder.Snapshot();
        Assert.Equal("time-limit", data.StopReason);
        Assert.Empty(data.Events);
    }

    [Fact]
    public Task ProductiveImageHookCapturesTwentyLockMissesAsOneSerialWithoutDemand() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX();
        using var canvas = new DPFCanvas();
        var owner = viewport.InitializeWpfPresentation(canvas);
        viewport.StopRendering();
        var demand = viewport.RenderDemand;
        while (demand.TryBeginFrame(out _)) demand.CompleteFrame(true);
        viewport.FlightRecorder.Start();
        viewport.PublicationObservation.Correlate(1, 232);
        var observe = canvas.ImageSource.ObservePublication!;
        observe("copy.ready", 0);
        for (var i = 0; i < 20; i++) {
            observe("lock.begin", 0);
            observe("lock.miss", 0);
        }
        observe("lock.acquired", 0);
        observe("unlock.done", 0);
        typeof(D3D12WpfPresentation).GetMethod("ObserveSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(owner, ["published"]);
        viewport.FlightRecorder.Stop();
        var data = viewport.FlightRecorder.Snapshot();
        Assert.Empty(data.Frames);
        Assert.Equal(20, data.Events.Count(item => item.Kind == "lock.miss"));
        Assert.All(data.Events, item => Assert.Equal(232UL, item.SubmissionSerial));
        Assert.Single(data.Events.Select(item => item.PresentationOwner).Distinct());
        Assert.NotEqual(Guid.Empty, data.Events[0].PresentationOwner);
        Assert.NotNull(data.Events[^1].Publication);
        Assert.Equal(data.Events.Select(item => item.ElapsedMs).Order(), data.Events.Select(item => item.ElapsedMs));
        Assert.False(demand.HasRenderDemand);
        Assert.Equal(0UL, owner.SubmittedFrames);
        Assert.False(viewport.PublicationObservation.WasArmed);
    });
}
