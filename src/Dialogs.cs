// CROWN's ALBION v2.0 — modal dialogs (queue, vault, library, diagnostics, profiles, settings).
// Every dialog is non-destructive: it edits the queue, copies text or changes settings.
// Only the main window's EXECUTE / RUN QUEUE writes a Lua payload.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CROWNsAlbion {

 public partial class Trainer {

  // ---------------------------------------------------------------- helpers

  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
   PopupForm d = new PopupForm();
   d.Text = title;
   d.ClientSize = new Size(width, height);
   d.BackColor = Albion.Leather; d.ForeColor = Albion.Text; d.Font = Albion.Body;
   d.KeyPreview = true; d.ShowInTaskbar = false;
   d.StartPosition = FormStartPosition.CenterParent;
   if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }
   return d;
  }

  ListBox ThemedList(int x, int y, int w, int h) {
   ListBox l = new ListBox();
   l.SetBounds(x, y, w, h);
   l.IntegralHeight = false;
   l.BackColor = Albion.PanelHi; l.ForeColor = Albion.Text;
   l.BorderStyle = BorderStyle.FixedSingle;
   l.Font = Albion.Body;
   return l;
  }

  TextBox ThemedText(int x, int y, int w, int h, Font font) {
   TextBox t = new TextBox();
   t.SetBounds(x, y, w, h);
   t.BackColor = Albion.PanelHi; t.ForeColor = Albion.Text;
   t.BorderStyle = BorderStyle.FixedSingle;
   t.Font = font ?? Albion.Body;
   return t;
  }

  static Color DiagColor(string status) {
   if (status == "ok") return Albion.Good;
   if (status == "warn") return Albion.Gold;
   if (status == "bad") return Albion.Danger;
   if (status == "info") return Albion.Info;
   return Albion.Muted;
  }

  // Queue a prepared command that is not currently selected in the feature list.
  public bool QueueCommand(GameCommand cmd, double? value) {
   if (cmd == null) return false;
   if (queue.Count >= 8) { Error("Maximum eight commands per queue."); return false; }
   if ((cmd.id.StartsWith("unlocker:", StringComparison.Ordinal) || cmd.id.StartsWith("vault:", StringComparison.Ordinal)) && queue.Any(q => q.cmd.id == cmd.id)) {
    Error("That grant is already queued. Repeating grants may duplicate items.");
    return false;
   }
   if (queue.Any(q => q.cmd.id == cmd.id && q.value == value)) { Error("That exact command is already queued."); return false; }
   queue.Add(new Queued(cmd, value));
   RenderQueue();
   SetStatus("Queued: " + cmd.title + ". Review before running.");
   Toast("Queued: " + cmd.title, Albion.GoldBright);
   return true;
  }

  // ---------------------------------------------------------------- queue manager

  public void OpenQueueManager() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   PopupForm d = NewDialog("CROWN — Queue review (nothing runs here)", 740, 560, true);
   d.Controls.Add(MakeLabel("REVIEW YOUR QUEUE", Albion.H1, Albion.GoldBright, 24, 14, 620, 34));
   d.Controls.Add(MakeLabel("Check values before pressing RUN QUEUE in the main window. This view never dispatches anything.", Albion.Small, Albion.Muted, 24, 50, 690, 22));

   ListBox list = ThemedList(24, 82, 692, 268);
   list.DrawMode = DrawMode.OwnerDrawFixed;
   list.ItemHeight = 32;
   list.DrawItem += DrawQueueRow;
   d.Controls.Add(list);

   Label count = MakeLabel("", Albion.Small, Albion.Muted, 24, 352, 690, 22);
   d.Controls.Add(count);

   Action refresh = delegate {
    int oldIndex = list.SelectedIndex;
    list.BeginUpdate();
    list.Items.Clear();
    for (int i = 0; i < queue.Count; i++) list.Items.Add(queue[i]);
    list.EndUpdate();
    if (queue.Count > 0) list.SelectedIndex = Math.Max(0, Math.Min(queue.Count - 1, oldIndex));
    count.Text = queue.Count + " / 8 actions  •  co-op protection " + (prefs.CoopSafe ? "ON" : "OFF") + "  •  nothing executes from this window";
   };

   list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };

   AlbionButton up = new AlbionButton(); up.Text = "↑ UP"; up.Style = AlbionButtonStyle.Ghost;
   up.SetBounds(24, 380, 96, 36);
   up.Click += delegate { queueIndex = list.SelectedIndex; MoveQueued(-1); refresh(); };
   d.Controls.Add(up);

   AlbionButton down = new AlbionButton(); down.Text = "↓ DOWN"; down.Style = AlbionButtonStyle.Ghost;
   down.SetBounds(128, 380, 96, 36);
   down.Click += delegate { queueIndex = list.SelectedIndex; MoveQueued(1); refresh(); };
   d.Controls.Add(down);

   AlbionButton edit = new AlbionButton(); edit.Text = "EDIT VALUE"; edit.Style = AlbionButtonStyle.Gold;
   edit.SetBounds(232, 380, 132, 36);
   edit.Click += delegate { queueIndex = list.SelectedIndex; EditQueued(); };
   d.Controls.Add(edit);

   AlbionButton update = new AlbionButton(); update.Text = "STORE EDIT"; update.Style = AlbionButtonStyle.Gold;
   update.SetBounds(372, 380, 132, 36);
   update.Click += delegate { queueIndex = list.SelectedIndex; UpdateQueued(); refresh(); };
   d.Controls.Add(update);

   AlbionButton remove = new AlbionButton(); remove.Text = "REMOVE"; remove.Style = AlbionButtonStyle.Quiet;
   remove.SetBounds(512, 380, 100, 36);
   remove.Click += delegate { queueIndex = list.SelectedIndex; RemoveQueued(); refresh(); };
   d.Controls.Add(remove);

   AlbionButton clear = new AlbionButton(); clear.Text = "CLEAR ALL"; clear.Style = AlbionButtonStyle.Danger;
   clear.SetBounds(620, 380, 96, 36);
   clear.Click += delegate {
    if (queue.Count == 0) return;
    if (Notice("Clear every queued action? Nothing is executed by this dialog.", "CROWN", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
    ClearQueue(); refresh();
   };
   d.Controls.Add(clear);

   d.Controls.Add(MakeLabel("PAD: D-pad select  •  A edit  •  X remove  •  Y up  •  Start down  •  B back", Albion.Tiny, Albion.Muted, 24, 428, 690, 22));

   AlbionButton done = new AlbionButton(); done.Text = "DONE / RETURN"; done.Style = AlbionButtonStyle.Primary;
   done.SetBounds(488, 462, 228, 42);
   done.Click += delegate { d.Close(); };
   d.Controls.Add(done);

   d.AcceptButton = edit; d.CancelButton = done;
   list.DoubleClick += delegate { edit.PerformClick(); };

   RegisterPad(d, delegate(Native.XState st, ushort press) {
    if ((press & Native.PAD_DPAD_UP) != 0 && list.Items.Count > 0) list.SelectedIndex = Math.Max(0, list.SelectedIndex - 1);
    if ((press & Native.PAD_DPAD_DOWN) != 0 && list.Items.Count > 0) list.SelectedIndex = Math.Min(list.Items.Count - 1, list.SelectedIndex + 1);
    if ((press & Native.PAD_A) != 0) { edit.PerformClick(); return; }
    if ((press & Native.PAD_X) != 0) remove.PerformClick();
    if ((press & Native.PAD_Y) != 0) up.PerformClick();
    if ((press & Native.PAD_START) != 0) down.PerformClick();
   });

   refresh();
   ShowPopup(d);
  }

  void DrawQueueRow(object sender, DrawItemEventArgs e) {
   ListBox lb = sender as ListBox;
   if (lb == null || e.Index < 0 || e.Index >= queue.Count) return;
   bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
   ListPaint.FillRow(e.Graphics, e.Bounds, selected, false, Albion.PanelHi);
   Queued q = queue[e.Index];
   string text = (e.Index + 1) + ".  " + q.cmd.title;
   Albion.Draw(e.Graphics, text, selected ? Albion.BodyBold : Albion.Body, selected ? Albion.GoldBright : Albion.Text,
    new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 200, e.Bounds.Height), ContentAlignment.MiddleLeft);
   string val = q.value.HasValue ? q.value.Value.ToString("N0", CultureInfo.InvariantCulture) : "—";
   Albion.Draw(e.Graphics, val, Albion.SmallBold, Albion.Gold,
    new Rectangle(e.Bounds.Right - 130, e.Bounds.Y, 118, e.Bounds.Height), ContentAlignment.MiddleRight);
  }

  void EditQueued() {
   int i = queueIndex;
   if (i < 0 || i >= queue.Count) return;
   Queued q = queue[i];
   if (!q.value.HasValue) { SetStatus("This queued command has no numeric parameter; remove or reorder it instead."); return; }
   GameCommand target = catalogue.FirstOrDefault(x => x.id == q.cmd.id);
   if (target != null) {
    int ci = Array.IndexOf(groupNames, target.group);
    if (ci >= 0 && categories != null) categories.SelectedIndex = ci;
    RefreshFeatures();
    if (features.Items.Contains(target)) features.SelectedItem = target;
   }
   if (target != null && target.@params != null && target.@params.Count > 0 && target.@params[0].type == "select") {
    for (int j = 0; j < selectBox.Items.Count; j++) if (((Choice)selectBox.Items[j]).value == q.value.Value) { selectBox.SelectedIndex = j; break; }
   } else if (numericText != null) {
    numericText.Text = q.value.Value.ToString("0.#####", CultureInfo.InvariantCulture);
   }
   InvalidateDetail();
   SetStatus("Queued value loaded into the editor. Adjust it, then press STORE EDIT in the queue window.");
  }

  void UpdateQueued() {
   int i = queueIndex;
   if (i < 0 || i >= queue.Count) return;
   Queued q = queue[i];
   if (q.cmd.@params == null || q.cmd.@params.Count == 0) { SetStatus("This command has no editable value."); return; }
   GameCommand sel = Selected();
   if (sel == null || sel.id != q.cmd.id) { Error("Select the matching feature before storing this queued value."); return; }
   try {
    queue[i] = new Queued(q.cmd, ParameterValue(q.cmd));
    RenderQueue();
    SetStatus("Queued value updated; review it before running.");
   } catch (Exception ex) { Error(ex.Message); }
  }

  // ---------------------------------------------------------------- research library

  public void OpenResearchLibrary() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   try {
    if (researchEntries == null) {
     string rpath = DataPath("references.json");
     if (!File.Exists(rpath)) { Error("references.json is missing. Re-extract the package."); return; }
     var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
     serializer.MaxJsonLength = 32 * 1024 * 1024;
     researchEntries = serializer.Deserialize<List<ResearchRecord>>(System.IO.File.ReadAllText(rpath, Encoding.UTF8));
     if (researchEntries == null) throw new Exception("Reference database is empty.");
    }

    PopupForm d = NewDialog("CROWN's ALBION — Research library (read-only)", 1040, 700, true);
    d.Controls.Add(MakeLabel("RESEARCH LIBRARY — UNVERIFIED REFERENCES", Albion.H2, Albion.GoldBright, 20, 12, 900, 30));
    d.Controls.Add(MakeLabel("Nothing on this page executes in Fable III. Copy commands only after checking syntax and protecting your saves.", Albion.Small, Albion.Muted, 20, 42, 1000, 22));

    ComboBox scope = new ComboBox();
    scope.SetBounds(20, 76, 190, 26);
    scope.DropDownStyle = ComboBoxStyle.DropDownList;
    scope.BackColor = Albion.PanelHi; scope.ForeColor = Albion.Text; scope.FlatStyle = FlatStyle.Flat;
    scope.Items.AddRange(new object[] { "All references", "Item identifiers", "Debug expressions", "Weapons", "Appearance", "Inventory", "Trade items", "Game records", "Collection functions", "Extracted Unlocker Lua" });
    scope.SelectedIndex = 0;
    d.Controls.Add(scope);

    TextBox q = ThemedText(220, 76, 800, 26, Albion.Body);
    d.Controls.Add(q);

    ListBox names = ThemedList(20, 112, 470, 486);
    d.Controls.Add(names);

    TextBox code = ThemedText(504, 112, 516, 290, new Font("Consolas", 9.5f));
    code.ReadOnly = true; code.Multiline = true; code.ScrollBars = ScrollBars.Both;
    d.Controls.Add(code);

    Label meta = MakeLabel("Pick an item or command. Reference material, not runtime-tested.", Albion.Small, Albion.Muted, 504, 410, 516, 120);
    d.Controls.Add(meta);

    Label count = MakeLabel("", Albion.Small, Albion.Muted, 20, 606, 400, 22);
    d.Controls.Add(count);

    AlbionButton copy = new AlbionButton(); copy.Text = "COPY LUA CANDIDATE"; copy.Style = AlbionButtonStyle.Primary;
    copy.SetBounds(504, 540, 250, 42);
    copy.Click += delegate {
     ResearchRecord r = names.SelectedItem as ResearchRecord;
     if (r != null && !string.IsNullOrEmpty(r.preview)) { Clipboard.SetText(r.preview); Toast("Candidate Lua copied. It was not executed or verified.", Albion.GoldBright); }
    };
    d.Controls.Add(copy);

    AlbionButton copyName = new AlbionButton(); copyName.Text = "COPY IDENTIFIER"; copyName.Style = AlbionButtonStyle.Gold;
    copyName.SetBounds(766, 540, 254, 42);
    copyName.Click += delegate {
     ResearchRecord r = names.SelectedItem as ResearchRecord;
     if (r != null) Clipboard.SetText(r.raw ?? r.name);
    };
    d.Controls.Add(copyName);

    AlbionButton queueItem = new AlbionButton(); queueItem.Text = "QUEUE ITEM (EXPERIMENTAL)"; queueItem.Style = AlbionButtonStyle.Danger;
    queueItem.SetBounds(504, 590, 516, 40);
    queueItem.Click += delegate {
     ResearchRecord record = names.SelectedItem as ResearchRecord;
     if (record == null || record.kind != "unlocker") { Notice("Select an extracted All Items Unlocker entry first.", "CROWN"); return; }
     string candidate = (record.preview ?? "").Trim();
     bool allowed = Regex.IsMatch(candidate, @"^Inventory\.AddItemOfType\(GetLocalHero\(\), 'Object[A-Za-z0-9_]+'\)$") ||
        Regex.IsMatch(candidate, @"^Debug\.SetHeroWearingItemOfType\('Object[A-Za-z0-9_]+', true\)$") ||
        Regex.IsMatch(candidate, @"^Debug\.GiveAll(MeleeWeapons|RangedWeapons|SpellGauntlets)\(\)$");
     if (!allowed) { Notice("That Lua is not in the audited grant / appearance allowlist. Nothing was queued.", "CROWN"); return; }
     GameCommand cmd = new GameCommand();
     cmd.id = "unlocker:" + record.name;
     cmd.title = "UNVERIFIED: " + record.name;
     cmd.group = "Inventory";
     cmd.summary = "Source-derived grant. May change the save or duplicate items.";
     cmd.template = candidate;
     cmd.risk = "high";
     cmd.kind = "action";
     cmd.evidence = "community";
     cmd.coop = "unverified";
     cmd.icon = "open-treasure-chest";
     cmd.@params = new List<Parameter>();
     QueueCommand(cmd, null);
    };
    d.Controls.Add(queueItem);

    AlbionButton done = new AlbionButton(); done.Text = "BACK / CLOSE"; done.Style = AlbionButtonStyle.Ghost;
    done.SetBounds(20, 600, 200, 42);
    done.Click += delegate { d.Close(); };
    d.Controls.Add(done);

    Action refresh = delegate {
     string needle = (q.Text ?? "").Trim();
     string chosen = scope.SelectedItem as string;
     if (chosen == null) chosen = "All references";
     IEnumerable<ResearchRecord> filtered = researchEntries.Where(r =>
      (chosen == "All references" ||
       (chosen == "Item identifiers" && r.kind == "item") ||
       (chosen == "Debug expressions" && r.kind == "debug") ||
       (chosen == "Extracted Unlocker Lua" && r.kind == "unlocker") ||
       r.category == chosen) &&
      (needle.Length == 0 || ((r.name ?? "") + " " + (r.raw ?? "") + " " + (r.category ?? "") + " " + (r.path ?? "")).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
     List<ResearchRecord> matches = filtered.Take(500).ToList();
     names.BeginUpdate(); names.Items.Clear();
     foreach (ResearchRecord r in matches) names.Items.Add(r);
     names.EndUpdate();
     count.Text = matches.Count == 500 ? "Showing the first 500 results; refine the search." : "Showing " + matches.Count + " result(s)";
     if (matches.Count > 0) names.SelectedIndex = 0;
     else { code.Text = ""; meta.Text = "No matching references."; }
    };

    names.SelectedIndexChanged += delegate {
     ResearchRecord r = names.SelectedItem as ResearchRecord;
     if (r == null) return;
     code.Text = string.IsNullOrEmpty(r.preview) ? "No known Lua snippet. Identifier only." : r.preview;
     meta.Text = "CATEGORY: " + r.category + "\r\nSOURCE: " + r.source + "\r\nSTATUS: " + r.validation + (string.IsNullOrEmpty(r.path) ? "" : "\r\nMENU PATH: " + r.path);
    };
    q.TextChanged += delegate { refresh(); };
    scope.SelectedIndexChanged += delegate { refresh(); };

    RegisterPad(d, delegate(Native.XState st, ushort press) {
     if ((press & Native.PAD_DPAD_UP) != 0 && names.Items.Count > 0) names.SelectedIndex = Math.Max(0, names.SelectedIndex - 1);
     if ((press & Native.PAD_DPAD_DOWN) != 0 && names.Items.Count > 0) names.SelectedIndex = Math.Min(names.Items.Count - 1, names.SelectedIndex + 1);
     if ((press & Native.PAD_DPAD_LEFT) != 0) scope.SelectedIndex = Math.Max(0, scope.SelectedIndex - 1);
     if ((press & Native.PAD_DPAD_RIGHT) != 0) scope.SelectedIndex = Math.Min(scope.Items.Count - 1, scope.SelectedIndex + 1);
     if ((press & Native.PAD_A) != 0) queueItem.PerformClick();
     if ((press & Native.PAD_X) != 0) copyName.PerformClick();
     if ((press & Native.PAD_Y) != 0) copy.PerformClick();
    });

    d.AcceptButton = queueItem; d.CancelButton = done;
    refresh();
    ShowPopup(d);
   } catch (Exception ex) { Error("Research library: " + ex.Message); }
  }

  // ---------------------------------------------------------------- item vault

  public void OpenItemVault() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   if (vault == null || vault.Count == 0) {
    Notice("The item vault is empty.\r\n\r\nGenerate data\\item_vault.json from the extracted item, weapon, outfit and unlocker sources to enable curated grants.", "Item vault");
    return;
   }

   PopupForm d = NewDialog("CROWN's ALBION — Item vault", 1040, 700, true);
   d.Controls.Add(MakeLabel("ITEM VAULT — CURATED GRANTS", Albion.H2, Albion.GoldBright, 20, 12, 700, 30));
   d.Controls.Add(MakeLabel("Source-derived grants. Every entry is queued first; nothing is written to the game until you RUN QUEUE.", Albion.Small, Albion.Muted, 20, 42, 1000, 22));

   ComboBox cat = new ComboBox();
   cat.SetBounds(20, 76, 230, 26);
   cat.DropDownStyle = ComboBoxStyle.DropDownList;
   cat.BackColor = Albion.PanelHi; cat.ForeColor = Albion.Text; cat.FlatStyle = FlatStyle.Flat;
   cat.Items.Add("All categories");
   foreach (string c in vault.Where(v => !string.IsNullOrEmpty(v.category)).Select(v => v.category).Distinct().OrderBy(x => x)) cat.Items.Add(c);
   cat.SelectedIndex = 0;
   d.Controls.Add(cat);

   TextBox q = ThemedText(260, 76, 760, 26, Albion.Body);
   d.Controls.Add(q);

   ListBox items = ThemedList(20, 112, 470, 500);
   items.DrawMode = DrawMode.OwnerDrawFixed;
   items.ItemHeight = 30;
   items.DrawItem += DrawVaultRow;
   d.Controls.Add(items);

   TextBox preview = ThemedText(504, 112, 516, 200, new Font("Consolas", 9.5f));
   preview.ReadOnly = true; preview.Multiline = true; preview.ScrollBars = ScrollBars.Both;
   preview.BackColor = Albion.LeatherHi;
   d.Controls.Add(preview);

   Label meta = MakeLabel("", Albion.Small, Albion.Muted, 504, 320, 516, 180);
   d.Controls.Add(meta);

   Label count = MakeLabel("", Albion.Small, Albion.Muted, 20, 620, 460, 22);
   d.Controls.Add(count);

   AlbionButton grant = new AlbionButton(); grant.Text = "QUEUE GRANT"; grant.Style = AlbionButtonStyle.Primary;
   grant.SetBounds(504, 540, 250, 42);
   grant.Click += delegate {
    VaultItem v = items.SelectedItem as VaultItem;
    if (v == null) return;
    GameCommand cmd = new GameCommand();
    cmd.id = "vault:" + v.id;
    cmd.title = v.name;
    cmd.group = VaultGroup(v.category);
    cmd.summary = "Curated vault grant from " + (v.evidence ?? "source-derived") + " material.";
    cmd.template = v.template;
    cmd.risk = string.IsNullOrEmpty(v.risk) ? "medium" : v.risk;
    cmd.kind = "action";
    cmd.evidence = v.evidence;
    cmd.coop = "unverified";
    cmd.icon = string.IsNullOrEmpty(v.icon) ? "open-treasure-chest" : v.icon;
    cmd.notes = v.note;
    cmd.@params = new List<Parameter>();
    QueueCommand(cmd, null);
   };
   d.Controls.Add(grant);

   AlbionButton copy = new AlbionButton(); copy.Text = "COPY LUA"; copy.Style = AlbionButtonStyle.Gold;
   copy.SetBounds(766, 540, 254, 42);
   copy.Click += delegate {
    VaultItem v = items.SelectedItem as VaultItem;
    if (v != null) { Clipboard.SetText(v.template ?? ""); Toast("Grant Lua copied.", Albion.GoldBright); }
   };
   d.Controls.Add(copy);

   AlbionButton done = new AlbionButton(); done.Text = "BACK / CLOSE"; done.Style = AlbionButtonStyle.Ghost;
   done.SetBounds(20, 610, 200, 42);
   done.Click += delegate { d.Close(); };
   d.Controls.Add(done);

   items.SelectedIndexChanged += delegate {
    VaultItem v = items.SelectedItem as VaultItem;
    if (v == null) { preview.Text = ""; meta.Text = ""; return; }
    preview.Text = v.template ?? "";
    meta.Text = "NAME: " + v.name + "\r\nCATEGORY: " + v.category + "\r\nRISK: " + (v.risk ?? "medium") +
     "\r\nEVIDENCE: " + (v.evidence ?? "source-derived") + "\r\n\r\n" + (v.note ?? "Repeat use may duplicate items.");
   };

   Action refresh = delegate {
    string needle = (q.Text ?? "").Trim();
    string chosen = cat.SelectedItem as string;
    IEnumerable<VaultItem> filtered = vault.Where(v =>
     (chosen == null || chosen == "All categories" || v.category == chosen) &&
     (needle.Length == 0 || ((v.name ?? "") + " " + (v.category ?? "") + " " + (v.template ?? "")).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
    List<VaultItem> list = filtered.ToList();
    items.BeginUpdate(); items.Items.Clear();
    foreach (VaultItem v in list) items.Items.Add(v);
    items.EndUpdate();
    count.Text = list.Count + " grant(s) shown";
    if (list.Count > 0) items.SelectedIndex = 0; else { preview.Text = ""; meta.Text = "No matching grants."; }
   };
   q.TextChanged += delegate { refresh(); };
   cat.SelectedIndexChanged += delegate { refresh(); };

   RegisterPad(d, delegate(Native.XState st, ushort press) {
    if ((press & Native.PAD_DPAD_UP) != 0 && items.Items.Count > 0) items.SelectedIndex = Math.Max(0, items.SelectedIndex - 1);
    if ((press & Native.PAD_DPAD_DOWN) != 0 && items.Items.Count > 0) items.SelectedIndex = Math.Min(items.Items.Count - 1, items.SelectedIndex + 1);
    if ((press & Native.PAD_DPAD_LEFT) != 0) cat.SelectedIndex = Math.Max(0, cat.SelectedIndex - 1);
    if ((press & Native.PAD_DPAD_RIGHT) != 0) cat.SelectedIndex = Math.Min(cat.Items.Count - 1, cat.SelectedIndex + 1);
    if ((press & Native.PAD_A) != 0) grant.PerformClick();
    if ((press & Native.PAD_Y) != 0) copy.PerformClick();
   });

   d.AcceptButton = grant; d.CancelButton = done;
   refresh();
   ShowPopup(d);
  }

  static string VaultGroup(string category) {
   if (string.IsNullOrEmpty(category)) return "Inventory";
   string c = category.ToLowerInvariant();
   if (c.Contains("outfit") || c.Contains("clothing") || c.Contains("appearance") || c.Contains("hair") || c.Contains("beard") || c.Contains("makeup") || c.Contains("tattoo")) return "Appearance";
   if (c.Contains("weapon") || c.Contains("gauntlet")) return "Inventory";
   return "Inventory";
  }

  void DrawVaultRow(object sender, DrawItemEventArgs e) {
   ListBox lb = sender as ListBox;
   if (lb == null || e.Index < 0) return;
   VaultItem v = lb.Items[e.Index] as VaultItem;
   bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
   ListPaint.FillRow(e.Graphics, e.Bounds, selected, false, Albion.PanelHi);
   if (v == null) return;
   Color iconColor = selected ? Albion.GoldBright : Albion.GoldDim;
   Bitmap icon = SvgIcon.Get(Albion.IconDir, v.icon, 18, iconColor, 0);
   if (icon != null) e.Graphics.DrawImage(icon, e.Bounds.X + 8, e.Bounds.Y + (e.Bounds.Height - 18) / 2, 18, 18);
   Albion.Draw(e.Graphics, v.name, selected ? Albion.BodyBold : Albion.Body, selected ? Albion.GoldBright : Albion.Text,
    new Rectangle(e.Bounds.X + 32, e.Bounds.Y, e.Bounds.Width - 150, e.Bounds.Height), ContentAlignment.MiddleLeft);
   Albion.Draw(e.Graphics, v.category, Albion.Tiny, Albion.Muted,
    new Rectangle(e.Bounds.Right - 130, e.Bounds.Y, 120, e.Bounds.Height), ContentAlignment.MiddleRight);
  }

  // ---------------------------------------------------------------- diagnostics

  public void OpenDiagnostics() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   detailMode = "Diagnostics";
   try {
    PopupForm d = NewDialog("CROWN's ALBION — Diagnostics", 1000, 700, true);
    d.Controls.Add(MakeLabel("DIAGNOSTICS", Albion.H2, Albion.GoldBright, 20, 12, 500, 30));
    d.Controls.Add(MakeLabel("Read-only status of the game folder, script path, injector and safety gates.", Albion.Small, Albion.Muted, 20, 42, 900, 22));

    ListBox rows = ThemedList(20, 76, 960, 520);
    rows.DrawMode = DrawMode.OwnerDrawFixed;
    rows.ItemHeight = 48;
    rows.DrawItem += DrawDiagRow;
    d.Controls.Add(rows);

    Action refresh = delegate {
     rows.BeginUpdate(); rows.Items.Clear();
     foreach (DiagRow r in Diagnostics()) rows.Items.Add(r);
     rows.EndUpdate();
    };

    AlbionButton again = new AlbionButton(); again.Text = "REFRESH"; again.Style = AlbionButtonStyle.Gold;
    again.SetBounds(20, 610, 150, 42);
    again.Click += delegate { refresh(); RefreshStatus(); };
    d.Controls.Add(again);

    AlbionButton copyReport = new AlbionButton(); copyReport.Text = "COPY REPORT"; copyReport.Style = AlbionButtonStyle.Ghost;
    copyReport.SetBounds(180, 610, 170, 42);
    copyReport.Click += delegate {
     StringBuilder sb = new StringBuilder();
     sb.AppendLine("CROWN's ALBION " + VERSION + " diagnostics — " + DateTime.Now.ToString("u"));
     foreach (DiagRow r in Diagnostics()) sb.AppendLine(r.Label + ": " + r.Value + "  [" + r.Status + "]  " + r.Hint);
     Clipboard.SetText(sb.ToString());
     Toast("Diagnostics copied to the clipboard.", Albion.Good);
    };
    d.Controls.Add(copyReport);

    AlbionButton done = new AlbionButton(); done.Text = "CLOSE"; done.Style = AlbionButtonStyle.Primary;
    done.SetBounds(830, 610, 150, 42);
    done.Click += delegate { d.Close(); };
    d.Controls.Add(done);

    RegisterPad(d, delegate(Native.XState st, ushort press) {
     if ((press & Native.PAD_DPAD_UP) != 0 && rows.Items.Count > 0) rows.SelectedIndex = Math.Max(0, rows.SelectedIndex - 1);
     if ((press & Native.PAD_DPAD_DOWN) != 0 && rows.Items.Count > 0) rows.SelectedIndex = Math.Min(rows.Items.Count - 1, rows.SelectedIndex + 1);
     if ((press & Native.PAD_X) != 0) again.PerformClick();
    });

    d.AcceptButton = again; d.CancelButton = done;
    refresh();
    ShowPopup(d);
   } finally { detailMode = "Feature"; }
  }

  void DrawDiagRow(object sender, DrawItemEventArgs e) {
   ListBox lb = sender as ListBox;
   if (lb == null || e.Index < 0) return;
   DiagRow r = lb.Items[e.Index] as DiagRow;
   bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
   ListPaint.FillRow(e.Graphics, e.Bounds, selected, false, Albion.PanelHi);
   if (r == null) return;
   Graphics g = e.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   Color c = DiagColor(r.Status);
   using (SolidBrush b = new SolidBrush(c)) g.FillEllipse(b, e.Bounds.X + 12, e.Bounds.Y + e.Bounds.Height / 2 - 6, 12, 12);
   Albion.Draw(g, r.Label, Albion.BodyBold, selected ? Albion.GoldBright : Albion.Text,
    new Rectangle(e.Bounds.X + 36, e.Bounds.Y + 3, 200, 22), ContentAlignment.MiddleLeft);
   Albion.Draw(g, r.Value, Albion.Small, c,
    new Rectangle(e.Bounds.X + 240, e.Bounds.Y + 3, e.Bounds.Width - 260, 22), ContentAlignment.MiddleLeft);
   Albion.Draw(g, r.Hint, Albion.Tiny, Albion.Muted,
    new Rectangle(e.Bounds.X + 36, e.Bounds.Y + 24, e.Bounds.Width - 60, 20), ContentAlignment.MiddleLeft);
  }

  // ---------------------------------------------------------------- profiles

  public void OpenProfiles() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   PopupForm d = NewDialog("CROWN's ALBION — Profiles", 660, 520, true);
   d.Controls.Add(MakeLabel("PROFILES & FAVOURITES", Albion.H2, Albion.GoldBright, 22, 12, 600, 30));
   d.Controls.Add(MakeLabel("A profile stores favourites and the current queue. It never touches game saves.", Albion.Small, Albion.Muted, 22, 42, 610, 22));

   ListBox list = ThemedList(22, 78, 610, 260);
   d.Controls.Add(list);

   TextBox name = ThemedText(22, 348, 610, 30, Albion.Body);
   d.Controls.Add(name);

   Action refresh = delegate {
    list.BeginUpdate(); list.Items.Clear();
    foreach (string p in ListProfiles()) list.Items.Add(p);
    list.EndUpdate();
    if (list.Items.Count > 0) list.SelectedIndex = 0;
   };

   AlbionButton save = new AlbionButton(); save.Text = "SAVE AS"; save.Style = AlbionButtonStyle.Primary;
   save.SetBounds(22, 392, 190, 42);
   save.Click += delegate {
    ShowMenu();
    string n = name.Text.Trim();
    if (n.Length == 0) { Error("Enter a profile name first."); return; }
    SaveProfile(n); refresh();
   };
   d.Controls.Add(save);

   AlbionButton load = new AlbionButton(); load.Text = "LOAD"; load.Style = AlbionButtonStyle.Gold;
   load.SetBounds(222, 392, 190, 42);
   load.Click += delegate {
    ShowMenu();
    string n = list.SelectedItem as string;
    if (n == null) { Error("Select a profile to load."); return; }
    LoadProfile(n); d.Close();
   };
   d.Controls.Add(load);

   AlbionButton del = new AlbionButton(); del.Text = "DELETE"; del.Style = AlbionButtonStyle.Danger;
   del.SetBounds(422, 392, 100, 42);
   del.Click += delegate {
    string n = list.SelectedItem as string;
    if (n == null) return;
    if (Notice("Delete profile \"" + n + "\"?", "CROWN", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
    DeleteProfile(n); refresh();
   };
   d.Controls.Add(del);

   AlbionButton done = new AlbionButton(); done.Text = "CLOSE"; done.Style = AlbionButtonStyle.Ghost;
   done.SetBounds(532, 392, 100, 42);
   done.Click += delegate { d.Close(); };
   d.Controls.Add(done);

   AlbionButton favOnly = new AlbionButton(); favOnly.Text = "CLEAR FAVOURITES"; favOnly.Style = AlbionButtonStyle.Quiet;
   favOnly.SetBounds(22, 440, 200, 34);
   favOnly.Click += delegate {
    if (prefs.Favourites.Count == 0) return;
    if (Notice("Remove every favourite from the current profile?", "CROWN", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
    prefs.Favourites.Clear(); SavePrefs(); RefreshFeatures();
   };
   d.Controls.Add(favOnly);

   d.Controls.Add(MakeLabel("Favourites: " + prefs.Favourites.Count + "  •  queue: " + queue.Count + " action(s)", Albion.Small, Albion.Muted, 240, 446, 392, 24));

   d.AcceptButton = save; d.CancelButton = done;
   refresh();
   ShowPopup(d);
  }

  // ---------------------------------------------------------------- settings

  public void OpenSettings() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   PopupForm d = NewDialog("CROWN's ALBION — Settings", 700, 560, true);
   d.Controls.Add(MakeLabel("SETTINGS", Albion.H2, Albion.GoldBright, 22, 12, 400, 30));
   d.Controls.Add(MakeLabel("Safety defaults are chosen for co-op and for a first run on a spare save.", Albion.Small, Albion.Muted, 22, 42, 660, 22));

   ToggleSwitch coop = new ToggleSwitch();
   coop.OnText = "PROTECTED"; coop.OffText = "OPEN";
   coop.Checked = prefs.CoopSafe;
   coop.SetBounds(480, 84, 190, 34);
   d.Controls.Add(coop);
   d.Controls.Add(MakeLabel("Co-op protection — block gameplay changes while an XLLN session may be active.", Albion.Small, Albion.Text, 22, 84, 450, 34));

   ToggleSwitch risk = new ToggleSwitch();
   risk.OnText = "ALLOWED"; risk.OffText = "LOCKED";
   risk.Checked = prefs.AllowRisk;
   risk.SetBounds(480, 128, 190, 34);
   d.Controls.Add(risk);
   d.Controls.Add(MakeLabel("Allow high-risk / save-changing commands.", Albion.Small, Albion.Text, 22, 128, 450, 34));

   ToggleSwitch pad = new ToggleSwitch();
   pad.OnText = "ENABLED"; pad.OffText = "DISABLED";
   pad.Checked = prefs.Controller;
   pad.SetBounds(480, 172, 190, 34);
   d.Controls.Add(pad);
   d.Controls.Add(MakeLabel("Gamepad navigation (L3 + R3 toggles the menu).", Albion.Small, Albion.Text, 22, 172, 450, 34));

   ToggleSwitch confirm = new ToggleSwitch();
   confirm.OnText = "ALWAYS"; confirm.OffText = "RISKY ONLY";
   confirm.Checked = prefs.ConfirmDispatch;
   confirm.SetBounds(480, 216, 190, 34);
   d.Controls.Add(confirm);
   d.Controls.Add(MakeLabel("Ask before every dispatch (not only high-risk ones).", Albion.Small, Albion.Text, 22, 216, 450, 34));

   ToggleSwitch focus = new ToggleSwitch();
   focus.OnText = "RETURN"; focus.OffText = "STAY";
   focus.Checked = prefs.RestoreGameFocus;
   focus.SetBounds(480, 260, 190, 34);
   d.Controls.Add(focus);
   d.Controls.Add(MakeLabel("Return focus to Fable III after a dispatch or when hiding.", Albion.Small, Albion.Text, 22, 260, 450, 34));

   ToggleSwitch reference = new ToggleSwitch();
   reference.OnText = "SHOWN"; reference.OffText = "HIDDEN";
   reference.Checked = prefs.ShowReferenceCommands;
   reference.SetBounds(480, 304, 190, 34);
   d.Controls.Add(reference);
   d.Controls.Add(MakeLabel("Show reference-only catalogue entries.", Albion.Small, Albion.Text, 22, 304, 450, 34));

   d.Controls.Add(MakeLabel("PULSE WINDOW (ms)", Albion.H3, Albion.Gold, 22, 352, 300, 24));
   d.Controls.Add(MakeLabel("The injector reads the script about once per 60 world frames. 5,000 ms is a safe default.", Albion.Small, Albion.Muted, 22, 374, 470, 40));

   NumericUpDown pulse = new NumericUpDown();
   pulse.SetBounds(480, 350, 190, 30);
   pulse.Minimum = 2500; pulse.Maximum = 60000; pulse.Increment = 500;
   pulse.ThousandsSeparator = true;
   pulse.Value = Math.Max(2500, Math.Min(60000, prefs.PulseMs));
   pulse.BackColor = Albion.PanelHi; pulse.ForeColor = Albion.Text; pulse.Font = Albion.Body;
   d.Controls.Add(pulse);

   d.Controls.Add(MakeLabel("GLOBAL HOTKEY", Albion.H3, Albion.Gold, 22, 414, 300, 24));
   TextBox hotkey = ThemedText(480, 410, 190, 30, Albion.Body);
   hotkey.Text = string.IsNullOrEmpty(prefs.Hotkey) ? "F8" : prefs.Hotkey;
   d.Controls.Add(hotkey);

   AlbionButton apply = new AlbionButton(); apply.Text = "APPLY"; apply.Style = AlbionButtonStyle.Primary;
   apply.SetBounds(22, 486, 200, 44);
   apply.Click += delegate {
    prefs.CoopSafe = coop.Checked;
    prefs.AllowRisk = risk.Checked;
    prefs.Controller = pad.Checked;
    prefs.ConfirmDispatch = confirm.Checked;
    prefs.RestoreGameFocus = focus.Checked;
    prefs.ShowReferenceCommands = reference.Checked;
    prefs.PulseMs = (int)pulse.Value;
    prefs.Hotkey = hotkey.Text.Trim().Length == 0 ? "F8" : hotkey.Text.Trim();
    // Push the gates into the always-visible main-window toggles.
    if (toggleCoop != null) toggleCoop.Checked = prefs.CoopSafe;
    if (toggleRisk != null) toggleRisk.Checked = prefs.AllowRisk;
    if (toggleController != null) toggleController.Checked = prefs.Controller;
    if (showReference != null) showReference.Checked = prefs.ShowReferenceCommands;
    SavePrefs();
    clearTimer.Interval = Math.Max(2500, prefs.PulseMs);
    RefreshFeatures();
    RefreshStatus();
    Toast("Settings applied.", Albion.Good);
    d.Close();
   };
   d.Controls.Add(apply);

   AlbionButton cancel = new AlbionButton(); cancel.Text = "CANCEL"; cancel.Style = AlbionButtonStyle.Ghost;
   cancel.SetBounds(232, 486, 160, 44);
   cancel.Click += delegate { d.Close(); };
   d.Controls.Add(cancel);

   d.AcceptButton = apply; d.CancelButton = cancel;
   ShowPopup(d);
  }
 }
}
