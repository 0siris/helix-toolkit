using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Controls;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// Contains a scene-node collection shared by multiple Direct3D 12 viewports.
/// </summary>
public class ModelContainer3DX : HelixItemsControl, IModelContainer {
    /// <summary>Publishes immutable consumer snapshots without allocating during node invalidation.</summary>
    private IRenderHost[] consumers = [];

    /// <summary>Retains the unique subscribed roots while at least one consumer is registered.</summary>
    private readonly HashSet<SceneNode> observedRoots = new(ReferenceEqualityComparer.Instance);

    /// <summary>Reuses the dispatcher callback for a manager change deferred by an active scene frame.</summary>
    private readonly Action synchronizeEffectsManagerCallback;

    /// <summary>Coalesces deferred manager changes into one dispatcher callback.</summary>
    private bool managerChangeQueued;

    /// <summary>Records a requested identity fanout independently of forced same-manager resource retirement.</summary>
    private bool managerIdentityChangePending;

    /// <summary>Retains a request to retire every consumer's current native resource generation.</summary>
    private bool resourceRetirementPending;

    /// <summary>Prevents application callbacks from nesting resource retirement or manager assignment.</summary>
    private bool isApplyingManagerChange;

    /// <summary>Gets whether pending or active shared manager work prevents nested scene preparation.</summary>
    internal bool IsSynchronizingEffectsManager { get; private set; }

    /// <summary>
    /// Identifies the effects-manager dependency property retained for existing XAML bindings.
    /// </summary>
    public static readonly DependencyProperty EffectsManagerProperty = DependencyProperty.Register(
        nameof(EffectsManager),
        typeof(IEffectsManager),
        typeof(ModelContainer3DX),
        new PropertyMetadata(null, EffectsManagerChanged));

    /// <summary>
    /// Initializes a shared model container.
    /// </summary>
    public ModelContainer3DX() {
        synchronizeEffectsManagerCallback = ProcessManagerSynchronization;
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Gets or sets the effects manager used by existing bindings.
    /// </summary>
    public IEffectsManager? EffectsManager {
        get => (IEffectsManager?) GetValue(EffectsManagerProperty);
        set => SetValue(EffectsManagerProperty, value);
    }

    /// <summary>Registers an active host once and observes each shared root without taking application ownership.</summary>
    /// <param name="host">The actual viewport host receiving shared scene invalidation.</param>
    internal void RegisterConsumer(IRenderHost host) {
        VerifyAccess();
        var previous = consumers;
        foreach (var consumer in previous)
            if (ReferenceEquals(consumer, host)) return;
        IRenderHost[] next = new IRenderHost[previous.Length + 1];
        Array.Copy(previous, next, previous.Length);
        next[^1] = host;
        Volatile.Write(ref consumers, next);
        if (previous.Length == 0) RefreshRootSubscriptions();
        if (EffectsManager is { } manager && !ReferenceEquals(host.EffectsManager, manager))
            RequestEffectsManagerSynchronization();
        host.InvalidateSceneGraph();
    }

    /// <summary>Stops notifications to one host without releasing another renderer's node attachments.</summary>
    /// <param name="host">The unloading, replaced or disposed viewport host.</param>
    internal void UnregisterConsumer(IRenderHost host) {
        VerifyAccess();
        var previous = consumers;
        var index = 0;
        while (index < previous.Length && !ReferenceEquals(previous[index], host)) index++;
        if (index == previous.Length) return;
        IRenderHost[] next = new IRenderHost[previous.Length - 1];
        Array.Copy(previous, next, index);
        Array.Copy(previous, index + 1, next, index, previous.Length - index - 1);
        Volatile.Write(ref consumers, next);
        if (next.Length == 0) RefreshRootSubscriptions();
    }

    /// <summary>Reconciles root observers after collection changes, including reset, and wakes all active hosts.</summary>
    /// <param name="e">The WPF item collection mutation.</param>
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        RefreshRootSubscriptions();
        InvalidateConsumers(null, InvalidateTypes.SceneGraph);
    }

    /// <summary>Rebuilds subscriptions only when the item collection or first/last consumer changes.</summary>
    private void RefreshRootSubscriptions() {
        foreach (var root in observedRoots) root.Invalidated -= RootInvalidated;
        observedRoots.Clear();
        if (consumers.Length == 0) return;
        for (var index = 0; index < Items.Count; index++)
            if (Items[index] is Element3D element && observedRoots.Add(element.SceneNode))
                element.SceneNode.Invalidated += RootInvalidated;
    }

    /// <summary>Forwards an original descendant notification already propagated to its shared root.</summary>
    /// <param name="sender">The original invalidating descendant.</param>
    /// <param name="type">The original render, graph or classification invalidation.</param>
    private void RootInvalidated(object? sender, InvalidateTypes type) =>
        InvalidateConsumers((sender as SceneNode)?.RenderHost, type);

