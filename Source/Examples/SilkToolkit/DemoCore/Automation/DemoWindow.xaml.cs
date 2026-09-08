// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Windows;

/// <summary>
///     Standard demo shell: one <see cref="HelixToolkit.Wpf.SharpDX.Controls.Viewport3DX" /> with default lights,
///     automation wiring, and disposal. For new demos; existing windows are not rebuilt onto it.
/// </summary>
public partial class DemoWindow : Window {
    /// <summary>
    ///     The attached automation host.
    /// </summary>
    private DemoAutomationHost? automationHost;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DemoWindow" /> class.
    /// </summary>
    public DemoWindow() {
        InitializeComponent();
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }

    /// <summary>
    ///     Attaches REST/MCP automation. The data context must be a <see cref="BaseViewModel" /> with an effects manager.
    /// </summary>
    /// <param name="sceneHost">The optional scene adapter for model and scene operations.</param>
    /// <param name="options">The options.</param>
    /// <returns>The host.</returns>
    protected DemoAutomationHost AttachAutomation(
        IDemoSceneHost? sceneHost = null,
        DemoAutomationOptions? options = null) {
        if (DataContext is not BaseViewModel viewModel || viewModel.EffectsManager is null) {
            throw new InvalidOperationException("DemoWindow requires a BaseViewModel data context with an effects manager.");
        }

        automationHost = DemoBootstrapper.Attach(View, viewModel.EffectsManager, options, sceneHost);
        return automationHost;
    }
}
