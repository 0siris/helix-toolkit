namespace HelixToolkit.SharpDX.Core.Utilities;
#if !WINDOWS_UWP
/// <summary>
///     Enable dedicated graphics card for rendering.
///     https://stackoverflow.com/questions/17270429/forcing-hardware-accelerated-rendering
/// </summary>
public static class NvOptimusEnabler {
    public static int Enable() {
        try {
            return Environment.Is64BitProcess
                ? NativeMethods.LoadNvApi64()
                : NativeMethods.LoadNvApi32();
        } catch {
            // will always fail since 'fake' entry point doesn't exists
            LoggerLib.Logger.Error("Failed to load NVAPI");
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

#endif
