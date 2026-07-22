using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities;

public interface IEnemyActions
{
    void FireProjectile(Vector2 origin, Vector2 direction, float speed, float damage);
}

public abstract class Enemy : Entity
{
    private readonly float _baseContactDamage;

    private Vector2 _playerPosition;
    private IEnemyActions? _actions;

    protected Enemy(Vector2 position, float radius, float health, float speed, float contactDamage, int scoreValue)
        : base(position, radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(health);
        ArgumentOutOfRangeException.ThrowIfNegative(speed);
        ArgumentOutOfRangeException.ThrowIfNegative(contactDamage);
        ArgumentOutOfRangeException.ThrowIfNegative(scoreValue);

        MaxHealth = health;
        Health = health;
        BaseSpeed = speed;
        _baseContactDamage = contactDamage;
        ScoreValue = scoreValue;
        Facing = new Vector2(0f, 1f);
    }

    public float Health { get; private set; }

    public float MaxHealth { get; }

    /// <summary>Move speed after any difficulty scaling from spawn.</summary>
    public float BaseSpeed { get; private set; }

    public float DamageScale { get; private set; } = 1f;

    public float ContactDamage => _baseContactDamage * DamageScale;

    public float AttackSpeedScale { get; private set; } = 1f;

    public int ScoreValue { get; }

    public Vector2 Facing { get; protected set; }

    public float HealthFraction => MaxHealth <= 0f ? 0f : GameMath.Clamp01(Health / MaxHealth);

    /// <summary>Seconds since last hit, drives the flash.</summary>
    public float SecondsSinceHit { get; private set; } = float.MaxValue;

    protected Vector2 PlayerPosition => _playerPosition;

    public void ApplyDifficultyScale(float speedScale, float damageScale, float attackSpeedScale = 1f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speedScale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damageScale);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attackSpeedScale);

        BaseSpeed *= speedScale;
        DamageScale = damageScale;
        AttackSpeedScale = attackSpeedScale;
    }

    public void Advance(Vector2 playerPosition, IEnemyActions actions, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(actions);

        _playerPosition = playerPosition;
        _actions = actions;

        Update(deltaSeconds);
    }

    public bool TakeDamage(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (!IsActive)
        {
            return false;
        }

        Health = MathF.Max(0f, Health - amount);
        SecondsSinceHit = 0f;

        if (Health > 0f)
        {
            return false;
        }

        Deactivate();
        return true;
    }

    /// <inheritdoc />
    protected sealed override void OnUpdate(float deltaSeconds)
    {
        if (SecondsSinceHit < float.MaxValue)
        {
            SecondsSinceHit += deltaSeconds;
        }

        Steer(deltaSeconds);
        Integrate(deltaSeconds);
    }

    protected abstract void Steer(float deltaSeconds);

    protected void FireProjectile(Vector2 direction, float speed, float damage) =>
        _actions?.FireProjectile(Position, direction, speed, damage);
}
