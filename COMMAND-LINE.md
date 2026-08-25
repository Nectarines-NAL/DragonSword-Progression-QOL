# Command-line switches

The normal user workflow requires no command-line arguments.

- `--build-test <output-folder>` builds and verifies a fixed validation configuration. It never installs or changes game files.
- `--build-test-maximums <output-folder>` builds and verifies the maximum supported multipliers. It never installs or changes game files.
- `--build-test-currency-xp <output-folder>` builds a focused x2 Currency EXP Items PAK with every other setting at vanilla.
- `--build-test-underwater <output-folder>` builds a combined x2 Crafting Materials and Gold PAK; validation specifically audits all eight underwater rows within that broader 276-row change set.
- `--validate-languages` validates every external language file, reports missing or unknown keys, and confirms the number of available locales.
- `--ui-snapshot <png-path>` captures the Rewards tab for interface review.
- `--ui-snapshot-wide <png-path>` captures a wide Rewards tab with default values.
- `--ui-snapshot-showcase <png-path>` captures a wide Rewards tab with clearly non-default demonstration values.
- `--ui-snapshot-build <png-path>` captures the Build & Install tab using sanitized example paths.
- `--ui-snapshot-audit <png-path>` captures the Audit Log using sanitized example paths.
- `--ui-snapshot-locale <locale> <png-path>` captures the Rewards tab in a specified locale.
- `--ui-snapshot-locale-tab <locale> <tab-index> <png-path>` captures a selected tab in a specified locale.

The screenshot switches exist only to produce consistent public documentation images. They do not build, install, scan the user's game, or make network requests.
