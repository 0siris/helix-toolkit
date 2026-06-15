/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
        public static class ScreenCapture
        {
            static readonly ILogger logger = Logger.LogManager.Create(nameof(ScreenCapture));

            /// <summary>
            /// Captures the texture.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="source">The source.</param>
            /// <param name="stagingTexture">The staging texture.</param>
            /// <returns></returns>
            public static bool CaptureTexture(Render.DeviceContextProxy context, Texture2D source, out Texture2D stagingTexture)
            {
                stagingTexture = null;
                if (context == null || source == null)
                {
                    return false;
                }

                var desc = source.Description;
                if (source.Description.SampleDescription.Count > 1)
                {
                    desc.SampleDescription.Count = 1;
                    desc.SampleDescription.Quality = 0;
                    using (var texture = context.NativeDevice.CreateTexture2D(desc))
                    {
                        for (var i = 0; i < desc.ArraySize; ++i)
                        {
                            for (var level = 0; level < desc.MipLevels; ++level)
                            {
                                var index = level + i * desc.MipLevels;
                                context.ResolveSubresource(source, index, texture, index, desc.Format);
                            }
                        }
                        desc.BindFlags = BindFlags.None;
                        desc.Usage = ResourceUsage.Staging;
                        desc.CpuAccessFlags = CpuAccessFlags.Read;
                        desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                        stagingTexture = context.NativeDevice.CreateTexture2D(desc);
                        context.CopyResource(texture, stagingTexture);
                    }
                }
                else if (desc.Usage == ResourceUsage.Staging && desc.CpuAccessFlags == CpuAccessFlags.Read)
                {
                    stagingTexture = source;
                }
                else
                {
                    desc.BindFlags = BindFlags.None;
                    desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                    desc.CpuAccessFlags = CpuAccessFlags.Read;
                    desc.Usage = ResourceUsage.Staging;
                    stagingTexture = context.NativeDevice.CreateTexture2D(desc);
                    context.CopyResource(source, stagingTexture);
                }
                return true;
            }

            /// <summary>
            /// Saves the wic texture to file.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="file">The file.</param>
            /// <param name="format">The format.</param>
            /// <returns></returns>
            public static bool SaveWICTextureToFile(IDeviceResources deviceResource, Texture2D source, string file, Direct2DImageFormat format)
            {
                return SaveWICTextureToFile(deviceResource, source, file, BitmapExtensions.ToWICImageFormat(format));
            }


            /// <summary>
            /// Saves the wic texture to file.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="fileName">Name of the file.</param>
            /// <param name="containerFormat">The container format.</param>
            /// <returns></returns>
            /// <exception cref="System.NotSupportedException"></exception>
            public static bool SaveWICTextureToFile(IDeviceResources deviceResource, Texture2D source, string fileName, Guid containerFormat)
            {
                logger.LogDebug("WIC screen capture encoding is not implemented in the Silk.NET migration path yet.");
                return false;
            }

            /// <summary>
            /// Saves the wic texture to bitmap stream.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="bitmapStream">The bitmap stream.</param>
            /// <returns></returns>
            /// <exception cref="System.NotSupportedException"></exception>
            public static bool SaveWICTextureToBitmapStream(IDeviceResources deviceResource, Texture2D source, MemoryStream bitmapStream)
            {
                logger.LogDebug("WIC screen capture encoding is not implemented in the Silk.NET migration path yet.");
                return false;
            }
        }
    }
}
