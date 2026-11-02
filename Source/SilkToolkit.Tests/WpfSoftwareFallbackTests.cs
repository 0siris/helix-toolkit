using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Xunit;
using Color = System.Windows.Media.Color;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Separates synthetic unavailable publication from real GPU, WPF lock and lifetime boundaries.</summary>
/// <param name="output">Receives prerequisite boundaries and the precise scope of the pixel evidence.</param>
/// <remarks>
/// Requires the existing matching HAL D3D12/D3D11/D3D9Ex chain; WARP is not an alternative.
/// Availability injection does not remove a device or force native WPF software composition.
/// CopyBackBuffer and RenderTargetBitmap provide synchronous snapshots, not fresh asynchronous compositor pixels.
/// </remarks>
[Collection(WpfCollection.Name)]
public sealed class WpfSoftwareFallbackTests(ITestOutputHelper output) {
    /// <summary>Publishes successive real material and alpha/channel changes while managed availability stays false.</summary>
    /// <returns>The explicit HAL owner and synchronous WPF pixel regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task FreshUnavailableFramesPreservePremultipliedPixelsTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            mesh.IsTransparent = true;
            driver.Viewport.OitRenderMode = OITRenderType.None;
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var generation = owner.Generation;
                var context = owner.RenderContext;
                var first = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(first, 32, 24, 0, 255, 0, 255);
                image.SetValue(key, false);
                (Color Clear, Vector4 Material, int Blue, int Green, int Red, int Alpha)[] changes = [
                    // Half red over the half-alpha clear: source-over premultiplied BGRA, rounded once.
                    (Color.FromArgb(128, 224, 96, 32), new Vector4(1, 0, 0, 0.5f), 8, 24, 184, 192),
                    (Color.FromArgb(64, 32, 192, 128), new Vector4(0, 0, 1, 1), 255, 0, 0, 255),
                    (Color.FromArgb(0, 255, 128, 64), new Vector4(0, 1, 0, 1), 0, 255, 0, 255)
                ];
                foreach (var (clear, material, blue, green, red, alpha) in changes) {
                    var before = driver.Counts;
                    var copied = owner.CopiedFrames;
                    var published = owner.PublishedFrames;
                    var copies = bridge.CopyCount;
                    var bound = canvas.ImageSource.BoundSurface;
                    driver.Viewport.BackgroundColor = clear;
                    P4PresentationDriver.SetMeshColor(mesh, material);
                    Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                    await driver.DrainAsync();

                    Assert.False(image.IsFrontBufferAvailable);
                    Assert.True(canvas.IsActive);
                    driver.AssertAdvanced(before, 1);
                    Assert.Equal(copied + 1, owner.CopiedFrames);
                    Assert.Equal(published + 1, owner.PublishedFrames);
                    Assert.Equal(copies + 1, bridge.CopyCount);
                    Assert.NotEqual(bound, canvas.ImageSource.BoundSurface);
                    Assert.Equal(owner.SubmittedFrames, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                    Assert.Equal(owner.SubmittedFrames, P4PresentationDriver.ReadField<ulong>(bridge, "lastPublishedSerial"));
                    Assert.False(bridge.IsCopyPending);
                    Assert.False(bridge.HasReadySurface);
                    Assert.Equal(0UL, P4PresentationDriver.ReadField<ulong>(owner, "pendingFence"));
                    Assert.Equal(generation, owner.Generation);
                    Assert.Same(context, owner.RenderContext);

                    var snapshot = SnapshotBackBuffer(image);
                    P4PresentationDriver.AssertPixel(
                        snapshot, 2, 2, clear.B * clear.A / 255, clear.G * clear.A / 255,
                        clear.R * clear.A / 255, clear.A
                    );
                    P4PresentationDriver.AssertPixel(snapshot, 32, 24, blue, green, red, alpha);
                    var visualSnapshot = driver.Snapshot();
                    P4PresentationDriver.AssertPixel(
                        visualSnapshot, 2, 2, clear.B * clear.A / 255, clear.G * clear.A / 255,
                        clear.R * clear.A / 255, clear.A
                    );
                    AssertIdle(driver);
                }

                var softwareCounts = driver.Counts;
                var softwareCopied = owner.CopiedFrames;
                var softwarePublished = owner.PublishedFrames;
                var softwareCopies = bridge.CopyCount;
                var softwareSurface = canvas.ImageSource.BoundSurface;
                var softwareSnapshot = SnapshotBackBuffer(image);
                var rebound = owner.ReboundFrames;
                image.SetValue(key, true);
                await driver.DrainAsync();
                Assert.Equal(softwareCounts, driver.Counts);
                Assert.Equal(softwareCopied, owner.CopiedFrames);
                Assert.Equal(softwarePublished, owner.PublishedFrames);
                Assert.Equal(softwareCopies, bridge.CopyCount);
                Assert.Equal(softwareSurface, canvas.ImageSource.BoundSurface);
                Assert.True(owner.ReboundFrames > rebound);
                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                var hardwareReturnSnapshot = SnapshotBackBuffer(image);
                Assert.Equal(softwareSnapshot.Pixels, hardwareReturnSnapshot.Pixels);
                AssertIdle(driver);

                output.WriteLine(
                    "Fresh GPU serial/copy/publication and synchronous CopyBackBuffer/RenderTargetBitmap pixels only; " +
                    "native asynchronous software-copy freshness, scanout and the cause of native availability loss remain unproved."
                );
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Rebinds the retained completed image during false and synthetic hardware return without new scene work.</summary>
    /// <returns>The same-generation, copy-free rebind and idle regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task RetainedUnavailableRebindAndHardwareReturnStayIdleTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            var beforeFrames = 0;
            EventHandler beforeFrame = (_, _) => beforeFrames++;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                driver.Viewport.OnRendering += beforeFrame;
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var copies = bridge.CopyCount;
                var surface = canvas.ImageSource.BoundSurface;
                var serial = P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial");
                var slot = P4PresentationDriver.ReadField<int>(bridge, "boundSlot");
                var generation = owner.Generation;
                var context = owner.RenderContext;
                var original = SnapshotBackBuffer(image);
                foreach (var availability in new[] { false, true }) {
                    var rebound = owner.ReboundFrames;
                    image.SetValue(key, availability);
                    Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                    Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
                    await driver.DrainAsync();

                    Assert.Equal(availability, image.IsFrontBufferAvailable);
                    Assert.True(owner.ReboundFrames > rebound);
                    Assert.Equal(before, driver.Counts);
                    Assert.Equal(copied, owner.CopiedFrames);
                    Assert.Equal(published, owner.PublishedFrames);
                    Assert.Equal(copies, bridge.CopyCount);
                    Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                    Assert.Equal(serial, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                    Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                    Assert.Equal(generation, owner.Generation);
                    Assert.Same(context, owner.RenderContext);
                    Assert.Equal(0, beforeFrames);
                    var snapshot = SnapshotBackBuffer(image);
                    Assert.Equal(original.Pixels, snapshot.Pixels);
                    AssertIdle(driver);
                }
            } finally {
                driver.Viewport.OnRendering -= beforeFrame;
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Completes a genuinely gated GPU frame after false without letting retained rebind or later demand replace it.</summary>
    /// <returns>The real fence, query, publication-order and preserved-demand regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task CompletedFramePublishesAfterAvailabilityChangesDuringGpuWaitTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var copies = bridge.CopyCount;
                var surface = canvas.ImageSource.BoundSurface;
                var generation = owner.Generation;
                using (var gate = new P4NativeQueueGate(driver)) {
                    P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                    var draws = driver.Tick();
                    Assert.True(draws > 0);
                    image.SetValue(key, false);
                    driver.Viewport.InvalidateRender();
                    for (var tick = 0; tick < 32; tick++) {
                        var blockedDraws = driver.Tick();
                        Assert.Equal(0, blockedDraws);
                        Assert.Equal(before.Submitted + 1, owner.SubmittedFrames);
                        Assert.Equal(before.Completed, owner.CompletedFrames);
                        Assert.Equal(copied, owner.CopiedFrames);
                        Assert.Equal(published, owner.PublishedFrames);
                        Assert.Equal(copies, bridge.CopyCount);
                        Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                        Assert.False(bridge.HasReadySurface);
                        Assert.True(owner.HasPendingTransport);
                        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                    }

                    gate.Release();
                }

                await driver.FinishTransportAsync();
                Assert.False(image.IsFrontBufferAvailable);
                driver.AssertAdvanced(before, 1);
                Assert.Equal(copied + 1, owner.CopiedFrames);
                Assert.Equal(published + 1, owner.PublishedFrames);
                Assert.Equal(copies + 1, bridge.CopyCount);
                Assert.Equal(before.Submitted + 1, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                Assert.NotEqual(surface, canvas.ImageSource.BoundSurface);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                Assert.False(bridge.IsCopyPending);
                Assert.False(bridge.HasReadySurface);
                Assert.Equal(generation, owner.Generation);
                var snapshot = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(snapshot, 32, 24, 0, 0, 255, 255);
                await driver.DrainAsync();
                driver.AssertAdvanced(before, 2);
                Assert.Equal(copies + 2, bridge.CopyCount);
                AssertIdle(driver);
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Preserves both actual slots under false TryLock and publishes the exact ready surface only after unlock.</summary>
    /// <returns>The real WPF lock, immutable bound pixels and direct ImageSource publication regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task UnavailableImagePublishRetainsReadySlotUnderLockPressureTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var copies = bridge.CopyCount;
                var surface = canvas.ImageSource.BoundSurface;
                var slot = P4PresentationDriver.ReadField<int>(bridge, "boundSlot");
                var original = SnapshotBackBuffer(image);
                nint ready = 0;
                ulong serial = 0;
                P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                await P4PresentationDriver.PollAsync(
                    () => {
                        var draws = driver.Tick();
                        if (owner.SubmittedFrames == before.Submitted) {
                            Assert.Equal(0, draws);
                            Assert.Equal(before, driver.Counts);
                            Assert.True(canvas.IsActive);
                            Assert.True(owner.IsRenderReady);
                            Assert.False(owner.HasPendingTransport);
                            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                            Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                            return false;
                        }

                        Assert.True(draws > 0);
                        Assert.Equal(before.Submitted + 1, owner.SubmittedFrames);
                        Assert.Equal(copied, owner.CopiedFrames);
                        Assert.Equal(published, owner.PublishedFrames);
                        Assert.Equal(copies, bridge.CopyCount);
                        Assert.False(bridge.HasReadySurface);
                        // Borrow on this accepting predicate's stack, before any dispatcher continuation.
                        WithBlockedImageWrites(image, () => {
                            image.SetValue(key, false);
                            PollWithoutDispatcher(
                                () => {
                                    _ = owner.RenderFrame(TimeSpan.FromSeconds(1), automaticFrame: true);
                                    return bridge.HasReadySurface;
                                }, "completed GPU fence and D3D11 query before blocked WPF publication"
                            );
                            ready = bridge.ReadySurface;
                            serial = bridge.ReadySerial;
                            Assert.NotEqual(nint.Zero, ready);
                            Assert.NotEqual(surface, ready);
                            Assert.Equal(before.Submitted + 1, serial);
                            Assert.False(bridge.IsCopyPending);
                            Assert.Equal(copied + 1, owner.CopiedFrames);
                            Assert.Equal(copies + 1, bridge.CopyCount);
                            driver.AssertAdvanced(before, 1);
                            var retained = SnapshotBackBuffer(image);
                            Assert.Equal(original.Pixels, retained.Pixels);
                            driver.Viewport.InvalidateRender();
                            var blocked = driver.Counts;
                            for (var tick = 0; tick < 32; tick++) {
                                var accepted = canvas.ImageSource.TryPublish(ready, canvas.PixelWidth, canvas.PixelHeight);
                                Assert.False(accepted);
                                var drawsBlocked = driver.Tick();
                                Assert.Equal(0, drawsBlocked);
                                Assert.False(image.IsFrontBufferAvailable);
                                Assert.Equal(blocked, driver.Counts);
                                Assert.Equal(published, owner.PublishedFrames);
                                Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                                Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                                Assert.Equal(ready, bridge.ReadySurface);
                                Assert.Equal(serial, bridge.ReadySerial);
                                Assert.Equal(copies + 1, bridge.CopyCount);
                                Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                                Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
                            }

                            Assert.Throws<InvalidOperationException>(() => bridge.BeginCopy(serial + 1));
                            Assert.False(bridge.IsDisposed);
                        });

                        // A successful direct bind is still unacknowledged by the bridge until the owner finishes publication.
                        var accepted = canvas.ImageSource.TryPublish(ready, canvas.PixelWidth, canvas.PixelHeight);
                        Assert.True(accepted);
                        Assert.Equal(ready, canvas.ImageSource.BoundSurface);
                        Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                        Assert.Equal(serial, bridge.ReadySerial);
                        Assert.Equal(published, owner.PublishedFrames);
                        var fresh = SnapshotBackBuffer(image);
                        P4PresentationDriver.AssertPixel(fresh, 32, 24, 0, 0, 255, 255);
                        return true;
                    }, "new scene submission before exclusive WPF write-event borrow"
                );

                await driver.FinishTransportAsync();
                Assert.False(image.IsFrontBufferAvailable);
                Assert.Equal(published + 1, owner.PublishedFrames);
                Assert.Equal(serial, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                Assert.Equal(ready, canvas.ImageSource.BoundSurface);
                Assert.False(bridge.HasReadySurface);
                Assert.Equal(copies + 1, bridge.CopyCount);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                await driver.DrainAsync();
                driver.AssertAdvanced(before, 2);
                AssertIdle(driver);
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Retains a requested rebind and fresh material demand across actual WPF lock misses.</summary>
    /// <returns>The transport-before-reservation, unchanged-budget and subsequent fresh-pixel regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task RetainedPublicationLockMissPreservesSceneDemandTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            EventHandler? observePublicationRequest = null;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var demand = driver.Viewport.RenderDemand;
                var laterPublicationRequests = 0UL;
                var notificationWithoutDemand = false;
                observePublicationRequest = (_, _) => {
                    if (demand.HasPublicationDemand) laterPublicationRequests++;
                    else notificationWithoutDemand = true;
                };
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
                var generation = owner.Generation;
                var context = owner.RenderContext;
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var rebound = owner.ReboundFrames;
                var copies = bridge.CopyCount;
                var surface = canvas.ImageSource.BoundSurface;
                var serial = P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial");
                var slot = P4PresentationDriver.ReadField<int>(bridge, "boundSlot");
                var copySlot = P4PresentationDriver.ReadField<int>(bridge, "copySlot");
                var copySerial = P4PresentationDriver.ReadField<ulong>(bridge, "copySerial");
                var availability = image.IsFrontBufferAvailable;
                var original = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(original, 32, 24, 0, 255, 0, 255);
                Assert.NotEqual(nint.Zero, surface);
                Assert.Equal(before.Submitted, serial);
                Assert.InRange(slot, 0, 1);
                Assert.False(demand.HasRenderDemand);
                var remaining = 0;
                WithBlockedImageWrites(image, () => {
                    demand.RequestPublication();
                    P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                    remaining = P4PresentationDriver.ReadField<int>(demand, "_remainingFrames");
                    Assert.Equal(1, remaining);
                    for (var tick = 0; tick < 32; tick++) {
                        var draws = driver.Tick();
                        Assert.Equal(0, draws);
                        Assert.Equal(before, driver.Counts);
                        Assert.Equal(copied, owner.CopiedFrames);
                        Assert.Equal(published, owner.PublishedFrames);
                        Assert.Equal(rebound, owner.ReboundFrames);
                        Assert.Equal(copies, bridge.CopyCount);
                        Assert.True(canvas.IsActive);
                        Assert.True(owner.IsRenderReady);
                        Assert.Equal(availability, image.IsFrontBufferAvailable);
                        Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                        Assert.Equal(surface, P4PresentationDriver.ReadField<nint>(owner, "publishedSurface"));
                        Assert.Equal(serial, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                        Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                        Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "lastPublishedSlot"));
                        Assert.Equal(serial, P4PresentationDriver.ReadField<ulong>(bridge, "lastPublishedSerial"));
                        Assert.Equal(copySlot, P4PresentationDriver.ReadField<int>(bridge, "copySlot"));
                        Assert.Equal(copySerial, P4PresentationDriver.ReadField<ulong>(bridge, "copySerial"));
                        Assert.False(bridge.IsCopyPending);
                        Assert.False(bridge.HasReadySurface);
                        Assert.Equal(-1, P4PresentationDriver.ReadField<int>(bridge, "readySlot"));
                        Assert.Equal(nint.Zero, bridge.ReadySurface);
                        Assert.Equal(0UL, bridge.ReadySerial);
                        Assert.False(owner.HasPendingTransport);
                        Assert.Equal(0UL, P4PresentationDriver.ReadField<ulong>(owner, "pendingFence"));
                        Assert.Equal(0UL, P4PresentationDriver.ReadField<ulong>(owner, "transportSerial"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(owner, "copyStarted"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(owner, "unfencedExecution"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(owner, "recreateRequested"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(owner, "stopping"));
                        Assert.False(owner.IsUpdating);
                        Assert.True(demand.HasRenderDemand);
                        Assert.True(demand.HasPublicationDemand);
                        Assert.False(demand.HasRecoveryDemand);
                        Assert.Equal(remaining, P4PresentationDriver.ReadField<int>(demand, "_remainingFrames"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(demand, "_frameActive"));
                        Assert.Equal(generation, owner.Generation);
                        Assert.Same(context, owner.RenderContext);
                        Assert.Same(device, P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device"));
                        Assert.Same(bridge, P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge"));
                        Assert.False(owner.IsDisposed);
                        Assert.False(driver.Viewport.IsDisposed);
                        Assert.False(device.IsDisposed);
                        Assert.False(bridge.IsDisposed);
                        Assert.False(mesh.SceneNode.IsDisposed);
                        Assert.False(effects.IsDisposed);
                        Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
                    }

                    var retained = SnapshotBackBuffer(image);
                    Assert.Equal(original.Pixels, retained.Pixels);
                });

                await P4PresentationDriver.PollAsync(
                    () => {
                        var draws = driver.Tick();
                        if (owner.SubmittedFrames == before.Submitted) {
                            Assert.Equal(0, draws);
                            Assert.Equal(before, driver.Counts);
                            Assert.Equal(rebound, owner.ReboundFrames);
                            Assert.Equal(copied, owner.CopiedFrames);
                            Assert.Equal(published, owner.PublishedFrames);
                            Assert.Equal(copies, bridge.CopyCount);
                            Assert.True(canvas.IsActive);
                            Assert.True(owner.IsRenderReady);
                            Assert.False(owner.HasPendingTransport);
                            Assert.True(demand.HasRenderDemand);
                            Assert.True(demand.HasPublicationDemand);
                            Assert.Equal(remaining, P4PresentationDriver.ReadField<int>(demand, "_remainingFrames"));
                            Assert.False(P4PresentationDriver.ReadField<bool>(demand, "_frameActive"));
                            return false;
                        }

                        Assert.True(draws > 0);
                        Assert.Equal(before.Submitted + 1, owner.SubmittedFrames);
                        Assert.Equal(rebound + 1, owner.ReboundFrames);
                        Assert.Equal(copied, owner.CopiedFrames);
                        Assert.Equal(published, owner.PublishedFrames);
                        Assert.Equal(copies, bridge.CopyCount);
                        Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                        Assert.Equal(slot, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                        Assert.Equal(serial, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                        Assert.False(bridge.HasReadySurface);
                        Assert.False(demand.HasRenderDemand);
                        Assert.Equal(0, P4PresentationDriver.ReadField<int>(demand, "_remainingFrames"));
                        Assert.False(P4PresentationDriver.ReadField<bool>(demand, "_frameActive"));
                        var reboundPixels = SnapshotBackBuffer(image);
                        Assert.Equal(original.Pixels, reboundPixels.Pixels);
                        // Include a request raised during the first rebind, then observe actual later producers.
                        laterPublicationRequests = demand.HasPublicationDemand ? 1UL : 0UL;
                        canvas.ImageSource.FrontBufferChanged += observePublicationRequest;
                        canvas.PresentationChanged += observePublicationRequest;
                        return true;
                    }, "retained rebind and exactly one fresh scene submission after WPF unlock"
                );
                await P4PresentationDriver.PollAsync(
                    () => {
                        var beforeRebind = owner.ReboundFrames;
                        var publicationRequested = demand.HasPublicationDemand;
                        _ = owner.RenderFrame(TimeSpan.FromSeconds(1), automaticFrame: true);
                        var rebinds = owner.ReboundFrames - beforeRebind;
                        Assert.InRange(rebinds, 0UL, publicationRequested ? 1UL : 0UL);
                        Assert.False(demand.HasRenderDemand);
                        return !owner.HasPendingTransport && !demand.HasPublicationDemand &&
                               !demand.HasRecoveryDemand;
                    }, "accepted WPF transport and subsequent publication demand"
                );
                driver.AssertAdvanced(before, 1);
                Assert.Equal(copied + 1, owner.CopiedFrames);
                Assert.Equal(published + 1, owner.PublishedFrames);
                Assert.Equal(copies + 1, bridge.CopyCount);
                Assert.False(notificationWithoutDemand);
                Assert.InRange(owner.ReboundFrames, rebound + 1, rebound + 1 + laterPublicationRequests);
                Assert.NotEqual(surface, canvas.ImageSource.BoundSurface);
                Assert.Equal(before.Submitted + 1, P4PresentationDriver.ReadField<ulong>(owner, "publishedSerial"));
                Assert.Equal(before.Submitted + 1, P4PresentationDriver.ReadField<ulong>(bridge, "lastPublishedSerial"));
                var freshSlot = P4PresentationDriver.ReadField<int>(bridge, "boundSlot");
                Assert.InRange(freshSlot, 0, 1);
                Assert.NotEqual(slot, freshSlot);
                Assert.False(bridge.IsCopyPending);
                Assert.False(bridge.HasReadySurface);
                Assert.Equal(-1, P4PresentationDriver.ReadField<int>(bridge, "readySlot"));
                Assert.Equal(nint.Zero, bridge.ReadySurface);
                Assert.Equal(0UL, bridge.ReadySerial);
                Assert.False(owner.HasPendingTransport);
                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                Assert.Same(device, P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device"));
                Assert.Same(bridge, P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge"));
                Assert.False(owner.IsDisposed);
                Assert.False(driver.Viewport.IsDisposed);
                Assert.False(device.IsDisposed);
                Assert.False(bridge.IsDisposed);
                Assert.False(mesh.SceneNode.IsDisposed);
                Assert.False(effects.IsDisposed);
                Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
                var fresh = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(fresh, 32, 24, 0, 0, 255, 255);
                AssertIdle(driver);
            } finally {
                canvas.ImageSource.FrontBufferChanged -= observePublicationRequest;
                canvas.PresentationChanged -= observePublicationRequest;
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Keeps real scene demand while the actual canvas is hidden or unloaded, then resumes it safely.</summary>
    /// <param name="unload">Whether to remove the viewport from its real visual tree instead of hiding it.</param>
    /// <returns>The inactive-surface, lifetime and subsequent real output regression.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task HiddenOrUnloadedSurfaceRetainsDemandTest(bool unload) => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var copies = bridge.CopyCount;
                var generation = owner.Generation;
                var context = owner.RenderContext;
                image.SetValue(key, false);
                if (unload) driver.Root.Children.Remove(driver.Viewport);
                else driver.Viewport.Visibility = Visibility.Hidden;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await P4PresentationDriver.PollAsync(
                    () => {
                        _ = owner.RenderFrame(TimeSpan.Zero, automaticFrame: true);
                        return canvas.ImageSource.BoundSurface == 0;
                    }, "inactive image detachment"
                );
                Assert.False(canvas.IsActive);
                if (unload) Assert.False(canvas.IsLoaded);
                else Assert.False(canvas.IsVisible);
                Assert.Equal(-1, P4PresentationDriver.ReadField<int>(bridge, "boundSlot"));
                P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                for (var tick = 0; tick < 32; tick++) {
                    var draws = driver.Tick();
                    Assert.Equal(0, draws);
                    Assert.Equal(before, driver.Counts);
                    Assert.Equal(copied, owner.CopiedFrames);
                    Assert.Equal(published, owner.PublishedFrames);
                    Assert.Equal(copies, bridge.CopyCount);
                    Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                    Assert.False(bridge.IsDisposed);
                    Assert.False(mesh.SceneNode.IsDisposed);
                    Assert.False(effects.IsDisposed);
                }

                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                image.SetValue(key, true);
                if (unload) driver.Root.Children.Add(driver.Viewport);
                else driver.Viewport.Visibility = Visibility.Visible;
                driver.Root.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await driver.DrainAsync();
                Assert.True(canvas.IsActive);
                Assert.True(owner.SubmittedFrames > before.Submitted);
                Assert.Equal(owner.SubmittedFrames, owner.CompletedFrames);
                Assert.Equal(owner.SubmittedFrames, owner.PublishedFrames);
                Assert.Equal(generation, owner.Generation);
                Assert.Same(context, owner.RenderContext);
                var snapshot = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(snapshot, 32, 24, 0, 0, 255, 255);
                AssertIdle(driver);
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Retains a genuinely removed device and its bound bridge until real WPF lock contention ends.</summary>
    /// <returns>The native device-loss suspend, safe retirement and new-generation output regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task RemovedDeviceRetainsBoundResourcesUntilUnlockTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var registration = P4PresentationDriver.ReadField<D3D12PresentationDeviceRecovery>(owner, "recoveryRegistration");
                var before = driver.Counts;
                var copied = owner.CopiedFrames;
                var published = owner.PublishedFrames;
                var generation = owner.Generation;
                var context = Assert.IsType<RenderContext>(owner.RenderContext);
                var surface = canvas.ImageSource.BoundSurface;
                image.SetValue(key, false);
                P4PresentationDriver.SetMeshColor(mesh, new Vector4(1, 0, 0, 1));
                WithBlockedImageWrites(image, () => {
                    RemoveDevice(device);
                    Assert.True(device.GetDeviceRemovedReason() < 0);
                    for (var tick = 0; tick < 32; tick++) {
                        var draws = driver.Tick();
                        Assert.Equal(0, draws);
                        Assert.Equal(before, driver.Counts);
                        Assert.Equal(copied, owner.CopiedFrames);
                        Assert.Equal(published, owner.PublishedFrames);
                        Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                        Assert.True(registration.IsRemovalRequested);
                        Assert.False(registration.CanRecreate);
                        Assert.False(device.IsDisposed);
                        Assert.False(bridge.IsDisposed);
                        Assert.False(mesh.SceneNode.IsDisposed);
                        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                        Assert.True(driver.Viewport.RenderDemand.HasRecoveryDemand);
                        Assert.Equal(generation, owner.Generation);
                        Assert.Same(context, owner.RenderContext);
                    }
                });

                await P4PresentationDriver.PollAsync(
                    () => {
                        _ = owner.RenderFrame(TimeSpan.Zero, automaticFrame: true);
                        return device.IsDisposed;
                    }, "removed generation retirement after actual WPF unlock"
                );
                Assert.True(bridge.IsDisposed);
                Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
                Assert.Equal(before, driver.Counts);
                Assert.Null(context.Camera);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                Assert.False(effects.IsDisposed);
                image.SetValue(key, true);
                await driver.DrainAsync();
                Assert.Equal(generation + 1, owner.Generation);
                Assert.NotSame(context, owner.RenderContext);
                Assert.True(owner.SubmittedFrames > before.Submitted);
                var snapshot = SnapshotBackBuffer(image);
                P4PresentationDriver.AssertPixel(snapshot, 32, 24, 0, 0, 255, 255);
                AssertIdle(driver);
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Retains native and application lifetime when false availability coincides with a real blocked detach.</summary>
    /// <returns>The failed disposal preflight and balanced, idempotent teardown regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task UnavailableDisposalRetainsOwnersUntilUnlockTest() => StaThread.RunAsync(async () => {
            var key = AvailabilityKey();
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd: false, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            driver.Viewport.Items.Add(mesh);
            var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
            var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
            var image = canvas.ImageSource.NativeImage;
            var originalAvailability = image.IsFrontBufferAvailable;
            try {
                await WarmSyntheticAvailabilityAsync(driver, key);
                var sceneNode = mesh.SceneNode;
                var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
                var bridge = P4PresentationDriver.ReadField<D3D12ImageInteropBridge>(owner, "bridge");
                var before = driver.Counts;
                var surface = canvas.ImageSource.BoundSurface;
                var disposing = 0;
                driver.Viewport.Disposing += (_, _) => disposing++;
                image.SetValue(key, false);
                WithBlockedImageWrites(image, () => {
                    Assert.Throws<TimeoutException>(driver.Viewport.Dispose);
                    Assert.False(owner.TryFinishDisposalPreparation());
                    Assert.Equal(before, driver.Counts);
                    Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                    Assert.False(owner.IsDisposed);
                    Assert.False(driver.Viewport.IsDisposed);
                    Assert.False(bridge.IsDisposed);
                    Assert.False(device.IsDisposed);
                    Assert.False(sceneNode.IsDisposed);
                    Assert.False(effects.IsDisposed);
                    Assert.Equal(0, disposing);
                    Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
                });

                Assert.True(owner.TryFinishDisposalPreparation());
                Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
                Assert.True(bridge.IsDisposed);
                Assert.False(device.IsDisposed);
                Assert.False(owner.IsDisposed);
                Assert.False(driver.Viewport.IsDisposed);
                Assert.False(sceneNode.IsDisposed);
                Assert.False(effects.IsDisposed);
                Assert.Equal(0, disposing);
                driver.Viewport.Dispose();
                driver.Viewport.Dispose();
                owner.Dispose();
                Assert.True(owner.IsDisposed);
                Assert.True(bridge.IsDisposed);
                Assert.True(device.IsDisposed);
                Assert.True(sceneNode.IsDisposed);
                Assert.False(effects.IsDisposed);
                Assert.Equal(1, disposing);
                Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
                Assert.Equal(before, driver.Counts);
                var draws = driver.Tick();
                Assert.Equal(0, draws);
                Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
            } finally {
                image.SetValue(key, originalAvailability);
            }
        }
    );

    /// <summary>Requires the existing actual read-only availability seam instead of silently skipping this contract.</summary>
    /// <returns>The WPF dependency-property key used only for explicitly synthetic availability.</returns>
    private static DependencyPropertyKey AvailabilityKey() {
        var field = typeof(D3DImage).GetField(
            "IsFrontBufferAvailablePropertyKey", BindingFlags.Static | BindingFlags.NonPublic
        );
        Assert.NotNull(field);
        var key = Assert.IsType<DependencyPropertyKey>(field.GetValue(null));
        Assert.Same(D3DImage.IsFrontBufferAvailableProperty, key.DependencyProperty);
        return key;
    }

    /// <summary>Bootstraps real cold GPU resources even when native WPF availability drops after the first bind.</summary>
    /// <param name="driver">The actual hidden-source HAL viewport with its unchanged demand and lifetime rules.</param>
    /// <param name="key">The explicitly synthetic availability key, restored by the calling test's finally block.</param>
    /// <returns>The bounded prerequisite; failures here are not a differential software-policy RED.</returns>
    /// <remarks>
    /// Only this prerequisite restores managed true before an attempt; it does not change native availability.
    /// The scenario begins after this helper returns. No scenario poll forces availability true.
    /// A missing HAL chain, failed WPF lock or failed native copy remains a real prerequisite failure.
    /// </remarks>
    private async Task WarmSyntheticAvailabilityAsync(P4PresentationDriver driver, DependencyPropertyKey key) {
        var syntheticChanges = 0;
        output.WriteLine(
            "PREREQUISITE: cold HAL/template setup with synthetic managed true on each bootstrap attempt; " +
            "a setup failure is not the planned availability-guard RED."
        );
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
        var canvas = Assert.IsType<DPFCanvas>(driver.Canvas);
        Assert.True(driver.Viewport.IsLoaded);
        Assert.True(canvas.IsActive);
        await P4PresentationDriver.PollAsync(
            () => {
                if (!canvas.ImageSource.IsFrontBufferAvailable) {
                    canvas.ImageSource.NativeImage.SetValue(key, true);
                    syntheticChanges++;
                }
                _ = driver.Tick(wait: true);
                var demand = driver.Viewport.RenderDemand;
                return owner.IsInitialized && !owner.HasPendingTransport && !demand.HasRenderDemand &&
                       !demand.HasRecoveryDemand && !demand.HasPublicationDemand;
            }, "PREREQUISITE: synthetic availability bootstrap of real HAL scene and WPF transport"
        );
        Assert.True(owner.SubmittedFrames > 0);
        Assert.Equal(owner.SubmittedFrames, owner.CompletedFrames);
        Assert.NotEqual(nint.Zero, canvas.ImageSource.BoundSurface);
        output.WriteLine(
            $"SCENARIO: real HAL resources ready; synthetic true changes={syntheticChanges}, " +
            $"submitted={owner.SubmittedFrames}, published={owner.PublishedFrames}. " +
            "Subsequent false is synthetic availability only, not physical device removal or asynchronous pixel proof."
        );
    }

    /// <summary>Reads WPF's synchronous software copy of the currently bound real D3D9 surface.</summary>
    /// <param name="image">The actual D3DImage, without changing its binding or any publication milestone.</param>
    /// <returns>A tightly packed premultiplied BGRA diagnostic snapshot.</returns>
    /// <remarks>
    /// CopyBackBuffer can return null or fail. Success does not observe the retained asynchronous MIL software bitmap,
    /// ProcessPresent's possibly ignored copy failure, native hardware-disable state, desktop pixels or module identity.
    /// </remarks>
    private static D3D12CapturedFrame SnapshotBackBuffer(D3DImage image) {
        var method = typeof(D3DImage).GetMethod("CopyBackBuffer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var bitmap = Assert.IsAssignableFrom<BitmapSource>(method.Invoke(image, null));
        Assert.Equal(PixelFormats.Pbgra32, bitmap.Format);
        Assert.Equal(64, bitmap.PixelWidth);
        Assert.Equal(48, bitmap.PixelHeight);
        var pixels = new byte[64 * 48 * 4];
        bitmap.CopyPixels(pixels, 64 * 4, 0);
        return new D3D12CapturedFrame(64, 48, pixels);
    }

    /// <summary>Checks idle counters and drained demand across repeated actual owner polls.</summary>
    /// <param name="driver">The initialized, active and settled WPF viewport.</param>
    private static void AssertIdle(P4PresentationDriver driver) {
        var owner = Assert.IsType<D3D12WpfPresentation>(driver.Wpf);
        var before = driver.Counts;
        var copied = owner.CopiedFrames;
        var published = owner.PublishedFrames;
        var rebound = owner.ReboundFrames;
        for (var tick = 0; tick < 32; tick++) {
            var draws = driver.Tick();
            Assert.Equal(0, draws);
        }

        Assert.Equal(before, driver.Counts);
        Assert.Equal(copied, owner.CopiedFrames);
        Assert.Equal(published, owner.PublishedFrames);
        Assert.Equal(rebound, owner.ReboundFrames);
        Assert.False(owner.HasPendingTransport);
        Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.False(driver.Viewport.RenderDemand.HasPublicationDemand);
        Assert.False(driver.Viewport.RenderDemand.HasRecoveryDemand);
    }

    /// <summary>Borrows and restores the existing real WPF write event without dispatcher pumping or event disposal.</summary>
    /// <param name="image">The actual bound WPF image, including when its managed availability is false.</param>
    /// <param name="verify">The synchronous STA scenario performed while writes are genuinely blocked.</param>
    private static void WithBlockedImageWrites(D3DImage image, Action verify) {
        var canWrite = P4PresentationDriver.ReadField<EventWaitHandle>(image, "_canWriteEvent");
        try {
            canWrite.Reset();
            AssertWriteLockFails(image);
            verify();
            AssertWriteLockFails(image);
            Assert.False(canWrite.WaitOne(0));
        } finally {
            canWrite.Set();
        }
    }

    /// <summary>Proves a genuine zero-duration lock failure and balances even its normally returned false result.</summary>
    /// <param name="image">The actual WPF image whose borrowed write event is reset.</param>
    private static void AssertWriteLockFails(D3DImage image) {
        var locked = image.TryLock(new Duration(TimeSpan.Zero));
        try {
            Assert.False(locked);
        } finally {
            image.Unlock();
        }

        Assert.Equal(0U, Convert.ToUInt32(P4PresentationDriver.ReadField<object>(image, "_lockCount")));
    }

    /// <summary>Polls real native progress without pumping the dispatcher while WPF's write event is borrowed.</summary>
    /// <param name="completed">The actual nonblocking owner service and passive completion observation.</param>
    /// <param name="operation">The operation identified if the unchanged five-second fixture deadline is reached.</param>
    /// <exception cref="TimeoutException">The existing native-progress deadline is reached.</exception>
    private static void PollWithoutDispatcher(Func<bool> completed, string operation) {
        var started = Stopwatch.GetTimestamp();
        var spin = new SpinWait();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(started) >= P4PresentationDriver.Timeout) throw new TimeoutException(operation);
            spin.SpinOnce();
        }
    }

    /// <summary>Uses the existing scoped ID3D12Device5 seam to remove the test process's device singleton.</summary>
    /// <param name="device">The actual productive device; this does not reset the physical adapter.</param>
    private static void RemoveDevice(SilkD3D12Device device) {
        var id = ID3D12Device5.Guid;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(device.NativePointer, in id, out var extended));
        try {
            var method = Marshal.ReadIntPtr(Marshal.ReadIntPtr(extended), 58 * nint.Size);
            Marshal.GetDelegateForFunctionPointer<RemoveNativeDevice>(method)(extended);
        } finally {
            Marshal.Release(extended);
        }
    }

    /// <summary>Matches the existing pinned ID3D12Device5.RemoveDevice COM signature.</summary>
    /// <param name="device">The balanced extended device interface.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RemoveNativeDevice(nint device);
}
