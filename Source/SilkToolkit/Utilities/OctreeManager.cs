// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OctreeManager.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public abstract class OctreeManagerBaseWrapper : FrameworkContentElement, IOctreeManagerWrapper {
    /// <summary>
    ///     The octree property
    /// </summary>
    public static readonly DependencyProperty OctreeProperty
        = DependencyProperty.Register("Octree",
                                      typeof(IOctreeBasic),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(null));

    /// <summary>
    ///     The enable octree output property
    /// </summary>
    public static readonly DependencyProperty EnableOctreeOutputProperty
        = DependencyProperty.Register("EnableOctreeOutput",
                                      typeof(bool),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(false,
                                                           (d, e) => {
                                                               (d as OctreeManagerBaseWrapper).enableOctreeOutput =
                                                                   (bool) e.NewValue;
                                                           }));

    /// <summary>
    ///     The minimum size property
    /// </summary>
    public static readonly DependencyProperty MinSizeProperty
        = DependencyProperty.Register("MinSize",
                                      typeof(float),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(1f,
                                                           (s, e) => {
                                                               (s as OctreeManagerBaseWrapper).Manager.Parameter
                                                                   .MinimumOctantSize = (float) e.NewValue;
                                                           }));

    /// <summary>
    ///     The automatic delete if empty property
    /// </summary>
    public static readonly DependencyProperty AutoDeleteIfEmptyProperty
        = DependencyProperty.Register("AutoDeleteIfEmpty",
                                      typeof(bool),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(true,
                                                           (s, e) => {
                                                               (s as OctreeManagerBaseWrapper).Manager.Parameter
                                                                   .AutoDeleteIfEmpty = (bool) e.NewValue;
                                                           }));

    /// <summary>
    ///     The cubify property property
    /// </summary>
    public static readonly DependencyProperty CubifyPropertyProperty
        = DependencyProperty.Register("Cubify",
                                      typeof(bool),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(false,
                                                           (s, e) => {
                                                               (s as OctreeManagerBaseWrapper).Manager.Parameter
                                                                   .Cubify = (bool) e.NewValue;
                                                           }));

    /// <summary>
    ///     The record hit path bounding boxes property
    /// </summary>
    public static readonly DependencyProperty RecordHitPathBoundingBoxesProperty
        = DependencyProperty.Register("RecordHitPathBoundingBoxes",
                                      typeof(bool),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(false,
                                                           (s, e) => {
                                                               (s as OctreeManagerBaseWrapper).Manager.Parameter
                                                                   .RecordHitPathBoundingBoxes = (bool) e.NewValue;
                                                           }));

    /// <summary>
    ///     The minimum object size to split property
    /// </summary>
    public static readonly DependencyProperty MinObjectSizeToSplitProperty
        = DependencyProperty.Register("MinObjectSizeToSplit",
                                      typeof(int),
                                      typeof(OctreeManagerBaseWrapper),
                                      new PropertyMetadata(0,
                                                           (s, e) => {
                                                               (s as OctreeManagerBaseWrapper).Manager.Parameter
                                                                   .MinObjectSizeToSplit = (int) e.NewValue;
                                                           }));

    /// <summary>
    ///     Gets or sets the octree.
    /// </summary>
    /// <value>
    ///     The octree.
    /// </value>
    public IOctreeBasic Octree {
        get => (IOctreeBasic) GetValue(OctreeProperty);
        set => SetValue(OctreeProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable octree output].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable octree output]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableOctreeOutput {
        get => (bool) GetValue(EnableOctreeOutputProperty);
        set => SetValue(EnableOctreeOutputProperty, value);
    }

    /// <summary>
    ///     Minimum octant size
    /// </summary>
    public float MinSize {
        get => (float) GetValue(MinSizeProperty);
        set => SetValue(MinSizeProperty, value);
    }

    /// <summary>
    ///     Delete octant node if its empty
    /// </summary>
    public bool AutoDeleteIfEmpty {
        get => (bool) GetValue(AutoDeleteIfEmptyProperty);
        set => SetValue(AutoDeleteIfEmptyProperty, value);
    }

    /// <summary>
    ///     Create cube octree
    /// </summary>
    public bool Cubify {
        get => (bool) GetValue(CubifyPropertyProperty);
        set => SetValue(CubifyPropertyProperty, value);
    }

    /// <summary>
    ///     Record the hit path bounding box for debugging
    /// </summary>
    public bool RecordHitPathBoundingBoxes {
        get => (bool) GetValue(RecordHitPathBoundingBoxesProperty);
        set => SetValue(RecordHitPathBoundingBoxesProperty, value);
    }

    /// <summary>
    ///     Minimum object in each octant to start splitting into smaller octant during build
    /// </summary>
    public int MinObjectSizeToSplit {
        get => (int) GetValue(MinObjectSizeToSplitProperty);
        set => SetValue(MinObjectSizeToSplitProperty, value);
    }
    private DispatcherOperation octreeOpt;
    private bool enableOctreeOutput;
    private IOctreeManager manager;

    /// <summary>
    ///     Gets the manager.
    /// </summary>
    /// <value>
    ///     The manager.
    /// </value>
    public IOctreeManager Manager {
        get {
            if (manager == null) {
                manager = OnCreateManager();
                manager.OnOctreeCreated += (s, e) => {
                    if (octreeOpt != null && octreeOpt.Status == DispatcherOperationStatus.Pending) octreeOpt.Abort();
                    if (enableOctreeOutput)
                        octreeOpt = Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                           new Action(() => {
                                                               Octree = null;
                                                               Octree = e.Octree;
                                                           }));
                };
            }

            return manager;
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="OctreeManagerBaseWrapper" /> is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if enabled; otherwise, <c>false</c>.
    /// </value>
    public bool Enabled {
        get => manager.Enabled;
        set => manager.Enabled = value;
    }

    /// <summary>
    ///     Gets or sets the parameter.
    /// </summary>
    /// <value>
    ///     The parameter.
    /// </value>
    public OctreeBuildParameter Parameter {
        get => Manager.Parameter;
        set => Manager.Parameter = value;
    }

    /// <summary>
    ///     Called when [create manager].
    /// </summary>
    /// <returns></returns>
    protected abstract IOctreeManager OnCreateManager();
}

/// <summary>
///     Use to create geometryModel3D octree for groups. Each ItemsModel3D must has its own manager, do not share between
///     two ItemsModel3D
/// </summary>
public sealed class GeometryModel3DOctreeManager : OctreeManagerBaseWrapper {
    protected override IOctreeManager OnCreateManager() {
        return new GroupNodeGeometryBoundOctreeManager();
    }
}

public sealed class InstancingModel3DOctreeManager : OctreeManagerBaseWrapper {
    protected override IOctreeManager OnCreateManager() {
        return new InstancingRenderableOctreeManager();
    }
}
