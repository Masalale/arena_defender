using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities.Enemies;

/// <summary>Fast and fragile. Runs straight at the player and relies on numbers.</summary>
public sealed class ChaserEnemy : Enemy
{
    public ChaserEnemy(Vector2 position)
        : base(position, radius: 13f, health: 30f, speed: 165f, contactDamage: 8f, scoreValue: 100)
    {
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

        Velocity = toPlayer * BaseSpeed;
        Facing = toPlayer;
    }
}
