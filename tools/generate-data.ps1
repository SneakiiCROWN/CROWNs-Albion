<#
  CROWN'S ALBION — data generator.

  Builds the runtime data files from the preserved baseline plus curated,
  source-backed additions:

    data\catalog.json       baseline 95 commands + curated debug-menu commands
    data\item_vault.json    curated grants from weapons / outfits / DLC outfits /
                            the extracted All Items Unlocker
    docs\FEATURE_LEDGER.md  honest per-command classification

  The baseline file (data\catalog_base.json) is never modified. Run this script
  after editing the additions below.

  Every addition is derived from docs\sources (the official debug-menu dump and
  the extracted item lists). Nothing here is invented gameplay logic.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $root 'data'
$docsDir = Join-Path $root 'docs'

function Param([string]$id, [string]$label, [string]$type, $min, $max, $step, $value) {
  return [ordered]@{
    id = $id; label = $label; type = $type
    min = $min; max = $max; step = $step; value = $value
  }
}

function Cmd {
  param(
    [string]$id, [string]$title, [string]$group, [string]$summary,
    [string]$template, [string]$risk, [string]$evidence, [string]$coop,
    [string]$icon, [string]$notes = '', $params = $null,
    [string]$kind = 'action', [string]$state = '', [string]$twin = ''
  )
  $o = [ordered]@{
    id = $id; title = $title; group = $group; summary = $summary
    template = $template; risk = $risk; kind = $kind
    evidence = $evidence; coop = $coop; icon = $icon
  }
  if ($state -ne '') { $o.state = $state }
  if ($twin -ne '') { $o.twin = $twin }
  if ($notes -ne '') { $o.notes = $notes }
  if ($null -ne $params) { $o.params = $params }
  return $o
}

$A = New-Object System.Collections.ArrayList

