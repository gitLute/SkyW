namespace SkyW.Abstractions.Scheduling;

// Однопоточный режим исполняет тот же цикл без второго потока и синхронизации.
public enum ThreadingMode
{
    Multi,
    Single,
}
