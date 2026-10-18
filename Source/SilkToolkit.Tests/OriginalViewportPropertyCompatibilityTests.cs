using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks original viewport property spellings without windows, templates or native rendering devices.</summary>
[Collection(WpfCollection.Name)]
public sealed class OriginalViewportPropertyCompatibilityTests {
    /// <summary>
    ///     Provides every spelling counterpart in the pinned original Viewport3DX.Properties.cs.
    /// </summary>
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

    /// <summary>
    ///     Preserves both registered names, unchanged render values and two-way WPF binding semantics.
    /// </summary>
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
        return StaThread.RunAsync(async () => {
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
            Assert.NotSame(currentDp, originalDp);
            Assert.Equal(currentName, currentDp.Name);
            Assert.Equal(originalName, originalDp.Name);
            Assert.Equal(currentProperty.PropertyType, originalProperty.PropertyType);
            Assert.Equal(originalProperty.PropertyType, originalDp.PropertyType);
            Assert.Equal(viewportType, originalDp.OwnerType);
            Assert.Equal(defaultValue, metadata.DefaultValue);
            Assert.Equal(defaultValue, currentDp.GetMetadata(viewportType).DefaultValue);
            var currentDescriptor = DependencyPropertyDescriptor.FromName(currentName, viewportType, viewportType);
            Assert.NotNull(currentDescriptor);
            Assert.Same(currentDp, currentDescriptor.DependencyProperty);
            Assert.NotNull(descriptor);
            Assert.Same(originalDp, descriptor.DependencyProperty);
            Assert.NotNull(schemaMember);
            Assert.Equal(originalProperty, schemaMember.UnderlyingMember);
            Assert.Equal(defaultValue, originalProperty.GetValue(source));
            Assert.Equal(defaultValue, currentProperty.GetValue(source));

            // Name-based WPF lookup must resolve the original name, rather than just a reflected CLR alias.
            var markup = "<hx:Viewport3DX xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                originalName + "=\"{Binding " + originalName + ", Mode=TwoWay}\" />";
            using var target = (Viewport3DX)XamlReader.Parse(markup);
            target.DataContext = source;
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            var expression = BindingOperations.GetBindingExpression(target, originalDp);
            Assert.NotNull(expression);
            originalProperty.SetValue(source, firstValue);
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(firstValue, currentProperty.GetValue(source));
            Assert.Equal(firstValue, originalProperty.GetValue(target));
            Assert.Equal(firstValue, currentProperty.GetValue(target));
            Assert.Equal(BindingStatus.Active, expression.Status);

            // The retained current wrapper synchronizes the original binding target.
            currentProperty.SetValue(source, secondValue);
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(secondValue, originalProperty.GetValue(target));
            Assert.Equal(secondValue, currentProperty.GetValue(target));
            target.SetCurrentValue(currentDp, firstValue);
            expression.UpdateSource();
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(firstValue, originalProperty.GetValue(source));
            Assert.Equal(firstValue, currentProperty.GetValue(source));

            // Current callers can still bind through the retained public DP field.
            BindingOperations.ClearBinding(target, originalDp);
            var currentExpression = BindingOperations.SetBinding(target, currentDp,
                new Binding(originalName) { Source = source });
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            currentProperty.SetValue(source, secondValue);
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(secondValue, originalProperty.GetValue(target));
            Assert.Equal(secondValue, currentProperty.GetValue(target));
            Assert.Equal(BindingStatus.Active, currentExpression.Status);

            // Current CLR literal XAML continues to work through its retained setter.
            var currentMarkup = "<hx:Viewport3DX xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                currentName + "=\"" + literal + "\" />";
            using var currentTarget = (Viewport3DX)XamlReader.Parse(currentMarkup);
            Assert.Equal(firstValue, originalProperty.GetValue(currentTarget));
            Assert.Equal(firstValue, currentProperty.GetValue(currentTarget));

            // Existing XAML consumers also retain name-based bindings through their current member spelling.
            var currentBindingMarkup =
                "<hx:Viewport3DX xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                currentName + "=\"{Binding " + originalName + ", Mode=TwoWay}\" />";
            using var currentBindingTarget = (Viewport3DX)XamlReader.Parse(currentBindingMarkup);
            currentBindingTarget.DataContext = source;
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            var currentNameExpression = BindingOperations.GetBindingExpression(currentBindingTarget, currentDp);
            Assert.NotNull(currentNameExpression);
            Assert.Equal(secondValue, currentProperty.GetValue(currentBindingTarget));
            originalProperty.SetValue(source, firstValue);
            await Dispatcher.Yield(DispatcherPriority.ContextIdle);
            Assert.Equal(firstValue, currentProperty.GetValue(currentBindingTarget));
            Assert.Equal(BindingStatus.Active, currentNameExpression.Status);

            // Exercise every source-name/target-name combination, preserving bindings on both target DPs.
            foreach (var sourceName in new[] { originalName, currentName }) {
                foreach (var targetName in new[] { originalName, currentName }) {
                    using var bindingSource = new Viewport3DX();
                    var targetDp = targetName == originalName ? originalDp : currentDp;
                    var otherDp = targetName == originalName ? currentDp : originalDp;
                    var bindingMarkup =
                        "<hx:Viewport3DX xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                        "xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\" " +
                        targetName + "=\"{Binding " + sourceName + ", Mode=TwoWay}\" />";
                    using var boundTarget = (Viewport3DX)XamlReader.Parse(bindingMarkup);
                    boundTarget.DataContext = bindingSource;
                    await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                    var targetBinding = BindingOperations.GetBindingExpression(boundTarget, targetDp);
                    Assert.NotNull(targetBinding);
                    var otherBinding = (BindingExpression)BindingOperations.SetBinding(boundTarget, otherDp,
                        new Binding(sourceName) { Source = bindingSource, Mode = BindingMode.TwoWay });
                    await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                    bindingSource.SetCurrentValue(currentDp, firstValue);
                    await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                    Assert.Equal(firstValue, boundTarget.GetValue(originalDp));
                    Assert.Equal(firstValue, boundTarget.GetValue(currentDp));
                    boundTarget.SetCurrentValue(otherDp, secondValue);
                    targetBinding.UpdateSource();
                    otherBinding.UpdateSource();
                    await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                    Assert.Equal(secondValue, bindingSource.GetValue(originalDp));
                    Assert.Equal(secondValue, bindingSource.GetValue(currentDp));
                    Assert.Equal(secondValue, boundTarget.GetValue(originalDp));
                    Assert.Equal(secondValue, boundTarget.GetValue(currentDp));
                    Assert.Same(targetBinding, BindingOperations.GetBindingExpression(boundTarget, targetDp));
                    Assert.Same(otherBinding, BindingOperations.GetBindingExpression(boundTarget, otherDp));
                    Assert.Equal(BindingStatus.Active, targetBinding.Status);
                    Assert.Equal(BindingStatus.Active, otherBinding.Status);
                    bindingSource.SetCurrentValue(originalDp, firstValue);
                    await Dispatcher.Yield(DispatcherPriority.ContextIdle);
                    Assert.Equal(firstValue, boundTarget.GetValue(originalDp));
                    Assert.Equal(firstValue, boundTarget.GetValue(currentDp));
                }
            }

            foreach (var name in new[] { originalName, currentName }) {
                var styleMarkup =
                    "<hx:Viewport3DX xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                    "xmlns:hx=\"http://helix-toolkit.org/wpf/SharpDX\"><hx:Viewport3DX.Style>" +
                    "<Style TargetType=\"{x:Type hx:Viewport3DX}\" " +
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
                    "<Setter Property=\"" + name + "\" Value=\"" + literal + "\" />" +
                    "</Style></hx:Viewport3DX.Style></hx:Viewport3DX>";
                using var styled = (Viewport3DX)XamlReader.Parse(styleMarkup);
                Assert.Equal(firstValue, styled.GetValue(originalDp));
                Assert.Equal(firstValue, styled.GetValue(currentDp));
            }

            // Each logical change notifies both registered names once; repeating the value is silent.
            using var observed = new CoercingViewport();
            var originalChanges = 0;
            var currentChanges = 0;
            EventHandler originalChanged = (_, _) => originalChanges++;
            EventHandler currentChanged = (_, _) => currentChanges++;
            descriptor.AddValueChanged(observed, originalChanged);
            currentDescriptor.AddValueChanged(observed, currentChanged);
            try {
                foreach (var changedDp in new[] { originalDp, currentDp }) {
                    observed.SetCurrentValue(currentDp, secondValue);
                    originalChanges = currentChanges = 0;
                    observed.CurrentChanges = 0;
                    observed.SetCurrentValue(changedDp, firstValue);
                    observed.SetCurrentValue(changedDp, firstValue);
                    Assert.Equal(firstValue, observed.GetValue(originalDp));
                    Assert.Equal(firstValue, observed.GetValue(currentDp));
                    Assert.Equal(1, originalChanges);
                    Assert.Equal(1, currentChanges);
                    Assert.Equal(1, observed.CurrentChanges);
                }

                // Current-side coercion remains authoritative when the original spelling is written.
                observed.CoercedValue = firstValue;
                observed.CurrentChanges = 0;
                observed.SetCurrentValue(originalDp, secondValue);
                Assert.Equal(firstValue, observed.GetValue(originalDp));
                Assert.Equal(firstValue, observed.GetValue(currentDp));
                Assert.Equal(0, observed.CurrentChanges);
                observed.SetCurrentValue(currentDp, secondValue);
                Assert.Equal(firstValue, observed.GetValue(originalDp));
                Assert.Equal(firstValue, observed.GetValue(currentDp));
                Assert.Equal(0, observed.CurrentChanges);
            } finally {
                descriptor.RemoveValueChanged(observed, originalChanged);
                currentDescriptor.RemoveValueChanged(observed, currentChanged);
            }

            using var coerced = new CoercingViewport { CoercedValue = firstValue };
            coerced.SetCurrentValue(originalDp, secondValue);
            Assert.Equal(firstValue, coerced.GetValue(originalDp));
            Assert.Equal(firstValue, coerced.GetValue(currentDp));
            Assert.Equal(1, coerced.CurrentChanges);
            coerced.SetCurrentValue(currentDp, secondValue);
            Assert.Equal(firstValue, coerced.GetValue(originalDp));
            Assert.Equal(firstValue, coerced.GetValue(currentDp));
            Assert.Equal(1, coerced.CurrentChanges);
        });
    }

    /// <summary>Observes current metadata callbacks and applies derived current-side coercion without rendering.</summary>
    private sealed class CoercingViewport : Viewport3DX {
        /// <summary>Installs one observer and coercion callback on each retained current property.</summary>
        static CoercingViewport() {
            foreach (var propertyCase in PropertyCases()) {
                var name = (string)propertyCase[1];
                var field = typeof(Viewport3DX).GetField(name + "Property", BindingFlags.Public | BindingFlags.Static)!;
                var property = (DependencyProperty)field.GetValue(null)!;
                property.OverrideMetadata(typeof(CoercingViewport), new PropertyMetadata(
                    property.DefaultMetadata.DefaultValue,
                    (owner, _) => ((CoercingViewport)owner).CurrentChanges++,
                    (owner, value) => ((CoercingViewport)owner).CoercedValue ?? value));
            }
        }

        /// <summary>Initializes an untemplated, headless viewport.</summary>
        public CoercingViewport() { }

        /// <summary>Gets or sets the number of effective current-side metadata changes.</summary>
        public int CurrentChanges { get; set; }

        /// <summary>Gets or sets the authoritative coerced value, or <see langword="null"/> to retain the proposed value.</summary>
        public object? CoercedValue { get; set; }
    }
}
