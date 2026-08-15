/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Model.Camera;

/// <summary>
///     Camera movement modes.
/// </summary>
public enum CameraMode {
    /// <summary>
    ///     Orbits around a point (fixed target position, move closer target when zooming).
    /// </summary>
    Inspect,

    /// <summary>
    ///     Walk around (fixed camera position when rotating, move in camera direction when zooming).
    /// </summary>
    WalkAround,

    /// <summary>
    ///     Fixed camera target, change field of view when zooming.
    /// </summary>
    FixedPosition
}
