/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

/// <summary>
/// </summary>
public abstract class LightNode : SceneNode, ILight3D {
    private LightCoreBase LightCore
        => RenderCore as LightCoreBase
           ?? throw new InvalidOperationException("The light render core has not been created.");

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => LightCore.Color;
        set => LightCore.Color = value;
    }

    /// <summary>
    ///     Gets the type of the light.
    /// </summary>
    /// <value>
    ///     The type of the light.
    /// </value>
    public LightType LightType => LightCore.LightType;

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
