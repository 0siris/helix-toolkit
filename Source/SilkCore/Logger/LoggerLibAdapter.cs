using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace HelixToolkit.Logger;

/// <summary>
/// Creates Microsoft logging adapters backed by the configured LoggerLib instance.
/// </summary>
internal sealed class LoggerLibLoggerFactory : ILoggerFactory {
    public ILogger CreateLogger(string categoryName) => new LoggerLibLoggerAdapter(categoryName);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }
}

/// <summary>
/// Adapts Microsoft logging state and templates to the LoggerLib message-template API.
/// </summary>
internal sealed class LoggerLibLoggerAdapter(string categoryName) : ILogger {
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && LoggerLib.Logger.MinimumLevel <= ToSerilogLevel(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        if (!IsEnabled(logLevel) || formatter is null)
            return;

        var logData = ExtractLogData(state, exception, formatter);
        if (string.IsNullOrWhiteSpace(logData.Template))
            return;

        switch (logLevel) {
            case LogLevel.Trace:
                WriteVerbose(logData, exception);
                break;
            case LogLevel.Debug:
                WriteDebug(logData, exception);
                break;
            case LogLevel.Information:
                WriteInfo(logData, exception);
                break;
            case LogLevel.Warning:
                WriteWarn(logData, exception);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                WriteError(logData, exception);
                break;
        }
    }

    private LogData ExtractLogData<TState>(
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        if (state is IEnumerable<KeyValuePair<string, object?>> structuredState) {
            string? originalFormat = null;
            var values = new List<object?>();

            foreach (var pair in structuredState) {
                if (pair.Key is "{OriginalFormat}" or "OriginalFormat")
                    originalFormat = pair.Value?.ToString();
                else
                    values.Add(pair.Value);
            }

            if (!string.IsNullOrWhiteSpace(originalFormat)) {
                var arguments = new object[values.Count + 1];
                arguments[0] = categoryName;
                for (var index = 0; index < values.Count; index++)
                    arguments[index + 1] = values[index]!;

                return new LogData(string.Concat("[{Category}] ", originalFormat), arguments);
            }
        }

        var message = formatter(state, exception);
        return new LogData("[{Category}] {Message}", [categoryName, message]);
    }

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel) => logLevel switch {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Fatal
    };

    private static void WriteVerbose(LogData data, Exception? exception) {
        if (exception is null)
            LoggerLib.Logger.Verbose(data.Template, data.Arguments);
        else
            LoggerLib.Logger.Verbose(exception, data.Template, data.Arguments);
    }

    private static void WriteDebug(LogData data, Exception? exception) {
        if (exception is null)
            LoggerLib.Logger.Debug(data.Template, data.Arguments);
        else
            LoggerLib.Logger.Debug(exception, data.Template, data.Arguments);
    }

    private static void WriteInfo(LogData data, Exception? exception) {
        if (exception is null)
            LoggerLib.Logger.Info(data.Template, data.Arguments);
        else
            LoggerLib.Logger.Info(exception, data.Template, data.Arguments);
    }

    private static void WriteWarn(LogData data, Exception? exception) {
        if (exception is null)
            LoggerLib.Logger.Warn(data.Template, data.Arguments);
        else
            LoggerLib.Logger.Warn(exception, data.Template, data.Arguments);
    }

    private static void WriteError(LogData data, Exception? exception) {
        if (exception is null)
            LoggerLib.Logger.Error(data.Template, data.Arguments);
        else
            LoggerLib.Logger.Error(exception, data.Template, data.Arguments);
    }

    private readonly record struct LogData(string Template, object[]? Arguments);

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        private NullScope() { }

        public void Dispose() { }
    }
}