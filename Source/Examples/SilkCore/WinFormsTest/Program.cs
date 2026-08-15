using LoggerLib;

namespace WinFormsTest;

internal static partial class Program {
    private static readonly int  optimumsReturnValue = NvOptimusEnabler.Enable();

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main() {
        Logger.Info("Optimus return value: {OptimiusValue}", optimumsReturnValue);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }


    public static class NvOptimusEnabler {
        public static int Enable() {
            try {
                return Environment.Is64BitProcess
                    ? NativeMethods.LoadNvApi64()
                    : NativeMethods.LoadNvApi32();
            } catch {
                // will always fail since 'fake' entry point doesn't exists
                Logger.Error("Failed to load NVAPI");
            }

            return -1;
        }
    }

    internal static partial class NativeMethods {
        [System.Runtime.InteropServices.LibraryImport("nvapi64.dll", EntryPoint = "fake")]
        internal static partial int LoadNvApi64();

        [System.Runtime.InteropServices.LibraryImport("nvapi.dll", EntryPoint = "fake")]
        internal static partial int LoadNvApi32();
    }
}
