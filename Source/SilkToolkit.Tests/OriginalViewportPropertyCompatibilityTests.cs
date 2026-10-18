using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks original viewport property spellings without windows, templates or native rendering devices.</summary>
[Collection(WpfCollection.Name)]
public sealed class OriginalViewportPropertyCompatibilityTests {
    /// <summary>Provides every spelling counterpart in the pinned original Viewport3DX.Properties.cs.</summary>
    /// <returns>Original/current names, unchanged defaults, two distinct values and a XAML literal.</returns>
    public static IEnumerable<object[]> PropertyCases() {
        yield return ["MSAA", "Msaa", MSAALevel.Disable, MSAALevel.Four, MSAALevel.Two, "Four"];
        yield return ["FXAALevel", "FxaaLevel", FXAALevel.None, FXAALevel.High, FXAALevel.Low, "High"];
        yield return ["OITRenderMode", "OitRenderMode", OITRenderType.DepthPeeling,
            OITRenderType.SinglePassWeighted, OITRenderType.None, "SinglePassWeighted"];
        yield return ["OITWeightPower", "OitWeightPower", 3.0, 2.0, 4.0, "2"];
        yield return ["OITWeightDepthSlope", "OitWeightDepthSlope", 1.0, 2.0, 3.0, "2"];
        yield return ["OITWeightMode", "OitWeightMode", OITWeightMode.Linear1,
            OITWeightMode.Linear2, OITWeightMode.NonLinear, "Linear2"];
        yield return ["OITDepthPeelingIteration", "OitDepthPeelingIteration", 4, 6, 8, "6"];
        yield return ["EnableSSAO", "EnableSsao", false, true, false, "True"];
        yield return ["SSAOSamplingRadius", "SsaoSamplingRadius", 0.5, 0.75, 0.25, "0.75"];
        yield return ["SSAOIntensity", "SsaoIntensity", 1.0, 1.5, 2.0, "1.5"];
        yield return ["SSAOQuality", "SsaoQuality", SSAOQuality.Low, SSAOQuality.High, SSAOQuality.Low, "High"];
    }

    /// <summary>Preserves one DP, unchanged defaults, CLR access and original WPF XAML binding semantics.</summary>
    /// <param name="originalName">The original Helix member spelling.</param>
    /// <param name="currentName">The retained current member spelling.</param>
    /// <param name="defaultValue">The unchanged existing render default.</param>
    /// <param name="firstValue">The first nondefault target value.</param>
    /// <param name="secondValue">The subsequent target value.</param>
    /// <param name="literal">The first value expressed in XAML.</param>
    /// <returns>The headless STA check.</returns>
    [Theory]
    [MemberData(nameof(PropertyCases))]
    [Trait("Category", "Wpf")]
    public Task OriginalPropertyContractTest(string originalName, string currentName, object defaultValue,
        object firstValue, object secondValue, string literal) {
        return StaThread.RunAsync(() => {
            using var source = new Viewport3DX();
            var viewportType = typeof(Viewport3DX);
            var originalField = viewportType.GetField(originalName + "Property", BindingFlags.Public | BindingFlags.Static);
            var currentField = viewportType.GetField(currentName + "Property", BindingFlags.Public | BindingFlags.Static);
            var originalProperty = viewportType.GetProperty(originalName);
            var currentProperty = viewportType.GetProperty(currentName);
            Assert.NotNull(originalField);
            Assert.NotNull(currentField);
            Assert.NotNull(originalProperty);
            Assert.NotNull(currentProperty);
            var originalDp = Assert.IsType<DependencyProperty>(originalField.GetValue(null));
            var currentDp = Assert.IsType<DependencyProperty>(currentField.GetValue(null));
            var descriptor = DependencyPropertyDescriptor.FromName(originalName, viewportType, viewportType);
            var metadata = originalDp.GetMetadata(viewportType);
            var schemaMember = XamlReader.GetWpfSchemaContext().GetXamlType(viewportType).GetMember(originalName);
            Assert.Same(currentDp, originalDp);
            Assert.Equal(originalName, originalDp.Name);
            Assert.Equal(currentProperty.PropertyType, originalProperty.PropertyType);
            Assert.Equal(originalProperty.PropertyType, originalDp.PropertyType);
            Assert.Equal(viewportType, originalDp.OwnerType);
            Assert.Equal(defaultValue, metadata.DefaultValue);
            Assert.Same(metadata, currentDp.GetMetadata(viewportType));
            Assert.NotNull(descriptor);
            Assert.Same(originalDp, descriptor.DependencyProperty);
            Assert.NotNull(schemaMember);
            Assert.Equal(originalProperty, schemaMember.UnderlyingMember);
            Assert.Equal(defaultValue, originalProperty.GetValue(source));
            Assert.Equal(defaultValue, currentProperty.GetValue(source));

            // Name-based WPF lookup must resolve the original name, rather than just a reflected CLR alias.
            var markup = "<hx:Viewport3DX xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                originalName + "=\"{Binding " + originalName + ", Mode=TwoWay}\" />";
            using var target = (Viewport3DX)XamlReader.Parse(markup);
            target.DataContext = source;
            var expression = BindingOperations.GetBindingExpression(target, originalDp);
            Assert.NotNull(expression);
            originalProperty.SetValue(source, firstValue);
            Assert.Equal(firstValue, currentProperty.GetValue(source));
            Assert.Equal(firstValue, originalProperty.GetValue(target));
            Assert.Equal(firstValue, currentProperty.GetValue(target));
            Assert.Equal(BindingStatus.Active, expression.Status);

            // The retained current wrapper updates the same DP and therefore the original binding target.
            currentProperty.SetValue(source, secondValue);
            Assert.Equal(secondValue, originalProperty.GetValue(target));
            Assert.Equal(secondValue, currentProperty.GetValue(target));
            target.SetCurrentValue(currentDp, firstValue);
            expression.UpdateSource();
            Assert.Equal(firstValue, originalProperty.GetValue(source));
            Assert.Equal(firstValue, currentProperty.GetValue(source));

            // Current callers can still bind through the retained public DP field.
            BindingOperations.ClearBinding(target, originalDp);
            var currentExpression = BindingOperations.SetBinding(target, currentDp,
                new Binding(originalName) { Source = source });
            currentProperty.SetValue(source, secondValue);
            Assert.Equal(secondValue, originalProperty.GetValue(target));
            Assert.Equal(secondValue, currentProperty.GetValue(target));
            Assert.Equal(BindingStatus.Active, currentExpression.Status);

            // Current CLR literal XAML continues to work through its retained setter.
            var currentMarkup = "<hx:Viewport3DX xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                currentName + "=\"" + literal + "\" />";
            using var currentTarget = (Viewport3DX)XamlReader.Parse(currentMarkup);
            Assert.Equal(firstValue, originalProperty.GetValue(currentTarget));
            Assert.Equal(firstValue, currentProperty.GetValue(currentTarget));
        });
    }
}
