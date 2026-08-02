using System;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX;

public class OctreeLineGeometryModel3D : CompositeModel3D {
    public static readonly DependencyProperty OctreeProperty
        = DependencyProperty.Register("Octree",
                                      typeof(IOctreeBasic),
                                      typeof(OctreeLineGeometryModel3D),
                                      new PropertyMetadata(null,
                                                           (s, e) => {
                                                               var d = s as OctreeLineGeometryModel3D;
                                                               if (e.OldValue != null)
                                                                   (e.OldValue as IOctreeBasic).Hit -=
                                                                       d.OctreeLineGeometryModel3D_OnHit;
                                                               if (e.NewValue != null)
                                                                   (e.NewValue as IOctreeBasic).Hit +=
                                                                       d.OctreeLineGeometryModel3D_OnHit;
                                                               d.CreateOctreeLines();
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

    private readonly LineGeometryModel3D HitVisual = new();

    private readonly LineGeometryModel3D OctreeVisual = new();

    public OctreeLineGeometryModel3D() {
        IsHitTestVisible = OctreeVisual.IsHitTestVisible = HitVisual.IsHitTestVisible = false;
        Children.Add(OctreeVisual);
        Children.Add(HitVisual);
        OctreeVisual.Color = LineColor;
        HitVisual.Color = HitLineColor;
        OctreeVisual.Thickness = 0;
        OctreeVisual.FillMode = FillMode.Wireframe;
        HitVisual.Thickness = 1.5;
        SceneNode.VisibleChanged += OctreeLineGeometryModel3D_OnVisibleChanged;
    }

    public IOctreeBasic Octree {
        get => (IOctreeBasic)GetValue(OctreeProperty);
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

    private void OctreeLineGeometryModel3D_OnVisibleChanged(object sender, BoolArgs e) {
        CreateOctreeLines();
    }

    private void CreateOctreeLines() {
        if (Octree != null && Visibility == Visibility.Visible && IsRendering) {
            OctreeVisual.Geometry = Octree.CreateOctreeLineModel();
            OctreeVisual.Color = LineColor;
        } else {
            OctreeVisual.Geometry = null;
        }
    }

    private void OctreeLineGeometryModel3D_OnHit(object sender, EventArgs args) {
        var node = sender as IOctreeBasic;
        if (node.HitPathBoundingBoxes.Count > 0 && Visibility == Visibility.Visible && IsRendering) {
            HitVisual.Geometry = node.HitPathBoundingBoxes.CreatePathLines();
            HitVisual.Color = HitLineColor;
        } else {
            HitVisual.Geometry = null;
        }
    }
}
