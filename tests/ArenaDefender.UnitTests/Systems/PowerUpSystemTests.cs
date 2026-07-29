using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Simulation;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.UnitTests.Systems;

    /// <summary>Collected power ups: the effect they apply, and when it expires.</summary>
public class PowerUpSystemTests
{
    private const float Tolerance = 1e-3f;

    private static GameSettings Settings() => new()
    {
        ArenaWidth = 800f,
        ArenaHeight = 600f,
        PlayerMaxHealth = 100f,
        PowerUpDuration = 4f
    };

    private static Player CreatePlayer(GameSettings settings) =>
        new(settings, new ArenaBounds(settings.ArenaWidth, settings.ArenaHeight));

    [Fact]
    public void Collect_RapidFire_RaisesTheFireRateMultiplier()
    {
        GameSettings settings = Settings();
        Player player = CreatePlayer(settings);
        PowerUpSystem system = new(settings);

        system.Collect(PowerUpKind.RapidFire, player);

        Assert.True(system.IsActive(PowerUpKind.RapidFire));
        Assert.True(player.FireRateMultiplier > 1f);
        Assert.Equal(1f, player.DamageMultiplier, Tolerance);
        Assert.Equal(1f, player.SpeedMultiplier, Tolerance);
    }

    [Theory]
    [InlineData(PowerUpKind.RapidFire)]
    [InlineData(PowerUpKind.DoubleDamage)]
    [InlineData(PowerUpKind.SpeedBoost)]
    public void Update_OnceTheDurationElapses_TheEffectExpiresAndMultipliersReturnToOne(PowerUpKind kind)
    {
        GameSettings settings = Settings();
        Player player = CreatePlayer(settings);
        PowerUpSystem system = new(settings);
        system.Collect(kind, player);

        system.Update(settings.PowerUpDuration, player);

        Assert.False(system.IsActive(kind));
        Assert.Empty(system.ActiveEffects);
        Assert.Equal(1f, player.FireRateMultiplier, Tolerance);
        Assert.Equal(1f, player.DamageMultiplier, Tolerance);
        Assert.Equal(1f, player.SpeedMultiplier, Tolerance);
    }
}
