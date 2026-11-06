using System.Reflection;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Exercises the real owner timer on a headless dispatcher with controlled native transport.</summary>
[Collection(WpfCollection.Name)]
public sealed class D3D12TransportTimerTests {
    /// <summary>Finishes the same accepted serial without any further composition or manual transport calls.</summary>
    /// <param name="heldStage">The controlled native stage that initially cannot complete.</param>
    [Theory]
    [InlineData("fence")]
    [InlineData("copy")]
    [InlineData("ready")]
    [InlineData("lock")]
    public Task ActiveTransportProgressesWithoutRenderingTicksTest(string heldStage) => StaThread.RunAsync(async () => {
        using var driver = new TimerDriver(heldStage);
        driver.Viewport.StartRendering();
        driver.Viewport.InvalidateRender();
        driver.StartAcceptedTransport();

        Assert.True(driver.Canvas.IsActive);
        Assert.True(driver.Timer.IsEnabled);
        await driver.Blocked.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(17UL, driver.BoundSerial);
        Assert.Equal(heldStage is "ready" or "lock" ? 18UL : 0UL, driver.ReadySerial);
        Assert.Equal(0UL, driver.Owner.PublishedFrames);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);

        driver.Released = true;
        await driver.Published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(18UL, driver.BoundSerial);
        Assert.Equal(18UL, Read<ulong>(driver.Owner, "publishedSerial"));
        Assert.Equal(0UL, driver.ReadySerial);
        Assert.Equal(18UL, driver.Owner.SubmittedFrames);
        Assert.Equal(1UL, driver.Owner.PublishedFrames);
        Assert.Equal(18UL, driver.Owner.CompletedFrames);
        Assert.Equal(18UL, driver.Owner.CopiedFrames);
        Assert.False(driver.Owner.HasPendingTransport);
        Assert.False(driver.Timer.IsEnabled);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.Equal(0, driver.SceneCallbacks);
        Assert.Equal(TimeSpan.FromSeconds(123), Read<TimeSpan>(driver.Owner, "lastRenderingTime"));
        var calls = driver.Calls;
        await Task.Delay(65);
        Assert.Equal(calls, driver.Calls);

        // The request is still reservable by the next normal frame, including its preparation flags.
        var reserved = driver.Viewport.RenderDemand.TryBeginFrame(out var invalidation);
        Assert.True(reserved);
        Assert.NotEqual(D3D12RenderInvalidation.None, invalidation);
        driver.Viewport.RenderDemand.CompleteFrame(false);
    });

    /// <summary>Shares the updating guard with rendering and retains invalidation from a nested render callback.</summary>
    [Fact]
    public Task NestedRenderingCannotReenterTimerTransportTest() => StaThread.RunAsync(async () => {
        using var driver = new TimerDriver("fence");
        driver.DuringService = () => {
            Assert.True(driver.Owner.IsUpdating);
            driver.InvokeService();
            var result = driver.Owner.RenderFrame(TimeSpan.FromSeconds(42), automaticFrame: true);
            Assert.Equal(0, result);
        };
        driver.StartAcceptedTransport();
        await driver.Blocked.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(1, driver.MaximumDepth);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.False(driver.Owner.IsUpdating);
        driver.DuringService = null;
        driver.Released = true;
        await driver.Published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False(driver.Owner.IsUpdating);
        Assert.False(driver.Timer.IsEnabled);
    });

    /// <summary>StopRendering retains accepted transport but cannot cause a scene submission from the timer.</summary>
    [Fact]
    public Task StoppedRenderingStillFinishesAcceptedTransportTest() => StaThread.RunAsync(async () => {
        using var driver = new TimerDriver("copy");
        driver.Viewport.StopRendering();
        driver.StartAcceptedTransport();
        await driver.Blocked.Task.WaitAsync(TimeSpan.FromSeconds(3));
        driver.Released = true;
        await driver.Published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(18UL, driver.Owner.SubmittedFrames);
        Assert.Equal(0, driver.SceneCallbacks);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Recovery and scene demand alone must not keep an active idle timer running.</summary>
    [Fact]
    public Task ActiveIdleRecoveryDoesNotStartTimerTest() => StaThread.RunAsync(async () => {
        using var driver = new TimerDriver("ready");
        driver.Viewport.InvalidateRender();
        driver.Owner.RequestRecreation();
        driver.RefreshTimer();
        Assert.False(driver.Timer.IsEnabled);
        await Task.Delay(65);
        Assert.Equal(0, driver.Calls);
        Assert.Equal(0UL, driver.Owner.SubmittedFrames);
        Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>A disposal preparation stops accepted service and keeps queued ticks from touching transport.</summary>
    [Fact]
    public Task DisposalPreparationStopsAcceptedServiceTest() => StaThread.RunAsync(() => {
        using var driver = new TimerDriver("copy");
        driver.StartAcceptedTransport();
        driver.Owner.BeginDisposalPreparation();
        driver.InvokeService();
        Assert.Equal(0, driver.Calls);
        Assert.False(driver.Timer.IsEnabled);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Queued ticks respect updating, final stop, stale ownership and disposal before native access.</summary>
    /// <param name="guard">The retained lifecycle guard to activate.</param>
    [Theory]
    [InlineData("updating")]
    [InlineData("stopping")]
    [InlineData("orphan")]
    [InlineData("disposed")]
    public Task QueuedServiceRespectsOwnerGuardsTest(string guard) => StaThread.RunAsync(() => {
        using var driver = new TimerDriver("ready");
        driver.StartAcceptedTransport();
        if (guard == "orphan") Write(driver.Viewport, "wpfPresentation", null);
        else if (guard == "disposed") driver.Owner.Dispose();
        else Write(driver.Owner, guard, true);
        driver.InvokeService();
        Assert.Equal(0, driver.Calls);
        Assert.False(driver.Timer.IsEnabled);
        Write(driver.Owner, "updating", false);
        Write(driver.Owner, "stopping", false);
        Write(driver.Viewport, "wpfPresentation", driver.Owner);
    });

    /// <summary>Automatic failures unwind updating and stop the timer through the existing error boundary.</summary>
    [Fact]
    public Task ServiceFailureStopsTimerAndReleasesUpdatingGuardTest() => StaThread.RunAsync(async () => {
        using var driver = new TimerDriver("lock");
        var failure = new InvalidOperationException("Controlled native transport failure");
        var reported = new TaskCompletionSource<Exception>();
        driver.Viewport.RenderExceptionOccurred += (_, args) => {
            args.Handled = true;
            reported.TrySetResult(args.Exception);
        };
        driver.DuringService = () => throw failure;
        driver.StartAcceptedTransport();
        var actual = await reported.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Same(failure, actual);
        Assert.False(driver.Owner.IsUpdating);
        Assert.False(driver.Timer.IsEnabled);
        Assert.Equal(18UL, driver.Owner.SubmittedFrames);
        Assert.Equal(0, driver.SceneCallbacks);
    });

    /// <summary>Reads retained owner fields without advancing any production work.</summary>
    private static T Read<T>(object owner, string name) =>
        (T) owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Seeds controlled native state without a window, GPU, or fabricated native surface pointer.</summary>
    private static void Write(object owner, string name, object? value) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    /// <summary>Retains a real canvas, owner and timer; only the native transport operation is controlled.</summary>
    private sealed class TimerDriver : IDisposable {
        /// <summary>The canvas is active by controlled state, without subscribing to global composition ticks.</summary>
        internal DPFCanvas Canvas { get; } = new();
        /// <summary>The productive viewport owns the timer consumer.</summary>
        internal Viewport3DX Viewport { get; } = new();
        /// <summary>The actual owner whose timer subscription is under test.</summary>
        internal D3D12WpfPresentation Owner { get; }
        /// <summary>The production timer, including its real registered callback.</summary>
        internal DispatcherTimer Timer => Read<DispatcherTimer>(Owner, "serviceTimer");
        /// <summary>Observes a held transport attempt without polling it from the test.</summary>
        internal TaskCompletionSource Blocked { get; } = new();
        /// <summary>Observes controlled publication of the exact accepted serial.</summary>
        internal TaskCompletionSource Published { get; } = new();
        /// <summary>Controls readiness at the native boundary.</summary>
        internal bool Released;
        /// <summary>Counts native-service calls rather than scene submissions.</summary>
        internal int Calls;
        /// <summary>Counts scene callbacks, which must not run on an active transport tick.</summary>
        internal int SceneCallbacks;
        /// <summary>Tracks recursion into controlled native work.</summary>
        private int depth;
        /// <summary>Records whether a reentrant tick got past the owner guard.</summary>
        internal int MaximumDepth;
        /// <summary>Models the previously published identity without binding a fake pointer to D3DImage.</summary>
        internal ulong BoundSerial = 17;
        /// <summary>Models the accepted frame identity awaiting publication.</summary>
        internal ulong ReadySerial = 18;
        /// <summary>Allows a completion callback or failure during transport servicing.</summary>
        internal Action? DuringService;
        /// <summary>The held native phase used to seed pending fence state.</summary>
        private readonly string stage;

        /// <summary>Connects the real subscription with a controlled, nonwaiting native service boundary.</summary>
        /// <param name="stage">The native phase held before release.</param>
        internal TimerDriver(string stage) {
            this.stage = stage;
            ReadySerial = stage is "ready" or "lock" ? 18UL : 0UL;
            Owner = new D3D12WpfPresentation(Viewport, Canvas, SilkDriverType.Hardware, Service);
            Write(Viewport, "wpfPresentation", Owner);
            Write(Canvas, "<IsActive>k__BackingField", true);
            Write(Owner, "lastRenderingTime", TimeSpan.FromSeconds(123));
            Viewport.OnRendering += (_, _) => SceneCallbacks++;
        }

        /// <summary>Models the state at an accepted submission's finally and uses its real timer activation.</summary>
        internal void StartAcceptedTransport() {
            Write(Owner, "<SubmittedFrames>k__BackingField", 18UL);
            Write(Owner, "transportSerial", 18UL);
            if (stage == "fence") Write(Owner, "pendingFence", 1UL);
            Write(Owner, "copyStarted", stage != "fence");
            Write(Owner, "<CompletedFrames>k__BackingField", stage == "fence" ? 17UL : 18UL);
            Write(Owner, "<CopiedFrames>k__BackingField", stage == "fence" ? 17UL : 18UL);
            RefreshTimer();
        }

        /// <summary>Uses the production activation decision, never starts the timer directly.</summary>
        internal void RefreshTimer() {
            typeof(D3D12WpfPresentation).GetMethod("UpdateServiceTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Owner, null);
        }

        /// <summary>Invokes only queued/reentrant callback cases; autonomous progress tests never call this.</summary>
        internal void InvokeService() => typeof(D3D12WpfPresentation)
            .GetMethod("ServiceSuspendedTransport", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<EventHandler>(Owner)(Timer, EventArgs.Empty);

        /// <summary>Models native backpressure and exact publication, leaving scene demand untouched.</summary>
        /// <param name="wait">The actual owner's wait permission, which must always be false here.</param>
        /// <returns>Whether the accepted transport has completed.</returns>
        private bool Service(bool wait) {
            Assert.False(wait);
            Calls++;
            depth++;
            MaximumDepth = Math.Max(MaximumDepth, depth);
            try {
                DuringService?.Invoke();
                if (!Released) {
                    if (Calls >= 2) Blocked.TrySetResult();
                    return false;
                }
                // Model completion of the held native phase before its exact serial becomes publishable.
                Write(Owner, "<CompletedFrames>k__BackingField", 18UL);
                Write(Owner, "<CopiedFrames>k__BackingField", 18UL);
                ReadySerial = 18;
                BoundSerial = ReadySerial;
                ReadySerial = 0;
                Write(Owner, "pendingFence", 0UL);
                Write(Owner, "transportSerial", 0UL);
                Write(Owner, "publishedSerial", BoundSerial);
                Write(Owner, "<PublishedFrames>k__BackingField", 1UL);
                // Post observation after the callback's finally has updated timer ownership.
                Canvas.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => Published.TrySetResult());
                return true;
            } finally {
                depth--;
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Timer.Stop();
            Write(Owner, "pendingFence", 0UL);
            Write(Owner, "transportSerial", 0UL);
            Owner.Dispose();
            Viewport.Dispose();
            Canvas.Dispose();
        }
    }
}
