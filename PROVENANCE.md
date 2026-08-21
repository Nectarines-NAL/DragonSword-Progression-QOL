# Independent provenance

DragonSword Progression QOL is independently authored by nectarines with OpenAI Codex assistance.

## Included inputs

- Unmodified DragonSword: Awakening game tables used as a transformation baseline.
- Activity target definitions developed for nectarines' Dungeon QOL project.
- Project-authored C# source, documentation, interface, and test tooling.
- Original AI-generated fantasy artwork created specifically for this project.
- The separately licensed open-source `repak` utility.

## Excluded inputs

The project does not use, copy, patch, redistribute, or depend upon another reward or material mod. No third-party mod tables, configurations, archives, code, branding, descriptions, screenshots, or artwork are included. The conflict scanner reads other PAK file listings only to report path-level overlap; it never ingests their data or changes those files.

## Derivation rules

- Enemy material targets are selected from unmodified reward rows marked as field drops and restricted to supported stackable material item types.
- Gathering targets are selected from positive unmodified collection-node item/count rows with supported stackable material types.
- Safe chest targets are limited by allowlisted vanilla relationships, while unique and progression rewards are excluded.
- Activity targets are categorized into equipment, materials, emblem currency, gold, and rank experience.
- Game 1.0.10 activity targets are derived through `MapDungeonData` clear rewards into `RewardData` and `RewardRandomData`, with first-clear and externally shared groups rejected. Hidden maps remain excluded except the explicitly allowlisted Sunken Ruins (`11901`) and Ruins Beneath the Waves (`11902`) repeat-reward routes.

Builds transform only the selected vanilla-derived targets. The configurator never asks for another mod archive as input.
