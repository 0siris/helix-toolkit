/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Globalization;
using HelixToolkit.SharpDX.Core.Model.Material;

namespace HelixToolkit.SharpDX.Core.Utilities;

public sealed class StreamToTextureModelConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(Stream);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) 
        => destinationType == typeof(TextureModel);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
        if (value is Stream st) 
            return new TextureModel(st);
        return null;
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType
    )
        => null;
}
