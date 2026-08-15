/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using System.Diagnostics.CodeAnalysis;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class PointLineRenderCore : GeometryRenderCore, IMaterialRenderParams {
    private MaterialVariable materialVariables = EmptyMaterialVariable.EmptyVariable;

    protected PointLineModelStruct ModelStruct;

    /// <summary>
    ///     Used to wrap all material resources
    /// </summary>
    [AllowNull]
    public MaterialVariable MaterialVariables {
        get => materialVariables;
        set {
            value ??= EmptyMaterialVariable.EmptyVariable;
            SetAffectsCanRenderFlag(ref materialVariables, value);
        }
    }

    protected virtual void OnUpdatePerModelStruct() {
        ModelStruct.World = ModelMatrix;
        ModelStruct.HasInstances = InstanceBuffer.HasElements
            ? 1
            : 0;
    }

    protected override bool OnUpdateCanRenderFlag()
        => base.OnUpdateCanRenderFlag() && materialVariables != EmptyMaterialVariable.EmptyVariable;

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) {
        var shaderPass = materialVariables.GetPass(RenderType, context);
        if (shaderPass.IsNull) return;
        OnUpdatePerModelStruct();
        if (!materialVariables.UpdateMaterialStruct(deviceContext, ref ModelStruct)) return;
        if (materialVariables.BindMaterialResources(context, deviceContext, shaderPass)) {
            shaderPass.BindShader(deviceContext);
            shaderPass.BindStates(deviceContext, DefaultStateBinding);
            materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
        }
    }

    protected sealed override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) {
        if (!materialVariables.UpdateMaterialStruct(deviceContext, ref ModelStruct)) return;
        materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
    }

    protected sealed override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) {
        var pass = materialVariables.GetShadowPass(RenderType, context);
        if (pass.IsNull) return;
        var v = new SimpleMeshStruct {
            World = ModelMatrix,
            HasInstances = InstanceBuffer.HasElements
                ? 1
                : 0
        };
        if (!materialVariables.UpdateNonMaterialStruct(deviceContext, ref v)) return;
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, ShadowStateBinding);
        materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
    }

    protected sealed override void OnRenderDepth(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass? customPass
    ) {
        var pass = customPass ?? materialVariables.GetDepthPass(RenderType, context);
        if (pass.IsNull) return;
        OnUpdatePerModelStruct();
        if (!materialVariables.UpdateMaterialStruct(deviceContext, ref ModelStruct)) return;
        if (materialVariables.BindMaterialResources(context, deviceContext, pass)) {
            pass.BindShader(deviceContext);
            pass.BindStates(deviceContext, DefaultStateBinding);
            materialVariables.Draw(deviceContext, GeometryBuffer, InstanceBuffer.ElementCount);
        }
    }
}
