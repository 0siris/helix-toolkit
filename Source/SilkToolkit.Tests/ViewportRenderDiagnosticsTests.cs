using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using Silk.NET.Direct3D12;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies device identity without a window, scene, hardware device or frame submission.</summary>
public sealed class ViewportRenderDiagnosticsTests {
    /// <summary>Retains an unknown adapter without initializing any native API or renderer.</summary>
    [Fact]
    public void MissingDeviceHasNoAdapter() {
        var diagnostics = Viewport3DX.ReadAdapterDiagnostics(null);

        Assert.Null(diagnostics);
    }

    /// <summary>Matches real device LUID and software metadata using explicit software WARP without hardware fallback.</summary>
    [Fact]
    public unsafe void AdapterIdentityComesFromExistingWarpDevice() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        var pointer = device.NativePointer;
        var nativeLuid = ((ID3D12Device*) pointer)->GetAdapterLuid();
        var expectedLuid = unchecked((long) ((ulong) (uint) nativeLuid.High << 32 | nativeLuid.Low));

        var first = Viewport3DX.ReadAdapterDiagnostics(device);
        var second = Viewport3DX.ReadAdapterDiagnostics(device);

        Assert.NotNull(first);
        Assert.Equal(expectedLuid, first.Luid);
        Assert.False(string.IsNullOrWhiteSpace(first.Name));
        Assert.True(first.IsSoftware);
        Assert.Equal(first, second);
        Assert.False(device.IsDisposed);
        Assert.Equal(pointer, device.NativePointer);
    }
}
