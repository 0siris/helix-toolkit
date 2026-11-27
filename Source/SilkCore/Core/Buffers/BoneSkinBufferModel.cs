using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public sealed class BoneSkinnedMeshBufferModel : DefaultMeshGeometryBufferModel, IBoneSkinMeshBufferModel {
    private IElementsBufferProxy? boneIdBuffer;

    public BoneSkinnedMeshBufferModel() {
        boneIdBuffer = new ImmutableBufferProxy(BoneIds.SizeInBytes, BindFlags.VertexBuffer);
    }

    public event EventHandler? BoneIdBufferUpdated;
    public IElementsBufferProxy BoneIdBuffer => boneIdBuffer
        ?? throw new ObjectDisposedException(nameof(BoneSkinnedMeshBufferModel));

    protected override bool IsVertexBufferChanged(string? propertyName, int bufferIndex) {
        if (propertyName == nameof(BoneSkinnedMeshGeometry3D.VertexBoneIds))
            return false;

        return base.IsVertexBufferChanged(propertyName, bufferIndex);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref boneIdBuffer);
        base.OnDispose(disposeManagedResources);
    }
}
