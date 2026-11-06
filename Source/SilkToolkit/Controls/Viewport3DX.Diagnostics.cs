using System;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Identifies the adapter of an existing D3D12 device, never a candidate adapter list.</summary>
/// <param name="Luid">The identity read from ID3D12Device.GetAdapterLuid.</param>
/// <param name="Name">The matching DXGI description, or null when metadata is unavailable.</param>
/// <param name="IsSoftware">The matching DXGI software flag, or null when metadata is unavailable.</param>
public sealed record D3D12AdapterDiagnostics(long Luid, string? Name, bool? IsSoftware);

/// <summary>Captures the existing presentation without submitting work or creating a device.</summary>
/// <param name="IsInitialized">Whether the selected presentation currently owns an initialized transport.</param>
/// <param name="Presentation">The actual existing presentation, or null before initialization.</param>
/// <param name="Adapter">The actual device adapter, or null when no live device exists.</param>
/// <param name="InteropAdapterLuid">The verified D3D11/D3D9Ex bridge adapter, or null without a bridge.</param>
/// <param name="IsFrontBufferAvailable">The current D3DImage front-buffer availability, or null without one.</param>
public sealed record ViewportRenderDiagnostics(bool IsInitialized, string? Presentation,
    D3D12AdapterDiagnostics? Adapter, long? InteropAdapterLuid, bool? IsFrontBufferAvailable);

public partial class Viewport3DX {
    /// <summary>Gets the passive bounded recorder; disabled until the application explicitly starts it.</summary>
    public RenderFlightRecorder FlightRecorder { get; } = new();

    /// <summary>Reads live dispatcher-owned state without initializing, probing or changing the renderer.</summary>
    /// <returns>A detached snapshot of the actual presentation and its device identity.</returns>
    public ViewportRenderDiagnostics GetRenderDiagnostics() {
        Dispatcher.VerifyAccess();
        if (IsDisposed) return new(false, null, null, null, null);
        if (wpfPresentation is { IsInitialized: true } wpf)
            return new(true, "D3D12 → D3D11 / D3D9Ex → WPF D3DImage",
                ReadAdapterDiagnostics(wpf.DiagnosticDevice), wpf.DiagnosticInteropAdapterLuid,
                wpf.Canvas.ImageSource.IsFrontBufferAvailable);
        if (d3d12Surface is { IsInitialized: true } surface)
            return new(true, "D3D12 / HWND SwapChain", ReadAdapterDiagnostics(surface.DiagnosticDevice), null, null);
        return new(false, null, null, null, null);
    }

    /// <summary>Resolves metadata solely by the live device LUID; a failed lookup retains the known identity.</summary>
    /// <param name="device">The borrowed live presentation device.</param>
    /// <returns>The actual device identity, or null without a device.</returns>
    internal static unsafe D3D12AdapterDiagnostics? ReadAdapterDiagnostics(SilkD3D12Device? device) {
        if (device is null) return null;
        var luid = device.Handle->GetAdapterLuid();
        var identity = unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
        ComPtr<IDXGIFactory4> factory = default;
        IDXGIAdapter1* adapter = null;
        try {
            if (SilkD3D12DeviceFactory.DxgiApi.CreateDXGIFactory2(0, out factory) < 0)
                return new(identity, null, null);
            var adapterGuid = IDXGIAdapter1.Guid;
            if (factory.Handle->EnumAdapterByLuid(luid, &adapterGuid, (void**) &adapter) < 0)
                return new(identity, null, null);
            AdapterDesc1 description;
            if (adapter->GetDesc1(&description) < 0 || description.AdapterLuid.High != luid.High ||
                description.AdapterLuid.Low != luid.Low)
                return new(identity, null, null);
            var name = new string(description.Description).TrimEnd('\0').Trim();
            return new(identity, name, ((AdapterFlag) description.Flags & AdapterFlag.Software) != 0);
        } finally {
            if (adapter != null) adapter->Release();
            factory.Dispose();
        }
    }
}
