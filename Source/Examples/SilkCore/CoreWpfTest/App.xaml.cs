using System.Windows;

namespace CoreWpfTest;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    private static NvOptimusEnabler? _enabler;

    protected override void OnStartup(StartupEventArgs e) {
        _enabler = new NvOptimusEnabler();
        base.OnStartup(e);
    }

    private sealed class NvOptimusEnabler {
        static NvOptimusEnabler() {
            try {
                if (Environment.Is64BitProcess)
                    NativeMethods.LoadNvApi64();
                else
                    NativeMethods.LoadNvApi32();
            } catch {
                LoggerLib.Logger.Error("Failed to load NVAPI");
            } // will always fail since 'fake' entry point doesn't exists
        }
    };

    internal static class NativeMethods {
        [System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi64();

        [System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi32();
    }
}