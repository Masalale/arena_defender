using System.Numerics;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Entities.Enemies;

    /// <summary>The degenerate case: player exactly behind the brute, where the cross product is zero.</summary>
public class BruteAntiparallelTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void Steer_WithPlayerExactlyBehind_BeginsTurningRatherThanStalling()
    {
        var brute = new BruteEnemy(new Vector2(500f, 500f));
        var actions = new RecordingEnemyActions();

        // Directly behind means the direction to the player is the exact negation of the facing,
        // so the cross product between them is zero.
        Vector2 playerPosition = brute.Position - (brute.Facing * 200f);
        Assert.Equal(-1f, GameMath.Dot(brute.Facing, GameMath.DirectionTo(brute.Position, playerPosition)), 1e-3f);

        Vector2 facingBefore = brute.Facing;
        brute.Advance(playerPosition, actions, Frame);

        Assert.NotEqual(GameMath.ToAngle(facingBefore), GameMath.ToAngle(brute.Facing), 4);
    }
}
