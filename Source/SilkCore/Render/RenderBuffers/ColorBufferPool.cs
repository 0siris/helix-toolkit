using System.Collections.Concurrent;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public sealed class PingPongColorBuffers : DisposeObject {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly IDevice3DResources deviceResources;
    private readonly object lockObj = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="PingPongColorBuffers" /> class.
    /// </summary>
    /// <param name="textureFormat">The texture format.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="deviceRes">The device resource.</param>
    public PingPongColorBuffers(Format textureFormat, int width, int height, IDevice3DResources deviceRes) {
        texture2DDesc.Format = textureFormat;
        deviceResources = deviceRes;
        texture2DDesc.Width = width;
        texture2DDesc.Height = height;
    }

    /// <summary>
    ///     Gets the current ShaderResourceViewProxy.
    /// </summary>
    /// <value>
    ///     The current SRV.
    /// </value>
    public ShaderResourceViewProxy? CurrentSrv => textures[0];

    /// <summary>
    ///     Gets the next SRV.
    /// </summary>
    /// <value>
    ///     The next SRV.
    /// </value>
    public ShaderResourceViewProxy? NextSrv => textures[1];

    public int Width => texture2DDesc.Width;

    public int Height => texture2DDesc.Height;

    /// <summary>
    ///     Gets the current RenderTargetView.
    /// </summary>
    /// <value>
    ///     The current RTV.
    /// </value>
    public ShaderResourceViewProxy? CurrentRtv => textures[0];

    /// <summary>
    ///     Gets the next RTV.
    /// </summary>
    /// <value>
    ///     The next RTV.
    /// </value>
    public ShaderResourceViewProxy? NextRtv => textures[1];

    public Resource? CurrentTexture => textures[0]?.Resource;
    public bool Initialized { get; private set; }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public void Initialize() {
        lock (lockObj) {
            if (Initialized) return;
            for (var i = 0; i < NumPingPongBlurBuffer; ++i) {
                var texture = new ShaderResourceViewProxy(deviceResources, texture2DDesc);
                texture.CreateRenderTargetView();
                texture.CreateTextureView();
                textures[i] = texture;
            }

            Initialized = true;
        }
    }

    /// <summary>
    ///     Swaps the targets.
    /// </summary>
    public void SwapTargets() {
        lock (lockObj) {
            //swap buffer
            var current = textures[0];
            textures[0] = textures[1];
            textures[1] = current;
        }
    }

    protected override void OnDispose(bool disposeManagedResources) {
        for (var i = 0; i < NumPingPongBlurBuffer; ++i) RemoveAndDispose(ref textures[i]);
        base.OnDispose(disposeManagedResources);
    }

    #region Texture Resources

    private const int NumPingPongBlurBuffer = 2;

    private readonly ShaderResourceViewProxy?[] textures = new ShaderResourceViewProxy?[NumPingPongBlurBuffer];

    private readonly Texture2DDescription texture2DDesc = new() {
        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
        CpuAccessFlags = CpuAccessFlags.None,
        Usage = ResourceUsage.Default,
        ArraySize = 1,
        MipLevels = 1,
        OptionFlags = ResourceOptionFlags.None,
        SampleDescription = new SampleDescription(1, 0)
    };

    #endregion Texture Resources
}


public sealed class TexturePool : DisposeObject {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly Texture2DDescription description;
    private readonly IDevice3DResources deviceResourse;

    private readonly ConcurrentDictionary<Format, ConcurrentBag<ShaderResourceViewProxy>> pool = new();

    public TexturePool(IDevice3DResources deviceResourse, Texture2DDescription desc) {
        this.deviceResourse = deviceResourse;
        description = desc;
    }

    public int Width => description.Width;

    public int Height => description.Height;

    /// <summary>
    ///     Gets the off screen texture with specified format. After using it, make sure to call Dispose() to return it back
    ///     into the pool.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns></returns>
    public ShaderResourceViewProxy Get(Format format) {
        if (IsDisposed) return ShaderResourceViewProxy.Empty;
        if (pool.TryGetValue(format, out var bag) && bag.TryTake(out var proxy) && !proxy.IsDisposed) {
            proxy.IncRef();
            return proxy;
        }

        bag ??= pool.GetOrAdd(format, _ => []);
        var desc = description;
        desc.Format = format;
        ShaderResourceViewProxy? texture = null;

        if ((desc.BindFlags & BindFlags.RenderTarget) != 0) {
            texture = new PooledShaderResourceViewProxy(deviceResourse, desc, bag);
            texture.CreateRenderTargetView();
            if ((desc.BindFlags & BindFlags.ShaderResource) != 0) texture.CreateTextureView();
        } else if ((desc.BindFlags & BindFlags.DepthStencil) != 0) {
            desc.Format = format.ComputeTextureFormat(out var canUseAsShaderResource);
            if (canUseAsShaderResource) desc.BindFlags |= BindFlags.ShaderResource;
            texture = new PooledShaderResourceViewProxy(deviceResourse, desc, bag);
            texture.CreateView(new DepthStencilViewDescription {
                Format = format.ComputeDsvFormat(),
                Dimension = DepthStencilViewDimension.Texture2D
            });
            if (canUseAsShaderResource)
                texture.CreateView(new ShaderResourceViewDescription {
                    Format = format.ComputeSrvFormat(),
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = desc.MipLevels }
                });
        }

        if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Create New Full Screen Texture");
        if (texture is null) return ShaderResourceViewProxy.Empty;
        texture.IncRef();
        return texture;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        foreach (var bag in pool.Values)
            while (bag.TryTake(out var proxy))
                proxy.Dispose(); // Set flag to false so it can be disposed

        pool.Clear();
        base.OnDispose(disposeManagedResources);
    }

    private sealed class PooledShaderResourceViewProxy : ShaderResourceViewProxy {
        private readonly ConcurrentBag<ShaderResourceViewProxy> pool;

        public PooledShaderResourceViewProxy(
            IDevice3DResources deviceResources,
            Texture2DDescription textureDesc,
            ConcurrentBag<ShaderResourceViewProxy> pool
        )
            : base(deviceResources, textureDesc) {
            this.pool = pool;
            AddBackToPool = _ => { pool.Add(this); };
        }
    }
}
