using System.Windows;
using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Provides original Helix property spellings over the existing viewport render state.</summary>
/// <remarks>
///     WPF resolves dependency properties by registered name, so the existing registrations use the original names.
///     Both public field spellings identify that single registration; current CLR wrappers remain available.
///     Current callers can bind through the retained dependency-property fields. Name-based WPF lookup uses the
///     original spelling, including XAML binding targets, style setters and dependency-property descriptors.
///     Alias fields follow their registrations in the same partial file to avoid cross-file initialization ordering.
/// </remarks>
public partial class Viewport3DX {
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
