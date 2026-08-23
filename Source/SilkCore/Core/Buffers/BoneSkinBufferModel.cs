/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using Buffer = HelixToolkit.SharpDX.Core.Native.Buffer;

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

    public override bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources) {
        if (boneIdChanged)
            lock (BoneIdBuffer) {
                if (boneIdChanged) {
                    if (Geometry is BoneSkinnedMeshGeometry3D { VertexBoneIds: { } boneIds, Positions: { } positions } && boneIds.Count == positions.Count)
                        BoneIdBuffer.UploadDataToBuffer(context,
                                                        boneIds,
                                                        boneIds.Count);
                    else
                        BoneIdBuffer.UploadDataToBuffer(context, new BoneIds[0], 0);
                    boneIdChanged = false;
                    BoneIdBufferUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

        return base.UpdateBuffers(context, deviceResources);
    }

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
