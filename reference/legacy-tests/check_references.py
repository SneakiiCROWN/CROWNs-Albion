"""Offline source/reference checks. Does not run Fable III or executable mods."""
from pathlib import Path
import json
base=Path(__file__).resolve().parents[1]
refs=json.loads((base/'references.json').read_text(encoding='utf8'))
commands=json.loads((base/'catalog.json').read_text(encoding='utf8'))
assert len(commands)==95, 'unexpected original actions count'
assert len(refs)==3229, 'reference count changed unexpectedly'
assert sum(r['kind']=='item' for r in refs)==1556
assert sum(r['kind']=='debug' for r in refs)==1429
assert len(set((r['kind'],r['raw']) for r in refs))==len(refs)
assert sum(r['kind']=='unlocker' for r in refs)==244
assert sum(r['category']=='Unlocker · Bulk Grants' for r in refs)==3
assert any(r['preview']=="Debug.GiveAllSpellGauntlets()" for r in refs)
assert any(r['name']=='ObjectInventoryLegendaryPistolBarnumificator' for r in refs)
assert any('MultiplayerMarriage' in r['raw'] for r in refs)
source=(base/'src'/'CROWNsAlbion.cs').read_text(encoding='utf-8-sig')
assert 'void OpenResearchLibrary()' in source
assert 'researchEntries=serializer.Deserialize<List<ResearchRecord>>' in source
assert 'F8' in source and 'ClearPulse(bool manual)' in source
assert (base/'CROWN_Research_Library.html').exists()
print('PASS: 3,229 references including 244 source-derived unlocker Lua calls; 95 legacy actions preserved.')
