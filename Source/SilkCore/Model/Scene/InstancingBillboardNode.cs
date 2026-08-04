/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class InstancingBillboardNode : BillboardNode {
    /// <summary>
    ///     The instance parameter buffer
    /// </summary>
    protected IElementsBufferModel<BillboardInstanceParameter> instanceParamBuffer =
        new InstanceParamsBufferModel<BillboardInstanceParameter>(BillboardInstanceParameter.SizeInBytes);

    /// <summary>
    ///     Gets or sets the instance parameter array.
    /// </summary>
    /// <value>
    ///     The instance parameter array.
    /// </value>
    public IList<BillboardInstanceParameter> InstanceParamArray {
        get => instanceParamBuffer.Elements;
        set => instanceParamBuffer.Elements = value;
    }

    #region Overridable Methods

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        return new InstancingBillboardRenderCore { ParameterBuffer = instanceParamBuffer };
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
        return effectsManager[DefaultRenderTechniqueNames.BillboardInstancing];
    }

    protected override bool OnAttach(IEffectsManager effectsManager) {
        // --- attach
        if (!base.OnAttach(effectsManager)) return false;
        instanceParamBuffer.Initialize();
        return true;
    }

    /// <summary>
    ///     Used to override Detach
    /// </summary>
    protected override void OnDetach() {
        instanceParamBuffer.DisposeAndClear();
        base.OnDetach();
    }

    #endregion
}
