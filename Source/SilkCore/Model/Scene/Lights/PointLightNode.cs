/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Lights;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Lights;
/// <summary>
/// </summary>
public class PointLightNode : LightNode {
    private PointLightCore LightCore => RenderCore as PointLightCore
        ?? throw new InvalidOperationException("Render core is not a point light core.");

    /// <summary>
    ///     Gets or sets the position.
    /// </summary>
    /// <value>
    ///     The position.
    /// </value>
    public Vector3 Position {
        get => LightCore.Position;
        set => LightCore.Position = value;
    }

    /// <summary>
    ///     Gets or sets the attenuation.
    /// </summary>
    /// <value>
    ///     The attenuation.
    /// </value>
    public Vector3 Attenuation {
        get => LightCore.Attenuation;
        set => LightCore.Attenuation = value;
    }

    /// <summary>
    ///     Gets or sets the range.
    /// </summary>
    /// <value>
    ///     The range.
    /// </value>
    public float Range {
        get => LightCore.Range;
        set => LightCore.Range = value;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PointLightCore();
}
