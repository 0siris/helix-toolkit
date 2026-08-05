/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public class AxisPlaneGridCore : RenderCore {
    private readonly ConstantBufferComponent modelCb;
    private bool autoSpacing = true;

    private float autoSpacingChangeRate = 5;
    private ShaderPass? defaultShaderPass;

    private PlaneGridModelStruct modelStruct;
    private int samplerSlot;
    private int shadowMapSlot;

    private SamplerStateProxy? ShadowSampler {
        get;
        set {
            if (value != field) 
                field?.Dispose();
            field = value;
        }
    }

    private Vector3 upDirection = Vector3.UnitY;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AxisPlaneGridCore" /> class.
    /// </summary>
    public AxisPlaneGridCore() : base(RenderType.Particle) {
        modelCb = AddComponent(new ConstantBufferComponent(new ConstantBufferDescription(
                                                               DefaultBufferNames.PlaneGridModelCB,
                                                               PlaneGridModelStruct.SizeInBytes)));
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
        get => autoSpacing;
        set {
            if (SetAffectsRender(ref autoSpacing, value) && !value) 
                modelStruct.GridSpacing = GridSpacing;
        }
    }

    /// <summary>
    ///     Gets or sets the automatic spacing rate. Default is 5 for perspective camera. If using orthographic camera,
    ///     increase the rate value to for example > 15.
    /// </summary>
    /// <value>
    ///     The automatic spacing rate.
    /// </value>
    public float AutoSpacingRate {
        get => autoSpacingChangeRate;
        set => SetAffectsRender(ref autoSpacingChangeRate, value);
    }

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
                modelStruct.Axis = (int) value;
                upDirection = value switch {
                    Axis.X => Vector3.UnitX,
                    Axis.Y => Vector3.UnitY,
                    Axis.Z => Vector3.UnitZ,
                    _      => upDirection
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
                modelStruct.Type = (int) value;
        }
    } = GridPattern.Tile;

    [MemberNotNull(nameof(ShadowSampler))]
    [MemberNotNull(nameof(defaultShaderPass))]
    protected override bool OnAttach(IRenderTechnique technique) {
        defaultShaderPass = technique[DefaultPassNames.Default];
        samplerSlot = defaultShaderPass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.ShadowMapSampler);
        shadowMapSlot = defaultShaderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.ShadowMapTB);
        ShadowSampler = technique.EffectsManager.StateManager.Register(DefaultSamplers.ShadowSampler);
        return true;
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        OnUpdatePerModelStruct(context);
        modelCb.Upload(deviceContext, ref modelStruct);
        defaultShaderPass.BindShader(deviceContext);
        defaultShaderPass.BindStates(deviceContext,
                                     StateType.BlendState | StateType.DepthStencilState |
                                     StateType.RasterState);
        if (RenderShadowMap && context.SharedResource.ShadowView != null) {
            defaultShaderPass.PixelShader.BindTexture(deviceContext, shadowMapSlot, context.SharedResource.ShadowView);
            defaultShaderPass.PixelShader.BindSampler(deviceContext, samplerSlot, ShadowSampler);
        }

        deviceContext.Draw(4, 0);
    }

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.World = ModelMatrix;
        if (autoSpacing) {
            //Disable auto spacing if view angle larger than 60 degree of plane normal
            var lookDir = SilkMath.Normalize(context.Camera.LookDirection);
            var angle = Math.Acos(Math.Abs(SilkMath.Dot(upDirection, lookDir)));
            if (angle > Math.PI / 3) 
                return;
            
            var r = new Ray(context.Camera.Position, SilkMath.Normalize(context.Camera.LookDirection));
            var plane = new Plane(upDirection, modelStruct.PlaneD);
            if (plane.Intersects(ref r, out var l)) {
                l /= autoSpacingChangeRate;
                var n = 1;
                while (n < 1e6) {
                    if (n > l) {
                        n /= 10;
                        break;
                    }

                    n *= 10;
                }

                modelStruct.GridSpacing = n;
            }
        }
    }

    protected override void OnDetach() => ShadowSampler = null;
}