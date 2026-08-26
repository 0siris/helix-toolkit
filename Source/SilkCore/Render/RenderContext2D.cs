using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Render;

/// <summary>
/// </summary>
public sealed class RenderContext2D : DisposeObject {
    private readonly Stack<Matrix3X2> relativeTransformStack = new();

    /// <summary>
    ///     The viewport width.
    /// </summary>
    private readonly double actualWidth;

    /// <summary>
    ///     The viewport height.
    /// </summary>
    private readonly double actualHeight;

    /// <summary>
    ///     The physical-pixel scale.
    /// </summary>
    private readonly float dpiScale;

    /// <summary>
    ///     The target stack
    /// </summary>
    private readonly Stack<BitmapProxy> targetStack = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="RenderContext2D" /> class.
    /// </summary>
    /// <param name="deviceContext">The device context.</param>
    /// <param name="deviceResources">The Direct2D resources.</param>
    /// <param name="actualWidth">The viewport width.</param>
    /// <param name="actualHeight">The viewport height.</param>
    /// <param name="dpiScale">The physical-pixel scale.</param>
    public RenderContext2D(D2DDeviceContext deviceContext,
        IDevice2DResources deviceResources,
        double actualWidth,
        double actualHeight,
        float dpiScale = 1) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actualWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actualHeight);
        if (!float.IsFinite(dpiScale) || dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        DeviceContext = deviceContext;
        DeviceResources = deviceResources;
        this.actualWidth = actualWidth;
        this.actualHeight = actualHeight;
        this.dpiScale = dpiScale;
    }

    /// <summary>
    ///     Gets the actual width.
    /// </summary>
    /// <value>
    ///     The actual width.
    /// </value>
    public double ActualWidth => actualWidth;

    /// <summary>
    ///     Gets the actual height.
    /// </summary>
    /// <value>
    ///     The actual height.
    /// </value>
    public double ActualHeight => actualHeight;

    /// <summary>
    ///     Gets the dpi scale.
    /// </summary>
    /// <value>
    ///     The dpi scale.
    /// </value>
    public float DpiScale => dpiScale;

    /// <summary>
    ///     Gets or sets the device context.
    /// </summary>
    /// <value>
    ///     The device context.
    /// </value>
    public D2DDeviceContext DeviceContext { get; }

    /// <summary>
    ///     Gets the device resources.
    /// </summary>
    /// <value>
    ///     The device resources.
    /// </value>
    public IDevice2DResources DeviceResources { get; private set; }

    /// <summary>
    ///     Gets or sets the last bitmap transform.
    /// </summary>
    /// <value>
    ///     The last bitmap transform.<see cref="RenderContext2D.RelativeTransform" />
    /// </value>
    public Matrix3X2 RelativeTransform { get; private set; } = Matrix3X2.Identity;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance has target.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has target; otherwise, <c>false</c>.
    /// </value>
    public bool HasTarget { get; private set; }

    /// <summary>
    ///     Pushes the last bitmap transform.
    /// </summary>
    /// <param name="transform">The transform.</param>
    public void PushRelativeTransform(Matrix3X2 transform) {
        relativeTransformStack.Push(RelativeTransform);
        RelativeTransform = transform;
    }

    /// <summary>
    ///     Pops the last bitmap transform.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopRelativeTransform() {
        RelativeTransform = relativeTransformStack.Pop();
    }

    /// <summary>
    ///     Pushes the render target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="clear">if set to <c>true</c> [clear].</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushRenderTarget(BitmapProxy target, bool clear) {
        if (targetStack.Count > 0) DeviceContext.EndDraw();
        targetStack.Push(target);
        DeviceContext.Target = targetStack.Peek();
        HasTarget = true;
        DeviceContext.BeginDraw();
        if (clear) DeviceContext.Clear(new Color4(0, 0, 0, 0));
    }

    /// <summary>
    ///     Pops the render target.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopRenderTarget() {
        DeviceContext.EndDraw();
        DeviceContext.Target = null;
        HasTarget = false;
        targetStack.Pop();
        if (targetStack.Count > 0) {
            DeviceContext.Target = targetStack.Peek();
            HasTarget = true;
            DeviceContext.BeginDraw();
        }
    }
}
