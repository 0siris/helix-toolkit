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

namespace ExampleBrowser.Workitems.Workitem10044;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Example("Issue 10044", "Make SharpDX elements more XAML friendly.")]
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(Viewport, effectsManager);
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
