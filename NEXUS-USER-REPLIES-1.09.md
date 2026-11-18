# Paste-ready replies for the 1.09 release

## Hakairoo — how much did the official patch increase rewards?

The patch notes do not give one percentage because the changes vary by activity and reward. Comparing the unmodified tables, gathering nodes were doubled. Dungeon materials generally increased around 1.7–2.9x, Currency Dungeon Gold around 2.5–3x, Hunt materials around 1.6–3.4x, and Raid materials around 1.4–2.7x; Raids also gained 150,000–350,000 Gold depending on the raid. Equipment roll counts were not generally increased. Our x2/x5/etc. settings multiply the new 1.09 values, not the older values.

## Ognavaa — Accumulated Fame shows 210/200

That is vanilla 1.09 behavior, not extra Fame created by the mod. The new Nightmare Barduk achievement awards 10 Combat Fame plus the Conqueror of Barduk title, while 200 remains the displayed final Fame milestone. Your save correctly contains 210 points; there is nothing you need to remove. It may continue to display 210/200 until the developers adjust the milestone display in a future patch.

## Zalaryan — Nightmare Barduk achievement disappears

Confirmed and fixed for the 1.09 release. The configurator is now rebuilt from the unmodified 1.09 tables, so the Nightmare Barduk achievement reward remains present.

## Moenalvein — Favor Better Rarity and Spread Equipment Rolls

Confirmed and fixed. Spread was rebuilding its selections from the original rarity pool, which could override Favor Better Rarity. Spread now keeps the selected better-tier chance.

## DigiGo0n — Loot Shape and Safe Chest Stacks

Loot Shape only changes eligible reward selection. Spread changes equipment variety, and Better Rarity changes supported mixed-tier odds; neither changes equipment main/substat rolls. Safe Chest Stacks applies to stackable rewards in one-time world exploration chests, not dungeon chests.

## Julian2580 — activity Gold, Emblems, materials, and EXP

Confirmed and fixed for 1.09. The update moved several repeat rewards to new activity routes; the new release covers those routes for Gold, Emblems, crafting materials, EXP, and equipment.

## Kafkasniffer — World Gathering x20

Confirmed and fixed. World Gathering x20 is now applied across all supported gathering rows in the 1.09 release.

## Sensitiveshiori — Karma amounts

If you mean Karma EXP items awarded by supported activities, those are controlled by Crafting Materials. The configurator does not change direct character Karma or progression values.

## JaK05 — Spread and Raid runes

Spread Equipment Rolls does not affect Raid runes. Crafting Materials controls supported rune quantities; Favor Better Rarity can affect supported mixed-tier rune pools.

## Wahrheit98 — equipment multiplier remains vanilla

Confirmed and fixed for 1.09. Several equipment rewards moved to new activity routes, and the current release now applies the selected multiplier to them.

## LeonTrig — world works, Trait dungeons and Hunts do not

Confirmed and fixed for 1.09. Trait dungeons, Hunts, Currency dungeons, and the other repeat-reward routes have been rebuilt from the new game tables.

## WhatAreWii — Trait stones disappear

Confirmed and fixed. Trait stone rewards are preserved and multiplied correctly in the 1.09 release.

## Text configuration request

A normal PAK cannot read an external text file while the game is running; that would require a runtime loader or injected code, which this project intentionally avoids. Named profiles now provide reusable settings while keeping the generated PAK fully offline.

## Underwater dungeons request

Not in the current release. The 1.09 tables do not expose standard reward IDs for those entries, so supporting them would require inventing behavior rather than safely multiplying an existing reward.

## SongJoJo — multiplayer Raid rewards

Only your local rewards are changed. The PAK does not alter another player's drops.

## FayMods — are older reward files still needed?

No. This is intended to be one configurable reward PAK. Disable older reward-table files and patches first so only one mod owns those tables.

## Demetricus — display scaling and scroll bars

The 1.09 release adds DPI-aware layout fixes and a scroll fallback for smaller or scaled displays.

## AuraFort — compatibility with another full-table mod

Two PAKs that replace the same reward tables cannot merge safely at game load. Use only one, and follow the configurator's conflict warning before installing.
