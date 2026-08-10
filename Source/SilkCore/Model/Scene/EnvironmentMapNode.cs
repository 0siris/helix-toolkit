/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class EnvironmentMapNode : SceneNode {
    private readonly bool useSkyDome;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EnvironmentMapNode" /> class. Default is using SkyBox. To use SkyDome,
    ///     pass true into the constructor
    /// </summary>
    public EnvironmentMapNode() {
        RenderOrder = 1000;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EnvironmentMapNode" /> class. Default is using SkyBox. To use SkyDome,
    ///     pass true into the constructor
    /// </summary>
    /// <param name="useSkyDome">if set to <c>true</c> [use sky dome].</param>
    public EnvironmentMapNode(bool useSkyDome) {
        this.useSkyDome = useSkyDome;
        RenderOrder = 1000;
    }

    /// <summary>
    ///     Gets or sets the environment texture. Must be 3D cube texture
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture {
        get => (RenderCore as ISkyboxRenderParams).CubeTexture;
        set => (RenderCore as ISkyboxRenderParams).CubeTexture = value;
    }

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    public bool SkipRendering {
        get => (RenderCore as ISkyboxRenderParams).SkipRendering;
        set => (RenderCore as ISkyboxRenderParams).SkipRendering = value;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        if (useSkyDome) return new SkyDomeRenderCore();

        return new SkyBoxRenderCore();
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
        return effectsManager[DefaultRenderTechniqueNames.Skybox];
    }

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) {
        return false;
    }

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        return false;
    }

    protected override bool CanRender(RenderContext context) {
        if (!base.CanRender(context)) {
            context.SharedResource.EnvironementMap = null;
            return false;
        }

        return true;
    }
}
