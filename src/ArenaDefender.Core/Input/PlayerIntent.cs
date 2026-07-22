using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Input;

public readonly struct PlayerIntent
{
    /// <summary>Builds an intent, normalising the directions.</summary>
    public PlayerIntent(Vector2 moveDirection, Vector2 aimDirection, bool firePrimary)
    {
        MoveDirection = GameMath.SafeNormalize(moveDirection);
        AimDirection = GameMath.SafeNormalize(aimDirection);
        FirePrimary = firePrimary;
    }

    public Vector2 MoveDirection { get; }

    public Vector2 AimDirection { get; }

    public bool FirePrimary { get; }

    public static PlayerIntent Idle => new(Vector2.Zero, Vector2.Zero, false);
}
