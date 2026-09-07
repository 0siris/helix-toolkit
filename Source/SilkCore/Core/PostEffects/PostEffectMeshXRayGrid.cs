/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core.PostEffects;

public interface IPostEffectMeshXRayGrid : IPostEffect {
    Color4 Color { get; set; }

    int GridDensity { get; set; }

    float DimmingFactor { get; set; }

    float BlendingFactor { get; set; }

    string XRayDrawingPassName { get; set; }

    bool UseDepthOcclusion { get; set; }
}

/// <summary>
/// </summary>
public class PostEffectMeshXRayGridCore : RenderCore, IPostEffectMeshXRayGrid {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEffectMeshXRayGridCore" /> class.
    /// </summary>
    public PostEffectMeshXRayGridCore() : base(RenderType.PostEffect) {
        Color = new Color4(0, 0, 1, 1);
    }

    protected override bool OnUpdateCanRenderFlag()
        => IsAttached && !string.IsNullOrEmpty(EffectName);

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.Param.M11 = gridDensity;
        modelStruct.Param.M12 = dimmingFactor;
        modelStruct.Param.M13 = blendingFactor;
    }

    #region Variables

    private readonly List<(Model.Scene.Abstract.SceneNode SceneNode, IEffectAttributes Effect)> currentCores = [];
    private BorderEffectStruct modelStruct;

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get;
        set => SetAffectsCanRenderFlag(ref field, value);
    } = DefaultRenderTechniqueNames.PostEffectMeshXRayGrid;

    /// <summary>
    ///     Gets or sets the color of the border.
    /// </summary>
    /// <value>
    ///     The color of the border.
    /// </value>
    public Color4 Color {
        get => modelStruct.Color;
        set => SetAffectsRender(ref modelStruct.Color, value);
    }

    private int gridDensity = 8;

    /// <summary>
    ///     Gets or sets the grid density.
    /// </summary>
    /// <value>
    ///     The grid density.
    /// </value>
    public int GridDensity {
        get => gridDensity;
        set => SetAffectsRender(ref gridDensity, value);
    }

    private float dimmingFactor = 0.8f;

    /// <summary>
    ///     Gets or sets the dim factor on original color
    /// </summary>
    /// <value>
    ///     The dim factor.
    /// </value>
    public float DimmingFactor {
        get => dimmingFactor;
        set => SetAffectsRender(ref dimmingFactor, value);
    }

    private float blendingFactor = 1f;

    /// <summary>
    ///     Gets or sets the blending factor for grid and original mesh color blending
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public float BlendingFactor {
        get => blendingFactor;
        set => SetAffectsRender(ref blendingFactor, value);
    }

    /// <summary>
    ///     Gets or sets the name of the x ray drawing pass. This is the final pass to draw mesh and grid overlay onto render
    ///     target
    /// </summary>
    /// <value>
    ///     The name of the x ray drawing pass.
    /// </value>
    public string XRayDrawingPassName { get; set; } = DefaultPassNames.EffectMeshXRayGridP3;

    /// <summary>
    ///     Uses the scene depth buffer to hide x-ray parts that are not occluded.
    ///     Disable this for overlays that must stay visible after OIT rendering.
    /// </summary>
    public bool UseDepthOcclusion {
        get;
        set => SetAffectsRender(ref field, value);
    } = true;

    #endregion
}
