using System;
using System.Windows;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Render;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.Wpf.SharpDX
{
    namespace Controls
    {
        public sealed class DX11ImageSourceArgs : EventArgs
        {
            public readonly DX11ImageSource Source;

            public DX11ImageSourceArgs(DX11ImageSource source)
            {
                Source = source;
            }
        }

        public sealed class DX11ImageSourceRenderHost : DefaultRenderHost
        {
            private static readonly ILogger logger = LogManager.Create<DX11ImageSourceRenderHost>();

            private bool frontBufferChange;
            private bool hasBackBuffer;

            private bool lastSurfaceD3DIsFrontBufferAvailable;

            private DX11ImageSource surfaceD3D;

            public DX11ImageSourceRenderHost(Func<IDevice3DResources, IRenderer> createRenderer) : base(createRenderer)
            {
                OnNewRenderTargetTexture += DX11ImageSourceRenderer_OnNewBufferCreated;
            }

            public DX11ImageSourceRenderHost()
            {
                OnNewRenderTargetTexture += DX11ImageSourceRenderer_OnNewBufferCreated;
            }

            public event EventHandler<DX11ImageSourceArgs> OnImageSourceChanged;

            protected override void PostRender()
            {
                if (!hasBackBuffer)
                {
                    logger.LogWarning("Back buffer is not set.");
                    return;
                }

                surfaceD3D?.InvalidateD3DImage();
                base.PostRender();
            }

            protected override void DisposeBuffers()
            {
                logger.LogInformation("Dispose buffers.");
                if (surfaceD3D != null)
                {
                    hasBackBuffer = false;
                    surfaceD3D.SetRenderTargetDX11(null);
                    if (!frontBufferChange)
                        surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
                    RemoveAndDispose(ref surfaceD3D);
                }

                base.DisposeBuffers();
            }

            private void DX11ImageSourceRenderer_OnNewBufferCreated(object sender, Texture2DArgs e)
            {
                try
                {
                    if (surfaceD3D == null)
                    {
                        logger.LogInformation("Create new D3DImageSource");
                        surfaceD3D = new DX11ImageSource(EffectsManager.AdapterIndex);
                        surfaceD3D.IsFrontBufferAvailableChanged += SurfaceD3D_IsFrontBufferAvailableChanged;
                    }

                    if (e.Texture != null && e.Texture.Resource is Texture2D tex2d)
                        surfaceD3D.SetRenderTargetDX11(tex2d);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to create surfaceD3D. Ex: {0}", ex.Message);
                    hasBackBuffer = false;
                    surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
                    RemoveAndDispose(ref surfaceD3D);
                    hasBackBuffer = false;
                    EndD3D();
                    ReinitializeEffectsManager();
                    return;
                }

                hasBackBuffer = e.Texture != null && e.Texture.Resource is Texture2D;
                OnImageSourceChanged(this, new DX11ImageSourceArgs(surfaceD3D));
                if (hasBackBuffer)
                    logger.LogInformation("New back buffer is set.");
                else
                    logger.LogInformation("Set back buffer failed.");
            }

            private void SurfaceD3D_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                var newValue = (bool) e.NewValue;
                if (EffectsManager == null || newValue == lastSurfaceD3DIsFrontBufferAvailable) return;

                logger.LogWarning("SurfaceD3D front buffer changed. Value = {0}, last value {1}", newValue,
                    lastSurfaceD3DIsFrontBufferAvailable);
                if (surfaceD3D != null)
                {
                    hasBackBuffer = false;
                    surfaceD3D.SetRenderTargetDX11(null);
                    surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
                    RemoveAndDispose(ref surfaceD3D);
                }

                if (newValue)
                {
                    frontBufferChange = false;
                    try
                    {
                        if (surfaceD3D?.IsDeviceStateOk() == true)
                        {
                            Restart(true);
                        }
                        else
                        {
                            EndD3D();
                            ReinitializeEffectsManager();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }
                else
                {
                    frontBufferChange = true;
                    if (surfaceD3D?.IsDeviceStateOk() != true)
                    {
                        hasBackBuffer = false;
                        EndD3D();
                    }
                }

                lastSurfaceD3DIsFrontBufferAvailable = newValue;
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                OnImageSourceChanged = null;
                if (surfaceD3D != null)
                {
                    hasBackBuffer = false;
                    surfaceD3D?.SetRenderTargetDX11(null);
                    surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
                    RemoveAndDispose(ref surfaceD3D);
                }

                base.OnDispose(disposeManagedResources);
            }
        }
    }
}