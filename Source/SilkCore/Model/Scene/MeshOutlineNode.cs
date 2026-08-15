/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class MeshOutlineNode : MeshNode {
    private IMeshOutlineParams OutlineCore => RenderCore as IMeshOutlineParams
        ?? throw new InvalidOperationException("Mesh-outline render core is not initialized.");

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new MeshOutlineRenderCore();

    #region Properties

    /// <summary>
    ///     Gets or sets a value indicating whether [enable outline].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable outline]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableOutline {
        get => OutlineCore.OutlineEnabled;
        set => OutlineCore.OutlineEnabled = value;
    }

    /// <summary>
    ///     Gets or sets the color of the outline.
    /// </summary>
    /// <value>
    ///     The color of the outline.
    /// </value>
    public Color4 OutlineColor {
        get => OutlineCore.Color;
        set => OutlineCore.Color = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is draw geometry.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is draw geometry; otherwise, <c>false</c>.
    /// </value>
    public bool IsDrawGeometry {
        get => OutlineCore.DrawMesh;
        set => OutlineCore.DrawMesh = value;
    }

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    public float OutlineFadingFactor {
        get => OutlineCore.OutlineFadingFactor;
        set => OutlineCore.OutlineFadingFactor = value;
    }

    #endregion
}
