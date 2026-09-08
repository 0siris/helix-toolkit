using System.Globalization;
using ValidSphere;
using HelixToolkit.SharpDX.Core.Logger;
using HelixToolkit.SharpDX.Core.Utilities;
using LoggerLib;
using Microsoft.Extensions.Logging;

namespace SilkCore.Tests;

public sealed class LoggerAndAssertionIntegrationTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void LogManagerCreate_UsesLoggerLibAndPreservesTemplateArguments() {
        var originalOutput = Console.Out;
        var previousFactory = LogManager.Factory;
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var logger = new ConsoleLogger(queueCapacity: 16, maxEnqueueWaitMs: 50);

        try {
            Console.SetOut(output);
            Logger.Use(logger);
            LogManager.Factory = new LoggerLibLoggerFactory();

            var state = new[] {
                new KeyValuePair<string, object?>("Path", "model.obj"),
                new KeyValuePair<string, object?>("{OriginalFormat}", "Loading {Path}")
            };
            var loggerFromFactory = LogManager.Create<LoggerAndAssertionIntegrationTests>();

            loggerFromFactory.Log(
                LogLevel.Information,
                default,
                state,
                null,
                (_, _) => "formatted fallback");
            logger.Dispose();

        } finally {
            LogManager.Factory = previousFactory;
            Logger.Use(NullLogger.Instance);
            Console.SetOut(originalOutput);
        }

        var rendered = output.ToString();
        Assert.Contains("Loading model.obj", rendered);
        Assert.DoesNotContain("formatted fallback", rendered);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Assertions_PreserveArgumentAndStateContracts() {
        const string valid = "valid";
        int[] values = [1, 2];

        Assert.Equal(valid, valid.AsGuardNotNull());
        Assert.Equal(valid, valid.AsNotNull());
        Assert.Same(values, values.Is().Satisfy(value => value.Length >= 2).Value);
        Assert.Throws<ArgumentNullException>(() => ((string?)null).AsGuardNotNull());
        Assert.Throws<AssertException>(() => { _ = ((string?)null).AsNotNull(); });
        Assert.Throws<AssertException>(() => values.Is().Satisfy(value => value.Length >= 3));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TextureModelRepository_PreservesOptionalInputsAndReusesStreams() {
        var repository = new TextureModelRepository();
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Null(repository.Create((Stream?)null));
        Assert.Null(repository.Create((string?)null));
        Assert.Null(repository.Create(string.Empty));

        var texture = repository.Create(stream);
        Assert.NotNull(texture);
        Assert.Same(texture, repository.Create(stream));
    }
}
