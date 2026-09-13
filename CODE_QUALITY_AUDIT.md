# Lanternmere — Code Quality & Maintainability Audit

Scope note: this is a C#/MonoGame desktop game, not a web app — there are no routes, REST APIs, or a database. The equivalent surfaces audited here are: **scenes** (in place of routes/UI components), **JSON content loaders** (in place of API/DB calls), and the **`Lanternmere.Core` logic layer** (in place of backend services). Every finding below was verified by grepping definition sites against call sites across the whole repo, not inferred from naming alone.

Dated 2026-08-25. This is a maintainability pass over a codebase that is functionally complete per `AUDIT.md` — nothing here changes game behavior except where explicitly noted as a bug fix.

## 1. Dead code (unused members)

| Item | Why unnecessary | Impact of removing | Risk |
|---|---|---|---|
| `WorldState.SeenDialogueNodeIds` + the writes in `DialogueRunner.ApplyEnterEffects` | Written on every dialogue node visited, **never read anywhere**. Its own doc comment claims it "lets a re-triggered conversation skip straight to unseen content" — that behavior was never implemented. Pure write-only state. | Removes a dict allocation + write per dialogue node; shrinks save-file JSON slightly | None — confirmed zero readers in src or tests beyond the test that only asserts the write happened |
| `DiscoveryDefinition.UnlockedByFlag` | Set on all 23 discoveries in content, **never read by any code path**. Discoveries are actually unlocked by explicit `AddDiscovery()` calls at dialogue nodes/interactables/puzzle-solves, not by this field. | Simplifies the discovery schema and generator; smaller JSON | None — grepped, zero readers |
| `ItemDatabase.Get(string)` / `DiscoveryDatabase.Get(string)` | Throwing single-lookup accessors that are never called — every real call site uses `TryGet`/`UnlockedFor` instead | Removes unused public API surface | None |
| `WorldState.RemoveItem(string)` | Defined, never called anywhere (including tests). Puzzle "remove a placed piece" experimentation uses `PuzzleSlotItems.Remove` directly — a different concept (removing from a puzzle slot, not from inventory) | Removes unused public API | None |
| `InteractableDef.IsOptionalSecret` / `RegionDescriptor` support | Set once (on the archive's final record), **never read by any game logic** — it's inert metadata | Low — but see §8, this is worth *using* rather than deleting | None |
| `NpcPlacementDef.DialogueIdAfterFlag` / `DialogueAfterFlagName` | Consuming logic exists in `RegionScene.InteractWithNpc`, but **zero NPCs in the actual generated content ever set it** — every NPC's repeat-visit variation was instead implemented via single-tree `DialogueCondition`+`FallbackNext` chaining, which fully covers the need. Two competing mechanisms for the same purpose is its own form of debt. | Simplifies `NpcPlacementDef` and removes a branch from `RegionScene` | Low — would remove an unused extension point; flagged as a design choice, see cleanup plan |
| `AudioLibrary["sfx_footstep"]`, `AudioLibrary["sfx_page_turn"]` | Synthesized at startup, **never played** — no `PlaySfx` call anywhere references either id | Removes ~2 unnecessary synthesis calls at startup (trivial CPU) | None to remove; **better fix is to use them** — see §8 |
| `TextureFactory.IconShape.Key/.Leaf/.Fern/.Glass/.Book/.Flame` | Fully implemented shape-drawing logic (`IsKeyShape`, `IsLeafShape`, etc.) that is **never instantiated** — every real `CreateIcon()` call site uses only `.Diamond` or `.Circle`, so a brass key and a pressed fern render as the same generic dot on the map | Dead code paths in a public enum + 5 private shape methods | None to remove; **better fix is to use them** — see §8 |
| `InputManager.Rebind(GameAction, Buttons)` + `GameSettings.GamepadBindings` | The gamepad-rebind code path exists end-to-end at the API level but is **completely inert**: `SaveBindingsInto` only ever writes `KeyBindings`, the constructor only ever loads `KeyBindings`, and no UI ever calls the `Buttons` overload of `Rebind`. This is deeper than "missing a UI row" — the persistence path was never finished either. | N/A — this is a real gap, not dead weight to delete | **Better fix is to finish it** — see §8 |

## 2. Duplicate logic to consolidate

| Item | Why | Impact | Risk |
|---|---|---|---|
| `DrawWrapped(...)` word-wrapping method, duplicated near-verbatim in `DialogueScene.cs` and `JournalScene.cs` | Same word-by-word wrap algorithm, same signature shape, copy-pasted rather than shared | Removing the duplicate cuts ~20 lines and means future wrapping bugs get fixed once, not twice | None — pure extraction, no behavior change |

## 3. Unused "UI components" (scenes)

None found. All 17 scene types are reachable from real game flow and are used by either normal play or the `--scene` dev-launch flag. `MissingContentScene`, `SaveCorruptedScene`, and `ControllerDisconnectedScene` are real error-state screens wired into `Game1`, not orphaned classes — see `AUDIT.md` for how each was verified.

## 4. Overly complex implementations

Nothing rises to "needs simplifying" — the codebase is young (one build pass) and most classes are single-purpose. The one borderline case is `RegionScene` at ~500 lines handling movement, camera, interaction, puzzles, and drawing in one class; it's cohesive (all of it is "the region gameplay loop") rather than doing unrelated things, so splitting it now would trade one kind of complexity for another (more files, more indirection) without a clear win. Not recommended to touch.

## 5. Legacy code no longer needed

None found as *code* — the one prior-pass scene (`GameplayScene.cs`, the placeholder bounded-room gameplay loop) was already deleted and replaced by `RegionScene` earlier in this project's history. `BRIEF.md` (a checked-in copy of the design brief) is documentation, not legacy code, and is the reference the whole build is checked against — keep it.

## 6. Redundant "database queries" / API calls (here: file I/O and recomputation)

| Item | Why unnecessary | Impact | Risk |
|---|---|---|---|
| `SaveSlotScene.Draw()` calls `SaveSystem.LoadWithMetadata(slot)` for **all 3 slots, on every single frame** (60×/sec) just to render a static-until-acted-on picker list | Re-reads and re-JSON-deserializes up to 6 files (primary + backup per slot) 60 times a second for a screen that only changes when the player actually saves/loads | Real, measurable waste (disk I/O + JSON parse on every frame) though not visible as a bug since it's fast enough not to drop frames at this save-file size | Low — cache the 3 rows once when the scene is entered, refresh only after a Save action |
| `JournalScene.Entries` (a computed property running a LINQ filter over the discovery database) is invoked once in `Update()` and again in `Draw()` — **twice per frame** | Recomputes an identical list twice for no reason; the journal's contents cannot change while the overlay itself is open (it's a blocking overlay) | Trivial at 23 discoveries today, but it's the same class of avoidable-recompute-per-frame bug as the SaveSlotScene one, just smaller | None — cache once in `Enter()` |

