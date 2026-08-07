# Progression QOL — Reward Configurator

Created by **nectarines**

Progression QOL lets you build one custom reward PAK instead of choosing between a pile of fixed multiplier files. Every setting starts at vanilla behavior, so you decide exactly what changes.

## What you can configure

- World gathering: plants, ore, and cooking-ingredient nodes
- Enemy materials: ordinary and field-enemy material drops
- Safe chest stacks: stackable chest rewards while known unique and progression items remain unchanged
- Equipment from dungeons, hunts, raids, and Sudden Missions
- Activity crafting materials, including boss parts, runes, upgrades, and XP items
- Adventurer's Emblems for the Exchange Shop
- Gold and Mercenary Corps Rank EXP
- Spread Equipment Rolls for more variety within the same total equipment quantity
- Favor Better Rarity for supported mixed-tier equipment pools

Equipment is capped at x10 because the game equipment inventory holds 500 items. World-chest scaling is also capped at x10.

## Vanilla defaults

All multipliers open at `x1 — Default`. Spread Equipment Rolls and Favor Better Rarity open **off**, which preserves vanilla behavior. The displayed 90% better-tier value is only used if Favor Better Rarity is enabled.

**Spread off:** multiplied equipment remains one stacked result, matching normal reward behavior.

**Spread on:** the same multiplied total is split into several independent selections from the original equipment pool. This can produce more item variety in one clear, but it does not force every item type and does not add extra quantity.

**Favor Better Rarity off:** each supported pool uses its original odds.

**Favor Better Rarity on:** a Rare/Epic pool favors Epic, while an Epic/Legendary pool favors Legendary. The selected percentage is the chance for the better of those two available tiers; quantity does not change.

## Install

1. Install the Microsoft .NET 8 Desktop Runtime (x64) if needed.
2. Extract the complete archive. Do not run it from inside the ZIP.
3. Close DragonSword: Awakening.
4. Run `DragonSword.ProgressionQoL.exe`.
5. Confirm the game folder and choose your settings.
6. Select **Build + Install**, review any conflict warning, and confirm.

The app installs one file to `DS\Content\Paks`:

`DS_ZZZ_ProgressionQoL_Configured_P.pak`

To change settings later, open the app and use **Build + Install** again. The previous project PAK is disabled and backed up automatically. No manual deletion is required.

## Compatibility

This release candidate has been tested with DragonSword: Awakening 1.0.8. Because another PAK that replaces the same Unreal reward tables cannot be row-merged at load time, disable any file identified by the built-in conflict scan before playing.

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

## Uninstall

Close the game and remove `DS_ZZZ_ProgressionQoL_Configured_P.pak` from `DS\Content\Paks`. Disabled backups are optional and stored in `DS\Content\Paks\ProgressionQoL-Backups`.

Please include `BUILD-REPORT.json`, the Audit Log, game version, and selected settings with bug reports.
