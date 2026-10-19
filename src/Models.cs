// CROWN's ALBION v2.0 — data model and native interop.
// Target: .NET Framework 4.x / C# 5 compatible (stock csc.exe).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace CROWNsAlbion {

 // ---------------------------------------------------------------- catalogue

 public class Choice {
  public double value { get; set; }
  public string label { get; set; }
  public override string ToString() { return label + " (" + value + ")"; }
 }

 public class Parameter {
  public string id { get; set; }
  public string label { get; set; }
  public string type { get; set; }      // number | range | select
  public double min { get; set; }
  public double max { get; set; }
  public double step { get; set; }
  public double value { get; set; }
  public List<Choice> options { get; set; }
 }

 public class GameCommand {
  public string id { get; set; }
  public string title { get; set; }
  public string group { get; set; }
  public string summary { get; set; }
  public string template { get; set; }
  public string risk { get; set; }       // low | medium | high
  public string kind { get; set; }       // action | toggle
  public string state { get; set; }      // on | off | null
  public bool repeatable { get; set; }
  public string evidence { get; set; }   // debug-menu | reference | community
  public string coop { get; set; }       // safe | local | unverified | unsafe
  public string icon { get; set; }       // svg icon name
  public string twin { get; set; }       // opposite toggle command id
  public string notes { get; set; }
  public List<Parameter> @params { get; set; }
  public override string ToString() { return title; }

  public bool IsToggle { get { return string.Equals(kind, "toggle", StringComparison.OrdinalIgnoreCase); } }
  public bool IsHighRisk { get { return string.Equals(risk, "high", StringComparison.OrdinalIgnoreCase); } }
  public bool IsReferenceOnly { get { return string.Equals(evidence, "reference", StringComparison.OrdinalIgnoreCase); } }
 }

 public class Queued {
  public GameCommand cmd;
  public double? value;
  public Queued(GameCommand c, double? v) { cmd = c; value = v; }
 }

 public class StoredQueue { public string id { get; set; } public double? value { get; set; } }

 // ---------------------------------------------------------------- research

 public class ResearchRecord {
  public string kind { get; set; }
  public string category { get; set; }
  public string name { get; set; }
  public string raw { get; set; }
  public string preview { get; set; }
  public string source { get; set; }
  public string validation { get; set; }
  public string path { get; set; }
  public string offset { get; set; }
  public override string ToString() { return name; }
 }

 // ---------------------------------------------------------------- item vault

 public class VaultItem {
  public string id { get; set; }
  public string name { get; set; }
  public string category { get; set; }   // Outfits | Hair | Beards | Weapons | Potions | Keys | Gauntlets | Tools
  public string template { get; set; }   // Lua grant expression
  public string risk { get; set; }
  public string evidence { get; set; }
  public string note { get; set; }
  public string icon { get; set; }
  public override string ToString() { return name; }
 }

 // ---------------------------------------------------------------- settings

 public class Prefs {
  public List<string> Favourites { get; set; }
  public bool AllowRisk { get; set; }
  public bool Controller { get; set; }
  public bool CoopSafe { get; set; }
  public string GamePath { get; set; }
  public string LastCategory { get; set; }
  public int PulseMs { get; set; }
  public bool ConfirmDispatch { get; set; }
  public bool RestoreGameFocus { get; set; }
  public bool ShowReferenceCommands { get; set; }
  public string Hotkey { get; set; }
  public string Theme { get; set; }
  public string Profile { get; set; }

  public Prefs() {
   Favourites = new List<string>();
   GamePath = "";
   LastCategory = "All";
   CoopSafe = true;
   Controller = true;
   PulseMs = 5000;
   ConfirmDispatch = true;
   RestoreGameFocus = true;
   ShowReferenceCommands = true;
   Hotkey = "F8";
   Theme = "Albion";
   Profile = "Default";
  }
 }

 // Persistent, non-secret UI state kept separate from settings.
 public class AppState {
  public Dictionary<string, string> ToggleState { get; set; }   // toggle group -> on/off
  public Dictionary<string, string> LastSent { get; set; }      // command id -> ISO timestamp
  public List<string> Recents { get; set; }
  public string CoopWarningAccepted { get; set; }
  public AppState() { ToggleState = new Dictionary<string, string>(); LastSent = new Dictionary<string, string>(); Recents = new List<string>(); }
 }

 public class StoredProfile {
  public string name { get; set; }
  public List<string> favourites { get; set; }
  public List<StoredQueue> queue { get; set; }
  public List<string> quickToggles { get; set; }
  public string category { get; set; }
  public string note { get; set; }
 }

 // ---------------------------------------------------------------- interop

 public static class Native {
  public const int WM_HOTKEY = 0x0312;
  public const int WM_KEYDOWN = 0x0100;
  public const int WM_LBUTTONDOWN = 0x0201;

  [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint virtualKey);
  [DllImport("user32.dll", SetLastError = true)] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

  [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] public static extern uint XInputGetState14(uint idx, out XState state);
  [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")] public static extern uint XInputGetState13(uint idx, out XState state);
  [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")] public static extern uint XInputGetState9(uint idx, out XState state);

  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int left, top, right, bottom; }
  [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo { public int size; public Rect monitor, work; public uint flags; }
  [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
  [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
  [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr hwnd, int id);
  [DllImport("user32.dll")] public static extern IntPtr GetFocus();
  [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetClassName(IntPtr hwnd, StringBuilder text, int size);
  [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int cmd);
  [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string name);

  [StructLayout(LayoutKind.Sequential)] public struct XState { public uint packet; public XPad pad; }
  [StructLayout(LayoutKind.Sequential)] public struct XPad { public ushort buttons; public byte lt; public byte rt; public short lx; public short ly; public short rx; public short ry; }

  // XInput button masks.
  public const ushort PAD_DPAD_UP = 0x0001, PAD_DPAD_DOWN = 0x0002, PAD_DPAD_LEFT = 0x0004, PAD_DPAD_RIGHT = 0x0008;
  public const ushort PAD_START = 0x0010, PAD_BACK = 0x0020;
  public const ushort PAD_LTHUMB = 0x0040, PAD_RTHUMB = 0x0080;
  public const ushort PAD_LB = 0x0100, PAD_RB = 0x0200;
  public const ushort PAD_A = 0x1000, PAD_B = 0x2000, PAD_X = 0x4000, PAD_Y = 0x8000;
  public const ushort PAD_CHORD = PAD_LTHUMB | PAD_RTHUMB;

  public static uint XInputGetState(uint idx, out XState state) {
   try { return XInputGetState14(idx, out state); }
   catch (DllNotFoundException) {
    try { return XInputGetState13(idx, out state); }
    catch (DllNotFoundException) { return XInputGetState9(idx, out state); }
    catch (EntryPointNotFoundException) { return XInputGetState9(idx, out state); }
   }
   catch (EntryPointNotFoundException) {
    try { return XInputGetState13(idx, out state); }
    catch (DllNotFoundException) { return XInputGetState9(idx, out state); }
    catch (EntryPointNotFoundException) { return XInputGetState9(idx, out state); }
   }
  }
 }
}
