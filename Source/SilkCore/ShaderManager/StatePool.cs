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

    protected override bool CanCreate(ref BlendStateDescription key, ref BlendStateDescription argument) {
        return !IsDisposed;
    }

    protected override BlendStateProxy OnCreate(
        ref BlendStateDescription key,
        ref BlendStateDescription description
    ) {
        if (device.FeatureLevel < SilkFeatureLevel.Level_11_0 && description.IndependentBlendEnable)
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
    ) {
        return !IsDisposed;
    }

    protected override DepthStencilStateProxy OnCreate(
        ref DepthStencilStateDescription key,
        ref DepthStencilStateDescription description
    ) {
        return new DepthStencilStateProxy(device.CreateDepthStencilState(description));
    }
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
    ) {
        return !IsDisposed;
    }

    protected override RasterizerStateProxy OnCreate(
        ref RasterizerStateDescription key,
        ref RasterizerStateDescription description
    ) {
        return new RasterizerStateProxy(device.CreateRasterizerState(description));
    }
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

    protected override bool CanCreate(ref SamplerStateDescription key, ref SamplerStateDescription argument) {
        return !IsDisposed;
    }

    protected override SamplerStateProxy OnCreate(
        ref SamplerStateDescription key,
        ref SamplerStateDescription description
    ) {
        return new SamplerStateProxy(device.CreateSamplerState(description));
    }
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
    public BlendStateProxy Register(BlendStateDescription desc) {
        return BlendStatePool.TryCreateOrGet(desc, desc, out var state) ? state : null;
    }

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public RasterizerStateProxy Register(RasterizerStateDescription desc) {
        return RasterStatePool.TryCreateOrGet(desc, desc, out var state) ? state : null;
    }

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public DepthStencilStateProxy Register(DepthStencilStateDescription desc) {
        return DepthStencilStatePool.TryCreateOrGet(desc, desc, out var state) ? state : null;
    }

    /// <summary>
    ///     Registers the specified desc.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    public SamplerStateProxy Register(SamplerStateDescription desc) {
        return SamplerStatePool.TryCreateOrGet(desc, desc, out var state) ? state : null;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        BlendStatePool.Dispose();
        RasterStatePool.Dispose();
        DepthStencilStatePool.Dispose();
        SamplerStatePool.Dispose();
        base.OnDispose(disposeManagedResources);
    }
}
