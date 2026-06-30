/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Native
    {
        public interface INativeDeviceResources : IDisposable
        {
            int AdapterIndex { get; }

            SilkD3DDevice Device { get; }

            SilkD3DDeviceContext ImmediateContext { get; }

            SilkDriverType DriverType { get; }

            SilkFeatureLevel FeatureLevel { get; }
        }
    }
}
