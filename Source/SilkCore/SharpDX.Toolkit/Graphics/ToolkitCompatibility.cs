/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     SharpDX-compatible DXGI format value used by the legacy Toolkit image path.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly struct Format : IEquatable<Format> {
    private Format(Silk.NET.DXGI.Format value) {
        NativeFormat = value;
    }

    internal Silk.NET.DXGI.Format NativeFormat { get; }

    public static implicit operator Silk.NET.DXGI.Format(Format format) {
        return format.NativeFormat;
    }

    public static implicit operator Format(Silk.NET.DXGI.Format format) {
        return new Format(format);
    }

    public static explicit operator int(Format format) {
        return (int)format.NativeFormat;
    }

    public bool Equals(Format other) {
        return NativeFormat == other.NativeFormat;
    }

    public override bool Equals(object obj) {
        return obj is Format other && Equals(other);
    }

    public override int GetHashCode() {
        return NativeFormat.GetHashCode();
    }

    public override string ToString() {
        return NativeFormat.ToString();
    }

    public static bool operator ==(Format left, Format right) {
        return left.Equals(right);
    }

    public static bool operator !=(Format left, Format right) {
        return !left.Equals(right);
    }

    public static readonly Format Unknown = Silk.NET.DXGI.Format.FormatUnknown;
    public static readonly Format R32G32B32A32_Typeless = Silk.NET.DXGI.Format.FormatR32G32B32A32Typeless;
    public static readonly Format R32G32B32A32_Float = Silk.NET.DXGI.Format.FormatR32G32B32A32Float;
    public static readonly Format R32G32B32A32_UInt = Silk.NET.DXGI.Format.FormatR32G32B32A32Uint;
    public static readonly Format R32G32B32A32_SInt = Silk.NET.DXGI.Format.FormatR32G32B32A32Sint;
    public static readonly Format R32G32B32_Typeless = Silk.NET.DXGI.Format.FormatR32G32B32Typeless;
    public static readonly Format R32G32B32_Float = Silk.NET.DXGI.Format.FormatR32G32B32Float;
    public static readonly Format R32G32B32_UInt = Silk.NET.DXGI.Format.FormatR32G32B32Uint;
    public static readonly Format R32G32B32_SInt = Silk.NET.DXGI.Format.FormatR32G32B32Sint;
    public static readonly Format R16G16B16A16_Typeless = Silk.NET.DXGI.Format.FormatR16G16B16A16Typeless;
    public static readonly Format R16G16B16A16_Float = Silk.NET.DXGI.Format.FormatR16G16B16A16Float;
    public static readonly Format R16G16B16A16_UNorm = Silk.NET.DXGI.Format.FormatR16G16B16A16Unorm;
    public static readonly Format R16G16B16A16_UInt = Silk.NET.DXGI.Format.FormatR16G16B16A16Uint;
    public static readonly Format R16G16B16A16_SNorm = Silk.NET.DXGI.Format.FormatR16G16B16A16SNorm;
    public static readonly Format R16G16B16A16_SInt = Silk.NET.DXGI.Format.FormatR16G16B16A16Sint;
    public static readonly Format R32G32_Typeless = Silk.NET.DXGI.Format.FormatR32G32Typeless;
    public static readonly Format R32G32_Float = Silk.NET.DXGI.Format.FormatR32G32Float;
    public static readonly Format R32G32_UInt = Silk.NET.DXGI.Format.FormatR32G32Uint;
    public static readonly Format R32G32_SInt = Silk.NET.DXGI.Format.FormatR32G32Sint;
    public static readonly Format R32G8X24_Typeless = Silk.NET.DXGI.Format.FormatR32G8X24Typeless;
    public static readonly Format D32_Float_S8X24_UInt = Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint;
    public static readonly Format R32_Float_X8X24_Typeless = Silk.NET.DXGI.Format.FormatR32FloatX8X24Typeless;
    public static readonly Format X32_Typeless_G8X24_UInt = Silk.NET.DXGI.Format.FormatX32TypelessG8X24Uint;
    public static readonly Format R10G10B10A2_Typeless = Silk.NET.DXGI.Format.FormatR10G10B10A2Typeless;
    public static readonly Format R10G10B10A2_UNorm = Silk.NET.DXGI.Format.FormatR10G10B10A2Unorm;
    public static readonly Format R10G10B10A2_UInt = Silk.NET.DXGI.Format.FormatR10G10B10A2Uint;
    public static readonly Format R11G11B10_Float = Silk.NET.DXGI.Format.FormatR11G11B10Float;
    public static readonly Format R8G8B8A8_Typeless = Silk.NET.DXGI.Format.FormatR8G8B8A8Typeless;
    public static readonly Format R8G8B8A8_UNorm = Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm;
    public static readonly Format R8G8B8A8_UNorm_SRgb = Silk.NET.DXGI.Format.FormatR8G8B8A8UnormSrgb;
    public static readonly Format R8G8B8A8_UInt = Silk.NET.DXGI.Format.FormatR8G8B8A8Uint;
    public static readonly Format R8G8B8A8_SNorm = Silk.NET.DXGI.Format.FormatR8G8B8A8SNorm;
    public static readonly Format R8G8B8A8_SInt = Silk.NET.DXGI.Format.FormatR8G8B8A8Sint;
    public static readonly Format R16G16_Typeless = Silk.NET.DXGI.Format.FormatR16G16Typeless;
    public static readonly Format R16G16_Float = Silk.NET.DXGI.Format.FormatR16G16Float;
    public static readonly Format R16G16_UNorm = Silk.NET.DXGI.Format.FormatR16G16Unorm;
    public static readonly Format R16G16_UInt = Silk.NET.DXGI.Format.FormatR16G16Uint;
    public static readonly Format R16G16_SNorm = Silk.NET.DXGI.Format.FormatR16G16SNorm;
    public static readonly Format R16G16_SInt = Silk.NET.DXGI.Format.FormatR16G16Sint;
    public static readonly Format R32_Typeless = Silk.NET.DXGI.Format.FormatR32Typeless;
    public static readonly Format D32_Float = Silk.NET.DXGI.Format.FormatD32Float;
    public static readonly Format R32_Float = Silk.NET.DXGI.Format.FormatR32Float;
    public static readonly Format R32_UInt = Silk.NET.DXGI.Format.FormatR32Uint;
    public static readonly Format R32_SInt = Silk.NET.DXGI.Format.FormatR32Sint;
    public static readonly Format R24G8_Typeless = Silk.NET.DXGI.Format.FormatR24G8Typeless;
    public static readonly Format D24_UNorm_S8_UInt = Silk.NET.DXGI.Format.FormatD24UnormS8Uint;
    public static readonly Format R24_UNorm_X8_Typeless = Silk.NET.DXGI.Format.FormatR24UnormX8Typeless;
    public static readonly Format X24_Typeless_G8_UInt = Silk.NET.DXGI.Format.FormatX24TypelessG8Uint;
    public static readonly Format R8G8_Typeless = Silk.NET.DXGI.Format.FormatR8G8Typeless;
    public static readonly Format R8G8_UNorm = Silk.NET.DXGI.Format.FormatR8G8Unorm;
    public static readonly Format R8G8_UInt = Silk.NET.DXGI.Format.FormatR8G8Uint;
    public static readonly Format R8G8_SNorm = Silk.NET.DXGI.Format.FormatR8G8SNorm;
    public static readonly Format R8G8_SInt = Silk.NET.DXGI.Format.FormatR8G8Sint;
    public static readonly Format R16_Typeless = Silk.NET.DXGI.Format.FormatR16Typeless;
    public static readonly Format R16_Float = Silk.NET.DXGI.Format.FormatR16Float;
    public static readonly Format D16_UNorm = Silk.NET.DXGI.Format.FormatD16Unorm;
    public static readonly Format R16_UNorm = Silk.NET.DXGI.Format.FormatR16Unorm;
    public static readonly Format R16_UInt = Silk.NET.DXGI.Format.FormatR16Uint;
    public static readonly Format R16_SNorm = Silk.NET.DXGI.Format.FormatR16SNorm;
    public static readonly Format R16_SInt = Silk.NET.DXGI.Format.FormatR16Sint;
    public static readonly Format R8_Typeless = Silk.NET.DXGI.Format.FormatR8Typeless;
    public static readonly Format R8_UNorm = Silk.NET.DXGI.Format.FormatR8Unorm;
    public static readonly Format R8_UInt = Silk.NET.DXGI.Format.FormatR8Uint;
    public static readonly Format R8_SNorm = Silk.NET.DXGI.Format.FormatR8SNorm;
    public static readonly Format R8_SInt = Silk.NET.DXGI.Format.FormatR8Sint;
    public static readonly Format A8_UNorm = Silk.NET.DXGI.Format.FormatA8Unorm;
    public static readonly Format R8G8_B8G8_UNorm = Silk.NET.DXGI.Format.FormatR8G8B8G8Unorm;
    public static readonly Format G8R8_G8B8_UNorm = Silk.NET.DXGI.Format.FormatG8R8G8B8Unorm;
    public static readonly Format BC1_Typeless = Silk.NET.DXGI.Format.FormatBC1Typeless;
    public static readonly Format BC1_UNorm = Silk.NET.DXGI.Format.FormatBC1Unorm;
    public static readonly Format BC1_UNorm_SRgb = Silk.NET.DXGI.Format.FormatBC1UnormSrgb;
    public static readonly Format BC2_Typeless = Silk.NET.DXGI.Format.FormatBC2Typeless;
    public static readonly Format BC2_UNorm = Silk.NET.DXGI.Format.FormatBC2Unorm;
    public static readonly Format BC2_UNorm_SRgb = Silk.NET.DXGI.Format.FormatBC2UnormSrgb;
    public static readonly Format BC3_Typeless = Silk.NET.DXGI.Format.FormatBC3Typeless;
    public static readonly Format BC3_UNorm = Silk.NET.DXGI.Format.FormatBC3Unorm;
    public static readonly Format BC3_UNorm_SRgb = Silk.NET.DXGI.Format.FormatBC3UnormSrgb;
    public static readonly Format BC4_Typeless = Silk.NET.DXGI.Format.FormatBC4Typeless;
    public static readonly Format BC4_UNorm = Silk.NET.DXGI.Format.FormatBC4Unorm;
    public static readonly Format BC4_SNorm = Silk.NET.DXGI.Format.FormatBC4SNorm;
    public static readonly Format BC5_Typeless = Silk.NET.DXGI.Format.FormatBC5Typeless;
    public static readonly Format BC5_UNorm = Silk.NET.DXGI.Format.FormatBC5Unorm;
    public static readonly Format BC5_SNorm = Silk.NET.DXGI.Format.FormatBC5SNorm;
    public static readonly Format B5G6R5_UNorm = Silk.NET.DXGI.Format.FormatB5G6R5Unorm;
    public static readonly Format B5G5R5A1_UNorm = Silk.NET.DXGI.Format.FormatB5G5R5A1Unorm;
    public static readonly Format B8G8R8A8_UNorm = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm;
    public static readonly Format B8G8R8X8_UNorm = Silk.NET.DXGI.Format.FormatB8G8R8X8Unorm;
    public static readonly Format B8G8R8A8_Typeless = Silk.NET.DXGI.Format.FormatB8G8R8A8Typeless;
    public static readonly Format B8G8R8A8_UNorm_SRgb = Silk.NET.DXGI.Format.FormatB8G8R8A8UnormSrgb;
    public static readonly Format B8G8R8X8_Typeless = Silk.NET.DXGI.Format.FormatB8G8R8X8Typeless;
    public static readonly Format B8G8R8X8_UNorm_SRgb = Silk.NET.DXGI.Format.FormatB8G8R8X8UnormSrgb;
    public static readonly Format BC6H_Typeless = Silk.NET.DXGI.Format.FormatBC6HTypeless;
    public static readonly Format BC6H_Uf16 = Silk.NET.DXGI.Format.FormatBC6HUF16;
    public static readonly Format BC6H_Sf16 = Silk.NET.DXGI.Format.FormatBC6HSF16;
    public static readonly Format BC7_Typeless = Silk.NET.DXGI.Format.FormatBC7Typeless;
    public static readonly Format BC7_UNorm = Silk.NET.DXGI.Format.FormatBC7Unorm;
    public static readonly Format BC7_UNorm_SRgb = Silk.NET.DXGI.Format.FormatBC7UnormSrgb;
    public static readonly Format B4G4R4A4_UNorm = Silk.NET.DXGI.Format.FormatB4G4R4A4Unorm;
}

