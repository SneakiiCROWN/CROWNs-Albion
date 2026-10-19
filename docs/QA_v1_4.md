# v1.4 independent QA — 17 October 2026

## Passed in this environment

- Preserved 95 unique original command templates and original JSON content.
- Preserved all 3,229 reference entries, including 244 source-derived unlocker expressions, with explicit source attribution.
- Every one of the 244 extracted Lua expressions matched one of the three conservatively admitted syntax forms (item grant, appearance equip, or bulk weapon/gauntlet grant).
- C# source checked lexically for balanced delimiters excluding string literals/comments. The source explicitly gates dispatch on co-op protection, a running game and a prepared script path.
- Both native menu and read-only PowerShell diagnostic scripts are included; no overwritten originals and no third-party game DLL executables bundled.
- Declined/failed dispatch does not clear command queue (source-level guard).
- Local catalog/integration and inherited reference tests passed (run `python -m unittest discover -s tests -p 'test_v14_integration.py' -v` and `python tests/check_references.py`).
- ZIP checksum and integrity verified after creation.

## NOT completed / MUST NOT be advertised as complete

- Compile `CROWNsAlbion.cs` using the Windows .NET 4.x compiler. This Linux runtime has **no C#/.NET Windows compiler**, so successful Windows builds cannot be claimed.
- Run/inspect live WinForms UI, overlay behaviour, Fullscreen/borderless interaction or input focus.
- Xbox controller testing on physical hardware, XInput detection, multiple controllers and menu activation at game FPS.
- Any real Lua game command executed inside Fable III.
- Existing XLiveLessNess co-op connection with Eb, preservation of property/partner income, DLC compatibility and co-op saves after trainer operations.
- Actual file mutation/rollback testing on the user's machine, external antivirus/malware scan, runtime stability or performance benchmarking.

## Remaining acceptance steps on the actual Windows PC

1. With Fable closed, run `CROWN_DIAGNOSE.cmd`; confirm the known Fable executable hash and XLLN presence.
2. Copy and verify saves, including any nondefault repack/XLLN profile paths. Test in a spare *offline* game copy.
3. Compile the source via `CROWN_ALBION.cmd`; send full compiler errors if any. Verify F8, F1, keyboard tabs/search and tray.
4. With Fable running borderless, verify controller toggle chord and other controls **don't activate while Fable has focus**. Test no unwanted focus theft on menu closure.
5. In the reference library filter `Extracted Unlocker Lua`, queue and inspect one candidate, ensure co-op protection blocks it. Don't dispatch a grant on a main save.
6. Only after independently verified XLLN-safe Lua injection in an isolated offline copy, execute `Utilities → Test script connection`. Confirm the in-game message actually appeared, script was cleared and no other mod file was overwritten.
7. Progressively test reversible/low-risk operations, confirm game-side effects and saves. For resource/quest/item actions, validate no repeat duplicates first.
8. Protect and test both PCs' main co-op separately. Never merge game data-bank replacement mods or Catspaw `xlive.dll` until a compatibility test proves no regressions.

## Known limitations of design

The external Windows Forms menu does not inject graphics into Fable III; borderless/windowed rendering is required. Fable may reject `SetForegroundWindow` activation depending on OS state. Global hotkey registration can fail if F8 is already owned by another app. A ~1.9-second Lua pulse guarded by a Lua global sentinel is **not a reliable exactly-once transaction** across every injector/runtime and can both miss delivery and duplicate writes. The diagnostics are read-only; they cannot attest injector runtime or co-op compatibility.
