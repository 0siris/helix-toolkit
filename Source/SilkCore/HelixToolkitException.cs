// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HelixToolkitException.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Represents errors that occurs in the Helix 3D Toolkit.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.SharpDX.Core;
#pragma warning disable 0436
    /// <summary>
    ///     Represents errors that occurs in the Helix 3D Toolkit.
    /// </summary>
public class HelixToolkitException : Exception {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HelixToolkitException" /> class.
        /// </summary>
        /// <param name="formatString">
        ///     The format string.
        /// </param>
        /// <param name="args">
        ///     The args.
        /// </param>
    public HelixToolkitException(string formatString, params object[] args)
        : base(string.Format(formatString, args)) { }
}
#pragma warning restore 0436
