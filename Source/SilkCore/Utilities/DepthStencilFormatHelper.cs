namespace HelixToolkit.SharpDX.Core
{
    namespace Utilities
    {
        public static class DepthStencilFormatHelper
        {
            public static Format ComputeDSVFormat(this Format format)
            {
                switch (format)
                {
                    case Format.FormatD32Float:
                    case Format.FormatR32Typeless:
                        return Format.FormatD32Float;
                    case Format.FormatD24UnormS8Uint:
                    case Format.FormatR24G8Typeless:
                        return Format.FormatD24UnormS8Uint;
                    case Format.FormatD32FloatS8X24Uint:
                    case Format.FormatR32G8X24Typeless:
                        return Format.FormatD32FloatS8X24Uint;
                }

                throw new InvalidOperationException(string.Format("Unsupported DXGI.FORMAT [{0}] for depth buffer",
                    format));
            }

            public static Format ComputeTextureFormat(this Format format, out bool canUseAsShaderResource)
            {
                Format viewFormat;
                canUseAsShaderResource = false;

                // Determine TypeLess Format and ShaderResourceView Format
                switch (format)
                {
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

            public static Format ComputeSRVFormat(this Format format)
            {
                switch (format)
                {
                    case Format.FormatD32Float:
                    case Format.FormatR32Typeless:
                        return Format.FormatR32Float;
                    case Format.FormatD24UnormS8Uint:
                    case Format.FormatR24G8Typeless:
                        return Format.FormatR24UnormX8Typeless;
                    case Format.FormatD32FloatS8X24Uint:
                    case Format.FormatR32G8X24Typeless:
                        return Format.FormatR32FloatX8X24Typeless;
                }

                throw new InvalidOperationException(
                    string.Format("Unsupported DXGI.FORMAT [{0}] for creating shaderResourceView", format));
            }

            public static bool CanUseAsShaderResource(this Format format)
            {
                format.ComputeTextureFormat(out var canUse);
                return canUse;
            }
        }
    }
}