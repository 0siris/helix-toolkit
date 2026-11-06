using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Core.Native;
using D11 = Silk.NET.Direct3D11;
using D12 = Silk.NET.Direct3D12;
using D9 = Silk.NET.Direct3D9;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Exercises production transport with CPU COM callbacks, without a window or GPU device.</summary>
[Collection(WpfCollection.Name)]
public sealed class D3D12StableOutputTests {
    /// <summary>Reuses the export's serializer contract.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
    /// <summary>Different completed contents retain the same output allocation across ordinary frames.</summary>
    [Fact]
    public Task NormalFramesKeepOutputIdentityTest() => StaThread.RunAsync(() => {
        using var native = new NativeTransport();
        var bindings = new List<nint>();
        using var image = new D3D12ImageSource(() => true, bindings.Add, _ => { }, () => { });
        native.Image = image;
        nint first = 0;
        for (ulong serial = 1; serial <= 3; serial++) {
            native.SourceContent = (int) serial;
            var locked = image.TryBeginWrite();
            Assert.True(locked);
            native.Bridge.BeginCopy(serial, image);
            native.Complete = true;
            var complete = native.Bridge.IsCopyComplete;
            var surface = native.Bridge.ReadySurface;
            Assert.True(complete);
            Assert.Equal((int) serial, native.OutputContent);
            if (serial == 1) first = surface;
            Assert.Equal(first, surface);
            var published = image.TryPublish(surface, 64, 48);
            Assert.True(published);
            native.Bridge.ConfirmPublication(serial);
        }
        Assert.Single(bindings);
        Assert.False(native.CopiedWithoutLock);
    });

    /// <summary>A real WPF zero-lock miss must precede and prevent the native copy.</summary>
    [Fact]
    public Task LockMissDoesNotCopyTest() => StaThread.RunAsync(() => {
        using var native = new NativeTransport();
        using var canvas = new DPFCanvas();
        using var viewport = new Viewport3DX();
        var owner = viewport.InitializeWpfPresentation(canvas);
        viewport.StopRendering();
        Write(owner, "device", native.Device);
        Write(owner, "bridge", native.Bridge);
        Write(owner, "transportSerial", 1UL);
        Write(canvas, "<IsActive>k__BackingField", true);
        var writable = (EventWaitHandle) typeof(D3DImage).GetField("_canWriteEvent", Flags)!
            .GetValue(canvas.ImageSource.NativeImage)!;
        writable.Reset();
        try {
            var service = typeof(D3D12WpfPresentation).GetMethod("ServiceTransportCore", Flags)!
                .CreateDelegate<Func<bool, bool>>(owner);
            var idle = service(false);
            Assert.False(idle);
            Assert.Equal(0, native.Copies);
            Assert.Equal(0u, typeof(D3DImage).GetField("_lockCount", Flags)!.GetValue(canvas.ImageSource.NativeImage));
            Assert.Equal(1UL, Read<ulong>(owner, "transportSerial"));
        } finally {
            writable.Set();
            Write(owner, "device", null);
            Write(owner, "bridge", null);
            Write(owner, "transportSerial", 0UL);
        }
    });

