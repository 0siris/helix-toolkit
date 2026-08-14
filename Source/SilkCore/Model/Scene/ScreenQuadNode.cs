/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class ScreenQuadNode : SceneNode {
    public ScreenQuadNode() {
        IsHitTestVisible = false;
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture {
        get => (RenderCore as DrawScreenQuadCore).Texture;
        set => (RenderCore as DrawScreenQuadCore).Texture = value;
    }

    /// <summary>
    ///     Gets or sets the sampler.
    /// </summary>
    /// <value>
    ///     The sampler.
    /// </value>
    public SamplerStateDescription Sampler {
        get => (RenderCore as DrawScreenQuadCore).SamplerDescription;
        set => (RenderCore as DrawScreenQuadCore).SamplerDescription = value;
    }

    public float Depth {
        get;
        set {
            if (SetAffectsRender(ref field, value)) {
                var core = RenderCore as DrawScreenQuadCore;
                core.ModelStruct.TopLeft.Z = core.ModelStruct.TopRight.Z =
                                                 core.ModelStruct.BottomLeft.Z =
                                                     core.ModelStruct.BottomRight.Z = value;
            }
        }
    } = 1f;

    protected override RenderCore OnCreateRenderCore() => new DrawScreenQuadCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.ScreenQuad];

    public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected sealed override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