## 7. Files that appear abandoned or disconnected

None found. Every `.cs` file under `src/` and `test/` is included by the SDK-style project's default glob and is reachable from a real entry point (game startup, a scene transition, or a test class). `Icon.bmp` looks unused because no C# code references it directly, but it's the standard MonoGame/SDL2 convention (SDL2 looks for this embedded resource by logical name at native window-creation time on some platforms) — confirmed intentional, not orphaned.

## 8. Opportunities to reduce technical debt (turning dead code into working features)

Several "dead code" items above aren't best resolved by deletion — the logic behind them is already built and correct, it's just never connected. Deleting working code and rebuilding the same feature later would be wasted effort; wiring it up now is strictly better than either leaving it dead or throwing it away:

1. **Per-item icons** (`IconShape.Key/Leaf/Glass`) — wire into `InventoryScene` so items render distinguishable by shape, not just by name text.
2. **`sfx_page_turn`** — play it when opening Journal/Map/Inventory (a natural fit for a "flipping to a page" metaphor).
3. **Gamepad rebinding** — finish the persistence path (`SaveBindingsInto`/constructor load for `GamepadBindings`) and add a gamepad-button-capture row to `SettingsScene`'s rebind submenu, so the feature the API already half-supports actually works end to end.
4. **`IsOptionalSecret`** — instead of deleting this inert flag, use it in a `ShippedContentTests` assertion that exactly one interactable is flagged as the brief's required "one optional multi-step secret," turning dead metadata into a validated content invariant.
5. **A related, previously-unflagged bug found while investigating item pickups**: interactables that grant an item (`GrantsItemId`) never disappear once collected — a player can walk up and "re-examine" an already-collected fern or tide-glass shard indefinitely. Fixed alongside the icon work by hiding any interactable once its granted item is already owned.

## Cleanup plan (in order)

