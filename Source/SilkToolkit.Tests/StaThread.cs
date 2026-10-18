using System.Windows.Threading;
using System.Runtime.ExceptionServices;
using Xunit;

namespace SilkToolkit.Tests;
/// <summary>Executes headless WPF checks on a dedicated dispatcher thread.</summary>
internal static class StaThread {
    /// <summary>Runs the supplied synchronous check on an STA dispatcher.</summary>
    /// <param name="action">The check to execute.</param>
    /// <returns>The completion task, retaining the original failure stack.</returns>
    public static Task RunAsync(Action action) {
        return RunAsync(() => {
            action();
            return Task.CompletedTask;
        });
    }

    /// <summary>Runs the supplied asynchronous check on an STA dispatcher.</summary>
    /// <param name="action">The check to execute.</param>
    /// <returns>The completion task, retaining the original failure stack.</returns>
    /// <exception cref="TimeoutException">The dispatcher thread does not stop within thirty seconds.</exception>
    public static Task RunAsync(Func<Task> action) {
        Exception? failure = null;
        var thread = new Thread(() => {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.InvokeAsync(async () => {
                try {
                    await action();
                } catch (Exception exception) {
                    failure = exception;
                } finally {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                }
            });
            Dispatcher.Run();
        }) {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return Task.Run(() => {
            if (!thread.Join(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("The STA dispatcher thread did not stop within 30 seconds.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        });
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfCollection {
    public const string Name = "WPF and native resources";
}
