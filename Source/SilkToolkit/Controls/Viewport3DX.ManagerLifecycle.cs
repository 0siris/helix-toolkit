using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX.Controls;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Coordinates borrowed effects resources and shared-scene consumers with actual presentation lifetimes.</summary>
public partial class Viewport3DX {
    /// <summary>The manager whose lifecycle notifications this viewport currently owns.</summary>
    private IEffectsManager? subscribedEffectsManager;

    /// <summary>The container in which this viewport currently registers for invalidation fanout.</summary>
    private ModelContainer3DX? registeredModelContainer;

    /// <summary>Retains shared native ownership across unload until this renderer generation retires.</summary>
    private ModelContainer3DX? resourceModelContainer;

    /// <summary>Remembers shared mutations at notification unload without consuming retained scene demand.</summary>
    private long sharedSceneNotificationRevision;

    /// <summary>Blocks new scene preparation between resource disposal and reinitialization.</summary>
    private bool effectsResourcesUnavailable;

    /// <summary>Retains a required renderer retirement until all application frame callbacks have unwound.</summary>
    private bool managerResourcesResetPending;

    /// <summary>Coalesces rare manager lifecycle transitions into one retained dispatcher operation.</summary>
    private DispatcherOperation? managerLifecycleOperation;

    /// <summary>Retains the callback used only for deferred manager lifecycle transitions.</summary>
    private Action? applyManagerLifecycle;

    /// <summary>Retries only a pending resource transition while GPU work or an image lock prevents retirement.</summary>
    private DispatcherTimer? managerLifecycleRetryTimer;

    /// <summary>Retains final viewport disposal requested from an HWND frame callback.</summary>
    private bool nativeViewportDisposalRequested;

    /// <summary>Owns the one deferred HWND viewport-disposal callback.</summary>
    private DispatcherOperation? nativeViewportDisposalOperation;

    /// <summary>Retains the final HWND disposal delegate outside the frequent frame path.</summary>
    private Action? finishNativeViewportDisposal;

    /// <summary>Gets whether any application frame callback currently borrows presentation resources.</summary>
    internal bool IsPreparingPresentationFrame => isD3D12FrameUpdating || wpfPresentation is { IsUpdating: true };

    /// <summary>Gets whether the current manager and shared-scene transaction allow new scene preparation.</summary>
    private bool CanRenderWithEffectsManager => !effectsResourcesUnavailable && !managerResourcesResetPending &&
                                                EffectsManager is not EffectsManager { ResourcesAvailable: false } &&
                                                !(EnableSharedModelMode && SharedModelContainerInternal is ModelContainer3DX {
                                                    IsSynchronizingEffectsManager: true
                                                });

    /// <summary>Gets whether an HWND recording may still be submitted after application callbacks return.</summary>
    internal bool CanSubmitNativeScene => !IsDisposed && !nativeViewportDisposalRequested && CanRenderWithEffectsManager &&
                                          d3d12Surface is not { IsRetirementPending: true };

    /// <summary>Delivers a frame completed by unload retirement while the actual viewport remains alive.</summary>
    /// <param name="surface">The retained native owner whose drain completed.</param>
    internal void ObserveNativeRetirement(D3D12PresentationSurface surface) {
        if (!IsDisposed && ReferenceEquals(d3d12Surface, surface)) RaiseCompletedD3D12Frames(surface);
    }

    /// <summary>Refreshes shared membership before borrowing any scene and services only pending lifecycle work.</summary>
    internal void PrepareHostFrameContracts() {
        UpdateSharedSceneSubscription(true);
        ScheduleManagerLifecycle();
        if (CanRenderWithEffectsManager && resourceModelContainer is null && registeredModelContainer is { } container) {
            resourceModelContainer = container;
            container.RegisterResourceConsumer(this);
        }
    }

    /// <summary>Retires every renderer lease belonging to this consumer after bounded native completion.</summary>
    /// <exception cref="InvalidOperationException">An application frame callback still owns the renderer.</exception>
    internal void RetireSceneResourcesForManagerChange() {
        Dispatcher.VerifyAccess();
        if (IsPreparingPresentationFrame)
            throw new InvalidOperationException("Scene resources cannot retire while a presentation frame is active.");
        wpfPresentation?.RetireSceneResources();
        if (d3d12Surface is { } surface) {
            _ = surface.CompletePendingFrame(true);
            RaiseCompletedD3D12Frames(surface);
            if (ReferenceEquals(surface, d3d12Surface) && !surface.IsDisposed) surface.RetireSceneResources();
        }

        ReleaseSharedSceneResourceRegistration();
    }

