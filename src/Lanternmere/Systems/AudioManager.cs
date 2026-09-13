using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Lanternmere.Core;

namespace Lanternmere.Systems;

/// <summary>
/// Mixer for music/ambience/SFX buses, reading levels from
/// <see cref="GameSettings"/> and crossfading between looping tracks on
/// region transitions, per the brief's "Layered ambient audio, music
/// crossfades, effects, and independent volume controls."
/// </summary>
public sealed class AudioManager
{
    private const float CrossfadeSeconds = 1.2f;

    private readonly AudioLibrary _library;
    private readonly GameSettings _settings;

    private SoundEffectInstance? _musicInstance;
    private SoundEffectInstance? _musicFadingOutInstance;
    private string? _currentMusicId;
    private float _musicFadeElapsed;

    private SoundEffectInstance? _ambienceInstance;
    private SoundEffectInstance? _ambienceFadingOutInstance;
    private string? _currentAmbienceId;
    private float _ambienceFadeElapsed;

    public AudioManager(AudioLibrary library, GameSettings settings)
    {
        _library = library;
        _settings = settings;
    }

    public void PlayMusic(string? musicId)
    {
        if (musicId == _currentMusicId) return;
        _musicFadingOutInstance?.Stop();
        _musicFadingOutInstance = _musicInstance;
        _musicInstance = CreateLoopingInstance(musicId);
        _currentMusicId = musicId;
        _musicFadeElapsed = 0f;
    }

    public void PlayAmbience(string? ambienceId)
    {
        if (ambienceId == _currentAmbienceId) return;
        _ambienceFadingOutInstance?.Stop();
        _ambienceFadingOutInstance = _ambienceInstance;
        _ambienceInstance = CreateLoopingInstance(ambienceId);
        _currentAmbienceId = ambienceId;
        _ambienceFadeElapsed = 0f;
    }

    public void PlaySfx(string sfxId)
    {
        if (!_library.TryGet(sfxId, out var effect)) return;
        effect.Play(Volume(_settings.EffectsVolume), 0f, 0f);
    }

    private SoundEffectInstance? CreateLoopingInstance(string? id)
    {
        if (id is null || !_library.TryGet(id, out var effect)) return null;
        var instance = effect.CreateInstance();
        instance.IsLooped = true;
        instance.Volume = 0f;
        instance.Play();
        return instance;
    }

    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateCrossfade(ref _musicFadeElapsed, dt, _musicInstance, _musicFadingOutInstance, _settings.MusicVolume, ref _musicFadingOutInstance);
        UpdateCrossfade(ref _ambienceFadeElapsed, dt, _ambienceInstance, _ambienceFadingOutInstance, _settings.AmbienceVolume, ref _ambienceFadingOutInstance);
    }

    private void UpdateCrossfade(ref float elapsed, float dt, SoundEffectInstance? incoming, SoundEffectInstance? outgoing, float bucketVolume, ref SoundEffectInstance? outgoingRef)
    {
        elapsed += dt;
        var t = MathHelper.Clamp(elapsed / CrossfadeSeconds, 0f, 1f);

        if (incoming is { IsDisposed: false })
        {
            incoming.Volume = Volume(bucketVolume) * t;
        }

        if (outgoing is { IsDisposed: false })
        {
            outgoing.Volume = Volume(bucketVolume) * (1f - t);
            if (t >= 1f)
            {
                outgoing.Stop();
                outgoingRef = null;
            }
        }
    }

    private float Volume(float bucketVolume) => MathHelper.Clamp(_settings.MasterVolume * bucketVolume, 0f, 1f);

    public void StopAll()
    {
        _musicInstance?.Stop();
        _musicFadingOutInstance?.Stop();
        _ambienceInstance?.Stop();
        _ambienceFadingOutInstance?.Stop();
    }
}
