/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core;


[DataContract]
public abstract class Geometry3D : ObservableObject, IGuid {
    public const string VertexBuffer = "VertexBuffer";
    public const string TriangleBuffer = "TriangleBuffer";
    private static readonly PropertyChangedEventArgs VertexBufferPropChanged = new(VertexBuffer);
    private static readonly PropertyChangedEventArgs TriangleBufferPropChanged = new(TriangleBuffer);
    private static readonly PropertyChangedEventArgs ColorsPropChanged = new(nameof(Colors));
    private static readonly PropertyChangedEventArgs PositionPropChanged = new(nameof(Positions));
    private static readonly PropertyChangedEventArgs IndicesPropChanged = new(nameof(Indices));

    [DataMember]
    public Guid Guid { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Indices, can be triangle list, line list, etc.
    /// </summary>
    [DataMember]
    public IntCollection? Indices {
        get;
        set {
            if (Set(ref field, value, false)) {
                ClearOctree();
                RaisePropertyChanged(IndicesPropChanged);
            }
        }
    }

    private Vector3Collection? position;

    /// <summary>
    ///     Vertex Positions
    /// </summary>
    [DataMember]
    public Vector3Collection? Positions {
        get => position;
        set {
            if (Set(ref position, value, false)) {
                ClearOctree();
                RaisePropertyChanged(PositionPropChanged);
                UpdateBounds();
            }
        }
    }

    /// <summary>
    ///     Geometry AABB
    /// </summary>
    [IgnoreDataMember]
    public BoundingBox Bound {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Geometry Bounding Sphere
    /// </summary>
    [IgnoreDataMember]
    public BoundingSphere BoundingSphere {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Vertex Color
    /// </summary>
    [DataMember]
    public Color4Collection? Colors {
        get;
        set {
            if (Set(ref field, value, false)) 
                RaisePropertyChanged(ColorsPropChanged);
        }
    }

    /// <summary>
    ///     TO use Octree during hit test to improve hit performance, please call UpdateOctree after model created.
    /// </summary>
    public IOctreeBasic? Octree { get; private set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [octree dirty], needs update.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [octree dirty]; otherwise, <c>false</c>.
    /// </value>
    public bool OctreeDirty { get; private set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is dynamic. Must be set before passing to GeometryModel3D.
    ///     <para>When set to true, the internal vertex/index buffer will be created using dynamic buffer.</para>
    ///     <para>Default is false, which is using immutable.</para>
    ///     <para>
    ///         Dynamic buffer is useful if user streaming similar sizes of Vertices/Indices into this geometry, this will
    ///         avoid unnecessary buffer creation and reuse the existing dynamic buffer if the max size less than the size of
    ///         existing buffer.
    ///     </para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is dynamic; otherwise, <c>false</c>.
    /// </value>
    public bool IsDynamic { get; set; }

    /// <summary>
    ///     The pre defined vertex count. Only used when <see cref="IsDynamic" /> = true.
    ///     <para>The pre define vertex count allows user to initialize a dynamic buffer with a minimum pre-define size.</para>
    ///     <para>
    ///         Example: If the vertex count increments from 0 to around 3000 during vertex array streaming,
    ///         pre-define a size of 3000 for this geometry allows the dynamic buffer to be reused and avoid recreating dynamic
    ///         buffer 3000 times.
    ///     </para>
    /// </summary>
    public int PreDefinedVertexCount { get; set; } = 0;

    /// <summary>
    ///     The pre defined index count. Used when <see cref="IsDynamic" /> = true.
    ///     <para>The pre define index count allows user to initialize a dynamic buffer with a minimum pre-define size.</para>
    ///     <para>
    ///         Example: If the index count increments from 0 to around 3000 during index array streaming,
    ///         pre-define a size of 3000 for this geometry allows the dynamic buffer to be reused and avoid recreating dynamic
    ///         buffer 3000 times.
    ///     </para>
    /// </summary>
    public int PreDefinedIndexCount { get; set; } = 0;

    /// <summary>
    ///     Gets a value indicating whether the geometry data are transient. Call <see cref="SetAsTransient" /> to set this
    ///     flag to true.
    ///     <para>
    ///         When this is true, geometry3D data will be cleared once being loaded into GPU.
    ///     </para>
    ///     <para>
    ///         This geometry3D can only be used by one Model3D in one Viewport.
    ///         Must not be shared.
    ///         Hit test is disabled as well.
    ///     </para>
    ///     <para>
    ///         Useful when loading a large geometry for view only and free up memory after geometry data being uploaded to
    ///         GPU.
    ///     </para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is transient; otherwise, <c>false</c>.
    /// </value>
    public bool IsTransient { get; private set; }

    /// <summary>
    ///     The disable update bound, only used in <see cref="AssignTo(Geometry3D)" />
    /// </summary>
    protected bool DisableUpdateBound;

    private readonly object octreeLock = new();

    /// <summary>
    ///     Gets or sets the octree parameter.
    /// </summary>
    /// <value>
    ///     The octree parameter.
    /// </value>
    public OctreeBuildParameter OctreeParameter { get; } = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Geometry3D" /> class.
    /// </summary>
    public Geometry3D() {
        OctreeParameter.PropertyChanged += OctreeParameter_PropertyChanged;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Geometry3D" /> class.
    /// </summary>
    /// <param name="isDynamic">if set to <c>true</c> [is dynamic].</param>
    public Geometry3D(bool isDynamic)
        : this() {
        IsDynamic = isDynamic;
    }

    private void OctreeParameter_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        OctreeDirty = true;
    }

    /// <summary>
    ///     Call to manually update vertex buffer. Use with <see cref="ObservableObject.DisablePropertyChangedEvent" />
    ///     <para>
    ///         This is useful if user want to reuse existing <see cref="Positions" /> list and update vertex value inside
    ///         the list.
    ///     </para>
    ///     <para>
    ///         Note: For performance purpose, this will not cause bounding box update.
    ///         User must manually call <see cref="UpdateBounds" /> to refresh geometry bounding box.
    ///     </para>
    /// </summary>
    public void UpdateVertices() {
        RaisePropertyChanged(VertexBufferPropChanged);
    }

    /// <summary>
    ///     Call to manually update triangle buffer.
    ///     <para>
    ///         This is useful if user want to reuse existing <see cref="Indices" /> object and update index value inside the
    ///         list.
    ///     </para>
    /// </summary>
    public void UpdateTriangles() {
        RaisePropertyChanged(TriangleBufferPropChanged);
    }

    /// <summary>
    ///     Call to manually update vertex color buffer.
    ///     <para>
    ///         This is useful if user want to reuse existing <see cref="Colors" /> object and update color value inside the
    ///         list.
    ///     </para>
    ///     <para>Make sure the <see cref="Colors" /> count equal to the <see cref="Positions" /> count</para>
    /// </summary>
    public void UpdateColors() {
        RaisePropertyChanged(ColorsPropChanged);
    }

    /// <summary>
    ///     Create Octree for current model.
    /// </summary>
    public void UpdateOctree(bool force = false) {
        if (!IsTransient && CanCreateOctree()) {
            if (OctreeDirty || force) {
                lock (octreeLock) {
                    if (OctreeDirty || force) {
                        Octree = CreateOctree(OctreeParameter);
                        Octree?.BuildTree();
                        OctreeDirty = false;
                    }
                }

                RaisePropertyChanged(nameof(Octree));
            }
        } else {
            Octree = null;
            OctreeDirty = true;
        }
    }

    protected virtual bool CanCreateOctree() => Positions != null && Indices != null && Positions.Count > 0 && Indices.Count > 0;


    /// <summary>
    ///     Override to create different octree in subclasses.
    /// </summary>
    /// <returns></returns>
    protected virtual IOctreeBasic? CreateOctree(OctreeBuildParameter parameter) => null;


    /// <summary>
    ///     Set octree to null
    /// </summary>
    public void ClearOctree() {
        Octree = null;
        OctreeDirty = true;
    }

    /// <summary>
    ///     Manuals the set octree.
    /// </summary>
    /// <param name="octree">The octree.</param>
    public void ManualSetOctree(IOctreeBasic octree) {
        Octree = octree;
        OctreeDirty = false;
    }

    /// <summary>
    ///     Assigns internal properties to another geometry3D. This does not assign <see cref="IsDynamic" />/
    ///     <see cref="PreDefinedIndexCount" />/<see cref="PreDefinedVertexCount" />
    ///     <para>
    ///         Following properties are assigned:
    ///         <see cref="Positions" />, <see cref="Indices" />, <see cref="Colors" />, <see cref="Bound" />,
    ///         <see cref="BoundingSphere" />, <see cref="Octree" />, <see cref="OctreeParameter" />
    ///     </para>
    ///     <para>Override <see cref="OnAssignTo(Geometry3D)" /> to assign custom properties in child class</para>
    /// </summary>
    /// <param name="target">The target.</param>
    public void AssignTo(Geometry3D target) {
        target.DisableUpdateBound = true;
        target.Positions = Positions;
        target.ClearOctree();
        target.DisableUpdateBound = false;
        target.Indices = Indices;
        target.Colors = Colors;
        target.Bound = Bound;
        target.BoundingSphere = BoundingSphere;
        target.OctreeParameter.MinimumOctantSize = OctreeParameter.MinimumOctantSize;
        target.OctreeParameter.MinObjectSizeToSplit = OctreeParameter.MinObjectSizeToSplit;
        target.OctreeParameter.Cubify = OctreeParameter.Cubify;
        target.OctreeParameter.EnableParallelBuild = OctreeParameter.EnableParallelBuild;
        if (Octree != null) target.ManualSetOctree(Octree);
        OnAssignTo(target);
    }

    protected virtual void OnAssignTo(Geometry3D target) { }

    /// <summary>
    ///     Manually call this function to update AABB and Bounding Sphere
    /// </summary>
    public virtual void UpdateBounds() {
        if (DisableUpdateBound) return;

        if (position is not { Count: > 0 } positions) {
            Bound = new BoundingBox();
            BoundingSphere = new BoundingSphere();
        } else {
            Bound = BoundingBoxExtensions.FromPoints(positions);
            BoundingSphere = BoundingSphereExtensions.FromPoints(positions);
        }

        if (Bound.Maximum.IsUndefined() || Bound.Minimum.IsUndefined() || BoundingSphere.Center.IsUndefined()
            || float.IsInfinity(Bound.Center().X) || float.IsInfinity(Bound.Center().Y) ||
            float.IsInfinity(Bound.Center().Z))
            throw new Exception("Position vertex contains invalid value(Example: Float.NaN, Float.Infinity).");
    }

    public struct Triangle {
        public Vector3 P0, P1, P2;
    }

    public struct Line {
        public Vector3 P0, P1;
    }

    public struct Point {
        public Vector3 P0;
    }

    /// <summary>
    ///     Sets this geometry as transient.
    ///     <para>
    ///         Once this is called, this geometry will be marked as <see cref="IsTransient" /> = true.
    ///     </para>
    ///     <para>
    ///         This function must be called before geometry is attached to a model for rendering.
    ///         Or before the model is attached to a viewport for rendering.
    ///     </para>
    ///     <para>
    ///         A transient geometry is being used to save memory. All geometry data will be cleared once being uploaded into
    ///         GPU.
    ///         Should not be shared with multiple models.
    ///     </para>
    ///     A transient geometry does not support hit test.
    /// </summary>
    public void SetAsTransient() {
        IsTransient = true;
        ClearOctree();
    }

    /// <summary>
    ///     Clears all geometry data.
    /// </summary>
    public void ClearAllGeometryData() {
        Positions?.Clear();
        Positions?.TrimExcess();
        Indices?.Clear();
        Indices?.TrimExcess();
        Colors?.Clear();
        Colors?.TrimExcess();
        OnClearAllGeometryData();
    }

    protected virtual void OnClearAllGeometryData() { }
}
