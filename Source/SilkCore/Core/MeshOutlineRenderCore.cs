/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class MeshOutlineRenderCore : MeshRenderCore, IMeshOutlineParams {
    /// <summary>
    ///     Initializes a new instance of the <see cref="MeshOutlineRenderCore" /> class.
    /// </summary>
    public MeshOutlineRenderCore() => OutlineFadingFactor = 1.5f;

    /// <summary>
    ///     Called when [update per model structure].
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnUpdatePerModelStruct(RenderContext context) {
        base.OnUpdatePerModelStruct(context);
        ModelStruct.Params.Y = OutlineFadingFactor;
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
        set => SetAffectsRender(ref field, value);
    } = DefaultPassNames.MeshOutline;

    #endregion
}
