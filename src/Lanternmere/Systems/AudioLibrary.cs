using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Lanternmere.Systems;

/// <summary>
/// Builds and caches every SoundEffect Lanternmere uses, all via
/// <see cref="AudioSynth"/>. Built once at startup (roughly a second of
/// synthesis work for ~15 short clips) rather than per-play.
/// </summary>
public sealed class AudioLibrary
{
    private readonly Dictionary<string, SoundEffect> _sounds = new();

    public AudioLibrary()
    {
        // Ambience loops (~4s each, designed to loop cleanly since start
        // and end amplitude both approach zero via the tremolo LFO).
        _sounds["ambience_village_hub"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyTremolo(AudioSynth.LowPass(AudioSynth.WhiteNoise(4f, 1, 0.12f), 0.05f), 0.25f, 0.4f));

        _sounds["ambience_rain_garden"] = AudioSynth.ToSoundEffect(
            AudioSynth.Mix(
                AudioSynth.ApplyTremolo(AudioSynth.LowPass(AudioSynth.WhiteNoise(4f, 2, 0.18f), 0.15f), 0.6f, 0.3f),
                AudioSynth.Sine(1200f, 4f, 0.02f)));

        _sounds["ambience_wind_cliffs"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyTremolo(AudioSynth.LowPass(AudioSynth.WhiteNoise(4f, 3, 0.22f), 0.08f), 0.15f, 0.5f));

        _sounds["ambience_amber_shore"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyTremolo(AudioSynth.LowPass(AudioSynth.WhiteNoise(4f, 4, 0.2f), 0.1f), 0.18f, 0.6f));

        _sounds["ambience_archive"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyTremolo(AudioSynth.LowPass(AudioSynth.WhiteNoise(4f, 5, 0.08f), 0.03f), 0.1f, 0.2f));

        // Music: a slow, restrained triad pad — nothing melodic or catchy
        // enough to risk resembling an existing work, per the brief.
        _sounds["music_theme"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyEnvelope(AudioSynth.Mix(
                AudioSynth.Sine(220f, 6f, 0.1f),
                AudioSynth.Sine(277.18f, 6f, 0.07f),
                AudioSynth.Sine(329.63f, 6f, 0.06f)), 1.5f, 1.5f));

        _sounds["music_ending"] = AudioSynth.ToSoundEffect(
            AudioSynth.ApplyEnvelope(AudioSynth.Mix(
                AudioSynth.Sine(261.63f, 8f, 0.1f),
                AudioSynth.Sine(329.63f, 8f, 0.07f),
                AudioSynth.Sine(392.00f, 8f, 0.06f)), 2f, 3f));

        // SFX
        _sounds["sfx_interact"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.Sine(660f, 0.08f, 0.35f), 0.005f, 0.06f));
        _sounds["sfx_menu_move"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.Sine(440f, 0.05f, 0.3f), 0.002f, 0.04f));
        _sounds["sfx_menu_select"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.SineSweep(500f, 800f, 0.1f, 0.35f), 0.005f, 0.08f));
        _sounds["sfx_page_turn"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.LowPass(AudioSynth.WhiteNoise(0.18f, 11, 0.25f), 0.3f), 0.01f, 0.15f));
        _sounds["sfx_footstep"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.LowPass(AudioSynth.WhiteNoise(0.05f, 12, 0.15f), 0.4f), 0.002f, 0.04f));

        _sounds["sfx_puzzle_correct"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(
            AudioSynth.Concat(AudioSynth.Sine(523.25f, 0.1f, 0.3f), AudioSynth.Sine(659.25f, 0.15f, 0.3f)), 0.005f, 0.1f));

        _sounds["sfx_puzzle_solved"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(
            AudioSynth.Concat(AudioSynth.Sine(523.25f, 0.12f, 0.3f), AudioSynth.Sine(659.25f, 0.12f, 0.3f), AudioSynth.Sine(783.99f, 0.25f, 0.3f)), 0.005f, 0.2f));

        _sounds["sfx_puzzle_wrong"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(AudioSynth.Sine(140f, 0.2f, 0.3f), 0.005f, 0.15f));

        _sounds["sfx_discovery"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(
            AudioSynth.Mix(AudioSynth.Sine(880f, 0.6f, 0.2f), AudioSynth.Sine(1320f, 0.6f, 0.1f)), 0.01f, 0.5f));

        _sounds["sfx_lantern_relight"] = AudioSynth.ToSoundEffect(AudioSynth.ApplyEnvelope(
            AudioSynth.SineSweep(220f, 660f, 1.2f, 0.3f), 0.3f, 0.6f));
    }

    public SoundEffect Get(string id) => _sounds[id];
    public bool TryGet(string id, out SoundEffect effect) => _sounds.TryGetValue(id, out effect!);
}
