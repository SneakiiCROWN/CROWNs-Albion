# Attributions and third-party notices

CROWN'S ALBION consists of original C# code plus selected third-party visual assets supplied with the
project. This file records what is used, under which licence, and what is deliberately **not**
redistributed.

## UI assets used

### Kenney — Fantasy UI Borders
- Source: https://kenney.nl/assets/fantasy-ui-borders
- Author: Kenney (www.kenney.nl)
- Licence: **Creative Commons Zero (CC0 1.0)**
- Usage: selected 48×48 9-slice panel and border PNGs, tinted at runtime for Albion-themed frames.
- Extracted subset: `Assets/CROWN_UI/`

### Game-icons.net
- Source: https://game-icons.net
- Licence: **Creative Commons Attribution 3.0 (CC BY 3.0)** unless a specific icon is marked CC0.
- Usage: category, feature and status icons. Icons are tinted at runtime; SVG path data is rendered
  by the trainer, not bundled as a sprite sheet. 153 icons are used.
- Required credit: *"Icons made by [author] from game-icons.net."* The authors of the selected icons
  are listed below and in `Assets/CROWN_UI/icons/CREDITS.txt`.

| Icon | Author |
|---|---|
| crown, crossed-swords, crowned-heart, castle, bolt-spell-cast, chest, gears | Lorc |
| coins, compass, boots, key, round-potion, dog, village, tower | Delapouite |
| others as recorded in `CREDITS.txt` | see CREDITS.txt |

If an icon is later found to be CC0, that fact does not remove the courtesy credit.

## Third-party tools referenced but not redistributed

| Component | Author / source | Treatment |
|---|---|---|
| **Improved Script Injector** | Nexus Mods Fable III mod #12 | Required at runtime by the user; not bundled, not modified |
| **XLiveLessNess / Fable III Co-op v2.5.0-beta.1** | Glitchy Scripts / community project | Installed by the user; LGPL and module notices remain with that package |
| **Fable 3 Debug Menu** | Community author | ReadMe and command list used as research evidence; binary not shipped |
| **Catspaw's XLive Emulator 15d** | Catspaw | Reference only; incompatible with XLLN co-op, never installed |
| **Fable III** | Lionhead Studios / Microsoft | Game and data files are not included; no DRM or licensing circumvention |

## Deliberately excluded

- Fable III game assets, executable files, DLC content or save files.
- The `Fable3DebugMenu.exe` binary and the unidentified NSIS setup executable.
- Any third-party code whose licence does not permit redistribution.
- Catspaw's `xlive.dll` (conflicts with the co-op stack).

## Original project code

The C# source in `src/` is original work created for this project and its predecessor sessions.
Official prerelease binaries may be downloaded for personal use under LICENSE.txt. Public source availability does not grant a general open-source licence. Third-party licence rights remain unchanged.
