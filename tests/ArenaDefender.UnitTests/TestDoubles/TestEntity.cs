using System.Numerics;
using ArenaDefender.Core.Entities;

namespace ArenaDefender.UnitTests.TestDoubles;

    /// <summary>
    /// A concrete <see cref="Entity"/> for exercising the shared entity and collision
    /// geometry without gameplay behaviour getting in the way.
    /// </summary>
public sealed class TestEntity : Entity
{
    public TestEntity(Vector2 position, float radius)
        : base(position, radius)
    {
    }

    /// <inheritdoc />
    protected override void OnUpdate(float deltaSeconds) => Integrate(deltaSeconds);
}
