// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupElement3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning element and scene node.

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Supports both ItemsSource binding and Xaml children. Binds with ObservableElement3DCollection
/// </summary>
[ContentProperty("Children")]
public abstract class GroupElement3D : Element3D {
    /// <summary>
    ///     ItemsSource for binding to collection. Please use ObservableElement3DCollection for observable, otherwise may cause
    ///     memory leak.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource",
                                    typeof(IEnumerable<Element3D>),
                                    typeof(GroupElement3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is GroupElement3D group && group.IsAttached)
                                                                 group.OnItemsSourceChanged(
                                                                     e.NewValue as IEnumerable<Element3D>);
                                                         }));

    /// <summary>
    ///     Add octree manager to use octree hit test.
    /// </summary>
    public static readonly DependencyProperty OctreeManagerProperty = DependencyProperty.Register("OctreeManager",
        typeof(IOctreeManagerWrapper),
        typeof(GroupElement3D),
        new PropertyMetadata(null,
                             (s, e) => {
                                  var d = (GroupElement3D)s;
                                  if (e.OldValue != null) d.RemoveLogicalChild(e.OldValue);

                                  if (e.NewValue != null) d.AddLogicalChild(e.NewValue);
                                  var groupNode = (GroupNode)d.SceneNode;
                                  groupNode.OctreeManager =
                                      e.NewValue == null ? null : ((IOctreeManagerWrapper)e.NewValue).Manager;
                             }));

    // Using a DependencyProperty as the backing store for AlwaysHittable.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AlwaysHittableProperty =
        DependencyProperty.Register("AlwaysHittable",
                                    typeof(bool),
                                    typeof(GroupElement3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                              ((GroupElement3D)d).SceneNode.AlwaysHittable =
                                                                 (bool)e.NewValue;
                                                         }));

    private IEnumerable<Element3D>? itemsSourceInternal;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GroupElement3D" /> class.
    /// </summary>
    public GroupElement3D() {
        Loaded += GroupElement3D_Loaded;
        Children.CollectionChanged += Items_CollectionChanged;
        SceneNode.Attached += SceneNode_Attached;
        SceneNode.Detached += SceneNode_Detached;
    }


    /// <summary>
    ///     ItemsSource for binding to collection. Please use ObservableElement3DCollection for observable, otherwise may cause
    ///     memory leak.
    /// </summary>
    public IList<Element3D>? ItemsSource {
        get => (IList<Element3D>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IOctreeManagerWrapper? OctreeManager {
        get => (IOctreeManagerWrapper?)GetValue(OctreeManagerProperty);
        set => SetValue(OctreeManagerProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [always hittable].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [always hittable]; otherwise, <c>false</c>.
    /// </value>
    public bool AlwaysHittable {
        get => (bool)GetValue(AlwaysHittableProperty);
        set => SetValue(AlwaysHittableProperty, value);
    }

    private IOctreeBasic? Octree => ((GroupNode)SceneNode).OctreeManager?.Octree;

    /// <summary>
    ///     Gets the children.
    /// </summary>
    /// <value>
    ///     The children.
    /// </value>
    public ObservableElement3DCollection Children { get; } = [];

    private void SceneNode_Attached(object? sender, EventArgs e) {
        if (ItemsSource != null) OnItemsSourceChanged(ItemsSource);
    }

    private void SceneNode_Detached(object? sender, EventArgs e) {
        if (itemsSourceInternal != null) OnItemsSourceChanged(null);
    }

    private void GroupElement3D_Loaded(object? sender, RoutedEventArgs e) {
        foreach (var c in Children)
            if (c.Parent == this)
                RemoveLogicalChild(c);

        foreach (var c in Children)
            if (c.Parent == null)
                AddLogicalChild(c);
    }

    protected override SceneNode OnCreateSceneNode() {
        return new GroupNode { AlwaysHittable = AlwaysHittable };
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        var node = (GroupNode)SceneNode;
        switch (e.Action) {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                    foreach (Element3D item in e.OldItems) {
                        if (item.Parent == this) RemoveLogicalChild(item);
                        node.RemoveChildNode(item.SceneNode);
                    }

                break;
            case NotifyCollectionChangedAction.Reset:
                if (e.OldItems != null)
                    foreach (Element3D item in e.OldItems)
                        if (item.Parent == this)
                            RemoveLogicalChild(item);

                node.Clear();
                break;
        }

        switch (e.Action) {
            case NotifyCollectionChangedAction.Reset:
                if (sender is IList list)
                    foreach (Element3D item in list) {
                        if (item.Parent == null) AddLogicalChild(item);
                        node.AddChildNode(item.SceneNode);
                    }

                break;
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                foreach (Element3D item in e.NewItems) {
                    if (item.Parent == null) AddLogicalChild(item);
                    node.AddChildNode(item.SceneNode);
                }

                break;
            case NotifyCollectionChangedAction.Move:
                node.MoveChildNode(e.OldStartingIndex, e.NewStartingIndex);
                break;
        }
    }

    private void OnItemsSourceChanged(IEnumerable<Element3D>? itemsSource) {
        if (itemsSourceInternal == itemsSource) return;
        if (itemsSourceInternal != null)
            if (itemsSourceInternal is INotifyCollectionChanged s)
                s.CollectionChanged -= S_CollectionChanged;

        //Must not use both ItemsSource and Children at the same time
        if (itemsSourceInternal == null && Children.Count > 0 && itemsSource != null)
            throw new InvalidOperationException("Children must be empty before using ItemsSource");
        Children.Clear();
        itemsSourceInternal = itemsSource;
        if (itemsSourceInternal != null) {
            if (itemsSourceInternal is INotifyCollectionChanged s) s.CollectionChanged += S_CollectionChanged;
            foreach (var item in itemsSourceInternal) Children.Add(item);
        }
    }

    private void S_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        switch (e.Action) {
            case NotifyCollectionChangedAction.Reset:
                Children.Clear();
                break;
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                foreach (Element3D item in e.OldItems) Children.Remove(item);
                break;
        }

        switch (e.Action) {
            case NotifyCollectionChangedAction.Reset:
                foreach (var item in itemsSourceInternal!) Children.Add(item);
                break;
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                foreach (Element3D item in e.NewItems) Children.Add(item);
                break;
            case NotifyCollectionChangedAction.Move:
                Children.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
        }
    }
}
