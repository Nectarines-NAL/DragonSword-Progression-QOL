# Steam Workshop Roadmap

Status: Future planning document  
Project: DragonSword Progression QOL  
Owner: nectarines  
Recorded: 2026-08-24

## Goal

If DragonSword: Awakening adds Steam Workshop support, preserve the project's existing principle: one Progression QOL installation with user-selected reward settings, rather than publishing a separate Workshop item for every multiplier combination.

The existing Windows configurator should not simply be uploaded unchanged. Steam Workshop distributes an item's content folder, but DragonSword's developers must define how the game discovers, mounts, and consumes that content.

Official references:

- Steam Workshop implementation: https://partner.steamgames.com/doc/features/workshop/implementation
- ISteamUGC interface: https://partner.steamgames.com/doc/api/isteamugc

## Preferred architecture

### Best case: official in-game configuration support

If DragonSword exposes a supported mod-settings, scripting, or reward-table API:

1. The player subscribes to one Progression QOL Workshop item.
2. Steam downloads and updates it automatically.
3. An in-game Progression QOL menu controls reward multipliers and Loot Shape settings.
4. Settings are stored per user.
5. The mod applies those settings through the supported game API without producing multiple static PAK variants.

This is the cleanest result. The current cooked DataTable replacement cannot read an arbitrary external configuration file by itself, so this design depends on developer-provided runtime support.

### Practical case: Workshop mounts ordinary PAK files

If Workshop integration only mounts static Unreal PAKs, use one Workshop resource package plus a separate cross-platform configurator.

Possible item structure:

```text
Progression QOL Workshop Item
├── mod.json or developer-required descriptor
├── Baselines
├── Languages
├── Tools
├── Progression QOL Configurator
└── Documentation
```

The configurator should be migrated from Windows Forms to a cross-platform interface, preferably Avalonia on .NET 8, with:

- Windows and native Linux/Steam Deck builds;
- automatic Steam, Workshop, and DragonSword path discovery;
- the existing reward controls and readable profile system;
- Windows Local AppData and Linux XDG-compatible persistent storage;
- game-build compatibility warnings and baseline validation;
- transactional PAK generation, unpack verification, and SHA-256 verification;
- a generated-mod location outside Steam's managed Workshop cache;
- a clear restore-to-vanilla path that changes only Progression QOL's owned output.

Steam should manage the subscribed source resources. User profiles and generated PAKs should remain outside the Workshop cache so a Workshop update cannot erase them.

## Workshop publication flow

Once the developer enables Workshop and documents its content contract:

1. Create the Workshop item through `ISteamUGC::CreateItem` or the developer's supplied tool.
2. Upload updates through `StartItemUpdate`, the relevant `SetItem...` calls, and `SubmitItemUpdate`, or use `steamcmd` when permitted.
3. Publish the content folder, preview artwork, description, tags, compatibility metadata, and version information together.
4. Verify the installed Workshop folder with `ISteamUGC::GetItemInstallInfo` or the game's documented equivalent.
5. Test subscription, update, rollback, unsubscribe, conflict handling, and offline startup on Windows and Steam Deck.

Steam supports allowing a separate editing application to publish into a base game's Workshop when the developer grants that application's App ID permission. This could support an official Progression QOL authoring/configuration tool if DragonSword's developers approve it.

## Information required from the developers

Before implementation, obtain authoritative answers for:

- Workshop consumer App ID and publishing rules;
- accepted Workshop folder structure;
- required mod descriptor or manifest format;
- whether subscribed `.pak` files are mounted automatically;
- expected PAK mount point and naming rules;
- load-order and table-conflict behavior;
- whether the game exposes a persistent user-mod output directory;
- whether mods may generate files outside the Workshop cache;
- whether an in-game settings, scripting, Blueprint, or DataTable override API exists;
- supported Windows, Linux, Proton, and Steam Deck behavior;
- whether native executables or separate configuration tools are accepted;
- whether a separate tool may receive App Publish Permissions.

## Designs to avoid

- Publishing many separate Workshop items for x2, x3, x5, x10, and x20 combinations.
- Writing customized output directly into Steam's managed Workshop cache.
- Shipping only the current Windows Forms executable and calling it Steam Deck compatible.
- Requiring Steam Deck users to install the Windows .NET Desktop Runtime through Proton.
- Assuming Workshop automatically mounts Unreal PAKs without developer documentation.
- Letting Workshop updates overwrite user profiles or generated configurations.
- Replacing or disabling files owned by another Workshop item.

## Fallback release models

If no runtime configuration API is available:

1. **Preferred fallback:** one Workshop resource item plus a native Windows/Linux configurator that creates one user-owned PAK.
2. **Limited fallback:** one carefully selected static preset PAK for subscribe-and-play users, with the full configurator distributed separately.
3. **Last resort:** a very small curated preset collection. Avoid the combinatorial file clutter the current configurator was created to eliminate.

## Release gate

Do not begin Workshop packaging until the developer's actual mounting and persistence rules are documented or proven through an isolated Workshop test item. A valid upload is not evidence that DragonSword will load the PAK correctly.

Before release, validate:

- exact installed Workshop paths;
- game-recognized content inventory;
- clean subscribe/unsubscribe behavior;
- Steam auto-update behavior;
- generated-output persistence;
- Windows and Steam Deck configuration workflows;
- reward-table compatibility against the current official game build;
- conflict reporting with other Workshop reward mods;
- rollback without changing another item's files.
