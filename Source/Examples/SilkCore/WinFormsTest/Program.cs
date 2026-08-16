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

}
