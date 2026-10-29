using SkyW.Abstractions.Logging;
using SkyW.Diagnostics.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

[Collection(ConsoleCollection.Name)]
public sealed class LogFormatterTests
{
    private static readonly DateTimeOffset Moment = new(2026, 10, 13, 9, 41, 5, 250, TimeSpan.Zero);

    [Fact]
    public void DefaultTimestampFormat_IsSortable() => Assert.Equal("yyyy-MM-dd HH:mm:ss.fff", LogFormatter.DefaultTimestampFormat);

    [Fact]
    public void Format_UsesInjectedClock_AndEngineLayout()
    {
        LogFormatter formatter = new(clock: () => Moment);
        string line = formatter.Format(LogLevel.Information, "сцена загружена");
        Assert.StartsWith("[2026-10-13 09:41:05.250] [", line, StringComparison.Ordinal);
        Assert.EndsWith(" / INF]: сцена загружена", line, StringComparison.Ordinal);
    }

    [Fact]
    public void Timestamp_HonoursCustomFormat()
    {
        LogFormatter formatter = new("HH:mm:ss", () => Moment);
        Assert.Equal("HH:mm:ss", formatter.TimestampFormat);
        Assert.Equal("09:41:05", formatter.Timestamp());
    }

    [Theory]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    public void LevelName_UsesThreeLetterAbbreviation(LogLevel level, string expected) => Assert.Equal(expected, new LogFormatter().LevelName(level));

    [Fact]
    public void LevelName_OfUnknownValue_IsMarked() => Assert.Equal("???", new LogFormatter().LevelName((LogLevel)42));

    [Fact]
    public void ThreadName_UsesThreadName_WhenItIsSet()
    {
        // Поток движка должен называться сам: иначе строки симуляции и рендера
        // в журнале не различить.
        LogFormatter formatter = new();
        string? captured = null;
        Thread thread = new(() => captured = formatter.ThreadName()) { Name = "Server" };
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "поток не завершился");
        Assert.Equal("Server", captured);
    }

    [Fact]
    public void ThreadName_FallsBackToManagedThreadId()
    {
        string? captured = null;
        Thread thread = new(() => captured = new LogFormatter().ThreadName());
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "поток не завершился");
        Assert.Matches(@"^#\d+$", captured!);
    }

    [Fact]
    public void EmptyTimestampFormat_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => _ = new LogFormatter(string.Empty));
        Assert.Throws<ArgumentNullException>(() => _ = new LogFormatter(null!));
    }

    [Fact]
    public void NullMessage_IsRejected()
    {
        LogFormatter formatter = new(clock: () => Moment);
        Assert.Throws<ArgumentNullException>(() => formatter.Format(LogLevel.Error, null!));
    }

    [Fact]
    public void EverySink_ProducesTheSameLine()
    {
        // Одно правило форматирования на все выводы: строки должны совпасть побайтово.
        using TempDirectory directory = new();
        LogFormatter formatter = new(clock: () => Moment);
        string path = directory.File("engine.log");
        using StringWriter output = new();
        TextWriter original = Console.Out;
        Console.SetOut(output);
        try
        {
            ConsoleLogSink console = new(LogLevel.Debug, formatter);
            using FileLogSink file = new(path, LogLevel.Debug, 0, 0, formatter);
            RingBufferLogSink ring = new(4, LogLevel.Debug, formatter);
            console.Write(LogLevel.Warning, "бюджет кадра исчерпан");
            file.Write(LogLevel.Warning, "бюджет кадра исчерпан");
            ring.Write(LogLevel.Warning, "бюджет кадра исчерпан");
            string fromConsole = output.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            string fromFile = File.ReadAllText(path).TrimEnd(Environment.NewLine.ToCharArray());
            string fromRing = Assert.Single(ring.Snapshot());
            Assert.Equal(fromConsole, fromFile);
            Assert.Equal(fromConsole, fromRing);
            Assert.StartsWith("[2026-10-13 09:41:05.250] [", fromConsole, StringComparison.Ordinal);
            Assert.EndsWith(" / WRN]: бюджет кадра исчерпан", fromConsole, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}