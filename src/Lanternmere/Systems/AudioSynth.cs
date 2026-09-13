using System;
using Microsoft.Xna.Framework.Audio;

namespace Lanternmere.Systems;

/// <summary>
/// Generates every sound in Lanternmere procedurally at runtime — sine
/// tones, filtered noise, simple envelopes — instead of using sourced or
/// downloaded audio samples. This is the audio equivalent of
/// <see cref="TextureFactory"/>'s placeholder art: fully original,
/// legally clean, and honestly labeled as placeholder in
/// ASSET_MANIFEST.md. Everything here is mono PCM16 at
/// <see cref="SampleRate"/>, built once at startup and cached as
/// <see cref="SoundEffect"/> instances by <see cref="AudioLibrary"/>.
/// </summary>
public static class AudioSynth
{
    public const int SampleRate = 22050;

    public static float[] Sine(float frequencyHz, float durationSeconds, float amplitude = 0.5f)
    {
        var count = (int)(durationSeconds * SampleRate);
        var buffer = new float[count];
        for (var i = 0; i < count; i++)
        {
            buffer[i] = amplitude * MathF.Sin(2f * MathF.PI * frequencyHz * i / SampleRate);
        }
        return buffer;
    }

    /// <summary>A sine sweep from startHz to endHz — used for chirpy UI stings.</summary>
    public static float[] SineSweep(float startHz, float endHz, float durationSeconds, float amplitude = 0.5f)
    {
        var count = (int)(durationSeconds * SampleRate);
        var buffer = new float[count];
        var phase = 0.0;
        for (var i = 0; i < count; i++)
        {
            var t = i / (float)count;
            var freq = startHz + (endHz - startHz) * t;
            phase += 2.0 * Math.PI * freq / SampleRate;
            buffer[i] = amplitude * (float)Math.Sin(phase);
        }
        return buffer;
    }

    public static float[] WhiteNoise(float durationSeconds, int seed, float amplitude = 0.3f)
    {
        var rng = new Random(seed);
        var count = (int)(durationSeconds * SampleRate);
        var buffer = new float[count];
        for (var i = 0; i < count; i++)
        {
            buffer[i] = amplitude * (float)(rng.NextDouble() * 2 - 1);
        }
        return buffer;
    }

    /// <summary>Simple one-pole low-pass filter — turns harsh white noise into a softer "wind"/"rain" hiss.</summary>
    public static float[] LowPass(float[] input, float amount)
    {
        var output = new float[input.Length];
        var prev = 0f;
        for (var i = 0; i < input.Length; i++)
        {
            prev += amount * (input[i] - prev);
            output[i] = prev;
        }
        return output;
    }

    /// <summary>Amplitude-modulates a buffer with a slow sine LFO — gives ambience loops a gentle "breathing" quality instead of a static hiss.</summary>
    public static float[] ApplyTremolo(float[] input, float lfoHz, float depth)
    {
        var output = new float[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var lfo = 1f - depth + depth * (0.5f + 0.5f * MathF.Sin(2f * MathF.PI * lfoHz * i / SampleRate));
            output[i] = input[i] * lfo;
        }
        return output;
    }

    /// <summary>Linear attack/release envelope applied in place.</summary>
    public static float[] ApplyEnvelope(float[] input, float attackSeconds, float releaseSeconds)
    {
        var output = (float[])input.Clone();
        var attackSamples = (int)(attackSeconds * SampleRate);
        var releaseSamples = (int)(releaseSeconds * SampleRate);

        for (var i = 0; i < output.Length && i < attackSamples; i++)
        {
            output[i] *= attackSamples == 0 ? 1f : i / (float)attackSamples;
        }
        for (var i = 0; i < output.Length && i < releaseSamples; i++)
        {
            var idx = output.Length - 1 - i;
            if (idx < 0) break;
            output[idx] *= releaseSamples == 0 ? 1f : i / (float)releaseSamples;
        }
        return output;
    }

    public static float[] Mix(params float[][] buffers)
    {
        var length = 0;
        foreach (var b in buffers) length = Math.Max(length, b.Length);
        var output = new float[length];
        foreach (var b in buffers)
        {
            for (var i = 0; i < b.Length; i++) output[i] += b[i];
        }
        for (var i = 0; i < output.Length; i++) output[i] = Math.Clamp(output[i], -1f, 1f);
        return output;
    }

    public static float[] Concat(params float[][] buffers)
    {
        var total = 0;
        foreach (var b in buffers) total += b.Length;
        var output = new float[total];
        var offset = 0;
        foreach (var b in buffers)
        {
            Array.Copy(b, 0, output, offset, b.Length);
            offset += b.Length;
        }
        return output;
    }

    public static SoundEffect ToSoundEffect(float[] samples)
    {
        var pcm = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return new SoundEffect(pcm, SampleRate, AudioChannels.Mono);
    }
}
