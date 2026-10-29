using SkyW.Abstractions.Scheduling;
using SkyW.Engine;
using Xunit;

namespace SkyW.Tests.Unit.Engine;

public sealed class EngineOptionsTests
{
    [Fact]
    public void Defaults_FollowOwnerDecisions()
    {
        // docs/answers.md: 60 Hz, порог долга 2 с, две полосы по умолчанию.
        EngineOptions options = new();
        Assert.Equal(60, options.SimulationRateHz);
        Assert.Equal(2, options.DebtLimitSeconds);
        Assert.Equal(ThreadingMode.Multi, options.Threading);
        Assert.True(options.MaxStepsPerFrame > 0);
        Assert.True(options.KeyframeBytes > options.BatchBytes);
    }

    [Fact]
    public void FrameResult_CarriesStepCountAndAlpha()
    {
        FrameResult result = new(Steps: 2, Tick: 120, Alpha: 0.5f, BatchBytes: 128, Running: true);
        Assert.Equal(2, result.Steps);
        Assert.Equal(120, result.Tick);
        Assert.Equal(0.5f, result.Alpha);
        Assert.True(result.Running);
    }
}
