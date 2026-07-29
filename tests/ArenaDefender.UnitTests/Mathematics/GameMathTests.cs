using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.UnitTests.Mathematics;

public class GameMathTests
{
    private const float Tolerance = 1e-4f;

    [Theory]
    [InlineData(0f, 10f, 0f, 0f)]
    [InlineData(0f, 10f, 0.5f, 5f)]
    [InlineData(0f, 10f, 1f, 10f)]
    [InlineData(20f, 40f, 0.25f, 25f)]
    public void Lerp_ReturnsProportionalValue(float from, float to, float t, float expected)
    {
        Assert.Equal(expected, GameMath.Lerp(from, to, t), Tolerance);
    }

    [Fact]
    public void Damp_IsIndependentOfFrameRate()
    {
        float atSixtyHz = 0f;
        for (int frame = 0; frame < 60; frame++)
        {
            atSixtyHz = GameMath.Damp(atSixtyHz, 100f, 4f, 1f / 60f);
        }

        float atThirtyHz = 0f;
        for (int frame = 0; frame < 30; frame++)
        {
            atThirtyHz = GameMath.Damp(atThirtyHz, 100f, 4f, 1f / 30f);
        }

        // One second of simulated time either way must land in the same place.
        Assert.Equal(atSixtyHz, atThirtyHz, 0.01f);
    }

    [Fact]
    public void LerpAngle_TakesShorterArcAcrossTheWrapPoint()
    {
        // From 170 degrees to -170 degrees is a 20 degree step, not a 340 degree sweep.
        float from = 170f * MathF.PI / 180f;
        float to = -170f * MathF.PI / 180f;

        Assert.Equal(MathF.PI, MathF.Abs(GameMath.LerpAngle(from, to, 0.5f)), 1e-3f);
    }

    [Fact]
    public void Distance_MatchesPythagoreanResult()
    {
        Assert.Equal(5f, GameMath.Distance(Vector2.Zero, new Vector2(3f, 4f)), Tolerance);
    }

    [Fact]
    public void IsWithinRange_WithNegativeRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GameMath.IsWithinRange(Vector2.Zero, Vector2.One, -1f));
    }

    [Theory]
    [InlineData(1f, 0f, 1f, 0f, 1f)]   // identical headings
    [InlineData(1f, 0f, 0f, 1f, 0f)]   // perpendicular
    [InlineData(1f, 0f, -1f, 0f, -1f)] // opposed
    public void Dot_OnUnitVectors_ReturnsCosineOfAngle(
        float ax, float ay, float bx, float by, float expected)
    {
        Assert.Equal(expected, GameMath.Dot(new Vector2(ax, ay), new Vector2(bx, by)), Tolerance);
    }

    [Fact]
    public void Cross_SignIndicatesWhichSideTheSecondVectorLiesOn()
    {
        Vector2 facing = new(1f, 0f);

        Assert.True(GameMath.Cross(facing, new Vector2(0f, 1f)) > 0f);
        Assert.True(GameMath.Cross(facing, new Vector2(0f, -1f)) < 0f);
        Assert.Equal(0f, GameMath.Cross(facing, new Vector2(2f, 0f)), Tolerance);
    }

    [Fact]
    public void IsInFieldOfView_AcceptsTargetsInsideTheConeAndRejectsThoseOutside()
    {
        Vector2 facing = new(1f, 0f);
        float halfAngle = MathF.PI / 4f; // a 90 degree cone

        Assert.True(GameMath.IsInFieldOfView(Vector2.Zero, facing, new Vector2(10f, 0f), halfAngle));
        Assert.True(GameMath.IsInFieldOfView(Vector2.Zero, facing, new Vector2(10f, 9f), halfAngle));
        Assert.False(GameMath.IsInFieldOfView(Vector2.Zero, facing, new Vector2(10f, 30f), halfAngle));
        Assert.False(GameMath.IsInFieldOfView(Vector2.Zero, facing, new Vector2(-10f, 0f), halfAngle));
    }

    [Fact]
    public void TurnDirection_ReportsTheShorterWayToRotate()
    {
        Vector2 facing = new(1f, 0f);

        Assert.Equal(1, GameMath.TurnDirection(facing, new Vector2(0f, 1f)));
        Assert.Equal(-1, GameMath.TurnDirection(facing, new Vector2(0f, -1f)));
        Assert.Equal(0, GameMath.TurnDirection(facing, new Vector2(5f, 0f)));
    }
}
