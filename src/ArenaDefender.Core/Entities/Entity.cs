using System;
using System.Numerics;

namespace ArenaDefender.Core.Entities;

/// <summary>Anything that sits somewhere in the arena and can collide.</summary>
public abstract class Entity
{
    private float _radius;

    protected Entity(Vector2 position, float radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);

        Position = position;
        _radius = radius;
        IsActive = true;
    }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public float Radius
    {
        get => _radius;
        protected set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _radius = value;
        }
    }

    public bool IsActive { get; private set; }

    public float Age { get; private set; }

    public void Update(float deltaSeconds)
    {
        if (!IsActive || deltaSeconds <= 0f)
        {
            return;
        }

        Age += deltaSeconds;
        OnUpdate(deltaSeconds);
    }

    public void Deactivate() => IsActive = false;

    protected abstract void OnUpdate(float deltaSeconds);

    protected void Integrate(float deltaSeconds) => Position += Velocity * deltaSeconds;
}
