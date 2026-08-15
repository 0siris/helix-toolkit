using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Utilities.Buffers;
/// <summary>
/// </summary>
/// <typeparam name="StateType">The type of the tate type.</typeparam>
public abstract class StateProxy<StateType> : DisposeObject where StateType : class, IDisposable {
    private StateType? state;

    public StateProxy(StateType? state) {
        this.state = state;
    }

    /// <summary>
    ///     Gets the state.
    /// </summary>
    /// <value>
    ///     The state.
    /// </value>
    public StateType? State => state;

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref state);
        base.OnDispose(disposeManagedResources);
    }

    /// <summary>
    ///     Performs an implicit conversion
    /// </summary>
    /// <param name="proxy">The proxy.</param>
    /// <returns>
    ///     The result of the conversion.
    /// </returns>
    public static implicit operator StateType?(StateProxy<StateType> proxy) => proxy.State;
}

/// <summary>
/// </summary>
public sealed class RasterizerStateProxy : StateProxy<RasterizerState> {
    public static readonly RasterizerStateProxy Empty = new(null);

    internal RasterizerStateProxy(RasterizerState? state) : base(state) { }
}

/// <summary>
/// </summary>
public sealed class BlendStateProxy : StateProxy<BlendState> {
    public static readonly BlendStateProxy Empty = new(null);

    internal BlendStateProxy(BlendState? state) : base(state) { }
}

/// <summary>
/// </summary>
public sealed class DepthStencilStateProxy : StateProxy<DepthStencilState> {
    public static readonly DepthStencilStateProxy Empty = new(null);

    internal DepthStencilStateProxy(DepthStencilState? state) : base(state) { }
}

/// <summary>
/// </summary>
public sealed class SamplerStateProxy : StateProxy<SamplerState> {
    public static readonly SamplerStateProxy Empty = new(null);

    internal SamplerStateProxy(SamplerState? state) : base(state) { }
}
