using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core.Render;

public partial class DeviceContextProxy {
    private static readonly RenderTargetView?[] ZeroRenderTargetArray = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTargets(DX11RenderBufferProxyBase buffer)
        => buffer.SetDefaultRenderTargets(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTarget(DepthStencilView? dsv, RenderTargetView? renderTarget)
        => NativeContext.SetRenderTargets(dsv, renderTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTargets(DepthStencilView? dsv, RenderTargetView?[]? renderTarget)
        => NativeContext.SetRenderTargets(dsv, renderTarget);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStreamOutputTarget(Buffer? buffer, int offset = 0)
        => NativeContext.SetStreamOutputTarget(buffer, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStreamOutputTarget(Buffer[] bufferBindings)
        => NativeContext.SetStreamOutputTargets(bufferBindings);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthStencil(DepthStencilView? dsv)
        => NativeContext.SetRenderTargets(dsv, ZeroRenderTargetArray);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTarget(RenderTargetView? rtv)
        => NativeContext.SetRenderTargets(null, rtv);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTarget(
        DepthStencilView? dsv,
        RenderTargetView? rtv,
        bool clearRtv,
        Color4 color,
        bool clearDsv,
        DepthStencilClearFlags flags = DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
        float depth = 1,
        byte stencil = 0
    ) {
        if (clearRtv && rtv != null) ClearRenderTargetView(rtv, color);

        if (clearDsv && dsv != null) ClearDepthStencilView(dsv, flags, depth, stencil);

        SetRenderTarget(dsv, rtv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRenderTarget(RenderTargetView? rtv, bool clearRtv, Color4 color) {
        if (clearRtv && rtv != null)
            ClearRenderTargetView(rtv, color);
        SetRenderTarget(null, rtv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetDepthStencilView(out DepthStencilView? depthStencilViewRef)
        => NativeContext.GetDepthStencilView(out depthStencilViewRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderTargetView?[] GetRenderTargets(int numViews)
        => NativeContext.GetRenderTargets(numViews);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderTargetView?[] GetRenderTargets(int numViews, out DepthStencilView? depthStencilViewRef)
        => NativeContext.GetRenderTargets(numViews, out depthStencilViewRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnorderedAccessView?[] GetUnorderedAccessViews(int startSlot, int count)
        => NativeContext.GetUnorderedAccessViews(startSlot, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRenderTargets(DX11RenderBufferProxyBase buffer, Color4 color)
        => buffer.ClearRenderTarget(this, color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearDepthStencilView(
        DepthStencilView? view,
        DepthStencilClearFlags clearFlag,
        float depth = 1,
        byte stencil = 0
    ) {
        if (view is not null)
            NativeContext.ClearDepthStencilView(view, clearFlag, depth, stencil);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRenderTargetView(RenderTargetView? renderTargetViewRef, Color4 colorRgba) {
        if (renderTargetViewRef is not null)
            NativeContext.ClearRenderTargetView(renderTargetViewRef, colorRgba);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRenderTagetBindings()
        => NativeContext.ClearRenderTargetBindings();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, Int4 values)
        => NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, Vector4 values)
        => NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, ref Int4 values)
        => NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, ref Vector4 values)
        => NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOutputUav(int slot, UnorderedAccessView uav)
        => NativeContext.SetOutputUnorderedAccessView(slot, uav);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOutputUaVs(int startSlot, UnorderedAccessView[] uavs)
        => NativeContext.SetOutputUnorderedAccessViews(startSlot, uavs);
}
