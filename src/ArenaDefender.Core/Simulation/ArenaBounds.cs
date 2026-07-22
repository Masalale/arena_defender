using System;
using System.Numerics;

namespace ArenaDefender.Core.Simulation;

/// <summary>The rectangular playfield plus the helpers for keeping entities inside it.</summary>
public readonly struct ArenaBounds
{
    public ArenaBounds(float width, float height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
    }

    public float Width { get; }

    public float Height { get; }

    public Vector2 Centre => new(Width * 0.5f, Height * 0.5f);

    // Pushes a position back inside so a body of the given radius stays fully visible.
    public Vector2 Clamp(Vector2 position, float radius)
    {
        float clampedRadius = Math.Clamp(radius, 0f, MathF.Min(Width, Height) * 0.5f);

        return new Vector2(
            Math.Clamp(position.X, clampedRadius, Width - clampedRadius),
            Math.Clamp(position.Y, clampedRadius, Height - clampedRadius));
    }

    public bool IsOutside(Vector2 position, float margin)
    {
        return position.X < -margin
            || position.Y < -margin
            || position.X > Width + margin
            || position.Y > Height + margin;
    }
}
