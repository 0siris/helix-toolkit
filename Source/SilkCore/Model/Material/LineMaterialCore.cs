/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
[DataContract]
public class LineMaterialCore : MaterialCore, ILineRenderParams {
    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    ) {
        return new LineMaterialVariable(manager, technique, this);
    }

    #region Properties

    /// <summary>
    /// </summary>
    public float Thickness {
        get;
        set => Set(ref field, value);
    } = 0.5f;

    /// <summary>
    /// </summary>
    public float Smoothness {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Final Line Color = LineColor * PerVertexLineColor
    /// </summary>
    public Color4 LineColor {
        get;
        set => Set(ref field, value);
    } = Color.Blue;

    public bool EnableDistanceFading {
        get;
        set => Set(ref field, value);
    }

    public float FadingNearDistance {
        get;
        set => Set(ref field, value);
    } = 100;

    public float FadingFarDistance {
        get;
        set => Set(ref field, value);
    }

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

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the texture scale.
    /// </summary>
    /// <value>
    ///     The texture scale.
    /// </value>
    public float TextureScale {
        get;
        set => Set(ref field, value);
    } = 1;

    /// <summary>
    ///     Gets or sets the alpha threshold. Pixel with color alpha value smaller than threshold will be set to transparent.
    ///     <para>This is used to avoid sampler color interpolation effects.</para>
    /// </summary>
    /// <value>
    ///     The alpha threshold
    /// </value>
    public float AlphaThreshold {
        get;
        set => Set(ref field, value);
    } = 0.2f;


    /// <summary>
    ///     Billboard texture sampler description
    /// </summary>
    public SamplerStateDescription SamplerDescription {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LineSamplerUWrapVClamp;

#endregion
}
