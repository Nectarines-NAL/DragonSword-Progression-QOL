# Nexus submission checklist

Reference reviewed: Nexus Mods File Submission Guidelines, live review 2026-08-06.

- [ ] Upload to the correct game and utility/mod category.
- [ ] Describe this as a 1.08 **test candidate**, not confirmed native 1.08 support.
- [ ] Apply `AI-Generated Content` and `AI Media` tags.
- [ ] State that code, UI, and documentation were AI-assisted and that the original fantasy reward-forge artwork was AI-generated for this project.
- [ ] Include the matching source archive, SHA-256 file, and third-party notices.
- [ ] Link the public source repository once created.
- [ ] Explain that the executable is unobfuscated .NET 8 and inspectable with ILSpy/dnSpy.
- [ ] Explain the second executable: open-source `repak.exe`, local-only PAK pack/list/unpack operations.
- [ ] State: no network, telemetry, auto-update, admin request, DLL injection, or game executable modification.
- [ ] Do not claim ownership of or redistribute another author's modified files.
- [ ] Confirm that the release uses only the new original project artwork; do not package older reference or promotional images.
- [ ] List exact table conflicts and state that Treasure Respawn has no file-path overlap.
- [ ] Upload the source archive and portable application as separate clearly labeled files if moderator review benefits.
- [ ] Never use credits as a substitute for permission where permission would actually be required.

## Suggested moderator note

This archive contains an unobfuscated, framework-dependent .NET 8 WinForms configurator plus the open-source `repak.exe` PAK utility. The application is fully offline and performs only local JSON/XML transformation, PAK pack/list/unpack verification, SHA-256 hashing, and an optional user-confirmed copy into the selected game folder. Source and third-party licenses are included. No installer, network connection, updater, DLL injection, elevation, or executable patching is used.
