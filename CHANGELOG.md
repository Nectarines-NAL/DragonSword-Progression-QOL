# Changelog

## Unreleased

- Added high-DPI window sizing, scrollable content areas, and an adaptive footer so every component remains reachable at high display scaling.
- Added read-only discovery and vanilla-relative three-way merging for active mod PAKs that replace managed resources; Progression QOL changes are applied after merged changes.
- Added merged-PAK provenance to build reports and fail-safe handling for unreadable overlaps or mods added after a build.
- Added an embedded AES key, automatic game-directory discovery with manual correction, and selectable static or current-game baselines.
- Added a self-contained build of Null993's open-source DragonSword PAK Tool; current-game baseline extraction now reads only pakchunk108 and pakchunk109 instead of attempting to open pakchunk0 with standard repak.

## 0.9.0-rc.1 — 2026-08-06

- Consolidated configuration into one generated PAK.
- Added independent controls for gathering, enemy materials, safe chest stacks, equipment, activity crafting materials, Adventurer's Emblems, gold, and rank experience.
- Added optional Spread Equipment Rolls and Favor Better Rarity behavior.
- Added path normalization, read-only conflict scanning, PAK listing/unpack verification, and SHA-256 verification.
- Added one-action Build + Install flow with explicit confirmation and owned-file backups.
- Set every reward control to vanilla behavior at first launch.
- Added original project artwork, release documentation, provenance disclosure, and security notes.
