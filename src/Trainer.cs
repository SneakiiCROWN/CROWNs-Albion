// CROWN's ALBION v2.0 — engine core.
// Non-injecting external trainer. Writes auditable Lua payloads to the game's
// registered MyMod script path; never modifies Fable3.exe, xlive.dll, DLC or saves.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]

namespace CROWNsAlbion {

 // One input history shared by every window. No hooks, no XInputEnable calls.
 public class PadInput {
  public ushort Pressed; public bool Toggle, Back, Close;
  ushort last; long context = long.MinValue; bool armed = false, chordLatch = false;
  double repeatAt, bSince = -1, ySince = -1, startSince = -1; bool bDone = false, yDone = false, startDone = false;
  public void Reset() { context = long.MinValue; armed = false; last = 0; chordLatch = false; bSince = ySince = startSince = -1; bDone = yDone = startDone = false; }
  bool Hold(bool down, double now, ref double since, ref bool done) {
   if (!down) { since = -1; done = false; return false; }
   if (since < 0) since = now;
   if (!done && now - since >= 650) { done = true; return true; }
   return false;
  }
  public void Sample(ushort b, byte lt, byte rt, long window, double now) {
   Pressed = 0; Toggle = Back = Close = false;
   const ushort chord = 0x00C0;
   if ((b & chord) == chord && !chordLatch) { Toggle = true; chordLatch = true; armed = false; }
   if ((b & chord) == 0) chordLatch = false;
   if (window != context) { context = window; armed = false; bSince = ySince = startSince = -1; repeatAt = 0; bDone = yDone = startDone = false; }
   if (!armed) { last = b; if (b == 0 && lt < 180 && rt < 180) armed = true; return; }
   Pressed = (ushort)(b & ~last);
   ushort direction = (ushort)(b & 0x030F);
   if ((Pressed & 0x030F) != 0) repeatAt = now + 350;
   else if (direction != 0 && now >= repeatAt) { Pressed |= direction; repeatAt = now + 110; }
   bool bDown = (b & 0x2000) != 0;
   bool wasBDone = bDone;
   if (Hold(bDown, now, ref bSince, ref bDone)) Close = true;
   if (!bDown && (last & 0x2000) != 0 && !wasBDone) Back = true;
   Pressed &= 0x7FEF;
   if (Hold((b & 0x8000) != 0, now, ref ySince, ref yDone)) Pressed |= 0x8000;
   if (Hold((b & 0x0010) != 0, now, ref startSince, ref startDone)) Pressed |= 0x0010;
   if (bDown || Back || Close) Pressed = 0;
   last = b;
  }
 }

 public class PopupForm : Form {
  public bool CloseOnEscape = true;
  protected override bool ProcessCmdKey(ref Message msg, Keys key) {
   if (CloseOnEscape && key == Keys.Escape) { if (CancelButton != null) CancelButton.PerformClick(); else { DialogResult = DialogResult.Cancel; Close(); } return true; }
   if (key == Keys.Enter && AcceptButton != null && !(ActiveControl is TextBox)) { AcceptButton.PerformClick(); return true; }
   return base.ProcessCmdKey(ref msg, key);
  }
 }

 public class DiagRow { public string Label; public string Value; public string Status; public string Hint; }

 public partial class Trainer : Form {
  public const string VERSION = "2.0.0";
  public const string TITLE = "CROWN's ALBION";
  const string EXPECTED_HASH = "AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42";
  const string MARK = "-- CROWN_ALBION_MANAGED";
  const string SENTINEL = "CROWN_ALBION_DONE";
  const int HOTKEY_ID = 113;

  public readonly string root = AppDomain.CurrentDomain.BaseDirectory;
  public List<GameCommand> catalogue = new List<GameCommand>();
  public List<VaultItem> vault = new List<VaultItem>();
  public List<ResearchRecord> researchEntries = null;
  public Prefs prefs = new Prefs();
  public AppState state = new AppState();
  public string game = "", script = "", manifest = "", logPath = "";

  // UI
  public Label status;
  public TextBox search;
  public ComboBox sortBox;
  public ListBox categories, features;
  public Panel detailHost, queueHost;
  public FlowLayoutPanel queueFlow;
  public ToggleSwitch toggleRisk, toggleController, toggleCoop;
  public CheckBox showReference;
  public ListBox queueBox;
  public AlbionButton executeButton, favouriteButton, queueButton;
  public System.Windows.Forms.Timer clearTimer = new System.Windows.Forms.Timer();
  public System.Windows.Forms.Timer controllerTimer = new System.Windows.Forms.Timer();
  public System.Windows.Forms.Timer toastTimer = new System.Windows.Forms.Timer();
  public System.Windows.Forms.Timer diagTimer = new System.Windows.Forms.Timer();

  // Dispatch
  public string pulseText = ""; public bool pulsePending = false, pulseBusy = false; public DateTime pulseAt = DateTime.MinValue;
  public readonly PadInput padInput = new PadInput();
  public readonly List<Form> popupStack = new List<Form>();
  public int commonDialogDepth = 0;
  public IntPtr commonWindow = IntPtr.Zero;
  public bool hideRequested = false;
  public readonly Stopwatch padClock = Stopwatch.StartNew();
  public readonly Dictionary<IntPtr, Action<Native.XState, ushort>> padRoutes = new Dictionary<IntPtr, Action<Native.XState, ushort>>();
  public List<Queued> queue = new List<Queued>();
  public NotifyIcon tray = new NotifyIcon();
  public bool controllerUnavailable = false;
  public bool suspendStateSave = false;
  public string toastText = ""; public DateTime toastUntil = DateTime.MinValue; public Color toastColor = Albion.Text;
  public bool hotkeyRegistered = false;

  public string[] groupNames = { "All", "Favourites", "Recent", "Hero", "Combat", "Kingdom", "Inventory", "Appearance", "World", "Spawning", "Chaos", "Utilities" };

  public Trainer() {
   logPath = Path.Combine(root, "CROWN_Log.txt");
   Text = TITLE + " v" + VERSION + " — Fable III";
   Width = 1380; Height = 880; MinimumSize = new Size(1180, 740);
   StartPosition = FormStartPosition.CenterScreen;
   BackColor = Albion.Leather; ForeColor = Albion.Text; Font = Albion.Body;
   TopMost = false; Opacity = 1; KeyPreview = true;
   Icon = SystemIcons.Application;
   Albion.Init(root);

   LoadPrefs();
   LoadState();
   LoadCatalogue();
   LoadVault();
   game = GuessGamePath();

   BuildUI();
   UpdatePaths();
   PopulateCategories();
   RefreshFeatures();
   RefreshStatus();
   CheckLeftoverScript();

   KeyDown += OnGlobalKey;
   clearTimer.Interval = Math.Max(2500, prefs.PulseMs);
   clearTimer.Tick += delegate { ClearPulse(false); };
   controllerTimer.Interval = 40; controllerTimer.Tick += OnControllerTick; controllerTimer.Start();
   toastTimer.Interval = 60; toastTimer.Tick += delegate { if (toastText.Length > 0 && DateTime.UtcNow > toastUntil) { toastText = ""; if (detailHost != null) detailHost.Invalidate(); } }; toastTimer.Start();
   diagTimer.Interval = 3000; diagTimer.Tick += delegate { if (Visible && detailMode == "Diagnostics") RefreshStatus(); }; diagTimer.Start();

   FormClosing += delegate {
    if (pulsePending) ClearPulse(false);
    SavePrefs(); SaveState(); tray.Visible = false;
   };

   tray.Icon = SystemIcons.Application; tray.Text = TITLE + " (F8 to show/hide)"; tray.Visible = true;
   ContextMenu menu = new ContextMenu();
   menu.MenuItems.Add("Show / hide (F8)", delegate { Toggle(); });
   menu.MenuItems.Add("Exact gold…", delegate { ShowMenu(); OpenGoldPad(); });
   menu.MenuItems.Add("Clear running script", delegate { ClearPulse(true); });
   menu.MenuItems.Add("Diagnostics…", delegate { ShowMenu(); OpenDiagnostics(); });
   menu.MenuItems.Add("-");
   menu.MenuItems.Add("Quit", delegate { Close(); });
   tray.ContextMenu = menu; tray.DoubleClick += delegate { ShowMenu(); };
  }

