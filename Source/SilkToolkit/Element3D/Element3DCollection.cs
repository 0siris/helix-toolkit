// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Element3DCollection.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a collection of Element3D.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
///     Provides an observable collection of Element3D.
/// </summary>
public class ObservableElement3DCollection : ObservableCollection<Model.Elements3D.AbstractElements3D.Element3D> {
    protected override void ClearItems() {
        CheckReentrancy();
        var items = Items.ToArray();
        base.ClearItems();
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, -1));
    }
}

public class ObservableElement2DCollection : ObservableCollection<Model.Elements2D.Abstract.Element2D> {
    protected override void ClearItems() {
        CheckReentrancy();
        var items = Items.ToArray();
        base.ClearItems();
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, -1));
    }
}
