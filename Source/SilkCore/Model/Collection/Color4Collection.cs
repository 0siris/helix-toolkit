/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Globalization;
using System.Text;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core;

#if !NETFX_CORE
    [Serializable]
    [TypeConverter(typeof(Color4CollectionConverter))]
#endif
public sealed class Color4Collection : FastList<Color4> {
    public Color4Collection() { }

    public Color4Collection(int capacity)
        : base(capacity) { }

    public Color4Collection(IEnumerable<Color4> items)
        : base(items) { }

    public static Color4Collection Parse(string source) {
        IFormatProvider formatProvider = CultureInfo.InvariantCulture;

        var th = new TokenizerHelper(source, formatProvider);
        var resource = new Color4Collection();

        Color4 value;

        while (th.NextToken()) {
            value = new Color4(Convert.ToSingle(th.GetCurrentToken(), formatProvider),
                               Convert.ToSingle(th.NextTokenRequired(), formatProvider),
                               Convert.ToSingle(th.NextTokenRequired(), formatProvider),
                               Convert.ToSingle(th.NextTokenRequired(), formatProvider));

            resource.Add(value);
        }

        return resource;
    }

    public string ConvertToString(string format, IFormatProvider provider) {
        if (Count == 0) return string.Empty;

        var str = new StringBuilder();
        for (var i = 0; i < Count; i++) {
            //str.AppendFormat(provider, "{0:" + format + "}", this[i]);
            str.AppendFormat(provider,
                             "{0},{1},{2},{3}",
                             this[i].GetRed(),
                             this[i].GetGreen(),
                             this[i].GetBlue(),
                             this[i].GetAlpha());
            if (i != Count - 1) str.Append(" ");
        }

        return str.ToString();
    }
}
