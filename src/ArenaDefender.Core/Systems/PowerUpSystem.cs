using System;
using System.Collections.Generic;
using System.Linq;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;

namespace ArenaDefender.Core.Systems;

/// <summary>One power up effect currently running on the player.</summary>
public readonly struct ActiveEffect
{
    public ActiveEffect(PowerUpKind kind, float remainingSeconds, float totalSeconds)
    {
        Kind = kind;
        RemainingSeconds = remainingSeconds;
        TotalSeconds = totalSeconds;
    }

    public PowerUpKind Kind { get; }

    public float RemainingSeconds { get; }

    public float TotalSeconds { get; }

    public float Fraction => TotalSeconds <= 0f ? 0f : RemainingSeconds / TotalSeconds;
}

/// <summary>Hands collected power ups to the player and expires them when their time runs out.</summary>
public sealed class PowerUpSystem
{
    private readonly Dictionary<PowerUpKind, (float Remaining, float Total)> _running = new();

    public PowerUpSystem(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Duration = settings.PowerUpDuration;
    }

    public float Duration { get; set; }

    public IReadOnlyList<ActiveEffect> ActiveEffects =>
        _running
            .Select(pair => new ActiveEffect(pair.Key, pair.Value.Remaining, pair.Value.Total))
            .OrderBy(effect => effect.RemainingSeconds)
            .ToList();

    public bool IsActive(PowerUpKind kind) => _running.ContainsKey(kind);

    public void Collect(PowerUpKind kind, Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        // Repair and Shield are instant; everything else waits on the timer.
        switch (kind)
        {
            case PowerUpKind.Repair:
                player.Heal(player.MaxHealth * 0.35f);
                break;

            case PowerUpKind.Shield:
                player.GrantShield(2);
                break;

            default:
                _running[kind] = (Duration, Duration);
                break;
        }

        Reapply(player);
    }

    public void Update(float deltaSeconds, Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (deltaSeconds > 0f && _running.Count > 0)
        {
            foreach (PowerUpKind kind in _running.Keys.ToList())
            {
                (float previous, float total) = _running[kind];
                float remaining = previous - deltaSeconds;

                if (remaining <= 0f)
                {
                    _running.Remove(kind);
                }
                else
                {
                    _running[kind] = (remaining, total);
                }
            }
        }

        Reapply(player);
    }

    public void Clear(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        _running.Clear();
        player.ResetModifiers();
    }

    private void Reapply(Player player)
    {
        player.ResetModifiers();

        if (IsActive(PowerUpKind.RapidFire))
        {
            player.FireRateMultiplier *= 2.1f;
        }

        if (IsActive(PowerUpKind.DoubleDamage))
        {
            player.DamageMultiplier *= 2f;
        }

        if (IsActive(PowerUpKind.BoostyBoost))
        {
            player.SpeedMultiplier *= 1.45f;
        }
    }
}
