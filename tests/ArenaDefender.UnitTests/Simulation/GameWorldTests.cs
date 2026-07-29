using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Simulation;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Simulation;

public class GameWorldTests
{
    private const float Tolerance = 1e-3f;
    private const float Frame = 1f / 60f;

    /// <summary>
    /// Small arena, fast spawns, tiny waves, so a short scripted random
    /// sequence can cover every decision the world makes.
    /// </summary>
    private static GameSettings Settings(int lives = 3, float maxHealth = 100f) => new()
    {
        ArenaWidth = 200f,
        ArenaHeight = 200f,
        PlayerRadius = 16f,
        PlayerMaxHealth = maxHealth,
        PlayerStartingLives = lives,
        PlayerProjectileDamage = 500f,
        PlayerFireInterval = 0.05f,
        InitialSpawnInterval = 0.05f,
        MinimumSpawnInterval = 0.05f,
        EnemiesInFirstWave = 1,
        EnemiesAddedPerWave = 1,
        WaveBreatherSeconds = 0f,
        PowerUpDropChance = 0f
    };

    /// <summary>Edge zero and the middle of it, then a roll that always resolves to a chaser.</summary>
    private static ScriptedRandom SpawnScript => new(new[] { 0.5f, 0.99f }, new[] { 0 });

    private static PlayerIntent Firing => new(Vector2.Zero, Vector2.Zero, true);

    [Fact]
    public void Constructor_NullDependency_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new GameWorld(null!, new FixedRandom(0.5f)));
        Assert.Throws<ArgumentNullException>(() => _ = new GameWorld(new GameSettings(), null!));
    }

    [Fact]
    public void StartNewRun_MovesToPlayingAndResetsEverything()
    {
        GameSettings settings = Settings();
        GameWorld world = new(settings, SpawnScript);

        world.StartNewRun();

        for (int frame = 0; frame < 120; frame++)
        {
            world.Update(Frame, Firing);
        }

        world.StartNewRun();

        Assert.Equal(GameState.Playing, world.State);
        Assert.Empty(world.Enemies);
        Assert.Empty(world.Projectiles);
        Assert.Empty(world.Pickups);
        Assert.Equal(0, world.Score.Score);
        Assert.Equal(0, world.Score.TotalKills);
        Assert.Equal(settings.PlayerStartingLives, world.Player.Lives);
        Assert.Equal(settings.PlayerMaxHealth, world.Player.Health, Tolerance);
        Assert.Equal(1, world.WaveNumber);
    }

    [Fact]
    public void Update_PlayerProjectileHitsAnEnemy_DestroysItAndAwardsScore()
    {
        GameWorld world = new(Settings(), SpawnScript);
        world.StartNewRun();

        for (int frame = 0; frame < 60; frame++)
        {
            world.Update(Frame, Firing);
        }

        Assert.True(world.Score.TotalKills >= 1);
        Assert.True(world.Score.Score > 0);
    }

    [Fact]
    public void Update_ChaserReachesThePlayer_DiesOnContactWithoutScoring()
    {
        // Health way above chaser contact damage, so the arena empties because the chaser died,
        // not because losing a life wiped it.
        GameWorld world = new(Settings(maxHealth: 1000f), SpawnScript);
        world.StartNewRun();

        while (world.Enemies.Count == 0)
        {
            world.Update(Frame, PlayerIntent.Idle);
        }

        Enemy chaser = world.Enemies[0];
        chaser.Position = world.Player.Position;
        float healthBefore = world.Player.Health;

        world.Update(Frame, PlayerIntent.Idle);

        Assert.False(chaser.IsActive);
        Assert.True(world.Player.Health < healthBefore);
        Assert.Equal(0, world.Score.Score);
    }

    [Fact]
    public void Update_AllLivesLost_EndsTheRunInGameOver()
    {
        GameWorld world = new(Settings(lives: 1, maxHealth: 5f), SpawnScript);
        world.StartNewRun();

        for (int frame = 0; frame < 600 && world.State != GameState.GameOver; frame++)
        {
            world.Update(Frame, PlayerIntent.Idle);
        }

        Assert.Equal(GameState.GameOver, world.State);
        Assert.True(world.Player.IsDefeated);
        Assert.Equal(0, world.Player.Lives);
    }
}
