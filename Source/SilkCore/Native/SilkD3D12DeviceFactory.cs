/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using SilkD3D12DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Device>;

namespace HelixToolkit.SharpDX.Core.Native;
public static unsafe class SilkD3D12DeviceFactory {
    // ponytail: D3D device vtables remain valid only while the native API library stays loaded.
    internal static readonly D3D12 Api = D3D12.GetApi();

    public static SilkD3D12Device CreateDefault(
        SilkFeatureLevel minimumFeatureLevel = SilkFeatureLevel.Level110
    ) {
        ID3D12Device* nativeDevice = null;
        var deviceGuid = ID3D12Device.Guid;

        SilkMarshal.ThrowHResult(Api.CreateDevice(null,
                                                  ToSilkFeatureLevel(minimumFeatureLevel),
                                                  ref deviceGuid,
                                                  (void**)&nativeDevice));

        return new SilkD3D12Device(new SilkD3D12DevicePtr(nativeDevice), minimumFeatureLevel);
    }

    private static D3DFeatureLevel ToSilkFeatureLevel(SilkFeatureLevel featureLevel) {
        return featureLevel switch {
            SilkFeatureLevel.Level111 => D3DFeatureLevel.Level111,
            SilkFeatureLevel.Level110 => D3DFeatureLevel.Level110,
            SilkFeatureLevel.Level101 => D3DFeatureLevel.Level101,
            SilkFeatureLevel.Level100 => D3DFeatureLevel.Level100,
            _ => D3DFeatureLevel.Level110
        };
    }
}
