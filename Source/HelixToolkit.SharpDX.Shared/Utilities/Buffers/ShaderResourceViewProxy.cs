/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.IO;
using SharpDX.Toolkit.Graphics;

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
    using Model;
    namespace Utilities
    {
        using Render;

        /// <summary>
        /// A proxy container to handle view resources.
        /// </summary>
        public class ShaderResourceViewProxy : DisposeObject
        {
            public Guid Guid { set; get; } = Guid.NewGuid();
            public static ShaderResourceViewProxy Empty { get; } = new ShaderResourceViewProxy();

            private readonly DeviceContextProxy context;
            private readonly Native.SilkD3DDevice nativeDevice;
            private ShaderResourceView textureView;
            private DepthStencilView depthStencilView;
            private RenderTargetView renderTargetView;
            private Resource resource;

            public ShaderResourceView TextureView => textureView;

            public DepthStencilView DepthStencilView => depthStencilView;

            public RenderTargetView RenderTargetView => renderTargetView;

            public Resource Resource => resource;

            public Format TextureFormat { private set; get; }

            private ShaderResourceViewProxy()
            {
            }

            public ShaderResourceViewProxy(DeviceContextProxy context)
            {
                this.context = context;
                nativeDevice = context?.NativeDevice;
            }

            public ShaderResourceViewProxy(DeviceContextProxy context, Resource resource)
                : this(context)
            {
                this.resource = resource;
            }

            public ShaderResourceViewProxy(DeviceContextProxy context, Resource resource, ShaderResourceViewDescription description)
                : this(context, resource)
            {
                CreateTextureView(ref description);
            }

            internal ShaderResourceViewProxy(Resource resource, ShaderResourceView textureView)
            {
                this.resource = resource;
                this.textureView = textureView;
                TextureFormat = textureView == null ? default : textureView.Description.Format;
            }

            public ShaderResourceViewProxy(object device)
            {
                nativeDevice = ResolveNativeDevice(device);
            }

            public ShaderResourceViewProxy(object device, Resource resource)
                : this(device)
            {
                this.resource = resource;
            }

            public ShaderResourceViewProxy(object device, Texture1DDescription textureDesc)
                : this(device)
            {
                if (nativeDevice != null)
                {
                    resource = nativeDevice.CreateTexture1D(textureDesc);
                }
                TextureFormat = textureDesc.Format;
            }

            public ShaderResourceViewProxy(object device, Texture2DDescription textureDesc)
                : this(device)
            {
                if (nativeDevice != null)
                {
                    resource = nativeDevice.CreateTexture2D(textureDesc);
                }
                TextureFormat = textureDesc.Format;
            }

            public ShaderResourceViewProxy(object device, Texture3DDescription textureDesc)
                : this(device)
            {
                if (nativeDevice != null)
                {
                    resource = nativeDevice.CreateTexture3D(textureDesc);
                }
                TextureFormat = textureDesc.Format;
            }

            public ShaderResourceViewProxy(object device, ref Texture1DDescription textureDesc)
                : this(device, textureDesc)
            {
            }

            public ShaderResourceViewProxy(object device, ref Texture2DDescription textureDesc)
                : this(device, textureDesc)
            {
            }

            public ShaderResourceViewProxy(object device, ref Texture3DDescription textureDesc)
                : this(device, textureDesc)
            {
            }

            public ShaderResourceViewProxy(ShaderResourceView view)
            {
                textureView = view;
                TextureFormat = view == null ? default : view.Description.Format;
            }

            public void CreateView(TextureModel texture, bool createSRV = true, bool enableAutoGenMipMap = true)
            {
                if (texture == null)
                {
                    return;
                }

                var info = texture.Load();
                var succeeded = false;
                try
                {
                    if (info.DataType == TextureDataType.Stream && info.IsCompressed)
                    {
                        CreateView(info.Texture, createSRV, enableAutoGenMipMap);
                        succeeded = resource != null;
                    }
                }
                finally
                {
                    texture.Complete(info, succeeded);
                }
            }

            public void CreateView(Stream texture, bool createSRV = true, bool enableAutoGenMipMap = true)
            {
                if (nativeDevice == null || texture == null)
                {
                    return;
                }

                var originalPosition = texture.CanSeek ? texture.Position : 0;
                try
                {
                    if (texture.CanSeek)
                    {
                        texture.Position = 0;
                    }

                    using (var image = Image.Load(texture))
                    {
                        if (image == null || image.Description.Dimension != TextureDimension.Texture2D)
                        {
                            return;
                        }

                        RemoveAndDispose(ref textureView);
                        RemoveAndDispose(ref resource);

                        var description = new Texture2DDescription
                        {
                            Width = image.Description.Width,
                            Height = image.Description.Height,
                            MipLevels = image.Description.MipLevels,
                            ArraySize = image.Description.ArraySize,
                            Format = image.Description.Format,
                            SampleDescription = new SampleDescription(1, 0),
                            BindFlags = createSRV ? BindFlags.ShaderResource : BindFlags.None,
                            CpuAccessFlags = CpuAccessFlags.None,
                            OptionFlags = ResourceOptionFlags.None,
                            Usage = ResourceUsage.Immutable
                        };
                        resource = nativeDevice.CreateTexture2D(description, image.ToDataBox());
                        TextureFormat = description.Format;
                        if (createSRV)
                        {
                            CreateTextureView();
                        }
                    }
                }
                finally
                {
                    if (texture.CanSeek)
                    {
                        texture.Position = originalPosition;
                    }
                }
            }

            public void CreateView(ShaderResourceViewDescription desc)
            {
                CreateTextureView(ref desc);
            }

            public void CreateView(DepthStencilViewDescription desc)
            {
                CreateDepthStencilView(ref desc);
            }

            public void CreateView(RenderTargetViewDescription desc)
            {
                CreateRenderTargetView(ref desc);
            }

            public void CreateTextureView()
            {
                if (nativeDevice == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref textureView);
                textureView = nativeDevice.CreateShaderResourceView(resource);
                TextureFormat = textureView?.Description.Format ?? default;
            }

            public void CreateTextureView(ShaderResourceViewDescription desc)
            {
                CreateTextureView(ref desc);
            }

            public void CreateTextureView(ref ShaderResourceViewDescription desc)
            {
                if (nativeDevice == null || resource == null)
                {
                    TextureFormat = desc.Format;
                    return;
                }

                RemoveAndDispose(ref textureView);
                textureView = nativeDevice.CreateShaderResourceView(resource, desc);
                TextureFormat = desc.Format;
            }

            public void CreateRenderTargetView()
            {
                if (nativeDevice == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref renderTargetView);
                renderTargetView = nativeDevice.CreateRenderTargetView(resource);
            }

            public void CreateRenderTargetView(RenderTargetViewDescription desc)
            {
                CreateRenderTargetView(ref desc);
            }

            public void CreateRenderTargetView(ref RenderTargetViewDescription desc)
            {
                if (nativeDevice == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref renderTargetView);
                renderTargetView = nativeDevice.CreateRenderTargetView(resource, desc);
            }

            public void CreateDepthStencilView()
            {
                if (nativeDevice == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref depthStencilView);
                depthStencilView = nativeDevice.CreateDepthStencilView(resource);
            }

            public void CreateDepthStencilView(DepthStencilViewDescription desc)
            {
                CreateDepthStencilView(ref desc);
            }

            public void CreateDepthStencilView(ref DepthStencilViewDescription desc)
            {
                if (nativeDevice == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref depthStencilView);
                depthStencilView = nativeDevice.CreateDepthStencilView(resource, desc);
            }

            public void CreateView<T>(T[] array, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                TextureFormat = format;
            }

            public void CreateView<T>(T[] array, int length, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                TextureFormat = format;
            }

            public unsafe void CreateView(IntPtr dataPtr, int width, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                TextureFormat = format;
            }

            public void CreateView<T>(T[] array, int width, int height, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                TextureFormat = format;
            }

            public unsafe void CreateView(IntPtr dataPtr, int width, int height, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                TextureFormat = format;
            }

            public void CreateView<T>(T[] pixels, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                unsafe
                {
                    fixed (T* pixelsPtr = pixels)
                    {
                        CreateView((IntPtr)pixelsPtr, width, height, depth, format, sizeof(T), createSRV, generateMipMaps);
                    }
                }
            }

            public unsafe void CreateView(IntPtr dataPtr, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                CreateView(dataPtr, width, height, depth, format, GetFormatSizeInBytes(format), createSRV, generateMipMaps);
            }

            private unsafe void CreateView(IntPtr dataPtr, int width, int height, int depth, Format format, int bytesPerPixel, bool createSRV, bool generateMipMaps)
            {
                TextureFormat = format;
                if (nativeDevice == null || dataPtr == IntPtr.Zero || width <= 0 || height <= 0 || depth <= 0 || bytesPerPixel <= 0)
                {
                    return;
                }

                RemoveAndDispose(ref textureView);
                RemoveAndDispose(ref resource);

                var desc = new Texture3DDescription
                {
                    Width = width,
                    Height = height,
                    Depth = depth,
                    MipLevels = 1,
                    Format = format,
                    BindFlags = createSRV ? BindFlags.ShaderResource : BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Immutable
                };
                var data = new[]
                {
                    new DataBox(dataPtr, width * bytesPerPixel, width * height * bytesPerPixel)
                };
                resource = nativeDevice.CreateTexture3D(desc, data);

                if (createSRV)
                {
                    var srvDesc = new ShaderResourceViewDescription
                    {
                        Format = format,
                        Dimension = ShaderResourceViewDimension.Texture3D,
                        Texture3D = new ShaderResourceViewDescription.Texture3DResource
                        {
                            MostDetailedMip = 0,
                            MipLevels = 1
                        }
                    };
                    CreateTextureView(ref srvDesc);
                }
            }

            private static int GetFormatSizeInBytes(Format format)
            {
                return format switch
                {
                    Format.FormatR8Unorm => 1,
                    Format.FormatR16Unorm => 2,
                    Format.FormatR32Float => 4,
                    Format.FormatR16G16B16A16Float => 8,
                    Format.FormatR32G32B32A32Float => 16,
                    _ => 0
                };
            }

            public void CreateViewFromColorArray(Color4[] array)
            {
                CreateView(array, Format.FormatR32G32B32A32Float);
            }

            public void CreateViewFromColorArray(Color4[] array, int width, int height, bool createSRV = true, bool generateMipMaps = true)
            {
                CreateView(array, width, height, Format.FormatR32G32B32A32Float, createSRV, generateMipMaps);
            }

            public static ShaderResourceViewProxy CreateView<T>(object device, T[] array, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(array, format, createSRV, generateMipMaps);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateView(object device, Stream texture, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(texture, createSRV, generateMipMaps);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateView<T>(object device, T[] array, int width, int height, Format format, bool createSRV = true, bool generateMipMaps = true)
                where T : unmanaged
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(array, width, height, format, createSRV, generateMipMaps);
                return proxy;
            }

            public unsafe static ShaderResourceViewProxy CreateView(object device, IntPtr dataPtr, int width, int height, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(dataPtr, width, height, format, createSRV, generateMipMaps);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateViewFromColorArray(object device, Color4[] array)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateViewFromColorArray(array);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateViewFromColorArray(object device, Color4[] array, int width, int height, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateViewFromColorArray(array, width, height, createSRV, generateMipMaps);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateViewFromPixelData(object device, byte[] pixels, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(pixels, width, height, depth, format, createSRV, generateMipMaps);
                return proxy;
            }

            public static ShaderResourceViewProxy CreateViewFromPixelData(object device, Half4[] pixels, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(pixels, width, height, depth, format, createSRV, generateMipMaps);
                return proxy;
            }

            public unsafe static ShaderResourceViewProxy CreateViewFromPixelData(object device, IntPtr pixels, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                var proxy = new ShaderResourceViewProxy(device);
                proxy.CreateView(pixels, width, height, depth, format, createSRV, generateMipMaps);
                return proxy;
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref textureView);
                RemoveAndDispose(ref depthStencilView);
                RemoveAndDispose(ref renderTargetView);
                RemoveAndDispose(ref resource);
                base.OnDispose(disposeManagedResources);
            }

            private static Native.SilkD3DDevice ResolveNativeDevice(object device)
            {
                return device switch
                {
                    DeviceContextProxy contextProxy => contextProxy.NativeDevice,
                    Native.SilkD3DDevice silkDevice => silkDevice,
                    Native.INativeDeviceResources nativeResources => nativeResources.Device,
                    IDevice3DResources deviceResources => deviceResources.NativeDeviceResources?.Device,
                    _ => null
                };
            }

            public static implicit operator ShaderResourceView(ShaderResourceViewProxy proxy)
            {
                return proxy == null ? null : proxy.textureView;
            }

            public static implicit operator DepthStencilView(ShaderResourceViewProxy proxy)
            {
                return proxy == null ? null : proxy.depthStencilView;
            }

            public static implicit operator RenderTargetView(ShaderResourceViewProxy proxy)
            {
                return proxy == null ? null : proxy.renderTargetView;
            }
        }
    }
}
