# CROWN'S ALBION ⚔️

A Windows trainer and command menu for **Fable III**, made by SneakiiCROWN.

It's for experimenting with commands, items and trainer controls. **It's a development project, not a promise that every cheat works in the game.**

## Want to try it?

See [Releases](https://github.com/SneakiiCROWN/CROWNs-Albion/releases).

A **portable Windows test ZIP is now attached** to the [latest Albion test release](https://github.com/SneakiiCROWN/CROWNs-Albion/releases/tag/albion-test-1). Download **CROWNs-Albion-Windows-Test.zip**, not GitHub's automatic source-code ZIP. This is an offline-tested preview, not a fully proven in-game trainer.

When a tested portable package is posted:

1. Download its Windows ZIP and **extract the whole folder**.
2. Open `CROWNsAlbion.exe` from the extracted folder.
3. You can look around the trainer's menus with Fable III closed.
4. Read the included setup and safety notes before using it with the game.

**Do not replace game DLLs or copy mod files into Fable III based only on source-code notes.** Live commands require separate game setup, and the complete command catalogue is not live-verified.

## Handy controls

- **F8:** show/hide the menu.
- **F1:** help. **Ctrl+F:** search.
- **L3 + R3:** controller menu toggle, when supported.
- **A / Enter:** select. **B / Escape:** back.

Keep **CO-OP PROTECTION** enabled and use backed-up test saves. Individual commands and co-op use still need live verification.

## Building from source (optional)

On Windows with .NET Framework 4.x available:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1
```

The output is under `Exe, Menu`; keep its data files alongside the executable.

More info: [START_HERE.txt](START_HERE.txt), [CHANGELOG.md](CHANGELOG.md), [feature evidence](docs/FEATURE_LEDGER.md), and [technical README](docs/README_TECHNICAL.md).

**Status:** Offline-tested development trainer. Not fully verified in live Fable III.