# ---------------------------------------------------------------- hero growth
function AddHero {
  [void]$A.Add((Cmd 'xp-strength' 'Add strength experience' 'Hero' 'Grant strength experience to the local hero.' `
    'Experience.Modify(GetLocalHero(), EExperienceType.EXPERIENCE_STRENGTH, {amount}, false)' 'medium' 'debug-menu' 'local' 'muscle-up' `
    'Source: debug menu. Enough experience can raise the hero level; save after testing.' `
    @((Param 'amount' 'Strength XP' 'number' 1000 1000000 1000 100000))))
  [void]$A.Add((Cmd 'xp-skill' 'Add skill experience' 'Hero' 'Grant skill experience to the local hero.' `
    'Experience.Modify(GetLocalHero(), EExperienceType.EXPERIENCE_SKILL, {amount}, false)' 'medium' 'debug-menu' 'local' 'archery-target' `
    'Source: debug menu.' @((Param 'amount' 'Skill XP' 'number' 1000 1000000 1000 100000))))
  [void]$A.Add((Cmd 'xp-will' 'Add will experience' 'Hero' 'Grant will experience to the local hero.' `
    'Experience.Modify(GetLocalHero(), EExperienceType.EXPERIENCE_WILL, {amount}, false)' 'medium' 'debug-menu' 'local' 'crystal-wand' `
    'Source: debug menu.' @((Param 'amount' 'Will XP' 'number' 1000 1000000 1000 100000))))
  [void]$A.Add((Cmd 'xp-general' 'Add general experience' 'Hero' 'Grant general experience to the local hero.' `
    'Experience.Modify(GetLocalHero(), EExperienceType.EXPERIENCE_GENERAL, {amount}, false)' 'medium' 'debug-menu' 'local' 'sparkles' `
    'Source: debug menu.' @((Param 'amount' 'General XP' 'number' 1000 1000000 1000 100000))))
  [void]$A.Add((Cmd 'hero-level' 'Set hero level' 'Hero' 'Set the hero level directly (debug range 0–10).' `
    'Debug.SetHeroLevel({level}, true, false)' 'medium' 'debug-menu' 'local' 'star-cycle' `
    'The debug slider exposes 0–10. Ability unlocks may not match a naturally levelled hero.' `
    @((Param 'level' 'Hero level' 'number' 0 10 1 5))))
  [void]$A.Add((Cmd 'health-full' 'Restore hero health' 'Hero' 'Restore the local hero to full health.' `
    'Health.Modify(GetLocalHero(), 10000, GetLocalHero())' 'low' 'debug-menu' 'local' 'health-normal' `
    'Source: debug menu. The 10,000 point top-up is clamped by the game.'))
  [void]$A.Add((Cmd 'will-full' 'Will to full' 'Hero' 'Set the hero will meter to full.' 'Debug.SetHeroWill(1.0)' 'low' 'debug-menu' 'local' 'crystal-wand' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'will-zero' 'Will to zero' 'Hero' 'Drain the hero will meter.' 'Debug.SetHeroWill(0.0)' 'low' 'debug-menu' 'local' 'crystal-wand' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'strength-full' 'Strength to full' 'Hero' 'Set hero strength to maximum.' 'Debug.SetHeroStrength(1.0)' 'low' 'debug-menu' 'local' 'muscle-up' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'strength-zero' 'Strength to zero' 'Hero' 'Reset hero strength to zero.' 'Debug.SetHeroStrength(0.0)' 'low' 'debug-menu' 'local' 'muscle-up' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'skill-full' 'Skill to full' 'Hero' 'Set hero skill to maximum.' 'Debug.SetHeroSkill(1.0)' 'low' 'debug-menu' 'local' 'archery-target' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'skill-zero' 'Skill to zero' 'Hero' 'Reset hero skill to zero.' 'Debug.SetHeroSkill(0.0)' 'low' 'debug-menu' 'local' 'archery-target' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'morality-good' 'Morality: fully good' 'Hero' 'Set morality to +1000 (fully good).' 'Debug.SetHeroMorality(1000.0)' 'medium' 'debug-menu' 'local' 'angel-wings' 'Also triggers good-morph appearance changes.'))
  [void]$A.Add((Cmd 'morality-evil' 'Morality: fully evil' 'Hero' 'Set morality to -1000 (fully evil).' 'Debug.SetHeroMorality(-1000.0)' 'medium' 'debug-menu' 'local' 'burning-skull' 'Also triggers evil-morph appearance changes.'))
  [void]$A.Add((Cmd 'morality-zero' 'Morality: neutral' 'Hero' 'Reset morality to zero.' 'Debug.SetHeroMorality(0.0)' 'medium' 'debug-menu' 'local' 'weight-scale' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'purity-pure' 'Purity: fully pure' 'Hero' 'Set purity to +1000.' 'Debug.SetHeroPurity(1000)' 'medium' 'debug-menu' 'local' 'holy-water' 'Also triggers purity-morph appearance changes.'))
  [void]$A.Add((Cmd 'purity-corrupt' 'Purity: fully corrupt' 'Hero' 'Set purity to -1000.' 'Debug.SetHeroPurity(-1000)' 'medium' 'debug-menu' 'local' 'poison' 'Also triggers corrupt-morph appearance changes.'))
  [void]$A.Add((Cmd 'purity-zero' 'Purity: neutral' 'Hero' 'Reset purity to zero.' 'Debug.SetHeroPurity(0)' 'medium' 'debug-menu' 'local' 'weight-scale' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'tan-full' 'Tan: fully tanned' 'Hero' 'Set the tan morph to maximum.' 'Debug.SetHeroTan(1.0)' 'low' 'debug-menu' 'local' 'sun' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'tan-pale' 'Tan: fully pale' 'Hero' 'Set the tan morph to minimum.' 'Debug.SetHeroTan(0.0)' 'low' 'debug-menu' 'local' 'moon' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'fat-full' 'Weight: maximum' 'Hero' 'Set the fat morph to maximum.' 'Debug.SetHeroFat(1.0)' 'low' 'debug-menu' 'local' 'weight' 'Source: debug menu.'))
  [void]$A.Add((Cmd 'fat-zero' 'Weight: minimum' 'Hero' 'Set the fat morph to zero.' 'Debug.SetHeroFat(0.0)' 'low' 'debug-menu' 'local' 'feather' 'Source: debug menu.'))
}
AddHero

