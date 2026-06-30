/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
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
    namespace Utilities
    {
        using Native;

        /// <summary>
        /// 
        /// </summary>
        public class BitmapProxy : DisposeObject, IGUID
        {
            /// <summary>
            /// Gets the unique identifier.
            /// </summary>
            /// <value>
            /// The unique identifier.
            /// </value>
            public Guid GUID { get; } = Guid.NewGuid();
            /// <summary>
            /// Gets the properties.
            /// </summary>
            /// <value>
            /// The properties.
            /// </value>
            public D2DBitmapProperties Properties
            {
                private set; get;
            }
            /// <summary>
            /// Gets the context.
            /// </summary>
            /// <value>
            /// The context.
            /// </value>
            public D2DDeviceContext Context
            {
                private set; get;
            }
            private D2DBitmap bitmap;
            /// <summary>
            /// Gets the bitmap.
            /// </summary>
            /// <value>
            /// The bitmap.
            /// </value>
            public D2DBitmap Bitmap => bitmap;
            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name
            {
                private set; get;
            }
            /// <summary>
            /// Gets the bitmap size.
            /// </summary>
            /// <value>
            /// The size.
            /// </value>
            public Size2 Size
            {
                private set; get;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="BitmapProxy"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="context">The context.</param>
            /// <param name="size">The size.</param>
            /// <param name="properties">The properties.</param>
            /// <param name="nativeBitmap">Optional native bitmap object.</param>
            public BitmapProxy(string name, D2DDeviceContext context, Size2 size, D2DBitmapProperties properties, object nativeBitmap = null)
            {
                Properties = properties;
                Context = context;
                bitmap = nativeBitmap as D2DBitmap ?? new D2DBitmap(size, nativeBitmap);
                Size = size;
                Name = name;
            }

            /// <summary>
            /// Creates the description.
            /// </summary>
            /// <param name="dpiX">The dpi x.</param>
            /// <param name="dpiY">The dpi y.</param>
            /// <param name="format">The format.</param>
            /// <param name="alphaMode">The alpha mode.</param>
            /// <param name="options">The options.</param>
            /// <param name="colorContext">The color context.</param>
            /// <returns></returns>
            public static D2DBitmapProperties CreateDescription(float dpiX, float dpiY, Format format,
                D2DAlphaMode alphaMode = D2DAlphaMode.Premultiplied,
                D2DBitmapOptions options = D2DBitmapOptions.Target | D2DBitmapOptions.CannotDraw, D2DColorContext colorContext = null)
            {
                // Make sure that the texture to create is a render target.
                options |= D2DBitmapOptions.Target;
                var description = NewDescription(dpiX, dpiY, new D2DPixelFormat(format, alphaMode), options, colorContext);
                return description;
            }

            /// <summary>
            /// News the description.
            /// </summary>
            /// <param name="dpiX">The dpi x.</param>
            /// <param name="dpiY">The dpi y.</param>
            /// <param name="format">The format.</param>
            /// <param name="bitmapOptions">The bitmap options.</param>
            /// <param name="colorContext">The color context.</param>
            /// <returns></returns>
            protected static D2DBitmapProperties NewDescription(float dpiX, float dpiY, D2DPixelFormat format,
                D2DBitmapOptions bitmapOptions, D2DColorContext colorContext)
            {
                return new D2DBitmapProperties(format, dpiX, dpiY, bitmapOptions, colorContext);
            }

            /// <summary>
            /// Creates by native surface.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="context">The context.</param>
            /// <param name="surface">The surface.</param>
            /// <returns></returns>
            public static BitmapProxy Create(string name, D2DDeviceContext context, object surface)
            {
                if (surface is Texture2D texture)
                {
                    var description = CreateDescription(
                        context.DotsPerInch.Width,
                        context.DotsPerInch.Height,
                        texture.Description.Format);
                    var bitmap = context.CreateTargetBitmap(texture, description);
                    return new BitmapProxy(
                        name,
                        context,
                        new Size2(texture.Description.Width, texture.Description.Height),
                        description,
                        bitmap);
                }
                return new BitmapProxy(name, context, default, CreateDescription(context.DotsPerInch.Width, context.DotsPerInch.Height, default), surface);
            }

            /// <summary>
            /// Creates by size and format.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="context">The context.</param>
            /// <param name="size">The size.</param>
            /// <param name="format">The format.</param>
            /// <returns></returns>
            public static BitmapProxy Create(string name, D2DDeviceContext context, Size2 size, Format format)
            {
                return new BitmapProxy(name, context, size, CreateDescription(context.DotsPerInch.Width, context.DotsPerInch.Height, format, D2DAlphaMode.Premultiplied, D2DBitmapOptions.Target));
            }

            public static BitmapProxy CreateEmpty(string name, D2DDeviceContext context)
            {
                return new BitmapProxy(name, context, default, CreateDescription(context.DotsPerInch.Width, context.DotsPerInch.Height, default));
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref bitmap);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
