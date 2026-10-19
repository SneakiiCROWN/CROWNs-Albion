# CROWN's ALBION v1.1 — acceptance / QA

**Completed offline checks (16 October 2026)**

- Reused the real previously created v1.0 project and retained its full source in `Legacy_v1_0/`.
- 95 unique command entries loaded and checked for required ID, group, Lua template and risk properties.
- Static reviews of new native WinForms controller, menu, hotkey registration, queue/preset source, script-preparation boundary, hash checking, file backups, emergency clear.
- C# delimiters lexically balanced; the source file contains a Unicode BOM for Windows `csc.exe`.
- ZIP integrity checked after packaging.

**Not completed — do not claim otherwise**

- New Windows Forms EXE compilation on an actual Windows machine (the current runtime is Linux and has no compiler for Windows .NET Framework).
- Windows 11 menu launch/graphics/hotkeys/gamepad tests.
- Injected Lua loading with Improved Script Injector, per-command gameplay acceptance or co-op compatibility.
- File-write tests against the user's installed game, save-backup location or XLLN co-op setup.

**Small first-launch acceptance test on the user's Windows 11 PC**

1. Drag extracted `CROWNs_Albion_DropIn_v1_1` folder into `B:\Games\Fable 3`.
2. Double-click `CROWN_ALBION.cmd`. Confirm it compiles and displays a native menu, then verify F8 hide/show.
3. Do not press EXECUTE until you have saved current game and copied important saves to a separate folder. Run `CROWN_BACKUP_SAVES.cmd` and manually verify any XLLN-specific saves too.
4. Inspect `data\dir.manifest`, run PREPARE SCRIPT PATH only after closing Fable III; confirm backup appears in `Backups` before trying any script action.
5. Obtain the original Improved Script Injector from Nexus mod 12 separately, then try **Utilities → Test script connection** with a **throwaway character** in offline/single-player mode.
6. Confirm the game shows the message; inspect that script returns to CROWN marker contents. Otherwise use emergency clear and stop testing.
7. Test one reversible, low-risk change and restore it; only then attempt additional commands.
8. Before using co-op, close trainer, verify original XLLN joining works, then keep the trainer disabled while testing shared income/relationship/saves.

**Failure reporting**: send the compiler output, a screenshot of the menu, and redacted `CROWN_Log.txt`. Do not share save files or private LAN addresses. Do not disable antivirus.
