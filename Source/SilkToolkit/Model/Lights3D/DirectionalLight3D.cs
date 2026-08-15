// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectionalLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

public sealed class DirectionalLight3D : Light3D {
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register("Direction",
                                    typeof(Vector3D),
                                    typeof(Light3D),
                                    new PropertyMetadata(new Vector3D(),
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: DirectionalLightNode node })
                                                                 node.Direction = ((Vector3D)e.NewValue).ToVector3();
                                                         }));

    /// <summary>
    ///     Direction of the light.
    ///     It applies to Directional Light and to Spot Light,
    ///     for all other lights it is ignored.
    /// </summary>
    public Vector3D Direction {
        get => (Vector3D)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new DirectionalLightNode();

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is DirectionalLightNode node)
            node.Direction = Direction.ToVector3();
    }
}
