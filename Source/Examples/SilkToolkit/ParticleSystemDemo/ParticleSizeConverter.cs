using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ParticleSystemDemo;

public class ParticleSizeConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Size size
            ? size.Width * 100
            : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is double number
            ? new Size(number / 100, number / 100)
            : null;
    }
}