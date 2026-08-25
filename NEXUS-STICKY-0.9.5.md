# PROGRESSION QOL 0.9.5 RC1 — LANGUAGE SUPPORT

Progression QOL now supports multiple interface languages.

This changes the Progression QOL configurator interface only. It does not change DragonSword's in-game language.

Included languages:

- English
- Japanese
- Korean
- Simplified Chinese
- Traditional Chinese
- French
- German
- Spanish (Spain)
- Russian
- Thai
- Portuguese (Brazil)

The application detects your Windows display language automatically. You can also choose a language manually from the new **Language** tab. Your selection is saved for future versions of the application.

English remains the authoritative source. The included non-English files are initial project translations and have not all received native-speaker review. Missing or invalid text safely falls back to English. Translation corrections are welcome.

Other changes:

- Added editable UTF-8 JSON language files.
- Improved control sizing for translated text and Windows display scaling.
- Added validation for malformed files, duplicate languages, missing text, and unknown entries.
- Added an automatic check of every supported Trait Dungeon reward row and completion route during each build.
- Improved build reliability on protected or synchronized Documents folders.
- Expanded the installed-mod scan so conflicting reward tables cannot hide behind an unexpected filename.
- The scanner now verifies Treasure Respawn by its internal files instead of trusting the filename; the verified version remains compatible.
- Automatic installation now uses `DS\Content\Paks\~mods` and safely backs up this app's older root-level PAK.
- A replacement PAK is copied and hash-verified before the currently installed Progression QOL PAK is disabled.
- Newer or unverifiable game builds now show a compatibility warning instead of being locked out. Known older builds must still be updated first.
- Confirmed that this interface update does not change reward generation or existing profiles.

If you continue on a newer game build, the configurator may be out of date and rewards may not work as intended. If anything looks wrong, restore vanilla behavior and watch the mod page for an update.

## ADDING OR EDITING A LANGUAGE

Yes—players can add new languages or improve the included translations without rebuilding the application.

1. Close Progression QOL.
2. Open the `Languages` folder beside `DragonSword.ProgressionQoL.exe`.
3. Copy `en-US.json` and rename the copy for the new locale, such as `it-IT.json`.
4. Update the language information at the top of the file.
5. Translate only the text on the right side of each entry inside `strings`.
6. Do not rename the keys on the left or alter placeholders such as `{name}`, `{count}`, or `{required}`.
7. Save the file as valid UTF-8 JSON and restart the application.

Incomplete community language files are supported. Any missing entry displays the English version instead.
Keep a backup of custom language files before replacing the complete application folder with a newer release.

Optional validation command:

```powershell
.\DragonSword.ProgressionQoL.exe --validate-languages
```

Full translation guide, source code, and contributions:
https://github.com/Nectarines-NAL/DragonSword-Progression-QOL
