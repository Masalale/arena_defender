using System;
using System.Numerics;

namespace ArenaDefender.Core.Mathematics;

public static class GameMath
{
    public const float Epsilon = 1e-6f;

    public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    public static float Lerp(float from, float to, float t) => from + (to - from) * Clamp01(t);

    // No clamping, so it can overshoot the ends.
    private static float LerpUnclamped(float from, float to, float t) => from + (to - from) * t;

    /// <summary>Frame-rate independent catch-up towards a target value.</summary>
    public static float Damp(float current, float target, float sharpness, float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
        {
            return current;
        }

        // 1 - e^(-sharpness * dt) only cares about real time, so 30 fps and 144 fps converge identically.
        float t = 1f - MathF.Exp(-MathF.Max(0f, sharpness) * deltaSeconds);
        return LerpUnclamped(current, target, t);
    }

    /// <summary>Damp applied per axis, so it works on positions too.</summary>
    public static Vector2 Damp(Vector2 current, Vector2 target, float sharpness, float deltaSeconds) =>
        new(Damp(current.X, target.X, sharpness, deltaSeconds),
            Damp(current.Y, target.Y, sharpness, deltaSeconds));

    /// <summary>Blends two angles in radians, taking the shorter way around.</summary>
    public static float LerpAngle(float fromRadians, float toRadians, float t)
    {
        // Normalise the difference first so a turn does not take the long way round the wrap point.
        float difference = NormalizeAngle(toRadians - fromRadians);
        return fromRadians + difference * Clamp01(t);
    }

    // Wraps an angle into -pi..pi.
    private static float NormalizeAngle(float radians)
    {
        float wrapped = (radians + MathF.PI) % MathF.Tau;

        if (wrapped < 0f)
        {
            wrapped += MathF.Tau;
        }

        return wrapped - MathF.PI;
    }

    public static float Distance(Vector2 a, Vector2 b) => (b - a).Length();

    // Squared distance, so range checks can skip the square root.
    private static float DistanceSquared(Vector2 a, Vector2 b) => (b - a).LengthSquared();

    public static bool IsWithinRange(Vector2 a, Vector2 b, float range)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(range);
        return DistanceSquared(a, b) <= range * range;
    }

    public static Vector2 DirectionTo(Vector2 origin, Vector2 target) => SafeNormalize(target - origin);

    /// <summary>Unit vector for the input, or zero for a (near) zero input so nothing ends up NaN.</summary>
    public static Vector2 SafeNormalize(Vector2 value)
    {
        float lengthSquared = value.LengthSquared();

        if (lengthSquared < Epsilon * Epsilon)
        {
            return Vector2.Zero;
        }

        return value / MathF.Sqrt(lengthSquared);
    }

    public static float Dot(Vector2 a, Vector2 b) => (a.X * b.X) + (a.Y * b.Y);

    /// <summary>2D cross product. The sign says which side of a the other vector lies on.</summary>
    public static float Cross(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);

    public static bool IsInFieldOfView(
        Vector2 viewerPosition,
        Vector2 viewerFacing,
        Vector2 target,
        float halfAngleRadians)
    {
        Vector2 facing = SafeNormalize(viewerFacing);
        Vector2 toTarget = SafeNormalize(target - viewerPosition);

        if (facing == Vector2.Zero || toTarget == Vector2.Zero)
        {
            return false;
        }

        // Cos falls as the angle grows, so a wider cone is just a lower dot product bar.
        return Dot(facing, toTarget) >= MathF.Cos(Math.Clamp(halfAngleRadians, 0f, MathF.PI));
    }

    /// <summary>Turn sign: +1 clockwise, -1 counter clockwise, 0 when already aligned.</summary>
    public static int TurnDirection(Vector2 facing, Vector2 toTarget)
    {
        float cross = Cross(SafeNormalize(facing), SafeNormalize(toTarget));

        if (MathF.Abs(cross) < Epsilon)
        {
            return 0;
        }

        return cross > 0f ? 1 : -1;
    }

    public static float AngleBetween(Vector2 a, Vector2 b)
    {
        Vector2 first = SafeNormalize(a);
        Vector2 second = SafeNormalize(b);

        if (first == Vector2.Zero || second == Vector2.Zero)
        {
            return 0f;
        }

        return MathF.Acos(Math.Clamp(Dot(first, second), -1f, 1f));
    }

    public static Vector2 FromAngle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    public static float ToAngle(Vector2 direction) => MathF.Atan2(direction.Y, direction.X);

    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (MathF.Abs(fromMax - fromMin) < Epsilon)
        {
            throw new ArgumentException("The source range must not be empty.", nameof(fromMax));
        }

        float normalised = Clamp01((value - fromMin) / (fromMax - fromMin));
        return LerpUnclamped(toMin, toMax, normalised);
    }
}
