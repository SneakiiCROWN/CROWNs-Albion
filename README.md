# CROWN'S ALBION — Fable III trainer

A native Windows trainer and command console for **Fable III (Patch 2, `Fable3.exe` 1.1.1.3)**.

CROWN'S ALBION is a **non-injecting external trainer**. It writes short, auditable Lua payloads to the
game's registered `MyScript01.lua` path and relies on the separately installed
**Improved Script Injector** DLC to execute them. It never patches, injects into, or replaces
`Fable3.exe`, `xlive.dll`, DLC, or save files.

> **Status:** development baseline recovered from v1.6.1. Runtime gameplay verification and live
> co-op testing are still pending. See `docs/FEATURE_LEDGER.md` for per-command evidence.

## Requirements

- Windows 10/11, .NET Framework 4.x (the stock `csc.exe` is sufficient)
- Fable III Patch 2 (`Fable3.exe` SHA-256
  `AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42`)
- Improved Script Injector installed as `DLC\10_ScriptInjector`
- Script path prepared (`data\scripts\MyMod\MyScript01.lua` listed in `data\dir.manifest`)
- Optional: XLiveLessNess / Fable III Co-op v2.5.0-beta.1

## Build

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1
```

Output: `Exe, Menu\CROWNsAlbion.exe` plus the runtime JSON data files.

Or double-click `CROWN_ALBION.cmd` after the first build.

## Run

Run `Exe, Menu\CROWNsAlbion.exe`. Keep Fable III closed while preparing the script path.
The menu works with the game closed for inspection and settings.

Controls: **F8** show/hide · **F1** help · **Ctrl+F** search · **Ctrl+G** exact gold ·
**Ctrl+Q** queue review · **Enter** queue · **Ctrl+Enter** run queue.

Gamepad: **L3+R3** toggle · **D-pad** navigate · **A** select/exact amount · **B** back ·
**hold B** close · **Back/View** queue · **LB/RB** value · **hold Y** run selected ·
**hold Start** run queue.

## Safety

- **CO-OP PROTECTION is ON by default.** Gameplay writes are blocked; only the harmless
  connection test is allowed.
- High-risk/save-changing commands require an explicit opt-in.
- Script writes are backed up and hash-verified; another mod's `MyScript01.lua` is never overwritten.
- No save, DLC, executable or `xlive.dll` is modified by this project.
- Co-op compatibility of individual commands is **not** verified. Use disposable saves.

## Documentation

- `docs/RECOVERY_AUDIT.md` — what was found, hashes, environment and build provenance
- `docs/FEATURE_LEDGER.md` — every exposed command and its real evidence level
- `docs/QA.md` and `docs/QA_v1_6_1.md` — inherited test history
- `ATTRIBUTIONS.md` — third-party assets and licences

## Project layout

```
src/            C# source (WinForms, .NET Framework 4.x)
data/           catalogue, references, unlocker candidates
docs/           audits, QA, design and source research
tests/          headless/UI regression harnesses
tools/          build, diagnostics and recovery scripts
Assets/         supplied third-party assets (preserved, not redistributed)
Backups/        local recovery snapshots (git-ignored)
Exe, Menu/      runnable build output
```