    /// <summary>Notifies every registered consumer except the host directly notified by the original node.</summary>
    /// <param name="alreadyNotified">The node's current native host, or null for a collection mutation.</param>
    /// <param name="type">The original invalidation kind.</param>
    private void InvalidateConsumers(IRenderHost? alreadyNotified, InvalidateTypes type) {
        foreach (var host in Volatile.Read(ref consumers)) {
            if (ReferenceEquals(host, alreadyNotified)) continue;
            switch (type) {
                case InvalidateTypes.Render: host.InvalidateRender(); break;
                case InvalidateTypes.SceneGraph: host.InvalidateSceneGraph(); break;
                case InvalidateTypes.PerFrameRenderables: host.InvalidatePerFrameRenderables(); break;
            }
        }
    }

    /// <summary>Coalesces shared manager assignment after retiring every old presentation's attachment leases.</summary>
    /// <param name="owner">The shared model container whose manager changed.</param>
    /// <param name="args">The dependency-property mutation.</param>
    private static void EffectsManagerChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((ModelContainer3DX)owner).RequestEffectsManagerSynchronization();

    /// <summary>Requests a latest-value manager fanout and blocks scene entry until every old owner retires.</summary>
    private void RequestEffectsManagerSynchronization() {
        managerIdentityChangePending = true;
        IsSynchronizingEffectsManager = true;
        SynchronizeEffectsManager();
    }

    /// <summary>Retires all shared native generations without changing independently supplied manager identities.</summary>
    /// <remarks>Call for same-instance manager resource disposal or reinitialization, on the container dispatcher.</remarks>
    internal void RequestSharedResourceRetirement() {
        VerifyAccess();
        resourceRetirementPending = true;
        IsSynchronizingEffectsManager = true;
        SynchronizeEffectsManager();
    }

    /// <summary>Retires all old native owners before assigning a replacement manager to any registered host.</summary>
    private void SynchronizeEffectsManager() {
        if (!managerIdentityChangePending && !resourceRetirementPending) return;
        var snapshot = consumers;
        if (isApplyingManagerChange || HasPreparingConsumer(snapshot)) {
            QueueManagerSynchronization();
            return;
        }

        var manager = EffectsManager;
        var assignManager = managerIdentityChangePending;
        var retireAll = resourceRetirementPending;
        managerIdentityChangePending = false;
        resourceRetirementPending = false;
        isApplyingManagerChange = true;
        var completed = false;
        try {
            foreach (var host in snapshot)
                if (host is Viewport3DX viewport)
                    viewport.RetireSceneResourcesForManagerChange();
            // Retirement can invoke application callbacks that change the requested manager or consumer set.
            if ((assignManager && !ReferenceEquals(manager, EffectsManager)) || !ReferenceEquals(snapshot, consumers)) {
                managerIdentityChangePending |= assignManager;
                resourceRetirementPending |= retireAll;
                QueueManagerSynchronization();
                completed = true;
                return;
            }
            if (assignManager) {
                foreach (var host in snapshot) {
                    if (!ReferenceEquals(manager, EffectsManager) || !ReferenceEquals(snapshot, consumers)) {
                        managerIdentityChangePending = true;
                        completed = true;
                        return;
                    }
                    if (!ReferenceEquals(host.EffectsManager, manager)) host.EffectsManager = manager;
                }
            }
            InvalidateConsumers(null, InvalidateTypes.SceneGraph);
            completed = true;
        } finally {
            isApplyingManagerChange = false;
            if (!completed) {
                managerIdentityChangePending |= assignManager;
                resourceRetirementPending |= retireAll;
            }
            IsSynchronizingEffectsManager = managerIdentityChangePending || resourceRetirementPending;
            if (completed && IsSynchronizingEffectsManager) QueueManagerSynchronization();
        }
    }

    /// <summary>Releases the queued callback slot before retrying the latest shared manager change.</summary>
    private void ProcessManagerSynchronization() {
        managerChangeQueued = false;
        SynchronizeEffectsManager();
    }

    /// <summary>Checks the entire consumer set before any presentation resources or host managers change.</summary>
    /// <param name="snapshot">The immutable dispatcher-owned consumer snapshot.</param>
    /// <returns>Whether any actual viewport is currently preparing a presentation frame.</returns>
    private static bool HasPreparingConsumer(IRenderHost[] snapshot) {
        foreach (var host in snapshot)
            if (host is Viewport3DX { IsPreparingPresentationFrame: true }) return true;
        return false;
    }

    /// <summary>Posts one retained callback that rechecks active frames and uses the latest requested manager.</summary>
    private void QueueManagerSynchronization() {
        if (managerChangeQueued || Dispatcher.HasShutdownStarted) return;
        managerChangeQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, synchronizeEffectsManagerCallback);
    }

    /// <inheritdoc />
    public IEnumerable<SceneNode> Renderables => Items
        .OfType<Element3D>()
        .Select(element => element.SceneNode);
}
