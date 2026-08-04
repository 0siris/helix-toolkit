/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;
public interface INativeDeviceResources : IDisposable {
    int AdapterIndex { get; }

    SilkD3DDevice Device { get; }

    SilkD3DDeviceContext ImmediateContext { get; }

    SilkDriverType DriverType { get; }

    SilkFeatureLevel FeatureLevel { get; }
}