# ---------------------------------------------------------------- appearance
function AddAppearance {
  $hair = @(
    @('hair-standard', 'Hair: standard', 'ObjectHairF3Standard'),
    @('hair-long', 'Hair: long', 'ObjectHairF3Long'),
    @('hair-long-thick', 'Hair: long and thick', 'ObjectHairF3LongThick'),
    @('hair-short-ponytail', 'Hair: short ponytail', 'ObjectHairF3ShortPonyTail'),
    @('hair-bob', 'Hair: bob', 'ObjectHairF3Bob'),
    @('hair-short-fringed', 'Hair: short fringed', 'ObjectHairF3ShortFringed'),
    @('hair-mohawk', 'Hair: mohawk', 'ObjectHairF3Mohawk'),
    @('hair-shaved', 'Hair: shaved', 'ObjectHairShavedHead')
  )
  foreach ($h in $hair) {
    [void]$A.Add((Cmd $h[0] $h[1] 'Appearance' ('Apply the ' + $h[1].Substring(6) + ' hairstyle to the local hero.') `
      ("Debug.SetHeroWearingItemOfType('" + $h[2] + "', true)") 'medium' 'debug-menu' 'local' 'hair-strands' `
      'Source: debug menu. Cosmetic; the game may keep the previous style in the save until the hero is re-rendered.'))
  }
  $beards = @(
    @('beard-classic', 'Beard: classic', 'ObjectBeardF3Beard'),
    @('beard-full', 'Beard: full', 'ObjectBeardF3Full'),
    @('beard-long-tied', 'Beard: long tied', 'ObjectBeardF3LongTied'),
    @('beard-goatee', 'Beard: goatee', 'ObjectBeardF3Mouth'),
    @('beard-muttons', 'Beard: muttons', 'ObjectBeardF3Muttons'),
    @('beard-tache', 'Beard: moustache', 'ObjectBeardF3Tache'),
    @('beard-tache-beard', 'Beard: moustache and beard', 'ObjectBeardF3TacheBeard'),
    @('beard-tache-sidies', 'Beard: moustache and sideburns', 'ObjectBeardF3TacheSidies')
  )
  foreach ($b in $beards) {
    [void]$A.Add((Cmd $b[0] $b[1] 'Appearance' ('Apply the ' + $b[1].Substring(7) + ' facial hair to the local hero.') `
      ("Debug.SetHeroWearingItemOfType('" + $b[2] + "', true)") 'medium' 'debug-menu' 'local' 'beard' `
      'Source: debug menu. Cosmetic; may need a scene change to refresh.'))
  }
  $suits = @(
    @('suit-will-m', 'Outfit: Will (male)', 'ObjectSuitWillM'),
    @('suit-will-f', 'Outfit: Will (female)', 'ObjectSuitWillF'),
    @('suit-strength-m', 'Outfit: Strength (male)', 'ObjectSuitStrengthM'),
    @('suit-strength-f', 'Outfit: Strength (female)', 'ObjectSuitStrengthF'),
    @('suit-skill-m', 'Outfit: Skill (male)', 'ObjectSuitSkillM'),
    @('suit-skill-f', 'Outfit: Skill (female)', 'ObjectSuitSkillF')
  )
  foreach ($s in $suits) {
    [void]$A.Add((Cmd $s[0] $s[1] 'Appearance' ('Wear the ' + $s[1].Substring(9) + ' suit.') `
      ("Debug.SetHeroWearingItemOfType('" + $s[2] + "', true)") 'medium' 'debug-menu' 'local' 'fur-shirt' `
      'Source: debug menu. Cosmetic; gender-specific meshes may not fit both hero bodies.'))
  }
}
AddAppearance

# ---------------------------------------------------------------- combat
function AddCombat {
  [void]$A.Add((Cmd 'finisher-test-on' 'Finishers: test mode ON' 'Combat' 'Force finishers to be attempted on every hit.' `
    'Debug.SetCombatFinishersTestMode(true)' 'medium' 'debug-menu' 'unverified' 'crossed-swords' 'Debug-only combat path. Test on a spare save.' `
    $null 'toggle' 'on' 'finisher-test-off'))
  [void]$A.Add((Cmd 'finisher-test-off' 'Finishers: test mode OFF' 'Combat' 'Restore normal finisher selection.' `
    'Debug.SetCombatFinishersTestMode(false)' 'medium' 'debug-menu' 'unverified' 'crossed-swords' '' `
    $null 'toggle' 'off' 'finisher-test-on'))
  [void]$A.Add((Cmd 'finisher-allow-all' 'Finishers: allow every match' 'Combat' 'Clear the finisher match filter so any animation can trigger.' `
    "Debug.SetCombatFinishersAllowOnlyMatchStrings('', '', '')" 'medium' 'debug-menu' 'unverified' 'spears' `
    'Debug-only. Combined with the ignore-* toggles this can produce unusual combat states.'))
  [void]$A.Add((Cmd 'ai-off' 'Disable world AI' 'Combat' 'Stop the AI manager from updating.' 'AIManager.DisableUpdate = true' 'high' 'debug-menu' 'unsafe' 'brain' `
    'Freezes NPC logic. It may soft-lock scenes and is NOT co-op safe. Reload the save to recover.'))
  [void]$A.Add((Cmd 'ai-on' 'Enable world AI' 'Combat' 'Resume the AI manager.' 'AIManager.DisableUpdate = false' 'high' 'debug-menu' 'unsafe' 'brain' `
    'Restores AI after Disable world AI.'))
}
AddCombat

