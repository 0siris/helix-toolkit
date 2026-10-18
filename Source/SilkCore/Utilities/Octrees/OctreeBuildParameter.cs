using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public sealed class OctreeBuildParameter : ObservableObject {
    /// <summary>
    /// </summary>
    public OctreeBuildParameter() { }

    /// <summary>
    /// </summary>
    /// <param name="minSize"></param>
    public OctreeBuildParameter(float minSize) => MinimumOctantSize = Math.Max(0, minSize);

    /// <summary>
    /// </summary>
    /// <param name="autoDeleteIfEmpty"></param>
    public OctreeBuildParameter(bool autoDeleteIfEmpty) => AutoDeleteIfEmpty = autoDeleteIfEmpty;

    /// <summary>
    /// </summary>
    /// <param name="minSize"></param>
    /// <param name="autoDeleteIfEmpty"></param>
    public OctreeBuildParameter(int minSize, bool autoDeleteIfEmpty) : this(minSize)
        => AutoDeleteIfEmpty = autoDeleteIfEmpty;

    /// <summary>
    ///     Minimum Octant size.
    /// </summary>
    public float MinimumOctantSize {
        get;
        set => Set(ref field, value);
    } = 1f;

    /// <summary>
    ///     Minimum object in each octant to start splitting into smaller octant during build
    /// </summary>
    public int MinObjectSizeToSplit {
        get;
        set => Set(ref field, value);
    } = 2;

    /// <summary>
    ///     Delete empty octant automatically
    /// </summary>
    public bool AutoDeleteIfEmpty {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Generate cube octants instead of rectangle octants
    /// </summary>
    public bool Cubify {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Record hit path bounding boxes for debugging or display purpose only
    /// </summary>
    public bool RecordHitPathBoundingBoxes { get; set; }

    /// <summary>
    ///     Use parallel tree traversal to build the octree
    /// </summary>
    public bool EnableParallelBuild { get; set; }
}
