// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutomationModels.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

/// <summary>
///     Scene statistics for REST and MCP.
/// </summary>
/// <param name="TriangleCount">Total visible triangles in the viewport.</param>
/// <param name="BoundsMin">Scene bounds minimum as [x, y, z].</param>
/// <param name="BoundsMax">Scene bounds maximum as [x, y, z].</param>
/// <param name="RenderableCount">Number of renderable scene nodes.</param>
public sealed record SceneStats(int TriangleCount, double[] BoundsMin, double[] BoundsMax, int RenderableCount);

/// <summary>
///     Model load request for REST and MCP.
/// </summary>
/// <param name="Path">Absolute or demo-relative model file path.</param>
public sealed record ModelLoadRequest(string Path);

/// <summary>
///     Model load result for REST and MCP.
/// </summary>
/// <param name="Name">File name of the loaded model.</param>
/// <param name="Triangles">Total visible triangles after loading.</param>
/// <param name="BoundsMin">Scene bounds minimum as [x, y, z].</param>
/// <param name="BoundsMax">Scene bounds maximum as [x, y, z].</param>
public sealed record ModelLoadResult(string Name, int Triangles, double[] BoundsMin, double[] BoundsMax);

/// <summary>
///     Demo status for REST and MCP.
/// </summary>
/// <param name="Demo">Demo name.</param>
/// <param name="ViewportWidth">Viewport width in device-independent units.</param>
/// <param name="ViewportHeight">Viewport height in device-independent units.</param>
/// <param name="CameraType">Active camera type literal.</param>
/// <param name="TriangleCount">Total visible triangles in the viewport.</param>
/// <param name="Mcp">Whether the MCP endpoint is enabled.</param>
public sealed record DemoStatus(
    string Demo,
    double ViewportWidth,
    double ViewportHeight,
    string CameraType,
    int TriangleCount,
    bool Mcp);

/// <summary>
///     Rendered screenshot for REST and MCP.
/// </summary>
/// <param name="Png">PNG-encoded bitmap bytes.</param>
/// <param name="Width">Bitmap width in pixels.</param>
/// <param name="Height">Bitmap height in pixels.</param>
public sealed record RenderedImage(byte[] Png, int Width, int Height);

/// <summary>
///     Zoom-extents request body (all fields optional).
/// </summary>
/// <param name="AnimationTimeMs">Camera animation time in milliseconds.</param>
public sealed record ZoomExtentsRequest(double AnimationTimeMs = 0);

/// <summary>
///     Single pick hit for REST and MCP (nearest first).
/// </summary>
/// <param name="NodeId">Node id, or null when the hit unwraps to no scene node.</param>
/// <param name="NodeName">Node name, or null.</param>
/// <param name="ModelType">CLR type name of the hit node, or null.</param>
/// <param name="Distance">Hit distance.</param>
/// <param name="Point">Hit point as [x, y, z].</param>
/// <param name="Normal">Hit normal as [x, y, z].</param>
/// <param name="Triangle">Triangle indices, or null.</param>
public sealed record PickHit(
    Guid? NodeId,
    string? NodeName,
    string? ModelType,
    double Distance,
    double[] Point,
    double[] Normal,
    int[]? Triangle);

/// <summary>
///     Render request body for picking (all fields optional except coordinates).
/// </summary>
/// <param name="X">Viewport-relative x in device-independent units.</param>
/// <param name="Y">Viewport-relative y in device-independent units.</param>
/// <param name="MaxHits">Maximum hit count.</param>
public sealed record PickRequest(double X, double Y, int MaxHits = 5);

/// <summary>
///     Render info for REST and MCP.
/// </summary>
/// <param name="Fps">Frames per second (exponential moving average, 0 before first frames).</param>
/// <param name="FrameMs">Average frame interval in milliseconds.</param>
/// <param name="Triangles">Total visible triangles in the viewport.</param>
/// <param name="Technique">Active viewport render technique name, or null.</param>
/// <param name="Techniques">Known technique names.</param>
public sealed record RenderInfo(double Fps, double FrameMs, int Triangles, string? Technique, string[] Techniques);

/// <summary>
///     Render technique request body.
/// </summary>
/// <param name="Name">Technique name.</param>
public sealed record TechniqueRequest(string Name);

/// <summary>
///     Visibility request body.
/// </summary>
/// <param name="Visible">The visibility.</param>
public sealed record VisibilityRequest(bool Visible);

/// <summary>
///     Log level request body.
/// </summary>
/// <param name="Level">verbose|debug|info|warn|error|fatal.</param>
public sealed record LogLevelRequest(string Level);
