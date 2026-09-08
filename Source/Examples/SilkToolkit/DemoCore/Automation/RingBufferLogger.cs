// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RingBufferLogger.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using LoggerLib;
using Serilog.Events;

/// <summary>
///     Log entry captured by <see cref="RingBufferLogger" />.
/// </summary>
/// <param name="Time">Capture time.</param>
/// <param name="Level">Lowercase level (verbose|debug|info|warn|error).</param>
/// <param name="Message">Rendered message.</param>
/// <param name="Caller">Caller location.</param>
public sealed record LogEntry(DateTimeOffset Time, string Level, string Message, string? Caller);

/// <summary>
///     Pure log filtering for endpoints and tests.
/// </summary>
public static class LogQuery {
    /// <summary>
    ///     Log level ranks for filtering.
    /// </summary>
    private static readonly Dictionary<string, int> Ranks = new(StringComparer.OrdinalIgnoreCase) {
        ["verbose"] = 0,
        ["debug"] = 1,
        ["info"] = 2,
        ["warn"] = 3,
        ["error"] = 4,
        ["fatal"] = 5,
    };

    /// <summary>
    ///     Parses a level string to a Serilog level.
    /// </summary>
    /// <param name="level">The level string.</param>
    /// <returns>The level.</returns>
    public static LogEventLevel ParseLevel(string level) =>
        level.ToLowerInvariant() switch {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "info" => LogEventLevel.Information,
            "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => throw new ArgumentException($"unknown-level: '{level}'.", nameof(level)),
        };

    /// <summary>
    ///     Filters entries by minimum level, newest first, capped to limit.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="level">Optional minimum level string.</param>
    /// <param name="limit">Maximum entry count.</param>
    /// <returns>The filtered entries.</returns>
    public static IReadOnlyList<LogEntry> Apply(IEnumerable<LogEntry> entries, string? level, int limit) {
        var threshold = level is null ? int.MinValue : RankOf(level);
        return entries.Where(entry => RankOf(entry.Level) >= threshold).Reverse().Take(limit).ToList();
    }

    /// <summary>
    ///     Gets the rank of a level string.
    /// </summary>
    /// <param name="level">The level string.</param>
    /// <returns>The rank.</returns>
    private static int RankOf(string level) =>
        Ranks.TryGetValue(level, out var rank)
            ? rank
            : throw new ArgumentException($"unknown-level: '{level}'.", nameof(level));
}

/// <summary>
///     Ring-buffer <see cref="ILog" /> decorator. Captures the last N entries and forwards everything to an inner logger.
/// </summary>
public sealed class RingBufferLogger : ILog {
    /// <summary>
    ///     Captured entries (oldest first).
    /// </summary>
    private readonly ConcurrentQueue<LogEntry> entries = new();

    /// <summary>
    ///     Trims the buffer.
    /// </summary>
    private readonly object gate = new();

    /// <summary>
    ///     The inner logger receiving every call.
    /// </summary>
    private readonly ILog inner;

    /// <summary>
    ///     Maximum retained entries.
    /// </summary>
    private readonly int capacity;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RingBufferLogger" /> class.
    /// </summary>
    /// <param name="inner">The inner logger.</param>
    /// <param name="capacity">Maximum retained entries.</param>
    public RingBufferLogger(ILog inner, int capacity = 500) {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        this.inner = inner;
        this.capacity = capacity;
    }

    /// <inheritdoc />
    public LogEventLevel MinimumLevel {
        get => inner.MinimumLevel;
        set => inner.MinimumLevel = value;
    }

    /// <summary>
    ///     Gets a snapshot of captured entries (oldest first).
    /// </summary>
    /// <returns>The snapshot.</returns>
    public LogEntry[] Snapshot() => entries.ToArray();

    /// <inheritdoc />
    public void Trace(
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", null, "Trace", null, path, line, method);
        inner.Trace(path, line, method);
    }

    /// <inheritdoc />
    public void VerboseTrace(
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", null, "Trace", null, path, line, method);
        inner.VerboseTrace(path, line, method);
    }

