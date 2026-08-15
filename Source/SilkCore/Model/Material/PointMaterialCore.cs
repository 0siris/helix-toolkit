/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;

namespace HelixToolkit.SharpDX.Core.Model.Material;
public class PointMaterialCore : MaterialCore, IPointRenderParams {
    public bool EnableDistanceFading {
        get;
        set => Set(ref field, value);
    }

    public float FadingNearDistance {
        get;
        set => Set(ref field, value);
    }

    public float FadingFarDistance {
        get;
        set => Set(ref field, value);
    } = 100;

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
    ///     Gets or sets a value indicating whether [enable blending].
    ///     <para>
    ///         Once enabled, final color
    ///         = <see cref="BlendingFactor" /> * <see cref="PointColor" /> + (1 - <see cref="BlendingFactor" />) * Vertex
    ///         Color.
    ///     </para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable blending]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableColorBlending {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the blending factor.
    ///     <para>Used when <see cref="EnableColorBlending" /> = true.</para>
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public float BlendingFactor {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the width.
    /// </summary>
    /// <value>
    ///     The width.
    /// </value>
    public float Width {
        get;
        set => Set(ref field, value);
    } = 0.5f;

    /// <summary>
    ///     Gets or sets the height.
    /// </summary>
    /// <value>
    ///     The height.
    /// </value>
    public float Height {
        get;
        set => Set(ref field, value);
    } = 0.5f;

    /// <summary>
    ///     Gets or sets the figure.
    /// </summary>
    /// <value>
    ///     The figure.
    /// </value>
    public PointFigure Figure {
        get;
        set => Set(ref field, value);
    } = PointFigure.Rect;

    /// <summary>
    ///     Gets or sets the figure ratio.
    /// </summary>
    /// <value>
    ///     The figure ratio.
    /// </value>
    public float FigureRatio {
        get;
        set => Set(ref field, value);
    } = 0.25f;

    /// <summary>
    ///     Final Point Color = PointColor * PerVertexPointColor
    /// </summary>
    public Color4 PointColor {
        get;
        set => Set(ref field, value);
    } = Color.Black;

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new PointMaterialVariable(manager, technique, this);
}
