"""Offline validation: package, provenance and safety invariants.
Does NOT assert that any game cheat works inside Fable III."""
import json,re,unittest
from pathlib import Path
BASE=Path(__file__).resolve().parents[1]
class Integration(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.src=(BASE/'src/CROWNsAlbion.cs').read_text('utf-8-sig')
  cls.base=json.loads((BASE/'catalog.json').read_text('utf-8'))
  cls.refs=json.loads((BASE/'references.json').read_text('utf-8'))
 @staticmethod
 def balanced_csharp(source):
  # lexical check ignoring comments, normal/verbatim string literals and chars.
  stack=[]; state='code'; i=0
  pairs={'}':'{',')':'(',']':'['}
  while i<len(source):
   c=source[i];n=source[i+1] if i+1<len(source) else ''
   if state=='code':
    if c=='/' and n=='/':state='line';i+=2;continue
    if c=='/' and n=='*':state='block';i+=2;continue
    if c=='@' and n=='"':state='verbatim';i+=2;continue
    if c=='"':state='string';i+=1;continue
    if c=="'":state='char';i+=1;continue
    if c in '{([':stack.append(c)
    if c in '})]':
     if not stack or stack.pop()!=pairs[c]:return False
   elif state=='line':
    if c=='\n':state='code'
   elif state=='block':
    if c=='*' and n=='/':state='code';i+=2;continue
   elif state in ('string','char'):
    if c=='\\':i+=2;continue
    if (c=='"' and state=='string') or (c=="'" and state=='char'):state='code'
   elif state=='verbatim':
    if c=='"' and n=='"':i+=2;continue
    if c=='"':state='code'
   i+=1
  return not stack and state in ('code','line')
 def test_catalog_preserved(self):
  self.assertEqual(len(self.base),95)
  self.assertEqual(len({x['id'] for x in self.base}),95)
  self.assertTrue(all(x.get('template') for x in self.base))
 def test_references_preserved(self):
  self.assertEqual(len(self.refs),3229)
  self.assertEqual(sum(x['kind']=='unlocker' for x in self.refs),244)
 def test_all_actual_unlocker_lua_recognized(self):
  pats=[r"^Inventory\.AddItemOfType\(GetLocalHero\(\), 'Object[A-Za-z0-9_]+'\)$",r"^Debug\.SetHeroWearingItemOfType\('Object[A-Za-z0-9_]+', true\)$",r"^Debug\.GiveAll(MeleeWeapons|RangedWeapons|SpellGauntlets)\(\)$"]
  entries=[x for x in self.refs if x['kind']=='unlocker']
  self.assertEqual(len(entries),244)
  for e in entries:self.assertTrue(any(re.fullmatch(pattern,e['preview']) for pattern in pats),e['name'])
 def test_structural_source(self):
  self.assertTrue(self.balanced_csharp(self.src),'C# lexical delimiters mismatch')
  self.assertIn('bool Execute(List<GameCommand> cmds,string lua)',self.src)
  self.assertIn('if(Execute(queue.Select',self.src)
  self.assertIn('if(!Visible||Form.ActiveForm!=this||Native.GetForegroundWindow()!=Handle)',self.src)
  self.assertIn('coopSafeToggle.Checked&&cmds.Any',self.src)
  self.assertIn('queueUnlock=B("QUEUE ITEM (EXPERIMENTAL)"',self.src)
  self.assertIn('GameCommand cmd=new GameCommand{id="unlocker:"',self.src)
  self.assertIn('if(Process.GetProcessesByName("Fable3").Length==0){Error(',self.src)
 def test_coop_default(self):
  self.assertIn('CoopSafe=true;',self.src)
  self.assertIn('prefs.CoopSafe=true;',self.src)
 def test_originals_untouched(self):
  self.assertNotIn('File.WriteAllText(Path.Combine(game,"xlive.dll")',self.src)
  self.assertNotIn('File.WriteAllText(Path.Combine(game,"Fable3.exe")',self.src)
  self.assertNotIn('File.WriteAllText(Path.Combine(game,"steam_api.dll")',self.src)
 def test_auxiliary(self):
  self.assertTrue((BASE/'CROWN_DIAGNOSE.ps1').exists())
  self.assertTrue((BASE/'CROWN_EMERGENCY_CLEAR.ps1').exists())
  self.assertTrue((BASE/'CROWN_BACKUP_SAVES.cmd').exists())
  self.assertTrue((BASE/'CROWN_Research_Library.html').exists())
if __name__=='__main__':unittest.main(verbosity=2)
