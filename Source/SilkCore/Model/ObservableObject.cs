/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Model;
public abstract class ObservableObject : INotifyPropertyChanged {
    public bool DisablePropertyChangedEvent {
        get;
        set {
            if (field == value) return;
            field = value;
            RaisePropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = StringHelper.EmptyStr) {
        if (!DisablePropertyChangedEvent)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void RaisePropertyChanged(PropertyChangedEventArgs args) {
        if (!DisablePropertyChangedEvent)
            PropertyChanged?.Invoke(this, args);
    }

    protected bool Set<T>(
        ref T backingField,
        T value,
        [CallerMemberName] string propertyName = StringHelper.EmptyStr
    ) {
        if (EqualityComparer<T>.Default.Equals(backingField, value)) return false;

        backingField = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected bool Set<T>(
        ref T backingField,
        T value,
        bool raisePropertyChanged,
        [CallerMemberName] string propertyName = StringHelper.EmptyStr
    ) {
        if (EqualityComparer<T>.Default.Equals(backingField, value)) return false;

        backingField = value;
        if (raisePropertyChanged) RaisePropertyChanged(propertyName);
        return true;
    }
}
