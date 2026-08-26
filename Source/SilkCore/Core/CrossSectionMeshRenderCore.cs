/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public class CrossSectionMeshRenderCore : MeshRenderCore, ICrossSectionRenderParams {

    public CrossSectionMeshRenderCore() {
        clipParamCb = AddComponent(new ConstantBufferComponent(
                                       new ConstantBufferDescription(
                                           DefaultBufferNames.ClipParamsCb,
                                           ClipPlaneStruct.SizeInBytes)));
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        if (!base.OnAttach(technique))
            return false;
        
        needsAssignVariables.Invalidate();
        drawBackfacePass = technique[DefaultPassNames.Backface];
        drawScreenQuadPass = technique[DefaultPassNames.ScreenQuad];
        return true;

    }

    protected override void OnDetach() {
        BackfaceRasterState = null;
        base.OnDetach();
    }

    protected override bool CreateRasterState(RasterizerStateDescription description, bool force) {
        if (!base.CreateRasterState(description, force)) 
            return false;

        var desc = new RasterizerStateDescription {
            FillMode = FillMode.Solid,
            CullMode = CullMode.Front,
            DepthBias = description.DepthBias,
            DepthBiasClamp = description.DepthBiasClamp,
            SlopeScaledDepthBias = description.SlopeScaledDepthBias,
            IsDepthClipEnabled = description.IsDepthClipEnabled,
            IsFrontCounterClockwise = description.IsFrontCounterClockwise,
            IsMultisampleEnabled = false,
            IsScissorEnabled = false
        };
        

        if (EffectTechnique is not { } technique)
            return false;
        BackfaceRasterState = technique.EffectsManager.StateManager.Register(desc);
        
        return true;
    }

    protected override void OnRender(RenderContext renderContext, DeviceContextProxy deviceContext) {
        needsAssignVariables.TryExecute(() => {
            clipParamCb.WriteValueByName(ClipPlaneStruct.CuttingOperationStr, (int)cuttingOperation);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossSectionColorStr, sectionColor);
            clipParamCb.WriteValueByName(ClipPlaneStruct.EnableCrossPlaneStr, planeEnabled);
            clipParamCb.WriteValueByName(ClipPlaneStruct.EnableCrossPlane5To8Str, plane5To8Enabled);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane1ParamsStr, plane1Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane2ParamsStr, plane2Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane3ParamsStr, plane3Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane4ParamsStr, plane4Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane5ParamsStr, plane5Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane6ParamsStr, plane6Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane7ParamsStr, plane7Params);
            clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane8ParamsStr, plane8Params);
        });
        

        clipParamCb.Upload(deviceContext);
        base.OnRender(renderContext, deviceContext);
        
    }

#region Shader Variables

    private ShaderPass drawBackfacePass = ShaderPass.NullPass;
    private ShaderPass drawScreenQuadPass = ShaderPass.NullPass;

    /// <summary>
    ///     Used to draw back faced triangles onto stencil buffer
    /// </summary>
    private RasterizerStateProxy? BackfaceRasterState { get;
        set {
            if(field!=value)
                field?.Dispose();
            field = value;
        } }

    private readonly ConstantBufferComponent clipParamCb;

    //private bool needsAssignVariables = true;   
    private readonly DirtyGate needsAssignVariables = new();


#endregion

#region Properties

    private CuttingOperation cuttingOperation = CuttingOperation.Intersect;

    /// <summary>
    ///     Gets or sets the cutting operation.
    /// </summary>
    /// <value>
    ///     The cutting operation.
    /// </value>
    public CuttingOperation CuttingOperation {
        get => cuttingOperation;
        set {
            if (SetAffectsRender(ref cuttingOperation, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CuttingOperationStr, (int)value);
        }
    }

    private Color4 sectionColor = Color.Green;

    /// <summary>
    ///     Defines the sectionColor
    /// </summary>
    public Color4 SectionColor {
        get => sectionColor;
        set {
            if (SetAffectsRender(ref sectionColor, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossSectionColorStr, value);
        }
    }

    private Bool4 planeEnabled;

    public Bool4 PlaneEnabled {
        get => planeEnabled;
        set {
            if (SetAffectsRender(ref planeEnabled, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.EnableCrossPlaneStr, value);
        }
    }

    private Bool4 plane5To8Enabled;

    public Bool4 Plane5To8Enabled {
        get => plane5To8Enabled;
        set {
            if (SetAffectsRender(ref plane5To8Enabled, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.EnableCrossPlane5To8Str, value);
        }
    }

    private Vector4 plane1Params;

    /// <summary>
    ///     Defines the plane 1(Normal + d)
    /// </summary>
    public Vector4 Plane1Params {
        get => plane1Params;
        set {
            if (SetAffectsRender(ref plane1Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane1ParamsStr, value);
        }
    }

    private Vector4 plane2Params;

    /// <summary>
    ///     Defines the plane 2(Normal + d)
    /// </summary>
    public Vector4 Plane2Params {
        get => plane2Params;
        set {
            if (SetAffectsRender(ref plane2Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane2ParamsStr, value);
        }
    }

    private Vector4 plane3Params;

    /// <summary>
    ///     Defines the plane 3(Normal + d)
    /// </summary>
    public Vector4 Plane3Params {
        get => plane3Params;
        set {
            if (SetAffectsRender(ref plane3Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane3ParamsStr, value);
        }
    }

    private Vector4 plane4Params;

    /// <summary>
    ///     Defines the plane 4(Normal + d)
    /// </summary>
    public Vector4 Plane4Params {
        get => plane4Params;
        set {
            if (SetAffectsRender(ref plane4Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane4ParamsStr, value);
        }
    }

    private Vector4 plane5Params;

    /// <summary>
    ///     Defines the plane 5(Normal + d)
    /// </summary>
    public Vector4 Plane5Params {
        get => plane5Params;
        set {
            if (SetAffectsRender(ref plane5Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane5ParamsStr, value);
        }
    }

    private Vector4 plane6Params;

    /// <summary>
    ///     Defines the plane 6(Normal + d)
    /// </summary>
    public Vector4 Plane6Params {
        get => plane6Params;
        set {
            if (SetAffectsRender(ref plane6Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane6ParamsStr, value);
        }
    }

    private Vector4 plane7Params;

    /// <summary>
    ///     Defines the plane 7(Normal + d)
    /// </summary>
    public Vector4 Plane7Params {
        get => plane7Params;
        set {
            if (SetAffectsRender(ref plane7Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane7ParamsStr, value);
        }
    }

    private Vector4 plane8Params;

    /// <summary>
    ///     Defines the plane 8(Normal + d)
    /// </summary>
    public Vector4 Plane8Params {
        get => plane8Params;
        set {
            if (SetAffectsRender(ref plane8Params, value))
                clipParamCb.WriteValueByName(ClipPlaneStruct.CrossPlane8ParamsStr, value);
        }
    }

#endregion
}