# ---------------------------------------------------------------- world
function AddWorld {
  $times = @(
    @('time-dawn', 'Time: dawn', 6), @('time-noon', 'Time: noon', 12),
    @('time-dusk', 'Time: dusk', 18), @('time-midnight', 'Time: midnight', 0)
  )
  foreach ($t in $times) {
    [void]$A.Add((Cmd $t[0] $t[1] 'World' ('Set the time of day to ' + $t[1].Substring(6) + '.') `
      ('Timing.SetTimeOfDay(' + $t[2] + ')') 'low' 'debug-menu' 'unverified' 'clockwork' `
      'Shared world clock; both players see the change in a co-op session.'))
  }
  [void]$A.Add((Cmd 'freeze-hard' 'Hard-freeze all entities' 'World' 'Fully freeze every entity in the level.' `
    'ScriptFunction.FullFreezeAllEntitiesInLevel()' 'high' 'debug-menu' 'unsafe' 'snowflake-1' `
    'Hard freeze can trap the player and may not be reversible with Unfreeze.'))
  [void]$A.Add((Cmd 'cutscene-loop-on' 'Loop cutscenes ON' 'World' 'Repeat active cutscenes.' 'ScriptFunction.RepeatCutscenes(true)' 'high' 'debug-menu' 'unverified' 'scroll-unfurled' `
    'Debug-only. Can trap the game in a cutscene loop; reload if stuck.' $null 'toggle' 'on' 'cutscene-loop-off'))
  [void]$A.Add((Cmd 'cutscene-loop-off' 'Loop cutscenes OFF' 'World' 'Stop repeating cutscenes.' 'ScriptFunction.RepeatCutscenes(false)' 'high' 'debug-menu' 'unverified' 'scroll-unfurled' '' $null 'toggle' 'off' 'cutscene-loop-on'))
  [void]$A.Add((Cmd 'cutscene-step' 'Step one cutscene' 'World' 'Advance the active cutscene by one step.' 'ScriptFunction.StepOneCutscene()' 'medium' 'debug-menu' 'unverified' 'scroll-unfurled' 'Debug-only.'))
  [void]$A.Add((Cmd 'cutscene-reload' 'Reload cutscenes' 'World' 'Reload cutscene state.' 'ScriptFunction.ReloadCutscenes()' 'medium' 'debug-menu' 'unverified' 'scroll-unfurled' 'Debug-only.'))
  [void]$A.Add((Cmd 'teleport-swift' 'Teleport Swift to hero' 'World' 'Bring Swift next to the hero.' 'ScriptFunction.TeleportSwiftNextToHero()' 'low' 'debug-menu' 'unverified' 'boots' 'Only meaningful in chapters where Swift exists.'))
}
AddWorld

# ---------------------------------------------------------------- kingdom
function AddKingdom {
  [void]$A.Add((Cmd 'gold-zero' 'Set hero gold to zero' 'Kingdom' 'Set the hero purse to zero. This is a SET, not an add.' 'Money.Set(GetLocalHero(),0,0)' 'high' 'debug-menu' 'unverified' 'coins-pile' `
    'Destructive. Back up the save first; there is no undo in this trainer.'))
  [void]$A.Add((Cmd 'province-update' 'Force province update' 'Kingdom' 'Force an immediate province simulation tick.' 'Province.ForceProvinceUpdate()' 'medium' 'debug-menu' 'unverified' 'castle' 'May change tax and town state immediately.'))
  [void]$A.Add((Cmd 'ruler-on' 'Become ruler ON' 'Kingdom' 'Enable the debug ruler flag.' 'Province.SetHeroRuler(true)' 'high' 'debug-menu' 'unverified' 'crown' 'Debug-only. May corrupt story progression if used before the coronation.' $null 'toggle' 'on' 'ruler-off'))
  [void]$A.Add((Cmd 'ruler-off' 'Become ruler OFF' 'Kingdom' 'Disable the debug ruler flag.' 'Province.SetHeroRuler(false)' 'high' 'debug-menu' 'unverified' 'crown' '' $null 'toggle' 'off' 'ruler-on'))
  [void]$A.Add((Cmd 'town-demands-accept' 'Accept all town demands' 'Kingdom' 'Accept every pending town demand in the active province.' 'Debug.RulerAcceptTownDemandsInProvince(Province.GetActiveProvinceID())' 'high' 'debug-menu' 'unverified' 'weight-scale' 'Changes kingdom state. Back up first.'))
  [void]$A.Add((Cmd 'town-demands-reject' 'Reject all town demands' 'Kingdom' 'Reject every pending town demand in the active province.' 'Debug.RulerRejectTownDemandsInProvince(Province.GetActiveProvinceID())' 'high' 'debug-menu' 'unverified' 'weight-scale' 'Changes kingdom state. Back up first.'))
  [void]$A.Add((Cmd 'followers-up' 'Add 50,000 followers' 'Kingdom' 'Add renown/followers in the active province.' 'Stats.ModifyRenownAbsolute(GetLocalHero(), 50000, Province.GetActiveProvinceID())' 'medium' 'debug-menu' 'unverified' 'flag-objective' 'Debug menu uses this for the full-wing morph test.'))
  [void]$A.Add((Cmd 'followers-down' 'Remove 50,000 followers' 'Kingdom' 'Remove renown/followers in the active province.' 'Stats.ModifyRenownAbsolute(GetLocalHero(), -50000, Province.GetActiveProvinceID())' 'medium' 'debug-menu' 'unverified' 'flag-objective' ''))
}
AddKingdom

