/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class PointLightNode : LightNode {
    /// <summary>
    ///     Gets or sets the position.
    /// </summary>
    /// <value>
    ///     The position.
    /// </value>
    public Vector3 Position {
        get => (RenderCore as PointLightCore).Position;
        set => (RenderCore as PointLightCore).Position = value;
    }

    /// <summary>
    ///     Gets or sets the attenuation.
    /// </summary>
    /// <value>
    ///     The attenuation.
    /// </value>
    public Vector3 Attenuation {
        get => (RenderCore as PointLightCore).Attenuation;
        set => (RenderCore as PointLightCore).Attenuation = value;
    }

    /// <summary>
    ///     Gets or sets the range.
    /// </summary>
    /// <value>
    ///     The range.
    /// </value>
    public float Range {
        get => (RenderCore as PointLightCore).Range;
        set => (RenderCore as PointLightCore).Range = value;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        return new PointLightCore();
    }
}
