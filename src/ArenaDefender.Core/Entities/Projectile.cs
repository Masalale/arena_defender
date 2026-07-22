using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities;

/// <summary>Who fired a projectile, so it only hurts the other side.</summary>
public enum ProjectileOwner
{
    Player,

    Enemy
}

public sealed class Projectile : Entity
{
    private readonly float _lifetime;

    public Projectile(
        Vector2 position,
        Vector2 direction,
        float speed,
        float damage,
        float radius,
        float lifetime,
        ProjectileOwner owner)
        : base(position, radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(speed);
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifetime);

        Damage = damage;
        Owner = owner;
        _lifetime = lifetime;

        // A zero direction leaves the shot on its firer, hitting everything nearby.
        Vector2 heading = GameMath.SafeNormalize(direction);
        Velocity = heading == Vector2.Zero ? Vector2.Zero : heading * speed;
    }

    public float Damage { get; }

    public ProjectileOwner Owner { get; }

    public float LifeFraction => GameMath.Clamp01(Age / _lifetime);

    /// <inheritdoc />
    protected override void OnUpdate(float deltaSeconds)
    {
        Integrate(deltaSeconds);

        if (Age >= _lifetime)
        {
            Deactivate();
        }
    }
}
