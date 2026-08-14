// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Workitem10053;

using System;
using System.ComponentModel;
using System.Windows;
using DemoCore;

public class MainViewModel : BaseViewModel {
    public MainViewModel() {
        // titles
        Title = "Simple Demo (Workitem 10053)";
        SubTitle = "ManipulationBindings: Pan-Rotate, TwoFingerPan-Pan, Pinch-Zoom";
        // old issue: this.SubTitle = "You can pan, rotate and zoom via touch.";
        PropertyChanged += OnPropertyChanged;
        EffectsManager = new DefaultEffectsManager();
    }

    /// <summary>
    /// Gets or sets the render exception.
    /// </summary>
    public Exception RenderException {
        get;

        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the viewport message.
    /// </summary>
    public string ViewportMessage {
        get;

        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Handles exceptions at the rendering subsystem.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    public void HandleRenderException(object sender, RelayExceptionEventArgs e) {
        if (e.Exception != null) {
            MessageBox.Show(e.Exception.ToString(), "RenderException");
        }
    }

    /// <summary>
    /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
        if ("RenderException".Equals(e.PropertyName)) {
            ViewportMessage = RenderException?.ToString();
        }
    }
}
