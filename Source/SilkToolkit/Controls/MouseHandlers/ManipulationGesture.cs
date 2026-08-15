// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManipulationGesture.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Defines a touch input gesture that can be used to invoke a command.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace HelixToolkit.Wpf.SharpDX.Controls.MouseHandlers;

/// <summary>
///     Defines a touch input gesture that can be used to invoke a command.
/// </summary>
[TypeConverter(typeof(ManipulationGestureConverter))]
public class ManipulationGesture : InputGesture {
    public ManipulationGesture(ManipulationAction manipulationAction) {
        ManipulationAction = manipulationAction;
        FingerCount = manipulationAction.FingerCount();
    }

    public ManipulationAction ManipulationAction { get; }

    public int FingerCount { get; }

    public override bool Matches(object targetElement, InputEventArgs inputEventArgs) {
        if (inputEventArgs is ManipulationDeltaEventArgs mdea) {
            // mdea.CumulativeManipulation.Translation.Length ...
            var manipulatorsCount = mdea.Manipulators.Count();
            return manipulatorsCount == FingerCount;
        }

        return false;
    }
}
