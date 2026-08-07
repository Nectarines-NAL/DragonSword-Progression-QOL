# Command-line switches

The normal user workflow requires no command-line arguments.

- `--build-test <output-folder>` builds and verifies a fixed validation configuration. It never installs or changes game files.
- `--ui-snapshot <png-path>` captures the Rewards tab for interface review.
- `--ui-snapshot-wide <png-path>` captures a wide Rewards tab with default values.
- `--ui-snapshot-showcase <png-path>` captures a wide Rewards tab with clearly non-default demonstration values.
- `--ui-snapshot-build <png-path>` captures the Build & Install tab using sanitized example paths.
- `--ui-snapshot-audit <png-path>` captures the Audit Log using sanitized example paths.

The screenshot switches exist only to produce consistent public documentation images. They do not build, install, scan the user's game, or make network requests.
