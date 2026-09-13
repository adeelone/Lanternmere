# Lanternmere — Narrative Bible

Required by the brief before dialogue implementation: "Create a concise narrative bible before dialogue implementation. Define theme, timeline, region purpose, NPC desire, information each NPC knows, and the order in which revelations may occur." This document is now implemented in full — every NPC, dialogue tree, and revelation gate described below exists in `Content/Data/dialogue/*.json` and is wired through `RegionScene`/`DialogueRunner`. See "What's NOT decided yet" at the bottom for the two items intentionally left as authorial choices, and their actual resolution.

## Theme

A place forgets itself when the people who named it stop telling the story. Restoring a name is an act of attention, not magic — the lantern didn't go dark because of a curse, it went dark because the valley's people scattered and no one kept it lit. The player's role is witness and archivist, not savior.

## Timeline (backstory, revealed out of order through play)

1. **Founding era** — Amaranth Hollow (rain garden), the Cliffwatch (wind cliffs), and Tern's Landing (amber shore) were three distinct settlements that federated around the central lantern for trade and safety.
2. **The dimming** — a multi-year drought strained the rain garden first; families began leaving for the coast.
3. **The scattering** — the last lantern-keeper left without naming a successor. The remaining inhabitants stopped using the old region names out of quiet grief, and the names were never written down anywhere but memory.
4. **Now** — the player arrives as an outside traveler (occupation and origin intentionally unspecified — see ROADMAP for whether to fill this in) and meets The Cartographer, the last resident still trying to keep a record.

## Regions and their purpose

- **Rain garden (Amaranth Hollow)**: teaches observation-based puzzles; theme of quiet resilience despite drought. First region, easiest.
- **Wind cliffs (The Cliffwatch)**: teaches puzzles combining clues from two locations; theme of vigilance and warning systems that outlived their purpose.
- **Amber shore (Tern's Landing)**: gates the finale; theme of departure and what people choose to carry with them when they leave.
- **Archive (interior/underground)**: optional-secret location; holds the full written history if the player pieces together enough fragments.

## NPCs (5 required — draft desires + knowledge)

| NPC | Desire | Knows |
|---|---|---|
| The Cartographer | Wants the map finished before their own memory of the names fades | All three region names; does NOT know why the lantern-keeper left without naming a successor |
| The Ferryperson (amber shore) | Wants someone to finally ask about Tern's Landing directly instead of avoiding the topic | The circumstances of the scattering; the amber shore's name |
| The Gardener (rain garden) | Wants the garden to matter to someone again | Amaranth Hollow's name; a personal reason for staying behind |
| The Watcher (wind cliffs) | Wants to be relieved of a duty they've kept alone for years | The Cliffwatch's name; the beacon puzzle's original purpose |
| The Archivist (archive, optional secret) | Wants the full record kept even if no one reads it | The complete history, including the lantern-keeper's real reason for leaving (final revelation, gated behind the optional secret) |

## Revelation order (must-follow gate) — as implemented

1. Player learns the lantern is dark and meets The Cartographer (village hub).
2. Player recovers each region's name fragment in any order the map allows: the rain garden is open immediately; the wind cliffs beacon puzzle is mechanically locked (`WindCliffsPuzzle.RainGardenVisitedFlag`) until the rain garden has actually been visited (docs/PUZZLES.md puzzle 2); the amber shore altar is locked until *both* the rain garden and wind cliffs fragments are recovered (docs/PUZZLES.md puzzle 3). So the practical order is rain garden → wind cliffs → amber shore, enforced by the puzzles themselves rather than by world geography alone.
3. **Correction from the original draft**: this bible originally said the Ferryperson's full scattering account requires *all three* fragments — that was circular, since the amber shore puzzle IS how the third fragment is obtained. As implemented, the Ferryperson's account (and the tide-glass pattern clue needed to actually solve puzzle 3) unlocks once the *other two* fragments are recovered (`both_prior_fragments_recovered`), matching puzzle 3's own unlock condition. See `docs/PUZZLES.md`.
4. The optional secret (archive) is reachable once the player has the brass key (found at the wind cliffs, after solving puzzle 2) to open the village hub's archive hatch. Its final document — the lantern-keeper's real reason for leaving — is gated on `fragment_amber_shore_recovered`, so reading it only makes narrative sense after the scattering account, resolving the "implementation detail" flagged in the original draft.
5. Relighting the central lantern requires all three fragments (`PuzzleProgression.CanRelightLantern`) and triggers the main ending; if the archive's final record was also read first, the ending shows the optional variation instead (`WorldState.ReachedOptionalEndingVariation`) — this is the optional ending variation's trigger condition.

## What was intentionally left open (and stayed open)

- **Player character's own background/reason for traveling through the valley** — still intentionally blank. No dialogue references it, by design: the theme ("the player's role is witness and archivist, not savior") doesn't need the player to explain themselves, and leaving it unstated keeps the traveler a blank enough vessel for the framing to work. Revisit only if a future pass wants first-person player dialogue lines.
- Exact final dialogue wording is, inevitably, a first pass rather than a polished literary one — see all five trees under `Content/Data/dialogue/` for the actual text now in place.
