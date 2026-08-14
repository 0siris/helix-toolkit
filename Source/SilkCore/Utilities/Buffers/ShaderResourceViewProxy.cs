/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using SharpDX.Toolkit.Graphics;

namespace HelixToolkit.SharpDX.Core.Utilities;
/// <summary>
///     A proxy container to handle view resources.
/// </summary>
public class ShaderResourceViewProxy : DisposeObject {
    private readonly DeviceContextProxy? context;
    private readonly NativeD3DDevice? nativeDevice;
    private DepthStencilView? depthStencilView;
    private RenderTargetView? renderTargetView;
    private Resource? resource;
    private ShaderResourceView? textureView;

    private ShaderResourceViewProxy() { }

    public ShaderResourceViewProxy(DeviceContextProxy context) {
        this.context = context;
        nativeDevice = context?.NativeDevice;
    }

    public ShaderResourceViewProxy(DeviceContextProxy context, Resource resource)
        : this(context) {
        this.resource = resource;
    }

    public ShaderResourceViewProxy(
        DeviceContextProxy context,
        Resource resource,
        ShaderResourceViewDescription description
    )
        : this(context, resource) {
        CreateTextureView(ref description);
    }

    internal ShaderResourceViewProxy(Resource resource, ShaderResourceView textureView) {
        this.resource = resource;
        this.textureView = textureView;
        TextureFormat = textureView == null ? default : textureView.Description.Format;
    }

    public ShaderResourceViewProxy(object device) {
        nativeDevice = ResolveNativeDevice(device);
    }

    public ShaderResourceViewProxy(object device, Resource resource)
        : this(device) {
        this.resource = resource;
    }

    public ShaderResourceViewProxy(object device, Texture1DDescription textureDesc)
        : this(device) {
        if (nativeDevice != null) resource = nativeDevice.CreateTexture1D(textureDesc);
        TextureFormat = textureDesc.Format;
    }

    public ShaderResourceViewProxy(object device, Texture2DDescription textureDesc)
        : this(device) {
        if (nativeDevice != null) resource = nativeDevice.CreateTexture2D(textureDesc);
        TextureFormat = textureDesc.Format;
    }

    public ShaderResourceViewProxy(object device, Texture3DDescription textureDesc)
        : this(device) {
        if (nativeDevice != null) resource = nativeDevice.CreateTexture3D(textureDesc);
        TextureFormat = textureDesc.Format;
    }

    public ShaderResourceViewProxy(object device, ref Texture1DDescription textureDesc)
        : this(device, textureDesc) { }

    public ShaderResourceViewProxy(object device, ref Texture2DDescription textureDesc)
        : this(device, textureDesc) { }

    public ShaderResourceViewProxy(object device, ref Texture3DDescription textureDesc)
        : this(device, textureDesc) { }

    public ShaderResourceViewProxy(ShaderResourceView view) {
        textureView = view;
        TextureFormat = view == null ? default : view.Description.Format;
    }

    public Guid Guid { get; set; } = Guid.NewGuid();
    public static ShaderResourceViewProxy Empty { get; } = new();

    public ShaderResourceView? TextureView => textureView;

    public DepthStencilView? DepthStencilView => depthStencilView;

    public RenderTargetView? RenderTargetView => renderTargetView;

    public Resource? Resource => resource;

    public Format TextureFormat { get; private set; }

    public void CreateView(TextureModel texture, bool createSrv = true, bool enableAutoGenMipMap = true) {
        if (texture == null) return;

        var info = texture.Load();
        var succeeded = false;
        try {
            if (info.DataType == TextureDataType.Stream && info.IsCompressed) {
                CreateView(info.Texture, createSrv, enableAutoGenMipMap);
                succeeded = resource != null;
            } else if (info.DataType == TextureDataType.ByteArray && info.Dimension == 2) {
                CreateView(info.TextureRaw,
                           info.Width,
                           info.Height,
                           info.PixelFormat,
                           createSrv,
                           enableAutoGenMipMap);
                succeeded = resource != null;
            }
        } finally {
            texture.Complete(info, succeeded);
        }
    }

