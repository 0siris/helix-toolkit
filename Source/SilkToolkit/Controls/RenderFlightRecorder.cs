using System;
using System.Diagnostics;
using System.Threading;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>One submitted frame; times are CPU milliseconds, never GPU durations.</summary>
public readonly record struct FlightFrame(long FrameId, ulong Generation, ulong SubmissionSerial,
    double ElapsedMs, double? FramePeriodMs, int InvalidationFlags, double SceneUpdateMs,
    double RecordMs, double ExecuteSignalMs, int DrawCount, Guid PresentationOwner = default);

/// <summary>Existing transport state at a publication milestone; slots are generation-local, not pointers.</summary>
public readonly record struct FlightPublicationState(ulong Submitted, ulong Completed, ulong Copied,
    ulong Published, ulong Rebound, ulong ReadySerial, int ReadySlot, ulong BoundSerial, int BoundSlot,
    bool FrontBufferAvailable);

/// <summary>Existing canvas values at a native recreation request; no HWND or scene content is exported.</summary>
public readonly record struct FlightRecreationState(string Reason, uint OldWidth, uint OldHeight,
    uint NewWidth, uint NewHeight, bool HandleChanged, bool HasNativeHandle,
    bool CanvasActive, bool CanvasLoaded, bool CanvasVisible, float DpiScale);

/// <summary>A fixed phase name and HRESULT from the existing publication boundary.</summary>
public readonly record struct FlightEvent(double ElapsedMs, ulong Generation, ulong SubmissionSerial,
    string Kind, int Status, FlightPublicationState? Publication, Guid PresentationOwner = default) {
    /// <summary>Gets optional request context; older recordings and other event kinds leave it null.</summary>
    public FlightRecreationState? Recreation { get; init; }
}

/// <summary>A bounded detached recording; elapsed values share StartedUtc and Stopwatch time.</summary>
public sealed record FlightRecording(DateTimeOffset StartedUtc, double DurationMs, string StopReason,
    long DroppedFrames, long DroppedEvents, int FrameCapacity, int EventCapacity, int MaxDurationSeconds,
    FlightFrame[] Frames, FlightEvent[] Events);

/// <summary>Opt-in bounded value buffers. Render writers never wait for a lock, serialize, or request work.</summary>
public sealed class RenderFlightRecorder {
    /// <summary>The fixed frame storage bound.</summary>
    public const int FrameCapacity = 8192;
    /// <summary>The fixed event storage bound.</summary>
    public const int EventCapacity = 32768;
    /// <summary>The independent wall-clock recording bound.</summary>
    public const int MaxDurationSeconds = 60;
    private readonly object gate = new();
    private FlightFrame[] frames = [];
    private FlightEvent[] events = [];
    private int frameCount, eventCount;
    private long droppedFrames, droppedEvents, started, ended, nextFrameId, previousFrame;
    private DateTimeOffset startedUtc;
    private volatile bool active;
    private string stopReason = "not-started";

    /// <summary>Whether record hooks may append; no lazy allocation occurs here.</summary>
    public bool IsRecording => active;
    /// <summary>Whether an unexported or exported recording is retained.</summary>
    public bool HasRecording => started != 0;
    /// <summary>Gets monotonic elapsed milliseconds, frozen after stop.</summary>
    public double ElapsedMs => started == 0 ? 0 : Milliseconds((active ? Stopwatch.GetTimestamp() : ended) - started);
    /// <summary>Gets the termination cause, or recording.</summary>
    public string StopReason => stopReason;
    /// <summary>Gets all samples rejected by contention or capacity.</summary>
    public long Dropped => Interlocked.Read(ref droppedFrames) + Interlocked.Read(ref droppedEvents);

    /// <summary>Starts only from an empty recorder; stop never discards retained data.</summary>
    public bool Start() {
        lock (gate) {
            if (active || HasRecording) return false;
            frames = new FlightFrame[FrameCapacity];
            events = new FlightEvent[EventCapacity];
            startedUtc = DateTimeOffset.UtcNow;
            started = Stopwatch.GetTimestamp();
            stopReason = "recording";
            active = true;
            return true;
        }
    }

