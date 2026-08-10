using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HelixToolkit.SharpDX.Core.Utilities;

namespace VolumeRendering;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    private NvOptimusEnabler optEnabler = new NvOptimusEnabler();
}
