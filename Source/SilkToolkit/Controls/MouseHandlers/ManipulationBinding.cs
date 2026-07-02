// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManipulationBinding.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Binds a <see cref="ManipulationGesture"/> to a <see cref="RoutedCommand"/> (or another <see cref="ICommand"/>
//   implementation).
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows.Input;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Binds a <see cref="ManipulationGesture" /> to a <see cref="RoutedCommand" /> (or another <see cref="ICommand" />
///     implementation).
/// </summary>
public class ManipulationBinding : InputBinding
{
    public ManipulationBinding()
    {
    }

    public ManipulationBinding(ICommand command, ManipulationGesture gesture)
        : base(command, gesture)
    {
    }

    public int FingerCount => ((ManipulationGesture) Gesture).FingerCount;

    [TypeConverter(typeof(ManipulationGestureConverter))]
    public override InputGesture Gesture
    {
        get => base.Gesture;

        set
        {
            var oldGesture = Gesture;
            if (value is ManipulationGesture newGesture)
            {
                if (oldGesture != newGesture) base.Gesture = newGesture;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }
    }
}