using System.Globalization;
using System.Windows;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Converters;
using Xunit;

namespace SilkToolkit.Tests {
    public sealed class ConverterTests {
        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(true, Visibility.Visible)]
        [InlineData(false, Visibility.Collapsed)]
        public void BoolConverterMapsVisibility(bool value, Visibility expected) {
            var converter = new BoolToVisibilityConverter();

            Assert.Equal(expected, converter.Convert(value, typeof(Visibility), null!, CultureInfo.InvariantCulture));
            Assert.Equal(value, converter.ConvertBack(expected, typeof(bool), null!, CultureInfo.InvariantCulture));
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(false, false, Visibility.Collapsed)]
        [InlineData(false, true, Visibility.Visible)]
        [InlineData(true, false, Visibility.Visible)]
        [InlineData(true, true, Visibility.Collapsed)]
        public void NotNullConverterHonorsInversion(bool inverted, bool hasValue, Visibility expected) {
            var converter = new NotNullToVisibilityConverter {Inverted = inverted};

            Assert.Equal(expected,
                         converter.Convert(hasValue ? new object() : null!,
                                           typeof(Visibility),
                                           null!,
                                           CultureInfo.InvariantCulture));
        }
    }
}
