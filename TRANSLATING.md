# Translating Progression QOL

Progression QOL language packs are plain UTF-8 JSON files in the `Languages` folder beside the application. They contain text only: the application does not execute scripts, markup, or commands from a language file.

## Included languages

The included set matches the eleven interface languages listed for DragonSword: Awakening on Steam:

- English (`en-US`) — authoritative source
- Japanese (`ja-JP`)
- Korean (`ko-KR`)
- Simplified Chinese (`zh-CN`)
- Traditional Chinese (`zh-TW`)
- French (`fr-FR`)
- German (`de-DE`)
- Spanish — Spain (`es-ES`)
- Russian (`ru-RU`)
- Thai (`th-TH`)
- Portuguese — Brazil (`pt-BR`)

The first non-English files are project-supplied initial translations, not claimed as native-reviewed translations. Missing or invalid entries automatically fall back to the embedded English source. Community corrections are welcome.

## Improve an existing translation

1. Close Progression QOL.
2. Make a backup of the language file.
3. Open the JSON file in a UTF-8 text editor.
4. Edit only the text on the right side of entries inside `strings`.
5. Keep every key on the left unchanged.
6. Preserve placeholders such as `{name}`, `{count}`, `{detected}`, `{required}`, `{language}`, `{base}`, and `{conflicts}` exactly.
7. Save valid JSON and restart the application.

Example:

```json
"footer.build": "BUILD + INSTALL"
```

Only change `BUILD + INSTALL`. Do not rename `footer.build`.

## Add another language

Copy `en-US.json` to a new filename and update:

- `locale`: a distinct locale identifier, such as `it-IT`;
- `nativeName`: the language name written in that language;
- `englishName`: its English name;
- `reviewStatus`: use `community-reviewed` only after a native review;
- `translatorCredit`: the contributor name or group;
- `fontFamily`: a Windows UI font that supports the script;
- values inside `strings`.

The application discovers valid `*.json` files in `Languages` when it starts. A custom pack may be incomplete; missing keys use English.

## Validation

From the complete application folder, run:

```powershell
.\DragonSword.ProgressionQoL.exe --validate-languages
```

The validator reports malformed files, duplicate locales, missing keys, and unknown keys. Each language file is limited to 512 KiB and must use schema version 1.

## Contributing

Submit corrections through the GitHub repository with the locale, translator credit, and native-review status clearly identified:

https://github.com/Nectarines-NAL/DragonSword-Progression-QOL
