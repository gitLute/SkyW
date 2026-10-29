namespace SkyW.Engine;

// Результат кадра: сколько шагов успело выполниться и что видит вывод.
public readonly record struct FrameResult(int Steps, long Tick, float Alpha, int BatchBytes, bool Running);
