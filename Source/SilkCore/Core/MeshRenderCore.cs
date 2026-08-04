/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public class MeshRenderCore : GeometryRenderCore, IMeshRenderParams, IDynamicReflectable {
    protected ModelStruct ModelStruct = new() { World = Matrix.Identity };

    protected override bool CreateRasterState(RasterizerStateDescription description, bool force) {
        if (base.CreateRasterState(description, force)) {
            var wireframeDesc = description with {
                FillMode = FillMode.Wireframe,
                DepthBias = -100,
                SlopeScaledDepthBias = 2f,
                DepthBiasClamp = -0.00008f
            };

            var newState = EffectTechnique.EffectsManager.StateManager.Register(wireframeDesc);
            RasterStateWireframe = newState;
            return true;
        }

        return false;
    }

    protected override void OnDetach() {
        RasterStateWireframe = null;
        base.OnDetach();
    }

    protected override bool OnUpdateCanRenderFlag() 
        => base.OnUpdateCanRenderFlag() && MaterialVariables != EmptyMaterialVariable.EmptyVariable;

    protected virtual void OnUpdatePerModelStruct(RenderContext context) {
        ModelStruct.World = ModelMatrix;
        ModelStruct.HasInstances = InstanceBuffer.HasElements ? 1 : 0;
        ModelStruct.Batched = Batched ? 1 : 0;
    }

    protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) {
        var pass = MaterialVariables.GetPass(RenderType, context);
        if (pass.IsNULL)
            return;
        
        OnUpdatePerModelStruct(context);
        if (!MaterialVariables.UpdateMaterialStruct(deviceContext, ref ModelStruct))
            return;
        
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, DefaultStateBinding);
        
        if (!MaterialVariables.BindMaterialResources(context, deviceContext, pass))
            return;

        DynamicReflector?.BindCubeMap(deviceContext);
        MaterialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
        DynamicReflector?.UnBindCubeMap(deviceContext);

        if (RenderWireframe) {
            pass = MaterialVariables.GetWireframePass(RenderType, context);
            if (pass.IsNULL) 
                return;
            
            pass.BindShader(deviceContext, false);
            pass.BindStates(deviceContext, DefaultStateBinding);
            deviceContext.SetRasterState(RasterStateWireframe);
            MaterialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
        }
    }

    protected override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) {
        if (!MaterialVariables.UpdateMaterialStruct(deviceContext, ref ModelStruct)) 
            return;
        
        MaterialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
    }

    protected override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) {
        var pass = MaterialVariables.GetShadowPass(RenderType, context);
        if (pass.IsNULL) 
            return;
        
        var v = new SimpleMeshStruct {
            World = ModelMatrix,
            HasInstances = InstanceBuffer.HasElements ? 1 : 0
        };
        if (!MaterialVariables.UpdateNonMaterialStruct(deviceContext, ref v))
            return;
        
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, ShadowStateBinding);
        MaterialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
    }

    protected override void OnRenderDepth(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass? customPass
    ) {
        var pass = customPass ?? MaterialVariables.GetDepthPass(RenderType, context);
        if (pass.IsNULL)
            return;
        
        var v = new SimpleMeshStruct {
            World = ModelMatrix,
            HasInstances = InstanceBuffer.HasElements ? 1 : 0
        };
        
        if (!MaterialVariables.UpdateNonMaterialStruct(deviceContext, ref v))
            return;
        
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, ShadowStateBinding);
        MaterialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
    }

#region Variables

    /// <summary>
    ///     Gets the raster state wireframe.
    /// </summary>
    /// <value>
    ///     The raster state wireframe.
    /// </value>
    protected RasterizerStateProxy? RasterStateWireframe {
        get;
        private set {
            if(field != value)
                field?.Dispose();
            field = value;
        }
    }

#endregion

#region Properties

    /// <summary>
    /// </summary>
    public bool InvertNormal {
        get => ModelStruct.InvertNormal == 1;
        set => SetAffectsRender(ref ModelStruct.InvertNormal, value ? 1 : 0);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render wireframe].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderWireframe {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the color of the wireframe.
    /// </summary>
    /// <value>
    ///     The color of the wireframe.
    /// </value>
    public Color4 WireframeColor {
        get => ModelStruct.WireframeColor;
        set => SetAffectsRender(ref ModelStruct.WireframeColor, value);
    }


    /// <summary>
    ///     Gets or sets the dynamic reflector.
    /// </summary>
    /// <value>
    ///     The dynamic reflector.
    /// </value>
    public IDynamicReflector? DynamicReflector { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="MeshRenderCore" /> is batched.
    /// </summary>
    /// <value>
    ///     <c>true</c> if batched; otherwise, <c>false</c>.
    /// </value>
    public bool Batched { get; set; }

    /// <summary>
    ///     Used to wrap all material resources
    /// </summary>
    [AllowNull]
    public MaterialVariable MaterialVariables {
        get;
        set {
            SetAffectsCanRenderFlag(ref field, value ?? EmptyMaterialVariable.EmptyVariable);
        }
    } = EmptyMaterialVariable.EmptyVariable;

#endregion
}