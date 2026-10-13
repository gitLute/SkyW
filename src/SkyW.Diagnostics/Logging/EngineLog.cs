using SkyW.Abstractions.Logging;

namespace SkyW.Diagnostics.Logging;

/// <summary>
/// Журнал движка: раскладывает сообщения по приёмникам.
/// </summary>
/// <remarks>
/// Инстанциируемый по требованию подсистемы: у движка нет общего журнала, нужный
/// создаётся на старте, передаётся дальше и освобождается вместе с владельцем.
/// </remarks>
/// <remarks>
/// Порог уровня проверяется до сборки текста: чтобы не платить за строку, которая
/// всё равно отбросится, дорогой вызов оборачивают проверкой <see cref="IsEnabled"/>.
/// </remarks>
public sealed class EngineLog : ILog, IDisposable
{
    private readonly ILogSink[] _sinks;
    private readonly bool[] _disabledSinks;
    private readonly Lock _writeLock = new();

    private LogLevel _minimumLevel;
    private bool _ownsSinks;
    private bool _disposed;

    /// <summary>
    /// Создаёт журнал с порогом <see cref="LogLevel.Information"/>.
    /// </summary>
    /// <param name="sinks">
    /// Повторы одного экземпляра отбрасываются: иначе общий приёмник попал бы в
    /// список дважды и вывел каждое сообщение дважды.
    /// </param>
    public EngineLog(params ILogSink[] sinks)
        : this(LogLevel.Information, sinks)
    {
    }

    /// <summary>
    /// Создаёт журнал с заданным порогом уровня.
    /// </summary>
    /// <param name="minimumLevel">
    /// Порог журнала — верхняя граница; приёмник может фильтровать строже своим
    /// порогом.
    /// </param>
    /// <param name="sinks">
    /// Остаются во владении вызывающего: приёмник может быть общим для нескольких
    /// журналов. Освобождаются только те, что созданы фабриками.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Список приёмников не задан либо в нём есть пустой элемент.
    /// </exception>
    public EngineLog(LogLevel minimumLevel, params ILogSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        _minimumLevel = minimumLevel;
        _sinks = RemoveDuplicates(sinks);
        _disabledSinks = new bool[_sinks.Length];
    }

    /// <summary>
    /// Порог уровня журнала: сообщения ниже него не собираются и не пишутся.
    /// </summary>
    /// <remarks>
    /// Менять до запуска потоков движка: поле читается и пишется отдельно, гонки
    /// нет, но порядок «поднять порог, потом запустить симуляцию» предпочтительнее.
    /// </remarks>
    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    /// <summary>Приёмники журнала в порядке вызова.</summary>
    public IReadOnlyList<ILogSink> Sinks => _sinks;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Текст сообщения не задан.</exception>
    /// <exception cref="ObjectDisposedException">Журнал уже освобождён.</exception>
    public void Log(LogLevel level, string message) => Write(level, message, exception: null);

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Текст сообщения или исключение не заданы.</exception>
    /// <exception cref="ObjectDisposedException">Журнал уже освобождён.</exception>
    public void Log(LogLevel level, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write(level, message, exception);
    }

    /// <inheritdoc/>
    public void Debug(string message) => Log(LogLevel.Debug, message);

    /// <inheritdoc/>
    public void Debug(string message, Exception exception) =>
        Log(LogLevel.Debug, message, exception);

    /// <inheritdoc/>
    public void Information(string message) => Log(LogLevel.Information, message);

    /// <inheritdoc/>
    public void Information(string message, Exception exception) =>
        Log(LogLevel.Information, message, exception);

    /// <inheritdoc/>
    public void Warning(string message) => Log(LogLevel.Warning, message);

    /// <inheritdoc/>
    public void Warning(string message, Exception exception) =>
        Log(LogLevel.Warning, message, exception);

    /// <inheritdoc/>
    public void Error(string message) => Log(LogLevel.Error, message);

    /// <inheritdoc/>
    public void Error(string message, Exception exception) =>
        Log(LogLevel.Error, message, exception);

    /// <summary>Освобождает приёмники, созданные фабриками этого класса.</summary>
    /// <remarks>
    /// Переданные в конструктор не трогаются: они могут быть общими. Упавший
    /// приёмник всё равно освобождается — иначе остался бы незакрытый файл.
    /// </remarks>
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

    /// <summary>Создаёт журнал только для консоли.</summary>
    /// <returns>Журнал, которому принадлежит приёмник.</returns>
    public static EngineLog CreateConsole(LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new ConsoleLogSink(minimumLevel)) { _ownsSinks = true };

    /// <summary>Создаёт журнал только для файла.</summary>
    /// <returns>Журнал, которому принадлежит приёмник.</returns>
    public static EngineLog CreateFile(
        string path,
        LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new FileLogSink(path, minimumLevel)) { _ownsSinks = true };

    /// <summary>Создаёт журнал для консоли и файла одновременно.</summary>
    /// <returns>Журнал, которому принадлежат оба приёмника.</returns>
    public static EngineLog CreateConsoleAndFile(
        string path,
        LogLevel minimumLevel = LogLevel.Information) =>
        new(minimumLevel, new ConsoleLogSink(minimumLevel), new FileLogSink(path, minimumLevel))
        {
            _ownsSinks = true,
        };

    /// <summary>Создаёт журнал с кольцевым буфером в памяти, без файла и консоли.</summary>
    /// <returns>Журнал, из приёмника которого читается снимок последних строк.</returns>
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

    /// <remarks>
    /// Освобождение не учитывается: проверка отвечает за уровень, а не за срок
    /// жизни. После освобождения запись бросает <see cref="ObjectDisposedException"/>.
    /// </remarks>
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
                    // Приёмник отключается, чтобы каждое следующее сообщение не
                    // платило за то же исключение.
                    _disabledSinks[index] = true;
                }
            }
        }
    }
}