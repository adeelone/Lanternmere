# Lanternmere — Narrative Bible (draft)

Required by the brief before dialogue implementation: "Create a concise narrative bible before dialogue implementation. Define theme, timeline, region purpose, NPC desire, information each NPC knows, and the order in which revelations may occur." This is a first draft scaffold, not final content — see ROADMAP.md for what's still open.

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

## Revelation order (must-follow gate)

1. Player learns the lantern is dark and meets The Cartographer (village hub).
2. Player recovers each region's name fragment in any order (rain garden → wind cliffs → amber shore, but non-linear is allowed).
3. Only after all three fragments are recovered does The Ferryperson's full scattering account become available — this is the puzzle-3 gate described in the brief ("uses knowledge gathered across regions and gates the finale").
4. The optional secret (archive) may be found any time after puzzle 2, but its final revelation about the lantern-keeper only makes narrative sense after step 3, so the archive's last document should refuse to "read" narratively satisfying until then (implementation detail — see ROADMAP puzzle documentation task).

## What's NOT decided yet (flag for follow-up before writing final dialogue)

- Player character's own background/reason for traveling through the valley — intentionally blank; needs a decision before VO/dialogue references it.
- Exact wording of all NPC dialogue trees beyond the one sample in `Content/Data/dialogue/cartographer_intro.json`.
- The optional ending variation's exact trigger condition.
