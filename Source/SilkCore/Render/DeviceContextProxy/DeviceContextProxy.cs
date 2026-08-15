using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public sealed partial class DeviceContextProxy : DisposeObject {
    public static bool AutoSkipRedundantStateSetting = false;
    public readonly bool IsDeferred;
    private Color4? currBlendFactor;
    private BlendStateProxy? currBlendState;
    private DepthStencilStateProxy? currDepthStencilState;
    private RasterizerStateProxy? currRasterState;
    private uint currSampleMask = uint.MaxValue;
    private int currStencilRef;
    private SilkD3DDeviceContext? nativeDeviceContext;

    #region Constructor

    /// <summary>
    ///     Initializes a proxy for a native Silk.NET D3D11 context.
    /// </summary>
    /// <param name="context">The native context.</param>
    /// <param name="device">The native device.</param>
    internal DeviceContextProxy(SilkD3DDeviceContext context, SilkD3DDevice device) {
        nativeDeviceContext = context;
        NativeDevice = device;
        IsDeferred = context.IsDeferred;
    }

    #endregion Constructor

    internal SilkD3DDeviceContext NativeContext
        => nativeDeviceContext ?? throw new ObjectDisposedException(nameof(DeviceContextProxy));

    internal SilkD3DDevice NativeDevice { get; }

    /// <summary>
    ///     Resets this instance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() {
        currRasterState = null;
        currBlendState = null;
        currDepthStencilState = null;
        currBlendFactor = null;
        currSampleMask = uint.MaxValue;
        currStencilRef = 0;
        currInputLayout = null;
        PrimitiveTopology = PrimitiveTopology.Undefined;
        CurrShaderPass = null;
        for (var i = 0; i < constantBufferCheck.Length; ++i) constantBufferCheck[i] = null;
        for (var i = 0; i < samplerStateCheck.Length; ++i) samplerStateCheck[i] = null;
    }

    /// <summary>
    ///     Restore all default settings.
    /// </summary>
    /// <remarks>
    ///     This method resets any device context to the default settings.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearState() {
        NativeContext.ClearState();
        Reset();
    }

    /// <summary>
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    protected override void OnDispose(bool disposeManagedResources) {
        if (nativeDeviceContext is { } context && !context.IsDisposed) context.ClearState();
        if (IsDeferred) RemoveAndDispose(ref nativeDeviceContext);
        base.OnDispose(disposeManagedResources);
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the last shader pass.
    /// </summary>
    /// <value>
    ///     The last shader pass.
    /// </value>
    public ShaderPass? CurrShaderPass { get; private set; }

    /// <summary>
    ///     Gets the number of draw calls.
    /// </summary>
    /// <value>
    ///     The number of draw calls.
    /// </value>
    public int NumberOfDrawCalls { get; private set; }

    #endregion Properties
}
