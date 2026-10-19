namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Identifies the immutable color-compositing contract selected by a native presentation owner.</summary>
internal enum D3D12ColorOutputMode {
    /// <summary>Preserves the existing HWND shader and blend behavior.</summary>
    Legacy = 0,

    /// <summary>Preserves coverage alpha and stores premultiplied color for composited presentation.</summary>
    Premultiplied = 1
}
