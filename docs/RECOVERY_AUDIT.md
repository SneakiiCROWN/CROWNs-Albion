# CROWN'S ALBION — recovery audit

**Audit date:** 19 October 2026  
**Auditor:** lead developer session  
**Scope:** `C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III`, the supplied asset archives, and
read-only inspection of the installed Fable III tree. No game file, save, DLC or DLL was modified.

---

## 1. Verdict

The newest recoverable source is:

```
Assets\CROWNs ALBION\CROWNs_Albion_v1_6_1_Modal_Dialog_Repair\
```

It is a **complete C# WinForms project with source, compiled EXE, catalogue, research data, tests,
QA evidence and rollback material**. Its title and assembly version are **v1.6.1**. Its changelog is
dated 17 October 2026 and it explicitly supersedes v1.6 and v1.5. The v1.5 ZIP in Downloads is older
(17 October 2026, 00:20) and must not replace it.

A full recovery snapshot was written to:

```
Backups\CROWNs_Albion_v1_6_1_recovery_baseline.zip
```

The project working tree was seeded from the v1.6.1 source and committed as the Git baseline before
any functional change.

---

## 2. Source and archive comparison

| Candidate | Date | Size | Result |
|---|---|---:|---|
| `CROWNs_Albion_v1_6_1_Modal_Dialog_Repair` | 17 Oct 2026 | 4.0 MB | **Recovered baseline.** Source + EXE + 95-command catalogue + 3,229 research records + 244 unlocker records + QA/docs/tests. |
| `CROWNs_Albion_Fable_III_v1.5_Exact_Amounts_and_Controller.zip` | 17 Oct 2026 00:20 | 3.8 MB | Older. 95 entries, exact amounts and controller, but before v1.6 router/modal fixes. Preserved, not used. |
| `For fable 3*.zip` | 16 Oct 2026 | 1.75 GB total | Asset/reference bundle only: debug-menu dump, item lists, injector DLC, XLLN co-op, GDB tools, ImGui, optional mods. No newer CROWN source. |
| `CROWNs ALBION` parent folder | 16–17 Oct 2026 | 4.0 MB | Contains only the v1.6.1 recovery candidate. |

No v1.7+ source or build exists in the searched locations. The newest **working** version is
therefore v1.6.1, subject to the runtime limitations listed below.

### Recovered v1.6.1 contents

- **Language:** C# 5-compatible, single file, **.NET Framework 4.x WinForms**.
- **Build tool:** stock `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
  (14.8.10001.0 on this machine). No Visual Studio, NuGet, WPF or external runtime required.
- **References:** `System.Windows.Forms`, `System.Drawing`, `System.Core`,
  `System.Web.Extensions` (for `JavaScriptSerializer`).
- **UI:** owner-drawn WinForms lists, modal dialogs, native common dialogs, XInput polling,
  global hotkey F8, tray icon.
- **Data:** `catalog.json` (95 commands), `references.json` (3,229 records),
  `unlocker_commands.json` (244 candidates).
- **Safety:** script backups with SHA-256 verification, atomic replace, foreign-script refusal,
  co-op gate, high-risk gate, auto-clear timer, emergency clear script.

### Recovered v1.6.1 fixes worth preserving

- Modal ownership assigned before child HWND creation; no always-on-top owner.
- Unified popup lifecycle (`ShowPopup`) for amount, queue, library, notices and confirmations.
- Owned native file/folder pickers, centred on the owner monitor.
- Keyboard routing intercepted before WinForms can activate a disabled owner.
- Focus restoration only when CROWN still owns the foreground.
- Controller: one foreground router, neutral-release arming, L3+R3 chord, A/B/Enter/Escape,
  hold-B close, hold-Y/Start destructive actions.
- Exact amount dialog with typed integers and presets; never executes on selection.

---

## 3. Build verification

| Item | Result |
|---|---|
| Clean compile of recovered source | **PASS**, exit 0, 0 errors / 0 warnings |
| Output | `Backups\BaselineBuild\CROWNsAlbion_baseline.exe` (88,064 bytes) |
| Recovered EXE | `CROWNsAlbion.exe` (88,064 bytes, assembly 1.6.1.0) |
| Fresh build SHA-256 | `1958409BEEC39AE8D1F1E41C709B0F92FA5A4031CF21BE401EACE29326311CED` |
| Recovered EXE SHA-256 | `FA32208D3C0A62820269B795A4DB8F4AAECE24A68C579A5C707F1A4538927247` |

The byte hashes differ because the recovered EXE was produced in a separate compile session
(PE timestamp/metadata), not because the source differs materially. Both report v1.6.1 and expose the
same catalogue and UI. **Provenance limitation:** the recovered binary cannot be proven byte-for-byte
identical to the recovered source without a deterministic rebuild from the original session.

The app was launched with Fable III closed and inspected on the current desktop. The baseline
screenshot is `docs/baseline-screen.png`. The UI renders, the catalogue loads and the status bar
reports `FABLE: PATCH 2 CHECKSUM OK · SCRIPT PATH READY`.

---

## 4. Installed Fable III environment (read-only inspection)

| Item | Observed |
|---|---|
| Game root | `B:\Games\Fable 3` |
| `Fable3.exe` SHA-256 | `AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42` |
| Supported build | Patch 2 / 1.1.1.3, matching the co-op package's required checksum |
| Launcher | `FableLauncher.exe` present |
| XLive layer | `xlive.dll` 5,414,400 bytes, **XLiveLessNess 1.6.1.1** (Glitchy Scripts) |
| XLLN module | `XLiveLessNess\modules\xlln-fable-3.dll` (1,211,392 bytes) |
| XLLN config | `XLiveLessNess\xlln-config-1.ini`, player 1 `SneakiiCROWN`, online/LIVE enabled |
| Co-op package | `B:\Games\Fable3-Coop-v2.5.0-beta.1` (source + payload + docs) |
| Script injector | `DLC\10_ScriptInjector` installed (content.xbx + bnk + bnk.dat + XMB) |
| Script path | `data\scripts\MyMod\MyScript01.lua` = `-- CROWN_ALBION_MANAGED / -- Ready` |
| Fade script | `data\scripts\MyMod\MyScript02.lua` managed placeholder |
| Manifest | `data\dir.manifest` lines 531–532 register both MyMod scripts |
| Saves | Not read, copied or modified |

### Reference-only material (not installed / not run)

- `Catpaws_XLive_Emulator_15d.7z` — **incompatible with co-op**; its own README states no multiplayer.
  Never overwrite XLLN's `xlive.dll` with it.
- `Fable3DebugMenu.exe` (138.83 MB) — external script editor, source not supplied.
  ReadMe inspected only; **not executed**.
- `1.0 Setup-...exe` — unidentified NSIS installer; **not executed**.
- Optional DXVK/ReShade/HUD/augment mods — not merged. `d3d9.dll` conflicts remain unresolved.

### Integration chain

```
CROWN's ALBION (WinForms)
  -> writes data\scripts\MyMod\MyScript01.lua (atomic, backed up)
  -> DLC\10_ScriptInjector runs RunScript("scripts\\MyMod\\MyScript01.lua")
     once per ~60 world frames (source: DEMO001_ScriptInjector.lua)
  -> Fable III Lua debug API executes the payload
