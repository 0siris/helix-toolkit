/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

public class VolumeTextureModel3D : Element3D {
    public static readonly DependencyProperty VolumeMaterialProperty =
        DependencyProperty.Register("VolumeMaterial",
                                    typeof(Material),
                                    typeof(VolumeTextureModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as VolumeTextureModel3D)
                                                              .SceneNode as VolumeTextureNode).Material =
                                                                 (Material)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the volume material.
    /// </summary>
    /// <value>
    ///     The volume material.
    /// </value>
    public Material VolumeMaterial {
        get => (Material)GetValue(VolumeMaterialProperty);
        set => SetValue(VolumeMaterialProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new VolumeTextureNode {
        Material = VolumeMaterial
    };
}
