// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Workitem10044;

using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Extensions;

public class MainViewModel : BaseViewModel {
    public MainViewModel() {
        // titles
        Title = "Simple Demo (Workitem 10044)";
        SubTitle = "Please note that this scene is defined completely in XAML.";

        EffectsManager = new DefaultEffectsManager();
    }
}
