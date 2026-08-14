namespace WinFormsTest;

internal static class Program {
    private static NvOptimusEnabler _nvEnabler = new();

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main() {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }

    public sealed class NvOptimusEnabler {
        static NvOptimusEnabler() {
            try {
                if (Environment.Is64BitProcess)
                    NativeMethods.LoadNvApi64();
                else
                    NativeMethods.LoadNvApi32();
            } catch { } // will always fail since 'fake' entry point doesn't exists
        }
    };

    internal static class NativeMethods {
        [System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi64();

        [System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi32();
    }
}
