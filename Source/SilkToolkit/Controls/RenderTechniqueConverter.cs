using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.Wpf.SharpDX.Controls;

public class RenderTechniqueConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is KeyValuePair<string, IRenderTechnique> pair ? pair.Value : null;
}
