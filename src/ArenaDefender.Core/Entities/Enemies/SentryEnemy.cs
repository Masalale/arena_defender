using System;
using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities.Enemies;

/// <summary>Stays at range and only fires at what is inside its vision cone, a dot product test.</summary>
public sealed class SentryEnemy : Enemy
{
    private const float VisionHalfAngle = 0.45f;
    private const float VisionRange = 420f;
    private const float SweepRateRadians = 0.9f;

    // Small on purpose: the cone still decides whether the sentry may fire at all.
    private const float AimLeanTowardsPlayer = 0.35f;

    private const float PreferredDistance = 300f;
    private const float FireInterval = 1.35f;
    private const float ShotSpeed = 340f;
    private const float ShotDamage = 12f;

    private float _fireCooldown = FireInterval * 0.5f;
    private int _sweepDirection = 1;
    private float _sweepOffset;

    public SentryEnemy(Vector2 position)
        : base(position, radius: 17f, health: 60f, speed: 62f, contactDamage: 10f, scoreValue: 250)
    {
    }

    public static float ConeHalfAngle => VisionHalfAngle;

    public static float ConeRange => VisionRange;

    public bool HasTargetInSight { get; private set; }

    /// <inheritdoc />
    protected override void Steer(float deltaSeconds)
    {
        _fireCooldown = MathF.Max(0f, _fireCooldown - deltaSeconds);

        Vector2 toPlayer = GameMath.DirectionTo(Position, PlayerPosition);

        if (toPlayer == Vector2.Zero)
        {
            Velocity = Vector2.Zero;
            HasTargetInSight = false;
            return;
        }

        UpdateAim(toPlayer, deltaSeconds);
        HoldPreferredDistance(toPlayer);

        HasTargetInSight =
            GameMath.IsWithinRange(Position, PlayerPosition, VisionRange)
            && GameMath.IsInFieldOfView(Position, Facing, PlayerPosition, VisionHalfAngle);

        if (HasTargetInSight && _fireCooldown <= 0f)
        {
            _fireCooldown = FireInterval / AttackSpeedScale;

            // Fired mostly along the swept facing, but leaned a little towards the player so a shot
            // that was allowed by the cone test is not thrown away by the sweep's offset.
            Vector2 aim = GameMath.SafeNormalize(Facing + ((toPlayer - Facing) * AimLeanTowardsPlayer));
            FireProjectile(aim == Vector2.Zero ? Facing : aim, ShotSpeed, ShotDamage * DamageScale);
        }
    }

    /// <summary>Sweeps the cone either side of the bearing to the player instead of locking on.</summary>
    private void UpdateAim(Vector2 toPlayer, float deltaSeconds)
    {
        _sweepOffset += _sweepDirection * SweepRateRadians * deltaSeconds;

        if (MathF.Abs(_sweepOffset) >= VisionHalfAngle * 2.2f)
        {
            _sweepDirection = -_sweepDirection;
            _sweepOffset = Math.Clamp(_sweepOffset, -VisionHalfAngle * 2.2f, VisionHalfAngle * 2.2f);
        }

        float desired = GameMath.ToAngle(toPlayer) + _sweepOffset;
        Facing = GameMath.FromAngle(GameMath.LerpAngle(GameMath.ToAngle(Facing), desired, GameMath.Clamp01(3f * deltaSeconds)));
    }

    private void HoldPreferredDistance(Vector2 toPlayer)
    {
        float distance = GameMath.Distance(Position, PlayerPosition);
        float error = distance - PreferredDistance;

        if (MathF.Abs(error) < 30f)
        {
            // Comfortable range, so strafe sideways instead. The perpendicular of a vector is a
            // rotation by ninety degrees, which in two dimensions is a swap and a sign flip.
            Velocity = new Vector2(-toPlayer.Y, toPlayer.X) * BaseSpeed * 0.6f;
            return;
        }

        Velocity = toPlayer * BaseSpeed * MathF.Sign(error);
    }
}
