// <copyright file="CrossSectionMeshGeometryModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
// </copyright>

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Defines the <see cref="CrossSectionMeshGeometryModel3D" />
/// </summary>
public class CrossSectionMeshGeometryModel3D : MeshGeometryModel3D
{
    protected override SceneNode OnCreateSceneNode()
    {
        return new CrossSectionMeshNode();
    }

    #region Dependency Properties

    /// <summary>
    ///     Gets or sets the cutting operation.
    /// </summary>
    /// <value>
    ///     The cutting operation.
    /// </value>
    public CuttingOperation CuttingOperation
    {
        get => (CuttingOperation) GetValue(CuttingOperationProperty);
        set => SetValue(CuttingOperationProperty, value);
    }

    /// <summary>
    ///     The cutting operation property
    /// </summary>
    public static readonly DependencyProperty CuttingOperationProperty =
        DependencyProperty.Register("CuttingOperation", typeof(CuttingOperation),
            typeof(CrossSectionMeshGeometryModel3D),
            new PropertyMetadata(CuttingOperation.Intersect,
                (d, e) =>
                {
                    ((d as Element3DCore).SceneNode as CrossSectionMeshNode).CuttingOperation =
                        (CuttingOperation) e.NewValue;
                }));


    /// <summary>
    ///     Defines the CrossSectionColorProperty
    /// </summary>
    public static DependencyProperty CrossSectionColorProperty = DependencyProperty.Register("CrossSectionColor",
        typeof(Media.Color), typeof(CrossSectionMeshGeometryModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Firebrick,
#else
        new PropertyMetadata(Media.Colors.Firebrick,
#endif
            (d, e) =>
            {
                ((d as Element3DCore).SceneNode as CrossSectionMeshNode).CrossSectionColor =
                    ((Media.Color) e.NewValue).ToColor4();
            }));

    /// <summary>
    ///     Gets or sets the CrossSectionColor
    /// </summary>
    public Media.Color CrossSectionColor
    {
        get => (Media.Color) GetValue(CrossSectionColorProperty);
        set => SetValue(CrossSectionColorProperty, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane1Property = DependencyProperty.Register("EnablePlane1", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane1 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane1
    {
        get => (bool) GetValue(EnablePlane1Property);
        set => SetValue(EnablePlane1Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane2Property = DependencyProperty.Register("EnablePlane2", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane2 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane2
    {
        get => (bool) GetValue(EnablePlane2Property);
        set => SetValue(EnablePlane2Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane3Property = DependencyProperty.Register("EnablePlane3", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane3 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane3
    {
        get => (bool) GetValue(EnablePlane3Property);
        set => SetValue(EnablePlane3Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane4Property = DependencyProperty.Register("EnablePlane4", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane4 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane4
    {
        get => (bool) GetValue(EnablePlane4Property);
        set => SetValue(EnablePlane4Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane5Property = DependencyProperty.Register("EnablePlane5", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane5 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane5
    {
        get => (bool) GetValue(EnablePlane5Property);
        set => SetValue(EnablePlane5Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane6Property = DependencyProperty.Register("EnablePlane6", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane6 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane6
    {
        get => (bool) GetValue(EnablePlane6Property);
        set => SetValue(EnablePlane6Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane7Property = DependencyProperty.Register("EnablePlane7", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane7 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane7
    {
        get => (bool) GetValue(EnablePlane7Property);
        set => SetValue(EnablePlane7Property, value);
    }

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public static DependencyProperty EnablePlane8Property = DependencyProperty.Register("EnablePlane8", typeof(bool),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).EnablePlane8 = (bool) e.NewValue; }));

    /// <summary>
    ///     Enable CrossSection Plane
    /// </summary>
    public bool EnablePlane8
    {
        get => (bool) GetValue(EnablePlane8Property);
        set => SetValue(EnablePlane8Property, value);
    }

    /// <summary>
    ///     Defines the Plane1Property
    /// </summary>
    public static DependencyProperty Plane1Property = DependencyProperty.Register("Plane1", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane1 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane1
    /// </summary>
    public Plane Plane1
    {
        get => (Plane) GetValue(Plane1Property);
        set => SetValue(Plane1Property, value);
    }

    /// <summary>
    ///     Defines the Plane2Property
    /// </summary>
    public static DependencyProperty Plane2Property = DependencyProperty.Register("Plane2", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane2 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane2
    /// </summary>
    public Plane Plane2
    {
        get => (Plane) GetValue(Plane2Property);
        set => SetValue(Plane2Property, value);
    }

    /// <summary>
    ///     Defines the Plane3Property
    /// </summary>
    public static DependencyProperty Plane3Property = DependencyProperty.Register("Plane3", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane3 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane3
    /// </summary>
    public Plane Plane3
    {
        get => (Plane) GetValue(Plane3Property);
        set => SetValue(Plane3Property, value);
    }

    /// <summary>
    ///     Defines the Plane4Property
    /// </summary>
    public static DependencyProperty Plane4Property = DependencyProperty.Register("Plane4", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane4 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane4
    /// </summary>
    public Plane Plane4
    {
        get => (Plane) GetValue(Plane4Property);
        set => SetValue(Plane4Property, value);
    }

    /// <summary>
    ///     Defines the Plane5Property
    /// </summary>
    public static DependencyProperty Plane5Property = DependencyProperty.Register("Plane5", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane5 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane5
    /// </summary>
    public Plane Plane5
    {
        get => (Plane) GetValue(Plane5Property);
        set => SetValue(Plane5Property, value);
    }

    /// <summary>
    ///     Defines the Plane6Property
    /// </summary>
    public static DependencyProperty Plane6Property = DependencyProperty.Register("Plane6", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane6 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane6
    /// </summary>
    public Plane Plane6
    {
        get => (Plane) GetValue(Plane6Property);
        set => SetValue(Plane6Property, value);
    }

    /// <summary>
    ///     Defines the Plane7Property
    /// </summary>
    public static DependencyProperty Plane7Property = DependencyProperty.Register("Plane7", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane7 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane7
    /// </summary>
    public Plane Plane7
    {
        get => (Plane) GetValue(Plane7Property);
        set => SetValue(Plane7Property, value);
    }

    /// <summary>
    ///     Defines the Plane8Property
    /// </summary>
    public static DependencyProperty Plane8Property = DependencyProperty.Register("Plane8", typeof(Plane),
        typeof(CrossSectionMeshGeometryModel3D),
        new PropertyMetadata(new Plane(),
            (d, e) => { ((d as Element3DCore).SceneNode as CrossSectionMeshNode).Plane8 = (Plane) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the Plane8
    /// </summary>
    public Plane Plane8
    {
        get => (Plane) GetValue(Plane8Property);
        set => SetValue(Plane8Property, value);
    }

    #endregion
}