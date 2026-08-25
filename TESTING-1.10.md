# DragonSword 1.0.10 compatibility test matrix

Baseline: unmodified Steam build `24831799`.

## Completed automated validation

- Official extraction registered only `pakchunk` archives and excluded installed mod PAKs.
- All managed 1.0.10 reward, dungeon, item, gathering, and chest tables are byte-identical to the validated 1.0.9 baseline. Only `StringData_en.table`, which this project does not package, changed.
- The regenerated schema-4 manifest resolves 822 activity rows across 563 random groups, 431 safe world-chest rows, and 31 supported rarity groups.
- The two explicitly allowlisted underwater maps resolve to exactly eight repeat rows: Gold and three material rows from Sunken Ruins, plus Gold and three material rows from Ruins Beneath the Waves.
- No repeat target overlaps a first-clear reward or is referenced by an unsupported reward route.
- Currency EXP Items x2 changes exactly 18 verified Character, Equipment, and Karma EXP rows and no other row. PAK SHA-256: `CE5CCFAAAFFBE8B54E20E824B8C9BF91D83EF66931944D0A95B6D776BA7C4A13`.
- The combined Activity Materials + Gold x2 build changes the expected 276 rows; validation specifically confirms all eight underwater rows are included. PAK SHA-256 for the recorded validation build: `DE17A70743692C6B1A5C91ECB489D5EF1148FD968C19BC52B3F69B9D812207B1`.
- The maximum-settings build packs, lists, unpacks, and hash-verifies all six managed client/server files. PAK SHA-256: `C2602B50076F2BD2F843AD7E36D60E170608C2FF42D753363F5A0A33B1C66B6F`.
- Maximum World Gathering changes exactly all 48 supported gathering rows.
- Nightmare Barduk achievement reward `2610811` is unchanged through the maximum Spread build.
- Release compilation completes with zero warnings and zero errors.
- Default, showcase, and Build & Install UI snapshots render successfully at the 1920×1120 test size with no observed clipping.
- Vanilla world-chest data references absent random group `15001003`. The configurator records this source defect and intentionally does not invent an unknown reward.

## 0.9.5 release hardening

- Rebuilt the published 0.9.4 validation profile and inspected all 135 Trait-category targets: 99 material/Emblem rows and 36 Rank EXP rows. No row was missing, zero-valued, negatively weighted, or incorrectly multiplied.
- Confirmed all 36 Trait Dungeon completion routes remain byte-equivalent after applying Equipment x5, Crafting Materials x5, Emblems x5, Rank EXP x10, Spread Equipment Rolls, and Favor Better Rarity together.
- Added the same Trait-row and completion-route assertions to every generated build. The checked counts are written to `BUILD-REPORT.json`.
- Confirmed the generated six client/server files are byte-identical to the previously audited 0.9.4 validation output; localization, installation, and staging changes do not alter reward-table output.
- Tested installation policy in isolated fake Steam layouts: exact build installs normally; newer and unknown builds require confirmation; a known older build remains blocked; root-level owned PAKs migrate to disabled backups; the active PAK is installed to `Paks\~mods`; and vanilla restore disables it successfully.
- Installation now verifies a non-loadable temporary copy before disabling the currently installed PAK, then verifies the final destination again.
- Confirmed temporary work falls back safely to the selected output folder when Local AppData is not writable, then removes the temporary cache after successful verification.
- Audited every currently installed non-official PAK without filename filtering, including Treasure Respawn; none besides Progression QOL contained a managed reward/material table.

## Required gameplay smoke test

Before promoting RC1 to a final release, verify on a clean mod stack:

- one Currency Dungeon for each of Character, Equipment, and Karma EXP Items;
- Sunken Ruins and Ruins Beneath the Waves with Activity Gold and Crafting Materials above x1;
- one Normal Dungeon with Equipment plus Spread and Favor Better Rarity;
- one Trait Dungeon, Hunt, Raid, and Sudden Mission;
- one gathering node and one safe one-time world exploration chest;
- Nightmare Barduk achievement visibility for a save that has not unlocked it.

Gameplay testing should use a backup save. Close the game before rebuilding or replacing the configured PAK.
