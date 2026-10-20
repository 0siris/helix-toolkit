using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies deferred HWND presentation retirement without creating a device or native window.</summary>
[Collection(WpfCollection.Name)]
public sealed class NativePresentationLifetimeTests {
    /// <summary>Retires an uninitialized generation repeatedly while preserving its usable HWND host.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task SceneRetirementPreservesHostAndClearsCapture() => StaThread.RunAsync(() => {
            using var surface = new D3D12PresentationSurface();
            var host = surface.WindowHost;
            SetField(surface, "<LastCapturedFrame>k__BackingField", new D3D12CapturedFrame(1, 1, [1, 2, 3, 4]));
            surface.RetireSceneResources();
            surface.RetireSceneResources();
            surface.EnsureNativeInitialized();
            Assert.Same(host, surface.WindowHost);
            Assert.Contains(host, surface.Children.Cast<UIElement>());
            Assert.False(surface.IsDisposed);
            Assert.False(surface.IsInitialized);
            Assert.False(surface.IsRetirementPending);
            Assert.Null(surface.LastCapturedFrame);
        }
    );

    /// <summary>Preserves unclaimed frame completions across retirement without redelivering claimed completions.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task RetirementPreservesExactlyOnceCompletionClaims() => StaThread.RunAsync(() => {
            using var surface = new D3D12PresentationSurface();
            var submission = GetField<D3D12FrameSubmission>(surface, "_frameSubmission");
            submission.Submit(1, TimeSpan.Zero);
            Assert.True(submission.TryComplete(1, TimeSpan.Zero));
            surface.RetireSceneResources();
            Assert.True(surface.TryTakeCompletedFrame());
            Assert.False(surface.TryTakeCompletedFrame());
            surface.RetireSceneResources();
            Assert.False(surface.TryTakeCompletedFrame());
        }
    );

    /// <summary>Delivers both host events after clearing native fields while reentrant disposal remains deferred.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task RetirementObserverDeliversTheEventPairBeforeReentrantSurfaceDisposal() => StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true };
            using var surface = new D3D12PresentationSurface { RenderHost = viewport };
            typeof(Viewport3DX).GetField("d3d12Surface", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(viewport, surface);
            var submission = GetField<D3D12FrameSubmission>(surface, "_frameSubmission");
            submission.Submit(1, TimeSpan.Zero);
            Assert.True(submission.TryComplete(1, TimeSpan.Zero));
            var rendered = 0;
            var onRendered = 0;
            viewport.Rendered += (_, _) => {
                rendered++;
                Assert.True(surface.IsRetirementPending);
                Assert.False(surface.IsInitialized);
                Assert.Null(surface.FrameRenderer);
                Assert.Null(surface.RenderContext);
                surface.EnsureNativeInitialized();
                surface.RetireSceneResources();
                surface.Dispose();
                Assert.False(surface.IsDisposed);
            };
            viewport.OnRendered += (_, _) => onRendered++;
            surface.RetireSceneResources();
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);
            Assert.False(surface.IsDisposed);
            PumpDispatcher();
            Assert.True(surface.IsDisposed);
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);
        }
    );

    /// <summary>Suppresses retirement notifications once final viewport disposal has notified lifetime consumers.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task FinalViewportDisposalDoesNotRaiseCompletedFramesAfterDisposing() => StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true };
            using var surface = new D3D12PresentationSurface { RenderHost = viewport };
            typeof(Viewport3DX).GetField("d3d12Surface", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(viewport, surface);
            var submission = GetField<D3D12FrameSubmission>(surface, "_frameSubmission");
            submission.Submit(1, TimeSpan.Zero);
            Assert.True(submission.TryComplete(1, TimeSpan.Zero));
            var disposing = false;
            var notificationsAfterDisposing = 0;
            viewport.Disposing += (_, _) => disposing = true;
            viewport.Rendered += (_, _) => {
                if (disposing) notificationsAfterDisposing++;
            };
            viewport.OnRendered += (_, _) => {
                if (disposing) notificationsAfterDisposing++;
            };
            viewport.Dispose();
            Assert.True(disposing);
            Assert.True(surface.IsDisposed);
            Assert.Equal(0, notificationsAfterDisposing);
        }
    );

    /// <summary>Defers the whole public disposal until CPU frame ownership is released.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task DirectDisposalDuringRecordingWaitsForFrameUnwind() => StaThread.RunAsync(() => {
            using var surface = new D3D12PresentationSurface();
            SetField(surface, "frameActive", true);
            surface.Dispose();
            Assert.False(surface.IsDisposed);
            Assert.True(surface.IsRetirementPending);
            Assert.True(GetField<bool>(surface, "disposeRequested"));
            Assert.Null(GetField<DispatcherOperation?>(surface, "retirementOperation"));
            SetField(surface, "frameActive", false);
            surface.SchedulePendingRetirement();
            var queued = GetField<DispatcherOperation>(surface, "retirementOperation");
            surface.SchedulePendingRetirement();
            Assert.Same(queued, GetField<DispatcherOperation>(surface, "retirementOperation"));
            PumpDispatcher();
            Assert.True(surface.IsDisposed);
            Assert.False(surface.IsRetirementPending);
            Assert.Null(GetField<DispatcherOperation?>(surface, "retirementOperation"));
        }
    );

    /// <summary>Retains retirement when a nested dispatcher pump invokes its callback during a later active frame.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task NestedRetirementCallbackDoesNotDisposeOrRepostInsideAFrame() => StaThread.RunAsync(() => {
            using var surface = new D3D12PresentationSurface();
            SetField(surface, "frameActive", true);
            surface.Dispose();
            SetField(surface, "frameActive", false);
            surface.SchedulePendingRetirement();
            SetField(surface, "frameActive", true);
            PumpDispatcher();
            Assert.False(surface.IsDisposed);
            Assert.True(GetField<bool>(surface, "disposeRequested"));
            Assert.Null(GetField<DispatcherOperation?>(surface, "retirementOperation"));
            SetField(surface, "frameActive", false);
            surface.SchedulePendingRetirement();
            PumpDispatcher();
            Assert.True(surface.IsDisposed);
        }
    );

    /// <summary>Defers unload retirement without converting it into final host disposal.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task UnloadDuringRecordingRetiresOnlyTheGenerationAfterUnwind() => StaThread.RunAsync(() => {
            using var surface = new D3D12PresentationSurface();
            SetField(surface, "frameActive", true);
            surface.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            Assert.True(GetField<bool>(surface, "retireRequested"));
            Assert.False(surface.IsDisposed);
            SetField(surface, "frameActive", false);
            surface.SchedulePendingRetirement();
            PumpDispatcher();
            Assert.False(GetField<bool>(surface, "retireRequested"));
            Assert.False(surface.IsDisposed);
            Assert.Contains(surface.WindowHost, surface.Children.Cast<UIElement>());
        }
    );

    /// <summary>Runs queued retirement callbacks without a native source, GPU operation or blocking wait.</summary>
    private static void PumpDispatcher() {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false)
        );
        Dispatcher.PushFrame(frame);
    }

    /// <summary>Sets private CPU ownership state without invoking native frame recording.</summary>
    /// <param name="surface">The actual presentation surface.</param>
    /// <param name="name">The retained state field.</param>
    /// <param name="value">The managed test state.</param>
    private static void SetField(D3D12PresentationSurface surface, string name, object value) =>
        typeof(D3D12PresentationSurface).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(surface, value);

    /// <summary>Reads retained lifetime state without adding a public testing API.</summary>
    /// <typeparam name="T">The retained field type.</typeparam>
    /// <param name="surface">The actual presentation surface.</param>
    /// <param name="name">The retained state field.</param>
    /// <returns>The field's current value.</returns>
    private static T GetField<T>(D3D12PresentationSurface surface, string name) =>
        (T) typeof(D3D12PresentationSurface).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(surface)!;
}