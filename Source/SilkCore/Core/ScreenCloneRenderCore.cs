/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

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
///     Native migration placeholder for the Desktop Duplication render core.
/// </summary>
/// <remarks>
///     The previous implementation depended on SharpDX DXGI output duplication types. The public render-core contract
///     is kept so scene nodes continue to compile; real Silk.NET DXGI duplication is a separate interop edge.
/// </remarks>
public class ScreenCloneRenderCore : RenderCore, IScreenClone {
    public ScreenCloneRenderCore()
        : base(RenderType.Opaque) { }

    /// <summary>
    ///     Gets or sets the output.
    /// </summary>
    public int Output {
        get;
        set => Set(ref field, value);
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

    protected override bool OnUpdateCanRenderFlag() => false;

    protected override bool OnAttach(IRenderTechnique technique) => true;

    protected override void OnDetach() { }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) { }
}


#endif
