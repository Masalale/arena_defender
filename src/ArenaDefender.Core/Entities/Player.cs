using System;
using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Simulation;

namespace ArenaDefender.Core.Entities;

public sealed class Player : Entity
{
    private readonly GameSettings _settings;
    private readonly ArenaBounds _bounds;

    private PlayerIntent _intent = PlayerIntent.Idle;
    private float _fireCooldown;

    public Player(GameSettings settings, ArenaBounds bounds)
        : base(bounds.Centre, settings?.PlayerRadius ?? 1f)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _bounds = bounds;

        MaxHealth = settings.PlayerMaxHealth;
        Health = settings.PlayerMaxHealth;
        Lives = settings.PlayerStartingLives;
        Facing = new Vector2(0f, -1f);

        ResetModifiers();
    }

    public float Health { get; private set; }

    public float MaxHealth { get; }

    /// <summary>Lives left, including the current one.</summary>
    public int Lives { get; private set; }

    public float HealthFraction => MaxHealth <= 0f ? 0f : GameMath.Clamp01(Health / MaxHealth);

    public Vector2 Facing { get; private set; }

    public float InvulnerabilitySeconds { get; private set; }

    public bool IsInvulnerable => InvulnerabilitySeconds > 0f || ShieldCharges > 0;

    public int ShieldCharges { get; private set; }

    public float SpeedMultiplier { get; internal set; }

    public float DamageMultiplier { get; internal set; }

    public float FireRateMultiplier { get; internal set; }

    public int ShotCount { get; private set; } = 1;

    public bool IsDefeated => Lives <= 0;

    /// <summary>Shot count persists across losing a life.</summary>
    public void SetShotCount(int shotCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shotCount, 1);
        ShotCount = shotCount;
    }

    public void Apply(PlayerIntent intent) => _intent = intent;

    public void ResetModifiers()
    {
        SpeedMultiplier = 1f;
        DamageMultiplier = 1f;
        FireRateMultiplier = 1f;
    }

    /// <summary>Invulnerability or a shield charge can absorb the hit entirely; true only when health actually dropped.</summary>
    public bool TakeDamage(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (InvulnerabilitySeconds > 0f)
        {
            return false;
        }

        if (ShieldCharges > 0)
        {
            ShieldCharges--;
            InvulnerabilitySeconds = 0.4f;
            return false;
        }

        Health = MathF.Max(0f, Health - amount);

        if (Health <= 0f)
        {
            LoseLife();
        }
        else
        {
            // A hit buys a moment of immunity, or touching an enemy would empty a life instantly.
            InvulnerabilitySeconds = MathF.Max(InvulnerabilitySeconds, _settings.PlayerHitInvulnerability);
        }

        return true;
    }

    public float Heal(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        float before = Health;
        Health = MathF.Min(MaxHealth, Health + amount);
        return Health - before;
    }

    /// <summary>Lives are capped at what the run started with.</summary>
    public bool GrantExtraLife()
    {
        if (Lives >= _settings.PlayerStartingLives)
        {
            return false;
        }

        Lives++;
        return true;
    }

    public void GrantShield(int charges)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(charges);
        ShieldCharges = Math.Max(ShieldCharges, charges);
    }

    public float CurrentFireInterval =>
        _settings.PlayerFireInterval / MathF.Max(0.01f, FireRateMultiplier);

    public float CurrentProjectileDamage => _settings.PlayerProjectileDamage * DamageMultiplier;

    /// <summary>True when the player wants to fire and the cooldown is clear, starting the next cooldown if so.</summary>
    public bool TryConsumeShot()
    {
        if (!_intent.FirePrimary || _fireCooldown > 0f || Facing == Vector2.Zero)
        {
            return false;
        }

        _fireCooldown = CurrentFireInterval;
        return true;
    }

    /// <inheritdoc />
    protected override void OnUpdate(float deltaSeconds)
    {
        _fireCooldown = MathF.Max(0f, _fireCooldown - deltaSeconds);
        InvulnerabilitySeconds = MathF.Max(0f, InvulnerabilitySeconds - deltaSeconds);

        Velocity = _intent.MoveDirection * _settings.PlayerSpeed * SpeedMultiplier;
        Integrate(deltaSeconds);
        Position = _bounds.Clamp(Position, Radius);

        UpdateFacing(deltaSeconds);
    }

    private void UpdateFacing(float deltaSeconds)
    {
        Vector2 desired = _intent.AimDirection != Vector2.Zero
            ? _intent.AimDirection
            : _intent.MoveDirection;

        if (desired == Vector2.Zero)
        {
            return;
        }

        float blend = GameMath.Clamp01(_settings.PlayerTurnSharpness * deltaSeconds);
        float angle = GameMath.LerpAngle(GameMath.ToAngle(Facing), GameMath.ToAngle(desired), blend);
        Facing = GameMath.FromAngle(angle);
    }

    private void LoseLife()
    {
        Lives--;

        if (Lives > 0)
        {
            Health = MaxHealth;
            Position = _bounds.Centre;
            Velocity = Vector2.Zero;
            ShieldCharges = 0;
            InvulnerabilitySeconds = _settings.PlayerRespawnInvulnerability;
            ResetModifiers();
        }
        else
        {
            Deactivate();
        }
    }
}
