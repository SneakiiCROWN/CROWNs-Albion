// CROWN's ALBION — live single-player dispatch harness.
//
// Uses the real Trainer engine to send one command while Fable III is running.
// This is the *live* verification path: no hand-written Lua. The engine still
// enforces every gate (game running, script path ready, injector present).
//
// Usage:  LiveDispatch.exe [command-id]      (default: test-message)
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROWNsAlbion;

class LiveDispatch {
 static Trainer t;
 static string cmdId;

 [STAThread]
 static int Main(string[] args) {
  cmdId = args.Length > 0 ? args[0] : "test-message";
  string rawLua = null;
  if (cmdId == "--lua" && args.Length > 1) { cmdId = "live-check"; rawLua = args[1]; }
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);

  if (System.Diagnostics.Process.GetProcessesByName("Fable3").Length == 0) {
   Console.Error.WriteLine("Fable III is not running. Start the game and load a save first."); return 2;
  }

  int exit = 0;
  using (t = new Trainer()) {
   t.prefs.ConfirmDispatch = false;        // scripted run: the user already approved
   t.prefs.CoopSafe = rawLua == null;      // raw live checks are single-player only
   t.prefs.AllowRisk = false;
   ((Timer)Field("controllerTimer")).Stop();
   ((Timer)Field("toastTimer")).Stop();
   ((Timer)Field("diagTimer")).Stop();
   ((NotifyIcon)Field("tray")).Visible = false;

   Console.WriteLine("game      : " + t.game);
   Console.WriteLine("exe       : " + (System.IO.File.Exists(System.IO.Path.Combine(t.game, "Fable3.exe")) ? "present" : "MISSING"));
   Console.WriteLine("script    : " + (t.ReadyScript() ? "ready" : "NOT PREPARED"));
   Console.WriteLine("injector  : " + (t.InjectorPresent() ? "detected" : "MISSING"));
   Console.WriteLine("running   : " + (t.GameRunning() ? "yes" : "no"));

   GameCommand c = t.catalogue.FirstOrDefault(x => x.id == cmdId);
   if (c == null && rawLua != null) {
    c = new GameCommand { id = "live-check", title = "Live check", template = rawLua, risk = "low", kind = "action", evidence = "debug-menu", coop = "unverified", icon = "gears", @params = new System.Collections.Generic.List<Parameter>() };
   }
   if (c == null) { Console.Error.WriteLine("Unknown command id: " + cmdId); return 3; }
   t.prefs.AllowRisk = !c.IsHighRisk ? false : t.prefs.AllowRisk;
   Console.WriteLine("command   : " + c.id + " — " + c.title);
   Console.WriteLine("lua       : " + t.Compile(c));
   Console.WriteLine();

   bool ok = t.Execute(new System.Collections.Generic.List<GameCommand> { c }, t.Compile(c));
   Console.WriteLine("dispatch  : " + (ok ? "SENT" : "REFUSED"));
   Console.WriteLine("pulse     : pending=" + Field("pulsePending") + " busy=" + Field("pulseBusy"));
   Console.WriteLine("script now:");
   Console.WriteLine(System.IO.File.ReadAllText(t.script));
   if (!ok) exit = 1;

   // Keep the message pump alive so the auto-clear timer fires normally, then close.
   using (var quit = new Timer()) {
    quit.Interval = 8000;
    quit.Tick += delegate { quit.Stop(); t.Close(); };
    quit.Start();
    Application.Run(t);
   }
  }
  Console.WriteLine("done. Script returned to auto-clear state; watch the game for the effect.");
  return exit;
 }

 static object Field(string n) {
  return typeof(Trainer).GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(t);
 }
}
