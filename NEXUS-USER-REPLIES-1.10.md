# Short replies for recent Nexus posts

## Unsupported newer game build

That message comes from the previous configurator's strict build check. The next update replaces the lock on newer game builds with a compatibility warning, so you can choose to continue. The warning is still worth reading because a game update can change reward tables; if anything looks wrong, restore vanilla behavior and watch the mod page for a validated update.

## xnebirosx — no Trait Dungeon rewards

Thanks for flagging this. I have an update in testing that verifies every supported Trait Dungeon reward row before it builds the PAK. For now, please try one Trait Dungeon with other reward mods disabled. If it still happens, just tell me which Trait Dungeon you used and whether its reward preview was already empty before entering. I am happy to keep looking into it.

## elknot / doilaaa — missing temporary GameData folder

Thanks—another player has now reported the same build error. The next update moves temporary packing files away from Documents when possible and includes a writable-folder fallback. You should not need to change your system setup. Please try the updated configurator when it is posted, and let me know if the same message returns.

## wahrheit98 — PAK works only from `~mods`

Thank you, this was useful. The next update installs automatically to `DS\Content\Paks\~mods` and safely backs up any older Progression QOL copy left directly in `Paks`. You will not need to move the file manually after updating.

## vreaper1 — Linux/Proton and the old Dungeon Rewards QOL

The missing Trait rewards came from the older Dungeon Rewards QOL tables after the game update, so removing that old PAK was the right move. Progression QOL generates a normal PAK that works independently of the configurator once built. A native Linux builder is not part of this update, but I am keeping the request in mind and looking at cleaner options than requiring a Windows VM.

## Santar0 — restore pre-1.0.9 reward values

I understand the request, but I do not want to restore the old complete tables because they can overwrite newer dungeon and achievement data. A safe slower-progression option would need to be rebuilt from the current tables instead. I will keep it as a possible future setting rather than shipping stale game data.

## a8542 — additional languages

Thank you for the suggestion. The next update adds Simplified Chinese, Traditional Chinese, Japanese, Korean, Russian, Spanish, French, German, Thai, Portuguese (Brazil), and English. The language files are editable UTF-8 JSON, so players can correct translations or add another language without recompiling the app. Initial community review is welcome.

## Simple follow-up for an unclear reward report

Thanks for letting me know. Please tell me the activity or dungeon and what you expected versus what happened. That is enough for me to start checking it, and I can ask one focused follow-up if I need anything else.
