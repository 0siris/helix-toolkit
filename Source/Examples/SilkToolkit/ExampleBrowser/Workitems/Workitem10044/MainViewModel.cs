// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DemoCore;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace ExampleBrowser.Workitems.Workitem10044;

public class MainViewModel : BaseViewModel {
    public MainViewModel() {
        // titles
        Title = "Simple Demo (Workitem 10044)";
        SubTitle = "Please note that this scene is defined completely in XAML.";

        EffectsManager = new DefaultEffectsManager();
    }
}