    /// <summary>Forgets shared resource ownership only after successful renderer or full presentation retirement.</summary>
    internal void ReleaseSharedSceneResourceRegistration() {
        var container = resourceModelContainer;
        resourceModelContainer = null;
        container?.UnregisterResourceConsumer(this);
    }

    /// <summary>Drains all submitted GPU use before a borrowed manager frees resources, including during CPU preparation.</summary>
    internal void DrainSubmittedWorkForManagerDisposal() {
        Dispatcher.VerifyAccess();
        wpfPresentation?.DrainSubmittedWorkForManagerDisposal();
        _ = d3d12Surface?.CompletePendingFrame(true);
    }

    /// <summary>Connects lifecycle handlers once without taking ownership of a supplied manager or scene container.</summary>
    private void InitializeHostContracts() {
        applyManagerLifecycle = ApplyQueuedManagerLifecycle;
        Loaded += HostContractsLoaded;
        Unloaded += HostContractsUnloaded;
    }

    /// <summary>Releases subscriptions and queued callbacks without disposing externally supplied objects.</summary>
    private void DetachHostContracts() {
        managerLifecycleOperation?.Abort();
        managerLifecycleOperation = null;
        managerLifecycleRetryTimer?.Stop();
        nativeViewportDisposalOperation?.Abort();
        nativeViewportDisposalOperation = null;
        Loaded -= HostContractsLoaded;
        Unloaded -= HostContractsUnloaded;
        SetSubscribedEffectsManager(null);
        registeredModelContainer?.UnregisterConsumer(this);
        registeredModelContainer = null;
        ReleaseSharedSceneResourceRegistration();
    }

    /// <summary>Registers an active shared-scene consumer and transfers notification ownership when its container changes.</summary>
    /// <param name="active">Whether this viewport is loaded or explicitly preparing an actual frame.</param>
    private void UpdateSharedSceneSubscription(bool active) {
        var configured = !IsDisposed && EnableSharedModelMode
            ? SharedModelContainerInternal as ModelContainer3DX
            : null;
        if (resourceModelContainer is { } resources && !ReferenceEquals(resources, configured)) {
            managerResourcesResetPending = true;
            ScheduleManagerLifecycle();
        }

        var selected = active
            ? configured
            : null;
        if (ReferenceEquals(selected, registeredModelContainer)) return;
        if (registeredModelContainer is { } previous) {
            sharedSceneNotificationRevision = previous.SceneRevision;
            previous.UnregisterConsumer(this);
        }

        registeredModelContainer = selected;
        selected?.RegisterConsumer(
            this, ReferenceEquals(selected, resourceModelContainer)
                ? sharedSceneNotificationRevision
                : null
        );
    }

    /// <summary>Restores shared notification membership after reload.</summary>
    /// <param name="sender">The loaded viewport.</param>
    /// <param name="args">The original lifecycle event.</param>
    private void HostContractsLoaded(object sender, RoutedEventArgs args) => UpdateSharedSceneSubscription(true);

    /// <summary>Removes inactive notification membership while retaining native ownership until its own safe retirement.</summary>
    /// <param name="sender">The unloaded viewport.</param>
    /// <param name="args">The original lifecycle event.</param>
    private void HostContractsUnloaded(object sender, RoutedEventArgs args) => UpdateSharedSceneSubscription(false);