    /// <summary>Checks real framework lock accounting and records the actual loaded runtime, without a surface.</summary>
    [Fact]
    public Task FrameworkWriteLeaseBalancesMissAndRetainedLockTest() => StaThread.RunAsync(() => {
        using var image = new D3D12ImageSource();
        var native = image.NativeImage;
        var writable = (EventWaitHandle) typeof(D3DImage).GetField("_canWriteEvent", Flags)!.GetValue(native)!;
        var count = typeof(D3DImage).GetField("_lockCount", Flags)!;
        writable.Reset();
        try {
            var acquired = image.TryBeginWrite();
            Assert.False(acquired);
            Assert.False(image.IsWriteLocked);
            Assert.Equal(0u, count.GetValue(native));
        } finally {
            writable.Set();
        }
        var held = image.TryBeginWrite();
        Assert.True(held);
        Assert.Equal(1u, count.GetValue(native));
        var detached = image.TryDetach();
        Assert.False(detached);
        Assert.Equal(1u, count.GetValue(native));
        image.EndWrite();
        Assert.Equal(0u, count.GetValue(native));
        var version = typeof(D3DImage).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "stable-output-runtime.txt"),
            $"{RuntimeInformation.FrameworkDescription}\n{typeof(D3DImage).Assembly.Location}\n{version}\n");
    });

    /// <summary>Same-output recovery needs no new binding; real detachment or a changed target still binds.</summary>
    [Fact]
    public Task RebindAndTargetChangeKeepExplicitBindingBoundariesTest() => StaThread.RunAsync(() => {
        var bindings = new List<nint>();
        using var image = new D3D12ImageSource(() => true, bindings.Add, _ => { }, () => { });
        var published = image.TryPublish(201, 64, 48);
        Assert.True(published);
        published = image.TryPublish(201, 64, 48);
        Assert.True(published);
        Assert.Single(bindings);
        var detached = image.TryDetach();
        Assert.True(detached);
        Assert.Equal(0, image.BoundSurface);
        published = image.TryPublish(201, 64, 48);
        Assert.True(published);
        published = image.TryPublish(202, 80, 48);
        Assert.True(published);
        Assert.Equal("201,0,201,202", string.Join(',', bindings));
        Assert.Equal(202, image.BoundSurface);
    });

    /// <summary>The real owner retains one lock and old displayed content until a later timer query completes.</summary>
    [Fact]
    public Task PendingCopyFinishesThroughTimerWithoutChangingOutputTest() => StaThread.RunAsync(async () => {
        using var driver = new TransportDriver();
        driver.Native.Complete = true;
        driver.Accept(1, 11);
        var idle = driver.Service(false);
        Assert.True(idle);
        var first = driver.Image.BoundSurface;
        driver.Native.Complete = false;
        driver.Accept(2, 22);
        idle = driver.Service(false);
        Assert.False(idle);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(11, driver.Displayed);
        Assert.Equal(first, driver.Image.BoundSurface);
        Assert.Single(driver.Bindings);
        Assert.False(driver.Image.TryDetach());
        Assert.Equal(1, driver.Unlocks);
        Assert.False(driver.Native.CopiedWithoutLock);
        var submissionCalls = driver.Native.Calls.TakeLast(4).ToArray();
        Assert.Equal("copy,end,flush,poll", string.Join(',', submissionCalls));
        var overlapping = Assert.Throws<InvalidOperationException>(() => driver.Native.Bridge.BeginCopy(3, driver.Image));
        Assert.NotNull(overlapping);

        driver.StartTimer();
        await driver.Polled.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(11, driver.Displayed);
        driver.Native.Complete = true;
        await driver.Published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(22, driver.Displayed);
        Assert.Equal(first, driver.Image.BoundSurface);
        Assert.Single(driver.Bindings);
        Assert.Equal(2, driver.Unlocks);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.False(driver.Timer.IsEnabled);
        Assert.Equal(2UL, driver.Owner.SubmittedFrames);
        Assert.Equal(2UL, driver.Owner.CopiedFrames);
        Assert.Equal(2UL, driver.Owner.PublishedFrames);
        Assert.False(driver.Owner.HasPendingTransport);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.Equal(2UL, driver.Owner.CompletedFrames);
        var phases = driver.Viewport.FlightRecorder;
        phases.Stop();
        var events = phases.Snapshot().Events;
        var frame = events.Where(e => e.SubmissionSerial == 2).Select(e => e.Kind).ToArray();
        Assert.True(Array.IndexOf(frame, "lock.acquired") < Array.IndexOf(frame, "copy.begin"));
        Assert.True(Array.IndexOf(frame, "copy.ready") < Array.IndexOf(frame, "dirty.begin"));
        Assert.True(Array.IndexOf(frame, "dirty.done") < Array.IndexOf(frame, "unlock.done"));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "flightrecorder-stable-events.json"),
            System.Text.Json.JsonSerializer.Serialize(events, JsonOptions));
    });

    /// <summary>Lock misses and publication failures retain accepted serials and the exact ready output for retry.</summary>
    [Fact]
    public Task MissThenDirtyFailureRetriesWithoutAnotherCopyTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Native.Complete = true;
        driver.Accept(1, 11);
        _ = driver.Service(false);
        var first = driver.Image.BoundSurface;
        driver.Accept(2, 22);
        driver.LockAvailable = false;
        for (var retry = 0; retry < 3; retry++) {
            var idle = driver.Service(false);
            Assert.False(idle);
            Assert.Equal(1, driver.Native.Copies);
            Assert.Equal(11, driver.Displayed);
            Assert.False(driver.Image.IsWriteLocked);
        }
        driver.LockAvailable = true;
        driver.FailDirty = true;
        Assert.Throws<InvalidOperationException>(() => driver.Service(false));
        Assert.Equal(2UL, driver.Native.Bridge.ReadySerial);
        Assert.Equal(first, driver.Native.Bridge.ReadySurface);
        Assert.Equal(1UL, driver.Owner.PublishedFrames);
        Assert.False(driver.Image.IsWriteLocked);
        driver.LockAvailable = false;
        var blocked = driver.Service(false);
        Assert.False(blocked);
        Assert.Equal(2UL, driver.Native.Bridge.ReadySerial);
        driver.LockAvailable = true;
        driver.FailDirty = false;
        var finished = driver.Service(false);
        Assert.True(finished);
        Assert.Equal(2, driver.Native.Copies);
        Assert.Equal(22, driver.Displayed);
        Assert.Equal(first, driver.Image.BoundSurface);
        Assert.Single(driver.Bindings);
        Assert.Equal(2UL, driver.Owner.PublishedFrames);
    });

    /// <summary>A post-decrement Unlock failure retains the ready serial for a copy-free publication retry.</summary>
    [Fact]
    public Task UnlockFailureRetriesReadyWithoutAnotherCopyTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Native.Complete = true;
        driver.Accept(1, 11);
        driver.FailUnlock = true;
        Assert.Throws<InvalidOperationException>(() => driver.Service(false));
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(1UL, driver.Native.Bridge.ReadySerial);
        Assert.Equal(0UL, driver.Owner.PublishedFrames);
        driver.FailUnlock = false;
        var finished = driver.Service(false);
        Assert.True(finished);
        Assert.Equal(1, driver.Native.Copies);
        Assert.Equal(1UL, driver.Owner.PublishedFrames);
        Assert.Equal(2, driver.Unlocks);
    });

    /// <summary>The shipped WPF Unlock decrements before invoking a throwing Changed subscriber.</summary>
    [Fact]
    public Task RealUnlockChangedFailureBalancesLeaseTest() => StaThread.RunAsync(() => {
        using var image = new D3D12ImageSource();
        var acquired = image.TryBeginWrite();
        Assert.True(acquired);
        Write(image.NativeImage, "_isChangePending", true);
        EventHandler fail = (_, _) => throw new InvalidOperationException("Controlled Changed failure");
        image.NativeImage.Changed += fail;
        Assert.Throws<InvalidOperationException>(image.EndWrite);
        image.NativeImage.Changed -= fail;
        Assert.False(image.IsWriteLocked);
        var count = Read<uint>(image.NativeImage, "_lockCount");
        Assert.Equal(0u, count);
        var retried = image.TryBeginWrite();
        Assert.True(retried);
        image.EndWrite();
    });

    /// <summary>Final disposal retains pending native ownership and its write lease until a nonwaiting retry.</summary>
    [Fact]
    public Task DisposalRetainsLeaseUntilCopyCompletesTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        var completed = driver.Owner.TryFinishDisposalPreparation();
        Assert.False(completed);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.False(driver.Native.Bridge.IsDisposed);
        Assert.Empty(driver.Bindings);
        driver.Native.Complete = true;
        completed = driver.Owner.TryFinishDisposalPreparation();
        Assert.True(completed);
        Assert.True(driver.Native.Bridge.IsDisposed);
        Assert.True(driver.Native.Releases > 0);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Image.BoundSurface);
    });

    /// <summary>A direct canvas/image disposal cannot unbalance an outstanding copy lease.</summary>
    [Fact]
    public Task DirectDisposalCannotUnlockPendingCopyTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        var disposed = driver.Image.TryDispose();
        Assert.False(disposed);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Unlocks);
        driver.Native.Complete = true;
        driver.ReleaseNativeResources();
        disposed = driver.Image.TryDispose();
        Assert.True(disposed);
        Assert.False(driver.Image.IsWriteLocked);
    });

    /// <summary>The image lease cannot authorize writes or be released on another thread.</summary>
    [Fact]
    public Task WriteLeaseRemainsDispatcherBoundTest() => StaThread.RunAsync(async () => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        await Task.Run(() => {
            Assert.Throws<InvalidOperationException>(() => driver.Image.EndWrite());
            Assert.Throws<InvalidOperationException>(() => driver.Native.Bridge.BeginCopy(2, driver.Image));
        });
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Unlocks);
    });

    /// <summary>Retirement waits for copy completion, detaches, and releases the old generation in order.</summary>
    /// <param name="reason">The existing owner trigger; actual detection is covered by neighbor regressions.</param>
    [Theory]
    [InlineData("canvas-parameters")]
    [InlineData("manager-changed")]
    [InlineData("manager-retirement")]
    public Task RecreationDrainsBeforeDetachingTest(string reason) => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Native.Complete = true;
        driver.Accept(1, 11);
        _ = driver.Service(false);
        driver.Native.Complete = false;
        driver.Accept(2, 22);
        _ = driver.Service(false);
        driver.Owner.RequestRecreation(reason);
        var idle = driver.Service(false);
        Assert.False(idle);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Single(driver.Bindings);
        Assert.Equal(0, driver.Native.Releases);
        driver.Native.Complete = true;
        idle = driver.Service(false);
        Assert.True(idle);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Image.BoundSurface);
        Assert.True(driver.Native.Bridge.IsDisposed);
        Assert.False(driver.Owner.IsInitialized);
        Assert.Equal(1UL, driver.Owner.PublishedFrames);
        Assert.Equal(11, driver.Displayed);
        Assert.Equal(2, driver.Native.Copies);
        Assert.Equal(0, driver.Bindings.Last());
    });

    /// <summary>D3D12 removal cannot abandon a still-live D3D11 write; confirmed D3D11 removal can.</summary>
    [Fact]
    public Task DeviceRemovalRetainsLiveCopyThenReleasesLeaseTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        var retire = typeof(D3D12WpfPresentation).GetMethod("TryRetireRemovedDevice", Flags)!
            .CreateDelegate<Func<bool>>(driver.Owner);
        var retired = retire();
        Assert.False(retired);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Native.Releases);
        driver.Native.Removal11 = unchecked((int) 0x887A0005);
        driver.Native.QueryFailure = driver.Native.Removal11;
        retired = retire();
        Assert.True(retired);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.True(driver.Native.Bridge.IsDisposed);
        Assert.False(driver.Owner.IsInitialized);
        Assert.Equal(0UL, driver.Owner.PublishedFrames);
    });

    /// <summary>A live-device timeout retains resources and can be retired after its query actually completes.</summary>
    [Fact]
    public Task TimeoutDoesNotUnlockOrReleaseLiveCopyTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        Write(driver.Native.Bridge, "copyStarted", System.Diagnostics.Stopwatch.GetTimestamp() -
            6 * System.Diagnostics.Stopwatch.Frequency);
        Assert.Throws<TimeoutException>(() => driver.Service(false));
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Native.Releases);
        driver.Native.Complete = true;
        driver.Owner.RequestRecreation();
        _ = driver.Service(false);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.True(driver.Native.Bridge.IsDisposed);
    });

    /// <summary>Source completion precedes write acquisition; S_OK with a false BOOL still retains the write.</summary>
    [Fact]
    public Task FenceAndQueryBothGatePublicationTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        Write(driver.Owner, "fence", driver.Native.Fence);
        Write(driver.Owner, "pendingFence", 1UL);
        Write(driver.Owner, "pendingScene", true);
        Write(driver.Owner, "<CompletedFrames>k__BackingField", 0UL);
        Write(driver.Owner, "stageStarted", System.Diagnostics.Stopwatch.GetElapsedTime(0));
        var idle = driver.Service(false);
        Assert.False(idle);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Native.Copies);
        driver.Native.FenceValue = 1;
        driver.Native.QuerySuccessWithoutCompletion = true;
        idle = driver.Service(false);
        Assert.False(idle);
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(1, driver.Native.Copies);
        Assert.Equal(0, driver.Unlocks);
        Assert.Equal(1UL, driver.Owner.CompletedFrames);
        driver.Native.QuerySuccessWithoutCompletion = false;
        driver.Native.Complete = true;
        idle = driver.Service(false);
        Assert.True(idle);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(11, driver.Displayed);
        Assert.Equal(1, driver.Unlocks);
    });

    /// <summary>An unconfirmed native failure retains the live-device write until controlled retry completes.</summary>
    [Fact]
    public Task QueryFailureDoesNotReleaseLiveWriteTest() => StaThread.RunAsync(() => {
        using var driver = new TransportDriver();
        driver.Accept(1, 11);
        _ = driver.Service(false);
        driver.Native.QueryFailure = unchecked((int) 0x80004005);
        Assert.Throws<COMException>(() => driver.Service(false));
        Assert.True(driver.Image.IsWriteLocked);
        Assert.Equal(0, driver.Native.Releases);
        Assert.Equal(0, driver.Unlocks);
        driver.Native.QueryFailure = 0;
        driver.Native.Complete = true;
        var idle = driver.Service(false);
        Assert.True(idle);
        Assert.False(driver.Image.IsWriteLocked);
        Assert.Equal(11, driver.Displayed);
    });

    /// <summary>Runs the actual owner service and timer with only COM/image calls controlled by the fixture.</summary>
    private sealed class TransportDriver : IDisposable {
        /// <summary>Owns inert native callbacks.</summary>
        internal NativeTransport Native { get; } = new();
        /// <summary>Retains the unshown canvas.</summary>
        internal DPFCanvas Canvas { get; } = new();
        /// <summary>Retains productive demand and recorder state.</summary>
        internal Viewport3DX Viewport { get; } = new();
        /// <summary>Owns the actual presentation state machine.</summary>
        internal D3D12WpfPresentation Owner { get; }
        /// <summary>Retains the image owner with controlled native calls.</summary>
        internal D3D12ImageSource Image { get; }
        /// <summary>Calls the real service without recording an extra scene.</summary>
        internal Func<bool, bool> Service { get; }
        /// <summary>Gets the productive timer and callback.</summary>
        internal DispatcherTimer Timer => Read<DispatcherTimer>(Owner, "serviceTimer");
        /// <summary>Tracks successful SetBackBuffer calls.</summary>
        internal List<nint> Bindings { get; } = [];
        /// <summary>Controls write availability.</summary>
        internal bool LockAvailable = true;
        /// <summary>Controls a retryable dirty failure after copy completion.</summary>
        internal bool FailDirty;
        /// <summary>Models a failure after WPF has decremented its native lock count.</summary>
        internal bool FailUnlock;
        /// <summary>Counts balanced native unlocks.</summary>
        internal int Unlocks;
        /// <summary>Models the last completed content delivered at DirtyRect.</summary>
        internal int Displayed;
        /// <summary>Observes autonomous polling while incomplete.</summary>
        internal TaskCompletionSource Polled { get; } = new();
        /// <summary>Observes successful dirtying from a later timer tick.</summary>
        internal TaskCompletionSource Published { get; } = new();
        /// <summary>Substitutes no scheduler, state transition or bridge method.</summary>
        internal TransportDriver() {
            Canvas.ImageSource.Dispose();
            Image = new D3D12ImageSource(() => LockAvailable, Bindings.Add, _ => {
                if (FailDirty) throw new InvalidOperationException("Controlled dirty failure");
                Displayed = Native.OutputContent;
                if (Displayed == 22) Published.TrySetResult();
            }, () => {
                Unlocks++;
                if (FailUnlock) throw new InvalidOperationException("Controlled post-decrement Unlock failure");
            });
            Native.Image = Image;
            Native.OnPoll = () => { if (!Native.Complete) Polled.TrySetResult(); };
            Write(Canvas, "<ImageSource>k__BackingField", Image);
            Owner = Viewport.InitializeWpfPresentation(Canvas);
            Viewport.StopRendering();
            Write(Owner, "device", Native.Device);
            Write(Owner, "bridge", Native.Bridge);
            Write(Owner, "width", 64u);
            Write(Owner, "height", 48u);
            Write(Owner, "nativeHandle", (nint) 7);
            Write(Owner, "<Generation>k__BackingField", 1UL);
            Write(Canvas, "<IsActive>k__BackingField", true);
            Write(Canvas, "<NativeHandle>k__BackingField", (nint) 7);
            Write(Canvas, "<PixelWidth>k__BackingField", 64u);
            Write(Canvas, "<PixelHeight>k__BackingField", 48u);
            Service = typeof(D3D12WpfPresentation).GetMethod("ServiceTransportCore", Flags)!
                .CreateDelegate<Func<bool, bool>>(Owner);
            Viewport.FlightRecorder.Start();
        }
        /// <summary>Seeds an accepted, fence-completed frame without executing a scene or queue.</summary>
        internal void Accept(ulong serial, int content) {
            Native.SourceContent = content;
            Write(Owner, "transportSerial", serial);
            Write(Owner, "<SubmittedFrames>k__BackingField", serial);
            Write(Owner, "<CompletedFrames>k__BackingField", serial);
        }
        /// <summary>Lets the existing owner decide whether its real transport timer should run.</summary>
        internal void StartTimer() => typeof(D3D12WpfPresentation).GetMethod("UpdateServiceTimer", Flags)!.Invoke(Owner, null);
        /// <summary>Completes controlled copy work before clearing inert native references for actual owner cleanup.</summary>
        internal void ReleaseNativeResources() {
            if (!Owner.IsInitialized) return;
            Native.Complete = true;
            var endWrite = typeof(D3D12WpfPresentation).GetMethod("TryEndCopyWrite", Flags)!
                .CreateDelegate<Func<bool>>(Owner);
            var ended = endWrite();
            Assert.True(ended);
            Native.Bridge.ConfirmDetached();
            Native.Bridge.Dispose();
            Native.Device.Dispose();
            Write(Owner, "fence", null);
            Write(Owner, "pendingFence", 0UL);
            Write(Owner, "bridge", null);
            Write(Owner, "device", null);
            Write(Owner, "transportSerial", 0UL);
        }
        /// <summary>Never passes fake COM allocations to production release.</summary>
        public void Dispose() {
            Timer.Stop();
            ReleaseNativeResources();
            Viewport.Dispose();
            Canvas.Dispose();
            Native.Dispose();
        }
    }

    /// <summary>Locates existing private managed state without introducing a production state model.</summary>
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    /// <summary>Seeds an existing native boundary for deterministic CPU execution.</summary>
    private static void Write(object owner, string name, object? value) =>
        owner.GetType().GetField(name, Flags)!.SetValue(owner, value);
    /// <summary>Reads retained production state.</summary>
    private static T Read<T>(object owner, string name) => (T) owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

    /// <summary>Replaces only native calls; copy selection, polling and acknowledgments run in the real bridge.</summary>
    private sealed unsafe class NativeTransport : IDisposable {
        /// <summary>Identifies the serialized fixture to unmanaged callbacks.</summary>
        private static NativeTransport current = null!;
        /// <summary>Owns a COM table large enough for all used D3D11 context and device methods.</summary>
        private readonly void** table = (void**) NativeMemory.AllocZeroed(128, (nuint) sizeof(nint));
        /// <summary>Owns the inert COM identity used at native boundaries.</summary>
        private readonly void** instance = (void**) NativeMemory.AllocZeroed(5, (nuint) sizeof(nint));
        /// <summary>Gets the real device wrapper, without creating a native device.</summary>
        internal SilkD3D12Device Device { get; } = (SilkD3D12Device) RuntimeHelpers.GetUninitializedObject(typeof(SilkD3D12Device));
        /// <summary>Gets the real bridge with native allocation replaced by callbacks.</summary>
        internal D3D12ImageInteropBridge Bridge { get; } =
            (D3D12ImageInteropBridge) RuntimeHelpers.GetUninitializedObject(typeof(D3D12ImageInteropBridge));
        /// <summary>Controls query completion independently of submission.</summary>
        internal bool Complete;
        /// <summary>Counts actual calls through CopyResource.</summary>
        internal int Copies;
        /// <summary>Retains the image lease observed at native submission.</summary>
        internal D3D12ImageSource? Image;
        /// <summary>Records any violation at the native copy boundary.</summary>
        internal bool CopiedWithoutLock;
        /// <summary>Models different source contents without reading GPU pixels.</summary>
        internal int SourceContent;
        /// <summary>Retains the submitted content until its query completes.</summary>
        private int pendingContent;
        /// <summary>Models completed output content.</summary>
        internal int OutputContent;
        /// <summary>Records the actual native call order.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Observes query calls without replacing production polling.</summary>
        internal Action? OnPoll;
        /// <summary>Counts calls through production COM cleanup.</summary>
        internal int Releases;
        /// <summary>Controls only the D3D11 removal HRESULT.</summary>
        internal int Removal11;
        /// <summary>Controls an infrastructure query failure independently of copy completion.</summary>
        internal int QueryFailure;
        /// <summary>Models S_OK without the event-query completion BOOL.</summary>
        internal bool QuerySuccessWithoutCompletion;
        /// <summary>Controls the completed source fence without a GPU queue.</summary>
        internal ulong FenceValue;
        /// <summary>Retains the real fence wrapper with a controlled native completed-value call.</summary>
        internal SilkD3D12Fence Fence { get; } =
            (SilkD3D12Fence) RuntimeHelpers.GetUninitializedObject(typeof(SilkD3D12Fence));
        /// <summary>Constructs no window, adapter, queue or GPU resource.</summary>
        internal NativeTransport() {
            current = this;
            for (var index = 0; index < 5; index++) instance[index] = table;
            table[2] = (void*) (delegate* unmanaged[Stdcall]<void*, uint>) &Release;
            table[8] = (void*) (delegate* unmanaged[Stdcall]<void*, ulong>) &GetCompletedValue;
            table[28] = (void*) (delegate* unmanaged[Stdcall]<void*, void*, void>) &End;
            table[29] = (void*) (delegate* unmanaged[Stdcall]<void*, void*, int*, uint, uint, int>) &GetData;
            table[37] = (void*) (delegate* unmanaged[Stdcall]<void*, int>) &Removed;
            table[39] = (void*) (delegate* unmanaged[Stdcall]<void*, int>) &Removed11;
            table[47] = (void*) (delegate* unmanaged[Stdcall]<void*, void*, void*, void>) &Copy;
            table[111] = (void*) (delegate* unmanaged[Stdcall]<void*, void>) &Flush;
            Write(Device, "nativeDevice", new ComPtr<D12.ID3D12Device> { Handle = (D12.ID3D12Device*) instance });
            Write(Fence, "nativeFence", new ComPtr<D12.ID3D12Fence> { Handle = (D12.ID3D12Fence*) instance });
            var trackerType = typeof(SilkD3D12Resource).Assembly.GetType("HelixToolkit.SharpDX.Core.Native.D3D12ResourceStateTracker")!;
            var tracker = Activator.CreateInstance(trackerType, true)!;
            trackerType.GetMethod("Track")!.Invoke(tracker, [(nint) instance, D12.ResourceStates.Common]);
            var source = (SilkD3D12Resource) RuntimeHelpers.GetUninitializedObject(typeof(SilkD3D12Resource));
            Write(source, "stateTracker", tracker);
            Write(source, "nativeResource", new ComPtr<D12.ID3D12Resource> { Handle = (D12.ID3D12Resource*) instance });
            Write(Bridge, "device12", Device);
            Write(Bridge, "source12", source);
            Write(Bridge, "ownerThread", Environment.CurrentManagedThreadId);
            Write(Bridge, "boundSlot", -1);
            Write(Bridge, "readySlot", -1);
            Write(Bridge, "lastPublishedSlot", -1);
            Write(Bridge, "device11", new ComPtr<D11.ID3D11Device> { Handle = (D11.ID3D11Device*) instance });
            Write(Bridge, "context11", new ComPtr<D11.ID3D11DeviceContext> { Handle = (D11.ID3D11DeviceContext*) instance });
            Write(Bridge, "source11", new ComPtr<D11.ID3D11Texture2D> { Handle = (D11.ID3D11Texture2D*) instance });
            Write(Bridge, "copyQuery11", new ComPtr<D11.ID3D11Query> { Handle = (D11.ID3D11Query*) instance });
            Write(Bridge, "destinations11", new ComPtr<D11.ID3D11Texture2D>[] {
                new() { Handle = (D11.ID3D11Texture2D*) (instance + 1) }
            });
            Write(Bridge, "surfaces9", new ComPtr<D9.IDirect3DSurface9>[] {
                new() { Handle = (D9.IDirect3DSurface9*) (instance + 3) }
            });
            Write(Bridge, "textures9", new ComPtr<D9.IDirect3DTexture9>[1]);
        }
        /// <summary>Models a live device.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int Removed(void* self) => 0;
        /// <summary>Returns the controlled source completion value.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static ulong GetCompletedValue(void* self) => current.FenceValue;
        /// <summary>Models independently confirmed D3D11 removal.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int Removed11(void* self) => current.Removal11;
        /// <summary>Records actual native ownership release without a native device.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint Release(void* self) { current.Releases++; return 0; }
        /// <summary>Records a queued copy without executing GPU work.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static void Copy(void* self, void* destination, void* source) {
            current.Copies++;
            current.Calls.Add("copy");
            current.pendingContent = current.SourceContent;
            current.CopiedWithoutLock |= current.Image is not null && !current.Image.IsWriteLocked;
        }
        /// <summary>Accepts the production event-query submission.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static void End(void* self, void* query) => current.Calls.Add("end");
        /// <summary>Accepts the production flush without completing the query.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static void Flush(void* self) => current.Calls.Add("flush");
        /// <summary>Returns a controlled nonwaiting query result.</summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int GetData(void* self, void* query, int* data, uint size, uint flags) {
            current.Calls.Add("poll");
            current.OnPoll?.Invoke();
            if (current.QueryFailure < 0) return current.QueryFailure;
            if (current.Complete) current.OutputContent = current.pendingContent;
            *data = current.Complete ? 1 : 0;
            return current.Complete || current.QuerySuccessWithoutCompletion ? 0 : 1;
        }
        /// <summary>Frees only fixture memory; fake COM identities must never reach production release.</summary>
        public void Dispose() {
            NativeMemory.Free(instance);
            NativeMemory.Free(table);
        }
    }
}
