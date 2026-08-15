// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TargetSymbolAdorner.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   A Target symbol adorner. This is shown in the HelixViewport3D when manipulating the camera with the mouse.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     A Target symbol adorner. This is shown in the HelixViewport3D when manipulating the camera with the mouse.
/// </summary>
/// <remarks>
///     Inspired by Google Earth...
/// </remarks>
public class TargetSymbolAdorner : Adorner {
    /// <summary>
    ///     Initializes a new instance of the <see cref="TargetSymbolAdorner" /> class.
    /// </summary>
    /// <param name="adornedElement">
    ///     The adorned element.
    /// </param>
    /// <param name="position">
    ///     The position.
    /// </param>
    public TargetSymbolAdorner(UIElement adornedElement, Point position)
        : base(adornedElement) {
        Position = position;
    }

    /// <summary>
    ///     Gets or sets the position.
    /// </summary>
    /// <value>The position.</value>
    public Point Position { get; set; }

    /// <summary>
    ///     Called when rendering.
    /// </summary>
    /// <param name="dc">
    ///     The drawing context.
    /// </param>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules",
                     "SA1407:ArithmeticExpressionsMustDeclarePrecedence",
                     Justification = "Reviewed. Suppression is OK here.")]
    protected override void OnRender(DrawingContext dc) {
        var lightBrush = new WpfSolidColorBrush(Colors.LightGray);
        var darkBrush = new WpfSolidColorBrush(Colors.Black);
        lightBrush.Opacity = 0.4;
        darkBrush.Opacity = 0.1;

        double t1 = 6; // thickness of dark circle pen
        double t2 = 2; // thickness of light pen (circle, arcs, segments)
        double d = 0;  // distance from light circle to segments
        double l = 10; // length of segments
        var r = 20.0;  // radius of light circle

        var r1 = r - (t1 + t2) / 2;
        var r2 = r + l;
        var r3 = r + t2 / 2 + d;
        var r4 = (r + r2) / 2;

        var darkPen = new Pen(darkBrush, t1);
        var lightPen = new Pen(lightBrush, t2);

        dc.DrawEllipse(null, lightPen, Position, r, r);
        dc.DrawEllipse(null, darkPen, Position, r1, r1);
        dc.DrawArc(null, lightPen, Position, 10, 80, r4, r4);
        dc.DrawArc(null, lightPen, Position, 100, 170, r4, r4);
        dc.DrawArc(null, lightPen, Position, 190, 260, r4, r4);
        dc.DrawArc(null, lightPen, Position, 280, 350, r4, r4);

        dc.DrawLine(lightPen,
                    new Point(Position.X, Position.Y - r2),
                    new Point(Position.X, Position.Y - r3));
        dc.DrawLine(lightPen,
                    new Point(Position.X, Position.Y + r2),
                    new Point(Position.X, Position.Y + r3));
        dc.DrawLine(lightPen,
                    new Point(Position.X - r2, Position.Y),
                    new Point(Position.X - r3, Position.Y));
        dc.DrawLine(lightPen,
                    new Point(Position.X + r2, Position.Y),
                    new Point(Position.X + r3, Position.Y));
    }
}
