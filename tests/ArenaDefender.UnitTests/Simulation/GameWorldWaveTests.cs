using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Simulation;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Simulation;

public class GameWorldWaveTests
{
    private const float Frame = 1f / 60f;

    private static PlayerIntent Firing => new(Vector2.Zero, Vector2.Zero, true);

    private static GameSettings Settings() => new()
    {
        ArenaWidth = 200f,
        ArenaHeight = 200f,
        PlayerMaxHealth = 100000f,
        PlayerProjectileDamage = 500f,
        PlayerFireInterval = 0.05f,
        InitialSpawnInterval = 0.05f,
        MinimumSpawnInterval = 0.05f,
        EnemiesInFirstWave = 2,
        EnemiesAddedPerWave = 1,
        WaveBreatherSeconds = 0f,
        PowerUpDropChance = 0f
    };

    /// <summary>Integer zero picks the top edge, 0.5 the middle of it, and 0.99 always a chaser.</summary>
    private static ScriptedRandom SpawnScript() => new(new[] { 0.5f, 0.99f }, new[] { 0 });

    private static bool AdvanceOneWave(GameWorld world, int frames = 1200)
    {
        int startingWave = world.WaveNumber;

        for (int frame = 0; frame < frames; frame++)
        {
            world.Update(Frame, Firing);

            if (world.WaveNumber != startingWave)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void Update_ClearingAWave_AdvancesTheNumber()
    {
        GameWorld world = new(Settings(), SpawnScript());
        world.StartNewRun();

        Assert.True(AdvanceOneWave(world), "The first wave was never cleared.");

        Assert.Equal(2, world.WaveNumber);
    }

    [Fact]
    public void Update_LosingALife_RestartsTheWaveRatherThanClearingIt()
    {
        GameSettings settings = new()
        {
            ArenaWidth = 200f,
            ArenaHeight = 200f,
            PlayerMaxHealth = 5f,
            PlayerStartingLives = 3,
            PlayerRespawnInvulnerability = 0f,
            InitialSpawnInterval = 0.0001f,
            MinimumSpawnInterval = 0.0001f,
            EnemiesInFirstWave = 6,
            WaveBreatherSeconds = 0f,
            PowerUpDropChance = 0f
        };

        GameWorld world = new(settings, new FixedRandom(0.99f));
        world.StartNewRun();

        for (int frame = 0; frame < 200 && world.Enemies.Count < 3; frame++)
        {
            world.Update(0.001f, PlayerIntent.Idle);
        }

        foreach (Enemy enemy in world.Enemies)
        {
            enemy.Position = world.Player.Position;
        }

        int scoreBefore = world.Score.Score;
        world.Update(Frame, PlayerIntent.Idle);

        // The arena is wiped by the life loss, which must not read as a cleared wave.
        Assert.Equal(2, world.Player.Lives);
        Assert.Equal(1, world.WaveNumber);
        Assert.Equal(scoreBefore, world.Score.Score);
    }
}
