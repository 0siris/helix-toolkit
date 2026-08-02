using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core {
    namespace Render {
        public partial class DeviceContextProxy {
            private static readonly RenderTargetView[] ZeroRenderTargetArray = [];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTargets(DX11RenderBufferProxyBase buffer) {
                buffer.SetDefaultRenderTargets(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTarget(DepthStencilView dsv, RenderTargetView renderTarget) {
                NativeContext.SetRenderTargets(dsv, renderTarget);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTargets(DepthStencilView dsv, RenderTargetView[] renderTarget) {
                NativeContext.SetRenderTargets(dsv, renderTarget);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetStreamOutputTarget(Buffer buffer, int offset = 0) {
                NativeContext.SetStreamOutputTarget(buffer, offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetStreamOutputTarget(Buffer[] bufferBindings) {
                NativeContext.SetStreamOutputTargets(bufferBindings);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetDepthStencil(DepthStencilView dsv) {
                NativeContext.SetRenderTargets(dsv, ZeroRenderTargetArray);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTarget(RenderTargetView rtv) {
                NativeContext.SetRenderTargets(null, rtv);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTarget(
                DepthStencilView dsv,
                RenderTargetView rtv,
                bool clearRTV,
                Color4 color,
                bool clearDSV,
                DepthStencilClearFlags flags = DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                float depth = 1,
                byte stencil = 0
            ) {
                if (clearRTV && rtv != null) ClearRenderTargetView(rtv, color);

                if (clearDSV && dsv != null) ClearDepthStencilView(dsv, flags, depth, stencil);

                SetRenderTarget(dsv, rtv);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRenderTarget(RenderTargetView rtv, bool clearRTV, Color4 color) {
                if (clearRTV && rtv != null) ClearRenderTargetView(rtv, color);

                SetRenderTarget(null, rtv);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetDepthStencilView(out DepthStencilView depthStencilViewRef) {
                NativeContext.GetDepthStencilView(out depthStencilViewRef);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RenderTargetView[] GetRenderTargets(int numViews) {
                return NativeContext.GetRenderTargets(numViews);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RenderTargetView[] GetRenderTargets(int numViews, out DepthStencilView depthStencilViewRef) {
                return NativeContext.GetRenderTargets(numViews, out depthStencilViewRef);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UnorderedAccessView[] GetUnorderedAccessViews(int startSlot, int count) {
                return NativeContext.GetUnorderedAccessViews(startSlot, count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearRenderTargets(DX11RenderBufferProxyBase buffer, Color4 color) {
                buffer.ClearRenderTarget(this, color);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearDepthStencilView(
                DepthStencilView view,
                DepthStencilClearFlags clearFlag,
                float depth = 1,
                byte stencil = 0
            ) {
                NativeContext.ClearDepthStencilView(view, clearFlag, depth, stencil);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearRenderTargetView(RenderTargetView renderTargetViewRef, Color4 colorRGBA) {
                NativeContext.ClearRenderTargetView(renderTargetViewRef, colorRGBA);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearRenderTagetBindings() {
                NativeContext.ClearRenderTargetBindings();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, Int4 values) {
                NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, Vector4 values) {
                NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, ref Int4 values) {
                NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessViewRef, ref Vector4 values) {
                NativeContext.ClearUnorderedAccessView(unorderedAccessViewRef, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetOutputUAV(int slot, UnorderedAccessView uav) {
                NativeContext.SetOutputUnorderedAccessView(slot, uav);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetOutputUAVs(int startSlot, UnorderedAccessView[] uavs) {
                NativeContext.SetOutputUnorderedAccessViews(startSlot, uavs);
            }
        }
    }
}
