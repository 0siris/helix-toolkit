/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;
using PlatformColor = System.Windows.Media.Color;
using PlatformColors = System.Windows.Media.Colors;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public class MeshGeometryModel3D : MaterialGeometryModel3D {
    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() {
        return new MeshNode();
    }

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="node">The node.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        var c = node as MeshNode;
        c.InvertNormal = InvertNormal;
        c.WireframeColor = WireframeColor.ToColor4();
        c.RenderWireframe = RenderWireframe;
        base.AssignDefaultValuesToSceneNode(node);
    }

    #region Dependency Properties

    /// <summary>
    ///     The front counter clockwise property
    /// </summary>
    public static readonly DependencyProperty FrontCounterClockwiseProperty = DependencyProperty.Register(
        "FrontCounterClockwise",
        typeof(bool),
        typeof(MeshGeometryModel3D),
        new PropertyMetadata(true,
                             (d, e) => { ((d as Element3DCore).SceneNode as MeshNode).FrontCCW = (bool)e.NewValue; }));

    /// <summary>
    ///     The cull mode property
    /// </summary>
    public static readonly DependencyProperty CullModeProperty = DependencyProperty.Register("CullMode",
        typeof(CullMode),
        typeof(MeshGeometryModel3D),
        new PropertyMetadata(CullMode.None,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshNode).CullMode = (CullMode)e.NewValue;
                             }));

    /// <summary>
    ///     The invert normal property
    /// </summary>
    public static readonly DependencyProperty InvertNormalProperty = DependencyProperty.Register("InvertNormal",
        typeof(bool),
        typeof(MeshGeometryModel3D),
        new PropertyMetadata(false,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshNode).InvertNormal = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The render wireframe property
    /// </summary>
    public static readonly DependencyProperty RenderWireframeProperty =
        DependencyProperty.Register("RenderWireframe",
                                    typeof(bool),
                                    typeof(MeshGeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as MeshNode)
                                                                 .RenderWireframe = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The wireframe color property
    /// </summary>
    public static readonly DependencyProperty WireframeColorProperty =
        DependencyProperty.Register("WireframeColor",
                                    typeof(PlatformColor),
                                    typeof(MeshGeometryModel3D),
                                    new PropertyMetadata(PlatformColors.SkyBlue,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as MeshNode)
                                                                 .WireframeColor =
                                                                 ((PlatformColor)e.NewValue).ToColor4();
                                                         }));

    /// <summary>
    ///     Gets or sets a value indicating whether [render overlapping wireframe].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render wireframe]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderWireframe {
        get => (bool)GetValue(RenderWireframeProperty);
        set => SetValue(RenderWireframeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the color of the wireframe.
    /// </summary>
    /// <value>
    ///     The color of the wireframe.
    /// </value>
    public PlatformColor WireframeColor {
        get => (PlatformColor)GetValue(WireframeColorProperty);
        set => SetValue(WireframeColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [front counter clockwise].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [front counter clockwise]; otherwise, <c>false</c>.
    /// </value>
    public bool FrontCounterClockwise {
        get => (bool)GetValue(FrontCounterClockwiseProperty);
        set => SetValue(FrontCounterClockwiseProperty, value);
    }

    /// <summary>
    ///     Gets or sets the cull mode.
    /// </summary>
    /// <value>
    ///     The cull mode.
    /// </value>
    public CullMode CullMode {
        get => (CullMode)GetValue(CullModeProperty);
        set => SetValue(CullModeProperty, value);
    }

    /// <summary>
    ///     Invert the surface normal during rendering
    /// </summary>
    public bool InvertNormal {
        get => (bool)GetValue(InvertNormalProperty);
        set => SetValue(InvertNormalProperty, value);
    }

    #endregion
}
