using System.Numerics;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Entities.Enemies;

    /// <summary>
    /// The chaser is the no frills case: no turn radius and no ranged attack, just a full
    /// speed run at the player until one of them dies.
    /// </summary>
public class ChaserEnemyTests
{
    private const float Tolerance = 1e-3f;

    [Fact]
    public void Steer_ChaserAheadOfThePlayer_HoldsFullSpeedStraightAtIt()
    {
        ChaserEnemy chaser = new(new Vector2(0f, 0f));
        RecordingEnemyActions actions = new();

        chaser.Advance(new Vector2(300f, 0f), actions, 1f);

        // The whole point of the chaser is that it never takes the scenic route.
        Assert.Equal(chaser.BaseSpeed, chaser.Velocity.Length(), Tolerance);
        Assert.Equal(1f, GameMath.Dot(chaser.Facing, new Vector2(1f, 0f)), Tolerance);
    }

    [Fact]
    public void Steer_ChaserHasNoRangedAttack_NeverAsksToFire()
    {
        ChaserEnemy chaser = new(new Vector2(0f, 0f));
        RecordingEnemyActions actions = new();

        chaser.Advance(new Vector2(300f, 0f), actions, 1f);

        Assert.Equal(0, actions.ShotCount);
    }
}
