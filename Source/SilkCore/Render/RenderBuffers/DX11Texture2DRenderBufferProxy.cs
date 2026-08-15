/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public class DX11Texture2DRenderBufferProxy : DX11RenderBufferProxyBase {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11Texture2DRenderBufferProxy" /> class.
    /// </summary>
    /// <param name="deviceResources"></param>
    public DX11Texture2DRenderBufferProxy(IDeviceResources deviceResources) : base(deviceResources) { }

    /// <summary>
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    protected override ShaderResourceViewProxy OnCreateBackBuffer(int width, int height) {
        var colordescNms = new Texture2DDescription {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Format,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            OptionFlags = ResourceOptionFlags.Shared,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        var backBuffer = new ShaderResourceViewProxy(DeviceResources, colordescNms);
        d2dTarget = new D2DTargetProxy();
        if (backBuffer.Resource is not Texture2D texture)
            throw new System.InvalidOperationException("The back buffer is not a texture resource.");

        d2dTarget.Initialize(texture, DeviceContext2D);
        return backBuffer;
    }

    /// <summary>
    ///     Presents this instance.
    /// </summary>
    /// <returns></returns>
    public override bool Present() {
        DeviceResources.NativeDeviceResources.ImmediateContext.Flush();
        return true;
    }
}
