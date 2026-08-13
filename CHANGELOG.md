# Changelog

## 0.9.2-rc.3 — 2026-08-13

- Moved saved profiles to version-independent, per-user Local AppData so future application releases retain them automatically on any Windows installation.
- Added non-destructive migration of valid portable profiles plus a manual Import from Older Version action.
- Replaced the Profiles flat border with an explicitly painted inset border to prevent bottom-edge clipping at scaled DPI.
- Added a confirmed vanilla-restore path: Build + Install at all-default settings disables and preserves the app's installed PAK instead of rejecting the configuration.
- Aligned the Profiles and Build + Install footer buttons.
- Added named configuration profiles with Save, Load, Delete, Reset to Vanilla Defaults, and Open Profiles Folder actions.
- Profiles are validated, human-readable JSON files containing reward settings only; startup remains vanilla and never auto-loads a profile.
- Rebuilt every bundled baseline from unmodified DragonSword 1.0.9 Steam build 24693558.
- Re-derived repeat rewards after 1.09 moved Normal, Currency, and Trait dungeons to direct clear rewards.
- Restored coverage for activity Gold, Emblems, materials, EXP, Trait stones, Hunts, Raids, and Sudden Missions.
- Preserved the new Nightmare Barduk achievement reward added in 1.09.
- Fixed Spread Equipment Rolls cloning vanilla rarity weights when Favor Better Rarity was also enabled.
- Verified world-gathering x20 across all 48 supported gathering rows.
- Added an offline Steam build check; automatic installation refuses unknown game builds.

## 0.9.0-rc.1 — 2026-08-06

- Consolidated configuration into one generated PAK.
- Added independent controls for gathering, enemy materials, safe chest stacks, equipment, activity crafting materials, Adventurer's Emblems, gold, and rank experience.
- Added optional Spread Equipment Rolls and Favor Better Rarity behavior.
- Added path normalization, read-only conflict scanning, PAK listing/unpack verification, and SHA-256 verification.
- Added one-action Build + Install flow with explicit confirmation and owned-file backups.
- Set every reward control to vanilla behavior at first launch.
- Added original project artwork, release documentation, provenance disclosure, and security notes.
