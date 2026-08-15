// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Represents a composite Model3D.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Represents a composite Model3D.
/// </summary>
[ContentProperty("Children")]
public class CompositeModel3D : Element3D, IHitable, ISelectable, IMouse3D {
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(CompositeModel3D), new PropertyMetadata(false));

    // Using a DependencyProperty as the backing store for AlwasyHittable.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AlwasyHittableProperty =
        DependencyProperty.Register("AlwaysHittable",
                                    typeof(bool),
                                    typeof(CompositeModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                              if (d is CompositeModel3D model && model.SceneNode is { } node)
                                                                  node.AlwaysHittable = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeModel3D" /> class.
    /// </summary>
    public CompositeModel3D() {
        Children.CollectionChanged += ChildrenChanged;
        Loaded += GroupElement3D_Loaded;
    }


    /// <summary>
    ///     Gets the children.
    /// </summary>
    /// <value>
    ///     The children.
    /// </value>
    public ObservableElement3DCollection Children { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether [always hittable].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [always hittable]; otherwise, <c>false</c>.
    /// </value>
    public bool AlwaysHittable {
        get => (bool)GetValue(AlwasyHittableProperty);
        set => SetValue(AlwasyHittableProperty, value);
    }

    public bool IsSelected {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private void GroupElement3D_Loaded(object sender, RoutedEventArgs e) {
        foreach (var c in Children)
            if (c.Parent == this)
                RemoveLogicalChild(c);

        foreach (var c in Children)
            if (c.Parent == null)
                AddLogicalChild(c);
    }

    /// <summary>
    ///     Handles changes in the Children collection.
    /// </summary>
    /// <param name="sender">
    ///     The sender.
    /// </param>
    /// <param name="e">
    ///     The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.
    /// </param>
    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (SceneNode is not GroupNode node)
            return;

        switch (e.Action) {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                    foreach (Element3D item in e.OldItems) {
                        if (item.Parent == this) RemoveLogicalChild(item);
                        if (item.SceneNode is { } childNode)
                            node.RemoveChildNode(childNode);
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
                foreach (var item in Children) {
                    if (item.Parent == null) AddLogicalChild(item);
                    if (item.SceneNode is { } childNode)
                        node.AddChildNode(childNode);
                }

                break;
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems is null)
                    break;

                foreach (Element3D item in e.NewItems) {
                    if (item.Parent == null) AddLogicalChild(item);
                    if (item.SceneNode is { } childNode)
                        node.AddChildNode(childNode);
                }

                break;
            case NotifyCollectionChangedAction.Move:
                node.MoveChildNode(e.OldStartingIndex, e.NewStartingIndex);
                break;
        }
    }

    protected override SceneNode OnCreateSceneNode() => new GroupNode { AlwaysHittable = AlwaysHittable };
}
