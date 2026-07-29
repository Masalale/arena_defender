using ArenaDefender.Core.Simulation;

namespace ArenaDefender.UnitTests.TestDoubles;

    /// <summary>
    /// An <see cref="IRandomSource"/> that always answers the same value, pinning one branch of a
    /// probabilistic decision so a test can assert it directly.
    /// </summary>
public sealed class FixedRandom : IRandomSource
{
    private readonly float _single;

    /// <summary>Every integer request answers with the minimum, whatever the single is.</summary>
    public FixedRandom(float single) => _single = single;

    /// <inheritdoc />
    public float NextSingle() => _single;

    /// <inheritdoc />
    public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
}
