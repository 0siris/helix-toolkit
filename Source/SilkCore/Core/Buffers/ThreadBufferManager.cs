/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

public static class ThreadBufferManagerConfig {
    /// <summary>
    ///     Gets or sets the maximum size to retain the buffer in memory.
    ///     If requested size is larger than this value, buffer will be temporary instead of being retained for reuse.
    /// </summary>
    /// <value>
    ///     The maximum size to retain mb.
    /// </value>
    public static int MaximumSizeToRetainMb { get; set; } = 64;

    /// <summary>
    ///     Gets or sets the minimum size to retain the buffer in memory.
    ///     If requested size is smaller than this value, buffer will always be retained for reuse.
    /// </summary>
    /// <value>
    ///     The minimum size to retain mb.
    /// </value>
    public static int MinimumSizeToRetainMb { get; set; } = 4;

    /// <summary>
    ///     Gets or sets the minimum buffer release threshold by seconds.
    ///     Buffer will not be released automatically if it has been used within last N seconds.
    /// </summary>
    /// <value>
    ///     The minimum buffer release threshold by seconds.
    /// </value>
    public static int MinimumAutoReleaseThresholdSeconds { get; set; } = 60;

    /// <summary>
    ///     Gets or sets the size reduction multiplier.
    ///     If buffer is not being used more than <see cref="MinimumAutoReleaseThresholdSeconds" /> seconds,
    ///     and new request size is smaller than buffer size / <see cref="SizeReductionDividend" /> but larger than
    ///     <see cref="MinimumSizeToRetainMb" />,
    ///     buffer will be released after usage.
    /// </summary>
    /// <value>
    ///     The size reduction multiplier.
    /// </value>
    public static float SizeReductionDividend { get; set; } = 2;
}

public static class ThreadBufferManager<T> where T : unmanaged {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    public static readonly int StructSize = Marshal.SizeOf<T>();

    private const int MByteToByte = 1024 * 1024;

    public static int MaximumElementCount =>
        ThreadBufferManagerConfig.MaximumSizeToRetainMb * MByteToByte / StructSize;

    public static int MinimumElementCount =>
        ThreadBufferManagerConfig.MinimumSizeToRetainMb * MByteToByte / StructSize;

    [ThreadStatic] private static T[]? _buffer;

    private static long _lastUsed;

    public static T[] GetBuffer(int requestCount) {
        var array = _buffer;
        if (array == null || array.Length < requestCount) {
            float scale = 1;
            if (requestCount < MinimumElementCount)
                scale = 2;
            else if (requestCount < MaximumElementCount) scale = 1.5f;
            array = new T[(int)(requestCount * scale)];
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.Debug("Created new thread buffer. Type: {Value0}; Size: {Value1} kB",
                             typeof(T),
                             array.Length * StructSize / 1024);
        }

        if (requestCount > MaximumElementCount) {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.Debug("Requested buffer size is larger than max retain size. Type: {Value0}", typeof(T));
            return array;
        }

        if (_lastUsed == 0) {
            _lastUsed = Stopwatch.GetTimestamp();
            _buffer = array;
            return array;
        }

        if (array.Length > MinimumElementCount
            && array.Length > ThreadBufferManagerConfig.SizeReductionDividend * requestCount) {
            var diff = Stopwatch.GetTimestamp() - _lastUsed;
            if (diff / Stopwatch.Frequency > ThreadBufferManagerConfig.MinimumAutoReleaseThresholdSeconds) {
                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.Debug("Disposing thread buffer. Type: {Value0}", typeof(T));
                _buffer = null;
                _lastUsed = 0;
                return array;
            }
        }

        _buffer = array;
        _lastUsed = Stopwatch.GetTimestamp();
        return array;
    }
}