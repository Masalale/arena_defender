using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Simulation;

namespace ArenaDefender.UnitTests.Entities;

public class PlayerTests
{
    private const float Tolerance = 1e-3f;

    private static GameSettings Settings() => new()
    {
        ArenaWidth = 1000f,
        ArenaHeight = 600f,
        PlayerRadius = 16f,
        PlayerMaxHealth = 100f,
        PlayerStartingLives = 3,
        PlayerSpeed = 260f,
        PlayerFireInterval = 0.2f,
        PlayerRespawnInvulnerability = 2f,
        PlayerHitInvulnerability = 0.5f
    };

    private static Player CreatePlayer(GameSettings settings) =>
        new(settings, new ArenaBounds(settings.ArenaWidth, settings.ArenaHeight));

    [Fact]
    public void TakeDamage_HealthyPlayer_ReducesHealthAndReportsAHit()
    {
        Player player = CreatePlayer(Settings());

        bool hurt = player.TakeDamage(30f);

        Assert.True(hurt);
        Assert.Equal(70f, player.Health, Tolerance);
        Assert.Equal(0.7f, player.HealthFraction, Tolerance);
    }

    [Fact]
    public void TakeDamage_HealthExhaustedWithLivesLeft_ConsumesALifeAndRespawnsAtTheCentre()
    {
        GameSettings settings = Settings();
        Player player = CreatePlayer(settings);

        player.Apply(new PlayerIntent(new Vector2(1f, 0f), Vector2.Zero, false));
        player.Update(1f);
        Assert.True(player.Position.X > 600f);

        player.TakeDamage(settings.PlayerMaxHealth);

        Assert.Equal(2, player.Lives);
        Assert.Equal(settings.PlayerMaxHealth, player.Health, Tolerance);
        Assert.Equal(500f, player.Position.X, Tolerance);
        Assert.Equal(300f, player.Position.Y, Tolerance);
        Assert.Equal(Vector2.Zero, player.Velocity);
        Assert.False(player.IsDefeated);
    }
}
