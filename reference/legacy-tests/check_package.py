import json,re,pathlib,hashlib
root=pathlib.Path(__file__).resolve().parents[1]
required=['CROWN_ALBION.cmd','CROWN_EMERGENCY_CLEAR.cmd','CROWN_EMERGENCY_CLEAR.ps1','catalog.json','src/CROWNsAlbion.cs','Legacy_v1_0/src/commands.js','README.md']
for path in required: assert (root/path).is_file(),path
catalog=json.loads((root/'catalog.json').read_text())
assert len(catalog)==95
assert len({entry['id'] for entry in catalog})==95
for c in catalog:
 assert re.match(r'^[a-z0-9-]+$',c['id'])
 assert c['risk'] in ('low','medium','high')
 assert c['template']
 assert c['group'] in ('Hero','Combat','Kingdom','Inventory','World','Spawning','Chaos','Utilities')
 for p in c.get('params',[]):
  assert '{'+p['id']+'}' in c['template'] or c.get('repeatable'),(c['id'],p['id'])
  if p['type']!='select': assert p['max']>=p['min'],c['id']
cs=(root/'src/CROWNsAlbion.cs').read_text(encoding='utf-8-sig')
for marker in ['RegisterHotKey','XInputGetState','EXPECTED_HASH','CROWN_ALBION_MANAGED','BackupFile','File.Replace','File.ReadAllText(script)','MyScript01.lua','ClearPulse','Favourites','LoadLoadout','LoadPreset']:
 assert marker in cs,marker
assert 'xlive.dll' in cs # documented only; no writes
writes=[x.group(1) for x in re.finditer(r'WriteAtomic\((\w+),',cs)]
assert set(writes) <= {'script','manifest','path'},writes
print('PASS: file presence, 95 unique command templates, preflight and script-write boundaries')
print('Offline package checks only; see docs/QA_v1_6_1.md for build and native UI results.')
