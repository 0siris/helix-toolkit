using System;
using System.Windows;
using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Provides original Helix property spellings over the existing viewport render state.</summary>
/// <remarks>
///     Each spelling has its own registered WPF dependency property. Changes flow through the retained current
///     property, preserving its render callback and both binding expressions with SetCurrentValue.
///     Pair fields precede both public fields in the same partial file to keep static initialization ordered.
/// </remarks>
public partial class Viewport3DX {
    /// <summary>Registers two WPF names synchronized through the existing current property metadata.</summary>
    /// <param name="currentName">The retained current property name.</param>
    /// <param name="originalName">The original Helix property name.</param>
    /// <param name="propertyType">The shared property value type.</param>
    /// <param name="metadata">The unchanged default, coercion and render-state callback for the current property.</param>
    /// <returns>The distinct current and original registrations.</returns>
    private static (DependencyProperty Current, DependencyProperty Original) RegisterPropertyPair(
        string currentName, string originalName, Type propertyType, PropertyMetadata metadata) {
        DependencyProperty original = null!;
        var changed = metadata.PropertyChangedCallback;
        metadata.PropertyChangedCallback = (owner, args) => {
            if (!Equals(owner.GetValue(original), args.NewValue))
                owner.SetCurrentValue(original, args.NewValue);
            changed?.Invoke(owner, args);
        };
        var current = DependencyProperty.Register(currentName, propertyType, typeof(Viewport3DX), metadata);
        original = DependencyProperty.Register(originalName, propertyType, typeof(Viewport3DX),
            new PropertyMetadata(metadata.DefaultValue, (owner, args) => {
                if (!Equals(owner.GetValue(current), args.NewValue))
                    owner.SetCurrentValue(current, args.NewValue);
                // Reflect the authoritative current value if its metadata coerced the requested original value.
                var effectiveValue = owner.GetValue(current);
                if (!Equals(owner.GetValue(original), effectiveValue))
                    owner.SetCurrentValue(original, effectiveValue);
            }, (owner, value) => {
                var coerce = current.GetMetadata(owner.GetType()).CoerceValueCallback;
                return coerce is null ? value : coerce(owner, value);
            }));
        return (current, original);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="Msaa"/>.</summary>
    public MSAALevel MSAA {
        get => (MSAALevel)GetValue(MSAAProperty);
        set => SetValue(MSAAProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="FxaaLevel"/>.</summary>
    public FXAALevel FXAALevel {
        get => (FXAALevel)GetValue(FXAALevelProperty);
        set => SetValue(FXAALevelProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="OitRenderMode"/>.</summary>
    public OITRenderType OITRenderMode {
        get => (OITRenderType)GetValue(OITRenderModeProperty);
        set => SetValue(OITRenderModeProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="OitWeightPower"/>.</summary>
    public double OITWeightPower {
        get => (double)GetValue(OITWeightPowerProperty);
        set => SetValue(OITWeightPowerProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="OitWeightDepthSlope"/>.</summary>
    public double OITWeightDepthSlope {
        get => (double)GetValue(OITWeightDepthSlopeProperty);
        set => SetValue(OITWeightDepthSlopeProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="OitWeightMode"/>.</summary>
    public OITWeightMode OITWeightMode {
        get => (OITWeightMode)GetValue(OITWeightModeProperty);
        set => SetValue(OITWeightModeProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="OitDepthPeelingIteration"/>.</summary>
    public int OITDepthPeelingIteration {
        get => (int)GetValue(OITDepthPeelingIterationProperty);
        set => SetValue(OITDepthPeelingIterationProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="EnableSsao"/>.</summary>
    public bool EnableSSAO {
        get => (bool)GetValue(EnableSSAOProperty);
        set => SetValue(EnableSSAOProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="SsaoSamplingRadius"/>.</summary>
    public double SSAOSamplingRadius {
        get => (double)GetValue(SSAOSamplingRadiusProperty);
        set => SetValue(SSAOSamplingRadiusProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="SsaoIntensity"/>.</summary>
    public double SSAOIntensity {
        get => (double)GetValue(SSAOIntensityProperty);
        set => SetValue(SSAOIntensityProperty, value);
    }

    /// <summary>Gets or sets the original Helix spelling of <see cref="SsaoQuality"/>.</summary>
    public SSAOQuality SSAOQuality {
        get => (SSAOQuality)GetValue(SSAOQualityProperty);
        set => SetValue(SSAOQualityProperty, value);
    }
}
