using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core
{
    namespace Render
    {
        public partial class DeviceContextProxy
        {
            #region Viewport and Scissors

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetScissorRectangle(int left, int top, int right, int bottom)
            {
                nativeDeviceContext.SetScissorRectangle(left, top, right, bottom);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetScissorRectangle(ref ViewportF viewport)
            {
                SetScissorRectangle((int) viewport.X, (int) viewport.Y, (int) (viewport.X + viewport.Width),
                    (int) (viewport.Y + viewport.Height));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetViewport(float x, float y, float width, float height, float minZ = 0, float maxZ = 1)
            {
                nativeDeviceContext.SetViewport(x, y, width, height, minZ, maxZ);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetViewport(ref ViewportF viewport)
            {
                SetViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth,
                    viewport.MaxDepth);
            }

            #endregion Viewport and Scissors
        }
    }
}