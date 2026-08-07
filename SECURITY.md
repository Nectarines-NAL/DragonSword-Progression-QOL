# Security and audit notes

## Runtime behavior

- Local filesystem access only.
- No network client, telemetry, update service, or remote code path.
- No administrator manifest, registry mutation, scheduled task, service, DLL injection, or game executable patch.
- Other PAKs are scanned read-only and are never moved, renamed, disabled, or deleted.
- Installation is user-confirmed and limited to the project-owned PAK filename.
- Replaced project PAKs are preserved with a non-loadable `.pak.disabled` suffix.
- The built and installed files are checked with SHA-256.

## Executables

`DragonSword.ProgressionQoL.exe` is an unobfuscated .NET 8 WinForms application. Its complete C# source is included in the source release and can be inspected with standard .NET tools.

`Tools\repak.exe` is a pinned open-source Unreal PAK utility used only for local pack, list, and unpack operations. Its upstream project and license texts are listed in `THIRD-PARTY-NOTICES.txt`.

## Reporting a problem

Include the application version, game version, `BUILD-REPORT.json`, Audit Log text, and the SHA-256 of the downloaded archive. Do not include personal paths or account information in public reports.
