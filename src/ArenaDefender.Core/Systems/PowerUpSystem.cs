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

    /// <summary>The power up that is running.</summary>
    public PowerUpKind Kind { get; }

    /// <summary>Seconds left before the effect lapses.</summary>
    public float RemainingSeconds { get; }

    public float TotalSeconds { get; }

    /// <summary>How much of the effect is left, 0..1.</summary>
    public float Fraction => TotalSeconds <= 0f ? 0f : RemainingSeconds / TotalSeconds;
}

/// <summary>Hands collected power ups to the player and expires them when their time runs out.</summary>
public sealed class PowerUpSystem
{
    private readonly Dictionary<PowerUpKind, (float Remaining, float Total)> _running = new();

    /// <summary>Creates the system; pulls the default duration out of settings.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public PowerUpSystem(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Duration = settings.PowerUpDuration;
    }

    /// <summary>Default length for newly collected timed power ups.</summary>
    public float Duration { get; set; }

    /// <summary>Running effects, soonest to expire first.</summary>
    public IReadOnlyList<ActiveEffect> ActiveEffects =>
        _running
            .Select(pair => new ActiveEffect(pair.Key, pair.Value.Remaining, pair.Value.Total))
            .OrderBy(effect => effect.RemainingSeconds)
            .ToList();

    /// <summary>Whether that effect is up right now.</summary>
    public bool IsActive(PowerUpKind kind) => _running.ContainsKey(kind);

    /// <summary>Applies the pickup: instant ones hit immediately, timed ones go on the clock.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="player"/> is null.</exception>
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

    /// <summary>Ticks the timers down and re-derives the multipliers.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="player"/> is null.</exception>
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

    /// <summary>Drops every running effect; called when a life is lost or a run restarts.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="player"/> is null.</exception>
    public void Clear(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        _running.Clear();
        player.ResetModifiers();
    }

    /// <summary>Recomputes the player's multipliers from whatever is still running.</summary>
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

        if (IsActive(PowerUpKind.SpeedBoost))
        {
            player.SpeedMultiplier *= 1.45f;
        }
    }
}
