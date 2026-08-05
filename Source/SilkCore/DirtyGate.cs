using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// Coordinates deferred work that must be executed after a resource or state
/// has been invalidated.
/// </summary>
/// <remarks>
/// <para>
/// Multiple invalidations are coalesced. If the gate is invalidated several
/// times before the action is executed, a single execution processes the
/// latest observed invalidation version.
/// </para>
/// <para>
/// If the gate is invalidated while the action is running, the invalidation
/// is preserved and the action will be executed again on a later
/// <see cref="TryExecute(Action)"/> call.
/// </para>
/// <para>
/// If the action throws an exception, the processed version is not advanced.
/// The gate therefore remains dirty and the operation can be retried.
/// </para>
/// <para>
/// This class serializes execution of the supplied action, but it does not
/// automatically synchronize access to external mutable state read or written
/// by that action. The caller remains responsible for the thread safety of
/// that external state.
/// </para>
/// <para>
/// The action is executed synchronously while the internal lock is held.
/// Asynchronous delegates must not be passed to this class.
/// </para>
/// </remarks>
[DebuggerDisplay("IsDirty = {IsDirty}, RequestedVersion = {requestedVersion}, ProcessedVersion = {processedVersion}")]
public sealed class DirtyGate {
    private readonly Lock executionLock = new();

    
    /// <summary>
    /// Incremented whenever new work is requested.
    /// 
    /// Interlocked.Increment provides an atomic update and publishes memory
    /// writes that occurred before Invalidate was called.
    /// </summary>
    private long requestedVersion;


    /// <summary>
    /// Contains the latest version successfully processed by an action.
    ///
    /// This field is published with Volatile.Write after the action has
    /// completed successfully.
    /// </summary>
    private long processedVersion;


    /// <summary>
    /// Protects against recursive execution on the same thread.
    ///
    /// System.Threading.Lock is reentrant. Without this guard, an action that
    /// calls TryExecute on the same DirtyGate could recursively execute itself.
    ///
    /// This field is accessed only while _executionLock is held.
    /// </summary>
    private bool isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirtyGate"/> class.
    /// </summary>
    /// <param name="initiallyDirty">
    /// <see langword="true"/> if the first call to <c>TryExecute</c> should
    /// execute the supplied action; otherwise, <see langword="false"/>.
    /// </param>
    public DirtyGate(bool initiallyDirty = false)
        => requestedVersion = initiallyDirty
                                  ? 1L
                                  : 0L;

    /// <summary>
    /// Gets a value indicating whether unprocessed work is currently pending.
    /// </summary>
    /// <remarks>
    /// This property represents a moment-in-time observation. The state may
    /// change immediately after the property returns.
    /// </remarks>
    public bool IsDirty {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            
            //Read the processed version first.
            //
            //A concurrent execution may produce a harmless false positive,
            //but an invalidation that was already visible before this check
            //is not accidentally hidden by a later processed-version read.
            
            var processed = Volatile.Read(ref processedVersion);
            var requested = Volatile.Read(ref requestedVersion);

            return processed != requested;
        }
    }

    /// <summary>
    /// Marks the gate as dirty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and does not acquire the execution lock.
    /// It may therefore be called while another thread is executing the
    /// registered action.
    /// </para>
    /// <para>
    /// Modify the associated state before calling this method. The atomic
    /// increment publishes those preceding writes to a thread that observes
    /// the new version.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate() => Interlocked.Increment(ref requestedVersion);

    /// <summary>
    /// Executes the specified action if the gate is dirty.
    /// </summary>
    /// <param name="action">
    /// The synchronous action that processes the invalidated state.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this call executed the action;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The action recursively attempted to execute the same
    /// <see cref="DirtyGate"/>.
    /// </exception>
    public bool TryExecute(Action action) {
        ArgumentNullException.ThrowIfNull(action);

         // Forward the delegate as state and use a non-capturing static lambda.
         // DirtyGate itself creates no closure for this overload.
        return TryExecuteCore(action,
                              static callback => callback());
    }

    /// <summary>
    /// Executes the specified action with caller-provided state if the gate
    /// is dirty.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of state passed to the action.
    /// </typeparam>
    /// <param name="state">
    /// The state passed directly to <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    /// The synchronous action that processes the invalidated state.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this call executed the action;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The action recursively attempted to execute the same
    /// <see cref="DirtyGate"/>.
    /// </exception>
    /// <remarks>
    /// Use this overload with a static lambda to avoid capturing caller state:
    /// <code>
    /// gate.TryExecute(this, static instance => instance.UpdateResource());
    /// </code>
    /// </remarks>
    public bool TryExecute<TState>(
        TState state,
        Action<TState> action
    ) =>
        TryExecuteCore(state, action);

    private bool TryExecuteCore<TState>(
        TState state,
        Action<TState> action
    ) {
        // Lock-free fast path.
        // In the normal clean state, no lock is acquired.
        if (!IsDirty)
            return false;

        lock (executionLock) {
            
            // Another thread may have processed the current version while
            // this thread was waiting for the lock.
            var requestedVersion = Volatile.Read(ref this.requestedVersion);

            if (requestedVersion == Volatile.Read(ref processedVersion))
                return false;


            //System.Threading.Lock is reentrant. Explicitly reject recursive
            //execution of the same gate because it would otherwise execute
            //the same dirty version repeatedly.
            if (isExecuting) 
                throw new InvalidOperationException("The same DirtyGate cannot be executed recursively.");

            isExecuting = true;

            try {
                //The processed version is advanced only after successful
                //completion. If the action throws, the gate remains dirty.
                action(state);

                // Publish all writes performed by the action before making the
                // processed version visible to other threads.
                //
                // Only the version captured before the action is marked as
                // processed. An invalidation occurring during the action
                // therefore remains pending.
                Volatile.Write(ref processedVersion, requestedVersion);

                return true;
            } finally {
                isExecuting = false;
            }
        }
    }
}