    /// <inheritdoc />
    public void Verbose(
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", null, message, propertyValues, path, line, method);
        inner.Verbose(message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1>(
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", null, message, [p1], path, line, method);
        inner.Verbose(message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1, T2>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", null, message, [p1, p2], path, line, method);
        inner.Verbose(message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1, T2, T3>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", null, message, [p1, p2, p3], path, line, method);
        inner.Verbose(message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose(
        Exception exception,
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", exception, message, propertyValues, path, line, method);
        inner.Verbose(exception, message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", exception, message, [p1], path, line, method);
        inner.Verbose(exception, message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1, T2>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", exception, message, [p1, p2], path, line, method);
        inner.Verbose(exception, message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Verbose<T1, T2, T3>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("verbose", exception, message, [p1, p2, p3], path, line, method);
        inner.Verbose(exception, message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Debug(
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", null, message, propertyValues, path, line, method);
        inner.Debug(message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1>(
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", null, message, [p1], path, line, method);
        inner.Debug(message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1, T2>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", null, message, [p1, p2], path, line, method);
        inner.Debug(message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1, T2, T3>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", null, message, [p1, p2, p3], path, line, method);
        inner.Debug(message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Debug(
        Exception exception,
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", exception, message, propertyValues, path, line, method);
        inner.Debug(exception, message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", exception, message, [p1], path, line, method);
        inner.Debug(exception, message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1, T2>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", exception, message, [p1, p2], path, line, method);
        inner.Debug(exception, message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Debug<T1, T2, T3>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("debug", exception, message, [p1, p2, p3], path, line, method);
        inner.Debug(exception, message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Info(
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", null, message, propertyValues, path, line, method);
        inner.Info(message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1>(
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", null, message, [p1], path, line, method);
        inner.Info(message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1, T2>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", null, message, [p1, p2], path, line, method);
        inner.Info(message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1, T2, T3>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", null, message, [p1, p2, p3], path, line, method);
        inner.Info(message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Info(
        Exception exception,
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", exception, message, propertyValues, path, line, method);
        inner.Info(exception, message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", exception, message, [p1], path, line, method);
        inner.Info(exception, message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1, T2>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", exception, message, [p1, p2], path, line, method);
        inner.Info(exception, message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Info<T1, T2, T3>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("info", exception, message, [p1, p2, p3], path, line, method);
        inner.Info(exception, message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Warn(
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", null, message, propertyValues, path, line, method);
        inner.Warn(message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1>(
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", null, message, [p1], path, line, method);
        inner.Warn(message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1, T2>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", null, message, [p1, p2], path, line, method);
        inner.Warn(message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1, T2, T3>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", null, message, [p1, p2, p3], path, line, method);
        inner.Warn(message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Warn(
        Exception exception,
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", exception, message, propertyValues, path, line, method);
        inner.Warn(exception, message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", exception, message, [p1], path, line, method);
        inner.Warn(exception, message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1, T2>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", exception, message, [p1, p2], path, line, method);
        inner.Warn(exception, message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Warn<T1, T2, T3>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("warn", exception, message, [p1, p2, p3], path, line, method);
        inner.Warn(exception, message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Error(
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", null, message, propertyValues, path, line, method);
        inner.Error(message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1>(
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", null, message, [p1], path, line, method);
        inner.Error(message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1, T2>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", null, message, [p1, p2], path, line, method);
        inner.Error(message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1, T2, T3>(
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", null, message, [p1, p2, p3], path, line, method);
        inner.Error(message, p1, p2, p3, path, line, method);
    }

    /// <inheritdoc />
    public void Error(
        Exception exception,
        [Localizable(false)] string message,
        object[]? propertyValues = null,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", exception, message, propertyValues, path, line, method);
        inner.Error(exception, message, propertyValues, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", exception, message, [p1], path, line, method);
        inner.Error(exception, message, p1, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1, T2>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", exception, message, [p1, p2], path, line, method);
        inner.Error(exception, message, p1, p2, path, line, method);
    }

    /// <inheritdoc />
    public void Error<T1, T2, T3>(
        Exception exception,
        [Localizable(false)] string message,
        T1 p1,
        T2 p2,
        T3 p3,
        [CallerFilePath] string path = "?",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string method = "?") {
        Emit("error", exception, message, [p1, p2, p3], path, line, method);
        inner.Error(exception, message, p1, p2, p3, path, line, method);
    }

    /// <summary>
    ///     Captures one entry and trims the buffer.
    /// </summary>
    /// <param name="level">Lowercase level.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="message">Message template.</param>
    /// <param name="propertyValues">Template values.</param>
    /// <param name="path">Caller path.</param>
    /// <param name="line">Caller line.</param>
    /// <param name="method">Caller member.</param>
    private void Emit(
        string level,
        Exception? exception,
        string message,
        object?[]? propertyValues,
        string path,
        int line,
        string method) {
        var rendered = RenderTemplate(message, propertyValues);
        var text = exception is null ? rendered : $"{rendered} ({exception.GetType().Name}: {exception.Message})";
        lock (gate) {
            entries.Enqueue(new LogEntry(
                DateTimeOffset.UtcNow,
                level,
                text,
                $"{Path.GetFileName(path)}:{line} ({method})"));
            while (entries.Count > capacity) {
                entries.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    ///     Renders a message template by substituting {placeholders} in order.
    /// </summary>
    /// <param name="message">The template.</param>
    /// <param name="values">The values.</param>
    /// <returns>The rendered message.</returns>
    public static string RenderTemplate(string message, object?[]? values) {
        if (values is null || values.Length == 0) {
            return message;
        }

        var builder = new StringBuilder(message.Length + (values.Length * 8));
        var valueIndex = 0;
        var index = 0;
        while (index < message.Length) {
            var open = message.IndexOf('{', index);
            if (open < 0 || valueIndex >= values.Length) {
                builder.Append(message, index, message.Length - index);
                break;
            }

            if (open + 1 < message.Length && message[open + 1] == '{') {
                builder.Append(message, index, open - index + 1);
                index = open + 2;
                continue;
            }

            var close = message.IndexOf('}', open);
            if (close < 0) {
                builder.Append(message, index, message.Length - index);
                break;
            }

            builder.Append(message, index, open - index);
            builder.Append(values[valueIndex++]);
            index = close + 1;
        }

        return builder.ToString();
    }
}
