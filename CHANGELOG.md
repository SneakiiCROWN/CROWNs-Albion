# Changelog

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
