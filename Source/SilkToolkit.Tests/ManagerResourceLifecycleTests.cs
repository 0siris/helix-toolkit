using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks real viewport subscriptions and shared notification membership without native initialization.</summary>
[Collection(WpfCollection.Name)]
public sealed class ManagerResourceLifecycleTests {
    /// <summary>Transfers invalidation subscriptions and keeps both externally supplied managers alive.</summary>
    /// <returns>The dispatcher-owned lifecycle verification.</returns>
    [Fact]
    public Task ManagerReplacementUnsubscribesThePreviousBorrowedOwner() => StaThread.RunAsync(() => {
        using var first = new DefaultEffectsManager();
        using var second = new DefaultEffectsManager();
        using var viewport = new Viewport3DX { EffectsManager = first };
        Drain(viewport.RenderDemand);
        first.RaiseInvalidateRender();
        Assert.True(viewport.RenderDemand.HasRenderDemand);
        viewport.EffectsManager = second;
        Drain(viewport.RenderDemand);
        first.RaiseInvalidateRender();
        Assert.False(viewport.RenderDemand.HasRenderDemand);
        second.RaiseInvalidateRender();
        Assert.True(viewport.RenderDemand.HasRenderDemand);
        viewport.Dispose();
        Assert.True(first.Initialized);
        Assert.True(second.Initialized);
    });

    /// <summary>Suspends two consumers across same-instance resource disposal and resumes them after reinitialization.</summary>
    /// <returns>The real manager-event verification.</returns>
    [Fact]
    public Task SharedManagerResourceEpochSuspendsAndWakesBothViewports() => StaThread.RunAsync(() => {
        using var manager = new DefaultEffectsManager();
        using var first = new Viewport3DX { EffectsManager = manager };
        using var second = new Viewport3DX { EffectsManager = manager };
        manager.DisposeAllResources();
        Assert.False(first.CanSubmitNativeScene);
        Assert.False(second.CanSubmitNativeScene);
        manager.Reinitialize();
        Assert.True(first.CanSubmitNativeScene);
        Assert.True(second.CanSubmitNativeScene);
        Assert.True(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        first.Dispose();
        Assert.True(manager.Initialized);
        Drain(second.RenderDemand);
        manager.RaiseInvalidateRender();
        Assert.True(second.RenderDemand.HasRenderDemand);
    });

    /// <summary>Marshals worker resource events before they return while preserving immediate worker invalidation demand.</summary>
    /// <returns>The asynchronous dispatcher verification.</returns>
    [Fact]
    public Task WorkerManagerEventsPreserveResourceOrdering() => StaThread.RunAsync(async () => {
        using var manager = new DefaultEffectsManager();
        using var viewport = new Viewport3DX { EffectsManager = manager };
        Drain(viewport.RenderDemand);
        await Task.Run(manager.RaiseInvalidateRender);
        Assert.True(viewport.RenderDemand.HasRenderDemand);
        await Task.Run(manager.DisposeAllResources);
        Assert.False(viewport.CanSubmitNativeScene);
        await Task.Run(manager.Reinitialize);
        Assert.True(viewport.CanSubmitNativeScene);
        Assert.True(viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Connects the actual frame-entry registration to unload and final disposal without disposing shared nodes.</summary>
    /// <returns>The dispatcher-owned shared membership verification.</returns>
    [Fact]
    public Task ActualViewportMembershipStopsOnUnloadAndReturnsForExplicitRendering() => StaThread.RunAsync(() => {
        var node = new GroupModel3D();
        var container = new ModelContainer3DX();
        container.Items.Add(node);
        using var first = new Viewport3DX { EnableSharedModelMode = true, SharedModelContainer = container };
        using var second = new Viewport3DX { EnableSharedModelMode = true, SharedModelContainer = container };
        first.PrepareHostFrameContracts();
        second.PrepareHostFrameContracts();
        Drain(first.RenderDemand);
        Drain(second.RenderDemand);
        node.Visibility = Visibility.Hidden;
        Assert.True(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        first.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Drain(first.RenderDemand);
        Drain(second.RenderDemand);
        node.Visibility = Visibility.Visible;
        Assert.False(first.RenderDemand.HasRenderDemand);
        Assert.True(second.RenderDemand.HasRenderDemand);
        first.PrepareHostFrameContracts();
        first.Dispose();
        Drain(second.RenderDemand);
        node.Visibility = Visibility.Hidden;
        Assert.True(second.RenderDemand.HasRenderDemand);
        Assert.False(node.SceneNode.IsDisposed);
        second.Dispose();
        node.Dispose();
    });

    /// <summary>Consumes only already requested work, leaving no synthetic invalidation in the measured state.</summary>
    /// <param name="demand">The viewport's actual bounded demand coordinator.</param>
    private static void Drain(D3D12RenderDemand demand) {
        while (demand.TryBeginFrame(out _)) demand.CompleteFrame(true);
    }
}
