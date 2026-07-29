using System.Numerics;
using ArenaDefender.Core.Entities;

namespace ArenaDefender.UnitTests.Entities;

    /// <summary>Projectile flight: it travels on its heading, then dies of old age.</summary>
public class ProjectileTests
{
    private const float Tolerance = 1e-3f;

    private static Projectile Create(Vector2 direction) =>
        new(Vector2.Zero, direction, speed: 400f, damage: 25f, radius: 4f, lifetime: 2f, ProjectileOwner.Player);

    [Fact]
    public void Update_TravelsAlongItsHeadingAtItsSpeed()
    {
        Projectile shot = Create(new Vector2(1f, 0f));

        shot.Update(0.5f);

        Assert.Equal(200f, shot.Position.X, Tolerance);
        Assert.Equal(0f, shot.Position.Y, Tolerance);
    }

    [Fact]
    public void Update_ZeroDirection_StaysPutInsteadOfCrashing()
    {
        Projectile shot = Create(Vector2.Zero);

        shot.Update(1f);

        // A zero heading must not produce a NaN velocity that then flies off the map.
        Assert.Equal(Vector2.Zero, shot.Position);
        Assert.True(shot.IsActive);
    }

    [Fact]
    public void Update_OnceItsLifetimeIsUp_Deactivates()
    {
        Projectile shot = Create(new Vector2(0f, 1f));

        shot.Update(2f);

        Assert.False(shot.IsActive);
    }
}
