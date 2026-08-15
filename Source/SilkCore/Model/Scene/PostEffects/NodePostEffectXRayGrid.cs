/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.PostEffects;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;
/// <summary>
/// </summary>
public class NodePostEffectXRayGrid : SceneNode {
    private IPostEffect PostEffectCore => RenderCore as IPostEffect
        ?? throw new InvalidOperationException("Post-effect render core is not initialized.");

    private IPostEffectMeshXRayGrid GridCore => RenderCore as IPostEffectMeshXRayGrid
        ?? throw new InvalidOperationException("X-ray grid render core is not initialized.");

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PostEffectMeshXRayGridCore();

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    #region Properties

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get => PostEffectCore.EffectName;
        set => PostEffectCore.EffectName = value;
    }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => GridCore.Color;
        set => GridCore.Color = value;
    }

    /// <summary>
    ///     Gets or sets the grid density.
    /// </summary>
    /// <value>
    ///     The grid density.
    /// </value>
    public int GridDensity {
        get => GridCore.GridDensity;
        set => GridCore.GridDensity = value;
    }

    /// <summary>
    ///     Gets or sets the dimming factor.
    /// </summary>
    /// <value>
    ///     The dimming factor.
    /// </value>
    public float DimmingFactor {
        get => GridCore.DimmingFactor;
        set => GridCore.DimmingFactor = value;
    }

    /// <summary>
    ///     Gets or sets the blending factor for grid and original mesh color blending
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public float BlendingFactor {
        get => GridCore.BlendingFactor;
        set => GridCore.BlendingFactor = value;
    }

    /// <summary>
    ///     Gets or sets the name of the x ray drawing pass. This is the final pass to draw mesh and grid overlay onto render
    ///     target
    /// </summary>
    /// <value>
    ///     The name of the x ray drawing pass.
    /// </value>
    public string XRayDrawingPassName {
        get => GridCore.XRayDrawingPassName;
        set => GridCore.XRayDrawingPassName = value;
    }

    /// <summary>
    ///     Gets or sets whether the x-ray grid uses the scene depth buffer to remove visible parts.
    /// </summary>
    public bool UseDepthOcclusion {
        get => GridCore.UseDepthOcclusion;
        set => GridCore.UseDepthOcclusion = value;
    }

    #endregion
}
