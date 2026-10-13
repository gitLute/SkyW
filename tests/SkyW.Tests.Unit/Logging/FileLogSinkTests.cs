using System.Text;
using SkyW.Abstractions.Logging;
using SkyW.Diagnostics.Logging;
using Xunit;

namespace SkyW.Tests.Unit.Logging;

/// <summary>
/// Файловый приёмник: журнал переживает перезапуск движка, поэтому файл
/// дописывается, не переживает переполнения и не остаётся незакрытым.
/// </summary>
public sealed class FileLogSinkTests
{
    [Fact]
    public void Write_CreatesMissingDirectoryAndFile()
    {
        using TempDirectory directory = new();
        string path = directory.File(Path.Combine("logs", "engine.log"));

        using (FileLogSink sink = new(path))
        {
            sink.Write(LogLevel.Information, "сцена загружена");
        }

        Assert.Contains("сцена загружена", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_AppendsToExistingFile()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using (FileLogSink sink = new(path))
        {
            sink.Write(LogLevel.Information, "первый запуск");
        }

        using (FileLogSink sink = new(path))
        {
            sink.Write(LogLevel.Information, "второй запуск");
        }

        string text = File.ReadAllText(path);
        Assert.Contains("первый запуск", text, StringComparison.Ordinal);
        Assert.Contains("второй запуск", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_UsesUtf8WithoutByteOrderMark()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using (FileLogSink sink = new(path))
        {
            sink.Write(LogLevel.Information, "сцена загружена");
        }

        byte[] bytes = File.ReadAllBytes(path);

        // Метка порядка байтов ломает разбор журнала сторонними инструментами.
        Assert.False(
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "журнал начался с метки порядка байтов");

        string text = new UTF8Encoding(false).GetString(bytes);
        Assert.Contains("сцена загружена", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ReachesDiskWithoutExplicitFlush()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using (FileLogSink sink = new(path))
        {
            sink.Write(LogLevel.Error, "движок упал");
        }

        // Строка ушла на диск сразу: буфера, теряющего последние записи при падении,
        // у приёмника нет.
        Assert.Contains("движок упал", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void FilePath_IsExposedForTheHost()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using FileLogSink sink = new(path);

        Assert.Equal(path, sink.FilePath);
    }

    [Fact]
    public void IsEnabled_FiltersBySinkThreshold()
    {
        using TempDirectory directory = new();

        using FileLogSink sink = new(directory.File("engine.log"), LogLevel.Warning);

        Assert.False(sink.IsEnabled(LogLevel.Debug));
        Assert.False(sink.IsEnabled(LogLevel.Information));
        Assert.True(sink.IsEnabled(LogLevel.Warning));
        Assert.True(sink.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void Write_BelowThreshold_IsIgnored()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using (FileLogSink sink = new(path, LogLevel.Warning))
        {
            sink.Write(LogLevel.Information, "не должно попасть");
            sink.Write(LogLevel.Error, "должно попасть");
        }

        string text = File.ReadAllText(path);
        Assert.DoesNotContain("не должно попасть", text, StringComparison.Ordinal);
        Assert.Contains("должно попасть", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Roll_KeepsRetainedFilesNextToTheLog()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using FileLogSink sink = new(path, LogLevel.Debug, maxFileSizeBytes: 400, retainedFiles: 2);

        for (int index = 0; index < 40; index++)
        {
            sink.Write(LogLevel.Information, $"строка {index:D2} движка пишется в файл и занимает место");
        }

        Assert.True(File.Exists(path), "текущий журнал должен существовать");
        Assert.True(File.Exists($"{path}.1"), "первый сторонний файл должен существовать");
        Assert.False(File.Exists($"{path}.3"), "третьего стороннего файла быть не должно");
        Assert.Contains("строка 39", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Roll_WithoutRetainedFiles_DropsTheOldOne()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using FileLogSink sink = new(path, LogLevel.Debug, maxFileSizeBytes: 400);

        for (int index = 0; index < 40; index++)
        {
            sink.Write(LogLevel.Information, $"строка {index:D2} без сохранения предыдущих файлов");
        }

        Assert.True(File.Exists(path), "текущий журнал должен существовать");
        Assert.False(File.Exists($"{path}.1"), "стороннего файла быть не должно");
        Assert.Contains("строка 39", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Roll_HappensBeforeTheLineThatDoesNotFit()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        using (FileLogSink sink = new(path, LogLevel.Debug, maxFileSizeBytes: 1))
        {
            sink.Write(LogLevel.Information, "первая строка");
            sink.Write(LogLevel.Information, "вторая строка");
        }

        // Предел в один байт: сворачивание идёт до записи, поэтому в свежем журнале
        // всегда есть хоть одна запись, а не пустой файл.
        string text = File.ReadAllText(path);
        Assert.Contains("вторая строка", text, StringComparison.Ordinal);
        Assert.DoesNotContain("первая строка", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_ClosesFile_AndIsIdempotent()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        FileLogSink sink = new(path);
        sink.Dispose();
        sink.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sink.Write(LogLevel.Error, "после освобождения"));

        // Flush после освобождения не бросает: закрывать уже нечего, а сбой при
        // освобождении не должен маскироваться (docs/engine-architecture.md раздел 8).
        sink.Flush();
    }

    [Fact]
    public void Constructor_RejectsBadArguments()
    {
        using TempDirectory directory = new();
        string path = directory.File("engine.log");

        Assert.Throws<ArgumentException>(() => _ = new FileLogSink(string.Empty));
        Assert.Throws<ArgumentException>(() => _ = new FileLogSink("   "));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new FileLogSink(path, maxFileSizeBytes: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new FileLogSink(path, retainedFiles: -1));
    }

    [Fact]
    public void Write_RejectsNullMessage()
    {
        using TempDirectory directory = new();

        using FileLogSink sink = new(directory.File("engine.log"));

        Assert.Throws<ArgumentNullException>(() => sink.Write(LogLevel.Error, null!));
    }
}