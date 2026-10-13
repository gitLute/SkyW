using SkyW.Abstractions.Logging;
using SkyW.Diagnostics.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

/// <summary>
/// Кольцевой приёмник: хранит последние строки в памяти, не растёт и не
/// выделяет память на каждом сообщении.
/// </summary>
public sealed class RingBufferLogSinkTests
{
    /// <summary>
    /// Текст сообщения без отметки времени и имени потока: имя потока в строке
    /// не предсказуемо, а проверить надо именно порядок сообщений.
    /// </summary>
    private static string MessageOf(string line)
    {
        int index = line.LastIndexOf("]: ", StringComparison.Ordinal);

        return line[(index + 3)..];
    }

    [Fact]
    public void EmptyBuffer_SnapshotIsEmpty()
    {
        RingBufferLogSink sink = new(capacity: 4);

        Assert.Equal(4, sink.Capacity);
        Assert.Equal(0, sink.Count);
        Assert.Equal(0, sink.AcceptedCount);
        Assert.Empty(sink.Snapshot());
        Assert.Equal(string.Empty, sink.SnapshotText());
    }

    [Fact]
    public void Write_StoresMessagesOldestFirst()
    {
        RingBufferLogSink sink = new(capacity: 4);

        sink.Write(LogLevel.Information, "первая");
        sink.Write(LogLevel.Warning, "вторая");

        Assert.Equal(["первая", "вторая"], sink.Snapshot().Select(MessageOf));
        Assert.Equal(2, sink.Count);
    }

    [Fact]
    public void Overwrite_KeepsTheNewestLines()
    {
        RingBufferLogSink sink = new(capacity: 3);

        for (int index = 0; index < 6; index++)
        {
            sink.Write(LogLevel.Information, $"строка {index}");
        }

        Assert.Equal(["строка 3", "строка 4", "строка 5"], sink.Snapshot().Select(MessageOf));
        Assert.Equal(3, sink.Count);
        Assert.Equal(6, sink.AcceptedCount);
    }

    [Fact]
    public void AcceptedCount_SurvivesOverwriteAndClear()
    {
        RingBufferLogSink sink = new(capacity: 2);

        for (int index = 0; index < 5; index++)
        {
            sink.Write(LogLevel.Information, $"строка {index}");
        }

        sink.Clear();

        // Счётчик принадлежит приёмнику, а не содержимому: по нему видно, сколько
        // сообщений прошло за запуск.
        Assert.Equal(5, sink.AcceptedCount);
        Assert.Equal(0, sink.Count);
        Assert.Empty(sink.Snapshot());
    }

    [Fact]
    public void SnapshotText_JoinsWithPlatformNewLine()
    {
        RingBufferLogSink sink = new(capacity: 4);

        sink.Write(LogLevel.Information, "первая");
        sink.Write(LogLevel.Error, "вторая");

        string[] parts = sink.SnapshotText().Split(Environment.NewLine);

        Assert.Equal(2, parts.Length);
        Assert.Equal(["первая", "вторая"], parts.Select(MessageOf));
    }

    [Fact]
    public void IsEnabled_FiltersBySinkThreshold()
    {
        RingBufferLogSink sink = new(capacity: 4, minimumLevel: LogLevel.Warning);

        Assert.False(sink.IsEnabled(LogLevel.Debug));
        Assert.False(sink.IsEnabled(LogLevel.Information));
        Assert.True(sink.IsEnabled(LogLevel.Warning));
        Assert.True(sink.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void Write_BelowThreshold_IsNotCounted()
    {
        RingBufferLogSink sink = new(capacity: 4, minimumLevel: LogLevel.Warning);

        sink.Write(LogLevel.Information, "не должно попасть");
        sink.Write(LogLevel.Error, "должно попасть");

        Assert.Equal(1, sink.Count);
        Assert.Equal(1, sink.AcceptedCount);
        Assert.Contains("должно попасть", Assert.Single(sink.Snapshot()), StringComparison.Ordinal);
    }

    [Fact]
    public void NonPositiveCapacity_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new RingBufferLogSink(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new RingBufferLogSink(capacity: -1));
    }

    [Fact]
    public void Write_RejectsNullMessage()
    {
        RingBufferLogSink sink = new(capacity: 4);

        Assert.Throws<ArgumentNullException>(() => sink.Write(LogLevel.Error, null!));
    }

    [Fact]
    public async Task WritesFromBothEngineThreads_KeepTheBufferConsistent()
    {
        // docs/answers.md: максимум два потока — симуляция и рендер.
        const int ThreadCount = 2;
        const int MessagesPerThread = 400;

        RingBufferLogSink sink = new(capacity: 64);

        Task[] writers =
        [
            .. Enumerable.Range(0, ThreadCount).Select(worker => Task.Run(() =>
            {
                for (int index = 0; index < MessagesPerThread; index++)
                {
                    sink.Write(LogLevel.Information, $"поток {worker}, сообщение {index}");
                }
            })),
        ];

        await Task.WhenAll(writers);

        Assert.Equal(64, sink.Count);
        Assert.Equal(ThreadCount * MessagesPerThread, sink.AcceptedCount);
        Assert.Equal(64, sink.Snapshot().Count);
        Assert.Equal(64, sink.Snapshot().Distinct(StringComparer.Ordinal).Count());
    }
}