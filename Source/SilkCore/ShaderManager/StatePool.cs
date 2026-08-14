/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.ShaderManager;
/// <summary>
/// </summary>
public sealed class BlendStatePool : ReferenceCountedDictionaryPool<BlendStateDescription, BlendStateProxy,
    BlendStateDescription> {
    private readonly NativeD3DDevice device;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlendStatePool" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    internal BlendStatePool(NativeD3DDevice device) : base(true) {
        this.device = device;
    }

    protected override bool CanCreate(ref BlendStateDescription key, ref BlendStateDescription argument) => !IsDisposed;

    protected override BlendStateProxy OnCreate(
        ref BlendStateDescription key,
        ref BlendStateDescription description
    ) {
        if (device.FeatureLevel < SilkFeatureLevel.Level110 && description.IndependentBlendEnable)
            description.IndependentBlendEnable = false;
        return new BlendStateProxy(device.CreateBlendState(description));
    }
}

/// <summary>
/// </summary>
public sealed class DepthStencilStatePool : ReferenceCountedDictionaryPool<DepthStencilStateDescription,
    DepthStencilStateProxy, DepthStencilStateDescription> {
    private readonly NativeD3DDevice device;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DepthStencilStatePool" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    internal DepthStencilStatePool(NativeD3DDevice device) : base(true) {
        this.device = device;
    }

    protected override bool CanCreate(
        ref DepthStencilStateDescription key,
        ref DepthStencilStateDescription argument
    )
        => !IsDisposed;

    protected override DepthStencilStateProxy OnCreate(
        ref DepthStencilStateDescription key,
        ref DepthStencilStateDescription description
    )
        => new(device.CreateDepthStencilState(description));
}

/// <summary>
/// </summary>
public sealed class RasterStatePool : ReferenceCountedDictionaryPool<RasterizerStateDescription,
    RasterizerStateProxy, RasterizerStateDescription> {
    private readonly NativeD3DDevice device;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RasterStatePool" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    internal RasterStatePool(NativeD3DDevice device) : base(true) {
        this.device = device;
    }

    protected override bool CanCreate(
        ref RasterizerStateDescription key,
        ref RasterizerStateDescription argument
    )
        => !IsDisposed;

    protected override RasterizerStateProxy OnCreate(
        ref RasterizerStateDescription key,
        ref RasterizerStateDescription description
    )
        => new(device.CreateRasterizerState(description));
}

/// <summary>
/// </summary>
public sealed class SamplerStatePool : ReferenceCountedDictionaryPool<SamplerStateDescription, SamplerStateProxy
  , SamplerStateDescription> {
    private readonly NativeD3DDevice device;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SamplerStatePool" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    internal SamplerStatePool(NativeD3DDevice device) : base(true) {
        this.device = device;
    }

    protected override bool CanCreate(ref SamplerStateDescription key, ref SamplerStateDescription argument) => !IsDisposed;

    protected override SamplerStateProxy OnCreate(
        ref SamplerStateDescription key,
        ref SamplerStateDescription description
    )
        => new(device.CreateSamplerState(description));
}

/// <summary>
/// </summary>
public sealed class StatePoolManager : DisposeObject, IStatePoolManager {
    /// <summary>
    ///     Initializes a new instance of the <see cref="StatePoolManager" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    internal StatePoolManager(NativeD3DDevice device) {
        BlendStatePool = new BlendStatePool(device);
        RasterStatePool = new RasterStatePool(device);
        DepthStencilStatePool = new DepthStencilStatePool(device);
        SamplerStatePool = new SamplerStatePool(device);
    }

    /// <summary>
    ///     Gets or sets the blend state pool.
    /// </summary>
    /// <value>
    ///     The blend state pool.
    /// </value>
    public BlendStatePool BlendStatePool { get; }

    /// <summary>
    ///     Gets or sets the raster state pool.
    /// </summary>
    /// <value>
    ///     The raster state pool.
    /// </value>
    public RasterStatePool RasterStatePool { get; }

    /// <summary>
    ///     Gets or sets the depth stencil state pool.
    /// </summary>
    /// <value>
    ///     The depth stencil state pool.
    /// </value>
    public DepthStencilStatePool DepthStencilStatePool { get; }

    /// <summary>
    ///     Gets or sets the sampler state pool.
    /// </summary>
    /// <value>
    ///     The sampler state pool.
    /// </value>
    public SamplerStatePool SamplerStatePool { get; }

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public BlendStateProxy Register(BlendStateDescription desc) => BlendStatePool.TryCreateOrGet(desc, desc, out var state)
        ? state
        : throw new InvalidOperationException("Unable to register blend state.");

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public RasterizerStateProxy Register(RasterizerStateDescription desc) => RasterStatePool.TryCreateOrGet(desc, desc, out var state)
        ? state
        : throw new InvalidOperationException("Unable to register rasterizer state.");

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public DepthStencilStateProxy Register(DepthStencilStateDescription desc) => DepthStencilStatePool.TryCreateOrGet(desc, desc, out var state)
        ? state
        : throw new InvalidOperationException("Unable to register depth stencil state.");

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public SamplerStateProxy Register(SamplerStateDescription desc) => SamplerStatePool.TryCreateOrGet(desc, desc, out var state)
        ? state
        : throw new InvalidOperationException("Unable to register sampler state.");

    protected override void OnDispose(bool disposeManagedResources) {
        BlendStatePool.Dispose();
        RasterStatePool.Dispose();
        DepthStencilStatePool.Dispose();
        SamplerStatePool.Dispose();
        base.OnDispose(disposeManagedResources);
    }
}
