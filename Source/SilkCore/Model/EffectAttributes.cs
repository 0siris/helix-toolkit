/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        /// <summary>
        /// </summary>
        public static class EffectAttributeNames {
            public static string ColorAttributeName = "color";
        }

        /// <summary>
        /// </summary>
        public interface IEffectAttributes {
            string EffectName { get; }

            void AddAttribute(string attName, object parameter);
            void RemoveAttribute(string attName);
            object GetAttribute(string attName);
            bool TryGetAttribute(string attName, out object attribute);
        }

        /// <summary>
        /// </summary>
        public sealed class EffectAttributes : IEffectAttributes {
            private readonly Dictionary<string, object> attributes = new();

            /// <summary>
            ///     Initializes a new instance of the <see cref="EffectAttributes" /> class.
            /// </summary>
            /// <param name="name">The name.</param>
            public EffectAttributes(string name) {
                EffectName = name;
            }

            public string EffectName { get; }

            /// <summary>
            ///     Adds the attribute.
            /// </summary>
            /// <param name="attName">Name of the att.</param>
            /// <param name="parameter">The parameter.</param>
            public void AddAttribute(string attName, object parameter) {
                if (attributes.ContainsKey(attName)) return;
                attributes.Add(attName, parameter);
            }

            /// <summary>
            ///     Removes the attribute.
            /// </summary>
            /// <param name="attName">Name of the att.</param>
            public void RemoveAttribute(string attName) {
                attributes.Remove(attName);
            }

            /// <summary>
            ///     Gets the attribute.
            /// </summary>
            /// <param name="attName">Name of the att.</param>
            /// <returns></returns>
            public object GetAttribute(string attName) {
                if (attributes.TryGetValue(attName, out var obj)) return obj;

                return null;
            }

            /// <summary>
            ///     Tries the get attribute.
            /// </summary>
            /// <param name="attName">Name of the att.</param>
            /// <param name="attribute">The attribute.</param>
            /// <returns></returns>
            public bool TryGetAttribute(string attName, out object attribute) {
                return attributes.TryGetValue(attName, out attribute);
            }

            /// <summary>
            ///     Parses the specified att string.
            /// </summary>
            /// <param name="attString">The att string.</param>
            /// <returns></returns>
            public static EffectAttributes[] Parse(string attString) {
                return Parse(attString, EffectParserConfiguration.Parser);
            }

            /// <summary>
            ///     Parses the specified att string.
            /// </summary>
            /// <param name="attString">The att string.</param>
            /// <param name="parser">The parser.</param>
            /// <returns></returns>
            public static EffectAttributes[] Parse(string attString, IEffectAttributeParser parser) {
                return parser.Parse(attString);
            }
        }

        /// <summary>
        /// </summary>
        public interface IEffectAttributeParser {
            EffectAttributes[] Parse(string attString);
        }

        /// <summary>
        /// </summary>
        public static class EffectParserConfiguration {
            public static IEffectAttributeParser Parser = new DefaultEffectAttributeParser();
        }

        /// <summary>
        /// </summary>
        public sealed class DefaultEffectAttributeParser : IEffectAttributeParser {
            public static readonly char[] EffectSeparator = new[] {';', ' '};
            public static readonly char[] AttributeSeparator = new[] {',', ' '};
            public static readonly char[] AttributeNameValueSeparator = new[] {':', ' '};
            public static readonly char[] NameAttributeSeparator = new[] {'[', ']', ' '};

            /// <summary>
            ///     Parses the specified att string.
            /// </summary>
            /// <param name="attString">The att string.</param>
            /// <returns></returns>
            public EffectAttributes[] Parse(string attString) {
                var effects = attString.Split(EffectSeparator, StringSplitOptions.RemoveEmptyEntries);
                IList<EffectAttributes> attributes = new List<EffectAttributes>();
                foreach (var effect in effects) {
                    var nameAttTokens = effect.Split(NameAttributeSeparator, StringSplitOptions.RemoveEmptyEntries);
                    if (nameAttTokens.Length > 0) {
                        var att = new EffectAttributes(nameAttTokens[0]);
                        for (var i = 1; i < nameAttTokens.Length; ++i) {
                            var attTokens = nameAttTokens[i]
                                .Split(AttributeSeparator, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var attToken in attTokens) {
                                var token = attToken.Split(AttributeNameValueSeparator,
                                                           StringSplitOptions.RemoveEmptyEntries);
                                if (token.Length == 2) att.AddAttribute(token[0].ToLower(), token[1]);
                            }
                        }

                        attributes.Add(att);
                    }
                }

                return attributes.ToArray();
            }
        }
    }
}
