# CROWN's ALBION Feature Ledger

Generated from `data/catalog.json` and `data/item_vault.json` on 2026-10-19 23:46.

## How to read this ledger

| Status | Meaning |
| --- | --- |
| Verified in game | Confirmed by watching the effect in Fable III on a spare save. |
| Integration-verified | The script path, manifest registration and injector hand-off chain was checked on disk. |
| Wired, not game-verified | The command is in the catalogue, its template is validated, and it is dispatchable. No live effect was observed. |
| UI-only reference | Shown for research; it is not dispatched as a cheat. |
| Broken | Known not to work. |

**No command in this build has been verified live in Fable III yet.** The integration chain (marked Lua → `data\scripts\MyMod\MyScript01.lua` → `DLC\10_ScriptInjector` → Fable III) is verified structurally, but every row below remains wired-not-verified until a supervised live test is recorded.

Evidence: `debug-menu` = expression taken from the official Fable III debug-menu dump; `legacy` = recovered from the v1.6.1 trainer catalogue; `reference` = research entry only; `community` = source-derived community grant.

Co-op column: `safe` = menu/UI only; `local` = touches the local hero only; `unverified` = world or shared state; `unsafe` = may break or desync a session.

## Catalogue (175 commands)

| ID | Command | Group | Kind | Risk | Evidence | Co-op | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `beard-classic` | Beard: classic | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-full` | Beard: full | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-goatee` | Beard: goatee | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-long-tied` | Beard: long tied | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-tache` | Beard: moustache | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-tache-beard` | Beard: moustache and beard | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-tache-sidies` | Beard: moustache and sideburns | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `beard-muttons` | Beard: muttons | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-bob` | Hair: bob | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-long` | Hair: long | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-long-thick` | Hair: long and thick | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-mohawk` | Hair: mohawk | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-shaved` | Hair: shaved | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-short-fringed` | Hair: short fringed | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-short-ponytail` | Hair: short ponytail | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `hair-standard` | Hair: standard | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-skill-f` | Outfit: Skill (female) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-skill-m` | Outfit: Skill (male) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-strength-f` | Outfit: Strength (female) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-strength-m` | Outfit: Strength (male) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-will-f` | Outfit: Will (female) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `suit-will-m` | Outfit: Will (male) | Appearance | action | medium | debug-menu | local | Wired, not game-verified |
| `mistpeak-quest` | Complete Mistpeak Demon Door tracker | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `cutscenes` | Finish active cutscenes | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `freeze-world` | Freeze all world entities | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `relation-fear` | Relationship: fear | Chaos | action | medium | debug-menu | unverified | Wired, not game-verified |
| `relation-friend` | Relationship: friends | Chaos | action | medium | debug-menu | unverified | Wired, not game-verified |
| `relation-hate` | Relationship: hate | Chaos | action | medium | debug-menu | unverified | Wired, not game-verified |
| `relation-neutral` | Relationship: neutral | Chaos | action | medium | debug-menu | unverified | Wired, not game-verified |
| `reset-opinions` | Reset villager opinions | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `npc-love` | Set current NPC relationship to love | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `unfreeze-world` | Unfreeze all world entities | Chaos | action | high | legacy | unverified | Wired, not game-verified |
| `autoheal-off` | Auto heal OFF | Combat | toggle | low | legacy | unverified | Wired, not game-verified |
| `autoheal-on` | Auto heal ON | Combat | toggle | low | legacy | unverified | Wired, not game-verified |
| `finisher-off` | Disable combat finishers | Combat | toggle | medium | legacy | unverified | Wired, not game-verified |
| `ai-off` | Disable world AI | Combat | action | high | debug-menu | unsafe | Wired, not game-verified |
| `finisher-on` | Enable combat finishers | Combat | toggle | medium | legacy | unverified | Wired, not game-verified |
| `ai-on` | Enable world AI | Combat | action | high | debug-menu | unsafe | Wired, not game-verified |
| `enemy-off` | Enemy god mode OFF | Combat | toggle | medium | legacy | unverified | Wired, not game-verified |
| `enemy-on` | Enemy god mode ON | Combat | toggle | medium | legacy | unverified | Wired, not game-verified |
| `finisher-allow-all` | Finishers: allow every match | Combat | action | medium | debug-menu | unverified | Wired, not game-verified |
| `finisher-test-off` | Finishers: test mode OFF | Combat | toggle | medium | debug-menu | unverified | Wired, not game-verified |
| `finisher-test-on` | Finishers: test mode ON | Combat | toggle | medium | debug-menu | unverified | Wired, not game-verified |
| `heal` | Heal hero | Combat | action | low | legacy | unverified | Wired, not game-verified |
| `god-off` | Hero god mode OFF | Combat | toggle | low | legacy | unverified | Wired, not game-verified |
| `god-on` | Hero god mode ON | Combat | toggle | low | legacy | unverified | Wired, not game-verified |
| `finisher-crescendo` | Ignore finisher crescendo requirement | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-enemy` | Ignore finisher enemy-health requirement | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-hero` | Ignore finisher hero-stat requirement | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-vicinity` | Ignore finisher participant proximity | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-ability` | Ignore finisher weapon ability gate | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-attacks` | Keep finisher minimum-attack check | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-angle` | Relax finisher angle restrictions | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `finisher-combo` | Relax finisher multiplier check | Combat | action | medium | legacy | unverified | Wired, not game-verified |
| `difficulty` | Set difficulty | Combat | action | low | legacy | unverified | Wired, not game-verified |
| `xp-general` | Add general experience | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `xp-skill` | Add skill experience | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `xp-strength` | Add strength experience | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `xp-will` | Add will experience | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `female` | Female hero model | Hero | action | high | legacy | unverified | Wired, not game-verified |
| `gender-female` | Female hero voice/pronouns | Hero | action | high | legacy | unverified | Wired, not game-verified |
| `age` | Hero age | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `tan` | Hero tan | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `fat` | Hero weight | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `male` | Male hero model | Hero | action | high | legacy | unverified | Wired, not game-verified |
| `gender-male` | Male hero voice/pronouns | Hero | action | high | legacy | unverified | Wired, not game-verified |
| `good` | Morality slider | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `morality-evil` | Morality: fully evil | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `morality-good` | Morality: fully good | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `morality-zero` | Morality: neutral | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `gender-neutral` | Neutral hero pronouns/voice | Hero | action | high | legacy | unverified | Wired, not game-verified |
| `primal` | Primal morph | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `purity` | Purity slider | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `purity-corrupt` | Purity: fully corrupt | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `purity-pure` | Purity: fully pure | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `purity-zero` | Purity: neutral | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `health-full` | Restore hero health | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `hero-level` | Set hero level | Hero | action | medium | debug-menu | local | Wired, not game-verified |
| `skill` | Skill morph | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `skill-full` | Skill to full | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `skill-zero` | Skill to zero | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `str` | Strength morph | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `strength-full` | Strength to full | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `strength-zero` | Strength to zero | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `tan-pale` | Tan: fully pale | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `tan-full` | Tan: fully tanned | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `costume-gloves` | Wear costume gloves | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `costume-prostitute` | Wear costume top | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `costume-prisoner` | Wear prisoner hat | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `costume-renegade` | Wear renegade shorts | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `costume-skillboots` | Wear skill boots | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `fat-full` | Weight: maximum | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `fat-zero` | Weight: minimum | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `will` | Will morph | Hero | action | medium | legacy | unverified | Wired, not game-verified |
| `will-full` | Will to full | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `will-zero` | Will to zero | Hero | action | low | debug-menu | local | Wired, not game-verified |
| `chicken` | Chicken outfit | Inventory | action | medium | legacy | unverified | Wired, not game-verified |
| `spell-gauntlets` | Give all spell gauntlets | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `potion-dead` | Give Dead Rising potions | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `gold-key` | Give gold key | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `potion-health` | Give health potions | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `silver-key` | Give silver key | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `potion-time` | Give slow-time potions | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `spade` | Give spade | Inventory | action | low | legacy | unverified | Wired, not game-verified |
| `weapon-spartan` | Give Spartan longsword | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `hide-all-weapons` | Hide all weapons in the sanctuary | Inventory | action | medium | debug-menu | unverified | Wired, not game-verified |
| `pyjamas` | Pyjamas outfit | Inventory | action | medium | legacy | unverified | Wired, not game-verified |
| `show-all-weapons` | Show all weapons in the sanctuary | Inventory | action | medium | debug-menu | unverified | Wired, not game-verified |
| `dyes-1` | Unlock Dye Pack I | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `dyes-2` | Unlock Dye Pack II | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `dyes-3` | Unlock Dye Pack III | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `weapon-foreground` | Weapon foreground colour | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `weapon-particles` | Weapon particle effect | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `weapon-primarycolour` | Weapon primary colour | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `weapon-primarymesh` | Weapon primary mesh | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `weapon-secondarymesh` | Weapon secondary mesh | Inventory | action | high | legacy | unverified | Wired, not game-verified |
| `town-demands-accept` | Accept all town demands | Kingdom | action | high | debug-menu | unverified | Wired, not game-verified |
| `followers-up` | Add 50,000 followers | Kingdom | action | medium | debug-menu | unverified | Wired, not game-verified |
| `gold` | Add hero gold | Kingdom | action | low | legacy | unverified | Wired, not game-verified |
| `ruler-off` | Become ruler OFF | Kingdom | toggle | high | debug-menu | unverified | Wired, not game-verified |
| `ruler-on` | Become ruler ON | Kingdom | toggle | high | debug-menu | unverified | Wired, not game-verified |
| `day-tax-down` | Decrease provincial taxes | Kingdom | action | high | legacy | unverified | Wired, not game-verified |
| `province-update` | Force province update | Kingdom | action | medium | debug-menu | unverified | Wired, not game-verified |
| `treasury` | Fund royal treasury | Kingdom | action | low | legacy | unverified | Wired, not game-verified |
| `seals` | Give Guild Seals | Kingdom | action | low | legacy | unverified | Wired, not game-verified |
| `day-tax-up` | Increase provincial taxes | Kingdom | action | high | legacy | unverified | Wired, not game-verified |
| `renown` | Increase renown | Kingdom | action | low | legacy | unverified | Wired, not game-verified |
| `town-demands-reject` | Reject all town demands | Kingdom | action | high | debug-menu | unverified | Wired, not game-verified |
| `followers-down` | Remove 50,000 followers | Kingdom | action | medium | debug-menu | unverified | Wired, not game-verified |
| `ruler` | Ruler summary | Kingdom | action | medium | legacy | unverified | Wired, not game-verified |
| `gold-zero` | Set hero gold to zero | Kingdom | action | high | debug-menu | unverified | Wired, not game-verified |
| `unlock-rtr` | Unlock entire Road to Rule | Kingdom | action | high | legacy | unverified | Wired, not game-verified |
| `spawn-villager-named` | Spawn a villager at hero | Spawning | action | medium | debug-menu | unverified | Wired, not game-verified |
| `spawn-balverine` | Spawn balverine | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `flock-bats` | Spawn bats | Spawning | action | medium | legacy | unverified | Wired, not game-verified |
| `flock-crows` | Spawn crows | Spawning | action | medium | legacy | unverified | Wired, not game-verified |
| `spawn-dervish` | Spawn dervish ambush | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-elliot` | Spawn Elliot | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `spawn-hobbe-family` | Spawn hobbe family | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-hobbes` | Spawn Hobbes | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `spawn-hollowmen` | Spawn hollow men family | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-logan` | Spawn Logan's soldiers | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-minions` | Spawn minions around hero | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-renegade-family` | Spawn renegade family | Spawning | action | high | debug-menu | unverified | Wired, not game-verified |
| `spawn-renegades` | Spawn renegades | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `spawn-villager` | Spawn villager | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `spawn-wolves` | Spawn wolves | Spawning | action | high | legacy | unverified | Wired, not game-verified |
| `fast-speech-off` | Fast dialogue OFF | Utilities | toggle | medium | legacy | unverified | Wired, not game-verified |
| `fast-speech-on` | Fast dialogue ON | Utilities | toggle | high | legacy | unverified | Wired, not game-verified |
| `finish-cutscene` | Finish active cutscenes | Utilities | action | high | legacy | unverified | Wired, not game-verified |
| `autosave` | Request autosave | Utilities | action | medium | legacy | unverified | Wired, not game-verified |
| `test-message` | Test script connection | Utilities | action | low | legacy | unverified | Wired, not game-verified |
| `dog-tricks-off` | All dog tricks OFF | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `dog-tricks-on` | All dog tricks ON | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `freecam-off` | Free camera OFF | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `freecam-on` | Free camera ON | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `unfreeze-time` | Freeze day cycle OFF | World | toggle | medium | legacy | unverified | Wired, not game-verified |
| `freeze-time` | Freeze day cycle ON | World | toggle | medium | legacy | unverified | Wired, not game-verified |
| `freeze-hard` | Hard-freeze all entities | World | action | high | debug-menu | unsafe | Wired, not game-verified |
| `dog-heal` | Heal dog | World | action | medium | legacy | unverified | Wired, not game-verified |
| `fps-off` | Hide FPS | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `cutscene-loop-off` | Loop cutscenes OFF | World | toggle | high | debug-menu | unverified | Wired, not game-verified |
| `cutscene-loop-on` | Loop cutscenes ON | World | toggle | high | debug-menu | unverified | Wired, not game-verified |
| `cutscene-reload` | Reload cutscenes | World | action | medium | debug-menu | unverified | Wired, not game-verified |
| `time` | Set time of day | World | action | low | legacy | unverified | Wired, not game-verified |
| `fps-on` | Show FPS | World | toggle | low | legacy | unverified | Wired, not game-verified |
| `cutscene-step` | Step one cutscene | World | action | medium | debug-menu | unverified | Wired, not game-verified |
| `ben-finn` | Teleport Ben Finn | World | action | high | legacy | unverified | Wired, not game-verified |
| `dog` | Teleport dog to hero | World | action | low | legacy | unverified | Wired, not game-verified |
| `sir-walter` | Teleport Sir Walter | World | action | high | legacy | unverified | Wired, not game-verified |
| `teleport-swift` | Teleport Swift to hero | World | action | low | debug-menu | unverified | Wired, not game-verified |
| `time-dawn` | Time: dawn | World | action | low | debug-menu | unverified | Wired, not game-verified |
| `time-dusk` | Time: dusk | World | action | low | debug-menu | unverified | Wired, not game-verified |
| `time-midnight` | Time: midnight | World | action | low | debug-menu | unverified | Wired, not game-verified |
| `time-noon` | Time: noon | World | action | low | debug-menu | unverified | Wired, not game-verified |

## Item vault (313 grants)

| Category | Grants |
| --- | --- |
|  | 313 |

Vault grants are queued first and only written to the game by RUN QUEUE. They are source-derived and have not been live-tested.
