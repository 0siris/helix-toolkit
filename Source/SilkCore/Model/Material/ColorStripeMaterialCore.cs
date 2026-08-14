/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
[DataContract]
public class ColorStripeMaterialCore : MaterialCore {
    /// <summary>
    ///     Gets or sets the color of the diffuse.
    /// </summary>
    /// <value>
    ///     The color of the diffuse.
    /// </value>
    public Color4 DiffuseColor {
        get;
        set => Set(ref field, value);
    } = Color.White;

    /// <summary>
    ///     Gets or sets the color stripe x. Use texture coordinate X for sampling
    /// </summary>
    /// <value>
    ///     The color stripe x.
    /// </value>
    public IList<Color4> ColorStripeX {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the color stripe y. Use texture coordinate Y for sampling
    /// </summary>
    /// <value>
    ///     The color stripe y.
    /// </value>
    public IList<Color4> ColorStripeY {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [color stripe x enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [color stripe x enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ColorStripeXEnabled {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [color stripe y enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [color stripe y enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ColorStripeYEnabled {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets the DiffuseMapSampler.
    /// </summary>
    /// <value>
    ///     DiffuseMapSampler
    /// </value>
    public SamplerStateDescription ColorStripeSampler {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LinearSamplerClampAni1;

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new ColorStripeMaterialVariables(manager, technique, this);
}
