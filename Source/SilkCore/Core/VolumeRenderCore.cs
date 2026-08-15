/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
Reference: https://graphicsrunner.blogspot.com/search/label/Volume%20Rendering
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class VolumeRenderCore : RenderCore {
    private static readonly MeshGeometry3D BoxMesh;
    private readonly ConstantBufferComponent modelCb;
    private int backTexSlot;

    private VolumeCubeBufferModel? buffer;

    private ShaderPass? cubeBackPass;
    private MaterialVariable materialVariables = EmptyMaterialVariable.EmptyVariable;
    private ShaderPass? meshFrontPass;
    private ModelMatrices modelMatrices;
    private ShaderPass? volumePass;

    static VolumeRenderCore() {
        BoxMesh = new MeshGeometry3D {
            Positions = [
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f)
            ],
            Indices = [
                0, 2, 3,
                3, 1, 0,
                4, 5, 7,
                7, 6, 4,
                0, 1, 5,
                5, 4, 0,
                1, 3, 7,
                7, 5, 1,
                3, 2, 6,
                6, 7, 3,
                2, 0, 4,
                4, 6, 2
            ]
        };
    }

    public VolumeRenderCore()
        : base(RenderType.Particle) {
        modelCb = AddComponent(new ConstantBufferComponent(new ConstantBufferDescription(
                                                               DefaultBufferNames.VolumeModelCb,
                                                               VolumeParamsStruct.SizeInBytes)));
    }

    /// <summary>
    ///     Used to wrap all material resources
    /// </summary>
    public MaterialVariable MaterialVariables {
        get => materialVariables;
        set {
            value.AssertNotNull("Use EmptyVariable const");

            SetAffectsCanRenderFlag(ref materialVariables, value);
        }
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        if (technique[DefaultPassNames.Backface] is not { } backPass
            || technique[DefaultPassNames.Positions] is not { } frontPass)
            return false;

        buffer = new VolumeCubeBufferModel {
            Geometry = BoxMesh,
            Topology = PrimitiveTopology.TriangleList
        };
        cubeBackPass = backPass;
        meshFrontPass = frontPass;
        modelCb.Attach(technique);
        return true;
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref buffer);
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (buffer is not { } volumeBuffer || cubeBackPass is not { } backPass || meshFrontPass is not { } frontPass
            || EffectTechnique is not { } technique)
            return;

        using var back = context.GetOffScreenRt(OffScreenTextureSize.Full, Format.FormatR16G16B16A16Float);
        var slot = 0;
        using (var depth =
               context.GetOffScreenDs(OffScreenTextureSize.Full, Format.FormatD32FloatS8X24Uint)) {
            if (depth.DepthStencilView is not { } depthView) return;

            deviceContext.ClearDepthStencilView(depthView,
                                                DepthStencilClearFlags.Depth |
                                                DepthStencilClearFlags.Stencil,
                                                1,
                                                1);
            deviceContext.ClearRenderTargetView(back, new Color4(0, 0, 0, 0));
            BindTarget(depthView, back, deviceContext, (int)context.ActualWidth, (int)context.ActualHeight);

        #region Render box back face and set stencil buffer to 0

            modelMatrices.Update(ref ModelMatrix);
            if (!materialVariables.UpdateMaterialStruct(deviceContext, ref modelMatrices)) return;
            volumeBuffer.AttachBuffers(deviceContext, ref slot, technique.EffectsManager);
            if (volumeBuffer.IndexBuffer is not { } indexBuffer) return;
            backPass.BindShader(deviceContext);
            backPass.BindStates(deviceContext, StateType.All);
            deviceContext.DrawIndexed(indexBuffer.ElementCount, 0, 0);

        #endregion

        #region Render all mesh Positions onto off-screen texture region with stencil = 0 only

            if (context.RenderHost.PerFrameOpaqueNodesInFrustum.Count > 0)
                for (var i = 0; i < context.RenderHost.PerFrameOpaqueNodesInFrustum.Count; ++i) {
                    var mesh = context.RenderHost.PerFrameOpaqueNodesInFrustum[i];
                    if (mesh.EffectTechnique is not { } meshTechnique) continue;
                    var meshPass = meshTechnique[DefaultPassNames.Positions];
                    if (meshPass.IsNull) continue;
                    meshPass.BindShader(deviceContext);
                    meshPass.BindStates(deviceContext, StateType.BlendState);
                    // Set special depth stencil state to only render into region with stencil region is 0
                    frontPass.BindStates(deviceContext, StateType.DepthStencilState);
                    mesh.RenderCustom(context, deviceContext);
                }

        #endregion
        }

    #region Render box back face again and do actual volume sampling

        context.RenderHost.SetDefaultRenderTargets(false);
        var pass = materialVariables.GetPass(RenderType.Opaque, context);
        if (pass != volumePass) {
            volumePass = pass;
            backTexSlot =
                pass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames
                    .VolumeBack);
        }

        slot = 0;
        volumeBuffer.AttachBuffers(deviceContext, ref slot, technique.EffectsManager);
        if (volumeBuffer.IndexBuffer is not { } finalIndexBuffer) return;
        materialVariables.BindMaterialResources(context, deviceContext, pass);
        pass.PixelShader.BindTexture(deviceContext, backTexSlot, back);
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, StateType.All);
        deviceContext.DrawIndexed(finalIndexBuffer.ElementCount, 0, 0);

    #endregion

        pass.PixelShader.BindTexture(deviceContext, backTexSlot, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BindTarget(
        DepthStencilView dsv,
        RenderTargetView? targetView,
        DeviceContextProxy context,
        int width,
        int height
    ) {
        context.SetRenderTargets(dsv, targetView == null ? null : [targetView]);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ModelMatrices {
        public Matrix ModelMatrix;
        public Matrix ModelMatrixInv;

        public void Update(ref Matrix modelMatrix) {
            if (ModelMatrix != modelMatrix) {
                ModelMatrix = modelMatrix;
                ModelMatrixInv = modelMatrix.Inverted();
            }
        }
    }

    /// <summary>
    /// </summary>
    private sealed class VolumeCubeBufferModel : MeshGeometryBufferModel<Vector3> {
        public VolumeCubeBufferModel() : base(SilkMath.Vector3SizeInBytes) {
            Topology = PrimitiveTopology.TriangleList;
        }

        protected override void OnCreateVertexBuffer(
            DeviceContextProxy context,
            IElementsBufferProxy buffer,
            int bufferIndex,
            Geometry3D? geometry,
            IDeviceResources deviceResources
        ) {
            // -- set geometry if given
            if (geometry != null && geometry.Positions != null && geometry.Positions.Count > 0)
                buffer.UploadDataToBuffer(context, geometry.Positions, geometry.Positions.Count);
            else
                buffer.UploadDataToBuffer(context, EmptyVerts, 0);
        }
    }
}
