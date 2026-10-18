using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX.Utilities;
using HelixToolkit.SharpDX.Core.Interface;

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
    IOctreeBasic? Octree { get; }

    /// <summary>
    ///     Gets the manager.
    /// </summary>
    /// <value>
    ///     The manager.
    /// </value>
    IOctreeManager Manager { get; }
}
