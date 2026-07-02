/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
namespace HelixToolkit.Wpf.SharpDX {
    public class VolumeTextureModel3D : Element3D
    {
        /// <summary>
        /// Gets or sets the volume material.
        /// </summary>
        /// <value>
        /// The volume material.
        /// </value>
        public Material VolumeMaterial
        {
            get
            {
                return (Material)GetValue(VolumeMaterialProperty);
            }
            set
            {
                SetValue(VolumeMaterialProperty, value);
            }
        }

        public static readonly DependencyProperty VolumeMaterialProperty =
            DependencyProperty.Register("VolumeMaterial", typeof(Material), typeof(VolumeTextureModel3D),
                new PropertyMetadata(null, (d, e) =>
                {
                    ((d as VolumeTextureModel3D).SceneNode as VolumeTextureNode).Material = (Material)e.NewValue;
                }));

        protected override SceneNode OnCreateSceneNode()
        {
            return new VolumeTextureNode()
            {
                Material = VolumeMaterial
            };
        }
    }
}
