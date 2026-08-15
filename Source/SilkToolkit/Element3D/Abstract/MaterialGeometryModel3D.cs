/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D.Abstract;

/// <summary>
/// </summary>
/// <seealso cref="GeometryModel3D" />
public abstract class MaterialGeometryModel3D : GeometryModel3D {
    /// <summary>
    ///     Assigns the default values to scene node.
    /// </summary>
    /// <param name="node">The node.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        if (node is MaterialGeometryNode n) n.Material = Material;
        base.AssignDefaultValuesToSceneNode(node);
    }

#region Dependency Properties

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty MaterialProperty =
        DependencyProperty.Register("Material",
                                    typeof(Material.Material),
                                    typeof(MaterialGeometryModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: MaterialGeometryNode node })
                                                                 node.Material = e.NewValue as Material.Material;
                                                         }));

    /// <summary>
    ///     Specifiy if model material is transparent.
    ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
    ///     are preserved.
    /// </summary>
    public static readonly DependencyProperty IsTransparentProperty =
        DependencyProperty.Register("IsTransparent",
                                    typeof(bool),
                                    typeof(MaterialGeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: MaterialGeometryNode node })
                                                                 node.IsTransparent = (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public Material.Material Material {
        get => (Material.Material) GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    /// <summary>
    ///     Specifiy if model material is transparent.
    ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
    ///     are preserved.
    /// </summary>
    public bool IsTransparent {
        get => (bool) GetValue(IsTransparentProperty);
        set => SetValue(IsTransparentProperty, value);
    }

#endregion
}