# ---------------------------------------------------------------- spawning
function AddSpawning {
  $families = @(
    @('spawn-renegade-family', 'Spawn renegade family', 'CreatureFamilyRenegadesLevel04_v1', '3 melee and 3 ranged renegades'),
    @('spawn-hollowmen', 'Spawn hollow men family', 'CreatureFamilyHollowMenLevel04_v1', '9 melee and 3 ranged hollow men'),
    @('spawn-logan', 'Spawn Logan''s soldiers', 'CreatureFamilyLoganSoldiersLevel06_v1', '5 soldiers and 1 grenadier'),
    @('spawn-hobbe-family', 'Spawn hobbe family', 'CreatureFamilyHobbesSmallNoStilts3', 'a small hobbe family')
  )
  foreach ($f in $families) {
    [void]$A.Add((Cmd $f[0] $f[1] 'Spawning' ('Spawn ' + $f[3] + ' around the hero.') `
      ("Debug.SpawnCreatureFamily('" + $f[2] + "')") 'high' 'debug-menu' 'unverified' 'burning-skull' `
      'Combat spawning. It can overwhelm the area and is not co-op tested.'))
  }
  [void]$A.Add((Cmd 'spawn-minions' 'Spawn minions around hero' 'Spawning' 'Spawn a chosen number of melee minions around the hero.' `
    "Debug.CreateCombatantsAroundHero('CreatureMinionMelee', {count}, true)" 'high' 'debug-menu' 'unverified' 'burning-skull' `
    'Combat spawning. Keep counts small.' @((Param 'count' 'Minions' 'number' 1 20 1 3))))
  [void]$A.Add((Cmd 'spawn-dervish' 'Spawn dervish ambush' 'Spawning' 'Create a dervish ambush around the hero.' `
    'Debug.CreateDervishAmbush({count}, false)' 'high' 'debug-menu' 'unverified' 'half-tornado' `
    'Combat spawning. Keep counts small.' @((Param 'count' 'Dervishes' 'number' 1 10 1 3))))
  [void]$A.Add((Cmd 'spawn-villager-named' 'Spawn a villager at hero' 'Spawning' 'Create a villager on the hero position.' `
    "Debug.CreateEntityAtPosition('CreatureVillager', 'Joe Blow', GetLocalHero():GetPosition())" 'medium' 'debug-menu' 'unverified' 'castle' `
    'Debug entity creation. The extra villager may persist in the level until reload.'))
}
AddSpawning

