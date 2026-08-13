# DragonSword Progression QOL

An offline reward configurator for **DragonSword: Awakening**, created by **nectarines**.

Version `0.9.2-rc.3` is a test candidate rebuilt from unmodified game version 1.0.9, Steam build `24693558`. It builds one custom Unreal PAK from independently maintained targets and official game-table baselines. Every multiplier starts at vanilla `x1`; both optional loot-shape features start off.

## Features

- World gathering and ordinary enemy materials: x1–x20.
- Safe stackable world-chest rewards: x1–x10. Known unique and progression rewards remain unchanged.
- Dungeon, hunt, raid, and Sudden Mission equipment, crafting materials, Adventurer's Emblems, gold, and Mercenary Corps Rank EXP.
- Equipment is capped at x10 because the equipment inventory holds 500 items.
- Optional Spread Equipment Rolls turns a multiplied equipment stack into several independent selections from the same pool, allowing more variety without forcing unique item types.
- Optional Favor Better Rarity shifts supported mixed pools toward their better available tier without adding quantity.
- One generated PAK for any supported configuration.
- Named configuration profiles stored as plain, readable JSON in the current Windows user's persistent Local AppData folder.
- PAK contents and SHA-256 are verified before installation.
- The app owns one installed file: `DS_ZZZ_ProgressionQoL_Configured_P.pak`.

## Install and use

1. Install the Microsoft [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) if it is not already present.
2. Extract the complete download to a normal folder. Do not run the app inside the ZIP.
3. Close DragonSword: Awakening.
4. Run `DragonSword.ProgressionQoL.exe`.
5. Confirm the detected game location or browse to the game root, `DS`, `Paks`, `~mods`, `Win64`, or the game executable.
6. Choose reward settings. `x1 — Default` and unchecked options preserve vanilla behavior.
7. Select **Build + Install**.
8. Review the conflict scan and confirm installation.

Use **Profiles** to save the current reward settings under a custom name, load a saved profile, delete a profile, reset to vanilla defaults, import profiles from an older extracted release, or open the profile folder. Profiles are stored under `%LOCALAPPDATA%\nectarines\DragonSword Progression QOL\Profiles`, resolved separately for each Windows user and retained when the application folder is replaced or moved. The app always starts at vanilla defaults and never auto-loads a saved profile. Loading or deleting a profile does not build, install, or remove a PAK; only **Build + Install** changes the app's owned game file.

On first launch after upgrading from the portable profile format, the app safely copies validated JSON profiles found in a `Profiles` folder beside that executable into persistent storage. Old files are preserved and existing persistent profiles are never overwritten. If the older release is in another folder, use **Profiles → Import from Older Version** and select either its application folder or its `Profiles` folder.

**Reset to Vanilla Defaults** resets the visible controls only. After resetting, use **Build + Install**: if this application's PAK is installed, the app asks whether to disable it and return this mod's rewards to vanilla behavior. The PAK is preserved as a non-loadable backup. Other mods are never changed.

The PAK is installed to `DS\Content\Paks`. Reconfiguring does not require manual deletion: the current owned PAK is moved to `DS\Content\Paks\ProgressionQoL-Backups` with a non-loadable `.pak.disabled` extension, then the new PAK is installed and hash-verified.

See [INSTALLATION.md](INSTALLATION.md) for troubleshooting and [UNINSTALL.md](UNINSTALL.md) for complete removal.

## Transparent security model

The application:

- is an unobfuscated, framework-dependent .NET 8 WinForms application;
- makes no network connections and contains no telemetry or updater;
- requests no administrator rights;
- performs no DLL injection or game-executable modification;
- uses no installer service and makes no registry changes;
- never downloads a dependency;
- stores optional profiles as unobfuscated JSON in the visible per-user `%LOCALAPPDATA%\nectarines\DragonSword Progression QOL\Profiles` folder;
- scans other PAKs read-only and never disables, deletes, or moves them;
- installs only after explicit confirmation.

The bundled `repak.exe` is the open-source Unreal PAK tool by trumank. It is used locally to pack, list, unpack, and verify the generated PAK. Its complete MIT and Apache-2.0 license texts are included.

## Compatibility and conflicts

The generated PAK may contain these game paths:

- `DS/Content/Design/GameData/RewardRandomData.table`
- `DS/Content/Design/GameData/RewardData.table` when Spread is enabled
- `DS/Content/Design/GameData/PropCollectData.table` when gathering is changed
- matching generated server XML files

Another PAK editing the same tables conflicts at the file level; Unreal PAK load order does not merge individual rows. The built-in scanner lists likely PAKs and verifies their internal paths before warning.

The table baseline was extracted from unmodified game version 1.0.9, Steam build `24693558`. Automatic installation checks the local Steam manifest and refuses a different build because a game update can change these full reward tables. Building alone never modifies the game.

## Independent provenance

- Activity targets come from the Dungeon QOL work authored by nectarines in this workspace.
- Enemy-material and gathering targets are derived from unmodified game-table fields and item classifications.
- Safe chest and rarity targets are derived from unmodified game-table relationships.
- The application does not ingest another mod, require another mod, or include another mod's files, settings, branding, or artwork.
- Release artwork is an original AI-generated fantasy reward-forge scene created for this project. It contains no game characters, logos, or third-party mod assets.

See [PROVENANCE.md](PROVENANCE.md) for the detailed boundary and [SECURITY.md](SECURITY.md) for the audit surface.

## Source and development

Build with the .NET 8 SDK:

```powershell
dotnet build .\src\DragonSword.ProgressionQoL\DragonSword.ProgressionQoL.csproj -c Release
```

The `--build-test <output-folder>` switch runs the same build engine with a validation profile. It does not install or change game files. Every supported switch is documented in [COMMAND-LINE.md](COMMAND-LINE.md).

## License

Application source and project-authored documentation are released under the MIT License. See [LICENSE.txt](LICENSE.txt). Third-party components retain their own licenses in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## AI disclosure

OpenAI Codex assisted with application code, interface implementation, testing tools, and documentation. Original release artwork was generated for this project with OpenAI image generation. The source remains unobfuscated and auditable.
