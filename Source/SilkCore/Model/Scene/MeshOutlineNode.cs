/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class MeshOutlineNode : MeshNode {
    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        return new MeshOutlineRenderCore();
    }

    #region Properties

    /// <summary>
    ///     Gets or sets a value indicating whether [enable outline].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable outline]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableOutline {
        get => (RenderCore as IMeshOutlineParams).OutlineEnabled;
        set => (RenderCore as IMeshOutlineParams).OutlineEnabled = value;
    }

    /// <summary>
    ///     Gets or sets the color of the outline.
    /// </summary>
    /// <value>
    ///     The color of the outline.
    /// </value>
    public Color4 OutlineColor {
        get => (RenderCore as IMeshOutlineParams).Color;
        set => (RenderCore as IMeshOutlineParams).Color = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is draw geometry.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is draw geometry; otherwise, <c>false</c>.
    /// </value>
    public bool IsDrawGeometry {
        get => (RenderCore as IMeshOutlineParams).DrawMesh;
        set => (RenderCore as IMeshOutlineParams).DrawMesh = value;
    }

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    public float OutlineFadingFactor {
        get => (RenderCore as IMeshOutlineParams).OutlineFadingFactor;
        set => (RenderCore as IMeshOutlineParams).OutlineFadingFactor = value;
    }

    #endregion
}
