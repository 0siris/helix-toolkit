/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SilkD3D11BufferPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Buffer>;
using SilkD3D11ResourcePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Resource>;
using SilkD3D11Texture1DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture1D>;
using SilkD3D11Texture2DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>;
using SilkD3D11Texture3DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture3D>;

using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.SharpDX.Core.Native;
internal static class D3DResourceConversions {
    public static BufferDesc ToSilkDesc(this BufferDescription description) => new() {
        ByteWidth = (uint)description.SizeInBytes,
        Usage = (Silk.NET.Direct3D11.Usage)description.Usage,
        BindFlags = (uint)description.BindFlags,
        CPUAccessFlags = (uint)description.CpuAccessFlags,
        MiscFlags = (uint)description.OptionFlags,
        StructureByteStride = (uint)description.StructureByteStride
    };

    public static Texture1DDesc ToSilkDesc(this Texture1DDescription description) => new() {
        Width = (uint)description.Width,
        MipLevels = (uint)description.MipLevels,
        ArraySize = (uint)description.ArraySize,
        Format = description.Format,
        Usage = (Silk.NET.Direct3D11.Usage)description.Usage,
        BindFlags = (uint)description.BindFlags,
        CPUAccessFlags = (uint)description.CpuAccessFlags,
        MiscFlags = (uint)description.OptionFlags
    };

    public static Texture2DDesc ToSilkDesc(this Texture2DDescription description) => new() {
        Width = (uint)description.Width,
        Height = (uint)description.Height,
        MipLevels = (uint)description.MipLevels,
        ArraySize = (uint)description.ArraySize,
        Format = description.Format,
        SampleDesc = new SampleDesc((uint)description.SampleDescription.Count,
            (uint)description.SampleDescription.Quality),
        Usage = (Silk.NET.Direct3D11.Usage)description.Usage,
        BindFlags = (uint)description.BindFlags,
        CPUAccessFlags = (uint)description.CpuAccessFlags,
        MiscFlags = (uint)description.OptionFlags
    };

    public static Texture3DDesc ToSilkDesc(this Texture3DDescription description) => new() {
        Width = (uint)description.Width,
        Height = (uint)description.Height,
        Depth = (uint)description.Depth,
        MipLevels = (uint)description.MipLevels,
        Format = description.Format,
        Usage = (Silk.NET.Direct3D11.Usage)description.Usage,
        BindFlags = (uint)description.BindFlags,
        CPUAccessFlags = (uint)description.CpuAccessFlags,
        MiscFlags = (uint)description.OptionFlags
    };

    public static Map ToSilkMap(this MapMode mode) => (Map)mode;

    public static uint ToSilkMapFlags(this MapFlags flags) => (uint)flags;

    public static unsafe DataBox ToDataBox(this MappedSubresource mapped) => new((nint)mapped.PData, (int)mapped.RowPitch, (int)mapped.DepthPitch);
}
