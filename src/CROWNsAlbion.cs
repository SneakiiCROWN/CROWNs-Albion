// CROWN's ALBION v1.6.1 — native WinForms overlay / Fable III script controller.
// The tool never injects into processes or modifies Fable3.exe, xlive.dll or DLC.
// Target: .NET Framework 4.x / Windows 10-11, stock csc.exe (C# 5 compatible).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly:System.Reflection.AssemblyVersion("1.6.1.0")]
namespace CROWNsAlbion {
 public class Parameter { public string id {get;set;} public string label {get;set;} public string type {get;set;} public double min {get;set;} public double max {get;set;} public double step {get;set;} public double value {get;set;} public List<Choice> options {get;set;} }
 public class Choice { public double value {get;set;} public string label {get;set;} public override string ToString(){return label+" ("+value+")";} }
 public class ResearchRecord { public string kind {get;set;} public string category {get;set;} public string name {get;set;} public string raw {get;set;} public string preview {get;set;} public string source {get;set;} public string validation {get;set;} public string path {get;set;} public string offset {get;set;} public override string ToString(){return name;} }
 public class GameCommand { public string id {get;set;} public string title {get;set;} public string group {get;set;} public string summary {get;set;} public string template {get;set;} public string risk {get;set;} public string kind {get;set;} public string state {get;set;} public bool repeatable {get;set;} public List<Parameter> @params {get;set;} public override string ToString(){ return title; } }
 public class Queued { public GameCommand cmd; public double? value; public Queued(GameCommand c,double? v){cmd=c;value=v;} }
 public class Prefs { public List<string> Favourites {get;set;} public bool AllowRisk {get;set;} public bool Controller {get;set;} public bool CoopSafe {get;set;} public string GamePath {get;set;} public string LastCategory {get;set;} public Prefs(){Favourites=new List<string>();GamePath="";LastCategory="All";CoopSafe=true;Controller=true;} }
 public static class Native {
  public const int WM_HOTKEY=0x0312;
  [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr hWnd,int id,uint mods,uint virtualKey);
  [DllImport("user32.dll", SetLastError=true)] public static extern bool UnregisterHotKey(IntPtr hWnd,int id);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd,out uint processId);
  [DllImport("xinput1_4.dll", EntryPoint="XInputGetState")] public static extern uint XInputGetState(uint idx,out XState state);
  [StructLayout(LayoutKind.Sequential)] public struct Rect {public int left,top,right,bottom;}
  [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo {public int size;public Rect monitor,work;public uint flags;}
  [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
  [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr hwnd,int id);
  [DllImport("user32.dll")] public static extern IntPtr GetFocus();
  [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetClassName(IntPtr hwnd,StringBuilder text,int size);
  [StructLayout(LayoutKind.Sequential)] public struct XState {public uint packet; public XPad pad;}
  [StructLayout(LayoutKind.Sequential)] public struct XPad {public ushort buttons;public byte lt;public byte rt;public short lx;public short ly;public short rx;public short ry;}
 }
 // One input history for every window. No hooks, XInputEnable, or game-side input patches.
 public class PadInput {
  public ushort Pressed; public bool Toggle,Back,Close;
  ushort last; long context=long.MinValue; bool armed=false,chordLatch=false;
  double repeatAt,bSince=-1,ySince=-1,startSince=-1; bool bDone=false,yDone=false,startDone=false;
  public void Reset(){context=long.MinValue;armed=false;last=0;chordLatch=false;bSince=ySince=startSince=-1;bDone=yDone=startDone=false;}
  bool Hold(bool down,double now,ref double since,ref bool done){
   if(!down){since=-1;done=false;return false;}
   if(since<0)since=now;
   if(!done&&now-since>=650){done=true;return true;}return false;
  }
  public void Sample(ushort b,byte lt,byte rt,long window,double now){
   Pressed=0;Toggle=Back=Close=false;
   const ushort chord=0x00C0; // Both stick clicks; single clicks have no actions.
   if((b&chord)==chord&&!chordLatch){Toggle=true;chordLatch=true;armed=false;}
   if((b&chord)==0)chordLatch=false;
   if(window!=context){context=window;armed=false;bSince=ySince=startSince=-1;repeatAt=0;bDone=yDone=startDone=false;}
   if(!armed){last=b;if(b==0&&lt<180&&rt<180)armed=true;return;}
   Pressed=(ushort)(b&~last);
   ushort direction=(ushort)(b&0x030F);
   if((Pressed&0x030F)!=0)repeatAt=now+350;
   else if(direction!=0&&now>=repeatAt){Pressed|=direction;repeatAt=now+110;}
   bool bDown=(b&0x2000)!=0;
   bool wasBDone=bDone;
   if(Hold(bDown,now,ref bSince,ref bDone))Close=true;
   if(!bDown&&(last&0x2000)!=0&&!wasBDone)Back=true;
   Pressed&=0x7FEF; // Y / Start are hold-only.
   if(Hold((b&0x8000)!=0,now,ref ySince,ref yDone))Pressed|=0x8000;
   if(Hold((b&0x0010)!=0,now,ref startSince,ref startDone))Pressed|=0x0010;
   if(bDown||Back||Close)Pressed=0;
   last=b;
  }
 }
 public class PopupForm : Form {
  protected override bool ProcessCmdKey(ref Message msg,Keys key){
   if(key==Keys.Escape){if(CancelButton!=null)CancelButton.PerformClick();else {DialogResult=DialogResult.Cancel;Close();}return true;}
   if(key==Keys.Enter&&AcceptButton!=null){AcceptButton.PerformClick();return true;}
   return base.ProcessCmdKey(ref msg,key);
  }
 }
 public class Trainer : Form {
  readonly string root=AppDomain.CurrentDomain.BaseDirectory;
  List<ResearchRecord> researchEntries=null;
  const string EXPECTED_HASH="AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42";
  const string MARK="-- CROWN_ALBION_MANAGED";
  List<GameCommand> catalogue=new List<GameCommand>();
  Prefs prefs=new Prefs();
  string game="", script="",manifest="";
  Label status,heroInfo,nameInfo,detail,hotkeyInfo,selectionTitle;
  ListBox categories, features, queueBox;
  TextBox search, numericText;
  ComboBox selectChoices; TrackBar valueSlider; bool valueChanging=false;
  CheckBox riskToggle, controllerToggle, coopSafeToggle;
  Button execute, saveFav, amountButton;
  System.Windows.Forms.Timer clearTimer=new System.Windows.Forms.Timer();
  System.Windows.Forms.Timer controllerTimer=new System.Windows.Forms.Timer();
  string pulseText=""; bool pulsePending=false; bool pulseBusy=false;
  readonly PadInput padInput=new PadInput();
  readonly List<Form> popupStack=new List<Form>();
  int commonDialogDepth=0;
  IntPtr commonWindow=IntPtr.Zero;
  bool hideRequested=false;
  readonly Stopwatch padClock=Stopwatch.StartNew();
  readonly Dictionary<IntPtr,Action<Native.XState,ushort>> padRoutes=new Dictionary<IntPtr,Action<Native.XState,ushort>>();
  List<Queued> queue=new List<Queued>();
  ComboBox presets;
  string[] groupNames={"All","Favourites","Hero","Combat","Kingdom","Inventory","World","Spawning","Chaos","Utilities"};
  Color bg=Color.FromArgb(17,21,30), panel=Color.FromArgb(26,32,44), panel2=Color.FromArgb(35,42,57), accent=Color.FromArgb(195,151,73), txt=Color.FromArgb(234,229,217), muted=Color.FromArgb(159,171,188);
  readonly string logPath;
  NotifyIcon tray=new NotifyIcon();
  public Trainer(){
   logPath=Path.Combine(root,"CROWN_Log.txt");
   Text="CROWN's ALBION v1.6.1 — Fable III"; Width=1140; Height=830; MinimumSize=new Size(1140,800); StartPosition=FormStartPosition.CenterScreen;
   BackColor=bg;ForeColor=txt; Font=new Font("Segoe UI",9.5f); TopMost=false;Opacity=1;
   Icon=SystemIcons.Application;KeyPreview=true;
   try{var json=File.ReadAllText(Path.Combine(root,"catalog.json"),Encoding.UTF8); catalogue=new JavaScriptSerializer().Deserialize<List<GameCommand>>(json);}catch(Exception ex){Notice("Catalogue missing: "+ex.Message,"CROWN",MessageBoxButtons.OK,MessageBoxIcon.Error);}
   try{var p=Path.Combine(root,"settings.json");if(File.Exists(p))prefs=new JavaScriptSerializer().Deserialize<Prefs>(File.ReadAllText(p));}catch{prefs=new Prefs();}
   if(prefs.Favourites==null)prefs.Favourites=new List<string>();
   // Upgrade existing settings conservatively. Old settings did not contain CoopSafe.
   try {var prefsPath=Path.Combine(root,"settings.json");if(!File.Exists(prefsPath)||File.ReadAllText(prefsPath).IndexOf("CoopSafe",StringComparison.Ordinal)<0)prefs.CoopSafe=true;}catch{prefs.CoopSafe=true;}
   game=GuessGamePath();
   BuildUI(); UpdatePaths(); PopulateCategories(); RefreshStatus();
   try{if(ReadyScript()){string active=File.ReadAllText(script);if(active.StartsWith(MARK)&&active.IndexOf("-- GENERATED",StringComparison.Ordinal)>=0)SetStatus("WARNING: Previous CROWN command remains on disk. Use STOP / CLEAR LUA before launching Fable.");}}catch{}
   KeyDown+=OnGlobalKey;
   // The community injector may poll once per ~60 rendered frames. This is still NOT exactly-once delivery.
   clearTimer.Interval=1900;clearTimer.Tick+=delegate {ClearPulse();};
   controllerTimer.Interval=40;controllerTimer.Tick+=OnControllerTick;controllerTimer.Start();
   FormClosing+=delegate { if(pulsePending)ClearPulse();SavePrefs();tray.Visible=false; };
   tray.Icon=SystemIcons.Application;tray.Text="CROWN's Albion (F8 to show/hide)"; tray.Visible=true;
   ContextMenu menu=new ContextMenu();menu.MenuItems.Add("Show / hide (F8)",delegate{Toggle();}); menu.MenuItems.Add("Clear running script",delegate{ClearPulse(true);});menu.MenuItems.Add("Quit",delegate{Close();});tray.ContextMenu=menu;tray.DoubleClick+=delegate{ShowMenu();};
  }
  string GuessGamePath(){
   if(!String.IsNullOrEmpty(prefs.GamePath)&&File.Exists(Path.Combine(prefs.GamePath,"Fable3.exe")))return prefs.GamePath;
   string parent=Path.GetFullPath(Path.Combine(root,".."));if(File.Exists(Path.Combine(parent,"Fable3.exe")))return parent;
   string direct=Path.Combine(root,"Fable3.exe");if(File.Exists(direct))return root;
   if(File.Exists(@"B:\Games\Fable 3\Fable3.exe"))return @"B:\Games\Fable 3";
   return parent;
  }
  void SavePrefs(){ try{prefs.GamePath=game;prefs.AllowRisk=riskToggle.Checked;prefs.Controller=controllerToggle.Checked;prefs.CoopSafe=coopSafeToggle!=null?coopSafeToggle.Checked:true;File.WriteAllText(Path.Combine(root,"settings.json"),new JavaScriptSerializer().Serialize(prefs));}catch(Exception ex){Log("Settings save: "+ex.Message);} }
  void Log(string s){try{File.AppendAllText(logPath,DateTime.Now.ToString("u")+" "+s+Environment.NewLine);}catch{}}
  Label L(string s,int x,int y,int w,int h,Color c,Font f=null){Label b=new Label();b.Text=s;b.Left=x;b.Top=y;b.Width=w;b.Height=h;b.ForeColor=c;b.Font=f??Font;b.BackColor=Color.Transparent;b.AutoEllipsis=true;return b;}
  Button B(string s,int x,int y,int w,int h,EventHandler click,bool gold=false){Button b=new Button();b.Text=s;b.Left=x;b.Top=y;b.Width=w;b.Height=h;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=gold?accent:Color.FromArgb(63,72,88);b.BackColor=gold?accent:panel2;b.ForeColor=gold?Color.FromArgb(15,18,23):txt;b.Font=new Font("Segoe UI",9.3f,FontStyle.Bold);b.Cursor=Cursors.Hand;b.Click+=click;return b;}
  Panel P(int x,int y,int w,int h){Panel p=new Panel();p.Left=x;p.Top=y;p.Width=w;p.Height=h;p.BackColor=panel;return p;}
  void BuildUI(){
   Panel titlebar=P(0,0,1124,68);titlebar.Dock=DockStyle.Top;titlebar.Dock=DockStyle.Top;titlebar.BackColor=Color.FromArgb(31,34,43);Controls.Add(titlebar);
   Label brand=L("♛  CROWN'S ALBION",20,11,390,30,accent,new Font("Segoe UI",18,FontStyle.Bold));titlebar.Controls.Add(brand);
   hotkeyInfo=L("L3+R3 / F8 menu  •  A select  •  B back  •  Hold B close  •  F1 help",22,42,850,20,muted,new Font("Segoe UI",9));titlebar.Controls.Add(hotkeyInfo);
   Button goldButton=B("QUICK GOLD",420,15,98,36,delegate{OpenGoldPad();},true);goldButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;titlebar.Controls.Add(goldButton);
   Button libButton=B("LIBRARY",523,15,87,36,delegate{OpenResearchLibrary();});libButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;titlebar.Controls.Add(libButton);
   presets=new ComboBox();presets.Left=617;presets.Top=20;presets.Width=205;presets.DropDownStyle=ComboBoxStyle.DropDownList;presets.BackColor=panel2;presets.ForeColor=txt;presets.Font=new Font("Segoe UI",9);
   presets.Items.AddRange(new object[]{"PRESET: God mode ON","PRESET: God mode OFF","PRESET: Photo mode","PRESET: Normal view","PRESET: Midnight","PRESET: Dawn","PRESET: Combat chaos","PRESET: Restore combat"});presets.SelectedIndex=0;presets.Anchor=AnchorStyles.Top|AnchorStyles.Right;titlebar.Controls.Add(presets);
   Button presetLoad=B("LOAD",830,15,90,36,delegate{LoadPreset();});presetLoad.Anchor=AnchorStyles.Top|AnchorStyles.Right;titlebar.Controls.Add(presetLoad);
   Button hide=B("HIDE  F8",980,15,120,36,delegate{HideMenu();});hide.Anchor=AnchorStyles.Right|AnchorStyles.Top;titlebar.Controls.Add(hide);
   // Layout uses splitter-based docking below the header to resize cleanly.
   Panel shell=new Panel();shell.Dock=DockStyle.Fill;shell.Padding=new Padding(11);shell.BackColor=bg;Controls.Add(shell);shell.BringToFront();titlebar.SendToBack();
   SplitContainer main=new SplitContainer();main.Dock=DockStyle.Fill;main.Orientation=Orientation.Vertical;main.SplitterWidth=6;main.Size=new Size(1090,620);main.SplitterDistance=230;main.Panel1.BackColor=panel;main.Panel2.BackColor=bg;shell.Controls.Add(main);
   Label catsLabel=L("CATEGORIES",14,12,180,24,accent,new Font("Segoe UI",11,FontStyle.Bold));main.Panel1.Controls.Add(catsLabel);
   categories=new ListBox();categories.Location=new Point(10,43);categories.Width=209;categories.Height=320;categories.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;categories.IntegralHeight=false;categories.DrawMode=DrawMode.OwnerDrawFixed;categories.ItemHeight=33;categories.BorderStyle=BorderStyle.None;categories.BackColor=panel;categories.ForeColor=txt;categories.DrawItem+=DrawList;categories.SelectedIndexChanged+=delegate{RefreshFeatures();};main.Panel1.Controls.Add(categories);
   // Fixed bottom action strip never overlaps the category list at normal DPI or on resize.
   Panel actionStrip=new Panel();actionStrip.Dock=DockStyle.Bottom;actionStrip.Height=187;actionStrip.BackColor=panel;main.Panel1.Controls.Add(actionStrip);actionStrip.BringToFront();
   Label quick=L("QUICK ACTIONS",12,2,197,22,accent,new Font("Segoe UI",10,FontStyle.Bold));actionStrip.Controls.Add(quick);
   Button prep=B("PREPARE SCRIPT PATH",10,29,208,35,delegate{Prepare();});prep.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;actionStrip.Controls.Add(prep);
   Button locate=B("CHOOSE GAME FOLDER",10,70,208,35,delegate{Locate();});locate.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;actionStrip.Controls.Add(locate);
   Button clear=B("STOP / CLEAR LUA",10,111,208,35,delegate{ClearPulse(true);},true);clear.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;actionStrip.Controls.Add(clear);
   Label hint=L("Esc returns to game  •  F1 help",11,151,208,25,muted,new Font("Segoe UI",8));actionStrip.Controls.Add(hint);
   // Horizontal detail split.
   SplitContainer work=new SplitContainer();work.Dock=DockStyle.Fill;work.Orientation=Orientation.Vertical;work.SplitterWidth=6;work.Size=new Size(850,620);work.SplitterDistance=410;work.Panel1.BackColor=panel;work.Panel2.BackColor=panel;main.Panel2.Controls.Add(work);
   Label all=L("FEATURES",12,10,150,24,accent,new Font("Segoe UI",11,FontStyle.Bold));work.Panel1.Controls.Add(all);
   search=new TextBox();search.Location=new Point(11,40);search.Width=385;search.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right;search.Font=new Font("Segoe UI",11);search.BackColor=panel2;search.ForeColor=txt;search.BorderStyle=BorderStyle.FixedSingle;search.TextChanged+=delegate{RefreshFeatures();};work.Panel1.Controls.Add(search);
   features=new ListBox();features.Location=new Point(10,78);features.Width=387;features.Height=430;features.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom;features.DrawMode=DrawMode.OwnerDrawFixed;features.ItemHeight=31;features.DrawItem+=DrawList;features.IntegralHeight=false;features.BorderStyle=BorderStyle.None;features.BackColor=panel;features.ForeColor=txt;features.SelectedIndexChanged+=delegate{SelectFeature();};features.DoubleClick+=delegate{QueueSelected();};work.Panel1.Controls.Add(features);
   selectionTitle=L("SELECT A FEATURE",16,12,390,47,accent,new Font("Segoe UI",14,FontStyle.Bold));selectionTitle.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;work.Panel2.Controls.Add(selectionTitle);
   detail=L("Choose a feature. Source-backed templates, NOT yet game-verified.",16,66,370,88,muted,new Font("Segoe UI",10));detail.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;work.Panel2.Controls.Add(detail);
   nameInfo=L("",16,152,380,26,Color.Orange);work.Panel2.Controls.Add(nameInfo);
   heroInfo=L("VALUE",16,187,380,21,accent,new Font("Segoe UI",10,FontStyle.Bold));work.Panel2.Controls.Add(heroInfo);
   numericText=new TextBox();numericText.Location=new Point(16,214);numericText.Width=260;numericText.Font=new Font("Segoe UI",12);numericText.BackColor=panel2;numericText.ForeColor=txt;work.Panel2.Controls.Add(numericText);
   selectChoices=new ComboBox();selectChoices.Location=new Point(16,214);selectChoices.Width=290;selectChoices.DropDownStyle=ComboBoxStyle.DropDownList;selectChoices.BackColor=panel2;selectChoices.ForeColor=txt;selectChoices.Font=new Font("Segoe UI",11);work.Panel2.Controls.Add(selectChoices);
   Button amountPad=B("EXACT AMOUNT",278,213,113,31,delegate{OpenAmountPad();});amountPad.Anchor=AnchorStyles.Top|AnchorStyles.Right;work.Panel2.Controls.Add(amountPad);
   amountButton=amountPad;amountPad.Font=new Font("Segoe UI",8,FontStyle.Bold);
   valueSlider=new TrackBar();valueSlider.Left=16;valueSlider.Top=242;valueSlider.Width=368;valueSlider.Minimum=0;valueSlider.Maximum=1000;valueSlider.TickStyle=TickStyle.None;valueSlider.BackColor=panel;work.Panel2.Controls.Add(valueSlider);
   valueSlider.Scroll+=delegate{if(valueChanging)return;GameCommand c=Selected();if(c==null||c.@params==null||c.@params.Count==0)return;Parameter param=c.@params[0];if(param.type=="select")return;double val=param.min+(param.max-param.min)*valueSlider.Value/1000.0;double step=param.step>0?param.step:1;val=Math.Round(val/step)*step;val=Math.Max(param.min,Math.Min(param.max,val));valueChanging=true;numericText.Text=val.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture);valueChanging=false;};
   numericText.TextChanged+=delegate{if(valueChanging)return;GameCommand c=Selected();if(c==null||c.@params==null||c.@params.Count==0)return;Parameter param=c.@params[0];double val;if(Double.TryParse(numericText.Text,System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out val)&&!Double.IsNaN(val)&&val>=param.min&&val<=param.max&&param.max>param.min){valueChanging=true;valueSlider.Value=Math.Max(0,Math.Min(1000,(int)Math.Round(1000.0*(val-param.min)/(param.max-param.min))));valueChanging=false;}};
   saveFav=B("☆ FAVOURITE",16,279,145,36,delegate{Favourite();});work.Panel2.Controls.Add(saveFav);
   execute=B("EXECUTE COMMAND",168,279,222,36,delegate{RunSelected();},true);execute.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;work.Panel2.Controls.Add(execute);
   riskToggle=new CheckBox();riskToggle.Left=16;riskToggle.Top=318;riskToggle.Width=376;riskToggle.Height=32;riskToggle.ForeColor=txt;riskToggle.Text="Allow high-risk / save-changing commands";riskToggle.Checked=prefs.AllowRisk;riskToggle.CheckedChanged+=delegate{SavePrefs();};work.Panel2.Controls.Add(riskToggle);
   controllerToggle=new CheckBox();controllerToggle.Left=16;controllerToggle.Top=360;controllerToggle.Width=390;controllerToggle.Height=30;controllerToggle.Text="Xbox pad navigation (L3 + R3 toggles)";controllerToggle.ForeColor=txt;controllerToggle.Checked=prefs.Controller;controllerToggle.CheckedChanged+=delegate{padInput.Reset();SavePrefs();};work.Panel2.Controls.Add(controllerToggle);
   coopSafeToggle=new CheckBox();coopSafeToggle.Left=16;coopSafeToggle.Top=391;coopSafeToggle.Width=392;coopSafeToggle.Height=28;coopSafeToggle.Text="CO-OP PROTECTION — limit gameplay changes";coopSafeToggle.ForeColor=Color.FromArgb(235,192,123);coopSafeToggle.Checked=prefs.CoopSafe;coopSafeToggle.CheckedChanged+=delegate{SavePrefs();SetStatus(coopSafeToggle.Checked?"Co-op protection ON. Only the harmless script test can dispatch.":"Co-op protection OFF. Use a spare SINGLE-PLAYER save; XLLN sessions unverified.");};work.Panel2.Controls.Add(coopSafeToggle);
   Label qo=L("COMMAND QUEUE  (review before sending)",16,423,360,24,accent,new Font("Segoe UI",10,FontStyle.Bold));work.Panel2.Controls.Add(qo);
   queueBox=new ListBox();queueBox.Left=16;queueBox.Top=449;queueBox.Width=374;queueBox.Height=35;queueBox.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom|AnchorStyles.Top;queueBox.BackColor=panel2;queueBox.ForeColor=txt;queueBox.DoubleClick+=delegate{EditQueued();};work.Panel2.Controls.Add(queueBox);
   ContextMenu qmenu=new ContextMenu();qmenu.MenuItems.Add("Edit selected value",delegate{EditQueued();});qmenu.MenuItems.Add("Update selected from value field",delegate{UpdateQueued();});qmenu.MenuItems.Add("Remove selected",delegate{RemoveQueued();});qmenu.MenuItems.Add("Move up",delegate{MoveQueued(-1);});qmenu.MenuItems.Add("Move down",delegate{MoveQueued(1);});queueBox.ContextMenu=qmenu;
   Button add=B("+ QUEUE",16,489,113,30,delegate{QueueSelected();});work.Panel2.Controls.Add(add);
   Button runQueue=B("RUN QUEUE",134,489,126,30,delegate{RunQueue();},true);work.Panel2.Controls.Add(runQueue);
   Button clearQueue=B("CLEAR QUEUE",266,489,125,30,delegate{queue.Clear();RenderQueue();});work.Panel2.Controls.Add(clearQueue);
   ToolTip tips=new ToolTip();tips.SetToolTip(queueBox,"Double-click a queued command to edit. Right-click to update, reorder or remove it.");tips.SetToolTip(execute,"Deliberate action; game execution requires a compatible script injector.");tips.SetToolTip(controllerToggle,"L3+R3 toggle; D-pad navigate; A select / exact amount; Back review queue; tap B back; hold B close.");
   Button reviewQueue=B("REVIEW QUEUE",16,527,141,31,delegate{OpenQueueManager();},true);work.Panel2.Controls.Add(reviewQueue);
   Button saveLoadout=B("SAVE",164,527,96,31,delegate{SaveLoadout();});work.Panel2.Controls.Add(saveLoadout);
   Button openLoadout=B("LOAD",267,527,124,31,delegate{LoadLoadout();});work.Panel2.Controls.Add(openLoadout);
   Panel footer=P(0,0,100,46);footer.Dock=DockStyle.Bottom;footer.BackColor=Color.FromArgb(27,35,42);Controls.Add(footer);footer.SendToBack();
   status=L("Checking installation...",14,5,930,36,muted,new Font("Segoe UI",9));status.Dock=DockStyle.Fill;footer.Controls.Add(status);
   Resize+=delegate{LayoutAdjust(main,work);};LayoutAdjust(main,work);
  }
  // Research library is read-only. Source lists are NOT automatically executable cheats.
  // Keep thousands of unverified commands out of the 95-item dispatch catalogue.
  void OpenResearchLibrary(){
   if(popupStack.Count>0||commonDialogDepth>0){ShowMenu();return;}
   try {
    if(researchEntries==null){
     string rpath=Path.Combine(root,"references.json");
     if(!File.Exists(rpath)){Error("references.json missing. Re-extract the complete package.");return;}
     var serializer=new JavaScriptSerializer();serializer.MaxJsonLength=15*1024*1024;
     researchEntries=serializer.Deserialize<List<ResearchRecord>>(File.ReadAllText(rpath,Encoding.UTF8));
     if(researchEntries==null)throw new Exception("Reference database is empty.");
    }
    Form dialog=new PopupForm();dialog.Text="CROWN's ALBION — Research Library (read-only)";
    dialog.Size=new Size(1020,690);dialog.MinimumSize=new Size(1000,690);dialog.StartPosition=FormStartPosition.CenterParent;dialog.KeyPreview=true;
    dialog.BackColor=bg;dialog.ForeColor=txt;dialog.Font=Font;dialog.ShowInTaskbar=false;
    Label intro=L("RESEARCH LIBRARY — UNVERIFIED REFERENCES",16,10,955,28,accent,new Font("Segoe UI",13,FontStyle.Bold));
    intro.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;dialog.Controls.Add(intro);
    Label warning=L("Nothing on this page executes in Fable III. Copy commands only after checking syntax and protecting your saves.",16,40,970,33,muted);
    warning.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;dialog.Controls.Add(warning);
    ComboBox scope=new ComboBox();scope.Left=16;scope.Top=79;scope.Width=175;scope.DropDownStyle=ComboBoxStyle.DropDownList;
    scope.Items.AddRange(new object[]{"All references","Item identifiers","Debug expressions","Weapons","Appearance","Inventory","Trade items","Game records","Collection functions","Extracted Unlocker Lua"});scope.SelectedIndex=0;
    scope.BackColor=panel2;scope.ForeColor=txt;dialog.Controls.Add(scope);
    TextBox q=new TextBox();q.Left=202;q.Top=78;q.Width=776;q.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;q.BackColor=panel2;q.ForeColor=txt;
    q.Font=new Font("Segoe UI",11);dialog.Controls.Add(q);
    ListBox names=new ListBox();names.Left=16;names.Top=119;names.Width=445;names.Height=485;
    names.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Bottom;names.BackColor=panel;names.ForeColor=txt;names.IntegralHeight=false;
    dialog.Controls.Add(names);
    TextBox code=new TextBox();code.Left=472;code.Top=119;code.Width=506;code.Height=290;
    code.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;code.BackColor=panel2;code.ForeColor=txt;code.ReadOnly=true;
    code.Multiline=true;code.ScrollBars=ScrollBars.Both;code.Font=new Font("Consolas",10);dialog.Controls.Add(code);
    Label meta=L("Pick an item or command. Reference material, not runtime-tested.",472,420,500,104,muted);
    meta.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;dialog.Controls.Add(meta);
    Label count=L("",16,611,350,24,muted);count.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;dialog.Controls.Add(count);
    Button copy=B("COPY LUA CANDIDATE",472,541,232,42,delegate {
      ResearchRecord r=names.SelectedItem as ResearchRecord;
      if(r!=null&&!String.IsNullOrEmpty(r.preview)){Clipboard.SetText(r.preview);Notice("Candidate Lua copied. It was NOT executed or verified in Fable III.","Research Library");}
    },true);copy.Anchor=AnchorStyles.Left|AnchorStyles.Top;dialog.Controls.Add(copy);
    Button queueUnlock=B("QUEUE ITEM (EXPERIMENTAL)",718,589,260,38,delegate{
      ResearchRecord record=names.SelectedItem as ResearchRecord;
      if(record==null||record.kind!="unlocker"){Notice("Select an extracted All Items Unlocker Lua entry first.","CROWN");return;}
      string candidate=(record.preview??"").Trim();
      bool allowed=Regex.IsMatch(candidate,@"^Inventory\.AddItemOfType\(GetLocalHero\(\), 'Object[A-Za-z0-9_]+'\)$")||
         Regex.IsMatch(candidate,@"^Debug\.SetHeroWearingItemOfType\('Object[A-Za-z0-9_]+', true\)$")||
         Regex.IsMatch(candidate,@"^Debug\.GiveAll(MeleeWeapons|RangedWeapons|SpellGauntlets)\(\)$");
      if(!allowed){Notice("Lua syntax is not in the audited grant/appearance allowlist. Nothing added.","CROWN");return;}
      if(queue.Count>=8){Notice("The queue holds at most eight actions. Review or clear entries first.","CROWN");return;}
      if(queue.Any(v=>v.cmd.template==candidate)){Notice("This candidate is already queued. Repeat grants may duplicate inventory.","CROWN");return;}
      GameCommand cmd=new GameCommand{id="unlocker:"+record.name,title="UNVERIFIED: "+record.name,group="Inventory",summary="Source: "+record.source+". May change save or duplicate items.",template=candidate,risk="high",kind="oneshot",state="unverified",repeatable=true,@params=new List<Parameter>()};
      queue.Add(new Queued(cmd,null));RenderQueue();SetStatus("Unverified unlocker entry queued. Co-op protection will prevent gameplay dispatch.");
    });queueUnlock.Anchor=AnchorStyles.Right|AnchorStyles.Top;dialog.Controls.Add(queueUnlock);
    Button copyName=B("COPY IDENTIFIER",718,541,260,42,delegate{
      ResearchRecord r=names.SelectedItem as ResearchRecord;
      if(r!=null)Clipboard.SetText(r.raw??r.name);
    });copyName.Anchor=AnchorStyles.Right|AnchorStyles.Top;dialog.Controls.Add(copyName);
    Action refresh=delegate {
      string searchValue=(q.Text??"").Trim();string chosen=scope.SelectedItem as string??"All references";
      IEnumerable<ResearchRecord> filtered=researchEntries.Where(r=>
       (chosen=="All references"||
        (chosen=="Item identifiers"&&r.kind=="item")||
        (chosen=="Debug expressions"&&r.kind=="debug")||
        (chosen=="Extracted Unlocker Lua"&&r.kind=="unlocker")||
        r.category==chosen)&&
       (searchValue.Length==0||(r.name+" "+r.raw+" "+r.category+" "+r.path).IndexOf(searchValue,StringComparison.OrdinalIgnoreCase)>=0));
      // bounded rendering for responsiveness; search still scans every record
      List<ResearchRecord> matches=filtered.Take(500).ToList();
      names.BeginUpdate();names.Items.Clear();foreach(var r in matches)names.Items.Add(r);names.EndUpdate();
      count.Text=matches.Count==500?"Showing first 500 results; refine search":"Showing "+matches.Count+" result(s)";
      if(matches.Count>0)names.SelectedIndex=0;else{code.Text="";meta.Text="No matching references.";}
    };
    names.SelectedIndexChanged+=delegate{
      ResearchRecord r=names.SelectedItem as ResearchRecord;if(r==null)return;
      code.Text=String.IsNullOrEmpty(r.preview)?"No known Lua snippet. Identifier only.":r.preview;
      meta.Text="CATEGORY: "+r.category+"\r\nSOURCE: "+r.source+"\r\nSTATUS: "+r.validation+
       (String.IsNullOrEmpty(r.path)?"":"\r\nMENU PATH: "+r.path);
    };
    q.TextChanged+=delegate{refresh();};scope.SelectedIndexChanged+=delegate{refresh();};
    dialog.KeyDown+=delegate(object sender,KeyEventArgs key){
      if(key.KeyCode==Keys.Escape){dialog.Close();key.Handled=true;}
      else if(key.Control&&key.KeyCode==Keys.F){q.Focus();key.Handled=true;}
      else if(key.KeyCode==Keys.Enter&&names.Focused){if(names.SelectedItem is ResearchRecord)copy.PerformClick();key.Handled=true;}
    };
    RegisterPad(dialog,delegate(Native.XState st,ushort press){
       if((press&0x0001)!=0&&names.Items.Count>0)names.SelectedIndex=Math.Max(0,names.SelectedIndex-1);
       if((press&0x0002)!=0&&names.Items.Count>0)names.SelectedIndex=Math.Min(names.Items.Count-1,names.SelectedIndex+1);
       if((press&0x0004)!=0)scope.SelectedIndex=Math.Max(0,scope.SelectedIndex-1);
       if((press&0x0008)!=0)scope.SelectedIndex=Math.Min(scope.Items.Count-1,scope.SelectedIndex+1);
       if((press&0x1000)!=0)queueUnlock.PerformClick();
       if((press&0x4000)!=0)copyName.PerformClick();
       if((press&0x8000)!=0)copy.PerformClick();
    });
    Button done=B("BACK / CLOSE",472,589,232,38,delegate{dialog.Close();});dialog.Controls.Add(done);
    dialog.AcceptButton=queueUnlock;dialog.CancelButton=done;
    refresh();ShowPopup(dialog);
   } catch(Exception e){Error("Research Library: "+e.Message);}
  }
  void LayoutAdjust(SplitContainer main,SplitContainer work){
   try{
    if(main.Width>300&&main.SplitterDistance>main.Width-250)main.SplitterDistance=220;
    if(work.Width>650&&work.SplitterDistance>work.Width-240)work.SplitterDistance=(int)(work.Width*.49);
    if(categories!=null)categories.Height=Math.Max(110,main.Panel1.Height-43-187-9);
    if(features!=null)features.Height=Math.Max(180,work.Panel1.Height-88);
    if(queueBox!=null)queueBox.Height=35;
   }catch{}
  }
  void DrawList(object sender,DrawItemEventArgs e){ListBox lb=sender as ListBox;if(e.Index<0||lb==null)return;
   bool selected=(e.State&DrawItemState.Selected)==DrawItemState.Selected;
   using(Brush br=new SolidBrush(selected?Color.FromArgb(74,60,45):panel))e.Graphics.FillRectangle(br,e.Bounds);
   string s=lb.Items[e.Index].ToString();GameCommand gc=lb.Items[e.Index] as GameCommand;
   if(gc!=null){s=gc.title; if(gc.risk=="high")s+="   !";if(prefs.Favourites.Contains(gc.id))s="★ "+s;}
   using(Brush textBrush=new SolidBrush(selected?Color.FromArgb(255,225,150):txt))e.Graphics.DrawString(s,lb.Font,textBrush,new RectangleF(e.Bounds.X+11,e.Bounds.Y+6,e.Bounds.Width-13,e.Bounds.Height-4));
  }
  void PopulateCategories(){categories.BeginUpdate();categories.Items.Clear();foreach(string s in groupNames)categories.Items.Add(s);categories.EndUpdate();int i=Array.IndexOf(groupNames,prefs.LastCategory);categories.SelectedIndex=i<0?0:i;}
  void RefreshFeatures(){if(features==null)return;string category=categories.SelectedItem as string ??"All",needle=(search.Text??"").Trim();prefs.LastCategory=category;
   GameCommand old=features.SelectedItem as GameCommand;
   var matches=catalogue.Where(c=>(category=="All"||c.group==category||(category=="Favourites"&&prefs.Favourites.Contains(c.id)))&&(needle.Length==0||((c.title+" "+c.summary+" "+c.group).IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0))).OrderBy(c=>c.title).ToList();
   features.BeginUpdate();features.Items.Clear();foreach(var f in matches)features.Items.Add(f);features.EndUpdate();
   if(old!=null&&matches.Any(c=>c.id==old.id))features.SelectedItem=matches.First(c=>c.id==old.id);else if(matches.Count>0)features.SelectedIndex=0;else SelectFeature();
  }
  GameCommand Selected(){return features.SelectedItem as GameCommand;}
  void SelectFeature(){GameCommand c=Selected();if(c==null){selectionTitle.Text="NO FEATURE SELECTED";detail.Text="Search or choose another category.";execute.Enabled=false;return;}execute.Enabled=true;
   selectionTitle.Text=c.title.ToUpperInvariant();detail.Text=c.summary;
   nameInfo.Text="CATEGORY: "+c.group.ToUpperInvariant()+"    •    RISK: "+c.risk.ToUpperInvariant()+(c.repeatable?"    •    MAY REPEAT":"");
   if(c.id=="gold"||c.id=="treasury"||c.id=="seals")detail.Text=c.summary+"\r\nAdds this amount; does NOT set current balance or drop a physical item.";
   Parameter p=c.@params!=null&&c.@params.Count>0?c.@params[0]:null;
   heroInfo.Visible=p!=null;amountButton.Visible=p!=null&&p.type!="select"&&p.step>=1;numericText.Visible=p!=null&&p.type!="select";selectChoices.Visible=p!=null&&p.type=="select";valueSlider.Visible=p!=null&&p.type!="select"&&p.max>p.min;
   if(p!=null){heroInfo.Text=p.label+"  ["+p.min+" – "+p.max+"]";
    if(p.type=="select"){selectChoices.Items.Clear();if(p.options!=null)foreach(var o in p.options)selectChoices.Items.Add(o);if(selectChoices.Items.Count>0){int sel=0;for(int i=0;i<selectChoices.Items.Count;i++)if(((Choice)selectChoices.Items[i]).value==p.value)sel=i;selectChoices.SelectedIndex=sel;}}
    else numericText.Text=p.value.ToString(System.Globalization.CultureInfo.InvariantCulture);
   }
   saveFav.Text=prefs.Favourites.Contains(c.id)?"★ FAVOURITED":"☆ FAVOURITE";
  }

  // Intentional, readable amount chooser. NumericUpDown preserves integer precision up to 100,000,000.
  // Selecting a number NEVER executes a cheat: it only changes the main value or queues the command.
  void OpenGoldPad(){
   if(popupStack.Count>0||commonDialogDepth>0){ShowMenu();return;}
   categories.SelectedItem="Kingdom";
   GameCommand target=catalogue.FirstOrDefault(c=>c.id=="gold");
   if(target==null){Error("Gold command missing from catalogue.");return;}
   if(features.Items.Contains(target))features.SelectedItem=target;
   OpenAmountPad();
  }
  void OpenAmountPad(){
   if(popupStack.Count>0||commonDialogDepth>0){ShowMenu();return;}
   GameCommand c=Selected();
   if(c==null||c.@params==null||c.@params.Count==0||c.@params[0].type=="select"||c.@params[0].step<1){
    Notice("Choose a numeric feature such as Add hero gold, Royal treasury or Guild Seals first.","Exact amount",MessageBoxButtons.OK,MessageBoxIcon.Information);return;
   }
   Parameter param=c.@params[0];
   if(param.max>1000000000||param.min<0||param.min>param.max){Notice("This parameter cannot use the exact amount pad.","CROWN");return;}
   Form f=new PopupForm();f.Text="CROWN — Exact amount: "+c.title;f.Size=new Size(530,472);f.MinimumSize=f.MaximumSize=f.Size;
   f.FormBorderStyle=FormBorderStyle.FixedDialog;f.MaximizeBox=false;f.MinimizeBox=false;f.ShowInTaskbar=false;
   f.BackColor=bg;f.ForeColor=txt;f.Font=new Font("Segoe UI",10);f.KeyPreview=true;f.StartPosition=FormStartPosition.CenterParent;
   Label h=L("EXACT AMOUNT",22,16,458,33,accent,new Font("Segoe UI",17,FontStyle.Bold));f.Controls.Add(h);
   Label explain=L(c.title+" — ADD / APPLY; never SET BALANCE.\nChoosing a number does not execute anything.",22,52,466,48,muted,new Font("Segoe UI",9.5f));f.Controls.Add(explain);
   NumericUpDown amount=new NumericUpDown();amount.Left=22;amount.Top=102;amount.Width=460;amount.Height=34;
   amount.Minimum=(decimal)param.min;amount.Maximum=(decimal)param.max;amount.DecimalPlaces=param.step<1?2:0;
   amount.Increment=(decimal)Math.Max(param.step,0.01);amount.ThousandsSeparator=true;
   double initial=param.value;try{initial=ParameterValue(c);}catch{}
   amount.Value=(decimal)Math.Max(param.min,Math.Min(param.max,initial));amount.Font=new Font("Segoe UI",16,FontStyle.Bold);
   amount.BackColor=panel2;amount.ForeColor=txt;f.Controls.Add(amount);
   Label presetsLabel=L("QUICK AMOUNTS — SELECT, THEN QUEUE",22,145,460,22,accent,new Font("Segoe UI",9,FontStyle.Bold));f.Controls.Add(presetsLabel);
   FlowLayoutPanel amounts=new FlowLayoutPanel();amounts.Left=20;amounts.Top=173;amounts.Width=469;amounts.Height=94;amounts.WrapContents=true;amounts.AutoScroll=false;
   amounts.BackColor=bg;f.Controls.Add(amounts);
   foreach(int quickVal in new int[]{1,100,500,1000,5000,10000,25000,50000,100000,1000000}){
    if(quickVal<param.min||quickVal>param.max)continue;
    int captured=quickVal;Button b=B(quickVal.ToString("N0"),0,0,84,35,delegate{amount.Value=captured;});b.Margin=new Padding(3,3,3,3);amounts.Controls.Add(b);
   }
   Label trackLabel=L("FINE CONTROL  •  1–100,000 (larger: type amount)",22,274,470,21,muted,new Font("Segoe UI",9));f.Controls.Add(trackLabel);
   TrackBar amountTrack=new TrackBar();amountTrack.Left=22;amountTrack.Top=297;amountTrack.Width=459;amountTrack.Height=42;
   amountTrack.Minimum=(int)Math.Min(param.min,100000);amountTrack.Maximum=(int)Math.Min(param.max,100000);
   if(amountTrack.Maximum<=amountTrack.Minimum)amountTrack.Maximum=amountTrack.Minimum+1;
   amountTrack.TickStyle=TickStyle.None;amountTrack.LargeChange=1000;amountTrack.Visible=param.min<=100000;trackLabel.Visible=amountTrack.Visible;
   amountTrack.Value=(int)Math.Max(amountTrack.Minimum,Math.Min(amountTrack.Maximum,(int)amount.Value));
   amountTrack.Scroll+=delegate {decimal v=amountTrack.Value;amount.Value=Math.Max(amount.Minimum,Math.Min(amount.Maximum,v));};
   amount.ValueChanged+=delegate{decimal v=amount.Value;if(v>=amountTrack.Minimum&&v<=amountTrack.Maximum)amountTrack.Value=(int)v;};
   f.Controls.Add(amountTrack);
   Button apply=B("USE VALUE",22,354,132,42,delegate{numericText.Text=amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);f.Close();});f.Controls.Add(apply);
   Button add=B("USE + QUEUE",164,354,167,42,delegate{
    numericText.Text=amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);QueueSelected();f.Close();},true);f.Controls.Add(add);
   Button cancel=B("CANCEL",343,354,138,42,delegate{f.Close();});f.Controls.Add(cancel);
   Label controls=L("PAD: LB/RB ±1 · Up/Down ±1,000 · Left/Right presets\nA use · X queue · B cancel · Hold B close menu",22,401,480,36,muted,new Font("Segoe UI",8));f.Controls.Add(controls);
   int amountIndex=-1;
   RegisterPad(f,delegate(Native.XState st,ushort press){
    if((press&0x0100)!=0)amount.Value=Math.Max(amount.Minimum,amount.Value-1);
    if((press&0x0200)!=0)amount.Value=Math.Min(amount.Maximum,amount.Value+1);
    if((press&0x0001)!=0)amount.Value=Math.Min(amount.Maximum,amount.Value+1000);
    if((press&0x0002)!=0)amount.Value=Math.Max(amount.Minimum,amount.Value-1000);
    if((press&0x000C)!=0&&amounts.Controls.Count>0){
     amountIndex=Math.Max(0,Math.Min(amounts.Controls.Count-1,amountIndex+((press&0x0008)!=0?1:-1)));
     ((Button)amounts.Controls[amountIndex]).PerformClick();amounts.Controls[amountIndex].Focus();
    }
    if((press&0x1000)!=0){apply.PerformClick();return;}
    if((press&0x4000)!=0){add.PerformClick();return;}
   });
   f.AcceptButton=apply;f.CancelButton=cancel;
   f.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){cancel.PerformClick();e.Handled=true;}};
   ShowPopup(f);
  }

  // A large readable queue view; nothing in this dialog executes a game command.
  void OpenQueueManager(){
   if(popupStack.Count>0||commonDialogDepth>0){ShowMenu();return;}
   Form dialog=new PopupForm();dialog.Text="CROWN — Queue Review (nothing executes here)";
   dialog.Size=new Size(670,525);dialog.MinimumSize=dialog.MaximumSize=dialog.Size;
   dialog.FormBorderStyle=FormBorderStyle.FixedDialog;dialog.MaximizeBox=false;dialog.MinimizeBox=false;
   dialog.StartPosition=FormStartPosition.CenterParent;dialog.BackColor=bg;dialog.ForeColor=txt;
   dialog.Font=new Font("Segoe UI",10);dialog.KeyPreview=true;dialog.ShowInTaskbar=false;
   Label head=L("REVIEW YOUR QUEUE",22,15,615,34,accent,new Font("Segoe UI",17,FontStyle.Bold));dialog.Controls.Add(head);
   Label msg=L("Check values before using RUN QUEUE in the main window. This view is safe.",22,54,615,25,muted);dialog.Controls.Add(msg);
   ListBox large=new ListBox();large.Left=22;large.Top=91;large.Width=610;large.Height=227;
   large.IntegralHeight=false;large.BackColor=panel2;large.ForeColor=txt;
   large.Font=new Font("Segoe UI",11);dialog.Controls.Add(large);
   Label count=L("",22,324,600,20,muted);dialog.Controls.Add(count);
   Action refresh=delegate{
    int old=large.SelectedIndex;large.BeginUpdate();large.Items.Clear();
    for(int i=0;i<queue.Count;i++){
     Queued q=queue[i];large.Items.Add((i+1)+". "+q.cmd.title+(q.value.HasValue?"  →  "+q.value.Value.ToString("N0",System.Globalization.CultureInfo.InvariantCulture):""));
    }large.EndUpdate();
    if(queue.Count>0)large.SelectedIndex=Math.Max(0,Math.Min(queue.Count-1,old));
    count.Text=queue.Count+" / 8 actions  •  Y never executes here  •  Co-op protection stays enabled";
   };
   Button up=B("↑ UP",22,357,96,36,delegate{if(large.SelectedIndex<1)return;
    queueBox.SelectedIndex=large.SelectedIndex;MoveQueued(-1);refresh();});dialog.Controls.Add(up);
   Button down=B("↓ DOWN",125,357,96,36,delegate{if(large.SelectedIndex<0||large.SelectedIndex>=queue.Count-1)return;
    queueBox.SelectedIndex=large.SelectedIndex;MoveQueued(1);refresh();});dialog.Controls.Add(down);
   Button edit=B("EDIT VALUE",228,357,129,36,delegate{if(large.SelectedIndex<0)return;
    queueBox.SelectedIndex=large.SelectedIndex;EditQueued();dialog.Close();});dialog.Controls.Add(edit);
   Button remove=B("REMOVE",365,357,116,36,delegate{if(large.SelectedIndex<0)return;
    queueBox.SelectedIndex=large.SelectedIndex;RemoveQueued();refresh();});dialog.Controls.Add(remove);
   Button clear=B("CLEAR",489,357,143,36,delegate{if(queue.Count==0)return;
    if(Notice("Clear all queued actions? Nothing was executed.","CROWN",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
    queue.Clear();RenderQueue();refresh();});dialog.Controls.Add(clear);
   Label padGuide=L("PAD: D-pad select · A edit · X remove · Hold Y up / Start down · B back",22,408,600,28,muted,new Font("Segoe UI",9));dialog.Controls.Add(padGuide);
   Button done=B("DONE / RETURN",403,438,229,36,delegate{dialog.Close();},true);dialog.Controls.Add(done);
   dialog.AcceptButton=edit;dialog.CancelButton=done;
   dialog.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){dialog.Close();e.Handled=true;}};
   large.DoubleClick+=delegate{edit.PerformClick();};
   RegisterPad(dialog,delegate(Native.XState st,ushort press){
     if((press&0x0001)!=0&&large.Items.Count>0)large.SelectedIndex=Math.Max(0,large.SelectedIndex-1);
     if((press&0x0002)!=0&&large.Items.Count>0)large.SelectedIndex=Math.Min(large.Items.Count-1,large.SelectedIndex+1);
     if((press&0x1000)!=0){edit.PerformClick();return;}
     if((press&0x4000)!=0)remove.PerformClick();
     if((press&0x8000)!=0)up.PerformClick();
     if((press&0x0010)!=0)down.PerformClick();
   });
   refresh();ShowPopup(dialog);
  }
  void Favourite(){GameCommand c=Selected();if(c==null)return;if(prefs.Favourites.Contains(c.id))prefs.Favourites.Remove(c.id);else prefs.Favourites.Add(c.id);SavePrefs();features.Invalidate();SelectFeature();}
  double ParameterValue(GameCommand c){if(c.@params==null||c.@params.Count==0)return 0;Parameter p=c.@params[0];double n;
   if(p.type=="select"){Choice choice=selectChoices.SelectedItem as Choice;if(choice==null)throw new Exception("Choose a supported setting.");n=choice.value;}
   else if(!Double.TryParse(numericText.Text,System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out n))throw new Exception("Enter a valid number.");
   if(Double.IsNaN(n)||Double.IsInfinity(n))throw new Exception("Enter a finite value.");
   if(p.type=="select"){if(p.options==null||!p.options.Any(o=>o.value==n))throw new Exception("Select a supported option value.");}
   else if(n<p.min||n>p.max)throw new Exception("Value must be between "+p.min+" and "+p.max);
   if(p.type!="select" && (c.id=="gold"||c.id=="treasury"||c.id=="seals") && n!=Math.Truncate(n))throw new Exception("Gold and Guild Seals use whole numbers only.");
   return n;
  }
  string Compile(GameCommand c,double? overrideValue=null){string output=c.template;
   if(c.@params!=null&&c.@params.Count>0){Parameter p=c.@params[0];double n=overrideValue.HasValue?overrideValue.Value:ParameterValue(c);string v=n.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture);
    if(c.repeatable&&p.id=="copies"){int copies=(int)n;return String.Join("\n",Enumerable.Repeat(output,copies));}
    output=output.Replace("{"+p.id+"}",v);
   }
   if(output.IndexOf("{",StringComparison.Ordinal)>=0)throw new Exception("Unresolved template parameter. Refusing to execute.");
   return output;
  }
  void EditQueued(){int i=queueBox.SelectedIndex;if(i<0||i>=queue.Count)return;
   Queued q=queue[i];if(!q.value.HasValue){SetStatus("This queued command has no numeric parameter; use the right-click menu to remove/reorder it.");return;}
   GameCommand target=catalogue.FirstOrDefault(c=>c.id==q.cmd.id);if(target!=null){categories.SelectedItem=target.group;features.SelectedItem=target;}
   if(target!=null&&target.@params!=null&&target.@params.Count>0&&target.@params[0].type=="select"){
    for(int j=0;j<selectChoices.Items.Count;j++)if(((Choice)selectChoices.Items[j]).value==q.value.Value){selectChoices.SelectedIndex=j;break;}
   }else numericText.Text=q.value.Value.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture);
   SetStatus("Queued value loaded into editor. Adjust it, then right-click queue → Update selected from value field.");
  }
  void UpdateQueued(){int i=queueBox.SelectedIndex;if(i<0||i>=queue.Count)return;
   Queued q=queue[i];if(q.cmd.@params==null||q.cmd.@params.Count==0){SetStatus("This command has no editable value.");return;}
   if(Selected()==null||Selected().id!=q.cmd.id){Error("Select the matching feature before updating this queued value.");return;}
   try{queue[i]=new Queued(q.cmd,ParameterValue(q.cmd));RenderQueue();queueBox.SelectedIndex=i;SetStatus("Queued value updated; review before running.");}catch(Exception e){Error(e.Message);}
  }
  void RemoveQueued(){int i=queueBox.SelectedIndex;if(i<0||i>=queue.Count)return;queue.RemoveAt(i);RenderQueue();if(queue.Count>0)queueBox.SelectedIndex=Math.Min(i,queue.Count-1);}
  void MoveQueued(int direction){int i=queueBox.SelectedIndex,j=i+direction;if(i<0||j<0||j>=queue.Count)return;Queued q=queue[i];queue[i]=queue[j];queue[j]=q;RenderQueue();queueBox.SelectedIndex=j;}
  void QueueSelected(){GameCommand c=Selected();if(c==null)return;
   try {double? n=c.@params!=null&&c.@params.Count>0?(double?)ParameterValue(c):null;
    if(queue.Count>=8){Error("Maximum eight commands per queue.");return;}
    if(c.id.StartsWith("unlocker:",StringComparison.Ordinal)&&queue.Any(q=>q.cmd.id==c.id)){Error("Item grant is already queued. Repeating grants may duplicate items.");return;}
    queue.Add(new Queued(c,n));RenderQueue();SetStatus("Queued: "+c.title+". Review before running.");}
   catch(Exception e){Error(e.Message);}
  }
  void RenderQueue(){queueBox.Items.Clear();foreach(Queued q in queue)queueBox.Items.Add(q.cmd.title+(q.value.HasValue?" = "+q.value.Value.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture):""));}
  void RunQueue(){if(queue.Count==0){Notice("Queue is empty.");return;}if(queue.Count>8){Notice("Limit a single run to eight actions.");return;}
   try{if(Execute(queue.Select(q=>q.cmd).ToList(), String.Join("\n",queue.Select(q=>Compile(q.cmd,q.value)).ToArray()))){queue.Clear();RenderQueue();}}catch(Exception e){Error(e.Message);}
  }
  void LoadPreset(){string[] ids;
   switch(presets.SelectedIndex){
    case 0: ids=new[]{"god-on","autoheal-on"};break;
    case 1: ids=new[]{"god-off","autoheal-off"};break;
    case 2: ids=new[]{"freecam-on","fps-off"};break;
    case 3: ids=new[]{"freecam-off","fps-off"};break;
    case 4: ids=new[]{"time"};break;
    case 5: ids=new[]{"time"};break;
    case 6: ids=new[]{"finisher-on","finisher-combo","finisher-angle","finisher-crescendo"};break;
    default: ids=new[]{"finisher-off","enemy-off","god-off"};break;
   }
   queue.Clear();foreach(string id in ids){GameCommand c=catalogue.FirstOrDefault(x=>x.id==id);if(c==null)continue;
    double? v=c.@params!=null&&c.@params.Count>0?(double?)((presets.SelectedIndex==4)?0:(presets.SelectedIndex==5)?6:c.@params[0].value):null;
    queue.Add(new Queued(c,v));}RenderQueue();SetStatus("Preset loaded into queue; inspect it before running.");
  }
  public class StoredQueue { public string id {get;set;} public double? value {get;set;} }
  void SaveLoadout(){if(queue.Count==0){Notice("Queue is empty.");return;}
   if(queue.Any(q=>q.cmd.id.StartsWith("unlocker:"))){Notice("Extracted unlocker commands are experimental and not saved in portable loadouts. Remove those entries before exporting.");return;}
   using(SaveFileDialog dlg=new SaveFileDialog()){dlg.Filter="CROWN loadout|*.json";dlg.FileName="My_Albion_Loadout.json";
    if(ShowCommon(dlg)!=DialogResult.OK)return;
    var items=queue.Select(q=>new StoredQueue{id=q.cmd.id,value=q.value}).ToArray();File.WriteAllText(dlg.FileName,new JavaScriptSerializer().Serialize(items),Encoding.UTF8);
    SetStatus("Loadout saved. This saves menu commands, not game progress.");}
  }
  void LoadLoadout(){using(OpenFileDialog dlg=new OpenFileDialog()){dlg.Filter="CROWN loadout|*.json";if(ShowCommon(dlg)!=DialogResult.OK)return;
    try{if(new FileInfo(dlg.FileName).Length>50000)throw new Exception("Loadout too large.");
     var items=new JavaScriptSerializer().Deserialize<List<StoredQueue>>(File.ReadAllText(dlg.FileName));
     if(items==null||items.Count>8)throw new Exception("Invalid queue length.");
     var loaded=new List<Queued>();foreach(var item in items){GameCommand c=catalogue.FirstOrDefault(x=>x.id==item.id);if(c==null)throw new Exception("Unknown command in loadout: "+item.id);
      if(item.value.HasValue&&c.@params!=null&&c.@params.Count>0){Parameter p=c.@params[0];if((p.type=="select"&&(p.options==null||!p.options.Any(o=>o.value==item.value.Value)))||(p.type!="select"&&(item.value.Value<p.min||item.value.Value>p.max)))throw new Exception("Value out of range for "+c.title);}
      loaded.Add(new Queued(c,item.value));}
     queue=loaded;RenderQueue();SetStatus("Loadout imported; inspect before executing.");}
    catch(Exception e){Error("Loadout import: "+e.Message);}
   }}
  void RunSelected(){GameCommand c=Selected();if(c==null)return;try{Execute(new List<GameCommand>{c},Compile(c));}catch(Exception e){Error(e.Message);}}
  bool Execute(List<GameCommand> cmds,string lua){
   if(cmds.Any(c=>c.risk=="high")&&!riskToggle.Checked){Error("High-risk commands are locked. Enable the checkbox only after backing up saves and testing on a spare character.");return false;}
   if(coopSafeToggle!=null&&coopSafeToggle.Checked&&cmds.Any(c=>c.id!="test-message")){Error("CO-OP PROTECTION is ON. Gameplay modifications are disabled while protecting your XLLN saves. Disable protection only for isolated single-player testing.");return false;}
   if(pulseBusy||pulsePending){Error("Another script is still active. Wait for the previous command to clear.");return false;}
   if(!ReadyScript()){Error("Script path is not ready. Use PREPARE SCRIPT PATH first, and install the separate Improved Script Injector before expecting game effects.");return false;}
   bool extra=cmds.Any(c=>c.risk=="high"||c.repeatable||new[]{"gold","treasury","seals","renown","spawn-hobbes","spawn-wolves","spawn-renegades"}.Contains(c.id));
   if(extra){string warn="This action can repeat every 60 rendered frames while the script is present. The auto-clear timer and Lua guard are best-effort, NOT guaranteed exactly-once. It may alter or corrupt a save, and co-op syncing is untested. Use a spare save. Continue?";
    if(Notice(warn,"CROWN — Repeated-action warning",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return false;}
   if(Process.GetProcessesByName("Fable3").Length==0){Error("Fable III is not running. No script was dispatched: startup could otherwise execute old instructions unexpectedly.");return false;}
   string unique=Guid.NewGuid().ToString("N");
   // A Lua global sentinel may suppress duplicate execution IF the injector retains Lua globals.
   // This is deliberately NOT treated as a reliable exactly-once mechanism.
   string payload=MARK+"\n-- GENERATED "+DateTime.Now.ToString("u")+"\n-- Clear on timeout. Community injector executes every ~60 frames.\n"+
     "CROWN_ALBION_DONE = CROWN_ALBION_DONE or {}\n"+
     "if not CROWN_ALBION_DONE['"+unique+"'] then\n  CROWN_ALBION_DONE['"+unique+"']=true\n"+
     String.Join("\n",lua.Split('\n').Select(x=>"  "+x).ToArray())+"\nend\n";
   string old=File.ReadAllText(script);if(old.Trim().Length>0&&!old.TrimStart().StartsWith(MARK)){Error("MyScript01.lua belongs to another mod. No changes made. Clear/backup its contents manually first.");return false;}
   BackupFile(script,"script");
   WriteAtomic(script,payload);
   string verify=File.ReadAllText(script);if(verify!=payload)throw new Exception("Script verification failed; check backups.");
   pulsePending=true;pulseText=payload;pulseBusy=true;clearTimer.Start();
   SetStatus("SENT: "+cmds.Count+" action(s); auto-clear scheduled. Check game for results. Not runtime-verified.");Log("Dispatched "+String.Join(",",cmds.Select(x=>x.id).ToArray()));
   // Return focus to Fable so scripts have a chance to tick, then F8 reopens this menu.
   try{var games=Process.GetProcessesByName("Fable3");if(games.Length>0&&games[0].MainWindowHandle!=IntPtr.Zero){HideMenu();Native.SetForegroundWindow(games[0].MainWindowHandle);}}catch(Exception ex){Log("Return focus: "+ex.Message);}
   return true;
  }
  bool ReadyScript(){if(!File.Exists(script)||!File.Exists(manifest))return false;
   try{return File.ReadAllLines(manifest).Any(line=>line.Trim().Equals(@"scripts\MyMod\MyScript01.lua",StringComparison.OrdinalIgnoreCase));}catch{return false;}}
  void ClearPulse(){ClearPulse(false);}
  void ClearPulse(bool manual){clearTimer.Stop();
   try{
    if(!ReadyScript()){SetStatus("No prepared script to clear.");return;}
    string now=File.ReadAllText(script);
    if(pulsePending){if(now==pulseText){BackupFile(script,"executed");WriteAtomic(script,MARK+"\n-- Ready\n");SetStatus("Script auto-cleared. Lua/game effects CANNOT be undone here.");Log("Auto-cleared pulse");}
     else SetStatus("Script changed externally; auto-clear refused to overwrite someone else's edits.");}
    else if(manual){if(now.Trim().Length>0&&!now.TrimStart().StartsWith(MARK)){Error("Non-CROWN script detected. Refusing to erase another mod.");return;}BackupFile(script,"manualclear");WriteAtomic(script,MARK+"\n-- Ready\n");SetStatus("CROWN script cleared. Does not undo in-game effects.");}
   }catch(Exception e){Error("Could not clear script: "+e.Message);}
   finally{pulsePending=false;pulseBusy=false;pulseText="";}
  }
  static void WriteAtomic(string path,string data){string temp=path+".crown.tmp";
   File.WriteAllText(temp,data,new UTF8Encoding(false));
   if(File.Exists(path)){File.Replace(temp,path,null);}else File.Move(temp,path);
  }
  void BackupFile(string path,string category){if(!File.Exists(path))return;
   string dir=Path.Combine(root,"Backups");Directory.CreateDirectory(dir);
   string dest=Path.Combine(dir,category+"-"+Path.GetFileName(path)+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".bak");File.Copy(path,dest,false);
   if(!SHA256File(path).Equals(SHA256File(dest),StringComparison.OrdinalIgnoreCase))throw new Exception("Backup hash mismatch. Original was not changed.");
  }
  static string SHA256File(string path){using(var s=File.OpenRead(path))using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","");}
  void Prepare(){UpdatePaths();if(Process.GetProcessesByName("Fable3").Length>0){Error("Close Fable III before preparing scripts or editing the manifest.");return;}if(!File.Exists(Path.Combine(game,"Fable3.exe"))){Error("Choose a valid Fable III game folder first.");return;}
   string hash=SHA256File(Path.Combine(game,"Fable3.exe"));if(!hash.Equals(EXPECTED_HASH,StringComparison.OrdinalIgnoreCase)){
    if(Notice("Fable3.exe checksum differs from your previously confirmed Patch 2 build. Prepare scripts anyway? Compatibility is NOT verified.","Unexpected executable",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;}
   string message="This creates data\\scripts\\MyMod script placeholders and appends two manifest lines if needed, with backup. It does NOT install the required third-party Improved Script Injector, nor change Fable3.exe, DLC, xlive.dll, XLLN or Steam files. Proceed?";
   if(Notice(message,"Prepare the non-destructive script path",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   try{
    string data=Path.Combine(game,"data");if(!Directory.Exists(data))throw new Exception("Missing data directory.");
    if(!File.Exists(manifest))throw new Exception("Missing data\\dir.manifest. Refusing to create one from scratch.");
    string folder=Path.GetDirectoryName(script);Directory.CreateDirectory(folder);
    if(!File.Exists(script))File.WriteAllText(script,MARK+"\n-- Ready\n",new UTF8Encoding(false));
    string fade=Path.Combine(folder,"MyScript02.lua");if(!File.Exists(fade))File.WriteAllText(fade,"-- CROWN_ALBION_MANAGED\n-- Empty fade script.\n",new UTF8Encoding(false));
    string current=File.ReadAllText(manifest);string updated=current;
    foreach(string line in new[]{@"scripts\MyMod\MyScript01.lua",@"scripts\MyMod\MyScript02.lua"})if(!current.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Any(s=>s.Trim().Equals(line,StringComparison.OrdinalIgnoreCase)))updated+=(updated.EndsWith("\n")?"":"\r\n")+line+"\r\n";
    if(updated!=current){BackupFile(manifest,"manifest");WriteAtomic(manifest,updated);}
    RefreshStatus();Notice("Script paths prepared. IMPORTANT: You must install the Improved Script Injector separately for Fable to execute these scripts. Test on a disposable save. No co-op files were altered.","Prepared",MessageBoxButtons.OK,MessageBoxIcon.Information);
   }catch(Exception e){Error("Prepare failed: "+e.Message);}
  }
  void Locate(){using(FolderBrowserDialog f=new FolderBrowserDialog()){f.Description="Select folder containing Fable3.exe (for you: B:\\Games\\Fable 3)";f.SelectedPath=game;if(ShowCommon(f)==DialogResult.OK){game=f.SelectedPath;UpdatePaths();SavePrefs();RefreshStatus();}}}
  void UpdatePaths(){script=Path.Combine(game,"data","scripts","MyMod","MyScript01.lua");manifest=Path.Combine(game,"data","dir.manifest");}
  void RefreshStatus(){string hash="NOT FOUND";string target=Path.Combine(game,"Fable3.exe");if(File.Exists(target)){
   try{hash=SHA256File(target).Equals(EXPECTED_HASH,StringComparison.OrdinalIgnoreCase)?"PATCH 2 CHECKSUM OK":"DIFFERENT EXE";}catch{hash="UNREADABLE";}}
   string ready=ReadyScript()?"SCRIPT PATH READY":"SCRIPT PATH NOT PREPARED";
   SetStatus("FABLE: "+hash+"  •  "+ready+"  •  Co-op protection: "+(coopSafeToggle!=null&&coopSafeToggle.Checked?"ON":"OFF")+"  •  F8 toggle / F1 help");
  }
  void SetStatus(string s){if(status!=null)status.Text=s;}
  void Error(string s){Log("ERROR: "+s);SetStatus("ERROR: "+s);Notice(s,"CROWN's ALBION",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
  bool IsGameForeground(){
   try{IntPtr fg=Native.GetForegroundWindow();if(fg==IntPtr.Zero)return false;uint pid;Native.GetWindowThreadProcessId(fg,out pid);
    foreach(var proc in Process.GetProcessesByName("Fable3"))if(proc.Id==(int)pid)return true;
   }catch{}return false;
  }
  bool IsMenuForeground(){
   IntPtr fg=Native.GetForegroundWindow();if(IsOwnForeground(fg))return true;
   foreach(Form child in OwnedForms){if(!child.IsDisposed&&child.Handle==fg)return true;}return false;
  }
  void RestoreGameFocus(){
   try{foreach(Process proc in Process.GetProcessesByName("Fable3")){if(proc.MainWindowHandle!=IntPtr.Zero){Native.SetForegroundWindow(proc.MainWindowHandle);return;}}}catch(Exception e){Log("Return focus: "+e.Message);}
  }
  void ShowMenu(){
   if(!Visible)Show();WindowState=FormWindowState.Normal;TopMost=false;
   if(commonDialogDepth>0&&commonWindow!=IntPtr.Zero){Native.SetForegroundWindow(commonWindow);return;}
   Form target=PopupOwner();if(target.Visible&&target.Enabled){target.Activate();target.BringToFront();if(target==this)features.Focus();}
  }
  void HideMenu(){
   Log("Menu hide requested; custom dialogs="+popupStack.Count+", common dialogs="+commonDialogDepth);
   hideRequested=true;padInput.Reset();
   if(commonDialogDepth>0){IntPtr fg=Native.GetForegroundWindow();if(IsOwnForeground(fg))NativeDialogButton(fg,false);return;}
   if(popupStack.Count>0){CancelPopup(popupStack[popupStack.Count-1]);return;}
   FinishHide();
  }
  void FinishHide(){
   if(!hideRequested)return;
   if(commonDialogDepth>0||popupStack.Count>0){BeginInvoke(new Action(HideMenu));return;}
   bool restore=IsMenuForeground();hideRequested=false;Hide();if(restore)RestoreGameFocus();
  }
  void ToggleFromGameHotkey(){
   if(!Visible&&Process.GetProcessesByName("Fable3").Length==0){ShowMenu();return;}
   if(!IsGameForeground()&&!IsMenuForeground())return; // F8 in other apps must not steal focus.
   Toggle();
  }
  void Toggle(){if(Visible)HideMenu();else ShowMenu();}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(!Native.RegisterHotKey(Handle,113,0,0x77))Log("F8 global hotkey registration failed");}
  protected override void OnHandleDestroyed(EventArgs e){Native.UnregisterHotKey(Handle,113);base.OnHandleDestroyed(e);}
  protected override void WndProc(ref Message m){if(m.Msg==Native.WM_HOTKEY&&m.WParam.ToInt32()==113){ToggleFromGameHotkey();return;}base.WndProc(ref m);}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){
   // Preprocess keys before WinForms can invoke a focused button on the owner.
   // This also handles input targeted at the main HWND by accessibility tools.
   if(popupStack.Count>0||commonDialogDepth>0){
    if(commonDialogDepth>0){if(key==Keys.Escape||key==Keys.Enter)NativeDialogButton(Native.GetForegroundWindow(),key==Keys.Enter);}
    else if(key==Keys.Escape)CancelPopup(PopupOwner());
    else if(key==Keys.Enter&&PopupOwner().AcceptButton!=null)PopupOwner().AcceptButton.PerformClick();
    return true;
   }
   return base.ProcessCmdKey(ref msg,key);
  }
  void OnGlobalKey(object sender,KeyEventArgs e){
   // Parent shortcuts must never run through a modal window, even if Windows
   // delivers an activation/key message to its disabled owner.
   if(popupStack.Count>0||commonDialogDepth>0){
    if(e.KeyCode==Keys.Escape){if(commonDialogDepth>0)NativeDialogButton(Native.GetForegroundWindow(),false);else CancelPopup(PopupOwner());}
    e.Handled=true;e.SuppressKeyPress=true;return;
   }
   if(e.KeyCode==Keys.Escape){HideMenu();e.Handled=true;e.SuppressKeyPress=true;return;}
   if(e.KeyCode==Keys.F1){ShowHelp();e.Handled=true;return;}
   if(e.Control&&e.KeyCode==Keys.F){search.Focus();e.Handled=true;return;}
   if(e.Control&&e.KeyCode==Keys.G){OpenGoldPad();e.Handled=true;return;}
   if(e.Control&&e.KeyCode==Keys.Q){OpenQueueManager();e.Handled=true;return;}
   if(e.Control&&e.KeyCode==Keys.Enter){RunQueue();e.Handled=true;return;}
   if(e.KeyCode==Keys.Enter&&features.Focused){QueueSelected();e.Handled=true;return;}
  }
  void ShowHelp(){Notice("KEYBOARD\nF8 menu; Ctrl+F search; Ctrl+G exact gold; Ctrl+Q queue\nEnter queue; Ctrl+Enter run queue; Esc hide\n\nXBOX PAD (enabled by default for new settings)\nL3 + R3 together: toggle menu\nD-pad Up/Down: features; Left/Right: categories\nA: select / open exact integer amount; otherwise queue\nBack (View): review queue; X: favourite\nLB/RB: change value (LT x10 / RT x100)\nHold Y: run selected; hold Start: run queue\nTap B: cancel / back; hold B 0.65s: close entire menu\n\nExact amount: A use; X use + queue; B cancel\nRelease buttons when switching windows.\nController actions require foreground menu; no game input hooks.\nCo-op protection stays ON by default. Physical pad and game QA pending.","CROWN's ALBION — Controls",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  void AdjustSelectedValue(int direction){GameCommand c=Selected();if(c==null||c.@params==null||c.@params.Count==0)return;Parameter p=c.@params[0];if(p.type=="select"){
    if(selectChoices.Items.Count>0)selectChoices.SelectedIndex=Math.Max(0,Math.Min(selectChoices.Items.Count-1,selectChoices.SelectedIndex+direction));return;}
   double n;if(!Double.TryParse(numericText.Text,System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out n))n=p.value;
   double step=p.step>0?p.step:1;n=Math.Max(p.min,Math.Min(p.max,n+step*direction));numericText.Text=n.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture);
  }
  // Ownership is established before any child HWND is created. All show/close paths
  // share this lifecycle; never raise a disabled owner over its modal child.
  Form PopupOwner(){return popupStack.Count>0?popupStack[popupStack.Count-1]:this;}
  void CancelPopup(Form form){
   if(form.IsDisposed)return;
   if(form.CancelButton!=null)form.CancelButton.PerformClick();
   else {form.DialogResult=DialogResult.Cancel;form.Close();}
  }
  void RestorePopupFocus(Form owner,Control previous){
   padInput.Reset();
   if(owner.IsDisposed||!owner.Visible||!owner.Enabled||hideRequested)return;
   // Do not steal focus back from an unrelated app after Alt-Tab.
   if(IsOwnForeground(Native.GetForegroundWindow())){
    owner.Activate();if(previous!=null&&!previous.IsDisposed&&previous.CanFocus)previous.Focus();
   }
  }
  DialogResult ShowPopup(Form form){
   Form owner=PopupOwner();Control previous=owner.ActiveControl;
   form.Owner=owner;form.TopMost=false;form.StartPosition=FormStartPosition.CenterParent;
   form.ShowInTaskbar=false;form.KeyPreview=true;
   popupStack.Add(form);padInput.Reset();
   Log("Popup open: "+form.Text+"; owner="+owner.Text);
   form.Shown+=delegate{form.Activate();form.BringToFront();};
   try{return form.ShowDialog(owner);}
   finally{
    popupStack.Remove(form);form.Dispose();RestorePopupFocus(owner,previous);FinishHide();Log("Popup closed; owner enabled="+owner.Enabled+", visible="+owner.Visible);
   }
  }
  DialogResult Notice(string message){return Notice(message,"CROWN's ALBION",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  DialogResult Notice(string message,string title){return Notice(message,title,MessageBoxButtons.OK,MessageBoxIcon.Information);}
  DialogResult Notice(string message,string title,MessageBoxButtons buttons,MessageBoxIcon icon){
   PopupForm dialog=new PopupForm();dialog.Text=title;dialog.BackColor=bg;dialog.ForeColor=txt;dialog.Font=Font;
   int textHeight=Math.Max(85,Math.Min(420,TextRenderer.MeasureText(message,Font,new Size(600,0),TextFormatFlags.WordBreak).Height+30));
   dialog.ClientSize=new Size(650,textHeight+90);dialog.FormBorderStyle=FormBorderStyle.FixedDialog;
   dialog.MaximizeBox=false;dialog.MinimizeBox=false;
   TextBox content=new TextBox();content.Text=message;content.Multiline=true;content.ReadOnly=true;
   content.ScrollBars=ScrollBars.Vertical;content.BorderStyle=BorderStyle.None;content.BackColor=bg;content.ForeColor=txt;
   content.SetBounds(22,18,606,textHeight);content.TabStop=false;dialog.Controls.Add(content);
   bool yesNo=buttons==MessageBoxButtons.YesNo;
   Button ok=B(yesNo?"YES / CONFIRM":"OK",yesNo?350:492,textHeight+29,136,40,delegate{dialog.DialogResult=yesNo?DialogResult.Yes:DialogResult.OK;dialog.Close();},true);
   dialog.Controls.Add(ok);dialog.AcceptButton=ok;
   if(yesNo){Button no=B("NO / CANCEL",492,textHeight+29,136,40,delegate{dialog.DialogResult=DialogResult.No;dialog.Close();});dialog.Controls.Add(no);dialog.CancelButton=no;}
   else dialog.CancelButton=ok;
   RegisterPad(dialog,delegate(Native.XState state,ushort press){
    if((press&0x000F)!=0)dialog.SelectNextControl(dialog.ActiveControl,(press&0x000A)!=0,true,true,true);
    if((press&0x1000)!=0){Button selected=dialog.ActiveControl as Button;if(selected!=null)selected.PerformClick();else dialog.AcceptButton.PerformClick();}
   });
   dialog.Shown+=delegate{ok.Focus();};
   return ShowPopup(dialog);
  }
  DialogResult ShowCommon(CommonDialog dialog){
   Form owner=PopupOwner();Control previous=owner.ActiveControl;commonDialogDepth++;padInput.Reset();
   using(System.Windows.Forms.Timer center=new System.Windows.Forms.Timer()){
    IntPtr positioned=IntPtr.Zero;DateTime positionUntil=DateTime.MinValue;center.Interval=40;
    center.Tick+=delegate{
     IntPtr fg=Native.GetForegroundWindow();
     if(hideRequested){if(IsOwnForeground(fg))NativeDialogButton(fg,false);return;}
     if(fg==owner.Handle||!IsOwnForeground(fg))return;
     StringBuilder cls=new StringBuilder(64);Native.GetClassName(fg,cls,64);if(cls.ToString()!="#32770")return;
     if(fg!=positioned){positioned=fg;positionUntil=DateTime.UtcNow.AddMilliseconds(750);}
     if(DateTime.UtcNow>positionUntil)return;
     Native.Rect bounds,parent;if(!Native.GetWindowRect(fg,out bounds)||!Native.GetWindowRect(owner.Handle,out parent))return;
     // Shell dialogs can switch thread DPI awareness. Use native coordinates for
     // both windows instead of mixing cached WinForms bounds with shell bounds.
     Native.MonitorInfo monitor=new Native.MonitorInfo();monitor.size=Marshal.SizeOf(monitor);
     if(!Native.GetMonitorInfo(Native.MonitorFromWindow(owner.Handle,2),ref monitor)){Log("Common monitor lookup failed");return;}
     Native.Rect area=monitor.work;
     int width=bounds.right-bounds.left,height=bounds.bottom-bounds.top;
     int x=Math.Max(area.left,Math.Min(area.right-width,parent.left+(parent.right-parent.left-width)/2));
     int y=Math.Max(area.top,Math.Min(area.bottom-height,parent.top+(parent.bottom-parent.top-height)/2));
     Native.SetWindowPos(fg,IntPtr.Zero,x,y,0,0,0x0015);positioned=fg;commonWindow=fg;
    };
    center.Start();
    try{return dialog.ShowDialog(owner);}
    finally{center.Stop();commonWindow=IntPtr.Zero;commonDialogDepth--;RestorePopupFocus(owner,previous);FinishHide();}
   }
  }
  void RegisterPad(Form form,Action<Native.XState,ushort> route){
   form.Owner=PopupOwner(); // Do not force Handle creation before assigning Owner.
   IntPtr registered=IntPtr.Zero;
   EventHandler add=delegate{registered=form.Handle;padRoutes[registered]=route;};
   form.HandleCreated+=add;
   if(form.IsHandleCreated)add(form,EventArgs.Empty);
   form.HandleDestroyed+=delegate{padRoutes.Remove(registered);};
   form.FormClosed+=delegate{padRoutes.Remove(registered);};
  }
  bool IsOwnForeground(IntPtr fg){uint pid;Native.GetWindowThreadProcessId(fg,out pid);return pid==(uint)Process.GetCurrentProcess().Id;}
  void NativeDialogButton(IntPtr fg,bool confirm){
   StringBuilder cls=new StringBuilder(64);Native.GetClassName(fg,cls,64);
   if(cls.ToString()!="#32770")return; // Windows standard dialogs only, owned by this process.
   int id=Native.GetDlgCtrlID(Native.GetFocus());
   if(confirm){if(id!=1&&id!=2&&id!=6&&id!=7)id=Native.GetDlgItem(fg,6)!=IntPtr.Zero?6:1;}
   else id=Native.GetDlgItem(fg,2)!=IntPtr.Zero?2:Native.GetDlgItem(fg,7)!=IntPtr.Zero?7:1;
   if(Native.GetDlgItem(fg,id)!=IntPtr.Zero)Native.PostMessage(fg,0x0111,new IntPtr(id),IntPtr.Zero);
  }
  void ControllerBack(IntPtr fg){
   Form active=Form.FromHandle(fg) as Form;
   if(active!=null&&active!=this){CancelPopup(active);return;}
   if(fg!=Handle){NativeDialogButton(fg,false);return;}
   if(search.Text.Length>0){search.Clear();features.Focus();return;}
   if(categories.SelectedIndex!=0){categories.SelectedIndex=0;features.Focus();return;}
   HideMenu();
  }
  void OnControllerTick(object sender,EventArgs e){
   if(controllerToggle==null||!controllerToggle.Checked){padInput.Reset();return;}
   try{
    Native.XState st;if(Native.XInputGetState(0,out st)!=0){padInput.Reset();return;}
    IntPtr fg=Native.GetForegroundWindow();bool own=IsOwnForeground(fg);
    if(!own&&!IsGameForeground()){padInput.Reset();return;}
    padInput.Sample(st.pad.buttons,st.pad.lt,st.pad.rt,fg.ToInt64(),padClock.Elapsed.TotalMilliseconds);
    if(padInput.Toggle){
     if(Visible)HideMenu();
     else ShowMenu();return;
    }
    if(!own||!Visible)return;
    if(padInput.Close){HideMenu();return;}
    if(padInput.Back){ControllerBack(fg);return;}
    ushort pressed=padInput.Pressed;
    Action<Native.XState,ushort> route;
    if(padRoutes.TryGetValue(fg,out route)){route(st,pressed);return;}
    if(fg!=Handle){
     if((pressed&0x000F)!=0)Native.PostMessage(fg,0x0100,new IntPtr((pressed&0x0005)!=0?0x25:0x27),IntPtr.Zero);
     if((pressed&0x1000)!=0)NativeDialogButton(fg,true);return;
    }
    if(!Visible||Form.ActiveForm!=this||Native.GetForegroundWindow()!=Handle)return;
    if((pressed&0x0001)!=0&&features.Items.Count>0){features.SelectedIndex=Math.Max(0,features.SelectedIndex-1);features.Focus();}
    if((pressed&0x0002)!=0&&features.Items.Count>0){features.SelectedIndex=Math.Min(features.Items.Count-1,features.SelectedIndex+1);features.Focus();}
    if((pressed&0x0004)!=0){categories.SelectedIndex=Math.Max(0,categories.SelectedIndex-1);features.Focus();}
    if((pressed&0x0008)!=0){categories.SelectedIndex=Math.Min(categories.Items.Count-1,categories.SelectedIndex+1);features.Focus();}
    int multiplier=st.pad.rt>=180?100:st.pad.lt>=180?10:1;
    if((pressed&0x0100)!=0)AdjustSelectedValue(-multiplier);
    if((pressed&0x0200)!=0)AdjustSelectedValue(multiplier);
    if((pressed&0x1000)!=0){
     GameCommand c=Selected();
     if(c!=null&&c.@params!=null&&c.@params.Count>0&&c.@params[0].type!="select"&&c.@params[0].step>=1)OpenAmountPad();
     else QueueSelected();return;
    }
    if((pressed&0x0020)!=0){OpenQueueManager();return;} // Back / View
    if((pressed&0x4000)!=0)Favourite();
    // Hold-to-confirm destructive shortcuts. Quick taps NEVER execute gameplay commands.
    if((pressed&0x8000)!=0){RunSelected();return;}
    if((pressed&0x0010)!=0){RunQueue();return;}
   }catch(DllNotFoundException){controllerTimer.Stop();SetStatus("XInput unavailable; use keyboard or mouse.");}
   catch(EntryPointNotFoundException){controllerTimer.Stop();SetStatus("XInput function unavailable.");}
   catch(Exception ex){Log("Controller: "+ex.Message);}
  }
  [STAThread] public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Trainer());}
 }
}
