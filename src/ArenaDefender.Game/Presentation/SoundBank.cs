using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace ArenaDefender.Presentation;

/// <summary>The events the game makes a noise for.</summary>
public enum GameSound
{
    /// <summary>The player let off a volley.</summary>
    Shot,

    EnemyDown,

    /// <summary>The player took a hit that removed health.</summary>
    PlayerHit,

    PickUp,

    /// <summary>A new wave began.</summary>
    Wave
}

/// <summary>Loads and plays the effects and the looping lobby music.</summary>
public sealed class SoundBank : IDisposable
{
    private static readonly string[] Files = { "shot", "enemy_down", "player_hit", "pickup", "wave" };
    private static readonly float[] MinimumGap = { 0.04f, 0.05f, 0.15f, 0.06f, 0.2f };
    private static readonly float[] Volume = { 0.16f, 0.3f, 0.45f, 0.4f, 0.5f };

    private readonly SoundEffect[] _effects = new SoundEffect[Files.Length];
    private readonly float[] _cooldowns = new float[Files.Length];
    private readonly SoundEffectInstance? _music;

    /// <summary>Loads every sound from Assets. Music is optional, effects are not.</summary>
    public SoundBank()
    {
        for (int i = 0; i < Files.Length; i++)
        {
            _effects[i] = Load(Files[i] + ".wav");
        }

        try
        {
            _music = Load("lobby_music.wav").CreateInstance();
            _music.IsLooped = true;
            _music.Volume = 0.35f;
        }
        catch (Exception)
        {
            _music = null;
        }
    }

    /// <summary>Ticks the repeat limiters down.</summary>
    public void Update(float deltaSeconds)
    {
        for (int i = 0; i < _cooldowns.Length; i++)
        {
            _cooldowns[i] = MathF.Max(0f, _cooldowns[i] - deltaSeconds);
        }
    }

    /// <summary>Plays a sound, unless the same one is still on cooldown.</summary>
    public void Play(GameSound sound, float pitch = 0f, float volumeScale = 1f)
    {
        int index = (int)sound;

        if (_cooldowns[index] > 0f)
        {
            return;
        }

        _cooldowns[index] = MinimumGap[index];

        try
        {
            _effects[index].Play(MathF.Min(1f, Volume[index] * volumeScale), pitch, 0f);
        }
        catch (Exception)
        {
            // MonoGame throws once too many instances are playing at once. Dropping one is fine.
        }
    }

    /// <summary>Shot sound: pitch drops and volume climbs with the bullet count.</summary>
    public void PlayShot(int shotCount) => Play(
        GameSound.Shot,
        MathF.Max(-0.4f, -0.08f * (shotCount - 1)),
        1f + (0.15f * (shotCount - 1)));

    /// <summary>Starts the lobby music, unless it's already playing.</summary>
    public void PlayLobbyMusic()
    {
        if (_music is { State: not SoundState.Playing })
        {
            _music.Play();
        }
    }

    /// <summary>Stops the lobby music.</summary>
    public void StopLobbyMusic()
    {
        if (_music is { State: not SoundState.Stopped })
        {
            _music.Stop();
        }
    }

    /// <summary>Disposes the music and every loaded effect.</summary>
    public void Dispose()
    {
        _music?.Dispose();

        foreach (SoundEffect effect in _effects)
        {
            effect.Dispose();
        }
    }

    private static SoundEffect Load(string relativePath)
    {
        string full = Path.Combine(AppContext.BaseDirectory, "Assets", relativePath);

        using FileStream stream = File.OpenRead(full);
        return SoundEffect.FromStream(stream);
    }
}
