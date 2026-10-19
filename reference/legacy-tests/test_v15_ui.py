"""Regression checks against v1.5 source. Offline only; not a WinForms or game runtime test."""
import re,unittest,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE=Path('/mnt/data/CROWNs_Albion_v1_4_Integrated')
class ExactAmounts(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.src=(ROOT/'src/CROWNsAlbion.cs').read_text(encoding='utf-8-sig')
  cls.catalog=json.loads((ROOT/'catalog.json').read_text())
 def test_source_preserved(self):
  if BASE.exists():
   self.assertEqual((ROOT/'catalog.json').read_bytes(),(BASE/'catalog.json').read_bytes())
   self.assertEqual((ROOT/'references.json').read_bytes(),(BASE/'references.json').read_bytes())
 def test_exact_amount_templates_are_add_only(self):
  for cmd_id in ('gold','treasury','seals'):
   cmd=next(c for c in self.catalog if c['id']==cmd_id)
   p=cmd['params'][0]
   self.assertEqual(p['min'],1)
   self.assertGreaterEqual(p['max'],100000)
   self.assertIn('{amount}',cmd['template'])
   self.assertIn('25000',cmd['template'].replace('{amount}','25000'))
   self.assertNotIn('Set(',cmd['template'])
 def test_direct_exact_controls(self):
  for name in ('QUICK GOLD','EXACT AMOUNT','USE + QUEUE','USE VALUE','NumericUpDown','OpenAmountPad','OpenGoldPad'):
   self.assertIn(name,self.src)
  self.assertIn('new int[]{1,100,500,1000,5000,10000,25000,50000,100000,1000000}',self.src)
  self.assertIn('n!=Math.Truncate(n)',self.src)
  body=self.src.split('void OpenAmountPad(){',1)[1].split('void Favourite(){',1)[0]
  self.assertNotIn('Execute(',body) # nothing runs when choosing a number
  self.assertIn('QueueSelected()',body)
 def test_queue_manager_and_long_press(self):
  self.assertIn('REVIEW QUEUE',self.src)
  self.assertIn('void OpenQueueManager()',self.src)
  self.assertIn('OpenQueueManager();return;} // Back / View',self.src)
  self.assertIn('Hold-to-confirm destructive shortcuts',self.src)
  self.assertIn('now-since>=650',self.src)
  self.assertNotIn('if((pressed&0x8000)!=0)RunSelected();',self.src)
 def test_focus_is_conditional(self):
  self.assertIn('GetWindowThreadProcessId',self.src)
  self.assertIn('if(!IsGameForeground()&&!IsMenuForeground())return;',self.src)
  self.assertIn('if(!own&&!IsGameForeground()){padInput.Reset();return;}',self.src)
  self.assertIn('const ushort chord=0x00C0',self.src)
  self.assertIn('if(restore)RestoreGameFocus();',self.src)
  self.assertIn('if(!Visible||Form.ActiveForm!=this||Native.GetForegroundWindow()!=Handle)',self.src)
 def test_sidebar_not_overlapping(self):
  self.assertIn('actionStrip.Dock=DockStyle.Bottom',self.src)
  self.assertIn('categories.Height=Math.Max(110,main.Panel1.Height-43-187-9)',self.src)
  self.assertNotIn('main.Panel1.Height-255',self.src)
 def test_safeguards_remain(self):
  for guard in ('CoopSafe=true','coopSafeToggle.Checked&&cmds.Any','Process.GetProcessesByName("Fable3").Length==0','if(pulseBusy||pulsePending)','File.Replace(temp,path,null)','if(Execute(queue.Select'):
   self.assertIn(guard,self.src)
  self.assertNotIn('File.WriteAllBytes(Path.Combine(game,"xlive.dll")',self.src)
 def test_build_script(self):
  launcher=(ROOT/'CROWN_ALBION.cmd').read_text()
  self.assertIn('v1.6',launcher)
  self.assertIn('/r:System.Web.Extensions.dll',launcher)
  self.assertIn('src\\CROWNsAlbion.cs',launcher)
if __name__=='__main__':unittest.main(verbosity=2)
