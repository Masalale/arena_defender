using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities.Enemies;

/// <summary>Slow and armoured, with a hard cap on how fast it can turn. The cross product picks the direction.</summary>
public sealed class BruteEnemy : Enemy
{
    private const float MaxTurnRateRadians = 1.5f;

    // Face the origin on spawn; if the brute somehow starts there, fall back to straight down.
    public BruteEnemy(Vector2 position)
        : base(position, radius: 24f, health: 140f, speed: 78f, contactDamage: 22f, scoreValue: 300)
    {
        Facing = GameMath.SafeNormalize(-position);

        if (Facing == Vector2.Zero)
        {
            Facing = new Vector2(0f, 1f);
        }
    }

    /// <inheritdoc />
    protected override void Steer(float deltaSeconds)
    {
        Vector2 toPlayer = GameMath.DirectionTo(Position, PlayerPosition);

        if (toPlayer == Vector2.Zero)
        {
            Velocity = Vector2.Zero;
            return;
        }

        // Cross product sign decides the rotation direction; the leftover angle decides whether a
        // full turn step would overshoot.
        int turn = GameMath.TurnDirection(Facing, toPlayer);
        float remaining = GameMath.AngleBetween(Facing, toPlayer);

        // A zero cross product means the vectors are parallel: already aimed, or exactly behind.
        // From behind there is no shorter way round, so it would stall facing away forever.
        // Picking a direction up front breaks the tie.
        if (turn == 0 && remaining > MathF.PI * 0.5f)
        {
            turn = 1;
        }

        float step = MathF.Min(MaxTurnRateRadians * deltaSeconds, remaining);

        if (turn != 0 && step > 0f)
        {
            Facing = GameMath.FromAngle(GameMath.ToAngle(Facing) + (turn * step));
        }

        // Speed drops while badly misaligned, so the brute lumbers through its turns.
        float alignment = GameMath.Clamp01(GameMath.Dot(Facing, toPlayer));
        Velocity = Facing * BaseSpeed * GameMath.Lerp(0.35f, 1f, alignment);
    }
}
