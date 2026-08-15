/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class InstancingBillboardNode : BillboardNode {
    /// <summary>
    ///     The instance parameter buffer
    /// </summary>
    protected IElementsBufferModel<BillboardInstanceParameter> InstanceParamBuffer =
        new InstanceParamsBufferModel<BillboardInstanceParameter>(BillboardInstanceParameter.SizeInBytes);

    /// <summary>
    ///     Gets or sets the instance parameter array.
    /// </summary>
    /// <value>
    ///     The instance parameter array.
    /// </value>
    public IList<BillboardInstanceParameter>? InstanceParamArray {
        get => InstanceParamBuffer.Elements;
        set => InstanceParamBuffer.Elements = value;
    }

    #region Overridable Methods

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new InstancingBillboardRenderCore { ParameterBuffer = InstanceParamBuffer };

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[DefaultRenderTechniqueNames.BillboardInstancing];

    protected override bool OnAttach(IEffectsManager effectsManager) {
        // --- attach
        if (!base.OnAttach(effectsManager)) return false;
        InstanceParamBuffer.Initialize();
        return true;
    }

    /// <summary>
    ///     Used to override Detach
    /// </summary>
    protected override void OnDetach() {
        InstanceParamBuffer.DisposeAndClear();
        base.OnDetach();
    }

    #endregion
}
