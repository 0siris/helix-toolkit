using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public abstract class LightCoreBase() : RenderCore(RenderType.Light), ILight3D {
    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get;
        set => SetAffectsRender(ref field, value);
    } = new(0.2f, 0.2f, 0.2f, 1.0f);

    /// <summary>
    ///     Gets or sets the type of the light.
    /// </summary>
    /// <value>
    ///     The type of the light.
    /// </value>
    public LightType LightType { get; protected set; }

    /// <summary>
    ///     Determines whether this instance can render the specified light scene.
    /// </summary>
    /// <param name="lightScene">The light scene.</param>
    /// <returns>
    ///     <c>true</c> if this instance can render the specified light scene; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool CanRender(Light3DSceneShared lightScene)
        => IsAttached && lightScene.LightModels.LightCount < Constants.MaxLights;

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="lightScene">The light scene.</param>
    /// <param name="idx">The index.</param>
    protected virtual void OnRender(Light3DSceneShared lightScene, int idx) {
        lightScene.LightModels.Lights[idx].LightColor = Color;
        lightScene.LightModels.Lights[idx].LightType = (int)LightType;
    }
}

/// <summary>
/// </summary>
public class AmbientLightCore : LightCoreBase {
    /// <summary>
    ///     Initializes a new instance of the <see cref="AmbientLightCore" /> class.
    /// </summary>
    public AmbientLightCore()
        => LightType = LightType.Ambient;

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="lightScene">The light scene.</param>
    /// <param name="idx">The index.</param>
    protected override void OnRender(Light3DSceneShared lightScene, int idx)
        => lightScene.LightModels.AmbientLight = Color;
}
