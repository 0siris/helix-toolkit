// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DX11ImageSource.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
#if COREWPF
using HelixToolkit.SharpDX.Core;
#endif

namespace HelixToolkit.Wpf.SharpDX
{
    public sealed class DX11ImageSource : D3DImage, IDisposable
    {
        private Texture2D renderTarget;

        public DX11ImageSource(int adapterIndex = 0)
        {
        }

        public void InvalidateD3DImage()
        {
            if (renderTarget == null)
            {
                return;
            }

            base.Lock();
            try
            {
                if (base.PixelWidth > 0 && base.PixelHeight > 0)
                {
                    base.AddDirtyRect(new Int32Rect(0, 0, base.PixelWidth, base.PixelHeight));
                }
            }
            finally
            {
                base.Unlock();
            }
        }

        public void SetRenderTargetDX11(Texture2D target)
        {
            EndD3D(false);
            if (target == null || target.IsDisposed)
            {
                return;
            }

            renderTarget = target;
        }

        private void EndD3D(bool disposeDevices)
        {
            base.Lock();
            try
            {
                base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            }
            finally
            {
                base.Unlock();
            }
            renderTarget = null;
        }

        public bool IsDeviceStateOk()
        {
            return true;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        [SuppressMessage("Microsoft.Usage", "CA2213: Disposable fields should be disposed", Justification = "False positive.")]
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    EndD3D(true);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
