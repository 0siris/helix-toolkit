/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SilkD3D11ContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DeviceContext>;
using SilkD3D11DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Device>;

namespace HelixToolkit.SharpDX.Core.Native;
internal static unsafe class SilkD3D11DeviceFactory {
    private const uint D3D11SdkVersion = 7;

    // ponytail: D3D device vtables remain valid only while the native API library stays loaded.
    private static readonly D3D11 D3D11Api = D3D11.GetApi(null);

    private static readonly D3DFeatureLevel[] DefaultFeatureLevels = [
        D3DFeatureLevel.Level111,
        D3DFeatureLevel.Level110,
        D3DFeatureLevel.Level101,
        D3DFeatureLevel.Level100
    ];

    [Obsolete]
    public static SilkD3DDeviceResources CreateDefault(
        int adapterIndex = 0,
        SilkDriverType driverType = SilkDriverType.Hardware,
        bool enableDebugLayer = false
    ) {
        var flags = CreateDeviceFlag.CreateDeviceBgraSupport;

        if (enableDebugLayer) flags |= CreateDeviceFlag.CreateDeviceDebug;

        ID3D11Device* nativeDevice = null;
        ID3D11DeviceContext* nativeContext = null;
        var selectedFeatureLevel = D3DFeatureLevel.Level110;

        fixed (D3DFeatureLevel* featureLevels = DefaultFeatureLevels) {
            var result = D3D11Api.CreateDevice(null,
                                               ToSilkDriverType(driverType),
                                               nint.Zero,
                                               (uint)flags,
                                               featureLevels,
                                               (uint)DefaultFeatureLevels.Length,
                                               D3D11SdkVersion,
                                               ref nativeDevice,
                                               ref selectedFeatureLevel,
                                               ref nativeContext);

            Marshal.ThrowExceptionForHR(result);
        }

        var device = new SilkD3DDevice(new SilkD3D11DevicePtr(nativeDevice),
                                       driverType,
                                       FromSilkFeatureLevel(selectedFeatureLevel));
        var context = new SilkD3DDeviceContext(new SilkD3D11ContextPtr(nativeContext), false);
        return new SilkD3DDeviceResources(adapterIndex, device, context);
    }

    private static D3DDriverType ToSilkDriverType(SilkDriverType driverType) {
        return driverType switch {
            SilkDriverType.Hardware => D3DDriverType.Hardware,
            SilkDriverType.Warp => D3DDriverType.Warp,
            SilkDriverType.Reference => D3DDriverType.Reference,
            SilkDriverType.Software => D3DDriverType.Software,
            _ => D3DDriverType.Unknown
        };
    }

    private static SilkFeatureLevel FromSilkFeatureLevel(D3DFeatureLevel featureLevel) {
        return featureLevel switch {
            D3DFeatureLevel.Level111 => SilkFeatureLevel.Level111,
            D3DFeatureLevel.Level110 => SilkFeatureLevel.Level110,
            D3DFeatureLevel.Level101 => SilkFeatureLevel.Level101,
            D3DFeatureLevel.Level100 => SilkFeatureLevel.Level100,
            D3DFeatureLevel.Level93 => SilkFeatureLevel.Level93,
            D3DFeatureLevel.Level92 => SilkFeatureLevel.Level92,
            D3DFeatureLevel.Level91 => SilkFeatureLevel.Level91,
            _ => SilkFeatureLevel.Unknown
        };
    }
}
