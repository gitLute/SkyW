using SkyW.Abstractions.Logging;
using SkyW.Diagnostics.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

[Collection(ConsoleCollection.Name)]
public sealed class LogSinkTests
{
    [Fact]
    public void Null_SwallowsEverythingWithoutThrowing()
    {
        NullLogSink sink = new();
        Assert.False(sink.IsEnabled(LogLevel.Debug));
        Assert.False(sink.IsEnabled(LogLevel.Information));
        Assert.False(sink.IsEnabled(LogLevel.Warning));
        Assert.False(sink.IsEnabled(LogLevel.Error));
        sink.Write(LogLevel.Error, "сообщение не должно никуда попасть");
    }

    [Fact]
    public void Console_DefaultThresholdIsInformation()
    {
        ConsoleLogSink sink = new();
        Assert.False(sink.IsEnabled(LogLevel.Debug));
        Assert.True(sink.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void Console_WritesAcceptedMessages()
    {
        using StringWriter output = new();
        TextWriter original = Console.Out;
        Console.SetOut(output);
        try
        {
            ConsoleLogSink sink = new();
            sink.Write(LogLevel.Information, "окно создано");
            Assert.Contains("окно создано", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("INF", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void Console_FiltersByMinimumLevel()
    {
        using StringWriter output = new();
        TextWriter original = Console.Out;
        Console.SetOut(output);
        try
        {
            ConsoleLogSink sink = new(LogLevel.Warning);
            Assert.False(sink.IsEnabled(LogLevel.Debug));
            Assert.False(sink.IsEnabled(LogLevel.Information));
            Assert.True(sink.IsEnabled(LogLevel.Warning));
            Assert.True(sink.IsEnabled(LogLevel.Error));
            sink.Write(LogLevel.Information, "не должно попасть");
            sink.Write(LogLevel.Error, "должно попасть");
            string text = output.ToString();
            Assert.DoesNotContain("не должно попасть", text, StringComparison.Ordinal);
            Assert.Contains("должно попасть", text, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void Console_RejectsNullMessage()
    {
        ConsoleLogSink sink = new();
        Assert.Throws<ArgumentNullException>(() => sink.Write(LogLevel.Error, null!));
    }
}