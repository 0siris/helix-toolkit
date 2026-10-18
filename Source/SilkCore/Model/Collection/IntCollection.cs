/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Globalization;
using System.Text;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core;

public sealed class IntCollection : FastList<int> {
    public IntCollection() { }

    public IntCollection(int capacity)
        : base(capacity) { }

    public IntCollection(IEnumerable<int> items)
        : base(items) { }

    public static IntCollection Parse(string source) {
        IFormatProvider formatProvider = CultureInfo.InvariantCulture;
        var th = new TokenizerHelper(source, formatProvider);
        var resource = new IntCollection();
        while (th.NextToken()) {
            var value = Convert.ToInt32(th.GetCurrentToken(), formatProvider);
            resource.Add(value);
        }

        return resource;
    }

    public string ConvertToString(string? format, IFormatProvider? provider) {
        if (Count == 0) return string.Empty;

        var str = new StringBuilder();
        for (var i = 0; i < Count; i++) {
            str.AppendFormat(provider, "{0:" + format + "}", this[i]);
            if (i != Count - 1) str.Append(" ");
        }

        return str.ToString();
    }
}
