# Installation

## Requirements

- Windows 10 or Windows 11, 64-bit
- DragonSword: Awakening
- Microsoft .NET 8 Desktop Runtime, x64
- About 15 MB for the application, plus space for generated builds

## First installation

1. Download the application archive and compare its SHA-256 value with the value shown on the release page.
2. Extract the entire archive to a normal folder. Keep `Assets`, `Baselines`, and `Tools` beside the application.
3. Close the game.
4. Run `DragonSword.ProgressionQoL.exe`. No administrator rights are needed.
5. Open **Build & Install** and confirm the detected DragonSword installation.
6. Select reward settings on **Rewards**. Every setting initially preserves vanilla behavior.
7. Select **Build + Install**.
8. Review any conflict warning, then confirm the installation.

The application creates and verifies a PAK in the selected build-output folder before copying it to:

```text
<DragonSword folder>\DS\Content\Paks\DS_ZZZ_ProgressionQoL_Configured_P.pak
```

## Changing settings

Open the configurator, choose new values, and select **Build + Install** again. The installed PAK does not need to be deleted manually. The application disables and backs up only its own previous PAK, installs the new one, and verifies that the installed SHA-256 matches the build.

## Conflict warnings

The scanner reads PAK listings in `DS\Content\Paks` and `DS\Content\Paks\~mods`. It warns when another likely PAK contains a table managed by this application. It does not alter that PAK. Disable a conflicting PAK yourself before launching the game.

## Troubleshooting

- **The app does not start:** install the Microsoft .NET 8 Desktop Runtime (x64), not only the general .NET runtime.
- **A required file is missing:** extract the complete archive again. Antivirus may quarantine unfamiliar executables; inspect the detection and archive hashes before restoring anything.
- **The game folder is rejected:** browse to the game root or to `DSClient-Win64-Shipping.exe`. The expected executable is under `DS\Binaries\Win64`.
- **The game is running:** close it before installation so the PAK can be replaced safely.
- **Build succeeds but installation is skipped:** use **Build + Install** again and accept the confirmation prompt.
- **Settings appear unchanged:** inspect **Audit Log**, confirm an `Installed:` line is present, and disable any PAK reported as editing the same reward tables.

Generated builds include `BUILD-REPORT.json`, `INSTALL.txt`, and the verified PAK. Preserve the report when filing a bug.
