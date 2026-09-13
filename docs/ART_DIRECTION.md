# Lanternmere — art direction

Per the brief: "Define base resolution, tile size, palette limits, sprite scale, animation frame rates, layering, lighting, and export naming before producing assets." This documents what's actually implemented (procedural placeholder art — see `ASSET_MANIFEST.md`) and, honestly, what a real asset pass still needs to define for itself since procedural generation sidesteps several of these decisions.

## What's fixed today

- **Tile size**: 20×20px, consistent across all five regions (`RegionDescriptor.TileSize`).
- **Window resolution**: defaults to 1280×720 (`GameSettings.ResolutionWidth/Height`), adjustable in Settings; not a fixed low-res internal canvas with pixel-perfect upscaling — tiles render at native size, so there's no "base resolution" separate from window resolution the way a strict pixel-art pipeline would define one. A real art pass should decide this (e.g. render to a 320×180 target and upscale) before hand-authoring pixel art, since hand-drawn pixel art usually assumes a fixed internal resolution.
- **Palette**: per-region `groundColorHex`/`accentColorHex` pairs (warm lamplight accents against cool natural ground tones, per the brief's direction) — see each region's JSON. Not a shared master palette file; `TextureFactory` derives tile shading from these two colors procedurally rather than sampling from swatches.
- **Sprite scale**: player/NPC figures are 16×22px (`TextureFactory.CreateFigure` default call sites in `RegionScene`), roughly 0.8-1.1 tiles tall.
- **Layering**: two layers only — ground/wall tiles, then interactables+NPCs+player on top, then UI. No parallax, no foreground occlusion layer.
- **Lighting**: none implemented as a rendering effect. The lantern's lit/dark state is currently represented narratively (dialogue, ending text) and by a static icon, not by an actual dynamic light/shadow system — the brief's "lantern states" animation requirement is unmet in the visual sense.

## What's NOT implemented (real gaps, not just unstated)

- **No animation at all.** No idle/walk cycles, no water/foliage motion, no interaction-highlight animation, no region-transition effects, no finale visual effects. Every sprite is a single static generated texture. This is the single biggest visual gap versus the brief's "Required animation" list.
- **No export naming convention**, since nothing is exported from an external tool — textures are generated directly as in-memory `Texture2D`s, never written to disk as intermediate art files.
- **No hand-authored palette limits** (e.g. "16 colors per region") — procedural generation computes shades algorithmically rather than picking from a constrained swatch, so it can't accidentally violate a limit, but it also isn't demonstrating the discipline a limited palette is meant to enforce.

A real art pass should treat this document's "What's fixed today" section as a starting point (tile size, per-region palette anchors) and explicitly define the resolution/animation/export decisions above before producing final assets.
