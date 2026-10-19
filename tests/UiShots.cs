// CROWN's ALBION v2.0 — dialog screenshot harness.
//
// Opens each themed dialog, renders it to a PNG under docs\ui, then closes it.
// Used as visual QA evidence for the premium shell. Never dispatches a script
// and never touches the game folder.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROWNsAlbion;

class UiShots {
 static Trainer t;
 static string outDir;

 static object Field(string n) {
  return typeof(Trainer).GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(t);
 }
 static void SetField(string n, object v) {
  typeof(Trainer).GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(t, v);
 }
 static void Call(string n, params object[] args) {
  typeof(Trainer).InvokeMember(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, t, args);
 }

 // Open a modal dialog, photograph it, then cancel it again.
 static void Shot(string name, string method) {
  bool captured = false;
  using (var timer = new Timer()) {
   timer.Interval = 350;
   timer.Tick += delegate {
    timer.Stop();
    var stack = (List<Form>)Field("popupStack");
    if (stack.Count == 0) return;
    Form f = stack[stack.Count - 1];
    try {
     using (var bmp = new Bitmap(Math.Max(1, f.Width), Math.Max(1, f.Height)))
     using (var g = Graphics.FromImage(bmp)) {
      g.Clear(Albion.Leather);
      f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
      Directory.CreateDirectory(outDir);
      bmp.Save(Path.Combine(outDir, name + ".png"), ImageFormat.Png);
      captured = true;
     }
    } catch (Exception ex) { Console.Error.WriteLine(name + ": " + ex.Message); }
    Call("CancelPopup", f);
   };
   timer.Start();
   try { Call(method); } catch (Exception ex) { Console.Error.WriteLine(method + ": " + ex.Message); }
  }
  Console.WriteLine((captured ? "PASS " : "FAIL ") + name + " captured");
 }

 [STAThread]
 static int Main() {
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);
  if (System.Diagnostics.Process.GetProcessesByName("Fable3").Length != 0) {
   Console.Error.WriteLine("Close Fable III before capturing dialogs."); return 2;
  }
  outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "docs", "ui");
  outDir = Path.GetFullPath(outDir);
  try {
   using (t = new Trainer()) {
    ((Timer)Field("controllerTimer")).Stop();
    ((Timer)Field("clearTimer")).Stop();
    ((Timer)Field("toastTimer")).Stop();
    ((Timer)Field("diagTimer")).Stop();
    ((NotifyIcon)Field("tray")).Visible = false;
    SetField("suspendStateSave", true);

    Shot("vault", "OpenItemVault");
    Shot("queue", "OpenQueueManager");
    Shot("library", "OpenResearchLibrary");
    Shot("diagnostics", "OpenDiagnostics");
    Shot("profiles", "OpenProfiles");
    Shot("settings", "OpenSettings");
    Shot("help", "ShowHelp");
   }
  } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
  Console.WriteLine("Dialogs captured to " + outDir);
  return 0;
 }
}
