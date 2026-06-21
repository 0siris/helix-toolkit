// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CanvasMock.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Render;
using HelixToolkit.Wpf.SharpDX.Utilities;

namespace HelixToolkit.Wpf.SharpDX.Tests.Controls
{
    class CanvasMock : IRenderCanvas
    {
        public IRenderHost RenderHost { private set; get; } = new DefaultRenderHost();
        public double DpiScale
        {
            set; get;
        }
        public bool EnableDpiScale
        {
            set; get;
        }

        public event EventHandler<RelayExceptionEventArgs> ExceptionOccurred;

        public CanvasMock()
        {
            RenderHost.EffectsManager = new DefaultEffectsManager();
        }
    }
}
