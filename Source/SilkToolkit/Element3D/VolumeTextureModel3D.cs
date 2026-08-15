/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

public class VolumeTextureModel3D : Model.Elements3D.AbstractElements3D.Element3D {
    public static readonly DependencyProperty VolumeMaterialProperty =
        DependencyProperty.Register("VolumeMaterial",
                                    typeof(Material.Material),
                                    typeof(VolumeTextureModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is VolumeTextureModel3D { SceneNode: VolumeTextureNode node })
                                                                 node.Material = (Material.Material)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the volume material.
    /// </summary>
    /// <value>
    ///     The volume material.
    /// </value>
    public Material.Material VolumeMaterial {
        get => (Material.Material)GetValue(VolumeMaterialProperty);
        set => SetValue(VolumeMaterialProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new VolumeTextureNode {
        Material = VolumeMaterial
    };
}
