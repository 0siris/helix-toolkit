/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
#if WINUI
    [SupportedOSPlatform("windows")]
#endif
public class ScreenQuadModel3D : Element3D {
    public TextureModel Texture {
        get => (TextureModel)GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    public static readonly DependencyProperty TextureProperty =
        DependencyProperty.Register("Texture",
                                    typeof(TextureModel),
                                    typeof(ScreenQuadModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as ScreenQuadModel3D).SceneNode as ScreenQuadNode)
                                                                 .Texture = (TextureModel)e.NewValue;
                                                         }));


    public SamplerStateDescription SamplerDescription {
        get => (SamplerStateDescription)GetValue(SamplerDescriptionProperty);
        set => SetValue(SamplerDescriptionProperty, value);
    }


    public static readonly DependencyProperty SamplerDescriptionProperty =
        DependencyProperty.Register("SamplerDescription",
                                    typeof(SamplerStateDescription),
                                    typeof(ScreenQuadModel3D),
                                    new PropertyMetadata(DefaultSamplers.LinearSamplerClampAni1,
                                                         (d, e) => {
                                                             ((d as ScreenQuadModel3D).SceneNode as ScreenQuadNode)
                                                                 .Sampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets the depth of the quad, range from 0 ~ 1.
    /// </summary>
    /// <value>
    ///     The depth.
    /// </value>
    public double Depth {
        get => (double)GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public static readonly DependencyProperty DepthProperty =
        DependencyProperty.Register("Depth",
                                    typeof(double),
                                    typeof(ScreenQuadModel3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as ScreenQuadModel3D).SceneNode as ScreenQuadNode)
                                                                 .Depth =
                                                                 (float)Math.Max(0, Math.Min(1, (double)e.NewValue));
                                                         }));


    protected override SceneNode OnCreateSceneNode() {
        return new ScreenQuadNode();
    }

    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        base.AssignDefaultValuesToSceneNode(node);
        if (node is ScreenQuadNode n) {
            n.Texture = Texture;
            n.Sampler = SamplerDescription;
            n.Depth = (float)Math.Max(0, Math.Min(1, Depth));
        }
    }
}
