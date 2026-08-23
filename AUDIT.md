# Lanternmere — AUDIT

Honest requirement-by-requirement status for this scaffolding pass, dated 2026-08-17. **PASS** = implemented and exercised by a test or verified run described below. **PARTIAL** = started but incomplete. **FAIL** / not listed = not started.

## Scope contract (30–60 min playthrough with specific content)

FAIL as a whole — this pass built architecture, not content. Zero of the required regions/NPCs/puzzles/discoveries/endings exist as playable content yet. Specifically:

| Requirement | Status |
|---|---|
| Central village/hub | PARTIAL — a placeholder bounded room exists where the hub will go; no real hub content |
| 3 outdoor regions | FAIL |
| 1 interior/underground archive | FAIL |
| 5 named NPCs with arcs | PARTIAL — all 5 are named and given desires/knowledge in `docs/NARRATIVE_BIBLE.md`; none exist in-game |
| 8 major landmarks | FAIL |
| 3 required puzzles | PARTIAL — fully specced (state machine, clues, accessibility alt, test cases) in `docs/PUZZLES.md`; none implemented |
| 1 optional secret | FAIL — not even specced yet |
| 12 journal discoveries | PARTIAL — 2 defined in `Content/Data/discoveries/discoveries.json` as a format example; 10 more needed |
| Main + optional endings | FAIL |
| Title/settings/credits/completion/continue | PARTIAL — title screen input handling exists (New Game, Quit); no settings/credits screens, no completion state, no continue-from-save |

## Recommended technology

| Requirement | Status | Evidence |
|---|---|---|
| C# on current supported .NET | PARTIAL | Targets .NET 8 LTS, not the newest .NET 9 the official MonoGame template defaults to — the sandboxed dev environment only had the 8.0 SDK available. Bump to 9 if desired; nothing in the code depends on 8-specific features. |
| MonoGame DesktopGL, Windows + macOS | PARTIAL | Builds and a 15s headless run succeeds on Linux via Xvfb in this environment; Windows/macOS have NOT been tested — do not treat as verified until run on those platforms |
| Tiled maps | FAIL | Not wired up at all; see ROADMAP #1 |
| Aseprite sprite sheets | FAIL | No art produced |
| xUnit | PASS | `test/Lanternmere.Tests`, 10 passing tests, run via `dotnet test` |
| Pinned tool versions, lockfile-equivalent | PASS | `Lanternmere.csproj`/`Lanternmere.Core.csproj` pin `net8.0` and `MonoGame.Framework.DesktopGL` `3.8.*`; no separate lockfile mechanism exists for NuGet the way npm has one, but versions are pinned in the csproj files themselves |

## Core loop

PARTIAL. Steps 1–3 (enter a place, notice something, investigate/move) are mechanically supported by movement + collision + a stubbed interaction action binding. Steps 4–7 (solve a puzzle, recover a fragment, journal it, unlock next stage) have no implementation — only design docs.

## Required gameplay systems

| Requirement | Status | Evidence |
|---|---|---|
| 8-direction movement with accel/decel | PASS | `Entities/Player.cs`; manually verified via headless run (no automated gameplay-feel test — feel tuning is inherently manual) |
| Tile/object collision, no slopes | PASS | `Systems/CollisionSystem.cs`, 3 passing unit tests in `CollisionSystemTests.cs` |
| Camera follow, room boundaries, transitions, shake+disable | FAIL | Not implemented |
| Context-sensitive interaction prompts | FAIL | `GameAction.Interact` binding exists; no prompt UI or interactable-object system |
| Branching dialogue, conditions, choices, history | FAIL | One static JSON dialogue tree exists as a data format example; no runtime dialogue system reads it |
| Inventory | PARTIAL | `WorldState.InventoryItemIds` list exists; no UI, no pickup/use logic |
| Journal | PARTIAL | `WorldState.JournalDiscoveryIds` exists; no UI |
| Fog-of-discovery map | FAIL | Not implemented |
| World flags, puzzle state, shortcuts, permanent changes | PARTIAL | Data model exists and is unit-tested (`WorldStateTests.cs`); nothing in gameplay actually sets these yet |
| Pause menu, settings, save slots, autosave, manual save, completion state | PARTIAL | Pause scene exists (blocks/overlays correctly); `SaveSystem` supports 3 slots with atomic writes/backup/corruption recovery (tested); nothing calls `SaveSystem.Save` from gameplay yet — no autosave, no manual save trigger, no completion state |
| Keyboard + controller input with rebinding | PASS | `Systems/InputManager.cs`; rebind API exists and is exercised implicitly (defaults load, bindings persist to `GameSettings`) — no dedicated rebind-UI test since there's no rebind UI yet, only the underlying mechanism |
| Layered ambient audio, crossfades, volume controls | FAIL | `GameSettings` has volume fields; nothing plays audio |

## Narrative requirements

PARTIAL. `docs/NARRATIVE_BIBLE.md` covers theme, timeline, region purpose, NPC desire/knowledge, and revelation order as required — but it's an honest first draft with open questions flagged (player background, exact dialogue), not final content, and none of it is implemented in-game beyond one sample dialogue file.

