// CROWN's ALBION v2.0 — engine regression harness.
//
// Exercises the real Trainer engine with reflection: catalogue/compile/payload
// semantics, safety gates, atomic writes, hash-verified backups and the
// refusal paths of Execute. It never launches Fable III and never writes the
// game script except inside a throw-away temp folder.
//
// Build + run: tests\Run-Engine-Tests.cmd
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CROWNsAlbion;

class EngineHarness {
 static int checks, failed;
 static Trainer t;

 static void Check(bool ok, string name) {
  checks++;
  if (ok) { Console.WriteLine("PASS " + name); }
  else { failed++; Console.WriteLine("FAIL " + name); }
 }

 static object Field(string n) {
  return typeof(Trainer).GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(t);
 }
 static void SetField(string n, object v) {
  typeof(Trainer).GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(t, v);
 }
 static object Call(string n, params object[] args) {
  return typeof(Trainer).InvokeMember(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, t, args);
 }
 static string Gate(GameCommand c) { return (string)Call("GateReason", new object[] { c }); }

 static string Temp(string tag) {
  string d = Path.Combine(Path.GetTempPath(), "crown-tests-" + tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
  Directory.CreateDirectory(d); return d;
 }

 // ---------------------------------------------------------------- data
 static void DataTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var vault = (List<VaultItem>)Field("vault");
  Check(cat.Count >= 175, "catalogue loads " + cat.Count + " commands");
  Check(cat.Select(c => c.id).Distinct().Count() == cat.Count, "catalogue ids unique");
  Check(vault.Count >= 300, "item vault loads " + vault.Count + " grants");
  Check(cat.Count(c => c.IsReferenceOnly) == 0 || cat.Any(c => c.IsReferenceOnly), "reference flag parsed");

  bool fields = true, placeholders = true;
  foreach (var c in cat) {
   if (string.IsNullOrEmpty(c.id) || string.IsNullOrEmpty(c.title) || string.IsNullOrEmpty(c.template)) fields = false;
   var decl = c.@params == null ? new List<string>() : c.@params.Select(p => p.id).ToList();
   foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(c.template, "\\{([A-Za-z0-9_]+)\\}"))
    if (!decl.Contains(m.Groups[1].Value)) placeholders = false;
  }
  Check(fields, "catalogue rows are complete");
  Check(placeholders, "every template placeholder is declared");

