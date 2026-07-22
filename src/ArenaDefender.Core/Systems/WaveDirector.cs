using System;
using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Simulation;

namespace ArenaDefender.Core.Systems;

/// <summary>
/// Runs the waves: enemies spawn one at a time, and a wave ends only when they are all dead.
/// </summary>
public sealed class WaveDirector
{
    private readonly GameSettings _settings;
    private readonly DifficultyCurve _difficulty;
    private readonly IRandomSource _random;
    private readonly ArenaBounds _bounds;

    private float _timeUntilNextSpawn;
    private int _remainingToSpawn;

    /// <summary>Starts on wave one and immediately kicks off the first wave.</summary>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public WaveDirector(
        GameSettings settings,
        DifficultyCurve difficulty,
        IRandomSource random,
        ArenaBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(difficulty);
        ArgumentNullException.ThrowIfNull(random);

        _settings = settings;
        _difficulty = difficulty;
        _random = random;
        _bounds = bounds;

        CurrentWave = 1;
        BeginWave();
    }

    /// <summary>Wave currently in progress, counting from one.</summary>
    public int CurrentWave { get; private set; }

    /// <summary>Enemies still to be sent out this wave.</summary>
    public int RemainingToSpawn => _remainingToSpawn;

    /// <summary>Enemies this wave will send out in total.</summary>
    public int WaveSize => _difficulty.GetEnemyCount(CurrentWave);

    /// <summary>Steps the wave forward; returns the next enemy to add, or null when nothing is due.</summary>
    public Enemy? Update(float deltaSeconds, int activeEnemyCount)
    {
        if (deltaSeconds <= 0f)
        {
            return null;
        }

        if (_remainingToSpawn == 0 && activeEnemyCount == 0)
        {
            CurrentWave++;
            BeginWave();
            return null;
        }

        if (_remainingToSpawn == 0)
        {
            return null;
        }

        _timeUntilNextSpawn -= deltaSeconds;

        if (_timeUntilNextSpawn > 0f)
        {
            return null;
        }

        _timeUntilNextSpawn = _difficulty.GetSpawnInterval(CurrentWave);
        _remainingToSpawn--;

        return CreateEnemy(CurrentWave);
    }

    /// <summary>Rebuilds the current wave, so a wiped arena is not read as a cleared one.</summary>
    public void RestartWave()
    {
        BeginWave();
    }

    /// <summary>Builds one enemy of a type that fits the wave's variety progress.</summary>
    public Enemy CreateEnemy(int waveNumber)
    {
        float variety = _difficulty.GetVarietyProgress(waveNumber);
        Vector2 position = ChooseSpawnPoint();

        float sentryChance = 0.32f * GameMath.Remap(variety, 0.12f, 0.7f, 0f, 1f);
        float bruteChance = 0.28f * GameMath.Remap(variety, 0.3f, 1f, 0f, 1f);

        float roll = _random.NextSingle();

        Enemy enemy = roll < bruteChance
            ? new BruteEnemy(position)
            : roll < bruteChance + sentryChance
                ? new SentryEnemy(position)
                : new ChaserEnemy(position);

        enemy.ApplyDifficultyScale(
            _difficulty.GetSpeedScale(waveNumber),
            _difficulty.GetDamageScale(waveNumber),
            _difficulty.GetAttackSpeedScale(waveNumber));

        return enemy;
    }

    private void BeginWave()
    {
        // First spawn comes out immediately; the breather timer owns the pause between waves.
        _remainingToSpawn = _difficulty.GetEnemyCount(CurrentWave);
        _timeUntilNextSpawn = 0f;
    }

    /// <summary>Spawn point just off a random edge, so enemies never pop in on the player.</summary>
    private Vector2 ChooseSpawnPoint()
    {
        const float margin = 48f;

        return _random.NextInt(0, 4) switch
        {
            0 => new Vector2(_random.NextRange(0f, _bounds.Width), -margin),
            1 => new Vector2(_random.NextRange(0f, _bounds.Width), _bounds.Height + margin),
            2 => new Vector2(-margin, _random.NextRange(0f, _bounds.Height)),
            _ => new Vector2(_bounds.Width + margin, _random.NextRange(0f, _bounds.Height))
        };
    }
}
