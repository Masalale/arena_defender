using System;
using System.Collections.Generic;
using System.Numerics;
using ArenaDefender.Core.Configuration;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Input;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.Core.Simulation;

public enum GameState
{
    Menu,

    Playing,

    GameOver
}

public readonly record struct WaveReward(int Wave, int Points, bool GrantedExtraShot, bool GrantedExtraLife);

public sealed class GameWorld : IEnemyActions
{
    private readonly GameSettings _settings;
    private readonly IRandomSource _random;
    private readonly DifficultyCurve _difficulty;
    private readonly ScoreBoard _scoreBoard;
    private readonly PowerUpSystem _powerUps;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly List<PowerUp> _pickups = new();

    private WaveDirector _director;
    private int _highestWaveReached;

    public GameWorld()
        : this(new GameSettings(), new SystemRandomSource())
    {
    }

    public GameWorld(GameSettings settings, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);

        _settings = settings;
        _random = random;

        Bounds = new ArenaBounds(settings.ArenaWidth, settings.ArenaHeight);
        _difficulty = new DifficultyCurve(settings);
        _scoreBoard = new ScoreBoard(settings);
        _powerUps = new PowerUpSystem(settings);
        _director = new WaveDirector(settings, _difficulty, random, Bounds);

        Player = new Player(settings, Bounds);
        State = GameState.Menu;
    }

    /// <summary>The player took damage that actually removed health.</summary>
    public event Action<Vector2>? PlayerDamaged;

    public event Action? PlayerFired;

    public event Action? EnemyDestroyed;

    public event Action? PowerUpCollected;

    public event Action<WaveReward>? WaveReached;

    public ArenaBounds Bounds { get; }

    public GameState State { get; private set; }

    public Player Player { get; private set; }

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    public IReadOnlyList<PowerUp> Pickups => _pickups;

    public ScoreBoard Score => _scoreBoard;

    public PowerUpSystem PowerUps => _powerUps;

    public int WaveNumber => _director.CurrentWave;

    public void StartNewRun()
    {
        _enemies.Clear();
        _projectiles.Clear();
        _pickups.Clear();

        _scoreBoard.Reset();
        _highestWaveReached = 1;

        Player = new Player(_settings, Bounds);
        _powerUps.Clear(Player);
        _director = new WaveDirector(_settings, _difficulty, _random, Bounds);

        State = GameState.Playing;
    }

    public void ReturnToMenu() => State = GameState.Menu;

    public void Update(float deltaSeconds, PlayerIntent intent)
    {
        // Written as a positive test so NaN is rejected instead of sneaking through.
        if (State != GameState.Playing || !(deltaSeconds > 0f))
        {
            return;
        }

        // Without a clamp, a frame stall moves everything so far that fast projectiles skip
        // clean through an enemy without ever overlapping it.
        float step = MathF.Min(deltaSeconds, 0.25f);

        UpdatePlayer(step, intent);
        UpdateSpawning(step);
        UpdateEnemies(step);
        UpdateProjectiles(step);
        UpdatePickups(step);

        ResolveCollisions();

        _scoreBoard.Update(step);
        RemoveInactive();

        if (Player.IsDefeated)
        {
            State = GameState.GameOver;
        }
    }

    void IEnemyActions.FireProjectile(Vector2 origin, Vector2 direction, float speed, float damage)
    {
        _projectiles.Add(new Projectile(
            origin,
            direction,
            speed,
            damage,
            _settings.ProjectileRadius,
            _settings.ProjectileLifetime,
            ProjectileOwner.Enemy));
    }

    private void UpdatePlayer(float step, PlayerIntent intent)
    {
        Player.SetShotCount(ShotCountForWave(WaveNumber));
        _powerUps.Duration = _difficulty.GetPowerUpDuration(WaveNumber);

        Player.Apply(intent);
        Player.Update(step);
        _powerUps.Update(step, Player);

        if (Player.TryConsumeShot())
        {
            FirePlayerShot();
        }
    }

    // Called after spawning, once the director has already advanced the wave.
    private void SettleWaveChange()
    {
        int wave = WaveNumber;

        if (wave <= _highestWaveReached)
        {
            return;
        }

        int cleared = _highestWaveReached;
        _highestWaveReached = wave;

        int points = 0;
        bool grantedLife = false;

        // Milestone waves pay out: a life back if lost, points otherwise.
        if (_settings.MilestoneWaveInterval > 0 && wave % _settings.MilestoneWaveInterval == 0)
        {
            grantedLife = Player.GrantExtraLife();

            if (!grantedLife)
            {
                points += _scoreBoard.AwardBonus(_settings.MilestonePoints);
            }
        }

        bool grantedShot = ShotCountForWave(wave) > ShotCountForWave(cleared);

        WaveReached?.Invoke(new WaveReward(wave, points, grantedShot, grantedLife));
    }

    private int ShotCountForWave(int waveNumber)
    {
        int count = 1;

        foreach (int threshold in _settings.ExtraShotWaves)
        {
            if (waveNumber >= threshold)
            {
                count++;
            }
        }

        return count;
    }

    // Even counts have no centre shot, so the offsets start either side of zero.
    private void FirePlayerShot()
    {
        Vector2 origin = Player.Position + (Player.Facing * Player.Radius);
        float aim = GameMath.ToAngle(Player.Facing);
        float start = -(Player.ShotCount - 1) * 0.5f * _settings.ShotSpreadRadians;

        for (int index = 0; index < Player.ShotCount; index++)
        {
            _projectiles.Add(new Projectile(
                origin,
                GameMath.FromAngle(aim + start + (index * _settings.ShotSpreadRadians)),
                _settings.PlayerProjectileSpeed,
                Player.CurrentProjectileDamage,
                _settings.ProjectileRadius,
                _settings.ProjectileLifetime,
                ProjectileOwner.Player));
        }

        PlayerFired?.Invoke();
    }

    private void UpdateSpawning(float step)
    {
        Enemy? spawned = _director.Update(step, _enemies.Count);

        if (spawned is not null)
        {
            _enemies.Add(spawned);
        }

        SettleWaveChange();
    }

    private void UpdateEnemies(float step)
    {
        foreach (Enemy enemy in _enemies)
        {
            enemy.Advance(Player.Position, this, step);
        }
    }

    private void UpdateProjectiles(float step)
    {
        foreach (Projectile projectile in _projectiles)
        {
            projectile.Update(step);

            // Wide enough to hit a sentry holding its standoff outside the arena. A tighter margin
            // made those sentries unkillable, and a wave ends only when the arena is empty.
            if (Bounds.IsOutside(projectile.Position, 400f))
            {
                projectile.Deactivate();
            }
        }
    }

    private void UpdatePickups(float step)
    {
        foreach (PowerUp pickup in _pickups)
        {
            pickup.Update(step);
            pickup.AttractTowards(Player.Position, _settings.PowerUpMagnetRange, step);
        }
    }

    private void ResolveCollisions()
    {
        ResolveProjectileHits();
        ResolveContactDamage();
        ResolvePickups();
    }

    private void ResolveProjectileHits()
    {
        foreach (Projectile projectile in _projectiles)
        {
            if (!projectile.IsActive)
            {
                continue;
            }

            if (projectile.Owner == ProjectileOwner.Player)
            {
                Enemy? hit = CollisionResolver.FindFirstOverlap(projectile, _enemies);

                if (hit is null)
                {
                    continue;
                }

                projectile.Deactivate();

                if (hit.TakeDamage(projectile.Damage))
                {
                    RewardKill(hit);
                }
            }
            else if (CollisionResolver.Overlaps(projectile, Player))
            {
                projectile.Deactivate();
                DamagePlayer(projectile.Damage, projectile.Position);
            }
        }
    }

    private void ResolveContactDamage()
    {
        foreach (Enemy enemy in _enemies)
        {
            if (!CollisionResolver.Overlaps(enemy, Player))
            {
                continue;
            }

            DamagePlayer(enemy.ContactDamage, enemy.Position);

            // A chaser spends itself on the hit. No score, the player did not kill it, but the
            // death still fires so the explosion and sound play.
            if (enemy is ChaserEnemy)
            {
                enemy.Deactivate();
                EnemyDestroyed?.Invoke();
            }
        }
    }

    private void ResolvePickups()
    {
        foreach (PowerUp pickup in _pickups)
        {
            if (!CollisionResolver.Overlaps(pickup, Player))
            {
                continue;
            }

            pickup.Deactivate();
            _powerUps.Collect(pickup.Kind, Player);
            _scoreBoard.AwardPickup();
            PowerUpCollected?.Invoke();
        }
    }

    private void RewardKill(Enemy enemy)
    {
        _scoreBoard.AwardKill(enemy.ScoreValue);
        EnemyDestroyed?.Invoke();

        if (_random.Chance(_settings.PowerUpDropChance))
        {
            _pickups.Add(new PowerUp(
                enemy.Position,
                ChooseDrop(),
                _settings.PowerUpRadius,
                _settings.PowerUpLifetime));
        }
    }

    /// <summary>Picks the drop an enemy leaves, leaning toward repairs when the player is hurt.</summary>
    private PowerUpKind ChooseDrop()
    {
        if (Player.HealthFraction < 0.45f && _random.Chance(0.55f))
        {
            return PowerUpKind.Repair;
        }

        return _random.NextInt(0, 5) switch
        {
            0 => PowerUpKind.Repair,
            1 => PowerUpKind.RapidFire,
            2 => PowerUpKind.DoubleDamage,
            3 => PowerUpKind.BoostyBoost,
            _ => PowerUpKind.Shield
        };
    }

    /// <summary>Hits the player, emptying the arena if a life was lost.</summary>
    private void DamagePlayer(float amount, Vector2 source)
    {
        int livesBefore = Player.Lives;
        bool wasHurt = Player.TakeDamage(amount);

        if (!wasHurt)
        {
            return;
        }

        _scoreBoard.BreakCombo();
        PlayerDamaged?.Invoke(source);

        if (Player.Lives == livesBefore)
        {
            return;
        }

        // A life lost, so the arena empties to give the player room. The wave restarts, or the
        // empty arena would count as cleared. Deactivated rather than removed because the lists
        // are mid-enumeration; RemoveInactive sweeps them at end of frame.
        _director.RestartWave();
        _powerUps.Clear(Player);

        foreach (Enemy enemy in _enemies)
        {
            enemy.Deactivate();
        }

        foreach (Projectile projectile in _projectiles)
        {
            projectile.Deactivate();
        }
    }

    private void RemoveInactive()
    {
        _enemies.RemoveAll(enemy => !enemy.IsActive);
        _projectiles.RemoveAll(projectile => !projectile.IsActive);
        _pickups.RemoveAll(pickup => !pickup.IsActive);
    }
}
