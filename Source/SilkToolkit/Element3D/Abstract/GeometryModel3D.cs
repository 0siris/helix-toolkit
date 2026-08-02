/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning model and scene node.

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Provides a base class for a scene model which contains geometry
/// </summary>
public abstract class GeometryModel3D : Element3D, IHitable, IThrowingShadow, IApplyPostEffect {
    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        if (node is GeometryNode n) {
            n.DepthBias = DepthBias;
            n.IsDepthClipEnabled = IsDepthClipEnabled;
            n.SlopeScaledDepthBias = (float)SlopeScaledDepthBias;
            n.IsMSAAEnabled = IsMultisampleEnabled;
            n.FillMode = FillMode;
            n.IsScissorEnabled = IsScissorEnabled;
            n.EnableViewFrustumCheck = EnableViewFrustumCheck;
            n.PostEffects = PostEffects;
            n.AlwaysHittable = AlwaysHittable;
        }

        base.AssignDefaultValuesToSceneNode(node);
    }

    #region DependencyProperties

    /// <summary>
    ///     The geometry property
    /// </summary>
    public static readonly DependencyProperty GeometryProperty =
        DependencyProperty.Register("Geometry",
                                    typeof(Geometry3D),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode).Geometry =
                                                                 e.NewValue as Geometry3D;
                                                         }));

    public static readonly DependencyProperty IsThrowingShadowProperty =
        DependencyProperty.Register("IsThrowingShadow",
                                    typeof(bool),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             if ((d as Element3D).SceneNode is IThrowingShadow t)
                                                                 t.IsThrowingShadow = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The depth bias property
    /// </summary>
    public static readonly DependencyProperty DepthBiasProperty =
        DependencyProperty.Register("DepthBias",
                                    typeof(int),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(0,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .DepthBias = (int)e.NewValue;
                                                         }));

    /// <summary>
    ///     The slope scaled depth bias property
    /// </summary>
    public static readonly DependencyProperty SlopeScaledDepthBiasProperty =
        DependencyProperty.Register("SlopeScaledDepthBias",
                                    typeof(double),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .SlopeScaledDepthBias = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The is selected property
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(GeometryModel3D), new PropertyMetadata(false));

    /// <summary>
    ///     The is multisample enabled property
    /// </summary>
    public static readonly DependencyProperty IsMultisampleEnabledProperty =
        DependencyProperty.Register("IsMultisampleEnabled",
                                    typeof(bool),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .IsMSAAEnabled = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The fill mode property
    /// </summary>
    public static readonly DependencyProperty FillModeProperty = DependencyProperty.Register("FillMode",
        typeof(FillMode),
        typeof(GeometryModel3D),
        new PropertyMetadata(FillMode.Solid,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as GeometryNode).FillMode = (FillMode)e.NewValue;
                             }));

    /// <summary>
    ///     The is scissor enabled property
    /// </summary>
    public static readonly DependencyProperty IsScissorEnabledProperty =
        DependencyProperty.Register("IsScissorEnabled",
                                    typeof(bool),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .IsScissorEnabled = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable view frustum check property
    /// </summary>
    public static readonly DependencyProperty EnableViewFrustumCheckProperty =
        DependencyProperty.Register("EnableViewFrustumCheck",
                                    typeof(bool),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .EnableViewFrustumCheck = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The is depth clip enabled property
    /// </summary>
    public static readonly DependencyProperty IsDepthClipEnabledProperty = DependencyProperty.Register(
        "IsDepthClipEnabled",
        typeof(bool),
        typeof(GeometryModel3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as GeometryNode).IsDepthClipEnabled =
                                     (bool)e.NewValue;
                             }));


    /// <summary>
    ///     The post effects property
    /// </summary>
    public static readonly DependencyProperty PostEffectsProperty =
        DependencyProperty.Register("PostEffects",
                                    typeof(string),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(string.Empty,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .PostEffects = e.NewValue as string;
                                                         }));

    /// <summary>
    ///     The always hittable property
    /// </summary>
    public static readonly DependencyProperty AlwaysHittableProperty =
        DependencyProperty.Register("AlwaysHittable",
                                    typeof(bool),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .AlwaysHittable = (bool)e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets the geometry.
    /// </summary>
    /// <value>
    ///     The geometry.
    /// </value>
    public Geometry3D Geometry {
        get => (Geometry3D)GetValue(GeometryProperty);
        set => SetValue(GeometryProperty, value);
    }

    /// <summary>
    ///     <see cref="IThrowingShadow.IsThrowingShadow" />
    /// </summary>
    public bool IsThrowingShadow {
        get => (bool)GetValue(IsThrowingShadowProperty);
        set => SetValue(IsThrowingShadowProperty, value);
    }

    /// <summary>
    ///     List of instance matrix.
    /// </summary>
    public static readonly DependencyProperty InstancesProperty =
        DependencyProperty.Register("Instances",
                                    typeof(IList<Matrix>),
                                    typeof(GeometryModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as GeometryNode)
                                                                 .Instances = e.NewValue as IList<Matrix>;
                                                         }));

    /// <summary>
    ///     List of instance matrix.
    /// </summary>
    public IList<Matrix> Instances {
        get => (IList<Matrix>)GetValue(InstancesProperty);
        set => SetValue(InstancesProperty, value);
    }

    /// <summary>
    ///     Gets or sets the depth bias.
    /// </summary>
    /// <value>
    ///     The depth bias.
    /// </value>
    public int DepthBias {
        get => (int)GetValue(DepthBiasProperty);
        set => SetValue(DepthBiasProperty, value);
    }

    /// <summary>
    ///     Gets or sets the slope scaled depth bias.
    /// </summary>
    /// <value>
    ///     The slope scaled depth bias.
    /// </value>
    public double SlopeScaledDepthBias {
        get => (double)GetValue(SlopeScaledDepthBiasProperty);
        set => SetValue(SlopeScaledDepthBiasProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is selected.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is selected; otherwise, <c>false</c>.
    /// </value>
    public bool IsSelected {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    ///     Only works under FillMode = Wireframe. MSAA is determined by viewport MSAA settings for FillMode = Solid
    /// </summary>
    public bool IsMultisampleEnabled {
        get => (bool)GetValue(IsMultisampleEnabledProperty);
        set => SetValue(IsMultisampleEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fill mode.
    /// </summary>
    /// <value>
    ///     The fill mode.
    /// </value>
    public FillMode FillMode {
        get => (FillMode)GetValue(FillModeProperty);
        set => SetValue(FillModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is scissor enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is scissor enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsScissorEnabled {
        get => (bool)GetValue(IsScissorEnabledProperty);
        set => SetValue(IsScissorEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is depth clip enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is depth clip enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsDepthClipEnabled {
        get => (bool)GetValue(IsDepthClipEnabledProperty);
        set => SetValue(IsDepthClipEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable view frustum check].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable view frustum check]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableViewFrustumCheck {
        get => (bool)GetValue(EnableViewFrustumCheckProperty);
        set => SetValue(EnableViewFrustumCheckProperty, value);
    }

    public string PostEffects {
        get => (string)GetValue(PostEffectsProperty);
        set => SetValue(PostEffectsProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether [always hittable] even it is not rendering.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [always hittable]; otherwise, <c>false</c>.
    /// </value>
    public bool AlwaysHittable {
        get => (bool)GetValue(AlwaysHittableProperty);
        set => SetValue(AlwaysHittableProperty, value);
    }

    #endregion
}

/// <summary>
/// </summary>
/// <seealso cref="System.EventArgs" />
public sealed class BoundChangedEventArgs : EventArgs {
    /// <summary>
    ///     The new bound
    /// </summary>
    public readonly BoundingBox NewBound;

    /// <summary>
    ///     The old bound
    /// </summary>
    public readonly BoundingBox OldBound;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BoundChangedEventArgs" /> class.
    /// </summary>
    /// <param name="newBound">The new bound.</param>
    /// <param name="oldBound">The old bound.</param>
    public BoundChangedEventArgs(BoundingBox newBound, BoundingBox oldBound) {
        NewBound = newBound;
        OldBound = oldBound;
    }
}
