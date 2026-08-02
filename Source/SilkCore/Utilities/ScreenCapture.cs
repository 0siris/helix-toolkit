/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Render;
using Microsoft.Extensions.Logging;
using SharpDX.Toolkit.Graphics;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public static class ScreenCapture {
            private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

            /// <summary>
            ///     Captures the texture.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="source">The source.</param>
            /// <param name="stagingTexture">The staging texture.</param>
            /// <returns></returns>
            public static bool CaptureTexture(
                DeviceContextProxy context,
                Texture2D source,
                out Texture2D stagingTexture
            ) {
                stagingTexture = null;
                if (context == null || source == null) return false;

                var desc = source.Description;
                if (source.Description.SampleDescription.Count > 1) {
                    desc.SampleDescription.Count = 1;
                    desc.SampleDescription.Quality = 0;
                    using var texture = context.NativeDevice.CreateTexture2D(desc);
                    for (var i = 0; i < desc.ArraySize; ++i)
                        for (var level = 0; level < desc.MipLevels; ++level) {
                            var index = level + i * desc.MipLevels;
                            context.ResolveSubresource(source, index, texture, index, desc.Format);
                        }

                    desc.BindFlags = BindFlags.None;
                    desc.Usage = ResourceUsage.Staging;
                    desc.CpuAccessFlags = CpuAccessFlags.Read;
                    desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                    stagingTexture = context.NativeDevice.CreateTexture2D(desc);
                    context.CopyResource(texture, stagingTexture);
                } else if (desc.Usage == ResourceUsage.Staging && desc.CpuAccessFlags == CpuAccessFlags.Read) {
                    stagingTexture = source;
                } else {
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
            ///     Saves the wic texture to file.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="file">The file.</param>
            /// <param name="format">The format.</param>
            /// <returns></returns>
            public static bool SaveWICTextureToFile(
                IDeviceResources deviceResource,
                Texture2D source,
                string file,
                Direct2DImageFormat format
            ) {
                return SaveWICTextureToFile(deviceResource, source, file, format.ToWICImageFormat());
            }


            /// <summary>
            ///     Saves the wic texture to file.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="fileName">Name of the file.</param>
            /// <param name="containerFormat">The container format.</param>
            /// <returns></returns>
            /// <exception cref="System.NotSupportedException"></exception>
            public static bool SaveWICTextureToFile(
                IDeviceResources deviceResource,
                Texture2D source,
                string fileName,
                Guid containerFormat
            ) {
                if (string.IsNullOrWhiteSpace(fileName)) return false;
                using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                return SaveWICTexture(deviceResource, source, stream, ToImageFileType(containerFormat));
            }

            /// <summary>
            ///     Saves the wic texture to bitmap stream.
            /// </summary>
            /// <param name="deviceResource">The device resource.</param>
            /// <param name="source">The source.</param>
            /// <param name="bitmapStream">The bitmap stream.</param>
            /// <returns></returns>
            /// <exception cref="System.NotSupportedException"></exception>
            public static bool SaveWICTextureToBitmapStream(
                IDeviceResources deviceResource,
                Texture2D source,
                MemoryStream bitmapStream
            ) {
                return SaveWICTexture(deviceResource, source, bitmapStream, ImageFileType.Bmp);
            }

            internal static bool SaveWICTextureToStream(
                IDeviceResources deviceResource,
                Texture2D source,
                Stream stream,
                Direct2DImageFormat format
            ) {
                return SaveWICTexture(deviceResource, source, stream, ToImageFileType(format.ToWICImageFormat()));
            }

            private static bool SaveWICTexture(
                IDeviceResources deviceResource,
                Texture2D source,
                Stream stream,
                ImageFileType fileType
            ) {
                if (deviceResource?.NativeDeviceResources?.ImmediateContext == null || source == null ||
                    stream == null) return false;

                var context = deviceResource.NativeDeviceResources.ImmediateContext;
                if (!CaptureTexture(new DeviceContextProxy(context, deviceResource.NativeDeviceResources.Device),
                                    source,
                                    out var stagingTexture)) return false;

                var disposeStaging = !ReferenceEquals(stagingTexture, source);
                try {
                    var data = context.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None);
                    try {
                        if (stagingTexture.Description.Format != Format.FormatB8G8R8A8Unorm) {
                            Logger.Warn("Screen capture format {Value0} is not supported for WPF encoding.",
                                              stagingTexture.Description.Format);
                            return false;
                        }

                        WICHelper.SaveBgra32(data.DataPointer,
                                             stagingTexture.Description.Width,
                                             stagingTexture.Description.Height,
                                             data.RowPitch,
                                             stream,
                                             fileType);
                        stream.Position = 0;
                        return true;
                    } finally {
                        context.UnmapSubresource(stagingTexture, 0);
                    }
                } finally {
                    if (disposeStaging) stagingTexture.Dispose();
                }
            }

            private static ImageFileType ToImageFileType(Guid containerFormat) {
                if (containerFormat == Direct2DImageFormat.Png.ToWICImageFormat()) return ImageFileType.Png;
                if (containerFormat == Direct2DImageFormat.Jpeg.ToWICImageFormat()) return ImageFileType.Jpg;
                if (containerFormat == Direct2DImageFormat.Gif.ToWICImageFormat()) return ImageFileType.Gif;
                if (containerFormat == Direct2DImageFormat.Tiff.ToWICImageFormat()) return ImageFileType.Tiff;
                if (containerFormat == Direct2DImageFormat.Wmp.ToWICImageFormat()) return ImageFileType.Wmp;
                return ImageFileType.Bmp;
            }
        }
    }
}
