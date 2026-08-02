using System.Collections.Concurrent;

namespace HelixToolkit.SharpDX.Core;

internal sealed class AsyncActionWaitable : DisposeObject {
    private static readonly ConcurrentBag<AsyncActionWaitable> pool = [];
    private readonly object waitable = new();
    private Action action;

    private AsyncActionWaitable() { }

    public void SetAction(Action action) {
        this.action = action;
    }

    public void Trigger() {
        lock (waitable) {
            var a = action;
            action = null;
            a?.Invoke();
            Monitor.Pulse(waitable);
        }
    }

    public void Wait() {
        lock (waitable) {
            if (action == null) return;
            Monitor.Wait(waitable);
        }
    }

    public static AsyncActionWaitable Get() {
        if (!pool.TryTake(out var obj))
            obj = new AsyncActionWaitable {
                AddBackToPool = Put
            };
        obj.IncRef();
        return obj;
    }

    private static void Put(DisposeObject obj) {
        if (obj is AsyncActionWaitable t) {
            t.action = null;
            pool.Add(t);
        }
    }
}

/// <summary>
///     Used to run real-time non-rendering tasks in RenderHost.
/// </summary>
internal sealed class AsyncActionThread : IDisposable {
    private readonly Queue<AsyncActionWaitable> jobs = new();
    private bool disposedValue;

    private Thread jobThread;
    private volatile bool running = true;

    public bool Enabled { get; set; }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~AsyncActionThread()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public AsyncActionWaitable EnqueueAction(Action action) {
        if (!running || !Enabled) {
            action.Invoke();
            return null;
        }

        var obj = AsyncActionWaitable.Get();
        obj.SetAction(action);
        lock (jobs) {
            jobs.Enqueue(obj);
            Monitor.Pulse(jobs);
        }

        return obj;
    }

    public void Start() {
        if (jobThread != null && jobThread.IsAlive) return;
        running = true;
        Clear();
        jobThread = new Thread(() => {
            while (running)
                lock (jobs) {
                    while (jobs.Count > 0 && running) {
                        var job = jobs.Dequeue();
                        Monitor.Exit(jobs);
                        job.Trigger();
                        Monitor.Enter(jobs);
                    }

                    Monitor.Wait(jobs, 100);
                }

            Clear();
        }) {
            Priority = ThreadPriority.AboveNormal
        };
        jobThread.Start();
    }

    public void Stop() {
        if (!running || jobThread == null) return;
        running = false;
        if (jobThread.IsAlive) {
            lock (jobs) {
                Monitor.Pulse(jobs);
            }

            jobThread.Join();
            jobThread = null;
        }
    }

    private void Clear() {
        lock (jobs) {
            while (jobs.Count > 0) jobs.Dequeue().Dispose();
        }
    }

    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) Stop();

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }
}
