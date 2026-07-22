using System;
using ArenaDefender.Core.Configuration;

namespace ArenaDefender.Core.Systems;

public sealed class ScoreBoard
{
    private readonly GameSettings _settings;

    private double _score;
    private float _secondsSinceLastKill;

    /// <summary>Parks the combo clock so the window does not start counting until the first kill.</summary>
    public ScoreBoard(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        _secondsSinceLastKill = float.MaxValue;
    }

    public int Score => (int)_score;

    public int ComboCount { get; private set; }

    public int TotalKills { get; private set; }

    public float ComboMultiplier =>
        Math.Clamp(1f + (ComboCount * 0.25f), 1f, _settings.MaxComboMultiplier);

    public float ComboSecondsRemaining =>
        ComboCount == 0 ? 0f : MathF.Max(0f, _settings.ComboWindowSeconds - _secondsSinceLastKill);

    public void Update(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || ComboCount == 0)
        {
            return;
        }

        _secondsSinceLastKill += deltaSeconds;

        if (_secondsSinceLastKill > _settings.ComboWindowSeconds)
        {
            ComboCount = 0;
        }
    }

    public int AwardKill(int baseValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseValue);

        TotalKills++;
        ComboCount++;
        _secondsSinceLastKill = 0f;

        int awarded = (int)MathF.Round(baseValue * ComboMultiplier);
        _score += awarded;
        return awarded;
    }

    public int AwardPickup()
    {
        _score += _settings.PowerUpPickupPoints;
        return _settings.PowerUpPickupPoints;
    }

    public int AwardBonus(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);

        _score += points;
        return points;
    }

    public void BreakCombo()
    {
        ComboCount = 0;
        _secondsSinceLastKill = float.MaxValue;
    }

    public void Reset()
    {
        _score = 0d;
        ComboCount = 0;
        TotalKills = 0;
        _secondsSinceLastKill = float.MaxValue;
    }
}
