/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SilkD3D12DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D12.ID3D12Device>;
using SilkDxgiAdapterPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGIAdapter>;
using SilkDxgiFactory4Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGIFactory4>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Creates Direct3D 12 devices for hardware or WARP rendering.
/// </summary>
public static unsafe class SilkD3D12DeviceFactory {
    /// <summary>
    ///     Keeps the Direct3D 12 native API loaded for the lifetime of created device vtables.
    /// </summary>
    // ponytail: D3D device vtables remain valid only while the native API library stays loaded.
    internal static readonly D3D12 Api = D3D12.GetApi();

    /// <summary>
    ///     Keeps the DXGI native API loaded while adapter vtables are in use.
    /// </summary>
    internal static readonly DXGI DxgiApi = DXGI.GetApi(null);

    /// <summary>
    ///     Creates the default Direct3D 12 hardware or WARP device.
    /// </summary>
    /// <param name="minimumFeatureLevel">The minimum requested feature level.</param>
    /// <param name="driverType">The hardware or WARP driver type.</param>
    /// <returns>The created Direct3D 12 device.</returns>
    public static SilkD3D12Device CreateDefault(
        SilkFeatureLevel minimumFeatureLevel = SilkFeatureLevel.Level110,
        SilkDriverType driverType = SilkDriverType.Hardware
    ) {
        if (driverType is not (SilkDriverType.Hardware or SilkDriverType.Warp))
            throw new ArgumentOutOfRangeException(nameof(driverType),
                driverType,
                "Direct3D 12 device creation supports hardware and WARP drivers.");

        SilkDxgiFactory4Ptr factory = default;
        SilkDxgiAdapterPtr adapter = default;
        IDXGIAdapter1* hardwareAdapter = null;
        ID3D12Device* nativeDevice = null;
        var deviceGuid = ID3D12Device.Guid;

        try {
            if (driverType == SilkDriverType.Warp) {
                SilkMarshal.ThrowHResult(DxgiApi.CreateDXGIFactory2(0, out factory));
                SilkMarshal.ThrowHResult(factory.EnumWarpAdapter(out adapter));
            } else {
                SilkMarshal.ThrowHResult(DxgiApi.CreateDXGIFactory2(0, out factory));
                var adapterIndex = SelectHardwareAdapter(factory, minimumFeatureLevel);
                SilkMarshal.ThrowHResult(factory.Handle->EnumAdapters1((uint) adapterIndex, &hardwareAdapter));
            }

            var nativeAdapter = driverType == SilkDriverType.Warp
                ? (IUnknown*) adapter.Handle
                : (IUnknown*) hardwareAdapter;
            SilkMarshal.ThrowHResult(Api.CreateDevice(nativeAdapter,
                ToSilkFeatureLevel(minimumFeatureLevel),
                &deviceGuid,
                (void**) &nativeDevice));

            return new SilkD3D12Device(new SilkD3D12DevicePtr(nativeDevice), minimumFeatureLevel);
        } finally {
            if (hardwareAdapter != null) hardwareAdapter->Release();
            adapter.Dispose();
            factory.Dispose();
        }
    }

    /// <summary>
    ///     Enumerates and ranks hardware adapters that support the requested Direct3D 12 feature level.
    /// </summary>
    /// <param name="factory">The DXGI factory.</param>
    /// <param name="minimumFeatureLevel">The requested feature level.</param>
    /// <returns>The selected DXGI adapter index.</returns>
    private static int SelectHardwareAdapter(
        SilkDxgiFactory4Ptr factory,
        SilkFeatureLevel minimumFeatureLevel
    ) {
        const int dxgiErrorNotFound = unchecked((int) 0x887A0002);
        var candidates = new List<D3D12AdapterCandidate>();
        for (uint index = 0;; index++) {
            IDXGIAdapter1* candidate = null;
            var result = factory.Handle->EnumAdapters1(index, &candidate);
            if (result == dxgiErrorNotFound)
                break;
            SilkMarshal.ThrowHResult(result);

            try {
                AdapterDesc1 description;
                candidate->GetDesc1(&description);
                ID3D12Device* testDevice = null;
                var deviceGuid = ID3D12Device.Guid;
                var createResult = Api.CreateDevice((IUnknown*) candidate,
                    ToSilkFeatureLevel(minimumFeatureLevel),
                    &deviceGuid,
                    (void**) &testDevice);
                if (testDevice != null) testDevice->Release();
                candidates.Add(new D3D12AdapterCandidate((int) index,
                    ((AdapterFlag) description.Flags).HasFlag(AdapterFlag.Software),
                    description.DedicatedVideoMemory,
                    createResult >= 0));
            } finally {
                if (candidate != null) candidate->Release();
            }
        }

        return D3D12AdapterSelector.Select(candidates);
    }

    /// <summary>
    ///     Converts the toolkit feature level to the Silk.NET Direct3D value.
    /// </summary>
    /// <param name="featureLevel">The toolkit feature level.</param>
    /// <returns>The corresponding native feature level.</returns>
    private static D3DFeatureLevel ToSilkFeatureLevel(SilkFeatureLevel featureLevel) {
        return featureLevel switch {
            SilkFeatureLevel.Level111 => D3DFeatureLevel.Level111,
            SilkFeatureLevel.Level110 => D3DFeatureLevel.Level110,
            SilkFeatureLevel.Level101 => D3DFeatureLevel.Level101,
            SilkFeatureLevel.Level100 => D3DFeatureLevel.Level100,
            _ => throw new ArgumentOutOfRangeException(nameof(featureLevel), featureLevel, "Unsupported feature level.")
        };
    }
}
