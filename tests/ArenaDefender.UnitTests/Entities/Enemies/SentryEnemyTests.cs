using System.Numerics;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.UnitTests.TestDoubles;

namespace ArenaDefender.UnitTests.Entities.Enemies;

    /// <summary>
    /// A sentry only shoots when the player is inside its vision cone and range,
    /// so standing behind it is safe.
    /// </summary>
public class SentryEnemyTests
{
    private const float Tolerance = 1e-3f;
    private const float Frame = 1f / 60f;

    private static void Run(SentryEnemy sentry, Vector2 player, RecordingEnemyActions actions, float seconds)
    {
        int frames = (int)(seconds / Frame);

        for (int frame = 0; frame < frames; frame++)
        {
            sentry.Advance(player, actions, Frame);
        }
    }

    [Fact]
    public void Steer_PlayerInsideTheConeAndRange_EventuallyFires()
    {
        SentryEnemy sentry = new(new Vector2(0f, 0f));
        RecordingEnemyActions actions = new();

        Run(sentry, new Vector2(300f, 0f), actions, 5f);

        Assert.True(actions.ShotCount > 0);
    }

    [Fact]
    public void Steer_PlayerDirectlyBehindButWellInRange_DoesNotFire()
    {
        // Range passes here, so only the dot product can reject the shot, which is the point.
        SentryEnemy sentry = new(new Vector2(0f, 0f));
        RecordingEnemyActions actions = new();

        // Give it a distant target first so the cooldown ticks down without a single shot.
        Run(sentry, new Vector2(0f, 3000f), actions, 2.2f);
        Assert.Equal(0, actions.ShotCount);

        Vector2 behind = sentry.Position - (sentry.Facing * 200f);
        Vector2 toPlayer = GameMath.DirectionTo(sentry.Position, behind);

        // A dot product of -1 means the player is exactly opposite the facing direction.
        Assert.Equal(-1f, GameMath.Dot(GameMath.SafeNormalize(sentry.Facing), toPlayer), Tolerance);
        Assert.True(GameMath.Distance(sentry.Position, behind) < SentryEnemy.ConeRange);

        sentry.Advance(behind, actions, 0.01f);

        Assert.False(sentry.HasTargetInSight);
        Assert.Equal(0, actions.ShotCount);
    }
}