internal static class FormatHelper {
    public static bool IsValid(Format format) {
        return format != Format.Unknown;
    }

    public static bool IsVideo(Format format) {
        var value = format.NativeFormat;
        return value >= Silk.NET.DXGI.Format.FormatAyuv
               && value <= Silk.NET.DXGI.Format.FormatV408;
    }

    public static bool IsCompressed(Format format) {
        switch (format.NativeFormat) {
            case Silk.NET.DXGI.Format.FormatBC1Typeless:
            case Silk.NET.DXGI.Format.FormatBC1Unorm:
            case Silk.NET.DXGI.Format.FormatBC1UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC2Typeless:
            case Silk.NET.DXGI.Format.FormatBC2Unorm:
            case Silk.NET.DXGI.Format.FormatBC2UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC3Typeless:
            case Silk.NET.DXGI.Format.FormatBC3Unorm:
            case Silk.NET.DXGI.Format.FormatBC3UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC4Typeless:
            case Silk.NET.DXGI.Format.FormatBC4Unorm:
            case Silk.NET.DXGI.Format.FormatBC4SNorm:
            case Silk.NET.DXGI.Format.FormatBC5Typeless:
            case Silk.NET.DXGI.Format.FormatBC5Unorm:
            case Silk.NET.DXGI.Format.FormatBC5SNorm:
            case Silk.NET.DXGI.Format.FormatBC6HTypeless:
            case Silk.NET.DXGI.Format.FormatBC6HUF16:
            case Silk.NET.DXGI.Format.FormatBC6HSF16:
            case Silk.NET.DXGI.Format.FormatBC7Typeless:
            case Silk.NET.DXGI.Format.FormatBC7Unorm:
            case Silk.NET.DXGI.Format.FormatBC7UnormSrgb:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPacked(Format format) {
        switch (format.NativeFormat) {
            case Silk.NET.DXGI.Format.FormatR8G8B8G8Unorm:
            case Silk.NET.DXGI.Format.FormatG8R8G8B8Unorm:
            case Silk.NET.DXGI.Format.FormatYuy2:
                return true;
            default:
                return false;
        }
    }

    public static int SizeOfInBytes(PixelFormat format) {
        return SizeOfInBytes(format.Value);
    }

    public static int SizeOfInBytes(Format format) {
        var bits = SizeOfInBits(format);
        return (bits + 7) / 8;
    }

    public static int SizeOfInBits(Format format) {
        switch (format.NativeFormat) {
            case Silk.NET.DXGI.Format.FormatR32G32B32A32Typeless:
            case Silk.NET.DXGI.Format.FormatR32G32B32A32Float:
            case Silk.NET.DXGI.Format.FormatR32G32B32A32Uint:
            case Silk.NET.DXGI.Format.FormatR32G32B32A32Sint:
                return 128;

            case Silk.NET.DXGI.Format.FormatR32G32B32Typeless:
            case Silk.NET.DXGI.Format.FormatR32G32B32Float:
            case Silk.NET.DXGI.Format.FormatR32G32B32Uint:
            case Silk.NET.DXGI.Format.FormatR32G32B32Sint:
                return 96;

            case Silk.NET.DXGI.Format.FormatR16G16B16A16Typeless:
            case Silk.NET.DXGI.Format.FormatR16G16B16A16Float:
            case Silk.NET.DXGI.Format.FormatR16G16B16A16Unorm:
            case Silk.NET.DXGI.Format.FormatR16G16B16A16Uint:
            case Silk.NET.DXGI.Format.FormatR16G16B16A16SNorm:
            case Silk.NET.DXGI.Format.FormatR16G16B16A16Sint:
            case Silk.NET.DXGI.Format.FormatR32G32Typeless:
            case Silk.NET.DXGI.Format.FormatR32G32Float:
            case Silk.NET.DXGI.Format.FormatR32G32Uint:
            case Silk.NET.DXGI.Format.FormatR32G32Sint:
            case Silk.NET.DXGI.Format.FormatR32G8X24Typeless:
            case Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint:
            case Silk.NET.DXGI.Format.FormatR32FloatX8X24Typeless:
            case Silk.NET.DXGI.Format.FormatX32TypelessG8X24Uint:
                return 64;

            case Silk.NET.DXGI.Format.FormatR10G10B10A2Typeless:
            case Silk.NET.DXGI.Format.FormatR10G10B10A2Unorm:
            case Silk.NET.DXGI.Format.FormatR10G10B10A2Uint:
            case Silk.NET.DXGI.Format.FormatR11G11B10Float:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8Typeless:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8UnormSrgb:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8Uint:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8SNorm:
            case Silk.NET.DXGI.Format.FormatR8G8B8A8Sint:
            case Silk.NET.DXGI.Format.FormatR16G16Typeless:
            case Silk.NET.DXGI.Format.FormatR16G16Float:
            case Silk.NET.DXGI.Format.FormatR16G16Unorm:
            case Silk.NET.DXGI.Format.FormatR16G16Uint:
            case Silk.NET.DXGI.Format.FormatR16G16SNorm:
            case Silk.NET.DXGI.Format.FormatR16G16Sint:
            case Silk.NET.DXGI.Format.FormatR32Typeless:
            case Silk.NET.DXGI.Format.FormatD32Float:
            case Silk.NET.DXGI.Format.FormatR32Float:
            case Silk.NET.DXGI.Format.FormatR32Uint:
            case Silk.NET.DXGI.Format.FormatR32Sint:
            case Silk.NET.DXGI.Format.FormatR24G8Typeless:
            case Silk.NET.DXGI.Format.FormatD24UnormS8Uint:
            case Silk.NET.DXGI.Format.FormatR24UnormX8Typeless:
            case Silk.NET.DXGI.Format.FormatX24TypelessG8Uint:
            case Silk.NET.DXGI.Format.FormatR8G8B8G8Unorm:
            case Silk.NET.DXGI.Format.FormatG8R8G8B8Unorm:
            case Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm:
            case Silk.NET.DXGI.Format.FormatB8G8R8X8Unorm:
            case Silk.NET.DXGI.Format.FormatB8G8R8A8Typeless:
            case Silk.NET.DXGI.Format.FormatB8G8R8A8UnormSrgb:
            case Silk.NET.DXGI.Format.FormatB8G8R8X8Typeless:
            case Silk.NET.DXGI.Format.FormatB8G8R8X8UnormSrgb:
            case Silk.NET.DXGI.Format.FormatB4G4R4A4Unorm:
                return 32;

            case Silk.NET.DXGI.Format.FormatR8G8Typeless:
            case Silk.NET.DXGI.Format.FormatR8G8Unorm:
            case Silk.NET.DXGI.Format.FormatR8G8Uint:
            case Silk.NET.DXGI.Format.FormatR8G8SNorm:
            case Silk.NET.DXGI.Format.FormatR8G8Sint:
            case Silk.NET.DXGI.Format.FormatR16Typeless:
            case Silk.NET.DXGI.Format.FormatR16Float:
            case Silk.NET.DXGI.Format.FormatD16Unorm:
            case Silk.NET.DXGI.Format.FormatR16Unorm:
            case Silk.NET.DXGI.Format.FormatR16Uint:
            case Silk.NET.DXGI.Format.FormatR16SNorm:
            case Silk.NET.DXGI.Format.FormatR16Sint:
            case Silk.NET.DXGI.Format.FormatB5G6R5Unorm:
            case Silk.NET.DXGI.Format.FormatB5G5R5A1Unorm:
                return 16;

            case Silk.NET.DXGI.Format.FormatR8Typeless:
            case Silk.NET.DXGI.Format.FormatR8Unorm:
            case Silk.NET.DXGI.Format.FormatR8Uint:
            case Silk.NET.DXGI.Format.FormatR8SNorm:
            case Silk.NET.DXGI.Format.FormatR8Sint:
            case Silk.NET.DXGI.Format.FormatA8Unorm:
                return 8;

            case Silk.NET.DXGI.Format.FormatBC1Typeless:
            case Silk.NET.DXGI.Format.FormatBC1Unorm:
            case Silk.NET.DXGI.Format.FormatBC1UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC4Typeless:
            case Silk.NET.DXGI.Format.FormatBC4Unorm:
            case Silk.NET.DXGI.Format.FormatBC4SNorm:
                return 4;

            case Silk.NET.DXGI.Format.FormatBC2Typeless:
            case Silk.NET.DXGI.Format.FormatBC2Unorm:
            case Silk.NET.DXGI.Format.FormatBC2UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC3Typeless:
            case Silk.NET.DXGI.Format.FormatBC3Unorm:
            case Silk.NET.DXGI.Format.FormatBC3UnormSrgb:
            case Silk.NET.DXGI.Format.FormatBC5Typeless:
            case Silk.NET.DXGI.Format.FormatBC5Unorm:
            case Silk.NET.DXGI.Format.FormatBC5SNorm:
            case Silk.NET.DXGI.Format.FormatBC6HTypeless:
            case Silk.NET.DXGI.Format.FormatBC6HUF16:
            case Silk.NET.DXGI.Format.FormatBC6HSF16:
            case Silk.NET.DXGI.Format.FormatBC7Typeless:
            case Silk.NET.DXGI.Format.FormatBC7Unorm:
            case Silk.NET.DXGI.Format.FormatBC7UnormSrgb:
                return 8;

            default:
                return 0;
        }
    }
}

internal static unsafe class Utilities {
    public static nint AllocateMemory(int sizeInBytes) {
        return Marshal.AllocHGlobal(sizeInBytes);
    }

    public static void FreeMemory(nint pointer) {
        if (pointer != nint.Zero) Marshal.FreeHGlobal(pointer);
    }

    public static byte[] ReadStream(Stream stream) {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static int SizeOf<T>() where T : struct {
        return Marshal.SizeOf<T>();
    }

    public static int SizeOf<T>(T[] values) where T : unmanaged {
        return values == null ? 0 : sizeof(T) * values.Length;
    }

    public static void CopyMemory(nint destination, nint source, int sizeInBytes) {
        Buffer.MemoryCopy(source.ToPointer(), destination.ToPointer(), sizeInBytes, sizeInBytes);
    }

    public static T Read<T>(nint source) where T : unmanaged {
        return *(T*)source.ToPointer();
    }

    public static void Read<T>(nint source, T[] destination, int startIndex, int count) where T : unmanaged {
        destination.AssertArgumentNotNull();

        fixed (T* destinationPointer = &destination[startIndex]) {
            Buffer.MemoryCopy(source.ToPointer(),
                              destinationPointer,
                              (destination.Length - startIndex) * sizeof(T),
                              count * sizeof(T));
        }
    }

    public static void Write<T>(nint destination, ref T value) where T : unmanaged {
        *(T*)destination.ToPointer() = value;
    }

    public static void Write<T>(nint destination, T[] source, int startIndex, int count) where T : unmanaged {
        source.AssertArgumentNotNull();

        fixed (T* sourcePointer = &source[startIndex]) {
            Buffer.MemoryCopy(sourcePointer, destination.ToPointer(), count * sizeof(T), count * sizeof(T));
        }
    }

    public static void Pin<T>(T[] source, Action<nint> action) where T : unmanaged {
        source.AssertArgumentNotNull();

        action.AssertArgumentNotNull();

        fixed (T* sourcePointer = source) {
            action((nint)sourcePointer);
        }
    }
}
