/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
/// </summary>
public sealed class BoneSkinnedMeshBufferModel : DefaultMeshGeometryBufferModel, IBoneSkinMeshBufferModel {
    private IElementsBufferProxy? boneIdBuffer;
    private bool boneIdChanged = true;

    public BoneSkinnedMeshBufferModel() {
        boneIdBuffer = new ImmutableBufferProxy(BoneIds.SizeInBytes, BindFlags.VertexBuffer);
    }

    public event EventHandler? BoneIdBufferUpdated;
    public IElementsBufferProxy BoneIdBuffer => boneIdBuffer
        ?? throw new ObjectDisposedException(nameof(BoneSkinnedMeshBufferModel));

    protected override bool IsVertexBufferChanged(string? propertyName, int bufferIndex) {
        if (propertyName?.Equals(nameof(BoneSkinnedMeshGeometry3D.VertexBoneIds)) == true) {
            boneIdChanged = true;
            return false;
        }

        return base.IsVertexBufferChanged(propertyName, bufferIndex);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref boneIdBuffer);
        base.OnDispose(disposeManagedResources);
    }
}
