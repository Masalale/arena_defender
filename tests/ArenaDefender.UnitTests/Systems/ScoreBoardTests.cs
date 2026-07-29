using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.UnitTests.Systems;

    /// <summary>Scoring: the points and combo a kill awards.</summary>
public class ScoreBoardTests
{
    private const float Tolerance = 1e-3f;

    private static GameSettings Settings() => new()
    {
        ComboWindowSeconds = 2f,
        MaxComboMultiplier = 3f
    };

    [Fact]
    public void AwardKill_AddsPointsAndCountsTheKill()
    {
        ScoreBoard board = new(Settings());

        int awarded = board.AwardKill(100);

        Assert.True(awarded > 0);
        Assert.Equal(awarded, board.Score);
        Assert.Equal(1, board.TotalKills);
        Assert.Equal(1, board.ComboCount);
    }
}
