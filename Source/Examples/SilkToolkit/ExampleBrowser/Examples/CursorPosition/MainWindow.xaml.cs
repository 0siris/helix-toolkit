// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using DemoCore.Automation;

namespace ExampleBrowser.Examples.CursorPosition;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Example("CursorPosition", "Shows the position of the mouse cursor in the Viewport3DX.")]
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View1, effectsManager);
        }

        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}
