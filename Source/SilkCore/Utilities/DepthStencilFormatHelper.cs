namespace HelixToolkit.SharpDX.Core.Utilities;
public static class DepthStencilFormatHelper {
    public static Format ComputeDsvFormat(this Format format) => format switch {
        Format.FormatD32Float or Format.FormatR32Typeless               => Format.FormatD32Float,
        Format.FormatD24UnormS8Uint or Format.FormatR24G8Typeless       => Format.FormatD24UnormS8Uint,
        Format.FormatD32FloatS8X24Uint or Format.FormatR32G8X24Typeless => Format.FormatD32FloatS8X24Uint,
        _                                                        => throw new InvalidOperationException($"Unsupported DXGI.FORMAT [{format}] for depth buffer")
    };

    public static Format ComputeTextureFormat(this Format format, out bool canUseAsShaderResource) {
        Format viewFormat;
        canUseAsShaderResource = false;

        // Determine TypeLess Format and ShaderResourceView Format
        switch (format) {
            case Format.FormatD32Float:
            case Format.FormatR32Typeless:
                viewFormat = Format.FormatR32Typeless;
                canUseAsShaderResource = true;
                break;
            case Format.FormatD24UnormS8Uint:
            case Format.FormatR24G8Typeless:
                viewFormat = Format.FormatR24G8Typeless;
                canUseAsShaderResource = true;
                break;
            case Format.FormatD32FloatS8X24Uint:
            case Format.FormatR32G8X24Typeless:
                viewFormat = Format.FormatR32G8X24Typeless;
                canUseAsShaderResource = true;
                break;
            default:
                viewFormat = format;
                break;
        }

        return viewFormat;
    }

    public static Format ComputeSrvFormat(this Format format) => format switch {
        Format.FormatD32Float or Format.FormatR32Typeless               => Format.FormatR32Float,
        Format.FormatD24UnormS8Uint or Format.FormatR24G8Typeless       => Format.FormatR24UnormX8Typeless,
        Format.FormatD32FloatS8X24Uint or Format.FormatR32G8X24Typeless => Format.FormatR32FloatX8X24Typeless,
        _ => throw new InvalidOperationException($"Unsupported DXGI.FORMAT [{format}] for creating shaderResourceView")
    };

    public static bool CanUseAsShaderResource(this Format format) {
        format.ComputeTextureFormat(out var canUse);
        return canUse;
    }
}
