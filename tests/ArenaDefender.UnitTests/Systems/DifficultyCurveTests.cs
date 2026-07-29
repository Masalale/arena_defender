using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.UnitTests.Systems;

public class DifficultyCurveTests
{
    private const float Tolerance = 1e-3f;

    private static GameSettings Settings => new()
    {
        EnemiesInFirstWave = 8,
        EnemiesAddedPerWave = 2,
        InitialSpawnInterval = 2f,
        MinimumSpawnInterval = 0.5f,
        WavesToFastestSpawn = 5,
        MaxSpeedScale = 1.2f,
        WavesToMaxSpeed = 10,
        MaxDamageScale = 2f,
        WavesToMaxDamage = 20
    };

    private static DifficultyCurve Curve => new(Settings);

    [Theory]
    [InlineData(1, 8)]
    [InlineData(2, 10)]
    [InlineData(3, 12)]
    [InlineData(10, 26)]
    public void GetEnemyCount_GrowsByAFixedStepEachWave(int wave, int expected)
    {
        Assert.Equal(expected, Curve.GetEnemyCount(wave));
    }

    [Fact]
    public void GetSpawnInterval_KeepsShrinkingPastTheOpeningRamp()
    {
        GameSettings settings = Settings;
        DifficultyCurve curve = Curve;

        float atRampEnd = curve.GetSpawnInterval(settings.WavesToFastestSpawn);

        // The endless game needs one dial that never settles, and this is it.
        Assert.True(curve.GetSpawnInterval(settings.WavesToFastestSpawn + 5) < atRampEnd);
        Assert.True(
            curve.GetSpawnInterval(settings.WavesToFastestSpawn + 20)
            < curve.GetSpawnInterval(settings.WavesToFastestSpawn + 5));
    }

    [Fact]
    public void GetDamageScale_StartsNeutralAndRisesToItsCeiling()
    {
        GameSettings settings = Settings;
        DifficultyCurve curve = Curve;

        Assert.Equal(1f, curve.GetDamageScale(1), Tolerance);
        Assert.Equal(settings.MaxDamageScale, curve.GetDamageScale(settings.WavesToMaxDamage), Tolerance);
        Assert.Equal(settings.MaxDamageScale, curve.GetDamageScale(400), Tolerance);
    }
}
