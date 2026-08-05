/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class MeshOutlineRenderCore : MeshRenderCore, IMeshOutlineParams {
    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshOutlineRenderCore" /> class.
    /// </summary>
    public MeshOutlineRenderCore() => OutlineFadingFactor = 1.5f;

#region Variables

    /// <summary>
    /// </summary>
    protected ShaderPass OutlineShaderPass { get; private set; }

#endregion

    /// <summary>
    ///     Called when [attach].
    /// </summary>
    /// <param name="technique">The technique.</param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderTechnique technique) {
        OutlineShaderPass = technique[OutlinePassName];
        return base.OnAttach(technique);
    }

    /// <summary>
    ///     Called when [update per model structure].
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnUpdatePerModelStruct(RenderContext context) {
        base.OnUpdatePerModelStruct(context);
        ModelStruct.Params.Y = OutlineFadingFactor;
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) {
        if (DrawOutlineBeforeMesh) {
            OutlineShaderPass.BindShader(deviceContext);
            OutlineShaderPass.BindStates(deviceContext, DefaultStateBinding);
            DrawIndexed(deviceContext, GeometryBuffer.IndexBuffer, InstanceBuffer);
        }

        if (DrawMesh) base.OnRender(context, deviceContext);
        if (!DrawOutlineBeforeMesh) {
            OutlineShaderPass.BindShader(deviceContext);
            OutlineShaderPass.BindStates(deviceContext, DefaultStateBinding);
            DrawIndexed(deviceContext, GeometryBuffer.IndexBuffer, InstanceBuffer);
        }
    }

#region Properties

    /// <summary>
    ///     Outline color
    /// </summary>
    public Color4 Color {
        get => ModelStruct.Color.ToColor4();
        set => SetAffectsRender(ref ModelStruct.Color, value);
    }

    /// <summary>
    ///     Enable outline
    /// </summary>
    public bool OutlineEnabled {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Draw original mesh
    /// </summary>
    public bool DrawMesh {
        get;
        set => SetAffectsRender(ref field, value);
    } = true;

    /// <summary>
    ///     Draw outline order
    /// </summary>
    public bool DrawOutlineBeforeMesh {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Outline fading
    /// </summary>
    public float OutlineFadingFactor {
        get => ModelStruct.Params.Y;
        set => SetAffectsRender(ref ModelStruct.Params.Y, value);
    }

    /// <summary>
    ///     Gets or sets the name of the outline pass.
    /// </summary>
    /// <value>
    ///     The name of the outline pass.
    /// </value>
    public string OutlinePassName {
        get;
        set {
            if (SetAffectsRender(ref field, value) && IsAttached)
                OutlineShaderPass = EffectTechnique[value];
        }
    } = DefaultPassNames.MeshOutline;

#endregion
}