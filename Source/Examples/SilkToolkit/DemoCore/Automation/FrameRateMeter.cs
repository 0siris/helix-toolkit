// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FrameRateMeter.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Diagnostics;

/// <summary>
///     Pure frame-rate accumulator over render timestamps. No WPF dependency, unit-testable.
/// </summary>
public sealed class FrameRateMeter {
    /// <summary>
    ///     EMA smoothing factor.
    /// </summary>
    private const double Alpha = 0.1;

    /// <summary>
    ///     Ticks per millisecond.
    /// </summary>
    private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000.0;

    /// <summary>
    ///     Last tick timestamp. Null before the first tick.
    /// </summary>
    private long? lastTicks;

    /// <summary>
    ///     Smoothed frame interval in milliseconds.
    /// </summary>
    private double frameMs;

    /// <summary>
    ///     Gets the frames per second (0 before the second tick).
    /// </summary>
    public double Fps => frameMs > 0 ? 1000.0 / frameMs : 0;

    /// <summary>
    ///     Gets the smoothed frame interval in milliseconds.
    /// </summary>
    public double FrameMs => frameMs;

    /// <summary>
    ///     Records a frame timestamp. The first tick only anchors the meter.
    /// </summary>
    /// <param name="timestampTicks">Timestamp in <see cref="Stopwatch"/> ticks.</param>
    public void Tick(long timestampTicks) {
        if (lastTicks is { } previous) {
            var intervalMs = (timestampTicks - previous) / TicksPerMillisecond;
            frameMs = frameMs <= 0 ? intervalMs : frameMs + ((intervalMs - frameMs) * Alpha);
        }

        lastTicks = timestampTicks;
    }
}
