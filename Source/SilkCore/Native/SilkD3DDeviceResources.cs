/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;
internal sealed class SilkD3DDeviceResources : INativeDeviceResources {
    public SilkD3DDeviceResources(
        int adapterIndex,
        SilkD3DDevice device,
        SilkD3DDeviceContext immediateContext
    ) {
        AdapterIndex = adapterIndex;
        Device = device ?? throw new ArgumentNullException(nameof(device));
        ImmediateContext = immediateContext ?? throw new ArgumentNullException(nameof(immediateContext));
    }

    public int AdapterIndex { get; }

    public SilkD3DDevice Device { get; }

    public SilkD3DDeviceContext ImmediateContext { get; }

    public SilkDriverType DriverType => Device.DriverType;

    public SilkFeatureLevel FeatureLevel => Device.FeatureLevel;

    public void Dispose() {
        ImmediateContext.Dispose();
        Device.Dispose();
    }
}
