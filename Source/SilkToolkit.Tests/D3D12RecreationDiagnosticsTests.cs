using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks actual request decisions and bounded context without a window, native device or surface.</summary>
[Collection(WpfCollection.Name)]
public sealed class D3D12RecreationDiagnosticsTests {
    /// <summary>Matches the consumer's camelCase contract without creating per-operation serializer options.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Repeated lifecycle checks with unchanged parameters or no HWND never request native destruction.</summary>
    /// <param name="active">The controlled canvas activity.</param>
    /// <param name="hasHandle">Whether a current source identity exists.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public Task UnchangedOrMissingSourceDoesNotRequestRecreationTest(bool active, bool hasHandle) =>
        StaThread.RunAsync(() => {
            using var driver = new RequestDriver();
            Write(driver.Canvas, "<IsActive>k__BackingField", active);
            if (!hasHandle) Write(driver.Canvas, "<NativeHandle>k__BackingField", (nint) 0);
            driver.Viewport.FlightRecorder.Start();

            driver.Detect();
            driver.Detect();
            driver.Viewport.FlightRecorder.Stop();
            var data = driver.Viewport.FlightRecorder.Snapshot();
            Assert.False(Read<bool>(driver.Owner, "recreateRequested"));
            Assert.Empty(data.Events);
            Assert.Empty(data.Frames);
            Assert.Equal(1UL, driver.Owner.Generation);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        });

    /// <summary>Logs the actual allocation and observed canvas once, without adding recovery or scene demand.</summary>
    /// <param name="newWidth">The observed width, including transient empty layout.</param>
    /// <param name="newHeight">The observed height.</param>
    /// <param name="newHandle">A managed identity token, never passed to a native API.</param>
    [Theory]
    [InlineData(80u, 48u, 7)]
    [InlineData(64u, 60u, 7)]
    [InlineData(64u, 48u, 8)]
    [InlineData(0u, 48u, 7)]
    public Task ChangedCanvasRecordsFirstRequestWithoutDemandTest(uint newWidth, uint newHeight, int newHandle) =>
        StaThread.RunAsync(() => {
            using var driver = new RequestDriver();
            Write(driver.Canvas, "<PixelWidth>k__BackingField", newWidth);
            Write(driver.Canvas, "<PixelHeight>k__BackingField", newHeight);
            Write(driver.Canvas, "<NativeHandle>k__BackingField", (nint) newHandle);
            driver.Viewport.FlightRecorder.Start();

            driver.Detect();
            driver.Detect();
            driver.Viewport.FlightRecorder.Stop();
            var data = driver.Viewport.FlightRecorder.Snapshot();
            var entry = Assert.Single(data.Events);
            var state = Assert.IsType<FlightRecreationState>(entry.Recreation);
            Assert.Equal("recreate.request", entry.Kind);
            Assert.Equal("canvas-parameters", state.Reason);
            Assert.Equal(64u, state.OldWidth);
            Assert.Equal(48u, state.OldHeight);
            Assert.Equal(newWidth, state.NewWidth);
            Assert.Equal(newHeight, state.NewHeight);
            Assert.Equal(newHandle != 7, state.HandleChanged);
            Assert.True(state.HasNativeHandle);
            Assert.True(state.CanvasActive);
            Assert.False(state.CanvasLoaded);
            Assert.False(state.CanvasVisible);
            Assert.Equal(1f, state.DpiScale);
            Assert.Equal(1UL, entry.Generation);
            Assert.Equal(17UL, entry.SubmissionSerial);
            Assert.NotEqual(Guid.Empty, entry.PresentationOwner);
            Assert.Null(entry.Publication);
            Assert.Empty(data.Frames);
            Assert.True(Read<bool>(driver.Owner, "recreateRequested"));
            Assert.False(driver.Viewport.RenderDemand.HasRecoveryDemand);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(1UL, driver.Owner.Generation);
        });

