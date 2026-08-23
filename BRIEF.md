# Lanternmere — Complete Game Build Brief

> Working name for the former **Wanderer** concept. Complete title, store, trademark, and domain clearance before release.

## Your role

Act as a lead game designer, C#/MonoGame engineer, narrative designer, technical artist, audio implementer, accessibility reviewer, QA tester, and release engineer. Deliver a small finished game, not an engine demo with an unfinished story.

## High concept

Build **Lanternmere**, a quiet 2D exploration adventure about restoring the names of forgotten places. A traveler reaches a valley where its central lantern has gone dark. By observing the environment, solving gentle puzzles, speaking with the remaining inhabitants, and recovering name-fragments, the player restores the valley's map and learns why the settlement chose to disappear.

The emotional target is curiosity, solitude, warmth, and a modest sense of wonder. Avoid combat. Do not imitate the art, characters, maps, writing, or music of an existing game or living artist.

## Scope contract

Make a polished 30–60 minute first playthrough containing:

- One central village/hub
- Three distinct outdoor regions: rain garden, wind cliffs, and amber shore
- One compact interior/underground archive
- Five named NPCs with small personal arcs
- Eight major landmarks
- Three required environmental puzzles
- One optional multi-step secret
- Twelve journal discoveries
- One main ending and one small optional ending variation
- A title sequence, settings, credits, completion state, and post-game continue

Do not expand the world until a clean save can reach the ending without cheats or developer tools.

## Recommended technology

- C# on the current supported .NET version
- MonoGame DesktopGL targeting Windows and macOS
- MonoGame content pipeline for textures, fonts, effects, and audio
- Tiled map files loaded through a maintained importer or a documented data-driven loader
- Aseprite-authored sprite sheets exported to PNG plus metadata
- xUnit or NUnit for deterministic domain tests

Pin tool versions and include setup for both command-line builds and the chosen editor workflow. All third-party and generated assets need a license/provenance manifest.

## Core loop

1. Enter a visually distinct place.
2. Notice a landmark, unusual sound, environmental pattern, or NPC clue.
3. Investigate using movement and a single context-sensitive interaction.
4. Solve an observation, sequence, light, sound, or route puzzle.
5. Recover a name-fragment, memory, map detail, or shortcut.
6. Record it in the journal and alter the world visibly.
7. Return to the central lantern and unlock the next stage.

## Required gameplay systems

- Eight-direction movement with consistent acceleration/deceleration
- Tile and object collision with slopes avoided unless fully supported
- Camera follow, room boundaries, transitions, and subtle shake with disable option
- Context-sensitive interaction prompts
- Branching dialogue, conditions, choices, and a dialogue history
- Inventory for a small fixed set of meaningful items
- Journal containing discoveries, NPC notes, and landmark sketches
- Fog-of-discovery map, not a GPS-style objective arrow
- World flags, puzzle state, shortcuts, and permanent environmental changes
- Pause menu, settings, save slots, autosave, manual save at safe locations, and completion state
- Keyboard and controller input with rebinding
- Layered ambient audio, music crossfades, effects, and independent volume controls

## Narrative requirements

Create a concise narrative bible before dialogue implementation. Define theme, timeline, region purpose, NPC desire, information each NPC knows, and the order in which revelations may occur. Environmental evidence should support the ending instead of relying on an exposition dump. Keep dialogue natural and brief. Every discovery must either deepen the setting, guide a puzzle, characterize someone, or foreshadow the conclusion.

## Puzzle requirements

- Puzzle 1 teaches observation and interaction without text-heavy instructions.
- Puzzle 2 combines clues from two nearby locations.
- Puzzle 3 uses knowledge gathered across regions and gates the finale.
- The optional secret should reward curiosity, not random wall-touching.
- Provide feedback for partial progress and incorrect attempts without punishment.
- Prevent softlocks and permit safe reset of each puzzle.
- Journal clues should help after repeated attempts without immediately giving the answer.

Document every puzzle's inputs, state machine, reset behavior, clues, solution, accessibility alternative, and test cases.

## Architecture

Separate game state, content data, input, simulation, rendering, audio, persistence, and UI. Use data files for maps, dialogue, items, discoveries, triggers, and NPC schedules rather than hardcoding content into the main game loop. Implement a scene/state stack for title, gameplay, pause, dialogue, transitions, and credits. Use a fixed-timestep update and deterministic state changes where practical.

Add developer-only overlays for FPS, coordinates, collision shapes, interactable bounds, room/region, active flags, and audio zones. Ensure release builds disable debug controls and overlays.

## Save system

Use multiple slots with versioned schema, atomic writes, backup copy, corruption detection, and graceful recovery. Save player location at safe anchors, inventory, journal, puzzle states, shortcuts, options, playtime, and ending state. Never autosave during a transition or partially applied puzzle mutation. Include migration handling and save/load round-trip tests.

## Art and animation direction

Use original low-resolution pixel art with warm lamplight against cool natural environments. Define base resolution, tile size, palette limits, sprite scale, animation frame rates, layering, lighting, and export naming before producing assets. Required animation includes idle/walk, NPC idles, water/foliage, lantern states, interaction highlights, region transitions, and finale effects. Avoid excessive bloom, particles, or camera motion.

If final custom assets cannot all be produced, create a cohesive legally usable placeholder set and label it honestly in the asset manifest; do not pull unlicensed images from search results.

## Audio direction

Use distinct ambient identities for each region, restrained musical themes, positional environmental effects where helpful, and clear interaction/puzzle feedback. Loop points must be clean. Provide credits and licenses for every audio file. The game must remain understandable without audio through visual cues and optional captions for meaningful sounds.

## Accessibility and settings

- Rebind keyboard and controller actions
- Adjustable text speed plus instant text
- Dialogue history and replayable critical clues
- Independent master/music/ambience/effects sliders
- Windowed/fullscreen and resolution choices
- Reduced flash, reduced shake, and reduced motion
- High-contrast interaction indicators
- Font/readability option and UI scale
- Hold/toggle choices where relevant
- Visual equivalents for meaningful sound cues

## Required screens and states

Title, new/continue/load, save slot, gameplay HUD, interaction, dialogue, journal, map, inventory, pause, settings, confirmation dialogs, credits, ending, post-game continue, corrupted-save recovery, controller-disconnected, and missing-content error.

## Testing and playthrough requirements

- Unit tests for flags, dialogue conditions, inventory, puzzle transitions, and saves
- Automated map validation for missing spawn points, bad destinations, duplicate IDs, and unreachable required triggers where feasible
- Clean-save playthrough checklist from title to credits
- Out-of-order exploration and repeated-interaction testing
- Save/reload before and after every major puzzle
- Keyboard and at least one common controller test
- Windows and macOS packaged-build smoke tests
- Performance check in the densest scene at the target resolution/frame rate
- No softlocks, progression blockers, missing assets, placeholder dialogue, or debug shortcuts in release

## Repository and delivery

Include source assets, export instructions, content build configuration, clean-build scripts, settings defaults, save-data location documentation, license/provenance manifest, credits, CI, and packaged release instructions. Provide screenshots or a short capture from a verified build, but never substitute a video for an actual build test.

## Definition of done

The game is complete only when a new player can begin, learn controls through play, visit every required region, solve all required puzzles, understand the central mystery, reach credits, reload the completed save, and access the ending variation if its conditions are met. All required content must be present and the packaged builds must launch outside the development environment.

Finish with `AUDIT.md`, marking every requirement **PASS**, **PARTIAL**, or **FAIL**, along with test or playthrough evidence and an honest list of placeholder or unaudited assets.
