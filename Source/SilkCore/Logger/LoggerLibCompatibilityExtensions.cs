using LoggerLib;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace HelixToolkit.Logger;

/// <summary>
/// Provides log-level compatibility checks for LoggerLib-backed loggers.
/// </summary>
public static class LoggerLibCompatibilityExtensions {
    /// <summary>
    /// Determines whether the requested log level is enabled by the configured logger.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="logLevel">The requested log level.</param>
    /// <returns><see langword="true" /> when the level is enabled; otherwise, <see langword="false" />.</returns>
    public static bool IsEnabled(this ILog logger, LogLevel logLevel) =>
        logLevel != LogLevel.None && logger.MinimumLevel <= ToSerilogLevel(logLevel);

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel) => logLevel switch {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        _ => LogEventLevel.Fatal
    };
}