    /// <summary>A transient resize already observed by the owner stays pending even if dimensions return.</summary>
    [Fact]
    public Task ObservedEmptySizeRequestRemainsPendingAfterRestorationTest() => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        Write(driver.Canvas, "<PixelWidth>k__BackingField", 0u);
        driver.Viewport.FlightRecorder.Start();
        driver.Detect();
        Write(driver.Canvas, "<PixelWidth>k__BackingField", 64u);
        driver.Detect();
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        var entry = Assert.Single(data.Events);
        Assert.Equal(0u, entry.Recreation!.Value.NewWidth);
        Assert.True(Read<bool>(driver.Owner, "recreateRequested"));
        Assert.Equal(1UL, driver.Owner.Generation);
    });

    /// <summary>Changes restored before the existing coalesced detection require no generation replacement.</summary>
    [Fact]
    public Task EmptySizeRestoredBeforeDetectionDoesNotRequestRecreationTest() => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        driver.Viewport.FlightRecorder.Start();
        Write(driver.Canvas, "<PixelWidth>k__BackingField", 0u);
        Write(driver.Canvas, "<PixelWidth>k__BackingField", 64u);
        driver.Detect();
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        Assert.Empty(data.Events);
        Assert.False(Read<bool>(driver.Owner, "recreateRequested"));
    });

    /// <summary>DPI alone requests the existing scene update when physical dimensions and HWND are unchanged.</summary>
    [Fact]
    public Task DpiOnlyChangeDoesNotRequestRecreationTest() => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        driver.Viewport.FlightRecorder.Start();
        Write(driver.Canvas, "<EffectiveDpiScale>k__BackingField", 1.5f);
        driver.Detect();
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        Assert.Empty(data.Events);
        Assert.False(Read<bool>(driver.Owner, "recreateRequested"));
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Manager/explicit boundaries preserve their existing recovery request and coalesce diagnostics.</summary>
    /// <param name="reason">The known constant forwarded by an existing lifecycle caller.</param>
    [Theory]
    [InlineData("manager-retirement")]
    [InlineData("manager-changed")]
    [InlineData("manager-disposing")]
    [InlineData("manager-reinitialized")]
    public Task ExplicitRequestsRetainReasonAndRecoveryTest(string reason) => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        driver.Viewport.FlightRecorder.Start();
        driver.Owner.RequestRecreation(reason);
        driver.Owner.RequestRecreation(reason);
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        var entry = Assert.Single(data.Events);
        Assert.Equal(reason, entry.Recreation!.Value.Reason);
        Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
        Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.Equal(0UL, driver.Owner.SubmittedFrames);
    });

    /// <summary>Initial setup and inactive recording do not append request events or create recording buffers.</summary>
    [Fact]
    public Task InitialAndDisabledRequestsAreSilentTest() => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        driver.Owner.RequestRecreation();
        Assert.False(driver.Viewport.FlightRecorder.HasRecording);
        Write(driver.Owner, "recreateRequested", false);
        Write(driver.Owner, "<Generation>k__BackingField", 0UL);
        driver.Viewport.FlightRecorder.Start();
        driver.Owner.RequestRecreation();
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        Assert.Empty(data.Events);
        Assert.Empty(data.Frames);
    });

    /// <summary>A retired generation number alone does not make empty resource setup another destruction.</summary>
    [Fact]
    public Task ReleasedResourcesDoNotRecordAnotherRecreationTest() => StaThread.RunAsync(() => {
        using var driver = new RequestDriver();
        Write(driver.Owner, "bridge", null);
        driver.Viewport.FlightRecorder.Start();
        driver.Owner.RequestRecreation("manager-reinitialized");
        driver.Viewport.FlightRecorder.Stop();

        var data = driver.Viewport.FlightRecorder.Snapshot();
        Assert.Empty(data.Events);
        Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
    });

    /// <summary>The additive nullable property keeps the existing event constructor and JSON fields intact.</summary>
    [Fact]
    public void RecreationSerializesAsOptionalSchemaOneContextTest() {
        var oldEvent = new FlightEvent(1, 2, 17, "published", 0, null, Guid.NewGuid());
        var state = new FlightRecreationState("canvas-parameters", 64, 48, 80, 48, false, true,
            true, true, true, 1);
        var newEvent = oldEvent with { Kind = "recreate.request", Recreation = state };
        var json = JsonSerializer.Serialize(new[] { oldEvent, newEvent }, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var oldJson = document.RootElement[0];
        var newJson = document.RootElement[1];
        Assert.Null(oldEvent.Recreation);
        Assert.Equal(JsonValueKind.Null, oldJson.GetProperty("recreation").ValueKind);
        Assert.Equal(17UL, newJson.GetProperty("submissionSerial").GetUInt64());
        Assert.Equal("canvas-parameters", newJson.GetProperty("recreation").GetProperty("reason").GetString());
        Assert.Equal(80u, newJson.GetProperty("recreation").GetProperty("newWidth").GetUInt32());
        Assert.Equal(oldJson.GetProperty("presentationOwner").GetGuid(),
            newJson.GetProperty("presentationOwner").GetGuid());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "flightrecorder-recreation-events.json"), json);
    }

    /// <summary>Reads an existing field without running native work.</summary>
    private static T Read<T>(object owner, string name) =>
        (T) owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Seeds managed state only; no fake pointer is bound, rendered or released.</summary>
    private static void Write(object owner, string name, object? value) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    /// <summary>Uses the actual detector with an inert presence sentinel instead of creating a native bridge.</summary>
    private sealed class RequestDriver : IDisposable {
        /// <summary>The unshown managed canvas.</summary>
        internal DPFCanvas Canvas { get; } = new();
        /// <summary>The actual viewport and bounded recorder.</summary>
        internal Viewport3DX Viewport { get; } = new();
        /// <summary>The owner whose decision and observation run unchanged.</summary>
        internal D3D12WpfPresentation Owner { get; }
        /// <summary>The actual detector, never the native retirement consumer.</summary>
        internal Action Detect { get; }

        /// <summary>Seeds only values read by detection; no source HWND, device or D3DImage surface is created.</summary>
        internal RequestDriver() {
            Owner = Viewport.InitializeWpfPresentation(Canvas);
            Viewport.StopRendering();
            while (Viewport.RenderDemand.TryBeginFrame(out _)) Viewport.RenderDemand.CompleteFrame(true);
            _ = Viewport.RenderDemand.TryTakeRecoveryRequest();
            Viewport.PublicationObservation.Correlate(1, 17);
            Write(Owner, "<Generation>k__BackingField", 1UL);
            Write(Owner, "nativeHandle", (nint) 7);
            Write(Owner, "width", 64u);
            Write(Owner, "height", 48u);
            Write(Owner, "dpiScale", 1f);
            Write(Canvas, "<NativeHandle>k__BackingField", (nint) 7);
            Write(Canvas, "<PixelWidth>k__BackingField", 64u);
            Write(Canvas, "<PixelHeight>k__BackingField", 48u);
            Write(Canvas, "<IsActive>k__BackingField", true);
            // Detection reads only bridge presence. Never call or dispose this uninitialized sentinel.
            Write(Owner, "bridge", RuntimeHelpers.GetUninitializedObject(typeof(D3D12ImageInteropBridge)));
            Detect = typeof(D3D12WpfPresentation)
                .GetMethod("DetectPresentationChanges", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action>(Owner);
        }

        /// <inheritdoc />
        public void Dispose() {
            Write(Owner, "bridge", null);
            Viewport.Dispose();
            Canvas.Dispose();
        }
    }
}
