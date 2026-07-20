using System.Windows.Threading;
using Xunit;

namespace SilkToolkit.Tests {
    internal static class StaThread {
        public static Task RunAsync(Action action) {
            return RunAsync(() => {
                action();
                return Task.CompletedTask;
            });
        }

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
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return Task.Run(() => {
                if (!thread.Join(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("The STA dispatcher thread did not stop within 30 seconds.");
                if (failure is not null)
                    throw failure;
            });
        }
    }

    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class WpfCollection {
        public const string Name = "WPF and native resources";
    }
}
