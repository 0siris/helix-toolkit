/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace HelixToolkit.Wpf.SharpDX;

public class OutLineMeshGeometryModel3D : MeshGeometryModel3D {
    public static readonly DependencyProperty EnableOutlineProperty = DependencyProperty.Register("EnableOutline",
        typeof(bool),
        typeof(OutLineMeshGeometryModel3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshOutlineNode).EnableOutline = (bool) e.NewValue;
                             }));

    public static DependencyProperty OutlineColorProperty = DependencyProperty.Register("OutlineColor",
        typeof(Color),
        typeof(OutLineMeshGeometryModel3D),
        new PropertyMetadata(Colors.White,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshOutlineNode).OutlineColor =
                                     ((Color) e.NewValue).ToColor4();
                             }));

    public static DependencyProperty IsDrawGeometryProperty = DependencyProperty.Register("IsDrawGeometry",
        typeof(bool),
        typeof(OutLineMeshGeometryModel3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshOutlineNode).IsDrawGeometry = (bool) e.NewValue;
                             }));


    public static DependencyProperty OutlineFadingFactorProperty = DependencyProperty.Register("OutlineFadingFactor",
        typeof(double),
        typeof(OutLineMeshGeometryModel3D),
        new PropertyMetadata(1.5,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as MeshOutlineNode).OutlineFadingFactor =
                                     (float) (double) e.NewValue;
                             }));

    public bool EnableOutline {
        get => (bool) GetValue(EnableOutlineProperty);
        set => SetValue(EnableOutlineProperty, value);
    }

    public Color OutlineColor {
        get => (Color) GetValue(OutlineColorProperty);
        set => SetValue(OutlineColorProperty, value);
    }

    public bool IsDrawGeometry {
        get => (bool) GetValue(IsDrawGeometryProperty);
        set => SetValue(IsDrawGeometryProperty, value);
    }

    public double OutlineFadingFactor {
        get => (double) GetValue(OutlineFadingFactorProperty);
        set => SetValue(OutlineFadingFactorProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() {
        return new MeshOutlineNode();
    }

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        if (core is MeshOutlineNode c) {
            c.OutlineColor = OutlineColor.ToColor4();
            c.EnableOutline = EnableOutline;
            c.OutlineFadingFactor = (float) OutlineFadingFactor;
            c.IsDrawGeometry = IsDrawGeometry;
        }

        base.AssignDefaultValuesToSceneNode(core);
    }
}
