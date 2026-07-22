using System.Collections.Generic;

namespace ArenaDefender.Core.Configuration;

/// <summary>
/// Every tunable number in the simulation. Kept together so a test can build an extreme
/// config without touching the defaults.
/// </summary>
public sealed class GameSettings
{
    public float ArenaWidth { get; init; } = 1280f;

    public float ArenaHeight { get; init; } = 720f;

    public float PlayerRadius { get; init; } = 16f;

    public float PlayerMaxHealth { get; init; } = 100f;

    public int PlayerStartingLives { get; init; } = 3;

    public float PlayerSpeed { get; init; } = 260f;

    public float PlayerRespawnInvulnerability { get; init; } = 2f;

    public float PlayerHitInvulnerability { get; init; } = 0.6f;

    public float PlayerTurnSharpness { get; init; } = 14f;

    public float PlayerFireInterval { get; init; } = 0.22f;

    public float PlayerProjectileDamage { get; init; } = 25f;

    public float PlayerProjectileSpeed { get; init; } = 620f;

    public float ProjectileRadius { get; init; } = 5f;

    public float ProjectileLifetime { get; init; } = 2.5f;

    public int EnemiesInFirstWave { get; init; } = 8;

    public int EnemiesAddedPerWave { get; init; } = 2;

    public float InitialSpawnInterval { get; init; } = 1.6f;

    public float MinimumSpawnInterval { get; init; } = 0.7f;

    public int WavesToFastestSpawn { get; init; } = 8;

    // In an endless game the spawn rate has to keep climbing or a good player never dies. Density is the fairest dial because killing and dodging always answer it.
    public float SpawnDecayPerWave { get; init; } = 0.97f;

    /// <summary>Hard floor on the spawn interval. A sanity guard more than a balance figure.</summary>
    public float SpawnIntervalFloor { get; init; } = 0.1f;

    public float AttackSpeedGrowthPerWave { get; init; } = 0.02f;

    public float WaveBreatherSeconds { get; init; } = 2f;

    public float MaxDamageScale { get; init; } = 2f;

    public int WavesToMaxDamage { get; init; } = 20;

    public float MaxSpeedScale { get; init; } = 1.15f;

    public int WavesToMaxSpeed { get; init; } = 12;

    public float PowerUpDropChance { get; init; } = 0.18f;

    public float PowerUpRadius { get; init; } = 14f;

    public float PowerUpLifetime { get; init; } = 12f;

    public float PowerUpDuration { get; init; } = 8f;

    // Grows with the wave so power ups keep pace, but capped so none of them become permanent.
    public float MaxPowerUpDuration { get; init; } = 12f;

    public int WavesToMaxPowerUpDuration { get; init; } = 25;

    public float PowerUpMagnetRange { get; init; } = 90f;

    /// <summary>
    /// Waves at a multiple of this pay a milestone: a life back if one was lost, or
    /// <see cref="MilestonePoints"/> otherwise.
    /// </summary>
    public int MilestoneWaveInterval { get; init; } = 7;

    /// <summary>Milestone payout when already at full lives.</summary>
    public int MilestonePoints { get; init; } = 2500;

    /// <summary>Waves where the player permanently gains one more projectile per shot.</summary>
    public IReadOnlyList<int> ExtraShotWaves { get; init; } = new[] { 8, 17, 26 };

    public float ShotSpreadRadians { get; init; } = 0.14f;

    public float ComboWindowSeconds { get; init; } = 2.5f;

    public float MaxComboMultiplier { get; init; } = 5f;

    public int PowerUpPickupPoints { get; init; } = 50;
}
