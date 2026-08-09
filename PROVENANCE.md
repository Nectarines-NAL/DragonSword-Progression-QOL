# Independent provenance

DragonSword Progression QOL is independently authored by nectarines with OpenAI Codex assistance.

## Included inputs

- Unmodified DragonSword: Awakening game tables used as a transformation baseline.
- Activity target definitions developed for nectarines' Dungeon QOL project.
- Project-authored C# source, documentation, interface, and test tooling.
- Original AI-generated fantasy artwork created specifically for this project.
- The separately licensed open-source `repak` utility.
- Null993's separately licensed open-source [DragonSword PAK Tool](https://github.com/Null993/DragonSword-Pak-Tool), packaged as a standalone helper for DragonSword-specific official PAK extraction; it uses CUE4Parse under Apache-2.0.

## Runtime compatibility inputs

No third-party mod is bundled or required. During a local build, installed active PAKs are inspected read-only. If one replaces a managed table, its differences from the verified vanilla baseline are merged into the user's generated PAK before Progression QOL changes are applied. Temporary extracted files are deleted before the build completes; source PAKs are never changed.

No third-party mod archives, code, branding, descriptions, screenshots, or artwork are distributed with this project.

## Derivation rules

- Enemy material targets are selected from unmodified reward rows marked as field drops and restricted to supported stackable material item types.
- Gathering targets are selected from positive unmodified collection-node item/count rows with supported stackable material types.
- Safe chest targets are limited by allowlisted vanilla relationships, while unique and progression rewards are excluded.
- Activity targets are categorized into equipment, materials, emblem currency, gold, and rank experience.

Progression QOL targets remain derived from the audited vanilla baseline. Compatibility merging uses only active PAKs already present in the selected game installation and records their names in `BUILD-REPORT.json`.
