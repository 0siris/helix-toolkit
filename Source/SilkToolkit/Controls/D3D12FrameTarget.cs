using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.DXGI;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Describes a borrowed color target without owning a window, swap chain, or GPU submission.</summary>
/// <param name="ColorResource">The color allocation, retained by its presentation owner until GPU completion.</param>
/// <param name="RenderTargetView">The render-target descriptor belonging to the color allocation.</param>
/// <param name="Width">The nonzero physical width.</param>
/// <param name="Height">The nonzero physical height.</param>
/// <param name="Format">The actual color format used by the target and graphics pipelines.</param>
internal readonly record struct D3D12FrameTarget(
    SilkD3D12Resource ColorResource,
    SilkD3D12Descriptor RenderTargetView,
    uint Width,
    uint Height,
    Format Format
);