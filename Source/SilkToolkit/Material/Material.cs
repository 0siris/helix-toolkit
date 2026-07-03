/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX;

[DataContract]
public abstract class Material : Freezable {
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register("Name",
                                    typeof(string),
                                    typeof(Material),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             (d as Material).Core.Name = (string) e.NewValue;
                                                         }));

    private MaterialCore core;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Material" /> class.
    /// </summary>
    public Material() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Material" /> class.
    /// </summary>
    /// <param name="core">The core.</param>
    public Material(MaterialCore core) {
        this.core = core;
        Name = core.Name;
    }

    public MaterialCore Core {
        get {
            if (core == null) core = OnCreateCore();
            return core;
        }
    }

    public string Name {
        get => (string) GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public override string ToString() {
        return Name;
    }

    protected abstract MaterialCore OnCreateCore();

    public static implicit operator MaterialCore(Material m) {
        return m?.Core;
    }
}
