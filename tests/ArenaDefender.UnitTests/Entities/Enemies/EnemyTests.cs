using System.Numerics;
using ArenaDefender.Core.Entities.Enemies;

namespace ArenaDefender.UnitTests.Entities.Enemies;

public class EnemyTests
{
    private const float Tolerance = 1e-3f;

    [Fact]
    public void ApplyDifficultyScale_MultipliesSpeedAndDamage()
    {
        ChaserEnemy enemy = new(Vector2.Zero);
        float baseSpeed = enemy.BaseSpeed;
        float baseDamage = enemy.ContactDamage;

        enemy.ApplyDifficultyScale(1.5f, 2f);

        Assert.Equal(baseSpeed * 1.5f, enemy.BaseSpeed, Tolerance);
        Assert.Equal(baseDamage * 2f, enemy.ContactDamage, Tolerance);
        Assert.Equal(2f, enemy.DamageScale, Tolerance);
    }

    [Fact]
    public void TakeDamage_KillingBlow_ReturnsTrueOnceAndDeactivates()
    {
        ChaserEnemy enemy = new(Vector2.Zero);

        Assert.False(enemy.TakeDamage(15f));
        Assert.True(enemy.TakeDamage(15f));
        Assert.False(enemy.IsActive);
        Assert.Equal(0f, enemy.Health, Tolerance);
    }
}
