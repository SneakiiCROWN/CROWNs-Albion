# CROWN'S ALBION

A Windows trainer and command menu for **Fable III**. Browse commands, item options and trainer tools without needing to write code.

**Current version: Windows development test.** The menu and offline tests work in the build environment; **the full set of cheats has NOT been verified inside Fable III**.

## Download and open it

1. [Download the Windows test ZIP](https://github.com/SneakiiCROWN/CROWNs-Albion/releases/download/albion-test-1/CROWNs-Albion-Windows-Test.zip).
2. Right-click it and choose **Extract All**.
3. Keep all files together. Double-click **CROWNsAlbion.exe**.
4. You can browse the menu with **Fable III closed**.

Use **F8** to show/hide the menu, **F1** for help, **Ctrl+F** to search, **Enter** to select and **Escape** to go back. When supported, **L3+R3** toggles the menu with a controller.

**Important:** This download is a trainer, **not Fable III itself**. It doesn't include the game, game DLLs, DLC or the separate script injector needed for live commands. Don't replace game files just because you see their names in the documentation.

## What's working, and what's still being tested?

- **Built successfully:** a portable Windows EXE with the supporting command/data files.
- **Offline-tested:** menu, command catalogue, item-vault data and safety/regression checks.
- **Not yet verified:** individual cheats actually working during Fable III gameplay, supported game setups and co-op interactions.

Keep **CO-OP PROTECTION** on. Back up saves before experimenting. Never assume all commands are safe in multiplayer.

## What's coming next?

More live-game verification, command reliability checks, further interface polish and clearer profiles/queue tools. For individual command status, see [the feature ledger](docs/FEATURE_LEDGER.md) and [changelog](CHANGELOG.md).

## Known limitations and updates

The main unresolved limitation is **live Fable III compatibility**: having a command in the menu does not guarantee it works, and co-op effects need separate checks. The game-specific injector/setup is not included. Keep your normal game files and saves backed up.

**Updating:** check [Releases](https://github.com/SneakiiCROWN/CROWNs-Albion/releases) for a newer ZIP. Extract each version into a separate folder, keep the old one for rollback and read its release notes before using it with a save.

## Need help or found a bug?

[Report it here](https://github.com/SneakiiCROWN/CROWNs-Albion/issues). Include the trainer version, what you tried, and whether Fable III was running. **Don't upload your personal game saves or private account information publicly.**

## For developers only

The Windows source uses .NET Framework 4.x. Build it using:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1
```

The resulting files are staged under `Exe, Menu`. For detailed setup and safety notes, see [START_HERE.txt](START_HERE.txt) and [technical documentation](docs/README_TECHNICAL.md).
