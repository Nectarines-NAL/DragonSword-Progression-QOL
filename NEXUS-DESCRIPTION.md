[b][color=#e69138][size=5]Progression QOL Reward Configurator[/size][/color][/b]

Build one custom reward PAK from the settings you choose. Every multiplier starts at vanilla x1, and both Loot Shape options start disabled.

[b]Release:[/b] 0.9.7 RC1
[b]Game baseline:[/b] DragonSword 1.1.2 — Steam build 26210326

This release refreshes the official client and server tables and reward coverage, including all 36 visible Hunts. Existing saved profiles remain compatible. After upgrading the app, load your settings and use Build + Install to rebuild the installed PAK.

[color=#e69138][size=5][b]What you can configure[/b][/size][/color]

- World gathering: plants, ore, and cooking-ingredient nodes
- Material drops from ordinary and field enemies
- Safe stackable rewards from one-time world exploration chests
- Equipment from supported Dungeons, Hunts, Raids, and Sudden Missions
- Activity crafting materials, including boss parts, Raid runes, upgrades, Trait stones, and underwater rewards
- Character, Equipment, and Karma EXP items from repeatable Currency Dungeons
- Adventurer's Emblems
- Gold
- Mercenary Corps Rank EXP
- Tower of Trials rewards
- Equipment roll variety
- Supported mixed-tier rarity odds

Equipment, Safe Chest Stacks, and Tower of Trials are capped at x10. Other quantity controls support up to x20.

[color=#e69138][size=5][b]Tower of Trials[/b][/size][/color]

The independent Tower multiplier covers 78 verified one-time reward routes:

[list]
[*]68 Fragment of Memory rows[/*]
[*]10 Whirling Thoughts rows[/*]
[/list]

These include verified floor-clear and cumulative-star rewards. The multiplier applies when an affected reward is newly earned or still waiting to be claimed. It does not change stars, difficulty, or rewards already collected, and it does not grant a completed reward again.

[color=#e69138][size=5][b]Loot Shape[/b][/size][/color]

[b][color=#e69138]Spread Equipment Rolls — Off is vanilla behavior[/color][/b]

With Spread disabled, multiplied equipment remains grouped into the original stacked reward result.

With Spread enabled, the same total equipment quantity is divided into independent selections from the original equipment pool. This allows more variety during one clear, but it does not force unique item types, add quantity, affect Raid runes, or change equipment stats.

[b][color=#e69138]Favor Better Rarity — Off is vanilla behavior[/color][/b]

With this option disabled, supported reward pools retain their original odds. When enabled, supported Rare/Epic pools favor Epic and supported Epic/Legendary pools favor Legendary. The Better-Tier Chance controls how often the better listed tier is selected. Quantity and equipment main/substat rolls are unchanged.

[color=#e69138][size=5][b]Safe Chest Stacks[/b][/size][/color]

This setting affects supported stackable contents from one-time world exploration chests. It does not affect dungeon chests, unique equipment, progression items, or protected one-time rewards. Tower rewards have their own separate control.

[color=#e69138][size=5][b]Profiles and languages[/b][/size][/color]

Profiles save readable JSON settings in the current Windows user's Local AppData folder. They remain available when the application is updated or moved. Profiles never install anything automatically; load a profile and then select Build + Install.

The configurator includes editable interface files for all eleven languages supported by DragonSword on Steam. English is the source language, and incomplete translations safely fall back to English per string.

[color=#00ff00][size=5][b]Installation[/b][/size][/color]

1. Install the Microsoft .NET 8 Desktop Runtime (x64) if needed.
2. Extract the complete archive to a normal folder. Do not run the app from inside the ZIP.
3. Close DragonSword: Awakening.
4. Run DragonSword.ProgressionQoL.exe.
5. Confirm the game folder and choose your settings.
6. Select Build + Install, review any compatibility or conflict warning, and confirm.

The app installs one owned file to:

DS\Content\Paks\~mods\DS_ZZZ_ProgressionQoL_Configured_P.pak

To change settings later, open the app and use Build + Install again. The current Progression QOL PAK is replaced transactionally and the previous copy is preserved as a disabled backup. No manual deletion is required.

To return this mod to vanilla behavior, use Profiles → Reset to Vanilla Defaults and then Build + Install. The app will ask before disabling its installed PAK. Other mods are never changed.

[color=#e69138][size=5][b]Compatibility and conflicts[/b][/size][/color]

Reward-table PAKs do not merge individual rows at game load. Disable any file identified by the built-in conflict scan before playing. The scanner reads other PAK listings only; it never deletes, moves, or disables another mod.

The app may generate these table paths:

- DS/Content/Design/GameData/RewardRandomData.table
- DS/Content/Design/GameData/RewardData.table when Spread is enabled
- DS/Content/Design/GameData/PropCollectData.table when gathering is changed
- Matching generated server XML files

[color=#e69138][size=5][b]Transparent and offline[/b][/size][/color]

- No network connection, telemetry, or automatic updater
- No administrator request
- No installer service or registry changes
- No DLL injection or game executable modification
- No obfuscation
- Generated and installed PAKs are SHA-256 verified
- Full source code and third-party license notices are provided

The archive includes the open-source repak utility for local PAK creation and verification. It is not downloaded at runtime.

[size=4][b][color=#4da6ff]Source code and documentation[/color][/b][/size]
[url=https://github.com/Nectarines-NAL/DragonSword-Progression-QOL]GitHub — DragonSword Progression QOL[/url]



[b]Release status[/b]

Offline build and installation/recovery checks passed. Gameplay testing is pending. Nexus is reviewing the new main archive, so its download is temporarily unavailable.
