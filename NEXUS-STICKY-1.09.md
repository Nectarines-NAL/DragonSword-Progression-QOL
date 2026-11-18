# PROGRESSION QOL 0.9.2 RC3 — DRAGONSWORD 1.0.9 UPDATE

Progression QOL has been rebuilt and tested for DragonSword: Awakening 1.0.9, Steam build 24693558.

## What changed

- Rebuilt every bundled baseline from the unmodified 1.0.9 game tables.
- Updated the Normal, Currency, and Trait dungeon reward routes that changed in 1.09.
- Restored the activity multipliers for equipment, crafting materials, Adventurer's Emblems, Gold, EXP, and Trait stones.
- Updated Hunts, Raids, and Sudden Missions.
- Preserved the new Nightmare Barduk achievement reward.
- Fixed World Gathering x20.
- Fixed Favor Better Rarity when Spread Equipment Rolls is also enabled.
- Added named profiles. Saved profiles remain available after replacing or moving the app.
- Added Reset to Vanilla Defaults. Building at full defaults now offers to disable and back up the installed Progression QOL PAK.
- Improved the layout for Windows display scaling and added scrolling when a smaller display needs it.
- Added an offline Steam build check. The app will not automatically install tables built for the wrong game version.

## Before installing

1. Close the game.
2. Extract the entire download; do not run the app from inside the ZIP.
3. Disable any older reward-table PAKs or patches reported by the conflict scan.
4. Run `DragonSword.ProgressionQoL.exe`, choose your settings, and select **Build + Install**.

You do not need to delete an older Progression QOL PAK first. Build + Install replaces this project's installed PAK and preserves the previous copy in its backup folder.

## Quick control notes

- **Spread Equipment Rolls** increases equipment variety within the selected total. It does not add quantity, force unique item types, affect Raid runes, or change equipment stats.
- **Favor Better Rarity** changes supported mixed-tier odds. It does not change quantity or equipment stats.
- **Safe Chest Stacks** affects stackable contents from one-time world exploration chests. It does not affect dungeon chests, unique items, or progression items.
- **Crafting Materials** covers supported activity materials such as boss parts, runes, upgrades, Trait stones, and EXP items.

Source code and documentation:
https://github.com/Nectarines-NAL/DragonSword-Progression-QOL

When reporting a problem, please include your game version, selected settings, Audit Log, and `BUILD-REPORT.json`. Tell us the exact activity and difficulty you tested.
