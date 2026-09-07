/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics;

namespace HelixToolkit.SharpDX.Core.Utilities;

public static class ScreenCapture {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private static ImageFileType ToImageFileType(Guid containerFormat) {
        if (containerFormat == Direct2DImageFormat.Png.ToWicImageFormat()) return ImageFileType.Png;
        if (containerFormat == Direct2DImageFormat.Jpeg.ToWicImageFormat()) return ImageFileType.Jpg;
        if (containerFormat == Direct2DImageFormat.Gif.ToWicImageFormat()) return ImageFileType.Gif;
        if (containerFormat == Direct2DImageFormat.Tiff.ToWicImageFormat()) return ImageFileType.Tiff;
        if (containerFormat == Direct2DImageFormat.Wmp.ToWicImageFormat()) return ImageFileType.Wmp;
        return ImageFileType.Bmp;
    }
}
