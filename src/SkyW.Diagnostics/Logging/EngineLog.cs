using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

// Инстанцируемый: у движка нет общего журнала, нужный создаётся на старте и
// освобождается с владельцем. Порог проверяется до сборки текста сообщения.
public sealed class EngineLog : ILog, IDisposable
{
    private readonly ILogSink[] _sinks;
    private readonly bool[] _disabledSinks;
    private readonly Lock _writeLock = new();

    private LogLevel _minimumLevel;

    // Приёмники из фабрик принадлежат журналу, переданные в конструктор — нет.
    private bool _ownsSinks;
    private bool _disposed;

    // Повторы одного экземпляра отбрасываются: иначе общий приёмник попал бы в
    // список дважды и вывел каждое сообщение дважды.
    public EngineLog(params ILogSink[] sinks)
        : this(LogLevel.Information, sinks)
    {
    }

    // Порог журнала — верхняя граница; приёмник может фильтровать строже своим порогом.
    // Приёмники остаются во владении вызывающего: они могут быть общими у журналов.
    public EngineLog(LogLevel minimumLevel, params ILogSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        _minimumLevel = minimumLevel;
        _sinks = RemoveDuplicates(sinks);
        _disabledSinks = new bool[_sinks.Length];
    }

    // Менять до запуска потоков движка: гонки нет, но порядок «поднять порог, потом
    // запустить симуляцию» предпочтительнее.
    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    public IReadOnlyList<ILogSink> Sinks => _sinks;

    public bool IsEnabled(LogLevel level)
    {
        if (level < _minimumLevel)
        {
            return false;
        }

        for (int index = 0; index < _sinks.Length; index++)
        {
            if (!_disabledSinks[index] && _sinks[index].IsEnabled(level))
            {
                return true;
            }
        }

        return false;
    }

    public void Log(LogLevel level, string message) => Write(level, message, exception: null);

    public void Log(LogLevel level, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write(level, message, exception);
    }

    public void Debug(string message) => Log(LogLevel.Debug, message);

    public void Debug(string message, Exception exception) =>
        Log(LogLevel.Debug, message, exception);

    public void Information(string message) => Log(LogLevel.Information, message);

    public void Information(string message, Exception exception) =>
        Log(LogLevel.Information, message, exception);

    public void Warning(string message) => Log(LogLevel.Warning, message);

    public void Warning(string message, Exception exception) =>
        Log(LogLevel.Warning, message, exception);

    public void Error(string message) => Log(LogLevel.Error, message);

    public void Error(string message, Exception exception) =>
        Log(LogLevel.Error, message, exception);

    // Упавший приёмник всё равно освобождается: иначе остался бы незакрытый файл.
    public void Dispose()
    {
        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!_ownsSinks)
            {
                return;
            }

            foreach (ILogSink sink in _sinks)
            {
                if (sink is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    public static EngineLog CreateConsole(LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new ConsoleLogSink(minimumLevel)) { _ownsSinks = true };

    public static EngineLog CreateFile(
        string path,
        LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new FileLogSink(path, minimumLevel)) { _ownsSinks = true };

    public static EngineLog CreateConsoleAndFile(
        string path,
        LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new ConsoleLogSink(minimumLevel), new FileLogSink(path, minimumLevel))
        {
            _ownsSinks = true,
        };

    public static EngineLog CreateRingBuffer(
        int capacity = RingBufferLogSink.DefaultCapacity,
        LogLevel minimumLevel = LogLevel.Information)
    {
        RingBufferLogSink sink = new(capacity, minimumLevel);

        return new EngineLog(minimumLevel, sink) { _ownsSinks = true };
    }

    private static ILogSink[] RemoveDuplicates(ILogSink[] sinks)
    {
        List<ILogSink> unique = new(sinks.Length);

        foreach (ILogSink sink in sinks)
        {
            ArgumentNullException.ThrowIfNull(sink, nameof(sinks));

            if (!unique.Contains(sink))
            {
                unique.Add(sink);
            }
        }

        return unique.ToArray();
    }

    // Освобождение здесь не учитывается: проверка отвечает за уровень, а не за срок
    // жизни. После освобождения запись бросает ObjectDisposedException.
    private void Write(LogLevel level, string message, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsEnabled(level))
        {
            return;
        }

        string text = exception is null ? message : $"{message}{Environment.NewLine}{exception}";

        lock (_writeLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            for (int index = 0; index < _sinks.Length; index++)
            {
                if (_disabledSinks[index])
                {
                    continue;
                }

                try
                {
                    _sinks[index].Write(level, text);
                }
                // Приёмник — чужой код и вправе бросить что угодно: ловится любое
                // исключение намеренно, иначе сбой вывода роняет кадр.
                catch (Exception)
                {
                    // Отключаем приёмник, чтобы следующее сообщение не платило за то же
                    // исключение; остальные приёмники продолжают работать.
                    _disabledSinks[index] = true;
                }
            }
        }
    }
}