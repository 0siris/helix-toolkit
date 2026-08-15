// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Light3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Direction of the light.
//   It applies to Directional Light and to Spot Light,
//   for all other lights it is ignored.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;

namespace HelixToolkit.Wpf.SharpDX.Model.Lights3D;

using Media = System.Windows.Media;

public abstract class Light3D : Elements3D.AbstractElements3D.Element3D {
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Media.Color),
                                    typeof(Light3D),
                                    new PropertyMetadata(Media.Colors.Gray,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: LightNode node })
                                                                 node.Color = ((Media.Color)e.NewValue).ToColor4();
                                                         }));

    /// <summary>
    ///     Color of the light.
    ///     For simplicity, this color applies to the diffuse and specular properties of the light.
    /// </summary>
    public Media.Color Color {
        get => (Media.Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public LightType LightType => SceneNode is LightNode node
        ? node.LightType
        : throw new InvalidOperationException("The light scene node has not been created.");

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        if (core is LightNode node)
            node.Color = Color.ToColor4();
        base.AssignDefaultValuesToSceneNode(core);
    }
}
