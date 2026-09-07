/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

internal class MorphTargetUploaderCore : RenderCore {

    private int[] morphTargetOffsets = [];
    private Vector3[] morphTargetsDeltas = [];
    private float[] morphTargetWeights = [];
    private int mtCount;

    private int mtPitch;
    private bool setCBuffer = true;

    private bool setDeltas;
    private bool weightUpdated;

    public MorphTargetUploaderCore()
        : base(RenderType.None) {
        //Setup cbuffer
        var cbd = new ConstantBufferDescription(DefaultBufferNames.MorphTargetCb, 16); //maybe no slot issue
    }

    public float[] MorphTargetWeights {
        get => morphTargetWeights;
        set {
            if (SetAffectsRender(ref morphTargetWeights, value)) {
                weightUpdated = true;
                WeightsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    ///     Gets whether a complete morph-target payload is available.
    /// </summary>
    internal bool HasMorphTarget => mtCount > 0 && mtPitch > 0;

    /// <summary>
    ///     Gets the number of initialized morph targets.
    /// </summary>
    internal int MorphTargetCount => mtCount;

    /// <summary>
    ///     Gets the vertex pitch between initialized morph targets.
    /// </summary>
    internal int MorphTargetPitch => mtPitch;

    /// <summary>
    ///     Gets the current morph-target weights for Direct3D 12 upload.
    /// </summary>
    internal ReadOnlySpan<float> D3D12Weights => morphTargetWeights;

    /// <summary>
    ///     Gets the compact morph-target deltas for Direct3D 12 upload.
    /// </summary>
    internal ReadOnlySpan<Vector3> D3D12Deltas => morphTargetsDeltas;

    /// <summary>
    ///     Gets the compact-delta offsets for Direct3D 12 upload.
    /// </summary>
    internal ReadOnlySpan<int> D3D12Offsets => morphTargetOffsets;

    /// <summary>
    ///     Applies the existing morph-target payload using the same target/vertex indexing as the skinning shader.
    /// </summary>
    /// <param name="vertices">The default mesh vertices to update before bone transforms.</param>
    /// <returns><see langword="true" /> when morph targets were applied.</returns>
    internal bool ApplyD3D12MorphTargets(Span<DefaultVertex> vertices) {
        if (!HasMorphTarget) return false;
        if (vertices.Length != mtPitch)
            throw new InvalidOperationException("The morph-target pitch must match the mesh vertex count.");
        if (morphTargetWeights.Length < mtCount)
            throw new InvalidOperationException("Every morph target requires one weight.");
        if (morphTargetOffsets.Length != checked(mtCount * mtPitch))
            throw new InvalidOperationException("The morph-target offset table is incomplete.");

        for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++) {
            var vertex = vertices[vertexIndex];
            for (var targetIndex = 0; targetIndex < mtCount; targetIndex++) {
                var offset = morphTargetOffsets[targetIndex * mtPitch + vertexIndex];
                if ((uint)offset > (uint)(morphTargetsDeltas.Length - 3))
                    throw new InvalidOperationException("A morph-target offset is outside the delta buffer.");
                var weight = morphTargetWeights[targetIndex];
                var positionDelta = morphTargetsDeltas[offset] * weight;
                vertex.Position.X += positionDelta.X;
                vertex.Position.Y += positionDelta.Y;
                vertex.Position.Z += positionDelta.Z;
                vertex.Normal += morphTargetsDeltas[offset + 1] * weight;
                vertex.Tangent += morphTargetsDeltas[offset + 2] * weight;
            }
            vertex.Normal = SilkMath.Normalize(vertex.Normal);
            vertex.Tangent = SilkMath.Normalize(vertex.Tangent);
            vertex.BiTangent = SilkMath.Cross(vertex.Normal, vertex.Tangent);
            vertices[vertexIndex] = vertex;
        }
        return true;
    }

    public StructuredBufferProxy? MtWeightsB {
        get;
        private set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    public ImmutableBufferProxy? MtDeltasB {
        get;
        private set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    public ImmutableBufferProxy? MtOffsetsB {
        get;
        private set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    public event EventHandler? WeightsChanged;

    protected override void OnDispose(bool disposeManagedResources) {
        if (disposeManagedResources)
            WeightsChanged = null;
        base.OnDispose(disposeManagedResources);
    }

    public bool InitializeMorphTargets(MorphTargetVertex[]? targets, int pitch) {
        if (targets == null || targets.Length == 0) {
            mtCount = 0;
            mtPitch = 0;
            return true;
        }
        if (pitch <= 0) throw new ArgumentOutOfRangeException(nameof(pitch));
        if (targets.Length % pitch != 0)
            throw new ArgumentException("Morph-target data must contain complete vertex pitches.", nameof(targets));

        //Setup buffer and keep track of data to update
        setDeltas = true;

        //Setup arrays for morph target data
        var mtdList = new FastList<Vector3>(targets.Length * 3);
        morphTargetOffsets = new int[targets.Length];

        //First element is always 0 delta
        mtdList.Add(Vector3.Zero);
        mtdList.Add(Vector3.Zero);
        mtdList.Add(Vector3.Zero);

        //Subsequent elements should never need 0 delta vertex
        var zv = Vector3.Zero;

        var current = 1;
        for (var i = 0; i < targets.Length; i++)
            //Skip if 0 delta
            if (targets[i].deltaNormal == zv && targets[i].deltaPosition == zv &&
                targets[i].deltaTangent == zv) {
                morphTargetOffsets[i] = 0;
            } else {
                morphTargetOffsets[i] = current * 3;

                mtdList.Add(targets[i].deltaPosition);
                mtdList.Add(targets[i].deltaNormal);
                mtdList.Add(targets[i].deltaTangent);

                current++;
            }

        morphTargetsDeltas = [.. mtdList];

        //Set cbuffer data {int count, int pitch}
        setCBuffer = true;
        mtCount = targets.Length / pitch;
        mtPitch = pitch;
        return true;
    }

    public void SetWeight(int i, float w) {
        MorphTargetWeights[i] = w;
        InvalidateWeight();
    }

    public void InvalidateWeight() {
        weightUpdated = true;
        WeightsChanged?.Invoke(this, EventArgs.Empty);
    }
}