  // ------------------------------------------------------------- data load

  public string DataPath(string name) {
   string[] candidates = {
    Path.Combine(root, name),
    Path.Combine(root, "data", name),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..")), "data", name),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..")), name)
   };
   foreach (string c in candidates) if (File.Exists(c)) return c;
   return Path.Combine(root, name);
  }

  void LoadPrefs() {
   try {
    string p = DataPath("settings.json");
    if (File.Exists(p)) {
     Prefs loaded = new JavaScriptSerializer().Deserialize<Prefs>(File.ReadAllText(p));
     if (loaded != null) prefs = loaded;
    }
   } catch { prefs = new Prefs(); }
   if (prefs.Favourites == null) prefs.Favourites = new List<string>();
   if (prefs.PulseMs < 2500) prefs.PulseMs = 5000;
   // Conservative upgrade: old settings without CoopSafe must default to protected.
   try {
    string p = DataPath("settings.json");
    if (!File.Exists(p) || File.ReadAllText(p).IndexOf("CoopSafe", StringComparison.Ordinal) < 0) prefs.CoopSafe = true;
   } catch { prefs.CoopSafe = true; }
  }

  void LoadState() {
   try {
    string p = DataPath("state.json");
    if (File.Exists(p)) {
     AppState loaded = new JavaScriptSerializer().Deserialize<AppState>(File.ReadAllText(p));
     if (loaded != null) state = loaded;
    }
   } catch { state = new AppState(); }
   if (state.ToggleState == null) state.ToggleState = new Dictionary<string, string>();
   if (state.LastSent == null) state.LastSent = new Dictionary<string, string>();
   if (state.Recents == null) state.Recents = new List<string>();
  }

  void LoadCatalogue() {
   try {
    string p = DataPath("catalog.json");
    var ser = new JavaScriptSerializer(); ser.MaxJsonLength = 20 * 1024 * 1024;
    List<GameCommand> loaded = ser.Deserialize<List<GameCommand>>(File.ReadAllText(p, Encoding.UTF8));
    if (loaded != null) catalogue = loaded;
    NormalizeCatalogue();
   } catch (Exception ex) {
    Log("Catalogue load failed: " + ex.Message);
    Notice("The command catalogue could not be loaded.\r\n\r\n" + ex.Message + "\r\n\r\nExpected: " + DataPath("catalog.json"), "CROWN's ALBION", MessageBoxButtons.OK, MessageBoxIcon.Error);
   }
  }

  void NormalizeCatalogue() {
   foreach (GameCommand c in catalogue) {
    if (c.@params == null) c.@params = new List<Parameter>();
    if (string.IsNullOrEmpty(c.risk)) c.risk = "medium";
    if (string.IsNullOrEmpty(c.kind)) c.kind = "action";
    if (string.IsNullOrEmpty(c.evidence)) c.evidence = "debug-menu";
    if (string.IsNullOrEmpty(c.coop)) c.coop = "unverified";
    // Derive missing twin ids from the -on/-off naming convention.
    if (c.IsToggle && string.IsNullOrEmpty(c.twin)) {
     string other = c.id.EndsWith("-on") ? c.id.Substring(0, c.id.Length - 3) + "-off"
                  : c.id.EndsWith("-off") ? c.id.Substring(0, c.id.Length - 4) + "-on" : "";
     if (other.Length > 0 && catalogue.Any(x => x.id == other)) c.twin = other;
    }
   }
  }

  void LoadVault() {
   try {
    string p = DataPath("item_vault.json");
    if (!File.Exists(p)) return;
    var ser = new JavaScriptSerializer(); ser.MaxJsonLength = 20 * 1024 * 1024;
    List<VaultItem> loaded = ser.Deserialize<List<VaultItem>>(File.ReadAllText(p, Encoding.UTF8));
    if (loaded != null) vault = loaded;
   } catch (Exception ex) { Log("Vault load failed: " + ex.Message); }
  }

  // ------------------------------------------------------------- persistence

  public void SavePrefs() {
   try {
    prefs.GamePath = game;
    if (toggleRisk != null) prefs.AllowRisk = toggleRisk.Checked;
    if (toggleController != null) prefs.Controller = toggleController.Checked;
    if (toggleCoop != null) prefs.CoopSafe = toggleCoop.Checked;
    if (showReference != null) prefs.ShowReferenceCommands = showReference.Checked;
    File.WriteAllText(Path.Combine(root, "settings.json"), new JavaScriptSerializer().Serialize(prefs), new UTF8Encoding(false));
   } catch (Exception ex) { Log("Settings save: " + ex.Message); }
  }

  public void SaveState() {
   if (suspendStateSave) return;
   try { File.WriteAllText(Path.Combine(root, "state.json"), new JavaScriptSerializer().Serialize(state), new UTF8Encoding(false)); }
   catch (Exception ex) { Log("State save: " + ex.Message); }
  }

  public void Log(string s) {
   try { File.AppendAllText(logPath, DateTime.Now.ToString("u") + " " + s + Environment.NewLine); } catch { }
  }

  // ------------------------------------------------------------- game paths

  public string GuessGamePath() {
   List<string> candidates = new List<string>();
   if (!string.IsNullOrEmpty(prefs.GamePath)) candidates.Add(prefs.GamePath);
   candidates.Add(Path.GetFullPath(Path.Combine(root, "..")));
   candidates.Add(root);
   candidates.Add(@"B:\Games\Fable 3");
   candidates.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Fable 3");
   candidates.Add(@"D:\SteamLibrary\steamapps\common\Fable 3");
   candidates.Add(@"E:\SteamLibrary\steamapps\common\Fable 3");
   candidates.Add(@"C:\Games\Fable 3");
   candidates.Add(@"D:\Games\Fable 3");
   foreach (string c in candidates) {
    try { if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, "Fable3.exe"))) return c; } catch { }
   }
   return candidates.Count > 0 ? candidates[0] : root;
  }

  public void UpdatePaths() {
   script = Path.Combine(game, "data", "scripts", "MyMod", "MyScript01.lua");
   manifest = Path.Combine(game, "data", "dir.manifest");
  }

  public bool ReadyScript() {
   if (!File.Exists(script) || !File.Exists(manifest)) return false;
   try { return File.ReadAllLines(manifest).Any(line => line.Trim().Equals(@"scripts\MyMod\MyScript01.lua", StringComparison.OrdinalIgnoreCase)); }
   catch { return false; }
  }

  public bool InjectorPresent() {
   try {
    string dlc = Path.Combine(game, "DLC", "10_ScriptInjector");
    return Directory.Exists(dlc) && (File.Exists(Path.Combine(dlc, "content.xbx")) || File.Exists(Path.Combine(dlc, "Content", "ScriptInjector.bnk.dat")));
   } catch { return false; }
  }

  public string GameProcessName = "Fable3";
  public bool GameRunning() { try { return Process.GetProcessesByName(GameProcessName).Length > 0; } catch { return false; } }

  // ------------------------------------------------------------- diagnostics

  public List<DiagRow> Diagnostics() {
   List<DiagRow> rows = new List<DiagRow>();
   string exe = Path.Combine(game, "Fable3.exe");
   string hash = "NOT FOUND"; string hstatus = "bad";
   if (File.Exists(exe)) {
    try { hash = SHA256File(exe); hstatus = hash.Equals(EXPECTED_HASH, StringComparison.OrdinalIgnoreCase) ? "ok" : "warn"; }
    catch { hash = "UNREADABLE"; }
   }
   rows.Add(new DiagRow { Label = "Fable3.exe", Value = File.Exists(exe) ? ShortHash(hash) + "  (" + game + ")" : "not found at " + game, Status = hstatus, Hint = hstatus == "ok" ? "Patch 2 checksum matches." : "Different build. Compatibility is not verified." });
   rows.Add(new DiagRow { Label = "Game running", Value = GameRunning() ? "yes" : "no", Status = GameRunning() ? "ok" : "idle", Hint = "Scripts are only dispatched while Fable III is running." });
   rows.Add(new DiagRow { Label = "Script injector", Value = InjectorPresent() ? "installed (DLC\\10_ScriptInjector)" : "not detected", Status = InjectorPresent() ? "ok" : "bad", Hint = "Improved Script Injector must be installed for scripts to execute." });
   rows.Add(new DiagRow { Label = "Script path", Value = ReadyScript() ? "ready" : "not prepared", Status = ReadyScript() ? "ok" : "warn", Hint = "Run PREPARE SCRIPT PATH once, with Fable III closed." });
   rows.Add(new DiagRow { Label = "MyScript01.lua", Value = ScriptStateText(), Status = ScriptStateStatus(), Hint = script });
   rows.Add(new DiagRow { Label = "XLive layer", Value = XLiveText(), Status = "info", Hint = "XLiveLessNess detected in the game folder." });
   rows.Add(new DiagRow { Label = "Co-op protection", Value = prefs.CoopSafe ? "ON" : "OFF", Status = prefs.CoopSafe ? "ok" : "warn", Hint = "Keep ON for XLLN sessions. Turn OFF only for disposable single-player saves." });
   rows.Add(new DiagRow { Label = "Pulse window", Value = prefs.PulseMs + " ms", Status = prefs.PulseMs >= 3500 ? "ok" : "warn", Hint = "Injector reads the script once per ~60 world frames." });
   rows.Add(new DiagRow { Label = "XInput", Value = controllerUnavailable ? "unavailable" : "available", Status = controllerUnavailable ? "warn" : "ok", Hint = "xinput1_4 / 1_3 / 9_1_0 are tried in order." });
   rows.Add(new DiagRow { Label = "Hotkey", Value = hotkeyRegistered ? (prefs.Hotkey + " registered") : (prefs.Hotkey + " not registered"), Status = hotkeyRegistered ? "ok" : "warn", Hint = "Another application may own this hotkey. Use the tray icon or HIDE button." });
   rows.Add(new DiagRow { Label = "Backups", Value = BackupCount() + " file(s)", Status = BackupCount() > 0 ? "ok" : "info", Hint = Path.Combine(root, "Backups") });
   rows.Add(new DiagRow { Label = "Catalogue", Value = catalogue.Count + " commands, " + catalogue.Count(c => c.IsReferenceOnly) + " reference-only", Status = "info", Hint = DataPath("catalog.json") });
   rows.Add(new DiagRow { Label = "Item vault", Value = vault.Count + " curated grants", Status = "info", Hint = DataPath("item_vault.json") });
   rows.Add(new DiagRow { Label = "Research library", Value = "3,229 records", Status = "info", Hint = DataPath("references.json") });
   return rows;
  }

  string ShortHash(string h) { return h.Length > 16 ? h.Substring(0, 16) + "…" : h; }

  string ScriptStateText() {
   try {
    if (!File.Exists(script)) return "missing";
    string now = File.ReadAllText(script).Trim();
    if (now.Length == 0) return "empty";
    if (now.StartsWith(MARK)) return now.IndexOf("-- GENERATED", StringComparison.Ordinal) >= 0 ? "active payload" : "managed / ready";
    return "owned by another mod";
   } catch { return "unreadable"; }
  }

  string ScriptStateStatus() {
   string s = ScriptStateText();
   if (s == "active payload") return "warn";
   if (s == "owned by another mod") return "bad";
   if (s == "missing" || s == "unreadable") return "warn";
   return "ok";
  }

  string XLiveText() {
   try {
    string dll = Path.Combine(game, "xlive.dll");
    if (!File.Exists(dll)) return "not present";
    FileVersionInfo vi = FileVersionInfo.GetVersionInfo(dll);
    return (vi.ProductName ?? "xlive.dll") + " " + (vi.FileVersion ?? "");
   } catch { return "present"; }
  }

  int BackupCount() { try { return Directory.Exists(Path.Combine(root, "Backups")) ? Directory.GetFiles(Path.Combine(root, "Backups")).Length : 0; } catch { return 0; } }

  // ------------------------------------------------------------- status/toast

  public void SetStatus(string s) { if (status != null) { status.Text = s; status.ForeColor = Albion.Muted; } }
  public void Toast(string s, Color color) { toastText = s; toastColor = color; toastUntil = DateTime.UtcNow.AddSeconds(4); if (detailHost != null) detailHost.Invalidate(); }

  public void Error(string s) {
   Log("ERROR: " + s);
   SetStatus("ERROR: " + s);
   Notice(s, "CROWN's ALBION", MessageBoxButtons.OK, MessageBoxIcon.Warning);
  }

  public void RefreshStatus() {
   string hash = "NOT FOUND"; string target = Path.Combine(game, "Fable3.exe");
   if (File.Exists(target)) {
    try { hash = SHA256File(target).Equals(EXPECTED_HASH, StringComparison.OrdinalIgnoreCase) ? "PATCH 2 OK" : "DIFFERENT EXE"; }
    catch { hash = "UNREADABLE"; }
   }
   string ready = ReadyScript() ? "SCRIPT READY" : "SCRIPT NOT PREPARED";
   SetStatus("Fable: " + hash + "  •  " + ready + "  •  Injector: " + (InjectorPresent() ? "detected" : "missing") + "  •  Co-op protection: " + (prefs.CoopSafe ? "ON" : "OFF") + "  •  " + catalogue.Count + " commands");
   RefreshHeaderChips();
  }

  // ------------------------------------------------------------- toggle state

  public string ToggleGroup(GameCommand c) {
   if (c == null) return "";
   if (!string.IsNullOrEmpty(c.twin)) return string.CompareOrdinal(c.id, c.twin) < 0 ? c.id + "|" + c.twin : c.twin + "|" + c.id;
   return c.id;
  }

  public string CurrentToggleState(GameCommand c) {
   if (c == null || !c.IsToggle) return null;
   string v;
   if (state.ToggleState.TryGetValue(ToggleGroup(c), out v)) return v;
   return null;
  }

  public void RecordApplied(GameCommand c) {
   if (c == null) return;
   if (c.IsToggle && !string.IsNullOrEmpty(c.state)) state.ToggleState[ToggleGroup(c)] = c.state;
   state.LastSent[c.id] = DateTime.UtcNow.ToString("u");
   state.Recents.Remove(c.id); state.Recents.Insert(0, c.id);
   while (state.Recents.Count > 25) state.Recents.RemoveAt(state.Recents.Count - 1);
   SaveState();
  }

  public string LastSentText(GameCommand c) {
   string v;
   if (state.LastSent.TryGetValue(c.id, out v)) {
    DateTime dt; if (DateTime.TryParse(v, out dt)) return "Last sent " + dt.ToString("HH:mm:ss");
    return "Last sent " + v;
   }
   return "Not sent this session";
  }

  // ------------------------------------------------------------- dispatch

  public string Compile(GameCommand c, double? overrideValue = null) {
   string output = c.template;
   if (c.@params != null && c.@params.Count > 0) {
    Parameter p = c.@params[0];
    double n = overrideValue.HasValue ? overrideValue.Value : ParameterValue(c);
    string v = n.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
    if (c.repeatable && p.id == "copies") { int copies = (int)n; return string.Join("\n", Enumerable.Repeat(output, copies)); }
    output = output.Replace("{" + p.id + "}", v);
   }
   if (output.IndexOf("{", StringComparison.Ordinal) >= 0) throw new Exception("Unresolved template parameter. Refusing to execute.");
   return output;
  }

  public string BuildPayload(List<GameCommand> cmds, string lua) {
   string unique = Guid.NewGuid().ToString("N");
   return MARK + "\n-- GENERATED " + DateTime.Now.ToString("u") + "\n-- Auto-cleared after " + prefs.PulseMs + " ms (best effort).\n" +
     SENTINEL + " = " + SENTINEL + " or {}\n" +
     "if not " + SENTINEL + "['" + unique + "'] then\n  " + SENTINEL + "['" + unique + "']=true\n" +
     string.Join("\n", lua.Split('\n').Select(x => "  " + x).ToArray()) + "\nend\n";
  }

  public string CompileQueuePreview() {
   try { return string.Join("\n", queue.Select(q => Compile(q.cmd, q.value)).ToArray()); }
   catch (Exception ex) { return "-- " + ex.Message; }
  }

  public bool Execute(List<GameCommand> cmds, string lua) {
   if (cmds == null || cmds.Count == 0) return false;
   if (cmds.Any(c => c.IsHighRisk) && !prefs.AllowRisk) {
    Error("High-risk commands are locked.\r\n\r\nEnable 'Allow high-risk / save-changing commands' only after backing up your saves and testing on a spare character.");
    return false;
   }
   if (prefs.CoopSafe && cmds.Any(c => c.id != "test-message")) {
    Error("CO-OP PROTECTION is ON. Gameplay modifications are blocked while protecting XLLN saves.\r\n\r\nDisable it only for isolated single-player testing on a disposable save.");
    return false;
   }
   if (pulseBusy || pulsePending) { Error("Another script is still active. Wait for the previous command to clear."); return false; }
   if (!ReadyScript()) { Error("Script path is not ready.\r\n\r\nUse PREPARE SCRIPT PATH first, and install the Improved Script Injector DLC before expecting game effects."); return false; }
   if (!GameRunning()) { Error("Fable III is not running. No script was dispatched: startup could otherwise execute old instructions unexpectedly."); return false; }
   if (!InjectorPresent()) {
    if (Notice("The Improved Script Injector DLC was not detected in DLC\\10_ScriptInjector.\r\nThe script will be written, but Fable III cannot execute it until the injector is installed.\r\n\r\nWrite the payload anyway?", "Injector not detected", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
   }

   bool extra = cmds.Any(c => c.IsHighRisk || c.repeatable || new[] { "gold", "treasury", "seals", "renown", "spawn-hobbes", "spawn-wolves", "spawn-renegades" }.Contains(c.id));
   if (extra || prefs.ConfirmDispatch) {
    string warn = "This writes a live Lua payload. The injector executes it approximately once per 60 world frames.\r\n\r\n" +
      "The auto-clear timer and Lua sentinel are best-effort, NOT guaranteed exactly-once.\r\n" +
      "It may alter a save, and co-op syncing is untested. Use a spare save.\r\n\r\n" +
      "Payload preview:\r\n" + Truncate(lua, 700) + "\r\n\r\nContinue?";
    if (Notice(warn, "CROWN — Confirm dispatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
   }

   string payload = BuildPayload(cmds, lua);
   try {
    string old = File.ReadAllText(script);
    if (old.Trim().Length > 0 && !old.TrimStart().StartsWith(MARK)) {
     Error("MyScript01.lua belongs to another mod. No changes were made.\r\n\r\nBack up or clear that file manually first.");
     return false;
    }
    BackupFile(script, "script");
    WriteAtomic(script, payload);
    string verify = File.ReadAllText(script);
    if (verify != payload) throw new Exception("Script verification failed; check backups.");
    pulsePending = true; pulseText = payload; pulseBusy = true; pulseAt = DateTime.UtcNow; clearTimer.Start();
    foreach (GameCommand c in cmds) RecordApplied(c);
    SetStatus("SENT: " + cmds.Count + " action(s); auto-clear in " + prefs.PulseMs + " ms. Not runtime-verified.");
    Log("Dispatched " + string.Join(",", cmds.Select(x => x.id).ToArray()));
    Toast("Sent " + cmds.Count + " action(s) to the game.", Albion.Good);
    if (prefs.RestoreGameFocus) {
     try {
      Process[] games = Process.GetProcessesByName(GameProcessName);
      if (games.Length > 0 && games[0].MainWindowHandle != IntPtr.Zero) { HideMenu(); Native.SetForegroundWindow(games[0].MainWindowHandle); }
     } catch (Exception ex) { Log("Return focus: " + ex.Message); }
    }
    if (detailHost != null) detailHost.Invalidate();
    return true;
   } catch (Exception ex) { Error(ex.Message); return false; }
  }

  static string Truncate(string s, int n) { if (s == null) return ""; return s.Length <= n ? s : s.Substring(0, n) + "…"; }

  public void RunSelected() {
   GameCommand c = Selected();
   if (c == null) return;
   GameCommand send = ResolveToggleSend(c);
   try { Execute(new List<GameCommand> { send }, Compile(send)); }
   catch (Exception ex) { Error(ex.Message); }
  }

  // If the selected command is part of a toggle pair, send the state the user chose
  // in the UI (the twin command). Otherwise send the selected command itself.
  public GameCommand ResolveToggleSend(GameCommand c) {
   if (c.IsToggle && toggleDesiredState != null) {
    string desired = toggleDesiredState;
    if (string.Equals(c.state, desired, StringComparison.OrdinalIgnoreCase)) return c;
    if (!string.IsNullOrEmpty(c.twin)) {
     GameCommand twin = catalogue.FirstOrDefault(x => x.id == c.twin);
     if (twin != null) return twin;
    }
   }
   return c;
  }

  public string toggleDesiredState = null; // set by the toggle switch in the detail pane

  public void ClearPulse() { ClearPulse(false); }

  public void ClearPulse(bool manual) {
   clearTimer.Stop();
   try {
    if (!ReadyScript()) { if (manual) SetStatus("No prepared script to clear."); return; }
    string now = File.ReadAllText(script);
    if (pulsePending) {
     if (now == pulseText) {
      BackupFile(script, "executed");
      WriteAtomic(script, MARK + "\n-- Ready\n");
      SetStatus("Script auto-cleared. In-game effects cannot be undone here.");
      Log("Auto-cleared pulse");
      Toast("Script cleared.", Albion.Muted);
     } else SetStatus("Script changed externally; auto-clear refused to overwrite someone else's edits.");
    } else if (manual) {
     if (now.Trim().Length > 0 && !now.TrimStart().StartsWith(MARK)) { Error("Non-CROWN script detected. Refusing to erase another mod."); return; }
     BackupFile(script, "manualclear");
     WriteAtomic(script, MARK + "\n-- Ready\n");
     SetStatus("CROWN script cleared. This does not undo in-game effects.");
     Toast("Script cleared.", Albion.Muted);
    }
   } catch (Exception ex) { Error("Could not clear script: " + ex.Message); }
   finally { pulsePending = false; pulseBusy = false; pulseText = ""; if (detailHost != null) detailHost.Invalidate(); }
  }

  // ------------------------------------------------------------- file safety

  public static void WriteAtomic(string path, string data) {
   string temp = path + ".crown.tmp";
   File.WriteAllText(temp, data, new UTF8Encoding(false));
   if (File.Exists(path)) { try { File.Replace(temp, path, null); } catch { File.Copy(temp, path, true); File.Delete(temp); } }
   else File.Move(temp, path);
  }

  public void BackupFile(string path, string category) {
   if (!File.Exists(path)) return;
   string dir = Path.Combine(root, "Backups"); Directory.CreateDirectory(dir);
   string dest = Path.Combine(dir, category + "-" + Path.GetFileName(path) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".bak");
   File.Copy(path, dest, false);
   if (!SHA256File(path).Equals(SHA256File(dest), StringComparison.OrdinalIgnoreCase)) throw new Exception("Backup hash mismatch. Original was not changed.");
  }

  public static string SHA256File(string path) {
   using (FileStream s = File.OpenRead(path)) using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "");
  }

  // ------------------------------------------------------------- prepare/locate

  public void Prepare() {
   UpdatePaths();
   if (GameRunning()) { Error("Close Fable III before preparing scripts or editing the manifest."); return; }
   if (!File.Exists(Path.Combine(game, "Fable3.exe"))) { Error("Choose a valid Fable III game folder first."); return; }
   string hash = SHA256File(Path.Combine(game, "Fable3.exe"));
   if (!hash.Equals(EXPECTED_HASH, StringComparison.OrdinalIgnoreCase)) {
    if (Notice("Fable3.exe checksum differs from the confirmed Patch 2 build.\r\n\r\nPrepare scripts anyway? Compatibility is NOT verified.", "Unexpected executable", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
   }
   string message = "This creates data\\scripts\\MyMod script placeholders and appends two manifest lines if needed, with backups.\r\n\r\n" +
     "It does NOT install the Improved Script Injector, and never changes Fable3.exe, DLC, xlive.dll, XLLN or Steam files.\r\n\r\nProceed?";
   if (Notice(message, "Prepare the non-destructive script path", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
   try {
    string data = Path.Combine(game, "data");
    if (!Directory.Exists(data)) throw new Exception("Missing data directory.");
    if (!File.Exists(manifest)) throw new Exception("Missing data\\dir.manifest. Refusing to create one from scratch.");
    string folder = Path.GetDirectoryName(script); Directory.CreateDirectory(folder);
    if (!File.Exists(script)) File.WriteAllText(script, MARK + "\n-- Ready\n", new UTF8Encoding(false));
    string fade = Path.Combine(folder, "MyScript02.lua");
    if (!File.Exists(fade)) File.WriteAllText(fade, MARK + "\n-- Empty fade script.\n", new UTF8Encoding(false));
    string current = File.ReadAllText(manifest); string updated = current;
    foreach (string line in new[] { @"scripts\MyMod\MyScript01.lua", @"scripts\MyMod\MyScript02.lua" })
     if (!current.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(s => s.Trim().Equals(line, StringComparison.OrdinalIgnoreCase)))
      updated += (updated.EndsWith("\n") ? "" : "\r\n") + line + "\r\n";
    if (updated != current) { BackupFile(manifest, "manifest"); WriteAtomic(manifest, updated); }
    RefreshStatus();
    Notice("Script paths prepared.\r\n\r\n" + (InjectorPresent() ? "Improved Script Injector detected." : "IMPORTANT: the Improved Script Injector was NOT detected. Install it separately for Fable III to execute these scripts.") + "\r\n\r\nTest on a disposable save. No co-op files were altered.", "Prepared", MessageBoxButtons.OK, MessageBoxIcon.Information);
   } catch (Exception ex) { Error("Prepare failed: " + ex.Message); }
  }

  public void Locate() {
   using (FolderBrowserDialog f = new FolderBrowserDialog()) {
    f.Description = "Select the folder containing Fable3.exe";
    f.SelectedPath = game;
    if (ShowCommon(f) == DialogResult.OK) { game = f.SelectedPath; UpdatePaths(); SavePrefs(); RefreshStatus(); if (detailHost != null) detailHost.Invalidate(); }
   }
  }

  void CheckLeftoverScript() {
   try {
    if (ReadyScript()) {
     string active = File.ReadAllText(script);
     if (active.StartsWith(MARK) && active.IndexOf("-- GENERATED", StringComparison.Ordinal) >= 0)
      SetStatus("WARNING: A previous CROWN command remains on disk. Use STOP / CLEAR LUA before launching Fable III.");
    }
   } catch { }
  }

  // ------------------------------------------------------------- favourites

  public void Favourite() {
   GameCommand c = Selected();
   if (c == null) return;
   if (prefs.Favourites.Contains(c.id)) prefs.Favourites.Remove(c.id); else prefs.Favourites.Add(c.id);
   SavePrefs();
   if (features != null) features.Invalidate();
   InvalidateDetail();
  }

  // ------------------------------------------------------------- queue

  public void QueueSelected() {
   GameCommand c = Selected();
   if (c == null) return;
   GameCommand send = ResolveToggleSend(c);
   try {
    double? n = send.@params != null && send.@params.Count > 0 ? (double?)ParameterValue(send) : null;
    if (queue.Count >= 8) { Error("Maximum eight commands per queue."); return; }
    if (send.id.StartsWith("unlocker:", StringComparison.Ordinal) && queue.Any(q => q.cmd.id == send.id)) { Error("Item grant is already queued. Repeating grants may duplicate items."); return; }
    if (queue.Any(q => q.cmd.id == send.id && q.value == n)) { Error("That exact command is already queued."); return; }
    queue.Add(new Queued(send, n)); RenderQueue();
    SetStatus("Queued: " + send.title + ". Review before running.");
    Toast("Queued: " + send.title, Albion.GoldBright);
   } catch (Exception ex) { Error(ex.Message); }
  }

  public void RenderQueue() {
   if (queueFlow == null) return;
   queueFlow.SuspendLayout();
   queueFlow.Controls.Clear();
   if (queue.Count == 0) {
    Label empty = new Label();
    empty.Text = "Queue is empty. Add commands with the + QUEUE button or the controller X button.";
    empty.ForeColor = Albion.Muted; empty.Font = Albion.Small; empty.AutoSize = false;
    empty.Height = 30; empty.Width = Math.Max(200, queueFlow.ClientSize.Width - 20); empty.TextAlign = ContentAlignment.MiddleLeft;
    queueFlow.Controls.Add(empty);
   } else {
    for (int i = 0; i < queue.Count; i++) {
     Queued q = queue[i];
     string text = (i + 1) + ". " + q.cmd.title + (q.value.HasValue ? "  =  " + q.value.Value.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture) : "");
     AlbionButton chip = new AlbionButton();
     chip.Text = text; chip.Style = AlbionButtonStyle.Quiet; chip.Font = Albion.Small; chip.Height = 28;
     chip.Width = Math.Min(430, 30 + TextRenderer.MeasureText(text, Albion.Small).Width + 24);
     int index = i;
     chip.Click += delegate { OpenQueueManager(); };
     queueFlow.Controls.Add(chip);
    }
   }
   queueFlow.ResumeLayout();
   InvalidateDetail();
  }

  public void RunQueue() {
   if (queue.Count == 0) { Notice("Queue is empty."); return; }
   if (queue.Count > 8) { Notice("Limit a single run to eight actions."); return; }
   try { if (Execute(queue.Select(q => q.cmd).ToList(), string.Join("\n", queue.Select(q => Compile(q.cmd, q.value)).ToArray()))) { queue.Clear(); RenderQueue(); } }
   catch (Exception ex) { Error(ex.Message); }
  }

  public void RemoveQueued() { int i = queueIndex; if (i < 0 || i >= queue.Count) return; queue.RemoveAt(i); RenderQueue(); }
  public void MoveQueued(int direction) { int i = queueIndex, j = i + direction; if (i < 0 || j < 0 || j >= queue.Count) return; Queued q = queue[i]; queue[i] = queue[j]; queue[j] = q; queueIndex = j; RenderQueue(); }
  public int queueIndex = 0;

  public void ClearQueue() { queue.Clear(); RenderQueue(); }

  // ------------------------------------------------------------- presets

  public string[] PresetNames = {
   "Preset: God mode ON", "Preset: God mode OFF", "Preset: Photo mode", "Preset: Normal view",
   "Preset: Midnight", "Preset: Dawn", "Preset: Combat chaos", "Preset: Restore combat"
  };

  public void LoadPreset(int index) {
   string[] ids;
   switch (index) {
    case 0: ids = new[] { "god-on", "autoheal-on" }; break;
    case 1: ids = new[] { "god-off", "autoheal-off" }; break;
    case 2: ids = new[] { "freecam-on", "fps-off" }; break;
    case 3: ids = new[] { "freecam-off", "fps-off" }; break;
    case 4: ids = new[] { "time" }; break;
    case 5: ids = new[] { "time" }; break;
    case 6: ids = new[] { "finisher-on", "finisher-combo", "finisher-angle", "finisher-crescendo" }; break;
    default: ids = new[] { "finisher-off", "enemy-off", "god-off" }; break;
   }
   queue.Clear();
   foreach (string id in ids) {
    GameCommand c = catalogue.FirstOrDefault(x => x.id == id);
    if (c == null) continue;
    double? v = c.@params != null && c.@params.Count > 0 ? (double?)((index == 4) ? 0 : (index == 5) ? 6 : c.@params[0].value) : null;
    queue.Add(new Queued(c, v));
   }
   RenderQueue();
   SetStatus("Preset loaded into the queue; inspect it before running.");
   Toast("Preset loaded. Review the queue before running.", Albion.GoldBright);
  }

  // ------------------------------------------------------------- profiles

  public string ProfilesDir { get { return Path.Combine(root, "profiles"); } }

  public string[] ListProfiles() {
   try {
    Directory.CreateDirectory(ProfilesDir);
    return Directory.GetFiles(ProfilesDir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();
   } catch { return new string[0]; }
  }

  public void SaveProfile(string name) {
   try {
    if (string.IsNullOrEmpty(name)) throw new Exception("Enter a profile name.");
    foreach (char bad in Path.GetInvalidFileNameChars()) name = name.Replace(bad, '_');
    Directory.CreateDirectory(ProfilesDir);
    StoredProfile p = new StoredProfile {
     name = name,
     favourites = new List<string>(prefs.Favourites),
     queue = queue.Select(q => new StoredQueue { id = q.cmd.id, value = q.value }).ToList(),
     quickToggles = state.ToggleState.Keys.ToList(),
     category = prefs.LastCategory,
     note = "Saved " + DateTime.Now.ToString("u") + " by CROWN's ALBION " + VERSION
    };
    File.WriteAllText(Path.Combine(ProfilesDir, name + ".json"), new JavaScriptSerializer().Serialize(p), new UTF8Encoding(false));
    prefs.Profile = name; SavePrefs();
    Toast("Profile saved: " + name, Albion.Good);
   } catch (Exception ex) { Error("Profile save: " + ex.Message); }
  }

  public void LoadProfile(string name) {
   try {
    string path = Path.Combine(ProfilesDir, name + ".json");
    if (!File.Exists(path)) throw new Exception("Profile not found.");
    StoredProfile p = new JavaScriptSerializer().Deserialize<StoredProfile>(File.ReadAllText(path));
    if (p == null) throw new Exception("Profile is empty.");
    prefs.Favourites = p.favourites != null ? p.favourites : new List<string>();
    if (!string.IsNullOrEmpty(p.category)) prefs.LastCategory = p.category;
    queue = new List<Queued>();
    if (p.queue != null) foreach (StoredQueue sq in p.queue) {
     GameCommand c = catalogue.FirstOrDefault(x => x.id == sq.id);
     if (c != null) queue.Add(new Queued(c, sq.value));
    }
    prefs.Profile = name; SavePrefs();
    PopulateCategories(); RefreshFeatures(); RenderQueue();
    Toast("Profile loaded: " + name, Albion.Good);
   } catch (Exception ex) { Error("Profile load: " + ex.Message); }
  }

  public void DeleteProfile(string name) {
   try {
    string path = Path.Combine(ProfilesDir, name + ".json");
    if (File.Exists(path)) File.Delete(path);
    Toast("Profile deleted: " + name, Albion.Muted);
   } catch (Exception ex) { Error("Profile delete: " + ex.Message); }
  }

  // ------------------------------------------------------------- hotkey / focus

  public void RegisterHotkey() {
   uint vk = (uint)Keys.F8;
   if (!string.IsNullOrEmpty(prefs.Hotkey)) {
    try { vk = (uint)new KeysConverter().ConvertFromString(prefs.Hotkey); }
    catch { vk = (uint)Keys.F8; }
   }
   hotkeyRegistered = Native.RegisterHotKey(Handle, HOTKEY_ID, 0, vk);
   if (!hotkeyRegistered) { Log(prefs.Hotkey + " global hotkey registration failed"); SetStatus("WARNING: " + prefs.Hotkey + " hotkey is owned by another application. Use the tray icon or HIDE button."); }
  }

  protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); RegisterHotkey(); }

  protected override void OnHandleDestroyed(EventArgs e) { Native.UnregisterHotKey(Handle, HOTKEY_ID); base.OnHandleDestroyed(e); }

  protected override void WndProc(ref Message m) {
   if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) { ToggleFromGameHotkey(); return; }
   base.WndProc(ref m);
  }

  public bool IsGameForeground() {
   try {
    IntPtr fg = Native.GetForegroundWindow(); if (fg == IntPtr.Zero) return false;
    uint pid; Native.GetWindowThreadProcessId(fg, out pid);
    foreach (Process proc in Process.GetProcessesByName(GameProcessName)) if (proc.Id == (int)pid) return true;
   } catch { }
   return false;
  }

  public bool IsOwnForeground(IntPtr fg) { uint pid; Native.GetWindowThreadProcessId(fg, out pid); return pid == (uint)Process.GetCurrentProcess().Id; }

  public bool IsMenuForeground() {
   IntPtr fg = Native.GetForegroundWindow(); if (IsOwnForeground(fg)) return true;
   foreach (Form child in OwnedForms) if (!child.IsDisposed && child.Handle == fg) return true;
   return false;
  }

  public void RestoreGameFocus() {
   try { foreach (Process proc in Process.GetProcessesByName(GameProcessName)) if (proc.MainWindowHandle != IntPtr.Zero) { Native.SetForegroundWindow(proc.MainWindowHandle); return; } }
   catch (Exception e) { Log("Return focus: " + e.Message); }
  }

  public void ShowMenu() {
   if (!Visible) Show();
   WindowState = FormWindowState.Normal; TopMost = false;
   if (commonDialogDepth > 0 && commonWindow != IntPtr.Zero) { Native.SetForegroundWindow(commonWindow); return; }
   Form target = PopupOwner();
   if (target.Visible && target.Enabled) { target.Activate(); target.BringToFront(); if (target == this && features != null) features.Focus(); }
  }

  public void ToggleFromGameHotkey() {
   if (!Visible && !GameRunning()) { ShowMenu(); return; }
   if (!IsGameForeground() && !IsMenuForeground()) return;
   Toggle();
  }

  public void Toggle() { if (Visible) HideMenu(); else ShowMenu(); }

  public void HideMenu() {
   Log("Menu hide requested; custom dialogs=" + popupStack.Count + ", common dialogs=" + commonDialogDepth);
   hideRequested = true; padInput.Reset();
   if (commonDialogDepth > 0) { IntPtr fg = Native.GetForegroundWindow(); if (IsOwnForeground(fg)) NativeDialogButton(fg, false); return; }
   if (popupStack.Count > 0) { CancelPopup(popupStack[popupStack.Count - 1]); return; }
   FinishHide();
  }

  public void FinishHide() {
   if (!hideRequested) return;
   if (commonDialogDepth > 0 || popupStack.Count > 0) { BeginInvoke(new Action(HideMenu)); return; }
   bool restore = IsMenuForeground(); hideRequested = false; Hide(); if (restore && prefs.RestoreGameFocus) RestoreGameFocus();
  }

  // ------------------------------------------------------------- modal lifecycle

  public Form PopupOwner() { return popupStack.Count > 0 ? popupStack[popupStack.Count - 1] : this; }

  public void CancelPopup(Form form) {
   if (form == null || form.IsDisposed) return;
   if (form.CancelButton != null) form.CancelButton.PerformClick();
   else { form.DialogResult = DialogResult.Cancel; form.Close(); }
  }

  public void RestorePopupFocus(Form owner, Control previous) {
   padInput.Reset();
   if (owner == null || owner.IsDisposed || !owner.Visible || !owner.Enabled || hideRequested) return;
   if (IsOwnForeground(Native.GetForegroundWindow())) { owner.Activate(); if (previous != null && !previous.IsDisposed && previous.CanFocus) previous.Focus(); }
  }

  public DialogResult ShowPopup(Form form) {
   Form owner = PopupOwner(); Control previous = owner.ActiveControl;
   form.Owner = owner; form.TopMost = false; form.StartPosition = FormStartPosition.CenterParent;
   form.ShowInTaskbar = false; form.KeyPreview = true;
   popupStack.Add(form); padInput.Reset();
   Log("Popup open: " + form.Text + "; owner=" + owner.Text);
   form.Shown += delegate { form.Activate(); form.BringToFront(); };
   try { return form.ShowDialog(owner); }
   finally { popupStack.Remove(form); form.Dispose(); RestorePopupFocus(owner, previous); FinishHide(); Log("Popup closed; owner enabled=" + owner.Enabled + ", visible=" + owner.Visible); }
  }

  public DialogResult Notice(string message) { return Notice(message, "CROWN's ALBION", MessageBoxButtons.OK, MessageBoxIcon.Information); }
  public DialogResult Notice(string message, string title) { return Notice(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information); }

  public DialogResult Notice(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon) {
   PopupForm dialog = new PopupForm();
   dialog.Text = title; dialog.BackColor = Albion.Leather; dialog.ForeColor = Albion.Text; dialog.Font = Albion.Body;
   int textHeight = Math.Max(90, Math.Min(440, TextRenderer.MeasureText(message, Albion.Body, new Size(620, 0), TextFormatFlags.WordBreak).Height + 34));
   dialog.ClientSize = new Size(660, textHeight + 96); dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
   dialog.MaximizeBox = false; dialog.MinimizeBox = false;
   TextBox content = new TextBox();
   content.Text = message; content.Multiline = true; content.ReadOnly = true;
   content.ScrollBars = ScrollBars.Vertical; content.BorderStyle = BorderStyle.None;
   content.BackColor = Albion.Leather; content.ForeColor = Albion.Text;
   content.SetBounds(24, 20, 612, textHeight); content.TabStop = false; dialog.Controls.Add(content);
   bool yesNo = buttons == MessageBoxButtons.YesNo;
   AlbionButton ok = new AlbionButton();
   ok.Text = yesNo ? "YES / CONFIRM" : "OK"; ok.Style = AlbionButtonStyle.Primary;
   ok.SetBounds(yesNo ? 356 : 490, textHeight + 32, 146, 42);
   ok.Click += delegate { dialog.DialogResult = yesNo ? DialogResult.Yes : DialogResult.OK; dialog.Close(); };
   dialog.Controls.Add(ok); dialog.AcceptButton = ok;
   if (yesNo) {
    AlbionButton no = new AlbionButton(); no.Text = "NO / CANCEL"; no.Style = AlbionButtonStyle.Ghost;
    no.SetBounds(498, textHeight + 32, 146, 42);
    no.Click += delegate { dialog.DialogResult = DialogResult.No; dialog.Close(); };
    dialog.Controls.Add(no); dialog.CancelButton = no;
   } else dialog.CancelButton = ok;
   RegisterPad(dialog, delegate(Native.XState s, ushort press) {
    if ((press & 0x000F) != 0) dialog.SelectNextControl(dialog.ActiveControl, (press & 0x000A) != 0, true, true, true);
    if ((press & Native.PAD_A) != 0) { Button selected = dialog.ActiveControl as Button; if (selected != null) selected.PerformClick(); else dialog.AcceptButton.PerformClick(); }
   });
   dialog.Shown += delegate { ok.Focus(); };
   return ShowPopup(dialog);
  }

  public DialogResult ShowCommon(CommonDialog dialog) {
   Form owner = PopupOwner(); Control previous = owner.ActiveControl; commonDialogDepth++; padInput.Reset();
   using (System.Windows.Forms.Timer center = new System.Windows.Forms.Timer()) {
    IntPtr positioned = IntPtr.Zero; DateTime positionUntil = DateTime.MinValue; center.Interval = 40;
    center.Tick += delegate {
     IntPtr fg = Native.GetForegroundWindow();
     if (hideRequested) { if (IsOwnForeground(fg)) NativeDialogButton(fg, false); return; }
     if (fg == owner.Handle || !IsOwnForeground(fg)) return;
     StringBuilder cls = new StringBuilder(64); Native.GetClassName(fg, cls, 64); if (cls.ToString() != "#32770") return;
     if (fg != positioned) { positioned = fg; positionUntil = DateTime.UtcNow.AddMilliseconds(750); }
     if (DateTime.UtcNow > positionUntil) return;
     Native.Rect bounds, parent;
     if (!Native.GetWindowRect(fg, out bounds) || !Native.GetWindowRect(owner.Handle, out parent)) return;
     Native.MonitorInfo monitor = new Native.MonitorInfo(); monitor.size = Marshal.SizeOf(monitor);
     if (!Native.GetMonitorInfo(Native.MonitorFromWindow(owner.Handle, 2), ref monitor)) return;
     Native.Rect area = monitor.work;
     int width = bounds.right - bounds.left, height = bounds.bottom - bounds.top;
     int x = Math.Max(area.left, Math.Min(area.right - width, parent.left + (parent.right - parent.left - width) / 2));
     int y = Math.Max(area.top, Math.Min(area.bottom - height, parent.top + (parent.bottom - parent.top - height) / 2));
     Native.SetWindowPos(fg, IntPtr.Zero, x, y, 0, 0, 0x0015); positioned = fg; commonWindow = fg;
    };
    center.Start();
    try { return dialog.ShowDialog(owner); }
    finally { center.Stop(); commonWindow = IntPtr.Zero; commonDialogDepth--; RestorePopupFocus(owner, previous); FinishHide(); }
   }
  }

  public void RegisterPad(Form form, Action<Native.XState, ushort> route) {
   form.Owner = PopupOwner();
   IntPtr registered = IntPtr.Zero;
   EventHandler add = delegate { registered = form.Handle; padRoutes[registered] = route; };
   form.HandleCreated += add;
   if (form.IsHandleCreated) add(form, EventArgs.Empty);
   form.HandleDestroyed += delegate { padRoutes.Remove(registered); };
   form.FormClosed += delegate { padRoutes.Remove(registered); };
  }

  public void NativeDialogButton(IntPtr fg, bool confirm) {
   StringBuilder cls = new StringBuilder(64); Native.GetClassName(fg, cls, 64);
   if (cls.ToString() != "#32770") return;
   int id = Native.GetDlgCtrlID(Native.GetFocus());
   if (confirm) { if (id != 1 && id != 2 && id != 6 && id != 7) id = Native.GetDlgItem(fg, 6) != IntPtr.Zero ? 6 : 1; }
   else id = Native.GetDlgItem(fg, 2) != IntPtr.Zero ? 2 : Native.GetDlgItem(fg, 7) != IntPtr.Zero ? 7 : 1;
   if (Native.GetDlgItem(fg, id) != IntPtr.Zero) Native.PostMessage(fg, 0x0111, new IntPtr(id), IntPtr.Zero);
  }

  // ------------------------------------------------------------- controller

  void OnControllerTick(object sender, EventArgs e) {
   if (toggleController == null || !toggleController.Checked) { padInput.Reset(); return; }
   try {
    Native.XState st;
    if (Native.XInputGetState(0, out st) != 0) { padInput.Reset(); return; }
    controllerUnavailable = false;
    IntPtr fg = Native.GetForegroundWindow(); bool own = IsOwnForeground(fg);
    if (!own && !IsGameForeground()) { padInput.Reset(); return; }
    padInput.Sample(st.pad.buttons, st.pad.lt, st.pad.rt, fg.ToInt64(), padClock.Elapsed.TotalMilliseconds);
    if (padInput.Toggle) { if (Visible) HideMenu(); else ShowMenu(); return; }
    if (!own || !Visible) return;
    if (padInput.Close) { HideMenu(); return; }
    if (padInput.Back) { ControllerBack(fg); return; }
    ushort pressed = padInput.Pressed;
    Action<Native.XState, ushort> route;
    if (padRoutes.TryGetValue(fg, out route)) { route(st, pressed); return; }
    if (fg != Handle) {
     if ((pressed & 0x000F) != 0) Native.PostMessage(fg, Native.WM_KEYDOWN, new IntPtr((pressed & 0x0005) != 0 ? 0x25 : 0x27), IntPtr.Zero);
     if ((pressed & Native.PAD_A) != 0) NativeDialogButton(fg, true);
     return;
    }
    if (!Visible || Form.ActiveForm != this || Native.GetForegroundWindow() != Handle) return;
    if ((pressed & Native.PAD_DPAD_UP) != 0 && features.Items.Count > 0) { features.SelectedIndex = Math.Max(0, features.SelectedIndex - 1); features.Focus(); }
    if ((pressed & Native.PAD_DPAD_DOWN) != 0 && features.Items.Count > 0) { features.SelectedIndex = Math.Min(features.Items.Count - 1, features.SelectedIndex + 1); features.Focus(); }
    if ((pressed & Native.PAD_DPAD_LEFT) != 0) { categories.SelectedIndex = Math.Max(0, categories.SelectedIndex - 1); features.Focus(); }
    if ((pressed & Native.PAD_DPAD_RIGHT) != 0) { categories.SelectedIndex = Math.Min(categories.Items.Count - 1, categories.SelectedIndex + 1); features.Focus(); }
    int multiplier = st.pad.rt >= 180 ? 100 : st.pad.lt >= 180 ? 10 : 1;
    if ((pressed & Native.PAD_LB) != 0) AdjustSelectedValue(-multiplier);
    if ((pressed & Native.PAD_RB) != 0) AdjustSelectedValue(multiplier);
    if ((pressed & Native.PAD_A) != 0) {
     GameCommand c = Selected();
     if (c != null && c.IsToggle) { toggleDesiredState = string.Equals(CurrentToggleState(c), "on", StringComparison.OrdinalIgnoreCase) ? "off" : "on"; InvalidateDetail(); RunSelected(); return; }
     if (c != null && c.@params.Count > 0 && c.@params[0].type != "select" && c.@params[0].step >= 1) OpenAmountPad();
     else QueueSelected();
     return;
    }
    if ((pressed & Native.PAD_BACK) != 0) { OpenQueueManager(); return; }
    if ((pressed & Native.PAD_X) != 0) Favourite();
    if ((pressed & Native.PAD_Y) != 0) { RunSelected(); return; }
    if ((pressed & Native.PAD_START) != 0) { RunQueue(); return; }
   } catch (DllNotFoundException) { controllerTimer.Stop(); controllerUnavailable = true; SetStatus("XInput unavailable; use keyboard or mouse."); }
   catch (EntryPointNotFoundException) { controllerUnavailable = true; SetStatus("XInput function unavailable."); }
   catch (Exception ex) { Log("Controller: " + ex.Message); }
  }

  void ControllerBack(IntPtr fg) {
   Form active = Form.FromHandle(fg) as Form;
   if (active != null && active != this) { CancelPopup(active); return; }
   if (fg != Handle) { NativeDialogButton(fg, false); return; }
   if (search != null && search.Text.Length > 0) { search.Clear(); features.Focus(); return; }
   if (categories.SelectedIndex != 0) { categories.SelectedIndex = 0; features.Focus(); return; }
   HideMenu();
  }

  void OnGlobalKey(object sender, KeyEventArgs e) {
   if (popupStack.Count > 0 || commonDialogDepth > 0) {
    if (e.KeyCode == Keys.Escape) { if (commonDialogDepth > 0) NativeDialogButton(Native.GetForegroundWindow(), false); else CancelPopup(PopupOwner()); }
    e.Handled = true; e.SuppressKeyPress = true; return;
   }
   if (e.KeyCode == Keys.Escape) { HideMenu(); e.Handled = true; e.SuppressKeyPress = true; return; }
   if (e.KeyCode == Keys.F1) { ShowHelp(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.F) { search.Focus(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.G) { OpenGoldPad(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.Q) { OpenQueueManager(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.Enter) { RunQueue(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.D) { OpenDiagnostics(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.L) { OpenResearchLibrary(); e.Handled = true; return; }
   if (e.Control && e.KeyCode == Keys.I) { OpenItemVault(); e.Handled = true; return; }
   if (e.KeyCode == Keys.Enter && features.Focused) { QueueSelected(); e.Handled = true; return; }
  }

  protected override bool ProcessCmdKey(ref Message msg, Keys key) {
   if (popupStack.Count > 0 || commonDialogDepth > 0) {
    if (commonDialogDepth > 0) { if (key == Keys.Escape || key == Keys.Enter) NativeDialogButton(Native.GetForegroundWindow(), key == Keys.Enter); }
    else if (key == Keys.Escape) CancelPopup(PopupOwner());
    else if (key == Keys.Enter && PopupOwner().AcceptButton != null && !(PopupOwner().ActiveControl is TextBox)) PopupOwner().AcceptButton.PerformClick();
    return true;
   }
   return base.ProcessCmdKey(ref msg, key);
  }

  [STAThread]
  public static void Main() {
   Application.EnableVisualStyles();
   Application.SetCompatibleTextRenderingDefault(false);
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
   Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { CrashLog("ThreadException", e.Exception); };
   AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { CrashLog("UnhandledException", e.ExceptionObject as Exception); };
   try { Application.Run(new Trainer()); }
   catch (Exception ex) { CrashLog("Startup", ex); }
  }

  public static void CrashLog(string stage, Exception ex) {
   try {
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CROWN_Crash.txt");
    File.AppendAllText(path, DateTime.Now.ToString("u") + "  [" + stage + "]" + Environment.NewLine + (ex == null ? "(no exception object)" : ex.ToString()) + Environment.NewLine + Environment.NewLine);
   } catch { }
  }
 }
}
