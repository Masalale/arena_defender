using System;

namespace ArenaDefender.Core.Simulation;

public interface IRandomSource
{
    /// <summary>Float in the half open range 0..1.</summary>
    float NextSingle();

    /// <summary>Integer in <paramref name="minInclusive"/>..<paramref name="maxExclusive"/>.</summary>
    int NextInt(int minInclusive, int maxExclusive);
}

public static class RandomSourceExtensions
{
    public static float NextRange(this IRandomSource source, float min, float max)
    {
        ArgumentNullException.ThrowIfNull(source);
        return min + ((max - min) * source.NextSingle());
    }

    public static bool Chance(this IRandomSource source, float probability)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (probability <= 0f)
        {
            return false;
        }

        return probability >= 1f || source.NextSingle() < probability;
    }
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource() => _random = new Random();

    public float NextSingle() => _random.NextSingle();

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
