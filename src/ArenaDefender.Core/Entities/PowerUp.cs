using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities;

/// <summary>Kinds of power up an enemy can drop.</summary>
public enum PowerUpKind
{
    // Heals instantly.
    Repair,

    /// <summary>Shortens the gap between shots for a while.</summary>
    RapidFire,

    /// <summary>More projectile damage for a while.</summary>
    DoubleDamage,

    /// <summary>Faster movement for a while.</summary>
    SpeedBoost,

    /// <summary>Soaks up a few hits.</summary>
    Shield
}

/// <summary>A collectible that drifts toward a nearby player and vanishes if left.</summary>
public sealed class PowerUp : Entity
{
    private readonly float _lifetime;

    /// <summary>Places a power up.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Lifetime must be positive.</exception>
    public PowerUp(Vector2 position, PowerUpKind kind, float radius, float lifetime)
        : base(position, radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifetime);

        Kind = kind;
        _lifetime = lifetime;
    }

    public PowerUpKind Kind { get; }

    /// <summary>Seconds left before it disappears.</summary>
    public float RemainingSeconds => MathF.Max(0f, _lifetime - Age);

    /// <summary>Opacity in 0..1, fading over the last two seconds as a warning.</summary>
    public float Opacity => GameMath.Clamp01(RemainingSeconds / 2f);

    /// <summary>Pulls the pickup toward the player once close enough, stronger as the gap closes.</summary>
    public void AttractTowards(Vector2 playerPosition, float magnetRange, float deltaSeconds)
    {
        if (!IsActive || magnetRange <= 0f || !GameMath.IsWithinRange(Position, playerPosition, magnetRange))
        {
            return;
        }

        float closeness = 1f - GameMath.Clamp01(GameMath.Distance(Position, playerPosition) / magnetRange);
        Position = GameMath.Damp(Position, playerPosition, closeness * 9f, deltaSeconds);
    }

    /// <inheritdoc />
    protected override void OnUpdate(float deltaSeconds)
    {
        if (Age >= _lifetime)
        {
            Deactivate();
        }
    }
}
