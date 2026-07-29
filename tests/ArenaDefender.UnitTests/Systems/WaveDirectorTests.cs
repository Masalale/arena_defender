using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Simulation;
using ArenaDefender.Core.Systems;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Systems;

    /// <summary>
    /// The wave director: how many enemies each wave sends, when one gives way to the next,
    /// and how the enemies it builds are scaled.
    /// </summary>
public class WaveDirectorTests
{
    private const float Frame = 1f / 60f;

    private static GameSettings Settings(int firstWave = 3, int addedPerWave = 2) => new()
    {
        EnemiesInFirstWave = firstWave,
        EnemiesAddedPerWave = addedPerWave,
        InitialSpawnInterval = 0.5f,
        MinimumSpawnInterval = 0.25f,
        WavesToFastestSpawn = 4,
        WaveBreatherSeconds = 1f
    };

    private static WaveDirector Create(GameSettings settings, IRandomSource random) =>
        new(settings, new DifficultyCurve(settings), random, new ArenaBounds(settings.ArenaWidth, settings.ArenaHeight));

    /// <summary>Drains a wave, keeping aliveCount non-zero so it cannot roll over mid drain.</summary>
    private static int DrainWave(WaveDirector director, int aliveCount)
    {
        int spawned = 0;

        for (int frame = 0; frame < 5000 && director.RemainingToSpawn > 0; frame++)
        {
            if (director.Update(Frame, aliveCount) is not null)
            {
                spawned++;
            }
        }

        return spawned;
    }

    [Fact]
    public void Update_SendsExactlyTheWavesCountAndThenStops()
    {
        WaveDirector director = Create(Settings(firstWave: 3), new FixedRandom(0.9f));

        int spawned = DrainWave(director, aliveCount: 1);

        Assert.Equal(3, spawned);
        Assert.Equal(0, director.RemainingToSpawn);
    }

    [Fact]
    public void Update_OnceTheQuotaIsSpentAndTheArenaIsEmpty_AdvancesTheWave()
    {
        WaveDirector director = Create(Settings(firstWave: 3, addedPerWave: 2), new FixedRandom(0.9f));
        DrainWave(director, aliveCount: 1);

        director.Update(Frame, 0);

        Assert.Equal(2, director.CurrentWave);
        Assert.Equal(5, director.WaveSize);
        Assert.Equal(5, director.RemainingToSpawn);
    }

    [Fact]
    public void CreateEnemy_ScalesSpeedAndDamageButNeverHealth()
    {
        GameSettings settings = new()
        {
            EnemiesInFirstWave = 3,
            MaxSpeedScale = 2f,
            WavesToMaxSpeed = 2,
            MaxDamageScale = 3f,
            WavesToMaxDamage = 2
        };

        WaveDirector director = new(
            settings,
            new DifficultyCurve(settings),
            new FixedRandom(0.99f),
            new ArenaBounds(settings.ArenaWidth, settings.ArenaHeight));

        Enemy first = director.CreateEnemy(1);
        Enemy later = director.CreateEnemy(2);

        Assert.Equal(first.MaxHealth, later.MaxHealth, 1e-3f);
        Assert.True(later.BaseSpeed > first.BaseSpeed);
        Assert.True(later.ContactDamage > first.ContactDamage);
    }
}
