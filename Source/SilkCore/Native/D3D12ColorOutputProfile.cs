using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Encodes the immutable presentation profile in the existing per-frame shader payload.</summary>
internal static class D3D12ColorOutputProfile {
    /// <summary>The reserved weight-mode bit identifying premultiplied color output.</summary>
    internal const int PremultipliedFlag = 1 << 30;

    /// <summary>The reserved bit marking opaque desktop pixels whose DXGI alpha is not scene coverage.</summary>
    internal const int DesktopCaptureFlag = 1 << 29;

    /// <summary>Copies frame constants and selects the output profile without changing the OIT weight mode.</summary>
    /// <param name="transforms">The caller's unchanged frame constants.</param>
    /// <param name="mode">The renderer's immutable output profile.</param>
    /// <returns>The value payload for one GPU binding.</returns>
    internal static GlobalTransformStruct Apply(in GlobalTransformStruct transforms, D3D12ColorOutputMode mode) {
        var result = transforms;
        result.OITWeightMode &= ~PremultipliedFlag;
        if (mode == D3D12ColorOutputMode.Premultiplied) result.OITWeightMode |= PremultipliedFlag;
        return result;
    }
}
