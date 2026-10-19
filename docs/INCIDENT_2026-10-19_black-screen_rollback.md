# INCIDENT REPORT — 2026-10-19 (black screen) — rollback record

Status: **ROLLBACK COMPLETE. All CROWN'S ALBION work frozen pending user confirmation that Fable III launches correctly.**
Prepared by: OpenCode agent (CROWN'S ALBION project) — actions below are the only changes made to the game installation.

---

## 1. What was MOVED out of the installation (fully reversible)

Source folder (this folder did **not** exist before the test — recorded empty, 0 entries, at 23:03:43):

```
B:\Games\Fable 3\XLiveLessNess\dlc\4D53090A\10_ScriptInjector\
```

Moved to quarantine **outside** the game installation:

```
C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\Backups\Quarantine_XLLN_DLC_10_ScriptInjector_20261019-231904\10_ScriptInjector\
```

Files moved (byte-identical, SHA-256 verified before and after the move):

| File | Size | SHA-256 |
|---|---|---|
| `content.xbx` | 1348 | E1F977C4ABD2565EFA91114D8F6B1B8854559A84E788E65C93A5E9E77380D2DF |
| `Content\package_collection_info.xmb` | 2180 | 2D090610A590C85CEA9790D4F5BE2334CA4AF28D8824E8312BF3A1E46A3D5909 |
| `Content\ScriptInjector.bnk` | 173 | 6CD1BEFE6B5EFD3B7E4CC75CF29FC83A9FD38D90CD95E43C7A6B5A9DE60BFC71 |
| `Content\ScriptInjector.bnk.dat` | 4704 | 15BC840CB48D2C4B251A63424C2FC3EDFC42C6BAA3DF153837029DC17C7070E5 |

Post-move verification: `XLiveLessNess\dlc\4D53090A\` contains **0 entries** again (exactly as before the test).
The files were **not deleted** — they are preserved in quarantine. `MANIFEST_SHA256.txt` is stored alongside them.

## 2. What was RESTORED (other test-specific change)

`B:\Games\Fable 3\data\scripts\MyMod\MyScript01.lua`

- Test content (an `AutoSave` probe written at 22:38) was **removed**.
- Restored byte-exact to its pre-test content from the trainer's verified backup
  (`Exe, Menu\Backups\script-MyScript01.lua-20261019-222047-993-602005.bak`, original timestamp 17/10/2026 16:29:37):

```
-- CROWN_ALBION_MANAGED
-- Ready
```

- Restored file: 33 bytes, SHA-256 `01E4CD5609191C5A2DF1E4BFEA783077D465173EE7388289FDCCB30EA9521E8A`
- The removed probe content is preserved (not deleted) at
  `Backups\Quarantine_XLLN_DLC_10_ScriptInjector_20261019-231904\MyScript01.lua.PROBE_CONTENT_removed_at_rollback.lua`

## 3. What was LEFT UNTOUCHED (verified by hash)

| File | SHA-256 (verified after rollback) |
|---|---|
| `xlive.dll` (XLLN 1.6.1.1) | 529EE0E584421281528B82E9825242703BD555FBEA72A4CD438A6B2F134E4F79 — unchanged |
| `Fable3.exe` | AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42 — unchanged |
| `FableLauncher.exe` | A97DDED5F75548ABE617E396F9ECC37AA5D4E1F330E84709FF6B876994BF2027 — unchanged |
| `data\dir.manifest` | 3A65C733C8EB513408A82D427B936D3FE3736725653420A07577A223294ADEBC — unchanged |
| `DLC\10_ScriptInjector\*` (original mod source) | byte-identical to the 23:03 hashes above — unchanged |
| `data\scripts\MyMod\MyScript02.lua` | 7063EF24A203A4CF7ACB9FAA8DC40BC4B8135E36E3E6612574D373F7EE7B765B — unchanged |
| Save files | Not modified by any test action (only copied **out** to `Backups\Saves_20261019-221550` and `Backups\Saves_20261019-230343`; `SHA256_MANIFEST.txt` in each) |

Not touched at all: save files/profiles, XLLN configuration (see note below), co-op files, `XLiveLessNess\modules\xlln-fable-3.dll`, game DLLs, DLC, licensing.

Note on `xlln-config-1.ini`: it was **not** edited by any test action. Its mtime (23:01:41, 23:14:01) matches XLLN's own session shutdown/startup — XLLN rewrites its own config. Its hash differs from the hash the co-op installer recorded on 16/10 (`A9FE4F1A…` vs current `6FFBC1EE…`), which is expected because XLLN has rewritten it during every session since. A read-only snapshot was taken to `Backups\CrashEvidence_20261019-231904\xlln-config-1.ini.snapshot`.

## 4. Crash evidence (preserved, originals untouched)

Copied to `Backups\CrashEvidence_20261019-231904\` (originals remain in `%LOCALAPPDATA%\CrashDumps`):

- `Fable3.exe.22680.dmp` (23:11:13), `Fable3.exe.2468.dmp` (23:11:50), `Fable3.exe.10916.dmp` (23:14:15)
- `F3Secu.exe.*.dmp` × 5 (22:17:44, 22:37:19, 23:11:05, 23:11:40, 23:13:59)
- `CROWNsAlbion.exe.*.dmp` × 2 (21:42 — the trainer's own earlier smoke-test crashes, unrelated to the game)
- `xlln-debug-failing-session-2314.log` (XLLN log of the failing session)
- `xlln-config-1.ini.snapshot`

### Facts established (Windows Application event log, read-only)

- `Fable3.exe` version 1.1.1.3 crashed **3 times, all today** (23:11:08, 23:11:45, 23:14:11) — no such crash in the previous 7 days.
- Faulting module: **`xlive.dll` (XLLN 1.6.1.1)** for all three, exception `0xc0000005` (access violation), **fault offset `0x0057274f` — identical every time** (deterministic crash inside XLLN).
- Crash happened 1–10 s after each launch (started 23:11:07 / 23:11:41 / 23:14:01).
- `F3Secu.exe` (the SecuROM wrapper) crashes are a **pre-existing pattern**: 2× on 16/10, 2× on 17/10, 5× today (22:17:41, 22:37:17, 23:11:03, 23:11:38, 23:13:57) — including immediately before the previous *working* session.
- The XLLN log of the failing session shows a normal startup (`InitXLLN`, profile, sockets, `Direct3D 9 Device Initialised`, `SocialOverlay: attached to D3D9 title window`) and the very last non-poll lines are that D3D9/SocialOverlay attach. **No errors, and no DLC/content handling at all** in that session.

### Assessment (honest)

- The crash occurs inside XLLN's own code (`xlive.dll`) right after it attaches its D3D9 "SocialOverlay", not in game content code. The XLLN log shows the DLC-folder copy was never even read in the failing sessions.
- Therefore the removed ScriptInjector copy is **not proven** to be the cause; it was removed because it was the only persistent installation change and removal is risk-free.
- The game's own message "Improper shutdown detected - if this persists, please check the readme file." (string embedded in `Fable3.exe`) appears because previous exits were crashes.
- Possible contributing factors that pre-date the copy: the repeated launch attempts in quick succession, a stray `FableLauncher.exe` instance started by the agent at ~23:11, and the agent's earlier force-stop of `FableLauncher.exe` at ~23:00 (the game itself was closed normally by the user seconds later). All disclosed here.

## 5. Agent actions on the installation during the whole session (complete list)

1. ~23:00 — attempted `Stop-Process` on `Fable3` (access denied, no effect); force-stopped `FableLauncher` (PID 22328) — **disclosed**.
2. 23:02–23:03 — copied saves **out** to the workspace (read-only on the originals).
3. 23:03:43 — additive copy of the 4 injector files into `XLiveLessNess\dlc\4D53090A\10_ScriptInjector\` (user-approved test) → **reverted in this rollback**.
4. 22:20–22:38 — writes to `data\scripts\MyMod\MyScript01.lua` by the trainer/live tests → **reverted in this rollback**.
5. ~23:11 — started one `FableLauncher.exe` instance (PID 28896) — **disclosed**.
6. Read-only diagnostics otherwise.

No other file in the game installation was created, copied, edited, moved or deleted.

## 6. If the game misbehaves again

Do **not** start new fixes without a decision. Options, in order of safety:

1. Retry a single launch via `FableLauncher.exe`. The "Improper shutdown" warning is the game's own and can normally be acknowledged.
2. A full Windows restart clears any stale XLLN/network/D3D state before retrying (agent action not required).
3. If `Fable3.exe` still dies in `xlive.dll`, the evidence points at XLLN's SocialOverlay/D3D9 path — decision for the user: consult the XLLN/co-op author (Glitchy Scripts), or temporarily test with the co-op module/overlay arrangement changed — **only with explicit approval and documented first**.

## 7. Rollback-of-rollback (if ever deliberately needed)

- Re-add the pack: copy `Backups\Quarantine_XLLN_DLC_10_ScriptInjector_20261019-231904\10_ScriptInjector\` back to `B:\Games\Fable 3\XLiveLessNess\dlc\4D53090A\`.
- Re-add the probe payload: copy from `Backups\Quarantine_XLLN_DLC_10_ScriptInjector_20261019-231904\MyScript01.lua.PROBE_CONTENT_removed_at_rollback.lua` to `B:\Games\Fable 3\data\scripts\MyMod\MyScript01.lua`.
- Files were never deleted, so both actions are exact restores.

---

Supersedes `Backups\XLLN_DLC_ADDED_20261019-230343.md` (the change record for the test that has now been reverted).
