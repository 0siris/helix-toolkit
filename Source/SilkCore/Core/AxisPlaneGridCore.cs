using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

public class AxisPlaneGridCore : RenderCore {
    private ShaderPass? defaultShaderPass;

    private PlaneGridModelStruct modelStruct;
    private int samplerSlot;
    private int shadowMapSlot;

    private Vector3 upDirection = Vector3.UnitY;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AxisPlaneGridCore" /> class.
    /// </summary>
    public AxisPlaneGridCore() : base(RenderType.Particle) {
        modelStruct = new PlaneGridModelStruct {
            World = Matrix.Identity,
            Axis = 1
        };

        GridSpacing = 10;
        GridThickness = 0.05f;
        FadingFactor = 0.2f;
        PlaneColor = Color.Gray;
        GridColor = Color.DarkGray;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [automatic spacing].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [automatic spacing]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoSpacing {
        get;
        set {
            if (SetAffectsRender(ref field, value) && !value)
                modelStruct.GridSpacing = GridSpacing;
        }
    } = true;

    /// <summary>
    ///     Gets or sets the automatic spacing rate. Default is 5 for perspective camera. If using orthographic camera,
    ///     increase the rate value to for example > 15.
    /// </summary>
    /// <value>
    ///     The automatic spacing rate.
    /// </value>
    public float AutoSpacingRate {
        get;
        set => SetAffectsRender(ref field, value);
    } = 5;

    /// <summary>
    ///     Gets the acutal spacing.
    /// </summary>
    /// <value>
    ///     The acutal spacing.
    /// </value>
    public float AcutalSpacing => modelStruct.GridSpacing;

    /// <summary>
    ///     Gets or sets the grid spacing.
    /// </summary>
    /// <value>
    ///     The grid spacing.
    /// </value>
    public float GridSpacing {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                modelStruct.GridSpacing = value;
        }
    }

    /// <summary>
    ///     Gets or sets the grid thickness.
    /// </summary>
    /// <value>
    ///     The grid thickness.
    /// </value>
    public float GridThickness {
        get => modelStruct.GridThickenss;
        set => SetAffectsRender(ref modelStruct.GridThickenss, value);
    }

    /// <summary>
    ///     Gets or sets the fading factor.
    /// </summary>
    /// <value>
    ///     The fading factor.
    /// </value>
    public float FadingFactor {
        get => modelStruct.FadingFactor;
        set => SetAffectsRender(ref modelStruct.FadingFactor, value);
    }

    /// <summary>
    ///     Gets or sets the color of the plane.
    /// </summary>
    /// <value>
    ///     The color of the plane.
    /// </value>
    public Color4 PlaneColor {
        get => modelStruct.PlaneColor.ToColor4();
        set => SetAffectsRender(ref modelStruct.PlaneColor, value);
    }

    /// <summary>
    ///     Gets or sets the color of the grid.
    /// </summary>
    /// <value>
    ///     The color of the grid.
    /// </value>
    public Color4 GridColor {
        get => modelStruct.GridColor.ToColor4();
        set => SetAffectsRender(ref modelStruct.GridColor, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render shadow map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render shadow map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderShadowMap {
        get => modelStruct.HasShadowMap;
        set => SetAffectsRender(ref modelStruct.HasShadowMap, value);
    }

    /// <summary>
    ///     Gets or sets Up Axis. Default is Y
    /// </summary>
    /// <value>
    ///     The plane.
    /// </value>
    public Axis UpAxis {
        get;
        set {
            if (SetAffectsRender(ref field, value)) {
                modelStruct.Axis = (int)value;
                upDirection = value switch {
                    Axis.X => Vector3.UnitX,
                    Axis.Y => Vector3.UnitY,
                    Axis.Z => Vector3.UnitZ,
                    _ => upDirection
                };
            }
        }
    } = Axis.Y;

    /// <summary>
    ///     Gets or sets the axis plane offset.
    /// </summary>
    /// <value>
    ///     The offset.
    /// </value>
    public float Offset {
        get => modelStruct.PlaneD;
        set => SetAffectsRender(ref modelStruct.PlaneD, value);
    }

    /// <summary>
    ///     Gets or sets the grid pattern.
    /// </summary>
    /// <value>
    ///     The grid pattern.
    /// </value>
    public GridPattern GridPattern {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                modelStruct.Type = (int)value;
        }
    } = GridPattern.Tile;

}
