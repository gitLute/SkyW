namespace SkyW.Abstractions.Replication;

// Один писатель и один читатель. Читатель освобождает арену после применения
// батча; TryPublish возвращает false, когда нужен keyframe.
public interface IStateStream
{
    bool TryPublish(in StateBatchHeader header, ReadOnlySpan<byte> payload);
    bool TryRead(out StateBatchHeader header, out ReadOnlySpan<byte> payload);
    void Release();
}