    public void CreateView(Stream texture, bool createSrv = true, bool enableAutoGenMipMap = true) {
        if (nativeDevice == null || texture == null) return;

        var originalPosition = texture.CanSeek ? texture.Position : 0;
        try {
            if (texture.CanSeek) texture.Position = 0;

            using var image = Image.Load(texture);
            if (image == null || image.Description.Dimension != TextureDimension.Texture2D) return;

            RemoveAndDispose(ref textureView);
            RemoveAndDispose(ref resource);

            var description = new Texture2DDescription {
                Width = image.Description.Width,
                Height = image.Description.Height,
                MipLevels = image.Description.MipLevels,
                ArraySize = image.Description.ArraySize,
                Format = image.Description.Format,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = createSrv ? BindFlags.ShaderResource : BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Immutable
            };
            resource = nativeDevice.CreateTexture2D(description, image.ToDataBox());
            TextureFormat = description.Format;
            if (createSrv) CreateTextureView();
        } finally {
            if (texture.CanSeek) texture.Position = originalPosition;
        }
    }

    public void CreateView(ShaderResourceViewDescription desc) {
        CreateTextureView(ref desc);
    }

    public void CreateView(DepthStencilViewDescription desc) {
        CreateDepthStencilView(ref desc);
    }

    public void CreateView(RenderTargetViewDescription desc) {
        CreateRenderTargetView(ref desc);
    }

    public void CreateTextureView() {
        if (nativeDevice == null || resource == null) return;

        RemoveAndDispose(ref textureView);
        textureView = nativeDevice.CreateShaderResourceView(resource);
        TextureFormat = textureView?.Description.Format ?? default;
    }

    public void CreateTextureView(ShaderResourceViewDescription desc) {
        CreateTextureView(ref desc);
    }

    public void CreateTextureView(ref ShaderResourceViewDescription desc) {
        if (nativeDevice == null || resource == null) {
            TextureFormat = desc.Format;
            return;
        }

        RemoveAndDispose(ref textureView);
        textureView = nativeDevice.CreateShaderResourceView(resource, desc);
        TextureFormat = desc.Format;
    }

    public void CreateRenderTargetView() {
        if (nativeDevice == null || resource == null) return;

        RemoveAndDispose(ref renderTargetView);
        renderTargetView = nativeDevice.CreateRenderTargetView(resource);
    }

    public void CreateRenderTargetView(RenderTargetViewDescription desc) {
        CreateRenderTargetView(ref desc);
    }

    public void CreateRenderTargetView(ref RenderTargetViewDescription desc) {
        if (nativeDevice == null || resource == null) return;

        RemoveAndDispose(ref renderTargetView);
        renderTargetView = nativeDevice.CreateRenderTargetView(resource, desc);
    }

    public void CreateDepthStencilView() {
        if (nativeDevice == null || resource == null) return;

        RemoveAndDispose(ref depthStencilView);
        depthStencilView = nativeDevice.CreateDepthStencilView(resource);
    }

    public void CreateDepthStencilView(DepthStencilViewDescription desc) {
        CreateDepthStencilView(ref desc);
    }

    public void CreateDepthStencilView(ref DepthStencilViewDescription desc) {
        if (nativeDevice == null || resource == null) return;

        RemoveAndDispose(ref depthStencilView);
        depthStencilView = nativeDevice.CreateDepthStencilView(resource, desc);
    }

    public void CreateView<T>(T[] array, Format format, bool createSrv = true, bool generateMipMaps = true)
        where T : unmanaged {
        TextureFormat = format;
        if (array == null) return;
        CreateView(array, array.Length, format, createSrv, generateMipMaps);
    }

    public void CreateView<T>(
        T[] array,
        int length,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    )
        where T : unmanaged {
        TextureFormat = format;
        if (array == null || length <= 0) return;
        unsafe {
            fixed (T* arrayPtr = array) {
                CreateView((nint)arrayPtr, length, format, sizeof(T), createSrv, generateMipMaps);
            }
        }
    }

