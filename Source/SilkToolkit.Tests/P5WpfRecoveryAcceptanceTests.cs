using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks real WPF recovery output with explicitly scoped synthetic and native-window evidence.</summary>
/// <param name="output">Receives the exercised recovery seam, real counters and evidence limitations.</param>
[Collection(WpfCollection.Name)]
public sealed class P5WpfRecoveryAcceptanceTests(ITestOutputHelper output)
{
    /// <summary>Changes the actual D3DImage property and event synthetically, retaining output and unavailable demand.</summary>
    /// <remarks>
    /// Uses WPF's private read-only property key, not a physical front-buffer loss, monitor change or RDP session.
    /// If that runtime seam is absent, this case skips; the separate direct-owner callback case remains narrower evidence.
    /// </remarks>
    /// <returns>The explicit hardware verification using genuine scene, copy and WPF publication resources.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task SyntheticD3DImageAvailabilityRebindsRetainedOutputAndPreservesUnavailableDemand() =>
        StaThread.RunAsync(async () =>
        {
            var key = typeof(D3DImage).GetField(
                "IsFrontBufferAvailablePropertyKey", BindingFlags.Static | BindingFlags.NonPublic
            )?.GetValue(null) as DependencyPropertyKey;
            if (key is null)
            {
                Assert.Skip("WPF's read-only availability key is unavailable. " +
                    "DirectOwnerFrontBufferCallbackRebindsWithoutSceneOrCopyWork covers only the owner callback.");
                return;
            }

            Assert.Same(D3DImage.IsFrontBufferAvailableProperty, key.DependencyProperty);
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            await driver.WarmAsync();
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
            var originalAvailability = image.IsFrontBufferAvailable;
            Assert.True(originalAvailability);
            Assert.Same(image, Assert.IsType<Image>(Assert.Single(canvas.Children.Cast<UIElement>())).Source);
            var original = driver.Snapshot();
            P4PresentationDriver.AssertPixel(original, 32, 24, 0, 255, 0, 255);
            var before = driver.Counts;
            var milestones = Milestones(owner);
            var copies = bridge.CopyCount;
            var rebound = owner.ReboundFrames;
            var surface = canvas.ImageSource.BoundSurface;
            var generation = owner.Generation;
            var context = owner.RenderContext;
            var node = Assert.IsType<P4ObservedMeshNode>(mesh.SceneNode);
            var updates = 0;
            node.OnNextUpdate = _ => updates++;
            var transitions = new List<(bool Old, bool New)>();
            DependencyPropertyChangedEventHandler changed = (sender, args) =>
            {
                Assert.Same(image, sender);
                Assert.Same(D3DImage.IsFrontBufferAvailableProperty, args.Property);
                transitions.Add(((bool)args.OldValue, (bool)args.NewValue));
            };
            image.IsFrontBufferAvailableChanged += changed;
            try
            {
                // The actual property callback and subscription chain run, but native availability is not disturbed.
                image.SetValue(key, false);
                Assert.False(image.IsFrontBufferAvailable);
                Assert.False(canvas.ImageSource.IsFrontBufferAvailable);
                Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
                for (var tick = 0; tick < 32; tick++) Assert.Equal(0, driver.Tick());
                Assert.Equal(before, driver.Counts);
                Assert.Equal(milestones, Milestones(owner));
                Assert.Equal(copies, bridge.CopyCount);
                Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                Assert.Equal(rebound, owner.ReboundFrames);
                Assert.Equal(0, updates);
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);

                image.SetValue(key, true);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await driver.DrainAsync();
                Assert.Equal(before, driver.Counts);
                Assert.Equal(milestones, Milestones(owner));
                Assert.Equal(copies, bridge.CopyCount);
                Assert.True(owner.ReboundFrames > rebound);
                Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                Assert.Equal(0, updates);
                Assert.Equal(original.Pixels, driver.Snapshot().Pixels);

                image.SetValue(key, false);
                P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                var blocked = driver.Counts;
                var blockedMilestones = Milestones(owner);
                var blockedRebound = owner.ReboundFrames;
                for (var tick = 0; tick < 32; tick++) Assert.Equal(0, driver.Tick());
                Assert.False(image.IsFrontBufferAvailable);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                Assert.Equal(blocked, driver.Counts);
                Assert.Equal(blockedMilestones, Milestones(owner));
                Assert.Equal(copies, bridge.CopyCount);
                Assert.Equal(blockedRebound, owner.ReboundFrames);
                Assert.Equal(0, updates);

                image.SetValue(key, true);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await driver.DrainAsync();
                driver.AssertAdvanced(blocked, 1);
                Assert.Equal(milestones.Copied + 1, owner.CopiedFrames);
                Assert.Equal(milestones.Published + 1, owner.PublishedFrames);
                Assert.Equal(copies + 1, bridge.CopyCount);
                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                Assert.Equal(1, updates);
                P4PresentationDriver.AssertPixel(driver.Snapshot(), 32, 24, 0, 0, 255, 255);
                Assert.Equal(new[] { (true, false), (false, true), (true, false), (false, true) }, transitions);
                driver.AssertIdleOrHidden();
                output.WriteLine($"Synthetic actual D3DImage property/events: retained rebind, then fresh red output; " +
                    $"submitted={owner.SubmittedFrames}, copied={owner.CopiedFrames}, " +
                    $"published={owner.PublishedFrames}, rebound={owner.ReboundFrames}. " +
                    "No physical monitor, native front-buffer loss or RDP proof.");
            }
            finally
            {
                try
                {
                    image.SetValue(key, originalAvailability);
                }
                finally
                {
                    image.IsFrontBufferAvailableChanged -= changed;
                    node.OnNextUpdate = null;
                }
            }
        });

    /// <summary>Exercises only the actual owner's callback when runtime availability injection cannot be used.</summary>
    /// <remarks>Does not change D3DImage availability or exercise its event, native loss, physical monitors or RDP.</remarks>
    /// <returns>The separately labelled, explicit hardware retained-image callback verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task DirectOwnerFrontBufferCallbackRebindsWithoutSceneOrCopyWork() => StaThread.RunAsync(async () =>
    {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd: false, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        await driver.WarmAsync();
        var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
        var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
        Assert.True(canvas.ImageSource.IsFrontBufferAvailable);
        var before = driver.Counts;
        var milestones = Milestones(owner);
        var rebound = owner.ReboundFrames;
        var surface = canvas.ImageSource.BoundSurface;
        var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
        var copies = bridge.CopyCount;
        var original = driver.Snapshot();
        P4PresentationDriver.AssertPixel(original, 32, 24, 0, 255, 0, 255);
        var callback = typeof(D3D12WpfPresentation).GetMethod(
            "OnFrontBufferChanged", BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(callback);
        callback.Invoke(owner, [canvas.ImageSource, EventArgs.Empty]);
        Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
        Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
        Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        await driver.DrainAsync();
        Assert.Equal(before, driver.Counts);
        Assert.Equal(milestones, Milestones(owner));
        Assert.Equal(copies, bridge.CopyCount);
        Assert.True(owner.ReboundFrames > rebound);
        Assert.Equal(surface, canvas.ImageSource.BoundSurface);
        Assert.Equal(original.Pixels, driver.Snapshot().Pixels);
        driver.AssertIdleOrHidden();
        output.WriteLine("Direct actual owner callback only: retained GPU image rebound with no scene/copy work. " +
            "D3DImage availability and its event were not changed; no native loss, monitor or RDP proof.");
    });

    /// <summary>Minimizes a visible native Window, observes idle counters and restores changed real GPU output.</summary>
    /// <remarks>Win32 IsIconic proves minimization; diagnostic WPF snapshots/readback do not prove physical display or RDP.</remarks>
    /// <returns>The explicit hardware minimize/restore acceptance with unconditional fixture and Window cleanup.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task VisibleWindowMinimizeRestoreKeepsIdleCountersAndPublishesFreshMaterialPixels() =>
        StaThread.RunAsync(async () =>
        {
            var harness = new P4WpfHarness(hiddenSource: false);
            var window = new Window
            {
                Title = "Phase 5 WPF minimize/restore acceptance",
                Content = harness.Root,
                Width = 240,
                Height = 200,
                ResizeMode = ResizeMode.CanMinimize,
                Left = 100,
                Top = 100,
                ShowInTaskbar = false
            };
            try
            {
                window.Show();
                window.UpdateLayout();
                await harness.WarmAsync();
                harness.Viewport.MinimumUpdateCount = 1;
                var owner = harness.Owner;
                var canvas = harness.Canvas;
                var handle = new WindowInteropHelper(window).Handle;
                Assert.NotEqual(nint.Zero, handle);
                Assert.False(IsIconic(handle));
                Assert.True(window.IsVisible);
                Assert.False(harness.Viewport.EnableSwapChainRendering);
                Assert.Null(harness.Viewport.D3D12Surface);
                var image = Assert.IsType<Image>(Assert.Single(canvas.Children.Cast<UIElement>()));
                Assert.Same(canvas.ImageSource.NativeImage, image.Source);
                Assert.Equal(handle, Assert.IsType<HwndSource>(PresentationSource.FromVisual(image)).Handle);
                P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Red);
                await AssertIdleIntervalAsync(harness);
                var before = Milestones(owner);

                window.WindowState = WindowState.Minimized;
                await P4PresentationDriver.PollAsync(() => IsIconic(handle), "actual Win32 Window minimization");
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await AssertIdleIntervalAsync(harness);
                Assert.Equal(before, Milestones(owner));
                Assert.True(IsIconic(handle));

                // Do not infer canvas visibility or front-buffer loss from IsIconic, or force either state.
                var mesh = Assert.Single(harness.Viewport.Items.OfType<MeshGeometryModel3D>());
                P4PresentationDriver.SetMeshColor(mesh, new Vector4(0, 0, 1, 1));
                Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
                window.WindowState = WindowState.Normal;
                await P4PresentationDriver.PollAsync(() => !IsIconic(handle), "actual Win32 Window restoration");
                window.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await harness.DrainAsync();
                Assert.Same(owner, harness.Owner);
                Assert.True(window.IsVisible);
                Assert.True(canvas.IsActive);
                Assert.Same(canvas.ImageSource.NativeImage, image.Source);
                Assert.NotEqual(nint.Zero, canvas.ImageSource.BoundSurface);
                Assert.True(owner.SubmittedFrames > before.Submitted);
                Assert.True(owner.CompletedFrames > before.Completed);
                Assert.True(owner.CopiedFrames > before.Copied);
                Assert.True(owner.PublishedFrames > before.Published);
                // Check the bound WPF image before an explicit capture can itself request another scene frame.
                P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Blue);
                var beforeCapture = owner.SubmittedFrames;
                var captured = owner.CaptureFrame();
                await harness.DrainAsync();
                Assert.Equal(beforeCapture + 1, owner.CapturedSerial);
                Assert.Equal(160U, captured.Width);
                Assert.Equal(120U, captured.Height);
                P4WpfHarness.AssertPixel(captured.Pixels, captured.Width, 80, 60, Colors.Blue);
                await AssertIdleIntervalAsync(harness);
                output.WriteLine($"Actual visible Window IsIconic false -> true -> false; idle before/during minimize " +
                    $"and after fresh blue WPF image/readback; {harness.Counters}. " +
                    "No forced availability, physical monitor, desktop pixel or RDP proof.");
            }
            finally
            {
                try
                {
                    await harness.DisposeAsync();
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            }
        });

    /// <summary>Observes idle scene/transport counters across real dispatcher opportunities without requesting work.</summary>
    /// <param name="harness">The actual template-backed viewport and owner.</param>
    /// <returns>The bounded test-only interval, also checking that demand stays drained.</returns>
    private static async Task AssertIdleIntervalAsync(P4WpfHarness harness)
    {
        Assert.False(harness.Viewport.RenderDemand.HasRenderDemand);
        var before = Milestones(harness.Owner);
        for (var tick = 0; tick < 8; tick++)
        {
            Assert.Equal(0, harness.Owner.RenderFrame(P4WpfHarness.FrameTime));
            await Task.Delay(20);
            Assert.False(harness.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(before, Milestones(harness.Owner));
        }
    }

    /// <summary>Reads genuine scene and transport milestones, excluding separately checked retained-image rebinds.</summary>
    /// <param name="owner">The productive WPF presentation owner.</param>
    /// <returns>The accepted, completed, copied and newly published frame counts.</returns>
    private static (ulong Submitted, ulong Completed, ulong Copied, ulong Published) Milestones(
        D3D12WpfPresentation owner) =>
        (owner.SubmittedFrames, owner.CompletedFrames, owner.CopiedFrames, owner.PublishedFrames);

    /// <summary>Reads the actual native minimized state without inferring it from a WPF property.</summary>
    /// <param name="window">The visible acceptance Window's HWND.</param>
    /// <returns>Whether Windows reports that HWND as iconic.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint window);
}
