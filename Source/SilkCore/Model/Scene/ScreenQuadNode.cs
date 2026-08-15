/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class ScreenQuadNode : SceneNode {
    private DrawScreenQuadCore Core => RenderCore as DrawScreenQuadCore
        ?? throw new InvalidOperationException("Render core is not a screen quad core.");

    public ScreenQuadNode() {
        IsHitTestVisible = false;
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel? Texture {
        get => Core.Texture;
        set => Core.Texture = value;
    }

    /// <summary>
    ///     Gets or sets the sampler.
    /// </summary>
    /// <value>
    ///     The sampler.
    /// </value>
    public SamplerStateDescription Sampler {
        get => Core.SamplerDescription;
        set => Core.SamplerDescription = value;
    }

    public float Depth {
        get;
        set {
            if (SetAffectsRender(ref field, value)) {
                var core = Core;
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
