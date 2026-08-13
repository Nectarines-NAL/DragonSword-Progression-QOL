# DragonSword 1.08 gameplay test matrix

Test with all older reward/material PAKs disabled. Keep Treasure Respawn enabled for the dedicated compatibility pass.

## Pass A — conservative baseline

- Gathering x1; enemy materials x1; equipment x2.
- Activity materials x2; Adventurer's Emblems x2; gold x2; rank EXP x2; chests x1.
- Spread Rolls on; enhanced rarity off.

Verify Warg Cave and at least two other activities. Record each individual equipment stack and whether helm/chest/gloves all appear across repeated runs.

## Pass B — Spread Rolls stress

- Equipment x5; other activity values x1; Spread Rolls on.
- Repeat with Favor Better Rarity on at 90%; generated spread rolls must retain the configured 90/10 better-tier weighting.

Run Warg Cave at least 20 times. Record the number of equipment roll events, item types, stack sizes, and whether rewards remain within expected totals. A run may repeat an equipment type; diversity must not be forced.

## Pass C — material coverage

- Gathering x2; enemy materials x2; activity materials x2.

Test early and late gathering nodes, ordinary enemies, elite/field enemies, and one dungeon material payout. Watch specifically for quest-item inflation or non-material rewards changing.

## Pass D — world chests

- Safe chest stacks x2; all other values x1.

Open ordinary, superior, hidden, and one unique/progression chest if safely repeatable. Stackable consumables/materials should scale; maps, costumes, Karma, Character Master Soul, and Fragments of Sacred Light must not multiply.

## Pass E — Treasure Respawn coexistence

- Keep `DS_TreasureRespawn.pak` enabled.
- Use Safe chest stacks x2.
- Open a chest, wait/trigger its respawn cycle, and open it again.

Confirm both that the chest respawns and whether the second opening awards the configured quantity. File-path compatibility is already verified; claim-state behavior requires this gameplay pass.

## Failure capture

Preserve the generated `BUILD-REPORT.json`, the exact PAK lists in `DS\Content\Paks` and `~mods`, dungeon/chest name and difficulty, expected versus actual quantities, screenshots or video of the reward panel, and whether restarting the game reproduces the failure.
