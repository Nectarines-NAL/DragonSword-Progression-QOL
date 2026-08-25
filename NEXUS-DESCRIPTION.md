# Progression QOL — Reward Configurator

Created by **nectarines**

Progression QOL lets you build one custom reward PAK instead of choosing between a pile of fixed multiplier files. Every setting starts at vanilla behavior, so you decide exactly what changes.

**Current release candidate:** Progression QOL 0.9.5 RC1, validated for DragonSword: Awakening 1.0.10, Steam build 24831799.

## Updated for DragonSword 1.0.10

- Revalidated against the unmodified 1.0.10 game tables
- Confirmed every managed reward-system table is unchanged from the validated 1.0.9 baseline
- Added Sunken Ruins and Ruins Beneath the Waves to Activity Gold and Crafting Materials
- Added a separate Currency EXP Items control for Character, Equipment, and Karma EXP items
- Retains the 1.0.9 fixes for activity rewards, World Gathering x20, Favor Better Rarity with Spread, and the Nightmare Barduk achievement
- Added named profiles that remain available after updating or moving the app
- Added a guided, confirmed return to vanilla behavior
- Retained DPI-aware layout and scroll fallback for scaled displays
- Added eleven editable interface languages with automatic English fallback
- Added direct validation of every supported Trait Dungeon reward row and completion route during each build
- Moved temporary pack/unpack work away from Documents when possible, with a writable-folder fallback
- Expanded conflict detection to inspect every non-official PAK instead of relying on filenames
- Changed automatic installation to `DS\Content\Paks\~mods` and safely migrates this app's older root-level PAK
- Verifies a non-loadable temporary copy before disabling the currently installed Progression QOL PAK, then verifies the final destination again
- Inspects Treasure Respawn by its internal file paths; the verified release remains compatible

## What you can configure

- World gathering: plants, ore, and cooking-ingredient nodes
- Enemy materials: ordinary and field-enemy material drops
- Safe chest stacks: stackable rewards from one-time world exploration chests; dungeon chests, unique items, and progression items remain unchanged
- Equipment from dungeons, hunts, raids, and Sudden Missions
- Activity crafting materials, including boss parts, runes, upgrades, Trait stones, and the two hidden underwater dungeons
- Currency EXP Items: Character, Equipment, and Karma EXP items from the three repeatable Currency Dungeons, independently configurable from other crafting materials
- Adventurer's Emblems for the Exchange Shop
- Gold and Mercenary Corps Rank EXP
- Spread Equipment Rolls for more variety within the same total equipment quantity
- Favor Better Rarity for supported mixed-tier equipment pools
- Named profiles for saving and reloading favorite configurations

Equipment is capped at x10 because the game equipment inventory holds 500 items. World-chest scaling is also capped at x10.

## Vanilla defaults

All multipliers open at `x1 — Default`. Spread Equipment Rolls and Favor Better Rarity open **off**, which preserves vanilla behavior. The displayed 90% better-tier value is only used if Favor Better Rarity is enabled.

**Spread off:** multiplied equipment remains one stacked result, matching vanilla reward behavior.

**Spread on:** the same multiplied total is split into several independent selections from the original equipment pool. This can produce more item variety in one clear, but it does not force every item type, add quantity, affect Raid runes, or change equipment stats.

**Favor Better Rarity off:** each supported pool uses its original odds.

**Favor Better Rarity on:** a supported Rare/Epic pool favors Epic, while an Epic/Legendary pool favors Legendary. Supported Raid rune pools are included. The selected percentage is the chance for the better of those two available tiers; quantity and equipment main/substat rolls do not change.

**Raid runes:** Spread Equipment Rolls does not affect them. The Crafting Materials multiplier changes supported rune quantities, while Favor Better Rarity can change supported mixed-tier rune pools.

**Profiles:** save the current controls under a custom name, then load them later from the **Profiles** button. Profiles are readable JSON stored in the current Windows user's Local AppData, so they remain available after replacing or moving the application folder. No username or drive is hard-coded. Portable profiles from an earlier version can be migrated automatically when beside the app or manually with **Import from Older Version**. The configurator always opens at vanilla defaults and loads a saved profile only when you choose it. Loading a profile does not install anything; use **Build + Install** afterward.

**Returning to vanilla:** choose **Reset to Vanilla Defaults**, then select **Build + Install**. If the Progression QOL PAK is installed, the app asks before disabling and backing up that exact file. Other mods are not changed. The built-in Default state is the vanilla control state, not a saved profile.

## Install

1. Install the Microsoft .NET 8 Desktop Runtime (x64) if needed.
2. Extract the complete archive. Do not run it from inside the ZIP.
3. Close DragonSword: Awakening.
4. Run `DragonSword.ProgressionQoL.exe`.
5. Confirm the game folder and choose your settings.
6. Select **Build + Install**, review any conflict warning, and confirm.

The app installs one file to `DS\Content\Paks\~mods`:

`DS_ZZZ_ProgressionQoL_Configured_P.pak`

To change settings later, open the app and use **Build + Install** again. The previous project PAK is disabled and backed up automatically. No manual deletion is required.

## Compatibility

This release was validated against DragonSword: Awakening 1.0.10, Steam build 24831799. A newer or unverifiable build receives a compatibility warning but may continue; a known older build must be updated first. The warning exists because game updates can change complete reward tables. If rewards behave unexpectedly on a newer build, restore vanilla behavior and watch this page for an update.

Because two PAKs that replace the same Unreal reward tables cannot be row-merged at load time, disable any file identified by the built-in conflict scan before playing.

## Fragment reward scope

Fragment rewards are included only where their repeatable source is verified. Fragment of Sacred Light from supported Sudden Missions follows Crafting Materials, and verified Fragment of Memory rows in one-time world exploration chests follow Safe Chest Stacks. One-time, story, unknown, and unverified fragment routes—including Whirling Thoughts—are intentionally not multiplied.

## Transparent and offline

- No internet connection, telemetry, or automatic updater
- No administrator request
- No installer service or registry changes
- No DLL injection or game executable modification
- No obfuscation
- Other PAKs are scanned read-only and never changed
- Generated and installed PAKs are SHA-256 verified
- Full source code and third-party license notices are provided

The archive includes the open-source `repak` utility for local PAK creation and verification. It is not downloaded at runtime.

## Independent work

Progression QOL is independently authored from unmodified game-table baselines and nectarines' own Dungeon QOL target work. It does not require, ingest, copy, or redistribute another mod. It includes no third-party mod files, settings, branding, descriptions, screenshots, or artwork.

The original fantasy reward-forge artwork was AI-generated specifically for this project and contains no game characters or logos. OpenAI Codex assisted with code, UI implementation, test tooling, and documentation. The application is unobfuscated and the matching source release is available for inspection.

Source and issue tracker: https://github.com/Nectarines-NAL/DragonSword-Progression-QOL

## Uninstall

Close the game and remove `DS_ZZZ_ProgressionQoL_Configured_P.pak` from `DS\Content\Paks\~mods`. If an older release left the same filename directly in `DS\Content\Paks`, remove that copy too. Disabled backups are optional and stored in `DS\Content\Paks\ProgressionQoL-Backups`.

If something looks wrong, tell us the activity or dungeon and what you expected versus what happened. We are happy to look into it and will ask for a build report only if it is actually needed.
