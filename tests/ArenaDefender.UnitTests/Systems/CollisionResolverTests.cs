using System.Numerics;
using ArenaDefender.Core.Systems;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Systems;

public class CollisionResolverTests
{
    [Fact]
    public void Overlaps_CirclesExactlyTouching_ReturnsTrue()
    {
        // The boundary case: distance equals the sum of the radii, which the comparison treats as
        // contact rather than as a miss.
        TestEntity first = new(new Vector2(0f, 0f), 10f);
        TestEntity second = new(new Vector2(15f, 0f), 5f);

        Assert.True(CollisionResolver.Overlaps(first, second));
    }

    [Fact]
    public void Overlaps_NullArgument_Throws()
    {
        TestEntity entity = new(Vector2.Zero, 5f);

        Assert.Throws<ArgumentNullException>(() => CollisionResolver.Overlaps(null!, entity));
        Assert.Throws<ArgumentNullException>(() => CollisionResolver.Overlaps(entity, null!));
    }
}
