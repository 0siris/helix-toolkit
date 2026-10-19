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

    /// <summary>Retains native resource owners independently of loaded notification membership.</summary>
    private Viewport3DX[] resourceConsumers = [];

    /// <summary>Records shared mutations even while every notification consumer is temporarily unloaded.</summary>
    private long sceneRevision;

    /// <summary>Gets the shared mutation revision published by dispatcher or worker-thread invalidations.</summary>
    internal long SceneRevision => Interlocked.Read(ref sceneRevision);

    /// <summary>Retains unique root subscriptions while notifications or native attachment owners remain.</summary>
    private readonly HashSet<SceneNode> observedRoots = new(ReferenceEqualityComparer.Instance);

    /// <summary>Reuses the dispatcher callback for a manager change deferred by an active scene frame.</summary>
    private readonly Action synchronizeEffectsManagerCallback;

    /// <summary>Coalesces deferred manager changes into one dispatcher callback.</summary>
    private bool managerChangeQueued;

    /// <summary>Retains a retry timer allocated only after a bounded native retirement times out.</summary>
    private DispatcherTimer? managerRetryTimer;

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
    internal void RegisterConsumer(IRenderHost host) => RegisterConsumer(host, null);

    /// <summary>Restores notifications without redrawing an unchanged retained shared generation.</summary>
    /// <param name="host">The actual viewport host receiving shared scene invalidation.</param>
    /// <param name="retainedSceneRevision">The revision observed at unload, or null for a new resource owner.</param>
    internal void RegisterConsumer(IRenderHost host, long? retainedSceneRevision) {
        VerifyAccess();
        var previous = consumers;
        foreach (var consumer in previous)
            if (ReferenceEquals(consumer, host)) return;
        IRenderHost[] next = new IRenderHost[previous.Length + 1];
        Array.Copy(previous, next, previous.Length);
        next[^1] = host;
        Volatile.Write(ref consumers, next);
        if (previous.Length == 0 && resourceConsumers.Length == 0) RefreshRootSubscriptions();
        if (EffectsManager is { } manager && !ReferenceEquals(host.EffectsManager, manager))
            RequestEffectsManagerSynchronization();
        // Publish membership before comparing revisions so a concurrent invalidation cannot fall between both checks.
        if (retainedSceneRevision != SceneRevision) host.InvalidateSceneGraph();
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
        if (next.Length == 0 && resourceConsumers.Length == 0) RefreshRootSubscriptions();
        SchedulePendingEffectsManagerSynchronization();
    }

    /// <summary>Registers a viewport before its renderer can borrow shared scene attachment leases.</summary>
    /// <param name="viewport">The actual native resource owner, retained across temporary unload.</param>
    internal void RegisterResourceConsumer(Viewport3DX viewport) {
        VerifyAccess();
        foreach (var consumer in resourceConsumers)
            if (ReferenceEquals(consumer, viewport)) return;
        var next = new Viewport3DX[resourceConsumers.Length + 1];
        Array.Copy(resourceConsumers, next, resourceConsumers.Length);
        next[^1] = viewport;
        resourceConsumers = next;
        if (consumers.Length == 0 && next.Length == 1) RefreshRootSubscriptions();
    }

    /// <summary>Releases an owner only after its old renderer and submitted GPU work have retired successfully.</summary>
    /// <param name="viewport">The viewport whose shared attachment leases no longer exist.</param>
    internal void UnregisterResourceConsumer(Viewport3DX viewport) {
        VerifyAccess();
        var previous = resourceConsumers;
        var index = Array.IndexOf(previous, viewport);
        if (index < 0) return;
        var next = new Viewport3DX[previous.Length - 1];
        Array.Copy(previous, next, index);
        Array.Copy(previous, index + 1, next, index, previous.Length - index - 1);
        resourceConsumers = next;
        if (consumers.Length == 0 && next.Length == 0) RefreshRootSubscriptions();
        SchedulePendingEffectsManagerSynchronization();
    }

    /// <summary>Reconciles root observers after collection changes, including reset, and wakes all active hosts.</summary>
    /// <param name="e">The WPF item collection mutation.</param>
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        RefreshRootSubscriptions();
        InvalidateConsumers(null, InvalidateTypes.SceneGraph);
    }

    /// <summary>Rebuilds subscriptions when items change or the first/last notification or resource owner changes.</summary>
    private void RefreshRootSubscriptions() {
        foreach (var root in observedRoots) root.Invalidated -= RootInvalidated;
        observedRoots.Clear();
        if (consumers.Length == 0 && resourceConsumers.Length == 0) return;
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
        Interlocked.Increment(ref sceneRevision);
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
    /// <exception cref="TimeoutException">Retirement timed out; the pending identity and owners remain retained.</exception>
    private void RequestEffectsManagerSynchronization() {
        managerIdentityChangePending = true;
        IsSynchronizingEffectsManager = true;
        try {
            SynchronizeEffectsManager();
        } catch (TimeoutException) {
            ScheduleManagerRetirementRetry();
            throw;
        }
    }

    /// <summary>Retires all shared native generations without changing independently supplied manager identities.</summary>
    /// <remarks>Call for same-instance manager resource disposal or reinitialization, on the container dispatcher.</remarks>
    /// <exception cref="TimeoutException">Retirement timed out; the barrier and owners remain retained.</exception>
    internal void RequestSharedResourceRetirement() {
        VerifyAccess();
        resourceRetirementPending = true;
        IsSynchronizingEffectsManager = true;
        // The manager frees borrowed resources as soon as its notification returns, even inside CPU preparation.
        try {
            foreach (var viewport in resourceConsumers) viewport.DrainSubmittedWorkForManagerDisposal();
            SynchronizeEffectsManager(forceRetirement: true);
        } catch (TimeoutException) {
            ScheduleManagerRetirementRetry();
            throw;
        }
    }

    /// <summary>Retires all old native owners before assigning a replacement manager to any registered host.</summary>
    /// <param name="forceRetirement">Whether a synchronous manager disposal requires bounded GPU draining.</param>
    /// <exception cref="TimeoutException">Retirement timed out; the barrier and resources remain owned for retry.</exception>
    private void SynchronizeEffectsManager(bool forceRetirement = false) {
        if (!managerIdentityChangePending && !resourceRetirementPending) return;
        var snapshot = consumers;
        var resources = resourceConsumers;
        if (isApplyingManagerChange) return;
        managerRetryTimer?.Stop();
        if (HasPreparingConsumer(snapshot) || HasPreparingConsumer(resources)) {
            // The last consumer's frame exit resumes this transaction without polling a nested dispatcher pump.
            return;
        }

        if (!forceRetirement) {
            foreach (var viewport in resources)
                if (!viewport.CanRetireManagerWithoutWaiting()) {
                    ScheduleManagerRetirementRetry();
                    return;
                }
        }

        var manager = EffectsManager;
        var assignManager = managerIdentityChangePending;
        var retireAll = resourceRetirementPending;
        managerIdentityChangePending = false;
        resourceRetirementPending = false;
        isApplyingManagerChange = true;
        var completed = false;
        try {
            foreach (var viewport in resources) viewport.RetireSceneResourcesForManagerChange();
            // Retirement can invoke application callbacks that change the requested manager or consumer set.
            // Successful retirement removes resource owners; that expected shrink must not restart the transaction.
            if ((assignManager && !ReferenceEquals(manager, EffectsManager)) ||
                !ReferenceEquals(snapshot, consumers) || resourceConsumers.Length != 0) {
                managerIdentityChangePending |= assignManager;
                resourceRetirementPending |= retireAll;
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
        } catch (TimeoutException) {
            ScheduleManagerRetirementRetry();
            throw;
        } finally {
            isApplyingManagerChange = false;
            if (!completed) {
                managerIdentityChangePending |= assignManager;
                resourceRetirementPending |= retireAll;
            }
            IsSynchronizingEffectsManager = managerIdentityChangePending || resourceRetirementPending;
            if (completed) SchedulePendingEffectsManagerSynchronization();
        }
    }

    /// <summary>Releases the queued callback slot and contains asynchronous retirement timeouts without clearing the barrier.</summary>
    private void ProcessManagerSynchronization() {
        managerChangeQueued = false;
        try {
            SynchronizeEffectsManager();
        } catch (TimeoutException exception) {
            ScheduleManagerRetirementRetry();
            LoggerLib.Logger.Error(exception, "Shared scene resources remain owned for retirement retry");
        }
    }

    /// <summary>Checks notification or retained-resource membership before any presentation resources or managers change.</summary>
    /// <param name="snapshot">The immutable dispatcher-owned notification or resource snapshot.</param>
    /// <returns>Whether any actual viewport is currently preparing a presentation frame.</returns>
    private static bool HasPreparingConsumer(IRenderHost[] snapshot) {
        foreach (var host in snapshot)
            if (host is Viewport3DX { IsPreparingPresentationFrame: true }) return true;
        return false;
    }

    /// <summary>Resumes pending shared work after the last consumer frame exits, without allocating on settled frames.</summary>
    internal void SchedulePendingEffectsManagerSynchronization() {
        if (!IsSynchronizingEffectsManager || isApplyingManagerChange || managerChangeQueued ||
            managerRetryTimer is { IsEnabled: true } || Dispatcher.HasShutdownStarted || HasPreparingConsumer(consumers))
            return;
        if (HasPreparingConsumer(resourceConsumers)) return;
        managerChangeQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, synchronizeEffectsManagerCallback);
    }

    /// <summary>Retries pending GPU work or exceptional retirement failures with one timer and no periodic idle callback.</summary>
    private void ScheduleManagerRetirementRetry() {
        if (Dispatcher.HasShutdownStarted) return;
        if (managerRetryTimer is null) {
            managerRetryTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            managerRetryTimer.Tick += RetryManagerRetirement;
        }
        managerRetryTimer.Start();
    }

    /// <summary>Stops the exceptional timer before trying the latest transaction or waiting for a consumer frame exit.</summary>
    /// <param name="sender">The retained retirement timer.</param>
    /// <param name="args">The original timer tick.</param>
    private void RetryManagerRetirement(object? sender, EventArgs args) {
        managerRetryTimer!.Stop();
        ProcessManagerSynchronization();
    }

    /// <inheritdoc />
    public IEnumerable<SceneNode> Renderables => Items
        .OfType<Element3D>()
        .Select(element => element.SceneNode);
}
