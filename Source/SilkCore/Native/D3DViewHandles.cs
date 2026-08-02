/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SilkD3D11DepthStencilViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilView>;
using SilkD3D11RenderTargetViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RenderTargetView>;
using SilkD3D11ShaderResourceViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ShaderResourceView>;
using SilkD3D11UnorderedAccessViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11UnorderedAccessView>;

namespace HelixToolkit.SharpDX.Core {
    [Flags]
    public enum DepthStencilClearFlags {
        Depth = 1,
        Stencil = 2
    }

    public enum ShaderResourceViewDimension {
        Unknown = 0,
        Buffer = 1,
        Texture1D = 2,
        Texture1DArray = 3,
        Texture2D = 4,
        Texture2DArray = 5,
        Texture2DMultisampled = 6,
        Texture2DMultisampledArray = 7,
        Texture3D = 8,
        TextureCube = 9,
        TextureCubeArray = 10,
        BufferExtended = 11
    }

    public enum UnorderedAccessViewDimension {
        Unknown = 0,
        Buffer = 1,
        Texture1D = 2,
        Texture1DArray = 3,
        Texture2D = 4,
        Texture2DArray = 5,
        Texture3D = 8
    }

    public enum RenderTargetViewDimension {
        Unknown = 0,
        Buffer = 1,
        Texture1D = 2,
        Texture1DArray = 3,
        Texture2D = 4,
        Texture2DArray = 5,
        Texture2DMultisampled = 6,
        Texture2DMultisampledArray = 7,
        Texture3D = 8
    }

    public enum DepthStencilViewDimension {
        Unknown = 0,
        Texture1D = 1,
        Texture1DArray = 2,
        Texture2D = 3,
        Texture2DArray = 4,
        Texture2DMultisampled = 5,
        Texture2DMultisampledArray = 6
    }

    [Flags]
    public enum DepthStencilViewFlags {
        None = 0,
        ReadOnlyDepth = 1,
        ReadOnlyStencil = 2
    }

    [Flags]
    public enum UnorderedAccessViewBufferFlags {
        None = 0,
        Raw = 1,
        Append = 2,
        Counter = 4
    }

    public struct ShaderResourceViewDescription {
        public Format Format;
        public ShaderResourceViewDimension Dimension;
        public BufferResource Buffer;
        public Texture1DResource Texture1D;
        public Texture2DResource Texture2D;
        public Texture3DResource Texture3D;
        public TextureCubeResource TextureCube;

        public struct BufferResource {
            public int FirstElement;
            public int ElementCount;
        }

        public struct Texture1DResource {
            public int MostDetailedMip;
            public int MipLevels;
        }

        public struct Texture2DResource {
            public int MostDetailedMip;
            public int MipLevels;
        }

        public struct Texture3DResource {
            public int MostDetailedMip;
            public int MipLevels;
        }

        public struct TextureCubeResource {
            public int MostDetailedMip;
            public int MipLevels;
        }
    }

    public struct UnorderedAccessViewDescription {
        public Format Format;
        public UnorderedAccessViewDimension Dimension;
        public BufferResource Buffer;

        public struct BufferResource {
            public int FirstElement;
            public int ElementCount;
            public UnorderedAccessViewBufferFlags Flags;
        }
    }

    public sealed unsafe class RenderTargetView : IDisposable {
        private SilkD3D11RenderTargetViewPtr nativeView;

        internal RenderTargetView(SilkD3D11RenderTargetViewPtr nativeView, Resource? resource = null) {
            this.nativeView = nativeView;
            Resource = resource;
        }

        public nint NativePointer => (nint)nativeView.Handle;

        public Resource Resource { get; }

