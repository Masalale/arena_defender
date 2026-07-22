using System;
using System.Collections.Generic;
using ArenaDefender.Core.Entities;

namespace ArenaDefender.Core.Systems;

/// <summary>Circle overlap checks shared by all the collision passes.</summary>
public static class CollisionResolver
{
    public static bool Overlaps(Entity first, Entity second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        float combinedRadius = first.Radius + second.Radius;

        return first.IsActive
            && second.IsActive
            && (second.Position - first.Position).LengthSquared() <= combinedRadius * combinedRadius;
    }

    public static TEntity? FindFirstOverlap<TEntity>(Entity subject, IEnumerable<TEntity> candidates)
        where TEntity : Entity
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(candidates);

        // Skip the scan entirely for an inactive subject.
        if (!subject.IsActive)
        {
            return null;
        }

        foreach (TEntity candidate in candidates)
        {
            if (candidate is not null && Overlaps(subject, candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
