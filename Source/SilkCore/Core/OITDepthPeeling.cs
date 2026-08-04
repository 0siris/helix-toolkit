/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#define MSAASEPARATE

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class OITDepthPeeling : RenderCore {
    private readonly ShaderResourceView[] finalSRVs = new ShaderResourceView[3];
    private readonly ShaderResourceViewProxy[] minMaxZTargets = new ShaderResourceViewProxy[2];
    private readonly RenderTargetView[] targets = new RenderTargetView[3];
    private int currWidth, currHeight;
    private ShaderPass finalPass = ShaderPass.NullPass;
    private ShaderResourceViewProxy frontBlendingTarget, backBlendingTarget;


    public OITDepthPeeling() : base(RenderType.Transparent) { }

    public RenderParameter ExternRenderParameter { get; set; }

    public int RenderCount { get; private set; }

    public int PeelingIteration { get; set; } = 4;

    private bool CreateRenderTargets(int width, int height) {
        if (currWidth == width && currHeight == height) 
            return false;
        
        DisposeAllTargets();
        currWidth = width;
        currHeight = height;
        var tex2DDesc = new Texture2DDescription {
            Width = width,
            Height = height,
            ArraySize = 1,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Usage = ResourceUsage.Default,
            CpuAccessFlags = CpuAccessFlags.None,
            Format = Format.FormatR32G32Float
        };

        minMaxZTargets[0] = CreateRtv(tex2DDesc);
        minMaxZTargets[1] = CreateRtv(tex2DDesc);
        
        tex2DDesc.Format = Format.FormatB8G8R8A8Unorm;
        frontBlendingTarget = CreateRtv(tex2DDesc);
        backBlendingTarget = CreateRtv(tex2DDesc);
        
        return true;
    }

    private ShaderResourceViewProxy CreateRtv(Texture2DDescription desc) {
        var rtv = new ShaderResourceViewProxy(Device, desc);
        rtv.CreateRenderTargetView();
        rtv.CreateTextureView();
        return rtv;
    }

    private void DisposeAllTargets() {
        
        RemoveAndDispose(ref minMaxZTargets[0]);
        RemoveAndDispose(ref minMaxZTargets[1]);
        RemoveAndDispose(ref frontBlendingTarget);
        RemoveAndDispose(ref backBlendingTarget);
    }

    private void InitializeMinMaxRenderTarget(DeviceContextProxy deviceContext) {
        var color = new Color4(0, 0, 0, 1);
        deviceContext.ClearRenderTargetView(frontBlendingTarget, color);
        if (ExternRenderParameter.RenderTargetView is {Length: > 0} && backBlendingTarget.Resource != null) {
            if (ExternRenderParameter.IsMSAATexture)
                deviceContext.ResolveSubresource(ExternRenderParameter.RenderTargetView[0].Resource,
                                                 0,
                                                 backBlendingTarget.Resource,
                                                 0,
                                                 Format.FormatB8G8R8A8Unorm);
            else
                deviceContext.CopyResource(ExternRenderParameter.RenderTargetView[0].Resource,
                                           backBlendingTarget.Resource);
        } else {
            color = new Color4(0, 0, 0, 0);
            deviceContext.ClearRenderTargetView(backBlendingTarget, color);
        }
        
        color = new Color4(-1, -1, 0, 0);
        deviceContext.ClearRenderTargetView(minMaxZTargets[0], color);
    }

    private void DrawMesh(RenderContext context, DeviceContextProxy deviceContext) {
        var parameter = ExternRenderParameter;
        if (!parameter.ScissorRegion.IsEmpty) {
            RenderCount = context.RenderHost.Renderer.RenderOpaque(context,
                                                                   context.RenderHost.PerFrameTransparentNodes,
                                                                   ref parameter,
                                                                   context.EnableBoundingFrustum);
        } else {
            var count = context.RenderHost.PerFrameTransparentNodes.Count;
            for (var i = 0; i < count; ++i) {
                var renderable = context.RenderHost.PerFrameTransparentNodes[i];
                renderable.RenderCore.Render(context, deviceContext);
                ++RenderCount;
            }
        }
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (CreateRenderTargets((int)context.ActualWidth, (int)context.ActualHeight)) {
            RaiseInvalidateRender();
            return;
        }

        var buffer = context.RenderHost.RenderBuffer;
        var hasMSAA = buffer.ColorBufferSampleDesc.Count > 1;
        var nonMSAADepthBuffer = hasMSAA 
                                     ? context.RenderHost.RenderBuffer.DepthStencilBufferNoMSAA 
                                     : null;
        
        var depthStencilView = hasMSAA 
                                   ? nonMSAADepthBuffer 
                                   : ExternRenderParameter.DepthStencilView;

        RenderCount = 0;
        InitializeMinMaxRenderTarget(deviceContext);
        context.OITRenderStage = OITRenderStage.DepthPeelingInitMinMaxZ;
        deviceContext.SetRenderTarget(depthStencilView, minMaxZTargets[0]);
        DrawMesh(context, deviceContext);

        context.OITRenderStage = OITRenderStage.DepthPeeling;
        var currId = 0;
        for (var layer = 1; layer < PeelingIteration; ++layer) {
            currId = layer % 2;
            var prevId = 1 - currId;
            var color = new Color4(-1, -1, 0, 0);
            deviceContext.ClearRenderTargetView(minMaxZTargets[currId], color);
            
            targets[0] = minMaxZTargets[currId];
            targets[1] = frontBlendingTarget;
            targets[2] = backBlendingTarget;
            
            deviceContext.SetRenderTargets(depthStencilView, targets);
            deviceContext.SetShaderResource(new PixelShaderType(), 100, minMaxZTargets[prevId]);
            DrawMesh(context, deviceContext);
            deviceContext.SetShaderResource(new PixelShaderType(), 100, null);
        }

        context.OITRenderStage = OITRenderStage.None;
        
        finalSRVs[0] = minMaxZTargets[currId];
        finalSRVs[1] = frontBlendingTarget;
        finalSRVs[2] = backBlendingTarget;
        
        finalPass.BindShader(deviceContext);
        finalPass.BindStates(deviceContext, StateType.All);
        
        deviceContext.SetRenderTargets(null, ExternRenderParameter.RenderTargetView);
        deviceContext.SetShaderResources(new PixelShaderType(), 100, finalSRVs);
        deviceContext.Draw(4, 0);
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        finalPass = technique[DefaultPassNames.OITDepthPeelingFinal];
        return !finalPass.IsNULL;
    }

    protected override void OnDetach() {
        DisposeAllTargets();
        currWidth = currHeight = 0;
        finalPass = ShaderPass.NullPass;
    }
}