using System;
using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Octrees;
using HelixToolkit.Wpf.SharpDX.Element3D;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements3D;

public class OctreeLineGeometryModel3D : CompositeModel3D {
    public static readonly DependencyProperty OctreeProperty
        = DependencyProperty.Register("Octree",
                                      typeof(IOctreeBasic),
                                      typeof(OctreeLineGeometryModel3D),
                                      new PropertyMetadata(null,
                                                           (s, e) => {
                                                                if (s is not OctreeLineGeometryModel3D model)
                                                                    return;
                                                                if (e.OldValue is IOctreeBasic oldOctree)
                                                                    oldOctree.Hit -= model.OctreeLineGeometryModel3D_OnHit;
                                                                if (e.NewValue is IOctreeBasic newOctree)
                                                                    newOctree.Hit += model.OctreeLineGeometryModel3D_OnHit;
                                                                model.CreateOctreeLines();
                                                           }));

    public static readonly DependencyProperty LineColorProperty
        = DependencyProperty.Register("LineColor",
                                      typeof(Media.Color),
                                      typeof(OctreeLineGeometryModel3D),
                                      new PropertyMetadata(Media.Colors.Green));

    public static readonly DependencyProperty HitLineColorProperty
        = DependencyProperty.Register("HitLineColor",
                                      typeof(Media.Color),
                                      typeof(OctreeLineGeometryModel3D),
                                      new PropertyMetadata(Media.Colors.Red));

    private readonly LineGeometryModel3D hitVisual = new();

    private readonly LineGeometryModel3D octreeVisual = new();

    public OctreeLineGeometryModel3D() {
        IsHitTestVisible = octreeVisual.IsHitTestVisible = hitVisual.IsHitTestVisible = false;
        Children.Add(octreeVisual);
        Children.Add(hitVisual);
        octreeVisual.Color = LineColor;
        hitVisual.Color = HitLineColor;
        octreeVisual.Thickness = 0;
        octreeVisual.FillMode = FillMode.Wireframe;
        hitVisual.Thickness = 1.5;
        SceneNode.VisibleChanged += OctreeLineGeometryModel3D_OnVisibleChanged;
    }

    public IOctreeBasic? Octree {
        get => GetValue(OctreeProperty) as IOctreeBasic;
        set => SetValue(OctreeProperty, value);
    }

    public Media.Color LineColor {
        get => (Media.Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public Media.Color HitLineColor {
        get => (Media.Color)GetValue(HitLineColorProperty);
        set => SetValue(HitLineColorProperty, value);
    }

    private void OctreeLineGeometryModel3D_OnVisibleChanged(object? sender, BoolArgs e) {
        CreateOctreeLines();
    }

    private void CreateOctreeLines() {
        if (Octree is { } octree && Visibility == Visibility.Visible && IsRendering) {
            octreeVisual.Geometry = octree.CreateOctreeLineModel();
            octreeVisual.Color = LineColor;
        } else {
            octreeVisual.Geometry = null;
        }
    }

    private void OctreeLineGeometryModel3D_OnHit(object? sender, EventArgs args) {
        if (sender is not IOctreeBasic node)
            return;
        if (node.HitPathBoundingBoxes.Count > 0 && Visibility == Visibility.Visible && IsRendering) {
            hitVisual.Geometry = node.HitPathBoundingBoxes.CreatePathLines();
            hitVisual.Color = HitLineColor;
        } else {
            hitVisual.Geometry = null;
        }
    }
}
