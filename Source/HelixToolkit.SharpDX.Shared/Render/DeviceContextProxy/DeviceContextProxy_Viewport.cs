using System.Runtime.CompilerServices;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
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
            public void SetViewport(float x, float y, float width, float height, float minZ = 0, float maxZ = 1)
            {
                nativeDeviceContext.SetViewport(x, y, width, height, minZ, maxZ);
            }

            #endregion Viewport and Scissors
        }
    }
}
