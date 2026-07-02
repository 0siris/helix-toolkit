using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

public class InstancingMeshGeometryModel3D : MeshGeometryModel3D
{
    #region DependencyProperties

    /// <summary>
    ///     If bind to identifiers, hit test returns identifier as Tag in HitTestResult.
    /// </summary>
    public static readonly DependencyProperty InstanceIdentifiersProperty = DependencyProperty.Register(
        "InstanceIdentifiers", typeof(IList<Guid>),
        typeof(InstancingMeshGeometryModel3D),
        new PropertyMetadata(null,
            (d, e) =>
            {
                ((d as Element3DCore).SceneNode as InstancingMeshNode).InstanceIdentifiers = e.NewValue as IList<Guid>;
            }));

    /// <summary>
    ///     Add octree manager to use octree hit test.
    /// </summary>
    public static readonly DependencyProperty OctreeManagerProperty = DependencyProperty.Register("OctreeManager",
        typeof(IOctreeManagerWrapper), typeof(InstancingMeshGeometryModel3D), new PropertyMetadata(null, (s, e) =>
        {
            var d = s as InstancingMeshGeometryModel3D;
#if NETFX_CORE || WINUI
                d.AttachChild(null);
                if(e.NewValue is Element3D elem)
                {
                    d.AttachChild(elem);
                }
#else
            if (e.OldValue != null) d.RemoveLogicalChild(e.OldValue);

            if (e.NewValue != null) d.AddLogicalChild(e.NewValue);
#endif
            (d.SceneNode as InstancingMeshNode).OctreeManager =
                e.NewValue == null ? null : (e.NewValue as IOctreeManagerWrapper).Manager;
        }));

    /// <summary>
    ///     List of instance parameter.
    /// </summary>
    public static readonly DependencyProperty InstanceAdvArrayProperty =
        DependencyProperty.Register("InstanceParamArray", typeof(IList<InstanceParameter>),
            typeof(InstancingMeshGeometryModel3D),
            new PropertyMetadata(null,
                (d, e) =>
                {
                    ((d as Element3DCore).SceneNode as InstancingMeshNode).InstanceParamArray =
                        e.NewValue as IList<InstanceParameter>;
                }));

    /// <summary>
    ///     If bind to identifiers, hit test returns identifier as Tag in HitTestResult.
    /// </summary>
    public IList<Guid> InstanceIdentifiers
    {
        get => (IList<Guid>) GetValue(InstanceIdentifiersProperty);
        set => SetValue(InstanceIdentifiersProperty, value);
    }

    public IOctreeManagerWrapper OctreeManager
    {
        get => (IOctreeManagerWrapper) GetValue(OctreeManagerProperty);
        set => SetValue(OctreeManagerProperty, value);
    }

    /// <summary>
    ///     List of instance parameters.
    /// </summary>
    public IList<InstanceParameter> InstanceParamArray
    {
        get => (IList<InstanceParameter>) GetValue(InstanceAdvArrayProperty);
        set => SetValue(InstanceAdvArrayProperty, value);
    }

    #endregion

    protected override SceneNode OnCreateSceneNode()
    {
        return new InstancingMeshNode();
    }
#if NETFX_CORE
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if(OctreeManager is Element3D elem)
            {
                AttachChild(elem);
            }
        }
#endif
}