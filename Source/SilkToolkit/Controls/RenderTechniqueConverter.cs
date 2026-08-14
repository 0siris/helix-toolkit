using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

public class RenderTechniqueConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => ((KeyValuePair<string, IRenderTechnique>)value).Value;
}
