# DragonSword 1.09 compatibility test matrix

Baseline: unmodified Steam build `24693558`.

Automated validation completed:

- all 814 independently derived activity rows resolve to their expected 1.09 item identity;
- all visible Normal, Currency, and Trait dungeons, Hunts, and Raids use their live 1.09 direct clear rewards;
- all eleven known Sudden Mission groups are included;
- no repeat-reward target is shared with first-clear, story, or achievement rewards;
- Nightmare Barduk achievement reward `2610811` is preserved through a Spread build;
- x20 world gathering changes all 48 supported gathering rows by exactly 20x;
- the maximum-multiplier build produced zero target quantity mismatches;
- generated PAKs were listed, unpacked, and hash-verified.

Gameplay validation still required:

- one Normal dungeon at each difficulty;
- each Currency dungeon type;
- one Trait dungeon, confirming Trait stones remain listed and awarded;
- one low and one high Hunt;
- one Raid, checking equipment, runes, Gold, and materials separately;
- one Sudden Mission;
- Nightmare Barduk achievement visibility and unlock behavior;
- World Gathering at x20;
- Spread plus Favor Better Rarity on Corrupted Bedchamber Hard;
- UI at Windows 100%, 125%, 150%, and 200% scaling.