    /// <summary>Updates shared notification membership immediately when either selection property changes.</summary>
    /// <param name="owner">The viewport changing shared-scene selection.</param>
    /// <param name="args">The original dependency-property change.</param>
    private static void SharedSceneSelectionChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) {
        var viewport = (Viewport3DX) owner;
        if (args.Property == SharedModelContainerProperty)
            viewport.SharedModelContainerInternal = (IModelContainer?) args.NewValue;
        viewport.UpdateSharedSceneSubscription(viewport.IsLoaded);
        viewport.InvalidateSceneGraph();
    }

    /// <summary>Defers final application-scene cleanup until an HWND frame no longer borrows its nodes.</summary>
    /// <returns>Whether disposal was retained for the end of the current frame.</returns>
    private bool DeferNativeViewportDisposalIfUpdating() {
        if (!isD3D12FrameUpdating) return false;
        nativeViewportDisposalRequested = true;
        ScheduleNativeViewportDisposal();
        return true;
    }

    /// <summary>Posts one retained final-disposal callback, including after a nested dispatcher pump deferred it.</summary>
    private void ScheduleNativeViewportDisposal() {
        if (!nativeViewportDisposalRequested || IsDisposed || Dispatcher.HasShutdownStarted ||
            nativeViewportDisposalOperation is { Status: DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing })
            return;
        finishNativeViewportDisposal ??= FinishNativeViewportDisposal;
        nativeViewportDisposalOperation = Dispatcher.BeginInvoke(DispatcherPriority.Send, finishNativeViewportDisposal);
    }

    /// <summary>Completes final viewport disposal after the native caller has unwound.</summary>
    private void FinishNativeViewportDisposal() {
        nativeViewportDisposalOperation = null;
        if (!isD3D12FrameUpdating) Dispose();
    }

    /// <summary>Queues or applies a new manager only after its predecessor's renderer resources have retired.</summary>
    /// <param name="owner">The actual viewport.</param>
    /// <param name="args">The manager dependency-property transition.</param>
    private static void EffectsManagerChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) {
        var viewport = (Viewport3DX) owner;
        if (viewport.IsDisposed) return;
        viewport.managerResourcesResetPending = true;
        viewport.InvalidateSceneGraph();
        if (viewport.IsPreparingPresentationFrame || !viewport.CanRetireManagerWithoutWaiting()) {
            viewport.wpfPresentation?.RequestRecreation();
            viewport.ScheduleManagerLifecycle();
        } else {
            viewport.ApplyManagerLifecycle();
        }
    }

    /// <summary>Transfers three event subscriptions exactly once, preserving same-instance disposal state.</summary>
    /// <param name="manager">The currently selected borrowed manager.</param>
    private void SetSubscribedEffectsManager(IEffectsManager? manager) {
        if (ReferenceEquals(subscribedEffectsManager, manager)) return;
        if (subscribedEffectsManager is { } previous) {
            previous.InvalidateRender -= EffectsManagerInvalidated;
            previous.DisposingResources -= EffectsManagerDisposingResources;
            previous.Reinitialized -= EffectsManagerReinitialized;
        }

        subscribedEffectsManager = manager;
        effectsResourcesUnavailable = manager is EffectsManager { ResourcesAvailable: false };
        if (manager is null) return;
        manager.InvalidateRender += EffectsManagerInvalidated;
        manager.DisposingResources += EffectsManagerDisposingResources;
        manager.Reinitialized += EffectsManagerReinitialized;
    }

    /// <summary>Preserves immediate worker-thread demand through the existing coalesced dispatcher notification.</summary>
    /// <param name="sender">The manager issuing invalidation.</param>
    /// <param name="args">The manager's event arguments.</param>
    private void EffectsManagerInvalidated(object? sender, EventArgs args) {
        if (ReferenceEquals(sender, subscribedEffectsManager)) InvalidateRender();
    }

    /// <summary>Drains GPU use synchronously before the manager frees resources after this event returns.</summary>
    /// <param name="sender">The manager about to release its resources.</param>
    /// <param name="args">The manager's event arguments.</param>
    private void EffectsManagerDisposingResources(object? sender, EventArgs args) =>
        DispatchManagerResourceChange(sender, false);

    /// <summary>Rebuilds attachments and pipelines even when the reinitialized manager retains its identity.</summary>
    /// <param name="sender">The manager whose resources are ready again.</param>
    /// <param name="args">The manager's event arguments.</param>
    private void EffectsManagerReinitialized(object? sender, EventArgs args) =>
        DispatchManagerResourceChange(sender, true);

    /// <summary>Marshals rare resource epochs synchronously; ordinary render invalidation uses no lifecycle closure.</summary>
    /// <param name="sender">The resource owner, checked again on the dispatcher to reject stale notifications.</param>
    /// <param name="available">Whether resources are initialized rather than being disposed.</param>
    private void DispatchManagerResourceChange(object? sender, bool available) {
        if (IsDisposed || Dispatcher.HasShutdownStarted || !ReferenceEquals(sender, subscribedEffectsManager)) return;
        if (!Dispatcher.CheckAccess()) {
            Dispatcher.Invoke(
                () => ApplyManagerResourceChange(sender, available), DispatcherPriority.Send,
                CancellationToken.None, TimeSpan.FromSeconds(5)
            );
        } else {
            ApplyManagerResourceChange(sender, available);
        }
    }

    /// <summary>Suspends new preparation and requests a shared retirement barrier before exposing a resource epoch.</summary>
    /// <param name="sender">The still-selected resource owner.</param>
    /// <param name="available">Whether the new resource epoch is available.</param>
    private void ApplyManagerResourceChange(object? sender, bool available) {
        if (IsDisposed || !ReferenceEquals(sender, subscribedEffectsManager)) return;
        effectsResourcesUnavailable = !available;
        managerResourcesResetPending = true;
        if (!available) {
            DrainSubmittedWorkForManagerDisposal();
        }

        (resourceModelContainer ?? registeredModelContainer)?.RequestSharedResourceRetirement();
        InvalidateSceneGraph();
        if (IsPreparingPresentationFrame) {
            // Submitted GPU work is complete; live permission checks discard any open CPU recording before submission.
            wpfPresentation?.RequestRecreation();
            ScheduleManagerLifecycle();
        } else {
            ApplyManagerLifecycle();
        }
    }

    /// <summary>Schedules at most one deferred resource transition; settled frames take only a field check.</summary>
    internal void ScheduleManagerLifecycle() {
        if (!IsDisposed && !IsPreparingPresentationFrame)
            (resourceModelContainer ?? registeredModelContainer)?.SchedulePendingEffectsManagerSynchronization();
        if (!managerResourcesResetPending || IsDisposed || Dispatcher.HasShutdownStarted ||
            managerLifecycleRetryTimer is { IsEnabled: true } ||
            managerLifecycleOperation is { Status: DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing })
            return;
        applyManagerLifecycle ??= ApplyQueuedManagerLifecycle;
        managerLifecycleOperation = Dispatcher.BeginInvoke(DispatcherPriority.Send, applyManagerLifecycle);
    }

    /// <summary>Retries a deferred bounded retirement while retaining owners and demand after failure.</summary>
    private void ApplyQueuedManagerLifecycle() {
        managerLifecycleOperation = null;
        if (IsDisposed || !managerResourcesResetPending || IsPreparingPresentationFrame) return;
        try {
            if (!CanRetireManagerWithoutWaiting()) {
                ScheduleManagerLifecycleRetry();
                return;
            }

            ApplyManagerLifecycle();
        } catch (TimeoutException exception) {
            ScheduleManagerLifecycleRetry();
            LoggerLib.Logger.Error(exception, "Viewport manager resources remain owned for retirement retry");
        }
    }

    /// <summary>Polls accepted work without blocking an ordinary manager identity change or dispatcher callback.</summary>
    /// <returns>Whether the preceding GPU and cross-device copy no longer borrow resources.</returns>
    internal bool CanRetireManagerWithoutWaiting() =>
        wpfPresentation is not { HasPendingGpuWork: true } &&
        (d3d12Surface is null || d3d12Surface.CompletePendingFrame());

    /// <summary>Retains one exceptional timer until pending GPU work or a transient lock permits retirement.</summary>
    private void ScheduleManagerLifecycleRetry() {
        if (IsDisposed || Dispatcher.HasShutdownStarted) return;
        if (managerLifecycleRetryTimer is null) {
            managerLifecycleRetryTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            managerLifecycleRetryTimer.Tick += RetryManagerLifecycle;
        }

        managerLifecycleRetryTimer.Start();
    }

    /// <summary>Stops the exceptional timer before retrying so settled viewports retain no periodic work.</summary>
    /// <param name="sender">The retained lifecycle timer.</param>
    /// <param name="args">The original timer arguments.</param>
    private void RetryManagerLifecycle(object? sender, EventArgs args) {
        managerLifecycleRetryTimer!.Stop();
        ApplyQueuedManagerLifecycle();
    }

    /// <summary>Retires the old generation, transfers subscriptions to the latest value and preserves pending scene demand.</summary>
    private void ApplyManagerLifecycle() {
        RetireSceneResourcesForManagerChange();
        if (IsDisposed) return;
        managerLifecycleRetryTimer?.Stop();
        SetSubscribedEffectsManager(EffectsManager);
        d3d12Surface?.SetEffectsManager(EffectsManager);
        managerResourcesResetPending = false;
        InvalidateSceneGraph();
    }
}