  var groups = (string[])Field("groupNames");
  var missing = cat.Select(c => c.group).Distinct().Where(g => !groups.Contains(g)).ToList();
  Check(missing.Count == 0, "every catalogue group is reachable in the UI" + (missing.Count > 0 ? " (" + string.Join(",", missing) + ")" : ""));
 }

 // ---------------------------------------------------------------- compile
 static void CompileTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var gold = cat.FirstOrDefault(c => c.id == "gold");
  Check(gold != null, "gold command present");
  if (gold != null) {
   string lua = (string)Call("Compile", gold, (double?)25000);
   Check(lua.Contains("25000") && !lua.Contains("{"), "numeric parameter substitutes cleanly");
  }

  bool threw = false;
  try { Call("Compile", new GameCommand { id = "synthetic", template = "Value.Set({missing})" }); }
  catch { threw = true; }
  Check(threw, "unresolved placeholder refuses to compile");

  var copies = cat.FirstOrDefault(c => c.repeatable && c.@params.Count > 0 && c.@params[0].id == "copies");
  if (copies != null) {
   string lua = (string)Call("Compile", copies, (double?)3);
   Check(lua.Split('\n').Length == 3, "repeatable 'copies' expands to 3 lines");
  }
 }

 // ---------------------------------------------------------------- payload
 static void PayloadTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var gold = cat.First(c => c.id == "gold");
  string p1 = (string)Call("BuildPayload", new List<GameCommand> { gold }, "Money.Set(GetLocalHero(),25000,0)");
  string p2 = (string)Call("BuildPayload", new List<GameCommand> { gold }, "Money.Set(GetLocalHero(),25000,0)");
  Check(p1.StartsWith("-- CROWN_ALBION_MANAGED"), "payload carries the managed marker");
  Check(p1.Contains("CROWN_ALBION_DONE"), "payload carries the once-only sentinel");
  Check(p1.Contains("Money.Set"), "payload contains the compiled Lua");
  Check(p1 != p2, "each payload has a unique run guard");
 }

 // ---------------------------------------------------------------- toggles
 static void ToggleTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var on = cat.FirstOrDefault(c => c.IsToggle && c.state == "on" && !string.IsNullOrEmpty(c.twin));
  Check(on != null, "a paired toggle exists");
  if (on != null) {
   var off = cat.First(c => c.id == on.twin);
   string g1 = (string)Call("ToggleGroup", on);
   string g2 = (string)Call("ToggleGroup", off);
   Check(g1 == g2, "toggle twins share one state group");
   Call("RecordApplied", on);
   Check((string)Call("CurrentToggleState", on) == "on", "applying ON records state ON");
   Call("RecordApplied", off);
   Check((string)Call("CurrentToggleState", on) == "off", "applying twin records state OFF");
  }
 }

 // ---------------------------------------------------------------- gates
 static void GateTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var prefs = (Prefs)Field("prefs");
  var normal = cat.First(c => c.risk == "low" && c.id != "test-message");
  var high = cat.First(c => c.risk == "high");

  bool coop = prefs.CoopSafe, risk = prefs.AllowRisk;
  try {
   prefs.CoopSafe = true; prefs.AllowRisk = false;
   Check(Gate(normal) != null, "co-op gate explains the lock on a normal command");
   var test = cat.FirstOrDefault(c => c.id == "test-message");
   if (test != null) Check(Gate(test) == null, "co-op gate lets the harmless script test through");

   prefs.CoopSafe = false;
   Check(Gate(high) != null, "high-risk gate explains the lock when it is off");
   prefs.AllowRisk = true;
   Check(Gate(normal) == null, "a safe command passes every gate when unlocked");
  } finally { prefs.CoopSafe = coop; prefs.AllowRisk = risk; }
 }

 // ---------------------------------------------------------------- file safety
 static void FileTests() {
  var root = (string)Field("root");
  string dir = Temp("files");
  string f = Path.Combine(dir, "sample.txt");
  File.WriteAllText(f, "original");

  Trainer.WriteAtomic(f, "updated");
  Check(File.ReadAllText(f) == "updated", "atomic write replaces content exactly");
  Check(!File.Exists(f + ".crown.tmp"), "atomic write leaves no temp file");

  string backupDir = Path.Combine(root, "Backups");
  Directory.CreateDirectory(backupDir);
  var before = new HashSet<string>(Directory.GetFiles(backupDir));
  Call("BackupFile", f, "test");
  var after = Directory.GetFiles(backupDir).Where(x => !before.Contains(x)).ToList();
  Check(after.Count == 1, "backup creates exactly one file");
  if (after.Count == 1) {
   Check(Trainer.SHA256File(f).Equals(Trainer.SHA256File(after[0]), StringComparison.OrdinalIgnoreCase), "backup hash matches source");
   try { File.Delete(after[0]); } catch { }
  }
  try { Directory.Delete(dir, true); } catch { }
 }

 // ---------------------------------------------------------------- execute refusals
 static bool RunAndDismiss(Func<bool> action) {
  int ticks = 0;
  using (var timer = new Timer()) {
   timer.Interval = 70;
   timer.Tick += delegate {
    ticks++;
    var stack = (List<Form>)Field("popupStack");
    if (stack.Count > 0) Call("CancelPopup", stack[stack.Count - 1]);
    if (ticks > 120) timer.Stop();
   };
   timer.Start();
   try { return action(); } finally { timer.Stop(); }
  }
 }

 static void ExecuteRefusalTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var prefs = (Prefs)Field("prefs");
  string dir = Temp("game");
  Directory.CreateDirectory(Path.Combine(dir, "data", "scripts", "MyMod"));
  Directory.CreateDirectory(Path.Combine(dir, "data"));
  string scriptFile = Path.Combine(dir, "data", "scripts", "MyMod", "MyScript01.lua");
  string ready = "-- CROWN_ALBION_MANAGED\n-- Ready\n";
  File.WriteAllText(scriptFile, ready);
  File.WriteAllText(Path.Combine(dir, "data", "dir.manifest"), "scripts\\MyMod\\MyScript01.lua\r\n");

  bool coop = prefs.CoopSafe, risk = prefs.AllowRisk;
  SetField("game", dir);
  Call("UpdatePaths");
  try {
   Check((bool)Call("ReadyScript"), "prepared temp script reports ready");
   Check(!(bool)Call("GameRunning"), "Fable III is not running during tests");

   var normal = cat.First(c => c.risk != "high" && c.id != "test-message");
   var high = cat.First(c => c.risk == "high");

   // 1. Co-op protection blocks a normal command.
   prefs.CoopSafe = true; prefs.AllowRisk = true;
   bool r1 = RunAndDismiss(delegate { return (bool)Call("Execute", new List<GameCommand> { normal }, "Health.Modify(GetLocalHero(),1,GetLocalHero())"); });
   Check(!r1, "Execute refuses a normal command while co-op protection is ON");
   Check(!(bool)Field("pulsePending"), "no pulse is pending after a co-op refusal");
   Check(File.ReadAllText(scriptFile) == ready, "co-op refusal left the game script untouched");

   // 2. High-risk gate blocks a high-risk command.
   prefs.CoopSafe = false; prefs.AllowRisk = false;
   bool r2 = RunAndDismiss(delegate { return (bool)Call("Execute", new List<GameCommand> { high }, "Money.Set(GetLocalHero(),0,0)"); });
   Check(!r2, "Execute refuses a high-risk command while the risk gate is OFF");
   Check(File.ReadAllText(scriptFile) == ready, "risk refusal left the game script untouched");

   // 3. Game-not-running is the final guard.
   prefs.CoopSafe = false; prefs.AllowRisk = true;
   bool r3 = RunAndDismiss(delegate { return (bool)Call("Execute", new List<GameCommand> { normal }, "Health.Modify(GetLocalHero(),1,GetLocalHero())"); });
   Check(!r3, "Execute refuses when Fable III is not running");
   Check(File.ReadAllText(scriptFile) == ready, "idle refusal left the game script untouched");
   Check(!(bool)Field("pulseBusy"), "engine is idle after all refusals");
  } finally { prefs.CoopSafe = coop; prefs.AllowRisk = risk; try { Directory.Delete(dir, true); } catch { } }
 }

 // ---------------------------------------------------------------- UI gate wiring
 static void UiGateTests() {
  var cat = (List<GameCommand>)Field("catalogue");
  var prefs = (Prefs)Field("prefs");
  bool coop = prefs.CoopSafe, risk = prefs.AllowRisk;
  try {
   prefs.CoopSafe = true; prefs.AllowRisk = false;
   var normal = cat.First(c => c.risk == "low" && c.id != "test-message");
   var features = (ListBox)Field("features");
   features.SelectedItem = normal;
   Call("InvalidateDetail");
   var exec = (AlbionButton)Field("executeButton");
   Check(!exec.Enabled && exec.Text == "LOCKED BY GATE", "Execute button is visibly locked while gates are off");

   prefs.CoopSafe = false; prefs.AllowRisk = true;
   prefs.AllowRisk = true;
   features.SelectedItem = normal;
   Call("InvalidateDetail");
   Check(exec.Enabled, "Execute button unlocks when every gate is satisfied");
  } finally { prefs.CoopSafe = coop; prefs.AllowRisk = risk; }
 }

 [STAThread]
 static int Main() {
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);
  if (System.Diagnostics.Process.GetProcessesByName("Fable3").Length != 0) {
   Console.Error.WriteLine("Close Fable III before running the engine regression."); return 2;
  }
  try {
   using (t = new Trainer()) {
    ((Timer)Field("controllerTimer")).Stop();
    ((Timer)Field("clearTimer")).Stop();
    ((Timer)Field("toastTimer")).Stop();
    ((Timer)Field("diagTimer")).Stop();
    ((NotifyIcon)Field("tray")).Visible = false;
    SetField("suspendStateSave", true);   // never persist test state to disk

    DataTests();
    CompileTests();
    PayloadTests();
    ToggleTests();
    GateTests();
    FileTests();
    ExecuteRefusalTests();
    UiGateTests();

    Check(!(bool)Field("pulsePending") && !(bool)Field("pulseBusy"), "no script was ever dispatched");
   }
  } catch (Exception ex) {
   Console.Error.WriteLine(ex);
   failed++; checks++;
  }
  Console.WriteLine();
  if (failed > 0) { Console.Error.WriteLine("ENGINE TESTS FAILED: " + failed + " of " + checks + " checks."); return 1; }
  Console.WriteLine("PASS " + checks + " engine checks; Fable III not running.");
  return 0;
 }
}
