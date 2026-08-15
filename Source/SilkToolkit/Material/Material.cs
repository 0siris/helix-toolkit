/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;

namespace HelixToolkit.Wpf.SharpDX.Material;

[DataContract]
public abstract class Material : Freezable {
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register("Name",
                                    typeof(string),
                                    typeof(Material),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is Material material && e.NewValue is string name)
                                                                 material.Core.Name = name;
                                                         }));

    /// <summary>
    ///     Initializes a new instance of the <see cref="Material" /> class.
    /// </summary>
    public Material() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Material" /> class.
    /// </summary>
    /// <param name="core">The core.</param>
    public Material(MaterialCore core) {
        Core = core;
        Name = core.Name;
    }

    public MaterialCore Core {
        get {
            field ??= OnCreateCore();
            return field;
        }
    }

    public string Name {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public override string ToString() => Name;

    protected abstract MaterialCore OnCreateCore();

    public static implicit operator MaterialCore?(Material? m) => m?.Core;
}
