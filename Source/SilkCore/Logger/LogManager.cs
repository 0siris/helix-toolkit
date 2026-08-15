using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Logger;

/// <summary>
/// </summary>
public static class LogManager {
    /// <summary>
    ///     Replace factory at app start up to use custom logger.
    /// </summary>
    public static ILoggerFactory Factory { get; set; } = new LoggerLibLoggerFactory();

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ILogger Create<T>() => Factory.CreateLogger<T>();

    /// <summary>
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public static ILogger Create(string categoryName) => Factory.CreateLogger(categoryName);
}
