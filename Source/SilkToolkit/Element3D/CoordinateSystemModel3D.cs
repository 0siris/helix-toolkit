// <copyright file="CoordinateSystemModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
//   Author: Lunci Hua
// </copyright>


using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using Media = System.Windows.Media;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning model and scene node.

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
public class CoordinateSystemModel3D : ScreenSpacedElement3D {
    /// <summary>
    ///     <see cref="AxisXColor" />
    /// </summary>
    public static readonly DependencyProperty AxisXColorProperty = DependencyProperty.Register("AxisXColor",
        typeof(Media.Color),
        typeof(CoordinateSystemModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Red,
#else
        new PropertyMetadata(Media.Colors.Red,
#endif
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).AxisXColor =
                                     ((Media.Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    ///     <see cref="AxisYColor" />
    /// </summary>
    public static readonly DependencyProperty AxisYColorProperty = DependencyProperty.Register("AxisYColor",
        typeof(Media.Color),
        typeof(CoordinateSystemModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Green,
#else
        new PropertyMetadata(Media.Colors.Green,
#endif
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).AxisYColor =
                                     ((Media.Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    ///     <see cref="AxisZColor" />
    /// </summary>
    public static readonly DependencyProperty AxisZColorProperty = DependencyProperty.Register("AxisZColor",
        typeof(Media.Color),
        typeof(CoordinateSystemModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Blue,
#else
        new PropertyMetadata(Media.Colors.Blue,
#endif
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).AxisZColor =
                                     ((Media.Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty LabelColorProperty = DependencyProperty.Register("LabelColor",
        typeof(Media.Color),
        typeof(CoordinateSystemModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Gray,
#else
        new PropertyMetadata(Media.Colors.Gray,
#endif
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).LabelColor =
                                     ((Media.Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    ///     The coordinate system label x property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelXProperty = DependencyProperty.Register(
        "CoordinateSystemLabelX",
        typeof(string),
        typeof(CoordinateSystemModel3D),
        new PropertyMetadata("X",
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).LabelX = e.NewValue as string;
                             }));

    /// <summary>
    ///     The coordinate system label Y property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelYProperty = DependencyProperty.Register(
        "CoordinateSystemLabelY",
        typeof(string),
        typeof(CoordinateSystemModel3D),
        new PropertyMetadata("Y",
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).LabelY = e.NewValue as string;
                             }));

    /// <summary>
    ///     The coordinate system label Z property
    /// </summary>
    public static readonly DependencyProperty CoordinateSystemLabelZProperty = DependencyProperty.Register(
        "CoordinateSystemLabelZ",
        typeof(string),
        typeof(CoordinateSystemModel3D),
        new PropertyMetadata("Z",
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as CoordinateSystemNode).LabelZ = e.NewValue as string;
                             }));

    /// <summary>
    ///     Axis X Color
    /// </summary>
    public Media.Color AxisXColor {
        get => (Media.Color)GetValue(AxisXColorProperty);
        set => SetValue(AxisXColorProperty, value);
    }

    /// <summary>
    ///     Axis Y Color
    /// </summary>
    public Media.Color AxisYColor {
        get => (Media.Color)GetValue(AxisYColorProperty);
        set => SetValue(AxisYColorProperty, value);
    }

    /// <summary>
    ///     Axis Z Color
    /// </summary>
    public Media.Color AxisZColor {
        get => (Media.Color)GetValue(AxisZColorProperty);
        set => SetValue(AxisZColorProperty, value);
    }

    /// <summary>
    ///     Label Color
    /// </summary>
    public Media.Color LabelColor {
        get => (Media.Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    /// <summary>
    /// </summary>
    public string CoordinateSystemLabelX {
        get => (string)GetValue(CoordinateSystemLabelXProperty);
        set => SetValue(CoordinateSystemLabelXProperty, value);
    }

    /// <summary>
    /// </summary>
    public string CoordinateSystemLabelY {
        get => (string)GetValue(CoordinateSystemLabelYProperty);
        set => SetValue(CoordinateSystemLabelYProperty, value);
    }

    /// <summary>
    /// </summary>
    public string CoordinateSystemLabelZ {
        get => (string)GetValue(CoordinateSystemLabelZProperty);
        set => SetValue(CoordinateSystemLabelZProperty, value);
    }


    protected override SceneNode OnCreateSceneNode() => new CoordinateSystemNode();

    public override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;
}
