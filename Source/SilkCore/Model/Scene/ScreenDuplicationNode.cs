/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

#if !WINDOWS_UWP
namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class ScreenDuplicationNode : SceneNode {
    private IScreenClone ScreenCloneCore => RenderCore as IScreenClone
        ?? throw new InvalidOperationException("Screen-duplication render core is not initialized.");

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScreenDuplicationNode" /> class.
    /// </summary>
    public ScreenDuplicationNode() {
        IsHitTestVisible = false;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new ScreenCloneRenderCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.ScreenDuplication];

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    #region Properties

    /// <summary>
    ///     Gets or sets the capture rectangle.
    /// </summary>
    /// <value>
    ///     The capture rectangle.
    /// </value>
    public Rectangle CaptureRectangle {
        get => ScreenCloneCore.CloneRectangle;
        set => ScreenCloneCore.CloneRectangle = value;
    }

    /// <summary>
    ///     Gets or sets the display index.
    /// </summary>
    /// <value>
    ///     The display index.
    /// </value>
    public int DisplayIndex {
        get => ScreenCloneCore.Output;
        set => ScreenCloneCore.Output = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [stretch to fill].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [stretch to fill]; otherwise, <c>false</c>.
    /// </value>
    public bool StretchToFill {
        get => ScreenCloneCore.StretchToFill;
        set => ScreenCloneCore.StretchToFill = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [show mouse cursor].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [show mouse cursor]; otherwise, <c>false</c>.
    /// </value>
    public bool ShowMouseCursor {
        get => ScreenCloneCore.ShowMouseCursor;
        set => ScreenCloneCore.ShowMouseCursor = value;
    }

    #endregion
}
#endif