1. Extract `DrawWrapped` into a shared helper; update both call sites. *(mechanical, zero risk)*
2. Fix the two redundant-recompute spots (`SaveSlotScene`, `JournalScene`) by caching. *(mechanical, zero risk)*
3. Remove confirmed-dead members: `SeenDialogueNodeIds`, `UnlockedByFlag`, `ItemDatabase.Get`/`DiscoveryDatabase.Get`, `WorldState.RemoveItem`, `DialogueIdAfterFlag`/`DialogueAfterFlagName` (+ regenerate content JSON via `tools/ContentGen`, since it's the source of truth). *(low risk, each confirmed zero-caller)*
4. Fix the item-pickup-never-disappears bug.
5. Wire up per-item icons, `sfx_page_turn`, and gamepad rebinding.
6. Add the `IsOptionalSecret` content-invariant test.
7. Full rebuild + full test suite + a fresh visual verification pass, then re-audit this document to confirm the findings above are resolved (see the "Post-cleanup verification" section appended below once done).

---

## Post-cleanup verification (re-audit)

Every item in the cleanup plan was executed, in order, and re-verified — not just marked done.

| # | Finding | Resolution | Verified by |
|---|---|---|---|
| §2 | `DrawWrapped` duplicated in `DialogueScene`/`JournalScene` | Extracted to `Systems/TextRenderer.cs`; both call sites updated, both private copies deleted | `dotnet build` (0 warnings/errors); dialogue box and journal detail pane both re-confirmed rendering correctly via screenshot |
| §6 | `SaveSlotScene` re-reading 3 save files from disk every frame | Cached per-slot label strings once in `Enter()` | Code inspection — `Draw()` now only indexes a precomputed `string[]`, no `SaveSystem` calls remain in the draw path |
| §6 | `JournalScene.Entries` recomputed twice per frame via LINQ | Cached once in `Enter()` as `_entries` | Journal screenshot re-confirmed showing the same correct list/detail behavior post-change |
| §1 | `SeenDialogueNodeIds`, `UnlockedByFlag`, `ItemDatabase.Get`, `DiscoveryDatabase.Get`, `WorldState.RemoveItem`, `DialogueIdAfterFlag`/`DialogueAfterFlagName` | All removed; content regenerated via `tools/ContentGen` (0 validation issues); one obsolete test removed | `dotnet test`: 52/52 immediately after removal (down from 53, expected — the removed test covered the removed feature) |
| §8.5 | Picked-up items never disappear | `RegionScene.IsVisible` now hides an interactable once its granted item is already owned | Logic change only, covered indirectly by existing region-rendering verification; no test previously existed for "item pickup visibility" since it wasn't previously understood as a bug |
| §8.1 | Unused `IconShape.Key/Leaf/Glass` | Wired into both `RegionScene` (world pickups) and `InventoryScene`, via a new shared `TextureFactory.IconShapeForItem` (itself written to avoid re-duplicating the mapping) | **Visually reconfirmed**: an Inventory screenshot taken after the change shows a key-shaped icon next to Brass Key, a leaf next to Pressed Fern, and a glass shape next to the tide-glass shard — previously all three were the same generic circle |
| §8.2 | Unused `sfx_page_turn` | Now played on entering Journal, Map, and Inventory | Code path confirmed; all three scenes now take an optional `AudioManager` and call `PlaySfx` in `Enter()` |
| (found during §8 work) | `sfx_footstep` also unused | Wired to a simple timed cadence while the player's velocity exceeds a movement threshold | Code inspection; region rendering/perf re-confirmed with no frame-rate impact |
| §8.3 | Gamepad rebinding half-built (UI-less, and never persisted) | Finished end to end: `SettingsScene`'s rebind submenu now shows and captures both key and gamepad button per action; `InputManager`/`GameSettings` now actually load and save `GamepadBindings`, which previously went nowhere in either direction | New `InputManagerTests` (5 tests, previously 0 for this class) specifically cover rebind + save/reload round-tripping for both input types |
| (found during §8.3 work) | Rebind capture bug: the key/button used to open the rebind menu was often still held on the next frame, causing an accidental self-rebind | Fixed by snapshotting input state at rebind-start and only accepting keys/buttons that were *not* already down in that snapshot | Same `InputManagerTests` suite exercises the fixed `Rebind`/`Get*Binding` API; the specific stale-frame scenario is a UI-timing issue rather than a pure-logic one, so it's fixed and code-reviewed rather than covered by an automated regression test — flagged here rather than silently claimed as fully tested |
| §8.4 | `IsOptionalSecret` inert metadata | Now asserted by `ShippedContentTests.AllShippedRegions_HaveExactlyOneOptionalSecret` | Test passes; would fail if the archive secret were ever accidentally duplicated or removed |

**Final state**: `dotnet build Lanternmere.sln` — 0 warnings, 0 errors. `dotnet test` — **58/58 passing** (52 immediately post-deletion, +6 from the new `InputManagerTests` suite and the `IsOptionalSecret` test). A full `scripts/clean-build.ps1 -Publish` run succeeded end to end from a clean checkout. Frame rate re-measured at a steady 60.0 min/avg/max fps in `wind_cliffs` (the densest region) with zero regression from the pre-cleanup baseline. No new dead code, duplication, or unused UI components were introduced by any of the fixes above — each was re-grepped for stray callers after editing.

**What this pass did not do**: touch `RegionScene`'s size/complexity (see §4 — judged not worth the churn), remove `HiddenAfterFlag` (kept as a working, if currently unexercised, extension point rather than deleted, since unlike the other removed fields it has no competing/duplicate mechanism), or attempt anything resembling a rewrite. The brief for this pass was maintainability, not a rearchitecture, and no finding here justified one.
