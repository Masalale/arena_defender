using System;
using ArenaDefender.Core.Mathematics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Presentation;

/// <summary>Kicks the view when the player takes a hit, then eases it back to centre.</summary>
public sealed class CameraShake
{
    private const float DecaySharpness = 3.4f;
    private const float MaxOffsetPixels = 22f;
    private const float NoiseSpeed = 34f;

    private readonly Random _random = new();

    private float _trauma;
    private float _noiseSeedX;
    private float _noiseSeedY;
    private float _time;

    /// <summary>Starts settled, with random noise phases so the first hit has no fixed direction.</summary>
    public CameraShake()
    {
        _noiseSeedX = (float)_random.NextDouble() * MathF.Tau;
        _noiseSeedY = (float)_random.NextDouble() * MathF.Tau;
    }

    public Vector2 Offset { get; private set; }

    /// <summary>Adds to the shake, clamped at full strength.</summary>
    /// <param name="amount">Shake strength, 0..1, one being the hardest.</param>
    public void Add(float amount)
    {
        _trauma = GameMath.Clamp01(_trauma + MathF.Max(0f, amount));

        // Rerolling the phase means two hits in quick succession do not shake along the same line.
        _noiseSeedX = (float)_random.NextDouble() * MathF.Tau;
        _noiseSeedY = (float)_random.NextDouble() * MathF.Tau;
    }

    /// <summary>Kills the shake outright, for screen changes.</summary>
    public void Reset()
    {
        _trauma = 0f;
        Offset = Vector2.Zero;
    }

    public void Update(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
        {
            return;
        }

        _time += deltaSeconds;
        _trauma = GameMath.Damp(_trauma, 0f, DecaySharpness, deltaSeconds);

        if (_trauma <= 0.001f)
        {
            _trauma = 0f;
            Offset = Vector2.Zero;
            return;
        }

        float magnitude = _trauma * _trauma * MaxOffsetPixels;

        Offset = new Vector2(
            MathF.Sin((_time * NoiseSpeed) + _noiseSeedX) * magnitude,
            MathF.Sin((_time * NoiseSpeed * 0.83f) + _noiseSeedY) * magnitude);
    }
}
