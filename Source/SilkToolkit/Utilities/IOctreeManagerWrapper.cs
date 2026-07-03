using HelixToolkit.SharpDX.Core;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public interface IOctreeManagerWrapper {
    /// <summary>
    ///     Gets the octree.
    /// </summary>
    /// <value>
    ///     The octree.
    /// </value>
    IOctreeBasic Octree { get; }

    /// <summary>
    ///     Gets the manager.
    /// </summary>
    /// <value>
    ///     The manager.
    /// </value>
    IOctreeManager Manager { get; }
}
