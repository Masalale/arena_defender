using System.Numerics;
using ArenaDefender.Core.Entities;

namespace ArenaDefender.UnitTests.TestDoubles;

/// <summary>
/// An <see cref="IEnemyActions"/> stub that counts shot requests instead of creating projectiles,
/// so an enemy can be tested without a world around it.
/// </summary>
public sealed class RecordingEnemyActions : IEnemyActions
{
    public int ShotCount { get; private set; }

    /// <inheritdoc />
    public void FireProjectile(Vector2 origin, Vector2 direction, float speed, float damage) => ShotCount++;
}
