using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROWNsAlbion;
class ControllerHarness {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Sample(PadInput p,ushort b,double ms,long window=1,byte lt=0){p.Sample(b,lt,0,window,ms);}
 static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Call(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
 static void InputTests(){
  var p=new PadInput();Sample(p,0,0);Sample(p,0x40,40);Check(!p.Toggle,"L3 alone does not toggle");
  Sample(p,0xC0,80);Check(p.Toggle,"L3 plus R3 toggles");Sample(p,0xC0,120);Check(!p.Toggle,"held chord does not repeat");
  Sample(p,0x40,160);Sample(p,0xC0,200);Check(!p.Toggle,"partial chord release does not rearm");
  Sample(p,0,240);Sample(p,0xC0,280);Check(p.Toggle,"fully released chord rearms");
  Sample(p,0,320);Sample(p,0x1000,360);Check(p.Pressed==0x1000,"A edge");
  Sample(p,0x1000,400,2);Check(p.Pressed==0,"held A blocked across modal focus transition");
  Sample(p,0x1000,440,2);Check(p.Pressed==0,"held A stays blocked");
  Sample(p,0,480,2);Sample(p,0x1000,520,2);Check(p.Pressed==0x1000,"A works after release");
  Sample(p,0,560,2);Sample(p,0x2000,600,2);Check(!p.Back&&!p.Close,"B press waits for tap or hold");
  Sample(p,0,680,2);Check(p.Back&&!p.Close,"B tap backs on release");
  Sample(p,0x2000,720,2);Sample(p,0x2000,1400,2);Check(p.Close&&!p.Back,"B hold closes at threshold");
  Sample(p,0x2000,1440,2);Check(!p.Close,"B hold fires once");Sample(p,0,1480,2);Check(!p.Back,"held B release does not back again");
  Sample(p,0x8000,1520,2);Check((p.Pressed&0x8000)==0,"Y tap cannot execute");
  Sample(p,0x8000,2200,2);Check((p.Pressed&0x8000)!=0,"Y hold executes once");
  Sample(p,0x8000,2240,2);Check((p.Pressed&0x8000)==0,"held Y no duplicate");
  Sample(p,0,2280,2);Sample(p,0x10,2320,2);Check((p.Pressed&0x10)==0,"Start tap cannot execute");
  Sample(p,0x10,3000,2);Check((p.Pressed&0x10)!=0,"Start hold event");
  Sample(p,0,3040,2);Sample(p,2,3080,2);Check(p.Pressed==2,"D-pad initial movement");
  Sample(p,2,3200,2);Check(p.Pressed==0,"D-pad initial repeat delay");Sample(p,2,3440,2);Check(p.Pressed==2,"D-pad repeats after delay");
  p.Reset();Sample(p,0x1000,3480,2);Check(p.Pressed==0,"disconnect or focus reset requires neutral");
  Sample(p,0,3520,2,255);Sample(p,0x1000,3560,2,255);Check(p.Pressed==0,"trigger held during focus acquisition remains gated");
 }
 [STAThread] static int Main(){try{
  InputTests();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  using(var t=new Trainer()){
   ((Timer)Field(t,"controllerTimer")).Stop();((NotifyIcon)Field(t,"tray")).Visible=false;
   t.Show();Application.DoEvents();
   Check(((List<GameCommand>)Field(t,"catalogue")).Count==95,"native UI loads all 95 commands");
   Check(((CheckBox)Field(t,"coopSafeToggle")).Checked,"co-op protection enabled");
   Check(((CheckBox)Field(t,"controllerToggle")).Checked,"new profile controller enabled");
   var routes=(Dictionary<IntPtr,Action<Native.XState,ushort>>)Field(t,"padRoutes");
   using(var timer=new Timer()){
    timer.Interval=80;timer.Tick+=delegate{timer.Stop();var f=t.OwnedForms.First(x=>x.Text.Contains("Exact amount"));
     var n=f.Controls.OfType<NumericUpDown>().Single();Check(n.Minimum==1&&n.Maximum>=100000,"native amount bounds");
     n.Value=25000;using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"docs/qa-native/amount-native.png"));}
     routes[f.Handle](new Native.XState(),0x4000);
    };timer.Start();Call(t,"OpenGoldPad");
   }
   var q=(List<Queued>)Field(t,"queue");Check(q.Count==1&&q[0].value==25000,"native amount X queues exact 25000");
   Check(routes.Count==0,"amount route removed on close");
   using(var timer=new Timer()){
    timer.Interval=80;timer.Tick+=delegate{timer.Stop();var f=t.OwnedForms.First(x=>x.Text.Contains("Queue Review"));
     Check(f.Controls.OfType<ListBox>().Single().Items.Count==1,"native queue review shows action");
     Call(t,"ControllerBack",f.Handle);
    };timer.Start();Call(t,"OpenQueueManager");
   }
   Check(q.Count==1&&routes.Count==0,"B back retains queue and unregisters route");
   using(var timer=new Timer()){
    timer.Interval=100;timer.Tick+=delegate{timer.Stop();Check(routes.Count==1,"native research route registered");foreach(var f in t.OwnedForms)f.Close();};
    timer.Start();Call(t,"OpenResearchLibrary");
   }Check(routes.Count==0,"research route removed");
   foreach(bool confirm in new bool[]{true,false})using(var timer=new Timer()){
    timer.Interval=100;timer.Tick+=delegate{timer.Stop();Call(t,"NativeDialogButton",Native.GetForegroundWindow(),confirm);};
    timer.Start();var result=MessageBox.Show(t,"Controller confirmation test: no game action.","ALBION test",MessageBoxButtons.YesNo);
    Check(result==(confirm?DialogResult.Yes:DialogResult.No),confirm?"native A confirms Yes":"native B declines No");
   }
   t.Show();Application.DoEvents();using(var image=new Bitmap(t.Width,t.Height)){t.DrawToBitmap(image,new Rectangle(Point.Empty,t.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"docs/qa-native/menu-native.png"));}t.Hide();
   Check(!(bool)Field(t,"pulsePending")&&!(bool)Field(t,"pulseBusy"),"UI tests never dispatched a script");
  }
  Console.WriteLine("PASS "+checks+" controller and native UI checks; no physical controller or gameplay test.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
