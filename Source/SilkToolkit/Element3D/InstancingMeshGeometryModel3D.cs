using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Utilities;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

public class InstancingMeshGeometryModel3D : MeshGeometryModel3D {
#region DependencyProperties

    /// <summary>
    ///     If bind to identifiers, hit test returns identifier as Tag in HitTestResult.
    /// </summary>
    public static readonly DependencyProperty InstanceIdentifiersProperty = DependencyProperty.Register(
        "InstanceIdentifiers",
        typeof(IList<Guid>),
        typeof(InstancingMeshGeometryModel3D),
        new PropertyMetadata(null,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: InstancingMeshNode node })
                                     node.InstanceIdentifiers = e.NewValue as IList<Guid>;
                              }));

    /// <summary>
    ///     Add octree manager to use octree hit test.
    /// </summary>
    public static readonly DependencyProperty OctreeManagerProperty = DependencyProperty.Register("OctreeManager",
        typeof(IOctreeManagerWrapper),
        typeof(InstancingMeshGeometryModel3D),
        new PropertyMetadata(null,
                             (s, e) => {
                                  if (s is not InstancingMeshGeometryModel3D model)
                                      return;
                                  if (e.OldValue is { } oldValue) model.RemoveLogicalChild(oldValue);
                                  if (e.NewValue is { } newValue) model.AddLogicalChild(newValue);
                                  if (model.SceneNode is InstancingMeshNode node)
                                      node.OctreeManager = (e.NewValue as IOctreeManagerWrapper)?.Manager;
                              }));

    /// <summary>
    ///     List of instance parameter.
    /// </summary>
    public static readonly DependencyProperty InstanceAdvArrayProperty =
        DependencyProperty.Register("InstanceParamArray",
                                    typeof(IList<InstanceParameter>),
                                    typeof(InstancingMeshGeometryModel3D),
                                    new PropertyMetadata(null,
                                                          (d, e) => {
                                                              if (d is Element3DCore { SceneNode: InstancingMeshNode node })
                                                                  node.InstanceParamArray = e.NewValue as IList<InstanceParameter>;
                                                          }));

    /// <summary>
    ///     If bind to identifiers, hit test returns identifier as Tag in HitTestResult.
    /// </summary>
    public IList<Guid>? InstanceIdentifiers {
        get => GetValue(InstanceIdentifiersProperty) as IList<Guid>;
        set => SetValue(InstanceIdentifiersProperty, value);
    }

    public IOctreeManagerWrapper? OctreeManager {
        get => GetValue(OctreeManagerProperty) as IOctreeManagerWrapper;
        set => SetValue(OctreeManagerProperty, value);
    }

    /// <summary>
    ///     List of instance parameters.
    /// </summary>
    public IList<InstanceParameter>? InstanceParamArray {
        get => GetValue(InstanceAdvArrayProperty) as IList<InstanceParameter>;
        set => SetValue(InstanceAdvArrayProperty, value);
    }

#endregion

    protected override SceneNode OnCreateSceneNode() => new InstancingMeshNode();
}