    public void CreateView(
        nint dataPtr,
        int width,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        CreateView(dataPtr, width, format, GetFormatSizeInBytes(format), createSrv, generateMipMaps);
    }

    public void CreateView<T>(
        T[] array,
        int width,
        int height,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    )
        where T : unmanaged {
        TextureFormat = format;
        if (array == null) return;
        unsafe {
            fixed (T* arrayPtr = array) {
                CreateView((nint)arrayPtr,
                           width,
                           height,
                           format,
                           GetFormatSizeInBytes(format),
                           createSrv,
                           generateMipMaps);
            }
        }
    }

    public void CreateView(
        nint dataPtr,
        int width,
        int height,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        CreateView(dataPtr, width, height, format, GetFormatSizeInBytes(format), createSrv, generateMipMaps);
    }

    public void CreateView<T>(
        T[] pixels,
        int width,
        int height,
        int depth,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    )
        where T : unmanaged {
        unsafe {
            fixed (T* pixelsPtr = pixels) {
                CreateView((nint)pixelsPtr,
                           width,
                           height,
                           depth,
                           format,
                           sizeof(T),
                           createSrv,
                           generateMipMaps);
            }
        }
    }

    public void CreateView(
        nint dataPtr,
        int width,
        int height,
        int depth,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        CreateView(dataPtr,
                   width,
                   height,
                   depth,
                   format,
                   GetFormatSizeInBytes(format),
                   createSrv,
                   generateMipMaps);
    }

    private void CreateView(
        nint dataPtr,
        int width,
        Format format,
        int bytesPerPixel,
        bool createSrv,
        bool generateMipMaps
    ) {
        TextureFormat = format;
        if (nativeDevice == null || dataPtr == nint.Zero || width <= 0 || bytesPerPixel <= 0) return;

        RemoveAndDispose(ref textureView);
        RemoveAndDispose(ref resource);

        var desc = new Texture1DDescription {
            Width = width,
            ArraySize = 1,
            MipLevels = 1,
            Format = format,
            BindFlags = createSrv ? BindFlags.ShaderResource : BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            Usage = ResourceUsage.Immutable
        };
        resource = nativeDevice.CreateTexture1D(desc, [new DataBox(dataPtr, width * bytesPerPixel, 0)]);

        if (createSrv) {
            var srvDesc = new ShaderResourceViewDescription {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture1D,
                Texture1D = new ShaderResourceViewDescription.Texture1DResource {
                    MostDetailedMip = 0,
                    MipLevels = 1
                }
            };
            CreateTextureView(ref srvDesc);
        }
    }

    private void CreateView(
        nint dataPtr,
        int width,
        int height,
        Format format,
        int bytesPerPixel,
        bool createSrv,
        bool generateMipMaps
    ) {
        TextureFormat = format;
        if (nativeDevice == null || dataPtr == nint.Zero || width <= 0 || height <= 0 ||
            bytesPerPixel <= 0) return;

        RemoveAndDispose(ref textureView);
        RemoveAndDispose(ref resource);

        var desc = new Texture2DDescription {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            BindFlags = createSrv ? BindFlags.ShaderResource : BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            Usage = ResourceUsage.Immutable
        };
        resource = nativeDevice.CreateTexture2D(desc,
                                                [
                                                    new DataBox(dataPtr,
                                                                width * bytesPerPixel,
                                                                width * height * bytesPerPixel)
                                                ]);

        if (createSrv) {
            var srvDesc = new ShaderResourceViewDescription {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource {
                    MostDetailedMip = 0,
                    MipLevels = 1
                }
            };
            CreateTextureView(ref srvDesc);
        }
    }

