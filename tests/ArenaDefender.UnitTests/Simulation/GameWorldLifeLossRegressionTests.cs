using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Simulation;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Simulation;

    /// <summary>Pins the crash from losing a life while the collision pass is still running.</summary>
public class GameWorldLifeLossRegressionTests
{
    /// <summary>Spawns arrive nearly instantly so the arena fills within a handful of frames.</summary>
    private static GameSettings Settings => new()
    {
        PlayerMaxHealth = 5f,
        PlayerStartingLives = 3,
        PlayerRespawnInvulnerability = 0f,
        PowerUpDropChance = 0f,
        InitialSpawnInterval = 0.0001f,
        MinimumSpawnInterval = 0.0001f,

        // Losing a life restarts the wave, and waves normally open with a breather. Zeroed so the
        // test can refill the arena instead of sitting through the pause.
        WaveBreatherSeconds = 0f,
        EnemiesInFirstWave = 40
    };

    [Fact]
    public void Update_WhenContactDamageCostsALife_DoesNotThrow()
    {
        GameWorld world = BuildWorldWithEnemiesTouchingPlayer(enemyCount: 6);

        // One frame of chaser contact deals more than the 5 health set above.
        Exception? caught = Record.Exception(() => world.Update(1f / 60f, PlayerIntent.Idle));

        Assert.Null(caught);
    }

    private static GameWorld BuildWorldWithEnemiesTouchingPlayer(int enemyCount)
    {
        var world = new GameWorld(Settings, new FixedRandom(0.99f));
        world.StartNewRun();
        PlaceEnemiesOnPlayer(world, enemyCount);
        return world;
    }

    /// <summary>Puts every enemy on the player so the next frame resolves contact damage.</summary>
    private static void PlaceEnemiesOnPlayer(GameWorld world, int enemyCount)
    {
        int guard = 0;

        // Tiny steps keep the enemies near their spawn edge while the arena fills.
        while (world.Enemies.Count < enemyCount && guard++ < 500)
        {
            world.Update(0.001f, PlayerIntent.Idle);
        }

        Vector2 playerPosition = world.Player.Position;

        foreach (Enemy enemy in world.Enemies)
        {
            enemy.Position = playerPosition;
        }
    }
}
