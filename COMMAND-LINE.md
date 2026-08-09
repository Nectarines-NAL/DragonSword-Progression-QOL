# Command-line switches

The normal user workflow requires no command-line arguments.

- `--build-test <output-folder> [game-folder]` builds and verifies a fixed validation configuration. When a game folder is supplied, active overlapping mod PAKs are merged as in the UI. It never installs or changes game files.
- `--build-test-current <output-folder> <game-folder>` performs the same validation build after extracting the baseline from the current game's pakchunk108 and pakchunk109 with DragonSword PAK Tool. It never installs or changes game files.
- `--ui-snapshot <png-path>` captures the Rewards tab for interface review.
- `--ui-snapshot-wide <png-path>` captures a wide Rewards tab with default values.
- `--ui-snapshot-showcase <png-path>` captures a wide Rewards tab with clearly non-default demonstration values.
- `--ui-snapshot-build <png-path>` captures the Build & Install tab using sanitized example paths.
- `--ui-snapshot-audit <png-path>` captures the Audit Log using sanitized example paths.

The screenshot switches exist only to produce consistent public documentation images. They do not build, install, scan the user's game, or make network requests.
