/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;

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
    namespace Native
    {
        using Utilities;

        public abstract class D2DNativeResource : IDisposable
        {
            protected D2DNativeResource(object nativeResource = null)
            {
                NativeResource = nativeResource;
            }

            public object NativeResource { get; }

            public bool IsDisposed { get; private set; }

            public virtual void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                if (NativeResource is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                IsDisposed = true;
            }
        }

        public sealed class D2DFactory : D2DNativeResource
        {
            public D2DFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DDevice : D2DNativeResource
        {
            public D2DDevice(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DDeviceContext : D2DNativeResource
        {
            public D2DDeviceContext(object nativeResource = null)
                : base(nativeResource)
            {
            }

            public BitmapProxy Target { get; set; }

            public D2DSizeF DotsPerInch { get; set; } = new D2DSizeF(96, 96);

            public int MaximumBitmapSize { get; set; }

            public void BeginDraw()
            {
            }

            public void EndDraw()
            {
            }

            public void Clear(Color4 color)
            {
            }
        }

        public sealed class WICImagingFactory : D2DNativeResource
        {
            public WICImagingFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class DirectWriteFactory : D2DNativeResource
        {
            public DirectWriteFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DColorContext : D2DNativeResource
        {
            public D2DColorContext(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DBitmap : D2DNativeResource
        {
            public D2DBitmap(Size2 size, object nativeResource = null)
                : base(nativeResource)
            {
                Size = new D2DSizeF(size.Width, size.Height);
            }

            public D2DSizeF Size { get; }
        }

        [Flags]
        public enum D2DBitmapOptions
        {
            None = 0,
            Target = 1,
            CannotDraw = 2
        }

        public enum D2DAlphaMode
        {
            Unknown = 0,
            Premultiplied = 1,
            Straight = 2,
            Ignore = 3
        }

        public struct D2DPixelFormat
        {
            public D2DPixelFormat(Format format, D2DAlphaMode alphaMode)
            {
                Format = format;
                AlphaMode = alphaMode;
            }

            public Format Format;
            public D2DAlphaMode AlphaMode;
        }

        public sealed class D2DBitmapProperties
        {
            public D2DBitmapProperties(D2DPixelFormat pixelFormat, float dpiX, float dpiY, D2DBitmapOptions options, D2DColorContext colorContext = null)
            {
                PixelFormat = pixelFormat;
                DpiX = dpiX;
                DpiY = dpiY;
                Options = options;
                ColorContext = colorContext;
            }

            public D2DPixelFormat PixelFormat { get; }

            public float DpiX { get; }

            public float DpiY { get; }

            public D2DBitmapOptions Options { get; }

            public D2DColorContext ColorContext { get; }
        }

        public readonly struct D2DSizeF
        {
            public D2DSizeF(float width, float height)
            {
                Width = width;
                Height = height;
            }

            public float Width { get; }

            public float Height { get; }
        }
    }
}