    private void CreateView(
        nint dataPtr,
        int width,
        int height,
        int depth,
        Format format,
        int bytesPerPixel,
        bool createSrv,
        bool generateMipMaps
    ) {
        TextureFormat = format;
        if (nativeDevice == null || dataPtr == nint.Zero || width <= 0 || height <= 0 || depth <= 0 ||
            bytesPerPixel <= 0) return;

        RemoveAndDispose(ref textureView);
        RemoveAndDispose(ref resource);

        var desc = new Texture3DDescription {
            Width = width,
            Height = height,
            Depth = depth,
            MipLevels = 1,
            Format = format,
            BindFlags = createSrv ? BindFlags.ShaderResource : BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            Usage = ResourceUsage.Immutable
        };
        var data = new[] {
            new DataBox(dataPtr, width * bytesPerPixel, width * height * bytesPerPixel)
        };
        resource = nativeDevice.CreateTexture3D(desc, data);

        if (createSrv) {
            var srvDesc = new ShaderResourceViewDescription {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture3D,
                Texture3D = new ShaderResourceViewDescription.Texture3DResource {
                    MostDetailedMip = 0,
                    MipLevels = 1
                }
            };
            CreateTextureView(ref srvDesc);
        }
    }

    private static int GetFormatSizeInBytes(Format format) {
        return format switch {
            Format.FormatR8Unorm => 1,
            Format.FormatR16Unorm => 2,
            Format.FormatR32Float => 4,
            Format.FormatR8G8B8A8Unorm => 4,
            Format.FormatB8G8R8A8Unorm => 4,
            Format.FormatR16G16B16A16Float => 8,
            Format.FormatR32G32B32A32Float => 16,
            _ => 0
        };
    }

    public void CreateViewFromColorArray(Color4[] array) {
        CreateView(array, Format.FormatR32G32B32A32Float);
    }

    public void CreateViewFromColorArray(
        Color4[] array,
        int width,
        int height,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        CreateView(array, width, height, Format.FormatR32G32B32A32Float, createSrv, generateMipMaps);
    }

    public static ShaderResourceViewProxy CreateView<T>(
        object device,
        T[] array,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    )
        where T : unmanaged {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(array, format, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateView(
        object device,
        Stream texture,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(texture, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateView<T>(
        object device,
        T[] array,
        int width,
        int height,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    )
        where T : unmanaged {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(array, width, height, format, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateView(
        object device,
        nint dataPtr,
        int width,
        int height,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(dataPtr, width, height, format, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateViewFromColorArray(object device, Color4[] array) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateViewFromColorArray(array);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateViewFromColorArray(
        object device,
        Color4[] array,
        int width,
        int height,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateViewFromColorArray(array, width, height, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateViewFromPixelData(
        object device,
        byte[] pixels,
        int width,
        int height,
        int depth,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(pixels, width, height, depth, format, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateViewFromPixelData(
        object device,
        Half4[] pixels,
        int width,
        int height,
        int depth,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(pixels, width, height, depth, format, createSrv, generateMipMaps);
        return proxy;
    }

    public static ShaderResourceViewProxy CreateViewFromPixelData(
        object device,
        nint pixels,
        int width,
        int height,
        int depth,
        Format format,
        bool createSrv = true,
        bool generateMipMaps = true
    ) {
        var proxy = new ShaderResourceViewProxy(device);
        proxy.CreateView(pixels, width, height, depth, format, createSrv, generateMipMaps);
        return proxy;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref textureView);
        RemoveAndDispose(ref depthStencilView);
        RemoveAndDispose(ref renderTargetView);
        RemoveAndDispose(ref resource);
        base.OnDispose(disposeManagedResources);
    }

    private static NativeD3DDevice? ResolveNativeDevice(object device) {
        return device switch {
            DeviceContextProxy contextProxy => contextProxy.NativeDevice,
            NativeD3DDevice silkDevice => silkDevice,
            INativeDeviceResources nativeResources => nativeResources.Device,
            IDevice3DResources deviceResources => deviceResources.NativeDeviceResources?.Device,
            _ => null
        };
    }

    public static implicit operator ShaderResourceView?(ShaderResourceViewProxy? proxy) => proxy?.textureView;

    public static implicit operator DepthStencilView?(ShaderResourceViewProxy? proxy) => proxy?.depthStencilView;

    public static implicit operator RenderTargetView?(ShaderResourceViewProxy? proxy) => proxy?.renderTargetView;
}
