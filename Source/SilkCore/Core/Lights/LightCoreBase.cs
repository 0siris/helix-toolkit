/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;

namespace HelixToolkit.SharpDX.Core.Core.Lights;

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

    protected override bool OnAttach(IRenderTechnique technique) 
        => true;

    protected override void OnDetach() { }

    /// <summary>
    ///     Renders the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    public sealed override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (!CanRender(context.LightScene))
            return;
        
        OnRender(context.LightScene, context.LightScene.LightModels.LightCount);
        
        switch (LightType) {
            case LightType.Ambient:
                break;
            default:
                context.LightScene.LightModels.IncrementLightCount();
                break;
        }
    }

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