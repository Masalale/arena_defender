using System;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Systems;

/// <summary>Turns a wave number into the spawn counts and scaling that make the game harder.</summary>
public sealed class DifficultyCurve
{
    private readonly GameSettings _settings;

    public DifficultyCurve(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    public static float GetProgress(int waveNumber, int wavesToMax)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waveNumber, 1);

        if (wavesToMax <= 1)
        {
            return 1f;
        }

        return GameMath.Clamp01((waveNumber - 1f) / (wavesToMax - 1f));
    }

    public int GetEnemyCount(int waveNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waveNumber, 1);
        return _settings.EnemiesInFirstWave + ((waveNumber - 1) * _settings.EnemiesAddedPerWave);
    }

    public float GetSpawnInterval(int waveNumber)
    {
        // Ramp first, then percent decay, so the spawn rate keeps climbing after the ramp tops out.
        float ramped = GameMath.Lerp(
            _settings.InitialSpawnInterval,
            _settings.MinimumSpawnInterval,
            GetProgress(waveNumber, _settings.WavesToFastestSpawn));

        int wavesPastRamp = Math.Max(0, waveNumber - _settings.WavesToFastestSpawn);

        if (wavesPastRamp == 0)
        {
            return ramped;
        }

        float decayed = ramped * MathF.Pow(_settings.SpawnDecayPerWave, wavesPastRamp);

        return MathF.Max(_settings.SpawnIntervalFloor, decayed);
    }

    public float GetAttackSpeedScale(int waveNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waveNumber, 1);

        int wavesPastRamp = Math.Max(0, waveNumber - _settings.WavesToFastestSpawn);

        return 1f + (wavesPastRamp * _settings.AttackSpeedGrowthPerWave);
    }

    public float GetSpeedScale(int waveNumber) => GameMath.Lerp(
        1f, _settings.MaxSpeedScale, GetProgress(waveNumber, _settings.WavesToMaxSpeed));

    public float GetDamageScale(int waveNumber) => GameMath.Lerp(
        1f, _settings.MaxDamageScale, GetProgress(waveNumber, _settings.WavesToMaxDamage));

    public float GetPowerUpDuration(int waveNumber) => GameMath.Lerp(
        _settings.PowerUpDuration,
        _settings.MaxPowerUpDuration,
        GetProgress(waveNumber, _settings.WavesToMaxPowerUpDuration));

    public float GetVarietyProgress(int waveNumber) =>
        GetProgress(waveNumber, _settings.WavesToFastestSpawn);
}
