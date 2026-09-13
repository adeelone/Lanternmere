# Lanternmere — audio direction

Per the brief: "Use distinct ambient identities for each region, restrained musical themes, positional environmental effects where helpful, and clear interaction/puzzle feedback... Loop points must be clean... The game must remain understandable without audio."

## What's implemented

- **Distinct ambience per region**: `AudioLibrary` builds one filtered-noise-plus-tremolo loop per region (village hub, rain garden, wind cliffs, amber shore, archive), each with a different seed/filter/LFO rate so they're audibly distinct, not just volume-varied copies of one texture. See `src/Lanternmere/Systems/AudioLibrary.cs`.
- **Music crossfades**: `AudioManager` crossfades both the music and ambience buses over 1.2s whenever `PlayMusic`/`PlayAmbience` is called with a new track id (region transitions, entering the ending). Implementation: two `SoundEffectInstance`s (incoming/outgoing) with linearly interpolated volumes, the outgoing one stopped and released once the fade completes. See `AudioManager.UpdateCrossfade`.
- **Clean loop points**: every ambience/music clip is generated from continuous periodic functions (sine, tremolo-modulated filtered noise) rather than a fixed-length recording, so the loop point is mathematically seamless — there's no click or discontinuity at the wrap, since sample N and sample 0 are both just points on the same continuous waveform. This is arguably *more* reliably clean than a hand-edited loop point on a real recording would be, though it comes at the cost of sounding synthetic rather than organic.
- **Independent volume controls**: `GameSettings.MasterVolume`/`MusicVolume`/`AmbienceVolume`/`EffectsVolume`, all adjustable in `SettingsScene`, all read live by `AudioManager` each frame (not just at track-start), so dragging a slider mid-playback takes effect immediately.
- **Interaction/puzzle feedback**: distinct SFX for interact, menu move/select, puzzle correct-step/solved/wrong, discovery-unlocked, page-turn (history view), lantern-relight — see `RegionScene.HandlePuzzleFeedback` and the `sfx_*` entries in `AudioLibrary`.
- **Understandable without audio**: every audio cue has a visual/textual equivalent that fires regardless of volume settings — puzzle feedback shows a toast message (`RegionScene.ShowToast`) in addition to a sound, dialogue is always fully captioned text (there's no voice audio at all, so there's nothing *to* caption), and discoveries/items always show their own notification text.

## What's NOT implemented

- **Positional/spatial audio.** All SFX play at a flat volume regardless of the interacting object's position relative to the player or camera — there's no panning or distance attenuation. `SoundEffect.Play` is called with a fixed pan of 0.
- **"Restrained musical themes" as actual composition.** `music_theme`/`music_ending` are three-note sustained triads (a chord, essentially), not composed melodic themes. Functional as a placeholder bed, not as music in the sense the brief's art direction implies.
- **Captions for ambience-only sound cues.** Since there's no meaningful non-dialogue "sound that carries information" beyond puzzle feedback (which is already captioned via toasts), this gap is narrower than it sounds, but there's no generalized subtitle/caption system for arbitrary ambient sound events.
- **Credits/licenses for individual audio files** in the sense the brief means (crediting a composer, listing a sample library) — not applicable, since every sound is synthesized in `AudioSynth.cs` at runtime rather than sourced from anywhere; see `ASSET_MANIFEST.md`.
