/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.IO;

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
            }

            public ShaderResourceViewProxy(object device, Resource resource)
            {
                this.resource = resource;
            }

            public ShaderResourceViewProxy(object device, Texture1DDescription textureDesc)
            {
                TextureFormat = textureDesc.Format;
            }

            public ShaderResourceViewProxy(object device, Texture2DDescription textureDesc)
            {
                TextureFormat = textureDesc.Format;
            }

            public ShaderResourceViewProxy(object device, Texture3DDescription textureDesc)
            {
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
            }

            public void CreateView(Stream texture, bool createSRV = true, bool enableAutoGenMipMap = true)
            {
            }

            public void CreateView(ShaderResourceViewDescription desc)
            {
                CreateTextureView(ref desc);
            }

            public void CreateView(DepthStencilViewDescription desc)
            {
            }

            public void CreateView(RenderTargetViewDescription desc)
            {
            }

            public void CreateTextureView()
            {
                if (context == null || resource == null)
                {
                    return;
                }

                RemoveAndDispose(ref textureView);
                textureView = context.NativeDevice.CreateShaderResourceView(resource);
                TextureFormat = textureView?.Description.Format ?? default;
            }

            public void CreateTextureView(ShaderResourceViewDescription desc)
            {
                CreateTextureView(ref desc);
            }

            public void CreateTextureView(ref ShaderResourceViewDescription desc)
            {
                if (context == null || resource == null)
                {
                    TextureFormat = desc.Format;
                    return;
                }

                RemoveAndDispose(ref textureView);
                textureView = context.NativeDevice.CreateShaderResourceView(resource, desc);
                TextureFormat = desc.Format;
            }

            public void CreateRenderTargetView()
            {
            }

            public void CreateDepthStencilView()
            {
            }

            public void CreateDepthStencilView(DepthStencilViewDescription desc)
            {
            }

            public void CreateDepthStencilView(ref DepthStencilViewDescription desc)
            {
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
                TextureFormat = format;
            }

            public unsafe void CreateView(IntPtr dataPtr, int width, int height, int depth, Format format, bool createSRV = true, bool generateMipMaps = true)
            {
                TextureFormat = format;
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
