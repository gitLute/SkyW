namespace SkyW.Abstractions.Logging;

// Порядок объявления значим: фильтр журнала — это сравнение, а не набор масок.
public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
}