```

This chain is **present and structurally complete**, but no live command was executed during this
audit. See `docs/FEATURE_LEDGER.md` for the distinction between source-backed commands and
runtime-verified commands.

---

## 5. Supplied UI assets

| Asset | Contents | Licence | Use |
|---|---|---|---|
| `kenney_fantasy-ui-borders.zip` | 302 files; 48×48 9-slice panels and borders | **CC0** | Selected gold-tintable frames extracted to `Assets/CROWN_UI` |
| `game-icons.net.svg.zip` | 4,181 SVGs | **CC BY 3.0** (some CC0) | Selected icons; attribution required |
| `Improved Script Injector` (×2) | Identical ZIPs; DLC package + Lua source | Third-party mod | Detection/reference; not redistributed |
| `Fable 3 Debug Menu 34 1.6` | ReadMe + 138.83 MB EXE | Third-party | ReadMe studied; binary not run |
| `Catpaws XLive Emulator 15d` | `xlive.dll` + source | Third-party | Reference only; never install with XLLN |

---

## 6. Known failure points found in v1.6.1

These are real issues to fix in the development pass, not cosmetic notes.

1. **Script pulse may expire before the injector reads it.** The injector runs the script once per
   **60 world frames**, not per second. The auto-clear timer is **1,900 ms**. At 30 FPS, the first
   read can occur at ~2,000 ms, after the file has already been cleared. At high FPS it works; at low
   FPS or during frame-time spikes it can silently execute nothing.
2. **No injector readiness check.** `ReadyScript()` verifies the file and manifest only. It does not
   check that `DLC\10_ScriptInjector` is actually present and enabled.
3. **No applied-state model.** Toggles do not remember what was last sent; the UI cannot distinguish
   "God mode ON sent" from "God mode OFF sent" or from an unknown game state.
4. **Global F8 registration can fail silently** beyond a log line. No fallback or user-visible warning.
5. **XInput dependency is hard-coded to `xinput1_4.dll`.** Older Windows or missing runtime produces
   only a status message and disables controller support.
6. **The queue is cramped** into a 35 px list and the detail pane lacks room for longer descriptions.
7. **The visual design is functional but generic dark-blue**, not the requested Albion fantasy theme.
8. **No settings/profile screen.** Preferences are a single `settings.json`; there are no named
   profiles, no exportable favourites, no reset.
9. **No dry-run / payload preview.** Users cannot inspect the exact Lua before a consequential write.
10. **Research library is read-only except a narrow unlocker allowlist.** Item and appearance data is
    not usable as a proper curated vault.
11. **Co-op guidance is a single checkbox.** There is no distinction between local-only commands,
    commands that may affect a guest, and commands that are structurally unsafe in a session.
12. **No first-class diagnostics screen** for exe hash, injector files, manifest lines, script state,
    XLLN version, backup health and log tail.

---

## 7. Safety decisions for this development pass

- The installed game tree is treated as **read-only** unless the user explicitly approves a change.
- No third-party EXE was executed.
- No save was opened or copied.
- No DLL was replaced, and Catspaw's emulator was not combined with XLLN.
- The working project is isolated to this workspace.
- Live gameplay and co-op verification require human testing; they are not claimed from UI tests.

---

## 8. Recovery conclusion

v1.6.1 is a credible, buildable baseline with a sound safety model and a working non-injecting
delivery mechanism. Its main weaknesses are timing reliability, shallow verification, cramped UI,
missing state feedback and a catalogue that is broader than its evidence. The development pass
therefore starts from the recovered code — not from scratch — and targets those exact weaknesses.
