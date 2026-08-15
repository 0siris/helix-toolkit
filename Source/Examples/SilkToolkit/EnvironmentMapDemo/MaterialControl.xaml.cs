// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MaterialControl.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MaterialControl.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.Wpf.SharpDX.Extensions;

namespace EnvironmentMapDemo;

using System;
using System.Windows.Controls;
using System.Windows.Data;
using Color4 = Silk.NET.Maths.Vector4D<float>;

/// <summary>
/// Interaction logic for MaterialControl.xaml
/// </summary>
public partial class MaterialControl : UserControl {
    public MaterialControl() {
        InitializeComponent();
    }
}

public class ColorConverter : IValueConverter {
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        System.Globalization.CultureInfo culture
    ) {
        return value is Color4 c
            ? c.ToColor()
            : null;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        System.Globalization.CultureInfo culture
    ) {
        return value is System.Windows.Media.Color c
            ? c.ToColor4()
            : null;
    }
}