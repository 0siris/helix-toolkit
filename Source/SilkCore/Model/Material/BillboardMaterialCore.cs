/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Model.Material;
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
}
