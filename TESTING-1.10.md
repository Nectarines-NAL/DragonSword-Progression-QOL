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

## Required gameplay smoke test

Before promoting RC1 to a final release, verify on a clean mod stack:

- one Currency Dungeon for each of Character, Equipment, and Karma EXP Items;
- Sunken Ruins and Ruins Beneath the Waves with Activity Gold and Crafting Materials above x1;
- one Normal Dungeon with Equipment plus Spread and Favor Better Rarity;
- one Trait Dungeon, Hunt, Raid, and Sudden Mission;
- one gathering node and one safe one-time world exploration chest;
- Nightmare Barduk achievement visibility for a save that has not unlocked it.

Gameplay testing should use a backup save. Close the game before rebuilding or replacing the configured PAK.
