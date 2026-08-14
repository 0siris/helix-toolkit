/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
public sealed class BillboardMaterialCore : MaterialCore, IBillboardRenderParams {
    /// <summary>
    ///     Gets or sets a value indicating whether [fixed size].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [fixed size]; otherwise, <c>false</c>.
    /// </value>
    public bool FixedSize {
        get;
        set => Set(ref field, value);
    } = true;

    public BillboardType Type {
        get;
        set => Set(ref field, value);
    } = BillboardType.SingleText;

    /// <summary>
    ///     Billboard texture sampler description
    /// </summary>
    public SamplerStateDescription SamplerDescription {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LinearSamplerClampAni1;

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new BillboardMaterialVariable(manager, technique, this);
}