    /// <summary>Stops without clearing. May run on the deadline timer even while the UI is blocked.</summary>
    public void Stop(string reason = "user") {
        lock (gate) StopCore(reason);
    }

    private void StopCore(string reason) {
        if (!active) return;
        ended = Stopwatch.GetTimestamp();
        stopReason = reason;
        active = false;
    }

    /// <summary>Explicitly discards stopped data; an active recording is never silently replaced.</summary>
    public bool Clear() {
        lock (gate) {
            if (active) return false;
            frames = [];
            events = [];
            frameCount = eventCount = 0;
            droppedFrames = droppedEvents = started = ended = nextFrameId = previousFrame = 0;
            stopReason = "not-started";
            return true;
        }
    }

    /// <summary>Converts one shared monotonic timestamp to milliseconds relative to recording start.</summary>
    public double ElapsedAt(long timestamp) => Milliseconds(timestamp - started);

    /// <summary>Records one naturally submitted frame from the productive owner.</summary>
    internal void RecordFrame(ulong generation, ulong serial, long frameStarted, int invalidationFlags,
        double sceneUpdateMs, double recordMs, double executeSignalMs, int draws, Guid presentationOwner = default) {
        if (!active) return;
        if (!Monitor.TryEnter(gate)) { Interlocked.Increment(ref droppedFrames); return; }
        try {
            if (!AcceptNow()) return;
            if (frameCount == frames.Length) { droppedFrames++; StopCore("frame-budget"); return; }
            frames[frameCount++] = new(++nextFrameId, generation, serial, ElapsedAt(frameStarted),
                previousFrame == 0 ? null : Milliseconds(frameStarted - previousFrame), invalidationFlags,
                sceneUpdateMs, recordMs, executeSignalMs, draws, presentationOwner);
            previousFrame = frameStarted;
        } finally { Monitor.Exit(gate); }
    }

    /// <summary>Records only toolkit-owned constant phase names and optional existing owner counters.</summary>
    internal void RecordEvent(ulong generation, ulong serial, string kind, int status = 0,
        FlightPublicationState? publication = null, Guid presentationOwner = default,
        FlightRecreationState? recreation = null) {
        if (!active) return;
        if (!Monitor.TryEnter(gate)) { Interlocked.Increment(ref droppedEvents); return; }
        try {
            if (!AcceptNow()) return;
            if (eventCount == events.Length) { droppedEvents++; StopCore("event-budget"); return; }
            events[eventCount++] = new(ElapsedAt(Stopwatch.GetTimestamp()), generation, serial, kind, status, publication,
                presentationOwner) { Recreation = recreation };
        } finally { Monitor.Exit(gate); }
    }

    /// <summary>Records only known input categories, never positions, control names, or text.</summary>
    public void RecordInput(FlightInputKind kind) {
        if (!active || !Enum.IsDefined(kind)) return;
        RecordEvent(0, 0, kind switch {
            FlightInputKind.PointerDown => "input.pointer-down",
            FlightInputKind.PointerDrag => "input.pointer-drag",
            FlightInputKind.Wheel => "input.wheel",
            FlightInputKind.Menu => "input.menu",
            _ => "input.curve-edit"
        });
    }

    private bool AcceptNow() {
        if (active && Stopwatch.GetElapsedTime(started).TotalSeconds >= MaxDurationSeconds) StopCore("time-limit");
        return active;
    }

    /// <summary>Copies only stopped buffers; call outside rendering before background serialization.</summary>
    public FlightRecording Snapshot() {
        lock (gate) {
            if (active || !HasRecording) throw new InvalidOperationException("Stop recording before export");
            return new(startedUtc, ElapsedMs, stopReason, droppedFrames, droppedEvents,
                FrameCapacity, EventCapacity, MaxDurationSeconds, frames[..frameCount], events[..eventCount]);
        }
    }

    private static double Milliseconds(long ticks) => ticks * 1000d / Stopwatch.Frequency;
}

/// <summary>Allowed privacy-preserving input categories.</summary>
public enum FlightInputKind { PointerDown, PointerDrag, Wheel, Menu, CurveEdit }
