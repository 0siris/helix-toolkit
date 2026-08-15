/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class SpotLightNode : PointLightNode {
    private SpotLightCore LightCore => RenderCore as SpotLightCore
        ?? throw new InvalidOperationException("Spot-light render core is not initialized.");

    /// <summary>
    ///     Gets or sets the direction.
    /// </summary>
    /// <value>
    ///     The direction.
    /// </value>
    public Vector3 Direction {
        get => LightCore.Direction;
        set => LightCore.Direction = value;
    }

    /// <summary>
    ///     Gets or sets the fall off.
    /// </summary>
    /// <value>
    ///     The fall off.
    /// </value>
    public float FallOff {
        get => LightCore.FallOff;
        set => LightCore.FallOff = value;
    }

    /// <summary>
    ///     Gets or sets the inner angle.
    /// </summary>
    /// <value>
    ///     The inner angle.
    /// </value>
    public float InnerAngle {
        get => LightCore.InnerAngle;
        set => LightCore.InnerAngle = value;
    }

    /// <summary>
    ///     Gets or sets the outer angle.
    /// </summary>
    /// <value>
    ///     The outer angle.
    /// </value>
    public float OuterAngle {
        get => LightCore.OuterAngle;
        set => LightCore.OuterAngle = value;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new SpotLightCore();
}
