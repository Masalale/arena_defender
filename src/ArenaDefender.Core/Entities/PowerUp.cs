using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities;

public enum PowerUpKind
{
    Repair,

    RapidFire,

    DoubleDamage,

    /// <summary>Faster movement for a while.</summary>
    BoostyBoost,

    Shield
}

public sealed class PowerUp : Entity
{
    private readonly float _lifetime;

    public PowerUp(Vector2 position, PowerUpKind kind, float radius, float lifetime)
        : base(position, radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifetime);

        Kind = kind;
        _lifetime = lifetime;
    }

    public PowerUpKind Kind { get; }

    public float RemainingSeconds => MathF.Max(0f, _lifetime - Age);

    /// <summary>Opacity in 0..1, fading over the last two seconds as a warning.</summary>
    public float Opacity => GameMath.Clamp01(RemainingSeconds / 2f);

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
