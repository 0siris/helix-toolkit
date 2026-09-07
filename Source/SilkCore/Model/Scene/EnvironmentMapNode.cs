/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class EnvironmentMapNode : SceneNode {
    private readonly bool useSkyDome;
    private ISkyboxRenderParams SkyboxRenderParams => RenderCore as ISkyboxRenderParams
        ?? throw new InvalidOperationException("Skybox render core is not initialized.");

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
    public TextureModel? Texture {
        get => SkyboxRenderParams.CubeTexture;
        set => SkyboxRenderParams.CubeTexture = value;
    }

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    public bool SkipRendering {
        get => SkyboxRenderParams.SkipRendering;
        set => SkyboxRenderParams.SkipRendering = value;
    }

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        if (useSkyDome) return new SkyDomeRenderCore();

        return new SkyBoxRenderCore();
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.Skybox];

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    protected override bool CanRender(RenderContext context) {
        if (!base.CanRender(context)) {
            return false;
        }

        return true;
    }
}
