/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

public class LineMaterialGeometryModel3D : GeometryModel3D
{
    /// <summary>
    /// </summary>
    public static readonly DependencyProperty MaterialProperty =
        DependencyProperty.Register("Material", typeof(Material), typeof(LineMaterialGeometryModel3D),
            new PropertyMetadata(null,
                (d, e) => { ((d as Element3DCore).SceneNode as LineNode).Material = e.NewValue as Material; }));

    /// <summary>
    ///     The hit test thickness property
    /// </summary>
    public static readonly DependencyProperty HitTestThicknessProperty =
        DependencyProperty.Register("HitTestThickness", typeof(double), typeof(LineMaterialGeometryModel3D),
            new PropertyMetadata(1.0,
                (d, e) => { ((d as Element3DCore).SceneNode as LineNode).HitTestThickness = (double) e.NewValue; }));

    /// <summary>
    /// </summary>
    public Material Material
    {
        get => (Material) GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    /// <summary>
    ///     Used only for point/line hit test
    /// </summary>
    public double HitTestThickness
    {
        get => (double) GetValue(HitTestThicknessProperty);
        set => SetValue(HitTestThicknessProperty, value);
    }

    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode()
    {
        return new LineNode();
    }

    protected override void AssignDefaultValuesToSceneNode(SceneNode node)
    {
        base.AssignDefaultValuesToSceneNode(node);
        if (node is LineNode p)
        {
            p.Material = Material;
            p.HitTestThickness = HitTestThickness;
        }
    }
}