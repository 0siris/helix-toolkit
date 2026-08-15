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
                        if (Geometry is BoneSkinnedMeshGeometry3D boneMesh
                            && boneMesh.VertexBoneIds is { } boneIds
                            && boneMesh.Positions is { } positions
                            && boneIds.Count == positions.Count)
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

    /// <summary>
    /// </summary>
    public sealed class BoneSkinPreComputeBufferModel : DisposeObject, IAttachableBufferModel,
                                                        IBoneSkinPreComputehBufferModel {
        private IBoneSkinMeshBufferModel meshBuffer;
        private IElementsBufferProxy? originalVertexBuffer;

        private VertexBufferBinding[] skinnedOutputBindings = [];
        private IElementsBufferProxy skinnedVertexBuffer;
        private IElementsBufferProxy skinnedVertexStagingBuffer;
        private bool stagingBufferValid;
        private VertexBufferBinding[] vertexBufferBindings = [];
        private bool vertexBufferUpdate = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoneSkinPreComputeBufferModel" /> class.
        /// </summary>
        /// <param name="meshBuffer">The mesh buffer.</param>
        /// <param name="structSize">Size of the structure.</param>
        public BoneSkinPreComputeBufferModel(IBoneSkinMeshBufferModel meshBuffer, int structSize) {
            this.meshBuffer = meshBuffer;
            this.meshBuffer.VertexBufferUpdated += MeshBuffer_OnVertexBufferUpdated;
            this.meshBuffer.BoneIdBufferUpdated += MeshBuffer_OnBoneIdBufferUpdated;
            skinnedVertexBuffer = new ImmutableBufferProxy(structSize,
                                                           BindFlags.VertexBuffer | BindFlags.StreamOutput,
                                                           ResourceOptionFlags.None,
                                                           ResourceUsage.Default);
            skinnedVertexStagingBuffer = new ImmutableBufferProxy(structSize,
                                                                  BindFlags.None,
                                                                  CpuAccessFlags.Read,
                                                                  ResourceOptionFlags.BufferStructured,
                                                                  ResourceUsage.Staging);
        }

        public PrimitiveTopology Topology {
            get => meshBuffer.Topology;
            set => meshBuffer.Topology = value;
        }

        public IElementsBufferProxy[] VertexBuffer { get; private set; } = [];

        public IEnumerable<int> VertexStructSize => VertexBuffer.Select(x => x.StructureSize);

        public IElementsBufferProxy? IndexBuffer => meshBuffer.IndexBuffer;

        public Guid Guid { get; } = new();

        /// <summary>
        ///     Attaches the buffers.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="vertexBufferStartSlot">The vertex buffer start slot.</param>
        /// <param name="deviceResources">The device resources.</param>
        /// <returns></returns>
        public bool AttachBuffers(
            DeviceContextProxy context,
            ref int vertexBufferStartSlot,
            IDeviceResources deviceResources
        ) {
            UpdateBuffers(context, deviceResources);
            if (VertexBuffer.Length > 0) {
                if (VertexBuffer.Length == vertexBufferBindings.Length) {
                    context.SetVertexBuffers(vertexBufferStartSlot, vertexBufferBindings);
                    vertexBufferStartSlot += VertexBuffer.Length;
                } else {
                    return false;
                }
            }

            if (IndexBuffer != null)
                context.SetIndexBuffer(IndexBuffer.Buffer, Format.FormatR32Uint, IndexBuffer.Offset);
            else
                context.SetIndexBuffer(null, Format.FormatUnknown, 0);
            context.PrimitiveTopology = Topology;
            return true;
        }

        /// <summary>
        ///     Updates the buffers.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="deviceResources">The device resources.</param>
        /// <returns></returns>
        public bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources) {
            var updated = false;
            if (meshBuffer.UpdateBuffers(context, deviceResources) || vertexBufferUpdate)
                lock (skinnedVertexBuffer) {
                    if (vertexBufferUpdate) {
                        if (meshBuffer.VertexBuffer.FirstOrDefault() is { } originalBufferProxy) {
                            VertexBuffer = [originalBufferProxy];
                            this.originalVertexBuffer = originalBufferProxy;
                            if (skinnedVertexBuffer.Buffer == null || skinnedVertexBuffer.ElementCount !=
                                originalBufferProxy.ElementCount) {
                                var array = new float[originalBufferProxy.ElementCount *
                                                      originalBufferProxy.StructureSize];
                                skinnedVertexBuffer.UploadDataToBuffer(context,
                                                                       array,
                                                                       originalBufferProxy.ElementCount);
                                if (originalBufferProxy.Buffer is { } originalBuffer
                                    && skinnedVertexBuffer.Buffer is { } skinnedBuffer)
                                    context.CopyResource(originalBuffer, skinnedBuffer);
                            }

                            if (originalBufferProxy.Buffer is { } originalVertexBufferResource
                                && skinnedVertexBuffer.Buffer is not null
                                && meshBuffer.BoneIdBuffer.Buffer is { } boneIdBufferResource) {
                                VertexBuffer[0] = skinnedVertexBuffer;
                                vertexBufferBindings = [.. VertexBuffer.Select(x =>
                                    x.Buffer is { } buffer
                                        ? new VertexBufferBinding(buffer, x.StructureSize, x.Offset)
                                        : new VertexBufferBinding())];
                                skinnedOutputBindings = [
                                    new VertexBufferBinding(originalVertexBufferResource,
                                                            originalBufferProxy.StructureSize,
                                                            originalBufferProxy.Offset),
                                    new VertexBufferBinding(boneIdBufferResource,
                                                            meshBuffer.BoneIdBuffer.StructureSize,
                                                            meshBuffer.BoneIdBuffer.Offset)
                                ];
                            } else {
                                VertexBuffer = [];
                                vertexBufferBindings = [];
                            }
                        } else {
                            VertexBuffer = [];
                            vertexBufferBindings = [];
                        }

                        vertexBufferUpdate = false;
                        updated = true;
                        stagingBufferValid = false;
                    }
                }

            return updated;
        }

        public bool CanPreCompute => meshBuffer.BoneIdBuffer.ElementCount != 0;

        /// <summary>
        ///     Binds the skinned vertex buffer to output.
        /// </summary>
        /// <param name="context">The context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindSkinnedVertexBufferToOutput(DeviceContextProxy context) {
            if (skinnedVertexBuffer.Buffer is not { } skinnedBuffer
                || meshBuffer.BoneIdBuffer.Buffer is not { })
                return;

            context.SetVertexBuffers(0, skinnedOutputBindings);
            context.SetIndexBuffer(null, Format.FormatUnknown, 0);
            context.SetStreamOutputTarget(skinnedBuffer, skinnedVertexBuffer.Offset);
            stagingBufferValid = false;
        }

        /// <summary>
        ///     Uns the bind skinned vertex buffer to gs stream output.
        /// </summary>
        /// <param name="context">The context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnBindSkinnedVertexBufferToOutput(DeviceContextProxy context) {
            context.SetStreamOutputTarget((Buffer?)null);
        }

        public void ResetSkinnedVertexBuffer(DeviceContextProxy context) {
            if (originalVertexBuffer?.Buffer is { } originalBuffer
                && skinnedVertexBuffer.Buffer is { } skinnedBuffer
                && skinnedVertexBuffer.ElementCount == originalVertexBuffer.ElementCount)
                context.CopyResource(originalBuffer, skinnedBuffer);
        }

        /// <summary>
        ///     Copies the skinned to array.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="array">The array.</param>
        /// <returns>Number of vertex has been copied.</returns>
        public int CopySkinnedToArray(DeviceContextProxy context, Vector3[] array) {
            if (skinnedVertexBuffer.Buffer is not { } skinnedBuffer) return 0;
            if (skinnedVertexStagingBuffer.Buffer == null ||
                skinnedVertexStagingBuffer.ElementCount != skinnedVertexBuffer.ElementCount) {
                skinnedVertexStagingBuffer.CreateBuffer(context, skinnedVertexBuffer.ElementCount);
                stagingBufferValid = false;
            }

            if (skinnedVertexStagingBuffer.Buffer is { } stagingBuffer) {
                var size = Math.Min(array.Length, skinnedVertexStagingBuffer.ElementCount);
                if (!stagingBufferValid) {
                    context.CopyResource(skinnedBuffer, stagingBuffer);
                    stagingBufferValid = true;
                }

                var box = context.MapSubresource(stagingBuffer,
                                                 MapMode.Read,
                                                 MapFlags.None);
                unsafe {
                    var p = (byte*)box.DataPointer;
                    for (var i = 0; i < size; ++i) {
                        array[i] = *(Vector3*)p;
                        p += skinnedVertexStagingBuffer.StructureSize;
                    }
                }

                context.UnmapSubresource(stagingBuffer, 0);
                return size;
            }

            return 0;
        }

        private void MeshBuffer_OnBoneIdBufferUpdated(object? sender, EventArgs e) {
            if (originalVertexBuffer?.Buffer is { } originalBuffer
                && meshBuffer.BoneIdBuffer.Buffer is { } boneIdBuffer)
                skinnedOutputBindings = [
                    new VertexBufferBinding(originalBuffer,
                                            originalVertexBuffer.StructureSize,
                                            originalVertexBuffer.Offset),
                    new VertexBufferBinding(boneIdBuffer,
                                            meshBuffer.BoneIdBuffer.StructureSize,
                                            meshBuffer.BoneIdBuffer.Offset)
                ];
        }

        private void MeshBuffer_OnVertexBufferUpdated(object? sender, EventArgs e) {
            vertexBufferUpdate = true;
            stagingBufferValid = false;
        }

        protected override void OnDispose(bool disposeManagedResources) {
            meshBuffer.BoneIdBufferUpdated -= MeshBuffer_OnBoneIdBufferUpdated;
            meshBuffer.VertexBufferUpdated -= MeshBuffer_OnVertexBufferUpdated;
            var meshBufferToDispose = meshBuffer;
            var skinnedVertexBufferToDispose = skinnedVertexBuffer;
            var skinnedVertexStagingBufferToDispose = skinnedVertexStagingBuffer;
            RemoveAndDispose(ref meshBufferToDispose);
            RemoveAndDispose(ref skinnedVertexBufferToDispose);
            RemoveAndDispose(ref skinnedVertexStagingBufferToDispose);
            base.OnDispose(disposeManagedResources);
        }
    }
