# Installation

## Requirements

- Windows 10 or Windows 11, 64-bit
- DragonSword: Awakening
- Game version 1.0.9, Steam build `24693558`, for this test candidate
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

## Configuration profiles

Select **Profiles** to save the current reward settings under a custom name. Profiles are readable `.json` files stored at `%LOCALAPPDATA%\nectarines\DragonSword Progression QOL\Profiles`. Windows resolves `%LOCALAPPDATA%` independently for every user, so no username, drive letter, or Steam path is hard-coded. Profiles contain reward multipliers, loot-shape switches, and the better-tier percentage only; they do not contain game paths, build-output paths, PAK data, or personal information.

Because the profile location is independent of the extracted application folder, installing a later configurator release automatically retains saved profiles. The first persistent-profile release also copies valid portable profiles found beside its executable without deleting the originals or replacing a same-named persistent profile. To migrate from an older release stored elsewhere, choose **Profiles → Import from Older Version** and select the older application folder or its `Profiles` folder.

The app intentionally starts at vanilla defaults every time. A saved profile changes the controls only after the player explicitly loads it. Loading, deleting, or resetting a profile never changes the installed PAK. After loading a profile, select **Build + Install** to apply those settings to the game.

The built-in **Default** state is not a saved profile. It means every multiplier is x1 and both loot-shape options are off. To return an already-modified game to vanilla, choose **Reset to Vanilla Defaults**, then select **Build + Install**. The app asks before moving its exact installed PAK into `ProgressionQoL-Backups` with a non-loadable `.disabled` extension. It does not create a vanilla PAK or change another mod.

## Conflict warnings

The scanner reads PAK listings in `DS\Content\Paks` and `DS\Content\Paks\~mods`. It warns when another likely PAK contains a table managed by this application. It does not alter that PAK. Disable a conflicting PAK yourself before launching the game.

## Troubleshooting

- **The app does not start:** install the Microsoft .NET 8 Desktop Runtime (x64), not only the general .NET runtime.
- **A required file is missing:** extract the complete archive again. Antivirus may quarantine unfamiliar executables; inspect the detection and archive hashes before restoring anything.
- **The game folder is rejected:** browse to the game root or to `DSClient-Win64-Shipping.exe`. The expected executable is under `DS\Binaries\Win64`.
- **The Steam build is rejected:** update DragonSword or use the configurator release made for that exact build. Automatic installation intentionally refuses unknown builds so an old full-table baseline cannot overwrite new game rewards or achievements.
- **The game is running:** close it before installation so the PAK can be replaced safely.
- **Build succeeds but installation is skipped:** use **Build + Install** again and accept the confirmation prompt.
- **Settings appear unchanged:** inspect **Audit Log**, confirm an `Installed:` line is present, and disable any PAK reported as editing the same reward tables.

Generated builds include `BUILD-REPORT.json`, `INSTALL.txt`, and the verified PAK. Preserve the report when filing a bug.
