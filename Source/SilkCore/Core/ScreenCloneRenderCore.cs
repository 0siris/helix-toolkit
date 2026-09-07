/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;

#if !WINDOWS_UWP //TODO why do we need this here?!
namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
///     Screen duplication render-core contract.
/// </summary>
public interface IScreenClone {
    /// <summary>
    ///     Gets or sets the output.
    /// </summary>
    int Output { get; set; }

    /// <summary>
    ///     Gets or sets the clone rectangle.
    /// </summary>
    Rectangle CloneRectangle { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether cloned rectangle is stretched during rendering, default is false.
    /// </summary>
    bool StretchToFill { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [show mouse cursor].
    /// </summary>
    bool ShowMouseCursor { get; set; }
}

/// <summary>
///     Owns the renderer-independent lifecycle of one lazily created desktop-capture source.
/// </summary>
public class ScreenCloneRenderCore : RenderCore, IScreenClone {
    /// <summary>
    ///     Creates the isolated capture implementation only when the first frame is requested.
    /// </summary>
    private readonly Func<IDesktopCaptureSource> sourceFactory;

    /// <summary>
    ///     Current capture source, or <see langword="null" /> before capture starts.
    /// </summary>
    private IDesktopCaptureSource? source;

    /// <summary>
    ///     Output used by the current source.
    /// </summary>
    private int activeOutput = -1;

    /// <summary>
    ///     Initializes the productive core without loading Direct3D 11.
    /// </summary>
    public ScreenCloneRenderCore()
        : this(static () => new D3D11DesktopCaptureSource()) { }

    /// <summary>
    ///     Initializes the core with a focused capture source factory.
    /// </summary>
    /// <param name="sourceFactory">The source factory used by deterministic tests.</param>
    internal ScreenCloneRenderCore(Func<IDesktopCaptureSource> sourceFactory)
        : base(RenderType.Opaque) => this.sourceFactory = sourceFactory
            ?? throw new ArgumentNullException(nameof(sourceFactory));

    /// <summary>
    ///     Gets or sets the output.
    /// </summary>
    public int Output {
        get;
        set {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (!Set(ref field, value)) return;
            StopCapture();
        }
    }

    /// <summary>
    ///     Gets or sets the clone rectangle.
    /// </summary>
    public Rectangle CloneRectangle {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating cloned rectangle is stretched during rendering, default is false.
    /// </summary>
    public bool StretchToFill {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [show mouse cursor].
    /// </summary>
    public bool ShowMouseCursor { get; set; } = true;

    /// <summary>
    ///     Gets whether the optional capture source has been created.
    /// </summary>
    internal bool IsCaptureStarted => source is not null;

    /// <summary>
    ///     Requests one frame without discarding the previously uploaded frame on timeout.
    /// </summary>
    /// <param name="timeout">The bounded duplication wait.</param>
    /// <returns>The acquired frame, or <see langword="null" /> on timeout.</returns>
    internal ScreenCaptureFrame? TryAcquireFrame(TimeSpan timeout) {
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (source is null) {
            var createdSource = sourceFactory();
            try {
                createdSource.Start(Output);
                source = createdSource;
                activeOutput = Output;
            } catch {
                createdSource.Dispose();
                throw;
            }
        } else if (activeOutput != Output) {
            StopCapture();
            return TryAcquireFrame(timeout);
        }
        try {
            return source.TryAcquire(timeout, out var frame) ? frame : null;
        } catch (ScreenCaptureResetException) {
            StopCapture();
            return null;
        }
    }

    /// <summary>
    ///     Stops capture and releases every Direct3D 11/DXGI resource.
    /// </summary>
    internal void StopCapture() {
        source?.Dispose();
        source = null;
        activeOutput = -1;
    }

    /// <inheritdoc />
    protected override bool OnUpdateCanRenderFlag() => IsAttached;

    /// <inheritdoc />
    protected override void OnDetachD3D12() => StopCapture();

    /// <inheritdoc />
    /// <inheritdoc />
    protected override void OnDispose(bool disposeManagedResources) {
        StopCapture();
        base.OnDispose(disposeManagedResources);
    }
}

/// <summary>
///     Provides one isolated desktop-capture session.
/// </summary>
internal interface IDesktopCaptureSource : IDisposable {
    /// <summary>Starts capture for one adapter-local output.</summary>
    /// <param name="output">The zero-based output index.</param>
    void Start(int output);

    /// <summary>Attempts to acquire one complete CPU-owned frame.</summary>
    /// <param name="timeout">The bounded wait.</param>
    /// <param name="frame">The acquired frame.</param>
    /// <returns>Whether a new frame was acquired.</returns>
    bool TryAcquire(TimeSpan timeout, out ScreenCaptureFrame frame);
}

/// <summary>
///     Contains one complete desktop frame after it has crossed the D3D11 isolation boundary.
/// </summary>
/// <param name="Width">The pixel width.</param>
/// <param name="Height">The pixel height.</param>
/// <param name="Format">The DXGI pixel format.</param>
/// <param name="RowPitch">The tightly packed row pitch.</param>
/// <param name="Pixels">The tightly packed pixels.</param>
internal readonly record struct ScreenCaptureFrame(
    uint Width,
    uint Height,
    Silk.NET.DXGI.Format Format,
    uint RowPitch,
    byte[] Pixels
) {
    /// <summary>Validates a frame before it crosses into Direct3D 12.</summary>
    internal void Validate() {
        ArgumentOutOfRangeException.ThrowIfZero(Width);
        ArgumentOutOfRangeException.ThrowIfZero(Height);
        if (Format is not (Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm or
            Silk.NET.DXGI.Format.FormatB8G8R8A8UnormSrgb))
            throw new NotSupportedException($"Desktop format {Format} is not supported.");
        var minimumPitch = checked(Width * 4);
        if (RowPitch != minimumPitch || Pixels.Length != checked((int)(RowPitch * Height)))
            throw new ArgumentException("Desktop frame bytes do not match the declared dimensions and format.");
    }
}

/// <summary>
///     Signals that DXGI duplication must be recreated, for example after a display mode change.
/// </summary>
internal sealed class ScreenCaptureResetException : Exception {
    /// <summary>Initializes the reset signal.</summary>
    internal ScreenCaptureResetException() : base("Desktop duplication must be recreated.") { }
}


#endif
