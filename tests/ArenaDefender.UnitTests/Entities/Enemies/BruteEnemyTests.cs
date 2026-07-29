using System.Numerics;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Entities.Enemies;

    /// <summary>
    /// The sign of the cross product decides which way round the brute turns.
    /// </summary>
public class BruteEnemyTests
{
    private const float Tolerance = 1e-3f;
    private const float MaxTurnRateRadians = 1.5f;

    /// <summary>A brute placed here starts facing along positive X, which makes the maths readable.</summary>
    private static readonly Vector2 SpawnFacingPositiveX = new(-100f, 0f);

    private static Vector2 PlayerAtBearing(float radians, float distance = 500f) =>
        SpawnFacingPositiveX + (GameMath.FromAngle(radians) * distance);

    [Fact]
    public void Steer_CannotTurnInstantly_SoTheFacingOnlyEdgesTowardsThePlayer()
    {
        BruteEnemy brute = new(SpawnFacingPositiveX);
        RecordingEnemyActions actions = new();
        Vector2 player = PlayerAtBearing(MathF.PI / 2f);

        brute.Advance(player, actions, 0.1f);

        // One tenth of a second buys 0.15 radians of rotation, nowhere near the 1.57 required.
        Assert.Equal(MaxTurnRateRadians * 0.1f, GameMath.ToAngle(brute.Facing), Tolerance);
        Assert.True(GameMath.AngleBetween(brute.Facing, new Vector2(0f, 1f)) > 1.4f);
    }

    [Theory]
    [InlineData(0.5f, 1)]
    [InlineData(-0.5f, -1)]
    [InlineData(1.2f, 1)]
    [InlineData(-1.2f, -1)]
    [InlineData(2.9f, 1)]
    [InlineData(-2.9f, -1)]
    public void Steer_RotatesAlongTheShorterArc(float bearingRadians, int expectedSign)
    {
        // Cross product sign between facing and target direction picks the turn, and it
        // always takes the shorter arc.
        BruteEnemy brute = new(SpawnFacingPositiveX);
        RecordingEnemyActions actions = new();

        brute.Advance(PlayerAtBearing(bearingRadians), actions, 0.1f);

        float angle = GameMath.ToAngle(brute.Facing);

        Assert.Equal(expectedSign, MathF.Sign(angle));
        Assert.Equal(MaxTurnRateRadians * 0.1f, MathF.Abs(angle), Tolerance);
    }
}