        internal ID3D11RenderTargetView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public sealed unsafe class DepthStencilView : IDisposable {
        private SilkD3D11DepthStencilViewPtr nativeView;

        internal DepthStencilView(SilkD3D11DepthStencilViewPtr nativeView) {
            this.nativeView = nativeView;
        }

        public nint NativePointer => (nint)nativeView.Handle;

        internal ID3D11DepthStencilView* Handle => nativeView.Handle;

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public sealed unsafe class ShaderResourceView : IDisposable {
        private SilkD3D11ShaderResourceViewPtr nativeView;

        internal ShaderResourceView(SilkD3D11ShaderResourceViewPtr nativeView) {
            this.nativeView = nativeView;
        }

        internal ShaderResourceView(
            SilkD3D11ShaderResourceViewPtr nativeView,
            ShaderResourceViewDescription description
        )
            : this(nativeView) {
            Description = description;
        }

        public nint NativePointer => (nint)nativeView.Handle;

        internal ID3D11ShaderResourceView* Handle => nativeView.Handle;

        public ShaderResourceViewDescription Description { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    public sealed unsafe class UnorderedAccessView : IDisposable {
        private SilkD3D11UnorderedAccessViewPtr nativeView;

        internal UnorderedAccessView(SilkD3D11UnorderedAccessViewPtr nativeView) {
            this.nativeView = nativeView;
        }

        internal UnorderedAccessView(
            SilkD3D11UnorderedAccessViewPtr nativeView,
            UnorderedAccessViewDescription description
        )
            : this(nativeView) {
            Description = description;
        }

        public nint NativePointer => (nint)nativeView.Handle;

        internal ID3D11UnorderedAccessView* Handle => nativeView.Handle;

        public UnorderedAccessViewDescription Description { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;

            nativeView.Dispose();
            IsDisposed = true;
        }
    }

    namespace Native {
        internal static class D3DViewConversions {
            public static RenderTargetViewDesc ToSilkDesc(this RenderTargetViewDescription description) {
                var desc = new RenderTargetViewDesc {
                    Format = description.Format,
                    ViewDimension = description.Dimension.ToSilkRtvDimension()
                };

                switch (description.Dimension) {
                    case RenderTargetViewDimension.Texture2D:
                    case RenderTargetViewDimension.Texture2DMultisampled:
                        desc.Anonymous.Texture2D.MipSlice = unchecked((uint)description.Texture2D.MipSlice);
                        break;
                    case RenderTargetViewDimension.Texture2DArray:
                    case RenderTargetViewDimension.Texture2DMultisampledArray:
                        desc.Anonymous.Texture2DArray.MipSlice = unchecked((uint)description.Texture2DArray.MipSlice);
                        desc.Anonymous.Texture2DArray.FirstArraySlice =
                            unchecked((uint)description.Texture2DArray.FirstArraySlice);
                        desc.Anonymous.Texture2DArray.ArraySize =
                            unchecked((uint)description.Texture2DArray.ArraySize);
                        break;
                }

                return desc;
            }

            public static DepthStencilViewDesc ToSilkDesc(this DepthStencilViewDescription description) {
                var desc = new DepthStencilViewDesc {
                    Format = description.Format,
                    ViewDimension = description.Dimension.ToSilkDsvDimension(),
                    Flags = (uint)description.Flags
                };

                switch (description.Dimension) {
                    case DepthStencilViewDimension.Texture2D:
                    case DepthStencilViewDimension.Texture2DMultisampled:
                        desc.Anonymous.Texture2D.MipSlice = unchecked((uint)description.Texture2D.MipSlice);
                        break;
                    case DepthStencilViewDimension.Texture2DArray:
                    case DepthStencilViewDimension.Texture2DMultisampledArray:
                        desc.Anonymous.Texture2DArray.MipSlice = unchecked((uint)description.Texture2DArray.MipSlice);
                        desc.Anonymous.Texture2DArray.FirstArraySlice =
                            unchecked((uint)description.Texture2DArray.FirstArraySlice);
                        desc.Anonymous.Texture2DArray.ArraySize =
                            unchecked((uint)description.Texture2DArray.ArraySize);
                        break;
                }

                return desc;
            }

            public static ShaderResourceViewDesc ToSilkDesc(this ShaderResourceViewDescription description) {
                var desc = new ShaderResourceViewDesc {
                    Format = description.Format,
                    ViewDimension = description.Dimension.ToSilkSrvDimension()
                };

                if (description.Dimension == ShaderResourceViewDimension.Buffer) {
                    desc.Anonymous.Buffer.Anonymous1.FirstElement = unchecked((uint)description.Buffer.FirstElement);
                    desc.Anonymous.Buffer.Anonymous2.NumElements = unchecked((uint)description.Buffer.ElementCount);
                } else if (description.Dimension == ShaderResourceViewDimension.Texture2D) {
                    desc.Anonymous.Texture2D.MostDetailedMip = unchecked((uint)description.Texture2D.MostDetailedMip);
                    desc.Anonymous.Texture2D.MipLevels = unchecked((uint)description.Texture2D.MipLevels);
                } else if (description.Dimension == ShaderResourceViewDimension.Texture3D) {
                    desc.Anonymous.Texture3D.MostDetailedMip = unchecked((uint)description.Texture3D.MostDetailedMip);
                    desc.Anonymous.Texture3D.MipLevels = unchecked((uint)description.Texture3D.MipLevels);
                } else if (description.Dimension == ShaderResourceViewDimension.TextureCube) {
                    desc.Anonymous.TextureCube.MostDetailedMip =
                        unchecked((uint)description.TextureCube.MostDetailedMip);
                    desc.Anonymous.TextureCube.MipLevels = unchecked((uint)description.TextureCube.MipLevels);
                }

                return desc;
            }

            public static UnorderedAccessViewDesc ToSilkDesc(this UnorderedAccessViewDescription description) {
                var desc = new UnorderedAccessViewDesc {
                    Format = description.Format,
                    ViewDimension = description.Dimension.ToSilkUavDimension()
                };

                if (description.Dimension == UnorderedAccessViewDimension.Buffer) {
                    desc.Anonymous.Buffer.FirstElement = unchecked((uint)description.Buffer.FirstElement);
                    desc.Anonymous.Buffer.NumElements = unchecked((uint)description.Buffer.ElementCount);
                    desc.Anonymous.Buffer.Flags = (uint)description.Buffer.Flags;
                }

                return desc;
            }

            private static D3DSrvDimension ToSilkSrvDimension(this ShaderResourceViewDimension dimension) {
                return dimension switch {
                    ShaderResourceViewDimension.Buffer => D3DSrvDimension.D3D11SrvDimensionBuffer,
                    ShaderResourceViewDimension.Texture1D => D3DSrvDimension.D3D11SrvDimensionTexture1D,
                    ShaderResourceViewDimension.Texture1DArray => D3DSrvDimension.D3D11SrvDimensionTexture1Darray,
                    ShaderResourceViewDimension.Texture2D => D3DSrvDimension.D3D11SrvDimensionTexture2D,
                    ShaderResourceViewDimension.Texture2DArray => D3DSrvDimension.D3D11SrvDimensionTexture2Darray,
                    ShaderResourceViewDimension.Texture2DMultisampled => D3DSrvDimension.D3D11SrvDimensionTexture2Dms,
                    ShaderResourceViewDimension.Texture2DMultisampledArray => D3DSrvDimension
                        .D3D11SrvDimensionTexture2Dmsarray,
                    ShaderResourceViewDimension.Texture3D => D3DSrvDimension.D3D11SrvDimensionTexture3D,
                    ShaderResourceViewDimension.TextureCube => D3DSrvDimension.D3D11SrvDimensionTexturecube,
                    ShaderResourceViewDimension.TextureCubeArray => D3DSrvDimension.D3D11SrvDimensionTexturecubearray,
                    ShaderResourceViewDimension.BufferExtended => D3DSrvDimension.D3D11SrvDimensionBufferex,
                    _ => D3DSrvDimension.D3D11SrvDimensionUnknown
                };
            }

            private static RtvDimension ToSilkRtvDimension(this RenderTargetViewDimension dimension) {
                return dimension switch {
                    RenderTargetViewDimension.Buffer => RtvDimension.Buffer,
                    RenderTargetViewDimension.Texture1D => RtvDimension.Texture1D,
                    RenderTargetViewDimension.Texture1DArray => RtvDimension.Texture1Darray,
                    RenderTargetViewDimension.Texture2D => RtvDimension.Texture2D,
                    RenderTargetViewDimension.Texture2DArray => RtvDimension.Texture2Darray,
                    RenderTargetViewDimension.Texture2DMultisampled => RtvDimension.Texture2Dms,
                    RenderTargetViewDimension.Texture2DMultisampledArray => RtvDimension.Texture2Dmsarray,
                    RenderTargetViewDimension.Texture3D => RtvDimension.Texture3D,
                    _ => RtvDimension.Unknown
                };
            }

            private static DsvDimension ToSilkDsvDimension(this DepthStencilViewDimension dimension) {
                return dimension switch {
                    DepthStencilViewDimension.Texture1D => DsvDimension.Texture1D,
                    DepthStencilViewDimension.Texture1DArray => DsvDimension.Texture1Darray,
                    DepthStencilViewDimension.Texture2D => DsvDimension.Texture2D,
                    DepthStencilViewDimension.Texture2DArray => DsvDimension.Texture2Darray,
                    DepthStencilViewDimension.Texture2DMultisampled => DsvDimension.Texture2Dms,
                    DepthStencilViewDimension.Texture2DMultisampledArray => DsvDimension.Texture2Dmsarray,
                    _ => DsvDimension.Unknown
                };
            }

            private static UavDimension ToSilkUavDimension(this UnorderedAccessViewDimension dimension) {
                return dimension switch {
                    UnorderedAccessViewDimension.Buffer => UavDimension.Buffer,
                    UnorderedAccessViewDimension.Texture1D => UavDimension.Texture1D,
                    UnorderedAccessViewDimension.Texture1DArray => UavDimension.Texture1Darray,
                    UnorderedAccessViewDimension.Texture2D => UavDimension.Texture2D,
                    UnorderedAccessViewDimension.Texture2DArray => UavDimension.Texture2Darray,
                    UnorderedAccessViewDimension.Texture3D => UavDimension.Texture3D,
                    _ => UavDimension.Unknown
                };
            }
        }
    }
}
