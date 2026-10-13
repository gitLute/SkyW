using SkyW.Abstractions.Logging;
using SkyW.Diagnostics.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

[Collection(ConsoleCollection.Name)]
public sealed class EngineLogTests
{
    [Fact]
    public void NullSinkList_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new EngineLog((ILogSink[]?)null!));
    }

    [Fact]
    public void NullSinkInsideList_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new EngineLog(LogLevel.Debug, new RecordingLogSink(), null!));
    }

    [Fact]
    public void DefaultThreshold_IsInformation()
    {
        using EngineLog log = new(new RecordingLogSink());

        Assert.Equal(LogLevel.Information, log.MinimumLevel);
        Assert.False(log.IsEnabled(LogLevel.Debug));
        Assert.True(log.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void DuplicateSink_IsRegisteredOnce()
    {
        RecordingLogSink sink = new();

        using EngineLog log = new(LogLevel.Debug, sink, sink);

        log.Information("сообщение один раз");

        Assert.Single(log.Sinks);
        Assert.Single(sink.Entries);
    }

    [Fact]
    public void EveryAcceptedSink_ReceivesMessageInOrder()
    {
        RecordingLogSink first = new();
        RecordingLogSink second = new();
        using EngineLog log = new(LogLevel.Debug, first, second);

        log.Information("сцена загружена");
        log.Warning("бюджет кадра исчерпан");

        Assert.Equal(
            ["сцена загружена", "бюджет кадра исчерпан"],
            first.Entries.Select(entry => entry.Message));
        Assert.Equal(
            ["сцена загружена", "бюджет кадра исчерпан"],
            second.Entries.Select(entry => entry.Message));
        Assert.Equal([LogLevel.Information, LogLevel.Warning], first.Entries.Select(entry => entry.Level));
    }

    [Fact]
    public void LoggerThreshold_IsUpperBound_AndSinkThresholdIsItsOwn()
    {
        // Журнал пропускает всё ниже Warning, приёмник — всё ниже Error.
        RecordingLogSink sink = new() { MinimumLevel = LogLevel.Error };
        using EngineLog log = new(LogLevel.Warning, sink);

        Assert.False(log.IsEnabled(LogLevel.Information));
        Assert.False(log.IsEnabled(LogLevel.Warning));
        Assert.True(log.IsEnabled(LogLevel.Error));

        log.Warning("журнал пропустит, приёмник отбросит");
        log.Error("журнал и приёмник пропустят");

        LogEntry entry = Assert.Single(sink.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public void DisabledMessage_NeverReachesSink()
    {
        RecordingLogSink sink = new();
        using EngineLog log = new(LogLevel.Warning, sink);

        log.Debug("дорогая строка, которую не должны даже собирать");

        Assert.False(log.IsEnabled(LogLevel.Debug));
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void Log_WithException_AppendsStackUnderMessage()
    {
        RecordingLogSink sink = new();
        using EngineLog log = new(LogLevel.Debug, sink);

        log.Error("сцена не загрузилась", new InvalidOperationException("не найден префаб игрока"));

        LogEntry entry = Assert.Single(sink.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.StartsWith("сцена не загрузилась", entry.Message, StringComparison.Ordinal);
        Assert.Contains("не найден префаб игрока", entry.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_WithoutException_DoesNotAppendAnything()
    {
        RecordingLogSink sink = new();
        using EngineLog log = new(LogLevel.Debug, sink);

        log.Log(LogLevel.Information, "ровно то, что написано");

        Assert.Equal("ровно то, что написано", Assert.Single(sink.Entries).Message);
    }

    [Fact]
    public void NullMessage_IsRejected()
    {
        using EngineLog log = new(LogLevel.Debug, new RecordingLogSink());

        Assert.Throws<ArgumentNullException>(() => log.Error(null!));
    }

    [Fact]
    public void FailingSink_IsDisabled_AndTheFrameContinues()
    {
        ThrowingLogSink broken = new();
        RecordingLogSink working = new();
        using EngineLog log = new(LogLevel.Debug, broken, working);

        log.Information("первое");
        log.Information("второе");

        // Второе сообщение не платит за то же исключение: приёмник отключён.
        Assert.Equal(1, broken.WriteAttempts);
        Assert.Equal(2, working.Entries.Count);
        Assert.True(log.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void FailingSoleSink_MakesEveryLevelDisabled()
    {
        ThrowingLogSink broken = new();
        using EngineLog log = new(LogLevel.Debug, broken);

        Assert.True(log.IsEnabled(LogLevel.Information));

        log.Information("приёмник падает");

        Assert.False(log.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void Dispose_KeepsConstructorSinks_Alive()
    {
        TrackedDisposableSink sink = new();
        EngineLog log = new(LogLevel.Debug, sink);

        log.Dispose();

        // Передан в конструктор, значит остаётся во владении вызывающего.
        Assert.False(sink.IsDisposed);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        EngineLog log = new(LogLevel.Debug, new RecordingLogSink());

        log.Dispose();
        log.Dispose();
    }

    [Fact]
    public void Log_AfterDispose_Throws()
    {
        using TempDirectory directory = new();
        EngineLog log = EngineLog.CreateFile(directory.File("engine.log"));

        log.Dispose();

        Assert.Throws<ObjectDisposedException>(() => log.Error("после освобождения"));
    }

    [Fact]
    public void FactoryOwnedSink_IsDisposedWithLog()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");
        EngineLog log = EngineLog.CreateFile(path);

        log.Information("сцена загружена");
        FileLogSink sink = Assert.IsType<FileLogSink>(Assert.Single(log.Sinks));

        log.Dispose();

        // Файл закрыт освобождением журнала: приёмник принадлежал журналу.
        Assert.Throws<ObjectDisposedException>(() => sink.Write(LogLevel.Information, "после освобождения"));
        Assert.Contains("сцена загружена", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Factories_BuildTheExpectedSinkSet()
    {
        using TempDirectory directory = new();
        using EngineLog console = EngineLog.CreateConsole();
        using EngineLog file = EngineLog.CreateFile(directory.File("file.log"));
        using EngineLog both = EngineLog.CreateConsoleAndFile(directory.File("both.log"));
        using EngineLog ring = EngineLog.CreateRingBuffer(capacity: 8);

        Assert.Equal(LogLevel.Information, console.MinimumLevel);
        Assert.IsType<ConsoleLogSink>(Assert.Single(console.Sinks));
        Assert.IsType<FileLogSink>(Assert.Single(file.Sinks));
        Assert.Equal(2, both.Sinks.Count);
        Assert.IsType<RingBufferLogSink>(Assert.Single(ring.Sinks));
        Assert.Equal(8, ((RingBufferLogSink)ring.Sinks[0]).Capacity);
    }

    [Fact]
    public async Task MessagesFromBothEngineThreads_AreAllDelivered()
    {
        // docs/answers.md: максимум два потока, симуляция и рендер.
        const int ThreadCount = 2;
        const int MessagesPerThread = 500;

        RecordingLogSink sink = new();
        using EngineLog log = new(LogLevel.Debug, sink);

        Task[] writers =
        [
            .. Enumerable.Range(0, ThreadCount).Select(worker => Task.Run(() =>
            {
                for (int index = 0; index < MessagesPerThread; index++)
                {
                    log.Information($"поток {worker}, сообщение {index}");
                }
            })),
        ];

        await Task.WhenAll(writers);

        Assert.Equal(ThreadCount * MessagesPerThread, sink.Entries.Count);
    }
}