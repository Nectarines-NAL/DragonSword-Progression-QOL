# Security and audit notes

## Runtime behavior

- Local filesystem access only.
- No network client, telemetry, update service, or remote code path.
- No administrator manifest, registry mutation, scheduled task, service, DLL injection, or game executable patch.
- Other PAKs are scanned read-only and are never moved, renamed, disabled, or deleted.
- Installation is user-confirmed and limited to the project-owned PAK filename.
- Returning to vanilla is also user-confirmed and moves only that exact project-owned PAK to a disabled backup.
- Replaced project PAKs are preserved with a non-loadable `.pak.disabled` suffix.
- A replacement PAK is first copied under a non-loadable temporary name and checked with SHA-256 before the current Progression QOL PAK is disabled; the final installed file is checked again.
- Optional named profiles are plain JSON files written only to the current user's `%LOCALAPPDATA%\nectarines\DragonSword Progression QOL\Profiles` folder. They contain configuration values only.
- Temporary pack/unpack files use `%LOCALAPPDATA%\nectarines\DragonSword Progression QOL\BuildCache` when writable and are removed after a verified build. If that location is protected, the app falls back to a temporary folder inside the selected build output.
- Legacy profile migration validates JSON before copying, never deletes the source, and never overwrites an existing same-named persistent profile.

## Executables

`DragonSword.ProgressionQoL.exe` is an unobfuscated .NET 8 WinForms application. Its complete C# source is included in the source release and can be inspected with standard .NET tools.

`Tools\repak.exe` is a pinned open-source Unreal PAK utility used only for local pack, list, and unpack operations. Its upstream project and license texts are listed in `THIRD-PARTY-NOTICES.txt`.

`Languages\*.json` files contain display text only. They are limited to 512 KiB, parsed as JSON schema 1, and never executed as code, commands, scripts, or markup. Invalid language files are ignored and the application falls back to an English source embedded in the executable.

## Reporting a problem

Start with the affected activity or dungeon and what you expected versus what happened. The author may ask for `BUILD-REPORT.json` if the first check does not explain the issue. Do not include personal paths or account information in public reports.
