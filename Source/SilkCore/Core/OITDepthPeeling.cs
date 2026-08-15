/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#define MSAASEPARATE

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class OitDepthPeeling : RenderCore {
    private readonly ShaderResourceView?[] finalSrVs = new ShaderResourceView?[3];
    private readonly ShaderResourceViewProxy?[] minMaxZTargets = new ShaderResourceViewProxy?[2];
    private readonly RenderTargetView?[] targets = new RenderTargetView?[3];
    private int currWidth, currHeight;
    private ShaderPass finalPass = ShaderPass.NullPass;
    private ShaderResourceViewProxy? frontBlendingTarget, backBlendingTarget;

    private ShaderResourceViewProxy MinMaxTarget(int index) => minMaxZTargets[index]
        ?? throw new InvalidOperationException("OIT depth peeling targets are not initialized.");

    private ShaderResourceViewProxy FrontBlendingTarget => frontBlendingTarget
        ?? throw new InvalidOperationException("OIT front blending target is not initialized.");

    private ShaderResourceViewProxy BackBlendingTarget => backBlendingTarget
        ?? throw new InvalidOperationException("OIT back blending target is not initialized.");


    public OitDepthPeeling() : base(RenderType.Transparent) { }

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
        if (Device is not { } device) throw new InvalidOperationException("The device is not initialized.");
        var rtv = new ShaderResourceViewProxy(device, desc);
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
        var frontTarget = FrontBlendingTarget;
        var backTarget = BackBlendingTarget;
        var minMaxTarget = MinMaxTarget(0);
        deviceContext.ClearRenderTargetView(frontTarget, color);
        if (ExternRenderParameter.RenderTargetView is { Length: > 0 } renderTargets
            && renderTargets[0] is { Resource: { } externalResource } externalTarget
            && backTarget.Resource is { } backResource) {
            if (ExternRenderParameter.IsMsaaTexture)
                deviceContext.ResolveSubresource(externalResource,
                                                 0,
                                                 backResource,
                                                 0,
                                                 Format.FormatB8G8R8A8Unorm);
            else
                deviceContext.CopyResource(externalResource, backResource);
        } else {
            color = new Color4(0, 0, 0, 0);
            deviceContext.ClearRenderTargetView(backTarget, color);
        }
        
        color = new Color4(-1, -1, 0, 0);
        deviceContext.ClearRenderTargetView(minMaxTarget, color);
    }

    private void DrawMesh(RenderContext context, DeviceContextProxy deviceContext) {
        var parameter = ExternRenderParameter;
        if (!parameter.ScissorRegion.IsEmpty) {
            if (context.RenderHost.Renderer is not { } renderer)
                return;

            RenderCount = renderer.RenderOpaque(context,
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

        if (context.RenderHost.RenderBuffer is not { } buffer)
            return;
        var hasMsaa = buffer.ColorBufferSampleDesc.Count > 1;
        var nonMsaaDepthBuffer = hasMsaa 
                                     ? context.RenderHost.RenderBuffer.DepthStencilBufferNoMsaa 
                                     : null;
        
        var depthStencilView = hasMsaa 
                                   ? nonMsaaDepthBuffer 
                                   : ExternRenderParameter.DepthStencilView;

        RenderCount = 0;
        InitializeMinMaxRenderTarget(deviceContext);
        context.OitRenderStage = OitRenderStage.DepthPeelingInitMinMaxZ;
        deviceContext.SetRenderTarget(depthStencilView, minMaxZTargets[0]);
        DrawMesh(context, deviceContext);

        context.OitRenderStage = OitRenderStage.DepthPeeling;
        var currId = 0;
        for (var layer = 1; layer < PeelingIteration; ++layer) {
            currId = layer % 2;
            var prevId = 1 - currId;
            var color = new Color4(-1, -1, 0, 0);
            var currentTarget = MinMaxTarget(currId);
            var previousTarget = MinMaxTarget(prevId);
            deviceContext.ClearRenderTargetView(currentTarget, color);
            
            targets[0] = currentTarget;
            targets[1] = FrontBlendingTarget;
            targets[2] = BackBlendingTarget;
            
            deviceContext.SetRenderTargets(depthStencilView, targets);
            deviceContext.SetShaderResource(new PixelShaderType(), 100, previousTarget);
            DrawMesh(context, deviceContext);
            deviceContext.SetShaderResource(new PixelShaderType(), 100, null);
        }

        context.OitRenderStage = OitRenderStage.None;
        
        finalSrVs[0] = MinMaxTarget(currId);
        finalSrVs[1] = FrontBlendingTarget;
        finalSrVs[2] = BackBlendingTarget;
        
        finalPass.BindShader(deviceContext);
        finalPass.BindStates(deviceContext, StateType.All);
        
        deviceContext.SetRenderTargets(null, ExternRenderParameter.RenderTargetView);
        deviceContext.SetShaderResources(new PixelShaderType(), 100, finalSrVs);
        deviceContext.Draw(4, 0);
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        finalPass = technique[DefaultPassNames.OitDepthPeelingFinal];
        return !finalPass.IsNull;
    }

    protected override void OnDetach() {
        DisposeAllTargets();
        currWidth = currHeight = 0;
        finalPass = ShaderPass.NullPass;
    }
}
