using System.Diagnostics;
using System.IO;
using DemoCore.Automation;
using LoggerLib;
using Serilog.Events;
using Xunit;

namespace SilkToolkit.Tests;

[Collection(WpfCollection.Name)]
public sealed class DemoAutomationV2Tests {
    /// <summary>
    ///     Verifies steady 60Hz ticks converge to 60 FPS.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void FrameRateMeterConvergesToTickRate() {
        var meter = new FrameRateMeter();
        var interval = (long)(Stopwatch.Frequency * 0.0166667);
        var timestamp = Stopwatch.GetTimestamp();
        for (var i = 0; i < 60; i++) {
            timestamp += interval;
            meter.Tick(timestamp);
        }

        Assert.InRange(meter.Fps, 55, 65);
        Assert.InRange(meter.FrameMs, 15, 18);
    }

    /// <summary>
    ///     Verifies a single tick anchors the meter without producing a rate.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void FrameRateMeterIgnoresFirstTick() {
        var meter = new FrameRateMeter();
        meter.Tick(Stopwatch.GetTimestamp());

        Assert.Equal(0, meter.Fps);
    }

    /// <summary>
    ///     Verifies the ring buffer forwards every level to the inner logger.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void RingBufferLoggerForwardsToInner() {
        var inner = new CountingLogger();
        var logger = new RingBufferLogger(inner, capacity: 500);

        logger.Verbose("v");
        logger.Debug("d");
        logger.Info("i");
        logger.Warn("w");
        logger.Error("e");

        Assert.Equal(5, inner.Calls);
        Assert.Equal(5, logger.Snapshot().Length);
    }

    /// <summary>
    ///     Verifies the ring buffer evicts the oldest entries beyond capacity.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void RingBufferLoggerEvictsOldest() {
        var logger = new RingBufferLogger(new CountingLogger(), capacity: 500);
        for (var i = 0; i < 600; i++) {
            logger.Info($"entry {i}");
        }

        var snapshot = logger.Snapshot();
        Assert.Equal(500, snapshot.Length);
        Assert.Contains("entry 100", snapshot[0].Message);
        Assert.Contains("entry 599", snapshot[^1].Message);
    }

    /// <summary>
    ///     Verifies template rendering substitutes placeholders in order.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void RingBufferLoggerRendersTemplates() {
        Assert.Equal(
            "Loading model.obj failed",
            RingBufferLogger.RenderTemplate("Loading {Path} {Outcome}", ["model.obj", "failed"]));
    }

    /// <summary>
    ///     Verifies log filtering by level, newest first, capped to limit.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void LogQueryFiltersByLevel() {
        var now = DateTimeOffset.UtcNow;
        LogEntry[] entries = [
            new(now, "debug", "d", null),
            new(now, "info", "i", null),
            new(now, "warn", "w", null),
        ];

        var filtered = LogQuery.Apply(entries, "info", 10);
        Assert.Equal(2, filtered.Count);
        Assert.Equal("w", filtered[0].Message);
        Assert.Equal("i", filtered[1].Message);
        Assert.Throws<ArgumentException>(() => LogQuery.Apply(entries, "nope", 10));
    }

    /// <summary>
    ///     Verifies discovery lists live demos and prunes dead URL files.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void DemoDiscoveryListsLiveAndPrunesDead() {
        var directory = Path.Combine(Path.GetTempPath(), $"helix-demo-test-{Environment.ProcessId}");
        Directory.CreateDirectory(directory);
        try {
            var deadPid = 999999;
            while (IsAlive(deadPid)) {
                deadPid++;
            }

            File.WriteAllText(Path.Combine(directory, $"{Environment.ProcessId}.url"), "http://127.0.0.1:1");
            File.WriteAllText(Path.Combine(directory, $"{deadPid}.url"), "http://127.0.0.1:2");

            var found = DemoDiscovery.ListRunning(directory);
            Assert.Single(found);
            Assert.Equal(Environment.ProcessId, found[0].Pid);
            Assert.False(File.Exists(Path.Combine(directory, $"{deadPid}.url")));
        } finally {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    ///     Verifies the missing-bound error carries the stable mapping token.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void MissingBoundCarriesStableToken() {
        Assert.Contains("no-bound", new NoNodeBoundException(Guid.NewGuid()).Message);
    }

    /// <summary>
    ///     Checks whether a process id is still running.
    /// </summary>
    /// <param name="pid">The process id.</param>
    /// <returns>True when the process exists.</returns>
    private static bool IsAlive(int pid) {
        try {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        } catch {
            return false;
        }
    }

    /// <summary>
    ///     Counting inner logger proving the decorator forwards calls.
    /// </summary>
    private sealed class CountingLogger : ILog {
        /// <summary>
        ///     Gets the forwarded call count.
        /// </summary>
        public int Calls { get; private set; }

        /// <summary>
        ///     Gets or sets the minimum level.
        /// </summary>
        public LogEventLevel MinimumLevel { get; set; }

        /// <inheritdoc />
        public void Trace(string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void VerboseTrace(string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose(string message, object[]? propertyValues = null, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1>(string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1, T2>(string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1, T2, T3>(string message, T1 p1, T2 p2, T3 p3, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose(Exception exception, string message, object[]? propertyValues = null,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1>(Exception exception, string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1, T2>(Exception exception, string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Verbose<T1, T2, T3>(Exception exception, string message, T1 p1, T2 p2, T3 p3,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug(string message, object[]? propertyValues = null, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1>(string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1, T2>(string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1, T2, T3>(string message, T1 p1, T2 p2, T3 p3, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug(Exception exception, string message, object[]? propertyValues = null,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1>(Exception exception, string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1, T2>(Exception exception, string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Debug<T1, T2, T3>(Exception exception, string message, T1 p1, T2 p2, T3 p3,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info(string message, object[]? propertyValues = null, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1>(string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1, T2>(string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1, T2, T3>(string message, T1 p1, T2 p2, T3 p3, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info(Exception exception, string message, object[]? propertyValues = null,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1>(Exception exception, string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1, T2>(Exception exception, string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Info<T1, T2, T3>(Exception exception, string message, T1 p1, T2 p2, T3 p3,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn(string message, object[]? propertyValues = null, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1>(string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1, T2>(string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1, T2, T3>(string message, T1 p1, T2 p2, T3 p3, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn(Exception exception, string message, object[]? propertyValues = null,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1>(Exception exception, string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1, T2>(Exception exception, string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Warn<T1, T2, T3>(Exception exception, string message, T1 p1, T2 p2, T3 p3,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error(string message, object[]? propertyValues = null, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1>(string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1, T2>(string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1, T2, T3>(string message, T1 p1, T2 p2, T3 p3, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error(Exception exception, string message, object[]? propertyValues = null,
            string path = "?", int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1>(Exception exception, string message, T1 p1, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1, T2>(Exception exception, string message, T1 p1, T2 p2, string path = "?",
            int line = -1, string method = "?") => Calls++;

        /// <inheritdoc />
        public void Error<T1, T2, T3>(Exception exception, string message, T1 p1, T2 p2, T3 p3,
            string path = "?", int line = -1, string method = "?") => Calls++;
    }
}
