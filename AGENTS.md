# CROWN'S ALBION — Repository Agent Instructions

## Project Overview
CROWN'S ALBION v2.0 is a Fable III trainer/mod menu built as a modular C# WinForms application.
It provides verified-safe command polish, offline test suite, and premium Albion UI.

## Critical Constraints
- **NEVER modify the live Fable III installation** (game files, DLLs, saves, xlive.dll, DLC)
- **NEVER restore the quarantined ScriptInjector** without explicit approval
- **NEVER perform live game tests** without explicit approval
- **NEVER run unknown third-party executables**
- All development must be within the project workspace only
- Preserve co-op safety restrictions and confirmation safeguards

## Build & Test
- Build: `powershell -ExecutionPolicy Bypass -File tools\build.ps1`
- Full test suite: `cmd /c tests\Run-All-Tests.cmd`
- Engine tests only: `cmd /c tests\Run-Engine-Tests.cmd`
- UI tests only: `cmd /c tests\Run-UI-Tests.cmd`
- Data check: `powershell -ExecutionPolicy Bypass -File tests\check-data.ps1`
- Safety check: `powershell -ExecutionPolicy Bypass -File tests\check-source.ps1`

## Key Files
- `src/Trainer.cs` — engine core, gates, atomic writes, backups, pulse/clear, profiles
- `src/Trainer.UI.cs` — shell layout, lists, detail pane, OpenAmountPad
- `src/Dialogs.cs` — queue/vault/library/diagnostics/profiles/settings dialogs
- `src/Models.cs` — Choice/Parameter/GameCommand classes
- `src/Theme.cs` — Albion palette (Leather/Parchment/Gold)
- `src/SvgIcon.cs` — SVG icon rendering
- `src/Controls.cs` — themed buttons, Chip status pill
- `data/catalog.json` — 175 commands
- `data/item_vault.json` — 313 grants
- `docs/FEATURE_LEDGER.md` — honest per-command classification
- `docs/INCIDENT_2026-10-19_black-screen_rollback.md` — crash investigation

## Git Workflow
1. Inspect `git status` before any changes
2. Preserve last verified checkpoint (tag or commit)
3. Implement authorised source changes
4. Run builds, tests, regression checks
5. Update CHANGELOG and FEATURE_LEDGER
6. Commit with descriptive message
7. Push to `origin/main`
8. Add tags for significant milestones
9. Create prerelease only when appropriate
10. Report version, commit SHA, test results, remaining limitations

## GitHub Repository
- **URL**: https://github.com/SneakiiCROWN/CROWNs-Albion
- **Visibility**: PRIVATE
- **Branch**: main
- **Tags**: v1.6.1-recovery, v2.0-dev, v2.0-dev.1

## Honest Status
- All 113 offline tests pass (22 data + 24 safety + 46 engine + 21 UI)
- NO command verified live in Fable III
- All commands classified "wired-not-verified"
- Lua injector requires genuine live verification
- Co-op safety restrictions intact

## Excluded from Repository
- Installed Fable III game files
- Third-party tools without redistribution permission
- Player saves and personal data
- Credentials, tokens, machine-specific settings
- Temporary files, compiler outputs, caches
- Large ZIP archives and compiled binaries
- Quarantined ScriptInjector pack

## Safety Rules
- Never write to Fable3.exe, xlive.dll, DLC, or save files
- Never launch external executables from the trainer
- Never perform network access from the trainer
- Always verify SHA-256 hashes before script writes
- Always create backups before consequential changes
- Co-op protection must remain functional
- High-risk gate must remain functional