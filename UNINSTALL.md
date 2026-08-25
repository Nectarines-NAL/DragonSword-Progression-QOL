# Uninstallation

Close DragonSword: Awakening, then delete this one file from the game folder:

```text
DS\Content\Paks\~mods\DS_ZZZ_ProgressionQoL_Configured_P.pak
```

Optional disabled backups are stored here and cannot load while their names end in `.pak.disabled`:

```text
DS\Content\Paks\ProgressionQoL-Backups
```

Older releases may have placed the same owned filename directly in `DS\Content\Paks`; remove that copy as well if present. Delete the backup folder only if you no longer want prior Progression QOL configurations. No registry entries, services, injected DLLs, or files outside the selected build folder, per-user application data, and these owned game locations are created.
