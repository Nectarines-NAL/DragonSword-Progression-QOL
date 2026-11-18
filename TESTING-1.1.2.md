# 0.9.7 RC1 — DragonSword 1.1.2 offline validation

Official installed Steam build: 26210326. Runtime title/build correspondence
was independently observed in the separate authorized mod-9 session; this
project did not launch the game or modify its installation/saves.

The current executable supplied a working archive-key candidate. Applying
the existing DragonSword custom AES/index decoder and registering each PAK
independently mounted all 50 official archives and indexed 328,188 files.
71 reward-system client TABLE/server XML files were extracted. No installed
mod archive was used as a baseline. Extraction inventory records file sizes
and SHA-256. Private key files are excluded from the release and source ZIP.

Compared with the old official 1.0.10 baseline, current RewardRandomData adds
22 groups, RewardData adds 13 entries, and GameItemData adds six entries and
changes 394 existing entries. PropCollectData is unchanged. The refreshed
generator derives 834 activity rows across 575 random groups, including all
36 visible Hunt maps; 431 safe chest rows, 31 rarity groups, 48 gathering
targets, and the existing 78 isolated Tower rows remain supported. Hunt-rank
and unique progression reward routes remain vanilla rather than being
selected as repeat rewards merely because they contain a familiar item.

Passed offline checks:

- Release compilation; five build/pack/list/unpack/SHA-256 checks: maximum,
  showcase, Currency EXP x2, underwater x2, and Tower x2.
- All 2,704 official random groups and every row's non-quantity identity survive.
- Ten newly added non-activity groups and all 13 new RewardData routes remain
  semantically identical to current official data under every tested profile.
- Client/server semantic parity for RewardRandomData, RewardData and
  PropCollectData, without relying on whole-table physical ordering.
- Tower-only changes exactly 78 groups; Currency EXP-only changes 18 rows.
- Existing Trait checks cover 135 targets and 36 completion routes per build.
- Nightmare Barduk achievement route 2610811 remains unchanged.
- Ten inert-folder install/recovery checks: exact build recognition, verified
  replacement, legacy-file migration, disabled backups preserving prior bytes,
  older-build refusal, newer-build confirmation, blocked-write preservation,
  vanilla restore and unrelated-mod preservation.
- Eleven language packs load. Five packs retain previously incomplete strings
  and fall back to English; this is not a claim of complete translation.

Runtime testing: NOT RUN for this mod. This release repairs the stale table
baseline and target coverage offline; it does not claim observed resolution
of Karma-shop crashes, Ryza display symptoms or the older Trait report.
Reports remain open pending evidence. Before any separately authorized game
test, back up saves and isolate the generated PAK from other reward-table mods.
Check Karma-shop scrolling/Ryza entry, new Hunt rewards, one Trait Dungeon,
Currency EXP, Tower claims and vanilla recovery. Do not grant collected
one-time rewards again or promise retroactive recovery of missing rewards.
