using System.Runtime.InteropServices;

namespace HelixToolkit.SharpDX.Core.Utilities;
#if !WINDOWS_UWP
/// <summary>
///     Enable dedicated graphics card for rendering.
///     https://stackoverflow.com/questions/17270429/forcing-hardware-accelerated-rendering
/// </summary>
public sealed class NVOptimusEnabler {
    static NVOptimusEnabler() {
        try {
            if (Environment.Is64BitProcess)
                NativeMethods.LoadNvApi64();
            else
                NativeMethods.LoadNvApi32();
        } catch { } // will always fail since 'fake' entry point doesn't exists
    }
}

internal static class NativeMethods {
    [DllImport("nvapi64.dll", EntryPoint = "fake")]
    internal static extern int LoadNvApi64();

    [DllImport("nvapi.dll", EntryPoint = "fake")]
    internal static extern int LoadNvApi32();
}
#endif