# ---------------------------------------------------------------- inventory
function AddInventory {
  [void]$A.Add((Cmd 'show-all-weapons' 'Show all weapons in the sanctuary' 'Inventory' 'Reveal every weapon on the sanctuary display.' 'Gameflow.RoadToRule.SHOW_ALL_WEAPONS()' 'medium' 'debug-menu' 'unverified' 'chest' `
    'Display-only helper from the debug menu; it does not add weapons to the inventory.'))
  [void]$A.Add((Cmd 'hide-all-weapons' 'Hide all weapons in the sanctuary' 'Inventory' 'Hide every weapon on the sanctuary display.' 'Gameflow.RoadToRule.HIDE_ALL_WEAPONS()' 'medium' 'debug-menu' 'unverified' 'chest' ''))
}
AddInventory

# ---------------------------------------------------------------- chaos
function AddChaos {
  $relations = @(
    @('relation-friend', 'Relationship: friends', 'ERS_FRIEND'),
    @('relation-neutral', 'Relationship: neutral', 'ERS_NEUTRAL'),
    @('relation-hate', 'Relationship: hate', 'ERS_HATE'),
    @('relation-fear', 'Relationship: fear', 'ERS_FEAR')
  )
  foreach ($r in $relations) {
    [void]$A.Add((Cmd $r[0] $r[1] 'Chaos' ('Reset the current relationship stage to ' + $r[1].Substring(14) + '.') `
      ("Debug.ResetCurrentRelationshipStage(GetLocalHero(), ERelationshipStages." + $r[2] + ")") 'medium' 'debug-menu' 'unverified' 'two-feathers' `
      'Villager social state change. It may not update all NPCs until the area reloads.'))
  }
}
AddChaos

# ---------------------------------------------------------------- merge + validate
$base = Get-Content (Join-Path $dataDir 'catalog_base.json') -Raw | ConvertFrom-Json

# The recovered v1.6.1 catalogue predates the richer v2.0 metadata. Backfill the
# missing fields so the shipped data is self-describing and the ledger is honest:
# these commands are recovered (not debug-menu sourced) and remain unverified.
$iconByGroup = @{
  Hero = 'muscle-up'; Combat = 'crossed-swords'; Kingdom = 'coins-pile'
  Inventory = 'chest'; Appearance = 'fur-shirt'; World = 'clockwork'
  Spawning = 'burning-skull'; Chaos = 'crossed-bones'; Utilities = 'gears'
}
foreach ($c in $base) {
  $props = $c.PSObject.Properties.Name
  if (($props -notcontains 'evidence') -or [string]::IsNullOrWhiteSpace([string]$c.evidence)) {
    $c | Add-Member -NotePropertyName evidence -NotePropertyValue 'legacy' -Force
  }
  if (($props -notcontains 'coop') -or [string]::IsNullOrWhiteSpace([string]$c.coop)) {
    $c | Add-Member -NotePropertyName coop -NotePropertyValue 'unverified' -Force
  }
  if (($props -notcontains 'notes') -or [string]::IsNullOrWhiteSpace([string]$c.notes)) {
    $c | Add-Member -NotePropertyName notes -NotePropertyValue 'Recovered from the CROWN''s ALBION v1.6.1 catalogue. Not live-verified in this build.' -Force
  }
  if (($props -notcontains 'icon') -or [string]::IsNullOrWhiteSpace([string]$c.icon)) {
    $icon = if ($iconByGroup.ContainsKey([string]$c.group)) { $iconByGroup[[string]$c.group] } else { 'scroll-unfurled' }
    $c | Add-Member -NotePropertyName icon -NotePropertyValue $icon -Force
  }
  if (($props -notcontains 'twin') -and (($c.kind -eq 'toggle') -or ($props -contains 'state'))) {
    $other = if ($c.id.EndsWith('-on')) { $c.id.Substring(0, $c.id.Length - 3) + '-off' } elseif ($c.id.EndsWith('-off')) { $c.id.Substring(0, $c.id.Length - 4) + '-on' } else { '' }
    if ($other -ne '' -and ($base | Where-Object { $_.id -eq $other })) { $c | Add-Member -NotePropertyName twin -NotePropertyValue $other -Force }
  }
}

$merged = New-Object System.Collections.ArrayList
$ids = New-Object System.Collections.Generic.HashSet[string]
foreach ($c in $base) { [void]$merged.Add($c); [void]$ids.Add([string]$c.id) }
$added = 0
foreach ($c in $A) {
  if ($ids.Contains([string]$c.id)) { throw "Duplicate catalogue id: $($c.id)" }
  [void]$merged.Add([pscustomobject]$c)
  [void]$ids.Add([string]$c.id)
  $added++
}

# Validate templates: every placeholder must match a declared parameter.
$errors = New-Object System.Collections.ArrayList
foreach ($c in $merged) {
  $matches = [regex]::Matches([string]$c.template, '\{([A-Za-z0-9_]+)\}')
  $declared = @()
  if ($null -ne $c.params) { foreach ($p in $c.params) { $declared += [string]$p.id } }
  foreach ($m in $matches) {
    if ($declared -notcontains $m.Groups[1].Value) { [void]$errors.Add("$($c.id): placeholder {$($m.Groups[1].Value)} has no parameter") }
  }
  if ($declared.Count -gt 1) { [void]$errors.Add("$($c.id): more than one parameter (engine supports one)") }
}
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ }; throw 'Catalogue validation failed.' }

$json = $merged | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText((Join-Path $dataDir 'catalog.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("catalogue: {0} baseline + {1} curated = {2} commands" -f $base.Count, $added, $merged.Count) -ForegroundColor Green

# ---------------------------------------------------------------- item vault
function Slug([string]$s) {
  $s = $s.ToLowerInvariant()
  $s = [regex]::Replace($s, '[^a-z0-9]+', '-')
  return $s.Trim('-')
}

function Humanize([string]$objectId) {
  $s = $objectId -replace '^ObjectInventory', ''
  $s = $s -replace '^Object', ''
  $s = [regex]::Replace($s, '([a-z0-9])([A-Z])', '$1 $2')
  $s = [regex]::Replace($s, '([A-Z]+)([A-Z][a-z])', '$1 $2')
  return $s.Trim()
}

function WeaponIcon([string]$objectId) {
  if ($objectId -match 'Hammer') { return 'battle-axe' }
  if ($objectId -match 'Gauntlet') { return 'crystal-wand' }
  if ($objectId -match 'Rifle|Pistol|Crossbow') { return 'crosshair' }
  return 'broadsword'
}

$vault = New-Object System.Collections.ArrayList
$vaultIds = New-Object System.Collections.Generic.HashSet[string]
$vaultTemplates = New-Object System.Collections.Generic.HashSet[string]

function AddVault([string]$id, [string]$name, [string]$category, [string]$template, [string]$risk, [string]$evidence, [string]$note, [string]$icon) {
  if ([string]::IsNullOrWhiteSpace($template)) { return }
  if (-not $vaultTemplates.Add($template.Trim())) { return }
  $unique = $id
  $n = 2
  while (-not $vaultIds.Add($unique)) { $unique = $id + '-' + $n; $n++ }
  [void]$vault.Add([ordered]@{
    id = $unique; name = $name; category = $category; template = $template
    risk = $risk; evidence = $evidence; note = $note; icon = $icon
  })
}

# 1. Legendary and common weapons from the extracted weapon list.
$weaponFile = Join-Path $docsDir 'sources\Fable 3 Weapons.txt'
if (Test-Path $weaponFile) {
  foreach ($line in Get-Content $weaponFile) {
    $m = [regex]::Match($line, "GiveHeroWeapon\('([^']+)'\)")
    if (-not $m.Success) { continue }
    $obj = $m.Groups[1].Value
    AddVault ('weapon-' + (Slug $obj)) (Humanize $obj) 'Weapons' ("Debug.GiveHeroWeapon('" + $obj + "')") 'medium' 'debug-menu' `
      'Source: Fable 3 Weapons.txt. Repeat grants may duplicate inventory entries.' (WeaponIcon $obj)
  }
}

# 2. Outfit sets from the extracted outfit list (Inventory.AddItemOfType bundles).
function ImportOutfitSections([string]$path, [string]$pattern, [string]$category, [string]$note) {
  if (-not (Test-Path $path)) { return }
  $name = $null
  $lines = New-Object System.Collections.ArrayList
  foreach ($line in Get-Content $path) {
    $m = [regex]::Match($line, '^\s*--\s*(.+?)\s*--\s*$')
    if ($m.Success) {
      if ($name -ne $null -and $lines.Count -gt 0) {
        AddVault ('outfit-' + (Slug $name)) $name $category ($lines -join "`n") 'medium' 'debug-menu' $note 'fur-shirt'
      }
      $name = $m.Groups[1].Value
      $lines = New-Object System.Collections.ArrayList
      continue
    }
    if ($name -eq $null) { continue }
    $t = [regex]::Match($line, $pattern)
    if ($t.Success) { [void]$lines.Add($t.Value.Trim()) }
  }
  if ($name -ne $null -and $lines.Count -gt 0) {
    AddVault ('outfit-' + (Slug $name)) $name $category ($lines -join "`n") 'medium' 'debug-menu' $note 'fur-shirt'
  }
}
ImportOutfitSections (Join-Path $docsDir 'sources\Fable 3 Outfits.txt') "Inventory\.AddItemOfType\([^\)]+\)" 'Outfits' 'Source: Fable 3 Outfits.txt. Grants the whole set; repeat use may duplicate clothing.'
ImportOutfitSections (Join-Path $docsDir 'sources\Fable 3 DLC Outfits.txt') "Debug\.SetHeroWearingItemOfType\([^\)]+\)" 'Outfits' 'Source: Fable 3 DLC Outfits.txt. Wears the set directly; DLC ownership may still be required.'

# 3. All Items Unlocker grants.
$unlockerFile = Join-Path $dataDir 'unlocker_commands.json'
if (Test-Path $unlockerFile) {
  $unlockers = Get-Content $unlockerFile -Raw | ConvertFrom-Json
  foreach ($u in $unlockers) {
    $cat = 'Inventory'
    if ($u.category -match 'Tattoo') { $cat = 'Tattoos' }
    elseif ($u.category -match 'Makeup') { $cat = 'Makeup' }
    elseif ($u.category -match 'Hair') { $cat = 'Hair' }
    elseif ($u.category -match 'Clothing') { $cat = 'Clothing' }
    elseif ($u.category -match 'Appearance') { $cat = 'Appearance' }
    elseif ($u.category -match 'Weapon|Inventory') { $cat = 'Weapons' }
    elseif ($u.category -match 'GFWL') { $cat = 'GFWL Exclusives' }
    elseif ($u.category -match 'Bulk') { $cat = 'Bulk Grants' }
    $icon = 'open-treasure-chest'
    if ($cat -eq 'Tattoos') { $icon = 'paint-brush' }
    elseif ($cat -eq 'Makeup') { $icon = 'palette' }
    elseif ($cat -eq 'Hair') { $icon = 'hair-strands' }
    elseif ($cat -eq 'Clothing') { $icon = 'fur-shirt' }
    elseif ($cat -eq 'Appearance') { $icon = 'carnival-mask' }
    elseif ($cat -eq 'Weapons') { $icon = 'broadsword' }
    elseif ($cat -eq 'GFWL Exclusives') { $icon = 'crown-coin' }
    $risk = 'medium'
    if ($cat -eq 'Bulk Grants') { $risk = 'high' }
    AddVault ('unlock-' + (Slug $u.name)) $u.name $cat ([string]$u.preview) $risk 'community' `
      'Source: machine4578 / CatchLightning All Items Unlocker. Not live-tested; repeat may duplicate items.' $icon
  }
}

$vaultJson = $vault | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText((Join-Path $dataDir 'item_vault.json'), $vaultJson, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("item vault: {0} curated grants" -f $vault.Count) -ForegroundColor Green

# ---------------------------------------------------------------- feature ledger
function Status([string]$evidence, [string]$kind) {
  if ($evidence -eq 'reference') { return 'UI-only reference' }
  return 'Wired, not game-verified'
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# CROWN''s ALBION Feature Ledger')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Generated from `data/catalog.json` and `data/item_vault.json` on ' + (Get-Date -Format 'yyyy-MM-dd HH:mm') + '.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## How to read this ledger')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Status | Meaning |')
[void]$sb.AppendLine('| --- | --- |')
[void]$sb.AppendLine('| Verified in game | Confirmed by watching the effect in Fable III on a spare save. |')
[void]$sb.AppendLine('| Integration-verified | The script path, manifest registration and injector hand-off chain was checked on disk. |')
[void]$sb.AppendLine('| Wired, not game-verified | The command is in the catalogue, its template is validated, and it is dispatchable. No live effect was observed. |')
[void]$sb.AppendLine('| UI-only reference | Shown for research; it is not dispatched as a cheat. |')
[void]$sb.AppendLine('| Broken | Known not to work. |')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('**No command in this build has been verified live in Fable III yet.** The integration chain (marked Lua → `data\scripts\MyMod\MyScript01.lua` → `DLC\10_ScriptInjector` → Fable III) is verified structurally, but every row below remains wired-not-verified until a supervised live test is recorded.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Evidence: `debug-menu` = expression taken from the official Fable III debug-menu dump; `legacy` = recovered from the v1.6.1 trainer catalogue; `reference` = research entry only; `community` = source-derived community grant.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Co-op column: `safe` = menu/UI only; `local` = touches the local hero only; `unverified` = world or shared state; `unsafe` = may break or desync a session.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Catalogue (' + $merged.Count + ' commands)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| ID | Command | Group | Kind | Risk | Evidence | Co-op | Status |')
[void]$sb.AppendLine('| --- | --- | --- | --- | --- | --- | --- | --- |')
foreach ($c in ($merged | Sort-Object group, title)) {
  [void]$sb.AppendLine('| `' + $c.id + '` | ' + $c.title + ' | ' + $c.group + ' | ' + $c.kind + ' | ' + $c.risk + ' | ' + $c.evidence + ' | ' + $c.coop + ' | ' + (Status $c.evidence $c.kind) + ' |')
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Item vault (' + $vault.Count + ' grants)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Category | Grants |')
[void]$sb.AppendLine('| --- | --- |')
foreach ($g in ($vault | Group-Object category | Sort-Object Name)) {
  [void]$sb.AppendLine('| ' + $g.Name + ' | ' + $g.Count + ' |')
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Vault grants are queued first and only written to the game by RUN QUEUE. They are source-derived and have not been live-tested.')
[System.IO.File]::WriteAllText((Join-Path $docsDir 'FEATURE_LEDGER.md'), $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host 'ledger: docs\FEATURE_LEDGER.md' -ForegroundColor Green
