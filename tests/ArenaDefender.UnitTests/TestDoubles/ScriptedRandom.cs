using System;
using System.Collections.Generic;
using System.Linq;
using ArenaDefender.Core.Simulation;

namespace ArenaDefender.UnitTests.TestDoubles;

    /// <summary>
    /// An <see cref="IRandomSource"/> that replays a caller supplied script of values, cycling
    /// back to the start when the script runs out, keeping spawns and enemy picks deterministic.
    /// </summary>
public sealed class ScriptedRandom : IRandomSource
{
    private readonly float[] _singles;
    private readonly int[] _integers;

    private int _singleCursor;
    private int _integerCursor;

    /// <summary>With no integer script, <see cref="NextInt"/> derives its values from the singles.</summary>
    public ScriptedRandom(IEnumerable<float> singles, IEnumerable<int> integers)
    {
        ArgumentNullException.ThrowIfNull(singles);
        ArgumentNullException.ThrowIfNull(integers);

        _singles = singles.ToArray();
        _integers = integers.ToArray();

        if (_singles.Length == 0)
        {
            throw new ArgumentException("At least one scripted single is required.", nameof(singles));
        }
    }

    /// <inheritdoc />
    public float NextSingle()
    {
        float value = _singles[_singleCursor];
        _singleCursor = (_singleCursor + 1) % _singles.Length;
        return value;
    }

    /// <inheritdoc />
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        int raw;

        if (_integers.Length == 0)
        {
            raw = minInclusive + (int)(NextSingle() * (maxExclusive - minInclusive));
        }
        else
        {
            raw = _integers[_integerCursor];
            _integerCursor = (_integerCursor + 1) % _integers.Length;
        }

        return Math.Clamp(raw, minInclusive, maxExclusive - 1);
    }
}
