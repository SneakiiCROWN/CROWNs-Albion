# Changelog

## v2.0 — modular Albion shell (19 October 2026)

The recovered v1.6.1 behaviour was rebuilt as a modular, premium-styled trainer. No gameplay command
has been executed yet and no game file, save, DLC or DLL was changed. Every command remains
*wired, not game-verified* (see `docs/FEATURE_LEDGER.md`).

### Added
- **Modular engine and UI** — `Models`, `Theme`, `Controls`, `SvgIcon`, `Trainer`, `Trainer.UI` and
  `Dialogs`, replacing the single-file monolith (kept under `reference/`).
- **Premium Albion theme** — leather/gold palette, 9-slice Kenney frames, 153 runtime-rendered
  game-icons.net SVGs, custom buttons, toggles, sliders and list painting.
- **Expanded catalogue** — 95 recovered commands plus 80 curated commands from the official debug
  menu (175 total) across Hero, Combat, Kingdom, Inventory, Appearance, World, Spawning, Chaos.
- **Item vault** — 313 curated grants (weapons, outfits, tattoos, makeup, clothing, hair, GFWL
  exclusives, bulk grants) built from the extracted item lists and the community All Items Unlocker.
- **Dialogs** — queue manager, research library, item vault, diagnostics, profiles and settings.
- **Visible safety gates** — `LOCKED BY GATE` state with the blocking reason, live-updating when the
  co-op or high-risk toggles change.
- **Honest evidence model** — `SOURCE-BACKED` / `v1.6.1 RECOVERED` / `COMMUNITY` badges and a
  generated `docs/FEATURE_LEDGER.md`.
- **Offline regression suite** — data integrity (22), engine safety (24) and runtime engine
  behaviour (40) checks, plus automated dialog screenshots in `docs/ui/`.

### Changed / fixed
- **Pulse window** raised to 5,000 ms with a persistent Lua sentinel, fixing the case where the
  1,900 ms auto-clear expired before the injector's once-per-60-frames read at low frame rates.
- **XInput fallback** across `xinput1_4`, `xinput1_3` and `xinput9_1_0`.
- Sidebar compacted so all categories fit without a native scrollbar; category counts are shown.
- `Exe, Menu\item_vault.json` is now staged by the build alongside the other data files.

### Verified in this milestone
- Clean build (7 sources, 0 errors/warnings) to `Exe, Menu\CROWNsAlbion.exe`.
- `tests\Run-All-Tests.cmd` passes: 86 offline checks.
- Dialog screenshots captured for vault, queue, library, diagnostics, profiles, settings and help.
- Diagnostics confirms the real install at `B:\Games\Fable 3` (Patch 2 hash, injector present, script
  path ready, XLiveLessNess 1.6.1.1).

### Still pending (requires a supervised session)
- Live in-game verification of any command.
- Separate single-player and co-op testing on disposable saves.

## v2.0 development — recovery baseline (19 October 2026)

Recovery, audit and checkpoint pass. No gameplay command was executed; no game file, save, DLC or
DLL was changed.

### Recovered
- Restored the newest source: **v1.6.1 "Modal Dialog Repair"** (17 October 2026).
- Preserved the v1.5 archive and all supplied third-party assets.
- Verified a clean .NET Framework 4.x build of the recovered source (exit 0, 0 warnings).
- Captured the baseline UI screenshot and the full recovery audit
  (`docs/RECOVERY_AUDIT.md`).
- Seeded the working project (`src/`, `data/`, `docs/`, `tests/`, `tools/`) and created the
  Git recovery checkpoint.

### Confirmed environment
- `Fable3.exe` matches the supported Patch 2 checksum.
- Improved Script Injector DLC is installed and the MyMod script path is registered.
- XLiveLessNess 1.6.1.1 and the co-op v2.5.0-beta.1 package are present.
- No game file was modified during the audit.

### Documented for repair
- Script pulse timing can miss the injector's 60-frame read at low frame rates.
- No injector readiness check, no applied-state model, hard-coded XInput 4, no settings/profile
  screen, no dry-run preview, cramped queue and generic visual theme.

### Historical versions
- **v1.6.1** — modal dialog ownership/z-order/focus repair. 288 dialog + 37 controller + 15 Python
  checks (source environment).
- **v1.6** — single foreground controller router, L3+R3 chord, hold-to-confirm actions.
- **v1.5** — exact amount dialog, queue review, controller safeguards, co-op protection.
- **v1.0–v1.4** — original browser planner and native WinForms menu.
