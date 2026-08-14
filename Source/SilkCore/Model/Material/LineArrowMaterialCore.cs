/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Model;
public class LineArrowHeadMaterialCore : LineMaterialCore {
    /// <summary>
    ///     Gets or sets the size of the arrow.
    /// </summary>
    /// <value>
    ///     The size of the arrow.
    /// </value>
    public float ArrowSize {
        get;
        set => Set(ref field, value);
    } = 0.1f;

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new LineArrowMaterialVariable(manager,
            manager.GetTechnique(DefaultRenderTechniqueNames.LinesArrowHead),
            this);
}

public class LineArrowHeadTailMaterialCore : LineArrowHeadMaterialCore {
    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new LineArrowMaterialVariable(manager,
            manager.GetTechnique(
                DefaultRenderTechniqueNames.LinesArrowHeadTail),
            this);
}