## Puzzle requirements

PARTIAL. All required documentation fields (inputs, state machine, reset behavior, clues, solution, accessibility alternative, test cases) are written for all 3 required puzzles in `docs/PUZZLES.md`. Zero puzzles are actually implemented or tested in code.

## Architecture

| Requirement | Status | Evidence |
|---|---|---|
| Separation of state/content/input/simulation/rendering/audio/persistence/UI | PASS | `Lanternmere.Core` (state, input, simulation, persistence) is a separate assembly from `Lanternmere` (rendering/UI); no audio layer exists yet to separate |
| Data files for maps/dialogue/items/discoveries/triggers/NPC schedules | PARTIAL | JSON format established and one example file per category exists under `Content/Data/`; nothing in code actually loads or parses these files yet (`village_hub.json` references an NPC schedule path that doesn't exist) |
| Scene/state stack | PASS | `Core/SceneManager.cs`, `Core/IScene.cs`; Title/Gameplay/Pause all working |
| Fixed-timestep, deterministic updates | PASS | `Game1` constructor sets `IsFixedTimeStep = true` explicitly |
| Developer-only overlays, compiled out of release | PASS | `Core/DebugOverlay.cs` uses `#if DEBUG` to become a complete no-op (including the toggle) in non-DEBUG builds — verified by reading the compiled behavior, not yet verified with an actual Release-configuration build in this pass |

## Save system

PASS for what's built: atomic writes (temp file + verified round-trip + backup-then-replace), 3 slots, corruption detection with backup fallback, and schema versioning with a migration hook are all implemented in `Core/SaveSystem.cs` and covered by 4 passing tests including a real corrupted-primary-falls-back-to-backup test (`SaveSystemTests.cs`). NOT yet verified: the "never autosave during a transition or partially applied puzzle mutation" rule, since nothing calls Save() from gameplay yet — there's no transition/puzzle-mutation code to violate it, but also no proof it will be respected once that code exists.

## Art, audio, accessibility, required screens

Mostly FAIL/PARTIAL — see ROADMAP.md items 5, 6, 8, 9, 10. The one accessibility item that IS real: `GameSettings` has concrete fields for every accessibility toggle the brief lists (reduced motion/shake/flash, high contrast, text speed/instant, rebinding), so the data model won't need to change when those features are built — but none of the actual in-game effects (e.g. actually reducing screen shake) exist yet because screen shake itself doesn't exist yet.

## Testing and playthrough requirements

| Requirement | Status |
|---|---|
| Unit tests for flags, dialogue conditions, inventory, puzzle transitions, saves | PARTIAL — flags (WorldState) and saves are tested; dialogue conditions, inventory logic, and puzzle transitions have no implementation to test yet |
| Automated map validation | FAIL — no map loader exists |
| Clean-save playthrough checklist | FAIL — no playable content to check |
| Save/reload before/after every major puzzle | FAIL — no puzzles exist |
| Keyboard + controller test | PARTIAL — keyboard input is exercised via the headless Xvfb run (no crash); no physical/virtual controller was available to test in this sandboxed environment |
| Windows/macOS packaged-build smoke test | FAIL — only Linux build+headless-run verified here |
| Performance check in densest scene | FAIL — no dense scene exists yet |
| No softlocks/missing assets/placeholder dialogue/debug shortcuts in release | UNVERIFIED — no Release-configuration build has been produced in this pass; `DebugOverlay`'s `#if DEBUG` guard is the only concrete protection in place so far |

## Repository and delivery

| Requirement | Status |
|---|---|
| Source assets, export instructions | FAIL — no assets yet |
| Content build configuration | PASS — `Content/Content.mgcb` exists and builds cleanly (0 content items currently, which is honest given no assets exist) |
| Clean-build scripts | PARTIAL — `dotnet build`/`dotnet test` documented in README; no single clean-build script |
| Settings defaults | PASS — `GameSettings` constructor defaults are reasonable and documented inline |
| Save-data location documentation | PASS — README "Save data location" |
| License/provenance manifest | PASS (honestly empty) — `ASSET_MANIFEST.md` |
| Credits | FAIL |
| CI | FAIL |
| Packaged release instructions | FAIL |

## Overall

This is a real, building, tested engine foundation for Lanternmere — not a mockup and not just an idea document. `dotnet build` succeeds with 0 warnings/0 errors across both projects, `dotnet test` passes 10/10 real tests (including a genuine corrupted-save-recovers-from-backup test), and a 15-second headless run under Xvfb starts without error. It contains **none of the actual game** described in the brief's scope contract: no regions, no NPCs, no puzzles, no art, no audio, no dialogue beyond one sample file. The narrative bible and puzzle specs are real design work meant to make that content buildable next, not a substitute for it. **Do not represent this as a playable slice of Lanternmere.** Windows/macOS packaged builds remain unverified until actually run on those platforms, per the brief's requirement not to substitute a description for an actual build test.
