# Changelog

## 0.9.5-rc.1 — 2026-08-21

- Added editable UTF-8 JSON interface language packs and an embedded English fallback.
- Matched DragonSword's eleven Steam interface languages: English, Japanese, Korean, Simplified Chinese, Traditional Chinese, French, German, Spanish (Spain), Russian, Thai, and Portuguese (Brazil).
- Added automatic Windows-language detection, a manual Language tab, and persistent per-user language selection.
- Added initial project translations. Japanese, both Chinese scripts, Spanish, and Russian cover the complete string catalog; the additional language packs cover the main interface and safely fall back to English for remaining dialogs pending native review.
- Added safe custom-pack discovery, schema and size validation, duplicate-locale detection, and a `--validate-languages` audit command.
- Expanded translated titles, dropdowns, and footer buttons responsively to reduce clipping across scripts and DPI scales.
- Replaced the hard block on newer or unverifiable Steam builds with a clear compatibility warning and user confirmation. Known older builds remain blocked until the game is updated.
- Changed automatic installation to `DS\Content\Paks\~mods` and safely migrates this application's older root-level PAK into its disabled backup folder.
- Expanded conflict detection to inspect every non-official PAK in `Paks` and `Paks\~mods`, regardless of filename.
- Made replacement installation transactional: the copied PAK is hash-verified before the existing Progression QOL PAK is disabled.
- Inspect Treasure Respawn PAK contents instead of granting compatibility solely from the filename; the verified release remains non-overlapping.
- Moved temporary packing and verification work out of Documents when possible, with a writable build-folder fallback for restricted systems.
- Added a mandatory build-time check for all 135 verified Trait Dungeon reward rows and all 36 Trait completion routes.
- Confirmed the localization update does not alter reward generation: the focused Currency EXP Items PAK retains verified SHA-256 `CE5CCFAAAFFBE8B54E20E824B8C9BF91D83EF66931944D0A95B6D776BA7C4A13`.

## 0.9.4-rc.1 — 2026-08-20

- Revalidated the project against unmodified DragonSword: Awakening 1.0.10, Steam build 24831799.
- Confirmed that every managed reward, dungeon, item, gathering, and chest table is byte-identical to the validated 1.0.9 baseline; only the English localization table changed.
- Added Sunken Ruins and Ruins Beneath the Waves to the existing Activity Gold and Crafting Materials controls.
- Added exactly eight independently verified underwater reward rows: two Gold rows and six material rows. Hidden content is still excluded unless explicitly allowlisted.
- Added the independent Currency EXP Items control developed in 0.9.3 RC1 to the public 1.0.10 candidate.
- Updated the automatic Steam build gate, application branding, manifest provenance, command-line validation, and release documentation for 1.0.10.
- Retained saved-profile compatibility. Existing profiles continue to load; underwater rewards follow their existing Gold and Crafting Materials values.
- Kept fragment handling conservative: verified Sudden Mission Sacred Light and safe world-chest Memory sources remain covered by their existing controls, while unverified or one-time fragment routes are not broadly multiplied.

## 0.9.3-rc.1 — 2026-08-14

- Added an independent Currency EXP Items multiplier for Character, Equipment, and Karma EXP items from repeatable Currency Dungeons.
- Kept Currency EXP items separate from Crafting Materials so the two controls never multiply the same reward row.
- Added explicit activity-category and item-type metadata to the verified 1.09 target manifest.
- Preserved older saved-profile behavior by inheriting the previous Crafting Materials value when an older profile has no Currency EXP Items field.
- Added a focused currency-EXP-only build test and row-isolation validation.

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
