// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeConverter.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Utilities;
using Color = System.Windows.Media.Color;

namespace HelixToolkit.Wpf.SharpDX
{
    namespace Utilities
    {
        public abstract class FromToStringTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;

                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string)) return true;

                return base.CanConvertTo(context, destinationType);
            }
        }

        public sealed class Vector2CollectionConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null) return Vector2Collection.Parse(source);

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType != null && value is Vector2Collection instance)
                    if (destinationType == typeof(string))
                        return instance.ConvertToString(null, culture);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Vector3CollectionConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null) return Vector3Collection.Parse(source);

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType != null && value is Vector3Collection instance)
                    if (destinationType == typeof(string))
                        return instance.ConvertToString(null, culture);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class IntCollectionConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null) return IntCollection.Parse(source);

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType != null && value is IntCollection instance)
                    if (destinationType == typeof(string))
                        return instance.ConvertToString(null, culture);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Color4CollectionConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null) return Color4Collection.Parse(source);

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType != null && value is Color4Collection instance)
                    if (destinationType == typeof(string))
                        return instance.ConvertToString(null, culture);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class ColorConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);
                if (value is string source)
                {
                    try
                    {
                        var c = System.Windows.Media.ColorConverter.ConvertFromString(source);
                        if (c != null)
                        {
                            var color = (Color) c;
                            return new HelixToolkit.SharpDX.Core.Color(color.R, color.G, color.B, color.A);
                        }
                    }
                    catch (FormatException)
                    {
                    }

                    var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                    var result = new HelixToolkit.SharpDX.Core.Color(
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                    return result;
                }

                if (value is Color) return (HelixToolkit.SharpDX.Core.Color) ((Color) value).ToColor4();
                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (value is HelixToolkit.SharpDX.Core.Color)
                {
                    var val = (HelixToolkit.SharpDX.Core.Color) value;
                    if (destinationType == typeof(string))
                    {
                        var str = string.Format("{0},{1},{2},{3}", val.R, val.G, val.B, val.A);
                        return str;
                    }

                    if (destinationType == typeof(Color)) return Color.FromArgb(val.A, val.R, val.G, val.B);
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Color4Converter : FromToStringTypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Color)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(Color)) return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);
                if (value is Color) return ((Color) value).ToColor4();

                var source = value as string;

                if (source != null)
                {
                    var sepChar = TokenizerHelper.GetNumericListSeparator(CultureInfo.InvariantCulture);
                    if (source.Contains(sepChar.ToString()))
                    {
                        var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                        var result = new Color4(
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                        return result;
                    }

                    try
                    {
                        var obj = System.Windows.Media.ColorConverter.ConvertFromString(source);
                        if (obj is Color color) return color.ToColor4();
                    }
                    catch (Exception)
                    {
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (value is Color4)
                {
                    var val = (Color4) value;
                    if (destinationType == typeof(Color)) return val.ToColor();

                    if (destinationType == typeof(string))
                    {
                        var str = string.Format("{0},{1},{2},{3}", val.X, val.Y, val.Z, val.W);
                        return str;
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Vector2Converter : FromToStringTypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Vector) || sourceType == typeof(Point)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(Vector) || destinationType == typeof(Point)) return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);
                if (value is Vector)
                {
                    var source = (Vector) value;
                    return new Vector2((float) source.X, (float) source.Y);
                }

                if (value is Point3D)
                {
                    var source = (Point3D) value;
                    return new Vector2((float) source.X, (float) source.Y);
                }
                else
                {
                    var source = value as string;

                    if (source != null)
                    {
                        var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                        var result = new Vector2(
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                        return result;
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (value is Vector2)
                {
                    var val = (Vector2) value;
                    if (destinationType == typeof(Vector)) return new Vector(val.X, val.Y);

                    if (destinationType == typeof(Point)) return new Point(val.X, val.Y);

                    if (destinationType == typeof(string))
                    {
                        var str = string.Format("{0},{1}", val.X, val.Y);
                        return str;
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Vector3Converter : FromToStringTypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Vector3D) || sourceType == typeof(Point3D)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(Vector3D) || destinationType == typeof(Point3D)) return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);
                if (value is Vector3D)
                {
                    var source = (Vector3D) value;
                    return new Vector3((float) source.X, (float) source.Y, (float) source.Z);
                }

                if (value is Point3D)
                {
                    var source = (Point3D) value;
                    return new Vector3((float) source.X, (float) source.Y, (float) source.Z);
                }
                else
                {
                    var source = value as string;

                    if (source != null)
                    {
                        var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                        var result = new Vector3(
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                        return result;
                    }
                }


                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (value is Vector3)
                {
                    var val = (Vector3) value;
                    if (destinationType == typeof(Vector3D)) return new Vector3D(val.X, val.Y, val.Z);

                    if (destinationType == typeof(Point3D)) return new Point3D(val.X, val.Y, val.Z);

                    if (destinationType == typeof(string))
                    {
                        var str = string.Format("{0},{1},{2}", val.X, val.Y, val.Z);
                        return str;
                    }
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class Vector4Converter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null)
                {
                    var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                    var result = new Vector4(
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                    return result;
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType == typeof(string) && value is Vector4)
                {
                    var val = (Vector4) value;
                    var str = string.Format("{0},{1},{2},{3}", val.X, val.Y, val.Z, val.W);
                    return str;
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public sealed class QuaternionConverter : FromToStringTypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null) throw GetConvertFromException(value);

                var source = value as string;

                if (source != null)
                {
                    var th = new TokenizerHelper(source, CultureInfo.InvariantCulture);
                    var result = new Quaternion(
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(th.NextTokenRequired(), CultureInfo.InvariantCulture));
                    return result;
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                if (destinationType == typeof(string) && value is Quaternion val)
                {
                    var str = string.Format("{0},{1},{2},{3}", val.X, val.Y, val.Z, val.W);
                    return str;
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}