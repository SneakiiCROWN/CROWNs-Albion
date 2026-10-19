using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using CROWNsAlbion;
class DialogHarness {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr hwnd);
 static int checks;static Trainer menu;static Exception failure;
 static object Get(string n){return typeof(Trainer).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);}
 static object Call(string n,params object[] args){return typeof(Trainer).InvokeMember(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.InvokeMethod,null,menu,args);}
 static void Assert(bool v,string text){if(!v)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static List<Form> Stack(){return (List<Form>)Get("popupStack");}
 static void Key(Form f,Keys key){object[] args={Message.Create(f.Handle,0x100,(IntPtr)(int)key,IntPtr.Zero),key};f.GetType().GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,args);}
 static void Pad(Form f,ushort buttons){((Dictionary<IntPtr,Action<Native.XState,ushort>>)Get("padRoutes"))[f.Handle](new Native.XState(),buttons);}
 static void After(Action action){var timer=new Timer();timer.Interval=180;timer.Tick+=delegate{
  timer.Stop();timer.Dispose();try{action();}catch(Exception ex){failure=ex;foreach(Form f in Stack().ToArray().Reverse())f.Close();menu.Close();}
 };timer.Start();}
 static Form Popup(string title){
  Assert(Stack().Count>0,title+" tracked");var f=Stack().Last();
  Assert(f.Visible&&f.Enabled&&!f.IsDisposed,title+" remains visible and interactive");
  Assert(f.Owner!=null&&!IsWindowEnabled(f.Owner.Handle)&&f.Owner.Visible,title+" disables only its visible owner");
  Assert(!menu.TopMost&&!f.TopMost,title+" has no competing topmost band");
  Assert(Native.GetForegroundWindow()==f.Handle,title+" owns foreground focus");
  Assert(Math.Abs((f.Left+f.Width/2)-(f.Owner.Left+f.Owner.Width/2))<35&&Math.Abs((f.Top+f.Height/2)-(f.Owner.Top+f.Owner.Height/2))<35,title+" centred on owner");
  return f;
 }
 static void Recovered(string title){Assert(menu.Visible&&menu.Enabled&&IsWindowEnabled(menu.Handle),title+" main remains visible/enabled");Assert(Stack().Count==0,title+" modal stack empty");Assert(((Dictionary<IntPtr,Action<Native.XState,ushort>>)Get("padRoutes")).Count==0,title+" input routes cleaned");}
 static void TestAmount(){
  for(int i=0;i<12;i++){
   int iteration=i;After(delegate{var f=Popup("Amount cycle "+iteration);int before=Stack().Count;
    Call("OpenGoldPad");Assert(Stack().Count==before,"repeat open cannot create nested amount");
    if(iteration==0){var numeric=f.Controls.OfType<NumericUpDown>().Single();numeric.Value=25000;Pad(f,0x4000);}
    else if(iteration%3==0)Key(f,Keys.Enter);
    else if(iteration%3==1)Key(menu,Keys.Enter); // input incorrectly addressed to disabled owner
    else Key(menu,Keys.Escape);
   });Call("OpenGoldPad");if(failure!=null)throw failure;Recovered("Amount cycle "+iteration);
  }
  var q=(List<Queued>)Get("queue");Assert(q.Count==1&&q[0].value==25000,"exact 25000 remains queued once");
  After(delegate{var f=Popup("Amount B cancel");Call("ControllerBack",f.Handle);});Call("OpenGoldPad");Recovered("Amount B cancel");
 }
 static void TestQueue(){
  After(delegate{var f=Popup("Queue review");Pad(f,0x1000);});Call("OpenQueueManager");Recovered("Queue A edit");
  After(delegate{var f=Popup("Queue nested confirmation");
   After(delegate{var child=Popup("Nested confirmation");Assert(child.Owner==f,"confirmation owned by queue, not main");Call("ControllerBack",child.Handle);});
   f.Controls.OfType<Button>().Single(b=>b.Text=="CLEAR").PerformClick();
   Assert(f.Enabled&&f.Visible&&Stack().Count==1,"queue recovers after nested B decline");
   Key(f,Keys.Enter);
  });Call("OpenQueueManager");Recovered("Nested confirmation");
  Assert(((List<Queued>)Get("queue")).Count==1,"decline keeps queued action");
 }
 static void TestResearch(){
  After(delegate{var f=Popup("Research library");
   After(delegate{var child=Popup("Research invalid item warning");Assert(child.Owner==f,"research warning owner correct");Key(child,Keys.Enter);});
   Pad(f,0x1000);Assert(f.Visible&&f.Enabled,"research enabled after warning");Key(f,Keys.Escape);
  });Call("OpenResearchLibrary");Recovered("Research warning");
 }
 static void TestMessages(){
  After(delegate{var f=Popup("Help");Key(menu,Keys.Enter);});Call("ShowHelp");Recovered("Help Enter");
  After(delegate{var f=Popup("Error");Call("ControllerBack",f.Handle);});Call("Error","Regression test only; no script dispatched.");Recovered("Error B");
  After(delegate{var f=Popup("Co-op protection warning");Key(f,Keys.Escape);});Call("RunSelected");Recovered("Co-op warning Escape");
  After(delegate{var f=Popup("Confirmation A");Pad(f,0x1000);});
  Assert((DialogResult)Call("Notice","No game action.","Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes,"A confirmation returns Yes");Recovered("Confirmation A");
  After(delegate{var f=Popup("Prepare cancelled");Key(f,Keys.Escape);});Call("Prepare");Recovered("Prepare cancellation");
  After(delegate{var f=Popup("Window close button");f.Close();});Call("ShowHelp");Recovered("Popup title-bar close");
 }
 static void TestNative(CommonDialog dialog,string name){using(dialog){
  After(delegate{After(delegate{
   IntPtr fg=Native.GetForegroundWindow();Assert(fg!=menu.Handle,"native "+name+" foreground child");Assert(!IsWindowEnabled(menu.Handle)&&menu.Visible,"native "+name+" modal owner visible");
   Native.Rect r;Assert(Native.GetWindowRect(fg,out r),"native "+name+" bounds");
   Native.Rect parent;Native.GetWindowRect(menu.Handle,out parent);
   Assert(Math.Abs((r.left+r.right)/2-(parent.left+parent.right)/2)<40&&Math.Abs((r.top+r.bottom)/2-(parent.top+parent.bottom)/2)<40,"native "+name+" centred in same DPI coordinate space");
   Call("ControllerBack",fg);
  });});Assert((DialogResult)Call("ShowCommon",dialog)==DialogResult.Cancel,"native "+name+" B cancels");if(failure!=null)throw failure;Recovered("Native "+name);
 }}
 static void TestCloseAll(){
  After(delegate{var parent=Popup("Close-all parent");
   After(delegate{Popup("Close-all nested");Call("HideMenu");});
   Call("Notice","Cancel this nested dialog and its parent.","Nested close",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
  });Call("OpenQueueManager");
  Application.DoEvents();Assert(!menu.Visible&&menu.Enabled&&Stack().Count==0,"hold-close unwinds nested modal sessions before hiding");
  Call("ShowMenu");Recovered("Reopen without Fable");
 }
 static void TestNativeConfirm(CommonDialog dialog,string name){using(dialog){
  After(delegate{After(delegate{Call("NativeDialogButton",Native.GetForegroundWindow(),true);});});
  Assert((DialogResult)Call("ShowCommon",dialog)==DialogResult.OK,"native "+name+" A confirms");Recovered("Native "+name+" confirm");
 }}
 static void TestNativeHide(){using(var dialog=new OpenFileDialog()){
  After(delegate{After(delegate{Call("HideMenu");});});
  Assert((DialogResult)Call("ShowCommon",dialog)==DialogResult.Cancel,"native hold-close cancels picker");
  Assert(!menu.Visible&&IsWindowEnabled(menu.Handle),"native hold-close hides after owner is unblocked");
  Call("ShowMenu");Recovered("Native picker reopen without game");
 }}
 [STAThread]static int Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  if(System.Diagnostics.Process.GetProcessesByName("Fable3").Length!=0){Console.Error.WriteLine("Close Fable III before this UI-only regression.");return 2;}
  try{using(menu=new Trainer()){
   ((Timer)Get("controllerTimer")).Stop();((NotifyIcon)Get("tray")).Visible=false;
   menu.Shown+=delegate{menu.BeginInvoke(new Action(delegate{try{
    Assert(menu.Visible&&menu.Enabled,"real visible main window before dialog tests");
    TestAmount();TestQueue();TestResearch();TestMessages();
    TestNative(new OpenFileDialog(),"open");TestNative(new SaveFileDialog(),"save");TestNative(new FolderBrowserDialog(),"folder");
    TestNativeConfirm(new OpenFileDialog{FileName=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"catalog.json")},"open");
    TestNativeConfirm(new SaveFileDialog{FileName=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tests","dialog-test-no-write.json")},"save");
    TestNativeConfirm(new FolderBrowserDialog{SelectedPath=AppDomain.CurrentDomain.BaseDirectory},"folder");
    TestNativeHide();TestCloseAll();Assert(!(bool)Get("pulsePending")&&!(bool)Get("pulseBusy"),"no game script dispatched");
   }catch(Exception ex){failure=ex;}finally{menu.Close();}}));};
   Application.Run(menu);
  }if(failure!=null)throw failure;Console.WriteLine("PASS "+checks+" visible-owner modal regression assertions; Fable III not running.");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
