/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.PostEffects;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;

/// <summary>
/// </summary>
public class NodePostEffectBorderHighlight : NodePostEffectMeshOutlineBlur {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NodePostEffectBorderHighlight" /> class.
    /// </summary>
    public NodePostEffectBorderHighlight() {
        EffectName = DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight;
    }

    /// <summary>
    ///     Gets or sets the draw mode.
    /// </summary>
    /// <value>
    ///     The draw mode.
    /// </value>
    public OutlineMode DrawMode {
        get => (RenderCore as PostEffectMeshOutlineBlurCore
                ?? throw new InvalidOperationException("Post-effect render core was not created.")).DrawMode;
        set => (RenderCore as PostEffectMeshOutlineBlurCore
                ?? throw new InvalidOperationException("Post-effect render core was not created.")).DrawMode = value;
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight];

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new PostEffectMeshOutlineBlurCore(false);
}
