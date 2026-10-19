// CROWN's ALBION v2.0 — user interface (Albion fantasy theme).
// Split from Trainer.cs so the engine, the shell and the dialogs stay readable.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CROWNsAlbion {

 // A double-buffered panel used for every painted surface.
 public class BuffPanel : Panel {
  public BuffPanel() {
   SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
   BackColor = Albion.Panel;
  }
 }

 // Small status pill used in the header and the detail pane.
 public class Chip : Control {
  public string Caption = "";
  public string Value = "—";
  public Color Accent = Albion.Gold;

  public Chip() {
   SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
   BackColor = Color.Transparent;
   Cursor = Cursors.Hand;
   Height = 24;
   Font = Albion.Tiny;
  }

  protected override void OnPaint(PaintEventArgs e) {
   Graphics g = e.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
   using (SolidBrush b = new SolidBrush(Color.FromArgb(48, Accent))) Albion.FillRounded(g, b, r, 4);
   using (Pen p = new Pen(Color.FromArgb(150, Accent), 1f)) Albion.DrawRounded(g, p, r, 4);
   string cap = Caption.ToUpperInvariant();
   Size cs = TextRenderer.MeasureText(cap, Albion.Tiny);
   if (cs.Width > Width - 24) cs = new Size(Math.Max(0, Width - 24), cs.Height);
   Rectangle capRect = new Rectangle(8, 0, cs.Width, Height);
   TextRenderer.DrawText(g, cap, Albion.Tiny, capRect, Color.FromArgb(200, Albion.Muted), TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
   Rectangle valRect = new Rectangle(8 + cs.Width + 6, 0, Math.Max(0, Width - (8 + cs.Width + 6) - 8), Height);
   TextRenderer.DrawText(g, Value, Albion.SmallBold, valRect, Accent, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
  }
 }

 public partial class Trainer {

  // Header chips + detail mode (referenced by the engine core).
  public Chip headerChipGame, headerChipScript, headerChipCoop, headerChipInjector;
  public string detailMode = "Feature";

  // Shell
  Panel headerPanel, footerPanel;
  Label toastLabel;

  SplitContainer mainSplit, workSplit;

  // Left column
  AlbionButton prepareButton, locateButton, clearButton, profilesButton, settingsButton;
  Panel stripPanel;

  // Middle column
  Label featuresLabel;

  // Detail column: painted surface + editors
  public TextBox numericText;
  public ComboBox selectBox;
  public AlbionSlider valueSlider;
  public ToggleSwitch toggleSwitch;
  public AlbionButton amountPadButton, queueViewButton;
  Label queueHeaderLabel;
  AlbionButton runQueueButton, clearQueueButton;

  // Detail layout rectangles (computed once, shared by layout + paint)
  Rectangle rTitle, rBadges, rSummary, rParamLabel, rNotes, rDivider;
  int detailParamRowY;

  bool syncing = false;

  // ---------------------------------------------------------------- build

  public void BuildUI() {
   SuspendLayout();

   TableLayoutPanel rootGrid = new TableLayoutPanel();
   rootGrid.Dock = DockStyle.Fill;
   rootGrid.ColumnCount = 1; rootGrid.RowCount = 3;
   rootGrid.BackColor = Albion.Leather;
   rootGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
   rootGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   rootGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
   Controls.Add(rootGrid);

   headerPanel = BuildHeader();
   rootGrid.Controls.Add(headerPanel, 0, 0);

   SplitContainer main = new SplitContainer();
   main.Dock = DockStyle.Fill; main.Orientation = Orientation.Vertical;
   main.FixedPanel = FixedPanel.Panel1; main.SplitterWidth = 6;
   main.BackColor = Albion.Leather;
   main.Panel1.BackColor = Albion.Leather;
   main.Panel2.BackColor = Albion.Leather;
   rootGrid.Controls.Add(main, 0, 1);
   mainSplit = main;

   BuildSidebar(main.Panel1);

   SplitContainer work = new SplitContainer();
   work.Dock = DockStyle.Fill; work.Orientation = Orientation.Vertical;
   work.SplitterWidth = 6;
   work.BackColor = Albion.Leather;
   work.Panel1.BackColor = Albion.Leather;
   work.Panel2.BackColor = Albion.Leather;
   main.Panel2.Controls.Add(work);
   workSplit = work;

   BuildFeatureColumn(work.Panel1);
   BuildDetailColumn(work.Panel2);

   footerPanel = BuildFooter();
   rootGrid.Controls.Add(footerPanel, 0, 2);

   ResumeLayout();
   ApplySplitters();
   Shown += delegate { ApplySplitters(); LayoutHeader(); InvalidateDetail(); RenderQueue(); };
   LayoutHeader();
  }

  void ApplySplitters() {
   ApplySplitter(mainSplit, 254, 220, 420);
   ApplySplitter(workSplit, 430, 340, 380);
  }

  static void ApplySplitter(SplitContainer s, int value, int min1, int min2) {
   if (s == null) return;
   try {
    if (s.Width < min1 + min2 + s.SplitterWidth) {
     s.Panel1MinSize = 25; s.Panel2MinSize = 25;
     return;
    }
    s.Panel1MinSize = min1; s.Panel2MinSize = min2;
    int max = s.Width - min2 - s.SplitterWidth;
    if (max < min1) return;
    s.SplitterDistance = Math.Max(min1, Math.Min(max, value));
   } catch { }
  }

  Panel BuildHeader() {
   Panel header = new Panel();
   header.Dock = DockStyle.Fill;
   header.BackColor = Albion.LeatherHi;
   header.Paint += HeaderPaint;

   AlbionButton gold = HeaderButton("QUICK GOLD", AlbionButtonStyle.Primary, delegate { ShowMenu(); OpenGoldPad(); });
   AlbionButton queueBtn = HeaderButton("QUEUE", AlbionButtonStyle.Gold, delegate { OpenQueueManager(); });
   AlbionButton vault = HeaderButton("VAULT", AlbionButtonStyle.Gold, delegate { OpenItemVault(); });
   AlbionButton lib = HeaderButton("LIBRARY", AlbionButtonStyle.Gold, delegate { OpenResearchLibrary(); });
   AlbionButton diag = HeaderButton("DIAGNOSTICS", AlbionButtonStyle.Ghost, delegate { OpenDiagnostics(); });
   AlbionButton help = HeaderButton("HELP", AlbionButtonStyle.Ghost, delegate { ShowHelp(); });
   AlbionButton hide = HeaderButton("HIDE  F8", AlbionButtonStyle.Ghost, delegate { HideMenu(); });

   headerChipGame = new Chip(); headerChipGame.Caption = "game";
   headerChipScript = new Chip(); headerChipScript.Caption = "script";
   headerChipCoop = new Chip(); headerChipCoop.Caption = "co-op";
   headerChipInjector = new Chip(); headerChipInjector.Caption = "injector";

   header.Controls.Add(gold); header.Controls.Add(queueBtn); header.Controls.Add(vault);
   header.Controls.Add(lib); header.Controls.Add(diag); header.Controls.Add(help); header.Controls.Add(hide);
   header.Controls.Add(headerChipGame); header.Controls.Add(headerChipScript);
   header.Controls.Add(headerChipCoop); header.Controls.Add(headerChipInjector);

   header.Tag = new object[] { gold, queueBtn, vault, lib, diag, help, hide };
   header.Resize += delegate { LayoutHeader(); };
   return header;
  }

  AlbionButton HeaderButton(string text, AlbionButtonStyle style, EventHandler click) {
   AlbionButton b = new AlbionButton();
   b.Text = text; b.Style = style; b.Height = 34; b.Font = Albion.SmallBold;
   b.Click += click;
   return b;
  }

  void LayoutHeader() {
   if (headerPanel == null) return;
   object[] buttons = headerPanel.Tag as object[];
   if (buttons == null) return;
   int[] widths = { 112, 88, 88, 92, 116, 76, 104 };
   int gap = 8, x = headerPanel.ClientSize.Width - 18;
   for (int i = 0; i < buttons.Length; i++) {
    AlbionButton b = (AlbionButton)buttons[i];
    x -= widths[i];
    b.SetBounds(x, 14, widths[i], 34);
    x -= gap;
   }
   int chipY = 64;
   Chip[] chips = { headerChipGame, headerChipScript, headerChipCoop, headerChipInjector };
   int[] cw = { 156, 168, 150, 150 };
   int cx = 24;
   for (int i = 0; i < chips.Length; i++) {
    chips[i].SetBounds(cx, chipY, cw[i], 24);
    cx += cw[i] + 8;
   }
  }

  void HeaderPaint(object sender, PaintEventArgs e) {
   Graphics g = e.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   Rectangle r = headerPanel.ClientRectangle;
   if (r.Width <= 0 || r.Height <= 0) return;
   using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, r.Width), Math.Max(1, r.Height)), Albion.PanelHi, Albion.Leather, LinearGradientMode.Vertical))
    g.FillRectangle(b, r);
   using (Pen p = new Pen(Albion.GoldDim, 2f)) g.DrawLine(p, 0, r.Height - 2, r.Width, r.Height - 2);
   using (Pen p = new Pen(Color.FromArgb(70, Albion.GoldDim), 1f)) g.DrawLine(p, 0, r.Height - 4, r.Width, r.Height - 4);
   Bitmap crown = SvgIcon.Get(Albion.IconDir, "crown", 28, Albion.GoldBright, 0);
   if (crown != null) g.DrawImage(crown, 22, 12, 28, 28);
   Albion.Draw(g, "CROWN'S ALBION", Albion.H1, Albion.GoldBright, new Rectangle(58, 8, 520, 34), ContentAlignment.MiddleLeft);
   Albion.Draw(g, "v" + VERSION + "  •  Albion fantasy shell  •  non-injecting external trainer  •  F8 menu  •  F1 help",
    Albion.Small, Albion.Muted, new Rectangle(60, 40, 760, 18), ContentAlignment.MiddleLeft);
  }

  void BuildSidebar(Panel host) {
   TableLayoutPanel grid = new TableLayoutPanel();
   grid.Dock = DockStyle.Fill; grid.ColumnCount = 1; grid.RowCount = 4;
   grid.Padding = new Padding(10, 8, 8, 8);
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
   grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
   host.Controls.Add(grid);

   Label cats = MakeLabel("CATEGORIES", Albion.H3, Albion.Gold, 0, 0, 200, 24);
   grid.Controls.Add(cats, 0, 0);

   categories = new ListBox();
   categories.Dock = DockStyle.Fill;
   categories.DrawMode = DrawMode.OwnerDrawFixed;
   categories.ItemHeight = 27;
   categories.IntegralHeight = false;
   categories.BorderStyle = BorderStyle.None;
   categories.BackColor = Albion.Panel;
   categories.ForeColor = Albion.Text;
   categories.Font = Albion.Body;
   categories.DrawItem += DrawCategoryItem;
   categories.SelectedIndexChanged += delegate { if (!syncing) RefreshFeatures(); };
   grid.Controls.Add(categories, 0, 1);

   stripPanel = new Panel();
   stripPanel.Dock = DockStyle.Fill; stripPanel.BackColor = Albion.Panel;
   stripPanel.Paint += delegate(object s, PaintEventArgs e) {
    Albion.NineSlice(e.Graphics, "panel-border-016", new Rectangle(0, 0, stripPanel.ClientSize.Width - 1, stripPanel.ClientSize.Height - 1), Albion.Border, 12);
   };
   grid.Controls.Add(stripPanel, 0, 2);

   stripPanel.Controls.Add(MakeLabel("QUICK ACTIONS", Albion.H3, Albion.Gold, 12, 4, 200, 22));

   prepareButton = new AlbionButton(); prepareButton.Text = "PREPARE SCRIPT PATH"; prepareButton.Style = AlbionButtonStyle.Ghost;
   prepareButton.Click += delegate { Prepare(); };
   prepareButton.SetBounds(12, 22, 208, 28);
   prepareButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
   stripPanel.Controls.Add(prepareButton);

   locateButton = new AlbionButton(); locateButton.Text = "CHOOSE GAME FOLDER"; locateButton.Style = AlbionButtonStyle.Ghost;
   locateButton.Click += delegate { Locate(); };
   locateButton.SetBounds(12, 54, 208, 28);
   locateButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
   stripPanel.Controls.Add(locateButton);

   clearButton = new AlbionButton(); clearButton.Text = "STOP / CLEAR LUA"; clearButton.Style = AlbionButtonStyle.Danger;
   clearButton.Click += delegate { ClearPulse(true); };
   clearButton.SetBounds(12, 86, 208, 28);
   clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
   stripPanel.Controls.Add(clearButton);

   profilesButton = new AlbionButton(); profilesButton.Text = "PROFILES"; profilesButton.Style = AlbionButtonStyle.Quiet;
   profilesButton.Click += delegate { OpenProfiles(); };
   profilesButton.SetBounds(12, 120, 99, 28);
   stripPanel.Controls.Add(profilesButton);

   settingsButton = new AlbionButton(); settingsButton.Text = "SETTINGS"; settingsButton.Style = AlbionButtonStyle.Quiet;
   settingsButton.Click += delegate { OpenSettings(); };
   settingsButton.SetBounds(117, 120, 103, 28);
   stripPanel.Controls.Add(settingsButton);

   // Protection gates stay visible at all times so the safety state is never a surprise.
   Panel gates = new Panel();
   gates.Dock = DockStyle.Fill; gates.BackColor = Albion.Panel;
   gates.Paint += delegate(object s, PaintEventArgs e) {
    Albion.NineSlice(e.Graphics, "panel-border-002", new Rectangle(0, 0, gates.ClientSize.Width - 1, gates.ClientSize.Height - 1), Albion.Border, 12);
   };
   grid.Controls.Add(gates, 0, 3);

   gates.Controls.Add(MakeLabel("PROTECTION GATES", Albion.H3, Albion.Gold, 12, 2, 220, 20));

   toggleCoop = new ToggleSwitch();
   toggleCoop.Checked = prefs.CoopSafe;
   toggleCoop.OnText = "CO-OP ON"; toggleCoop.OffText = "CO-OP OFF";
   toggleCoop.SetBounds(112, 24, 118, 28);
   toggleCoop.CheckedChanged += delegate {
    prefs.CoopSafe = toggleCoop.Checked; SavePrefs(); InvalidateDetail();
    SetStatus(toggleCoop.Checked ? "Co-op protection ON. Only the harmless script test can dispatch." : "Co-op protection OFF. Use a spare SINGLE-PLAYER save; XLLN sessions are unverified.");
   };
   gates.Controls.Add(toggleCoop);

   toggleRisk = new ToggleSwitch();
   toggleRisk.Checked = prefs.AllowRisk;
   toggleRisk.OnText = "RISK ON"; toggleRisk.OffText = "RISK OFF";
   toggleRisk.SetBounds(112, 54, 118, 28);
   toggleRisk.CheckedChanged += delegate { prefs.AllowRisk = toggleRisk.Checked; SavePrefs(); InvalidateDetail(); };
   gates.Controls.Add(toggleRisk);

   toggleController = new ToggleSwitch();
   toggleController.Checked = prefs.Controller;
   toggleController.OnText = "PAD ON"; toggleController.OffText = "PAD OFF";
   toggleController.SetBounds(112, 84, 118, 28);
   toggleController.CheckedChanged += delegate { padInput.Reset(); prefs.Controller = toggleController.Checked; SavePrefs(); };
   gates.Controls.Add(toggleController);

   gates.Controls.Add(MakeLabel("Co-op safe", Albion.Small, Albion.Muted, 12, 24, 96, 28));
   gates.Controls.Add(MakeLabel("High-risk", Albion.Small, Albion.Muted, 12, 54, 96, 28));
   gates.Controls.Add(MakeLabel("Xbox pad", Albion.Small, Albion.Muted, 12, 84, 96, 28));
  }

  void BuildFeatureColumn(Panel host) {
   TableLayoutPanel grid = new TableLayoutPanel();
   grid.Dock = DockStyle.Fill; grid.ColumnCount = 1; grid.RowCount = 4;
   grid.Padding = new Padding(10, 10, 6, 10);
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
   grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
   host.Controls.Add(grid);

   featuresLabel = MakeLabel("FEATURES", Albion.H3, Albion.Gold, 0, 0, 200, 24);
   grid.Controls.Add(featuresLabel, 0, 0);

   Panel searchRow = new Panel(); searchRow.Dock = DockStyle.Fill; searchRow.BackColor = Color.Transparent;
   grid.Controls.Add(searchRow, 0, 1);

   search = new TextBox();
   search.SetBounds(0, 4, 240, 28);
   search.Font = Albion.Body; search.BackColor = Albion.PanelHi; search.ForeColor = Albion.Text;
   search.BorderStyle = BorderStyle.FixedSingle;
   search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
   search.TextChanged += delegate { if (!syncing) RefreshFeatures(); };
   searchRow.Controls.Add(search);

   sortBox = new ComboBox();
   sortBox.SetBounds(248, 4, 118, 28);
   sortBox.DropDownStyle = ComboBoxStyle.DropDownList;
   sortBox.BackColor = Albion.PanelHi; sortBox.ForeColor = Albion.Text;
   sortBox.FlatStyle = FlatStyle.Flat; sortBox.Font = Albion.Small;
   sortBox.Items.AddRange(new object[] { "Name A–Z", "Risk", "Group", "Co-op" });
   sortBox.SelectedIndex = 0;
   sortBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
   sortBox.SelectedIndexChanged += delegate { if (!syncing) RefreshFeatures(); };
   searchRow.Controls.Add(sortBox);

   features = new ListBox();
   features.Dock = DockStyle.Fill;
   features.DrawMode = DrawMode.OwnerDrawFixed;
   features.ItemHeight = 42;
   features.IntegralHeight = false;
   features.BorderStyle = BorderStyle.None;
   features.BackColor = Albion.Panel;
   features.ForeColor = Albion.Text;
   features.Font = Albion.Body;
   features.DrawItem += DrawFeatureItem;
   features.SelectedIndexChanged += delegate { if (!syncing) InvalidateDetail(); };
   features.DoubleClick += delegate { QueueSelected(); };
   grid.Controls.Add(features, 0, 2);

   Panel tools = new Panel(); tools.Dock = DockStyle.Fill; tools.BackColor = Color.Transparent;
   grid.Controls.Add(tools, 0, 3);

   showReference = new CheckBox();
   showReference.Text = "Show reference-only entries";
   showReference.ForeColor = Albion.Muted; showReference.Font = Albion.Small;
   showReference.BackColor = Color.Transparent;
   showReference.SetBounds(2, 10, 220, 24);
   showReference.Checked = prefs.ShowReferenceCommands;
   showReference.Click += delegate { prefs.ShowReferenceCommands = showReference.Checked; SavePrefs(); RefreshFeatures(); };
   tools.Controls.Add(showReference);
  }

  void BuildDetailColumn(Panel host) {
   TableLayoutPanel grid = new TableLayoutPanel();
   grid.Dock = DockStyle.Fill; grid.ColumnCount = 1; grid.RowCount = 2;
   grid.Padding = new Padding(6, 10, 10, 10);
   grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
   host.Controls.Add(grid);

   detailHost = new BuffPanel();
   detailHost.Dock = DockStyle.Fill;
   detailHost.BackColor = Albion.PanelSoft;
   detailHost.Paint += DetailPaint;
   detailHost.Resize += delegate { LayoutDetail(); detailHost.Invalidate(); };
   grid.Controls.Add(detailHost, 0, 0);

   numericText = new TextBox();
   numericText.Font = Albion.Numeral;
   numericText.BackColor = Albion.PanelHi; numericText.ForeColor = Albion.GoldBright;
   numericText.BorderStyle = BorderStyle.FixedSingle;
   numericText.TextChanged += delegate { if (syncing) return; OnNumericEdited(); };
   detailHost.Controls.Add(numericText);

   selectBox = new ComboBox();
   selectBox.DropDownStyle = ComboBoxStyle.DropDownList;
   selectBox.BackColor = Albion.PanelHi; selectBox.ForeColor = Albion.Text;
   selectBox.FlatStyle = FlatStyle.Flat; selectBox.Font = Albion.Body;
   selectBox.SelectedIndexChanged += delegate { if (syncing) return; detailHost.Invalidate(); };
   detailHost.Controls.Add(selectBox);

   valueSlider = new AlbionSlider();
   valueSlider.Step = 1;
   valueSlider.ValueChanged += delegate { if (syncing) return; OnSliderMoved(); };
   detailHost.Controls.Add(valueSlider);

   toggleSwitch = new ToggleSwitch();
   toggleSwitch.OnText = "ENABLED"; toggleSwitch.OffText = "DISABLED";
   toggleSwitch.CheckedChanged += delegate { if (syncing) return; if (Selected() != null) { toggleDesiredState = toggleSwitch.Checked ? "on" : "off"; detailHost.Invalidate(); } };
   detailHost.Controls.Add(toggleSwitch);

   amountPadButton = new AlbionButton(); amountPadButton.Text = "EXACT AMOUNT…"; amountPadButton.Style = AlbionButtonStyle.Gold;
   amountPadButton.Click += delegate { OpenAmountPad(); };
   detailHost.Controls.Add(amountPadButton);

   executeButton = new AlbionButton(); executeButton.Text = "EXECUTE"; executeButton.Style = AlbionButtonStyle.Primary;
   executeButton.Click += delegate { RunSelected(); };
   detailHost.Controls.Add(executeButton);

   queueButton = new AlbionButton(); queueButton.Text = "+ QUEUE"; queueButton.Style = AlbionButtonStyle.Gold;
   queueButton.Click += delegate { QueueSelected(); };
   detailHost.Controls.Add(queueButton);

   favouriteButton = new AlbionButton(); favouriteButton.Text = "☆"; favouriteButton.Style = AlbionButtonStyle.Quiet;
   favouriteButton.Click += delegate { Favourite(); };
   detailHost.Controls.Add(favouriteButton);

   // Queue preview pump.
   queueHost = new BuffPanel();
   queueHost.Dock = DockStyle.Fill;
   queueHost.BackColor = Albion.Panel;
   queueHost.Paint += delegate(object s, PaintEventArgs e) {
    Albion.NineSlice(e.Graphics, "panel-border-002", new Rectangle(0, 0, queueHost.ClientSize.Width - 1, queueHost.ClientSize.Height - 1), Albion.Border, 12);
   };
   grid.Controls.Add(queueHost, 0, 1);

   TableLayoutPanel qgrid = new TableLayoutPanel();
   qgrid.Dock = DockStyle.Fill; qgrid.ColumnCount = 1; qgrid.RowCount = 2;
   qgrid.Padding = new Padding(10, 6, 10, 8);
   qgrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
   qgrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   queueHost.Controls.Add(qgrid);

   Panel qbar = new Panel(); qbar.Dock = DockStyle.Fill; qbar.BackColor = Color.Transparent;
   qgrid.Controls.Add(qbar, 0, 0);

   queueHeaderLabel = MakeLabel("COMMAND QUEUE", Albion.H3, Albion.Gold, 0, 2, 240, 24);
   qbar.Controls.Add(queueHeaderLabel);

   runQueueButton = new AlbionButton(); runQueueButton.Text = "RUN QUEUE"; runQueueButton.Style = AlbionButtonStyle.Primary;
   runQueueButton.Click += delegate { RunQueue(); };
   runQueueButton.SetBounds(300, 0, 118, 28);
   runQueueButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
   qbar.Controls.Add(runQueueButton);

   queueViewButton = new AlbionButton(); queueViewButton.Text = "REVIEW"; queueViewButton.Style = AlbionButtonStyle.Gold;
   queueViewButton.Click += delegate { OpenQueueManager(); };
   queueViewButton.SetBounds(424, 0, 92, 28);
   queueViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
   qbar.Controls.Add(queueViewButton);

   clearQueueButton = new AlbionButton(); clearQueueButton.Text = "CLEAR"; clearQueueButton.Style = AlbionButtonStyle.Quiet;
   clearQueueButton.Click += delegate { ClearQueue(); };
   clearQueueButton.SetBounds(522, 0, 84, 28);
   clearQueueButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
   qbar.Controls.Add(clearQueueButton);

   queueFlow = new FlowLayoutPanel();
   queueFlow.Dock = DockStyle.Fill;
   queueFlow.AutoScroll = true;
   queueFlow.BackColor = Color.Transparent;
   queueFlow.FlowDirection = FlowDirection.LeftToRight;
   queueFlow.WrapContents = true;
   qgrid.Controls.Add(queueFlow, 0, 1);
  }

  Panel BuildFooter() {
   Panel footer = new Panel();
   footer.Dock = DockStyle.Fill; footer.BackColor = Albion.Panel;
   footer.Paint += delegate(object s, PaintEventArgs e) {
    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
    using (Pen p = new Pen(Albion.GoldDim, 1.4f)) e.Graphics.DrawLine(p, 0, 0, footer.ClientSize.Width, 0);
   };

   status = new Label();
   status.Text = "Checking installation…";
   status.ForeColor = Albion.Muted; status.Font = Albion.Small; status.BackColor = Color.Transparent;
   status.AutoSize = false; status.AutoEllipsis = true;
   status.SetBounds(16, 4, 100, 34);
   status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
   status.TextAlign = ContentAlignment.MiddleLeft;
   footer.Controls.Add(status);

   toastLabel = new Label();
   toastLabel.Text = ""; toastLabel.TextAlign = ContentAlignment.MiddleRight;
   toastLabel.ForeColor = Albion.Good; toastLabel.Font = Albion.SmallBold; toastLabel.BackColor = Color.Transparent;
   toastLabel.AutoSize = false;
   toastLabel.SetBounds(100, 4, 300, 34);
   toastLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
   footer.Controls.Add(toastLabel);

   footer.Resize += delegate {
    toastLabel.Width = 320;
    toastLabel.Left = Math.Max(120, footer.ClientSize.Width - 336);
    status.Width = Math.Max(80, toastLabel.Left - 24);
   };
   return footer;
  }

  Label MakeLabel(string text, Font font, Color color, int x, int y, int w, int h) {
   Label l = new Label();
   l.Text = text; l.Font = font; l.ForeColor = color; l.BackColor = Color.Transparent;
   l.AutoSize = false; l.AutoEllipsis = true;
   l.SetBounds(x, y, w, h);
   l.TextAlign = ContentAlignment.MiddleLeft;
   return l;
  }

  // ---------------------------------------------------------------- lists

  void DrawCategoryItem(object sender, DrawItemEventArgs e) {
   ListBox lb = sender as ListBox;
   if (lb == null || e.Index < 0) return;
   bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
   bool hover = (e.State & DrawItemState.HotLight) == DrawItemState.HotLight;
   ListPaint.FillRow(e.Graphics, e.Bounds, selected, hover, Albion.Panel);
   string name = Convert.ToString(lb.Items[e.Index]);
   Albion.Draw(e.Graphics, name, selected ? Albion.BodyBold : Albion.Body,
    selected ? Albion.GoldBright : Albion.Text,
    new Rectangle(e.Bounds.X + 14, e.Bounds.Y, e.Bounds.Width - 64, e.Bounds.Height), ContentAlignment.MiddleLeft);
   Albion.Draw(e.Graphics, CountForCategory(name).ToString(), Albion.Tiny,
    selected ? Albion.Gold : Albion.Muted,
    new Rectangle(e.Bounds.Right - 50, e.Bounds.Y, 36, e.Bounds.Height), ContentAlignment.MiddleRight);
  }

  int CountForCategory(string name) {
   if (name == "All") return catalogue.Count(c => prefs.ShowReferenceCommands || !c.IsReferenceOnly);
   if (name == "Favourites") return catalogue.Count(c => prefs.Favourites.Contains(c.id));
   if (name == "Recent") return state.Recents == null ? 0 : state.Recents.Count;
   return catalogue.Count(c => c.group == name);
  }

  void DrawFeatureItem(object sender, DrawItemEventArgs e) {
   ListBox lb = sender as ListBox;
   if (lb == null || e.Index < 0) return;
   GameCommand c = lb.Items[e.Index] as GameCommand;
   bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
   bool hover = (e.State & DrawItemState.HotLight) == DrawItemState.HotLight;
   ListPaint.FillRow(e.Graphics, e.Bounds, selected, hover, Albion.Panel);
   if (c == null) return;
   Graphics g = e.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;

   // icon
   Color iconColor = selected ? Albion.GoldBright : Albion.GoldDim;
   Bitmap icon = SvgIcon.Get(Albion.IconDir, c.icon, 22, iconColor, 0);
   int iy = e.Bounds.Y + (e.Bounds.Height - 22) / 2;
   if (icon != null) g.DrawImage(icon, e.Bounds.X + 12, iy, 22, 22);

   bool fav = prefs.Favourites.Contains(c.id);
   string title = (fav ? "★  " : "") + c.title;
   Rectangle titleRect = new Rectangle(e.Bounds.X + 44, e.Bounds.Y + 4, e.Bounds.Width - 172, 20);
   Albion.Draw(g, title, selected ? Albion.BodyBold : Albion.Body,
    selected ? Albion.GoldBright : Albion.Text, titleRect, ContentAlignment.MiddleLeft);

   string sub = c.group + "  •  " + (c.IsToggle ? "toggle" : "action");
   Albion.Draw(g, sub, Albion.Tiny, Albion.Muted,
    new Rectangle(e.Bounds.X + 44, e.Bounds.Y + 22, e.Bounds.Width - 172, 16), ContentAlignment.MiddleLeft);

   // right badges
   string risk = c.risk == "high" ? "HIGH" : c.risk == "medium" ? "MED" : "LOW";
   int bw = TextRenderer.MeasureText(risk, Albion.Tiny).Width + 14;
   int bx = e.Bounds.Right - bw - 10;
   ListPaint.Badge(g, risk, Albion.Tiny, Albion.RiskColor(c.risk), bx, e.Bounds.Y + 6);

   string coop = c.coop == "safe" ? "SAFE" : c.coop == "local" ? "LOCAL" : c.coop == "unsafe" ? "RISK" : "UNV";
   int cw = TextRenderer.MeasureText(coop, Albion.Tiny).Width + 14;
   ListPaint.Badge(g, coop, Albion.Tiny, Albion.CoopColor(c.coop), bx - cw - 6, e.Bounds.Y + 6);

   string state = CurrentToggleState(c);
   if (state != null) {
    string st = state == "on" ? "ON" : "OFF";
    int sw = TextRenderer.MeasureText(st, Albion.Tiny).Width + 14;
    ListPaint.Badge(g, st, Albion.Tiny, state == "on" ? Albion.Good : Albion.Muted, bx - cw - sw - 12, e.Bounds.Y + 6);
   }
  }

  // ---------------------------------------------------------------- filtering

  public void PopulateCategories() {
   if (categories == null) return;
   syncing = true;
   try {
    categories.BeginUpdate();
    categories.Items.Clear();
    foreach (string s in groupNames) categories.Items.Add(s);
    categories.EndUpdate();
    int i = Array.IndexOf(groupNames, prefs.LastCategory);
    categories.SelectedIndex = i < 0 ? 0 : i;
   } finally { syncing = false; }
  }

  public void RefreshFeatures() {
   if (features == null || categories == null) return;
   GameCommand old = Selected();
   string category = categories.SelectedItem as string;
   if (string.IsNullOrEmpty(category)) category = "All";
   prefs.LastCategory = category;
   string needle = search == null ? "" : (search.Text ?? "").Trim();

   IEnumerable<GameCommand> matches = catalogue.Where(c =>
    !(!prefs.ShowReferenceCommands && c.IsReferenceOnly) &&
    (category == "All" || c.group == category || (category == "Favourites" && prefs.Favourites.Contains(c.id)) || (category == "Recent" && state.Recents != null && state.Recents.Contains(c.id))) &&
    (needle.Length == 0 || ((c.title ?? "") + " " + (c.summary ?? "") + " " + (c.group ?? "") + " " + (c.id ?? "")).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));

   string sort = sortBox == null ? "Name A–Z" : Convert.ToString(sortBox.SelectedItem);
   if (sort == "Risk") matches = matches.OrderBy(c => RiskRank(c.risk)).ThenBy(c => c.title);
   else if (sort == "Group") matches = matches.OrderBy(c => c.group).ThenBy(c => c.title);
   else if (sort == "Co-op") matches = matches.OrderBy(c => CoopRank(c.coop)).ThenBy(c => c.title);
   else matches = matches.OrderBy(c => c.IsReferenceOnly).ThenBy(c => c.title);

   List<GameCommand> list = matches.ToList();
   syncing = true;
   try {
    features.BeginUpdate();
    features.Items.Clear();
    foreach (GameCommand c in list) features.Items.Add(c);
    features.EndUpdate();
    if (old != null && list.Any(c => c.id == old.id)) features.SelectedItem = list.First(c => c.id == old.id);
    else if (list.Count > 0) features.SelectedIndex = 0;
   } finally { syncing = false; }
   InvalidateDetail();
   if (categories != null) categories.Invalidate();
  }

  static int RiskRank(string risk) { return string.Equals(risk, "high", StringComparison.OrdinalIgnoreCase) ? 0 : string.Equals(risk, "medium", StringComparison.OrdinalIgnoreCase) ? 1 : 2; }
  static int CoopRank(string coop) { return string.Equals(coop, "unsafe", StringComparison.OrdinalIgnoreCase) ? 0 : string.Equals(coop, "unverified", StringComparison.OrdinalIgnoreCase) ? 1 : 2; }

  public GameCommand Selected() { return features == null ? null : features.SelectedItem as GameCommand; }

  // ---------------------------------------------------------------- detail

  public void InvalidateDetail() {
   if (detailHost == null) return;
   SyncDetailEditors();
   LayoutDetail();
   detailHost.Invalidate();
   InvalidateChips();
  }

  void InvalidateChips() {
   if (headerChipGame != null) headerChipGame.Invalidate();
   if (headerChipScript != null) headerChipScript.Invalidate();
   if (headerChipCoop != null) headerChipCoop.Invalidate();
   if (headerChipInjector != null) headerChipInjector.Invalidate();
   if (toastLabel != null) {
    toastLabel.Text = toastText;
    toastLabel.ForeColor = toastColor;
   }
  }

  public void RefreshHeaderChips() {
   if (headerChipGame == null) return;
   string hash = "NOT FOUND";
   string target = Path.Combine(game, "Fable3.exe");
   if (File.Exists(target)) {
    try { hash = SHA256File(target).Equals(EXPECTED_HASH, StringComparison.OrdinalIgnoreCase) ? "PATCH 2" : "DIFFERENT"; }
    catch { hash = "UNREADABLE"; }
   }
   headerChipGame.Value = hash;
   headerChipGame.Accent = hash == "PATCH 2" ? Albion.Good : hash == "NOT FOUND" ? Albion.Muted : Albion.Gold;
   bool ready = ReadyScript();
   headerChipScript.Value = ready ? "READY" : "NOT PREPARED";
   headerChipScript.Accent = ready ? Albion.Good : Albion.Gold;
   headerChipCoop.Value = prefs.CoopSafe ? "ON" : "OFF";
   headerChipCoop.Accent = prefs.CoopSafe ? Albion.Good : Albion.Danger;
   bool injector = InjectorPresent();
   headerChipInjector.Value = injector ? "DETECTED" : "MISSING";
   headerChipInjector.Accent = injector ? Albion.Good : Albion.Danger;
   InvalidateChips();
  }

  bool GateBlocked(GameCommand c) { return GateReason(c) != null; }

  // Mirrors the checks in Trainer.Execute so the UI can explain a locked command
  // before the user clicks. Execution still re-checks every gate server-side.
  string GateReason(GameCommand c) {
   if (c == null) return null;
   if (prefs.CoopSafe && c.id != "test-message")
    return "Co-op protection is ON — only the harmless script test may run.";
   if (c.IsHighRisk && !prefs.AllowRisk)
    return "High-risk gate is OFF — back up saves before enabling it.";
   return null;
  }

  void SyncDetailEditors() {
   if (numericText == null || selectBox == null || valueSlider == null || toggleSwitch == null) return;
   GameCommand c = Selected();
   bool has = c != null;
   bool isToggle = has && c.IsToggle;
   Parameter p = has && c.@params != null && c.@params.Count > 0 ? c.@params[0] : null;

   syncing = true;
   try {
    numericText.Visible = has && !isToggle && p != null && p.type != "select";
    selectBox.Visible = has && !isToggle && p != null && p.type == "select";
    valueSlider.Visible = has && !isToggle && p != null && p.type != "select" && p.max > p.min;
    amountPadButton.Visible = has && !isToggle && p != null && p.type != "select";
    toggleSwitch.Visible = isToggle;
    bool blocked = has && GateBlocked(c);
    executeButton.Enabled = has && !blocked;
    queueButton.Enabled = has;
    favouriteButton.Enabled = has;

    if (has) {
     favouriteButton.Text = prefs.Favourites.Contains(c.id) ? "★" : "☆";
     executeButton.Text = blocked ? "LOCKED BY GATE" : isToggle ? "APPLY" : (c.IsHighRisk ? "EXECUTE (HIGH RISK)" : "EXECUTE");
    }

    if (isToggle) {
     string cur = CurrentToggleState(c);
     bool on = cur == null ? string.Equals(c.state, "on", StringComparison.OrdinalIgnoreCase) : string.Equals(cur, "on", StringComparison.OrdinalIgnoreCase);
     toggleSwitch.Checked = on;
     toggleDesiredState = on ? "on" : "off";
    } else if (p != null) {
     if (p.type == "select") {
      selectBox.Items.Clear();
      if (p.options != null) foreach (Choice o in p.options) selectBox.Items.Add(o);
      int sel = 0;
      for (int i = 0; i < selectBox.Items.Count; i++) if (((Choice)selectBox.Items[i]).value == p.value) sel = i;
      if (selectBox.Items.Count > 0) selectBox.SelectedIndex = sel;
     } else {
      double v = p.value;
      if (v < p.min) v = p.min;
      if (v > p.max) v = p.max;
      numericText.Text = v.ToString("0.#####", CultureInfo.InvariantCulture);
      valueSlider.Minimum = p.min;
      valueSlider.Maximum = p.max;
      valueSlider.Step = p.step > 0 ? p.step : 1;
      valueSlider.Value = v;
     }
    }
   } finally { syncing = false; }
  }

  void OnNumericEdited() {
   GameCommand c = Selected();
   if (c == null || c.@params == null || c.@params.Count == 0) return;
   Parameter p = c.@params[0];
   if (p.type == "select") return;
   double v;
   if (!Double.TryParse(numericText.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return;
   if (Double.IsNaN(v) || Double.IsInfinity(v)) return;
   if (v < p.min || v > p.max) return;
   syncing = true;
   try { valueSlider.Value = v; } finally { syncing = false; }
  }

  void OnSliderMoved() {
   GameCommand c = Selected();
   if (c == null || c.@params == null || c.@params.Count == 0) return;
   Parameter p = c.@params[0];
   if (p.type == "select") return;
   double v = valueSlider.Value;
   if (p.step > 0) v = Math.Round(v / p.step) * p.step;
   if (v < p.min) v = p.min;
   if (v > p.max) v = p.max;
   syncing = true;
   try { numericText.Text = v.ToString("0.#####", CultureInfo.InvariantCulture); } finally { syncing = false; }
  }

  public double ParameterValue(GameCommand c) {
   if (c == null || c.@params == null || c.@params.Count == 0) return 0;
   Parameter p = c.@params[0];
   double n;
   if (p.type == "select") {
    Choice choice = selectBox == null ? null : selectBox.SelectedItem as Choice;
    if (choice == null) throw new Exception("Choose a supported setting first.");
    n = choice.value;
   } else {
    string text = numericText == null ? "" : numericText.Text;
    if (!Double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out n)) throw new Exception("Enter a valid number.");
   }
   if (Double.IsNaN(n) || Double.IsInfinity(n)) throw new Exception("Enter a finite value.");
   if (p.type == "select") {
    if (p.options == null || !p.options.Any(o => o.value == n)) throw new Exception("Select a supported option value.");
   } else if (n < p.min || n > p.max) throw new Exception("Value must be between " + p.min.ToString("0.#####", CultureInfo.InvariantCulture) + " and " + p.max.ToString("0.#####", CultureInfo.InvariantCulture) + ".");
   if (p.type != "select" && (c.id == "gold" || c.id == "treasury" || c.id == "seals") && n != Math.Truncate(n)) throw new Exception("Gold and Guild Seals use whole numbers only.");
   return n;
  }

  public void AdjustSelectedValue(int direction) {
   GameCommand c = Selected();
   if (c == null || c.@params == null || c.@params.Count == 0) return;
   Parameter p = c.@params[0];
   if (p.type == "select") {
    if (selectBox.Items.Count > 0) selectBox.SelectedIndex = Math.Max(0, Math.Min(selectBox.Items.Count - 1, selectBox.SelectedIndex + direction));
    return;
   }
   double n;
   if (!Double.TryParse(numericText.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out n)) n = p.value;
   double step = p.step > 0 ? p.step : 1;
   n = Math.Max(p.min, Math.Min(p.max, n + step * direction));
   numericText.Text = n.ToString("0.#####", CultureInfo.InvariantCulture);
   detailHost.Invalidate();
  }

  void LayoutDetail() {
   if (detailHost == null || numericText == null || executeButton == null) return;
   int W = detailHost.ClientSize.Width;
   int pad = 26;
   int w = Math.Max(200, W - pad - 26);
   int y = 18;
   rTitle = new Rectangle(pad, y, w, 34); y += 40;
   rBadges = new Rectangle(pad, y, w, 22); y += 30;
   rSummary = new Rectangle(pad, y, w, 48); y += 54;
   rDivider = new Rectangle(pad, y, w, 2); y += 12;
   rParamLabel = new Rectangle(pad, y, w, 22); y += 26;
   detailParamRowY = y;
   y += 38; // numericText / selectBox row
   y += 40; // slider row
   y += 38; // amount pad row
   y += 46; // action buttons row
   y += 44; // toggle row
   rNotes = new Rectangle(pad, y, w, Math.Max(30, detailHost.ClientSize.Height - y - 12));

   int editorW = Math.Min(300, w - 150);
   numericText.SetBounds(pad, detailParamRowY, editorW, 32);
   selectBox.SetBounds(pad, detailParamRowY, editorW, 32);
   amountPadButton.SetBounds(pad + editorW + 12, detailParamRowY, 140, 32);
   valueSlider.SetBounds(pad, detailParamRowY + 40, w, 34);

   int bw = w;
   int ex = pad;
   int btnY = detailParamRowY + 116;
   executeButton.SetBounds(ex, btnY, Math.Max(160, bw - 210), 40);
   queueButton.SetBounds(ex + Math.Max(160, bw - 210) + 8, btnY, 104, 40);
   favouriteButton.SetBounds(ex + Math.Max(160, bw - 210) + 118, btnY, 60, 40);

   toggleSwitch.SetBounds(pad, detailParamRowY + 96, 150, 36);
  }

  void DetailPaint(object sender, PaintEventArgs e) {
   Graphics g = e.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   Rectangle client = detailHost.ClientRectangle;
   if (client.Width <= 4 || client.Height <= 4) return;
   using (SolidBrush b = new SolidBrush(Albion.PanelSoft)) g.FillRectangle(b, client);
   Rectangle frame = new Rectangle(4, 4, client.Width - 9, client.Height - 9);
   using (SolidBrush b = new SolidBrush(Albion.Panel)) Albion.FillRounded(g, b, frame, 8);
   Albion.NineSlice(g, "panel-border-010", frame, Albion.GoldDim, 14);

   GameCommand c = Selected();
   if (c == null) {
    Albion.Draw(g, "SELECT A COMMAND", Albion.H2, Albion.GoldDim, new Rectangle(26, 40, client.Width - 52, 30), ContentAlignment.MiddleLeft);
    Albion.Draw(g, "Choose a category on the left, then pick a feature. Nothing runs until you confirm.", Albion.Body, Albion.Muted,
     new Rectangle(26, 76, client.Width - 52, 60), ContentAlignment.TopLeft);
    DrawToast(g);
    return;
   }

   Albion.Draw(g, c.title, Albion.H1, Albion.GoldBright, rTitle, ContentAlignment.MiddleLeft);

   // badges
   int bx = rBadges.X;
   bx = DrawDetailBadge(g, bx, rBadges.Y, Albion.EvidenceLabel(c.evidence), Albion.EvidenceColor(c.evidence));
   bx = DrawDetailBadge(g, bx, rBadges.Y, Albion.CoopLabel(c.coop), Albion.CoopColor(c.coop));
   bx = DrawDetailBadge(g, bx, rBadges.Y, "RISK: " + c.risk.ToUpperInvariant(), Albion.RiskColor(c.risk));
   if (c.IsHighRisk) bx = DrawDetailBadge(g, bx, rBadges.Y, "SAVE-CHANGING", Albion.Danger);
   string gate = GateReason(c);
   if (gate != null) DrawDetailBadge(g, bx, rBadges.Y, "GATE LOCKED", Albion.Danger);

   TextRenderer.DrawText(g, c.summary ?? "", Albion.Body, rSummary, Albion.Text, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
   using (Pen p = new Pen(Albion.Border, 1f)) g.DrawLine(p, rDivider.X, rDivider.Y, rDivider.Right, rDivider.Y);

   Parameter prm = c.@params != null && c.@params.Count > 0 ? c.@params[0] : null;
   if (c.IsToggle) {
    Albion.Draw(g, "TOGGLE STATE", Albion.H3, Albion.Gold, rParamLabel, ContentAlignment.MiddleLeft);
    Albion.Draw(g, "Apply the twin command that matches the switch position.", Albion.Small, Albion.Muted,
     new Rectangle(rParamLabel.X, rParamLabel.Y + 4, rParamLabel.Width - 170, 20), ContentAlignment.MiddleRight);
   } else if (prm != null) {
    string range = prm.type == "select" ? "select" : (prm.min.ToString("0.#####", CultureInfo.InvariantCulture) + " – " + prm.max.ToString("0.#####", CultureInfo.InvariantCulture) + "  step " + (prm.step > 0 ? prm.step.ToString("0.#####", CultureInfo.InvariantCulture) : "1"));
    Albion.Draw(g, (prm.label ?? "VALUE").ToUpperInvariant(), Albion.H3, Albion.Gold, rParamLabel, ContentAlignment.MiddleLeft);
    Albion.Draw(g, range, Albion.Small, Albion.Muted, new Rectangle(rParamLabel.X, rParamLabel.Y, rParamLabel.Width, 22), ContentAlignment.MiddleRight);
   } else {
    Albion.Draw(g, "NO PARAMETERS", Albion.H3, Albion.Gold, rParamLabel, ContentAlignment.MiddleLeft);
   }

   // notes / provenance
   string notes = c.notes;
   if (string.IsNullOrEmpty(notes)) notes = "Source-backed from the extracted Fable III debug menu. Not verified live in-game by this build.";
   StringBuilder sb = new StringBuilder();
   sb.Append(notes);
   sb.Append("\r\nEvidence: ").Append(c.evidence).Append("  •  Co-op: ").Append(c.coop);
   sb.Append("\r\n").Append(LastSentText(c));
   if (gate != null) sb.Append("\r\nGATE: ").Append(gate);
   TextRenderer.DrawText(g, sb.ToString(), Albion.Small, rNotes, Albion.Muted, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

   DrawToast(g);
  }

  int DrawDetailBadge(Graphics g, int x, int y, string text, Color color) {
   Size s = TextRenderer.MeasureText(text, Albion.Tiny);
   Rectangle r = new Rectangle(x, y, s.Width + 16, 22);
   using (SolidBrush b = new SolidBrush(Color.FromArgb(52, color))) Albion.FillRounded(g, b, r, 4);
   using (Pen p = new Pen(Color.FromArgb(170, color), 1f)) Albion.DrawRounded(g, p, r, 4);
   TextRenderer.DrawText(g, text, Albion.Tiny, r, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
   return r.Right + 8;
  }

  void DrawToast(Graphics g) {
   if (toastText == null || toastText.Length == 0 || DateTime.UtcNow > toastUntil) return;
   Size s = TextRenderer.MeasureText(toastText, Albion.SmallBold);
   Rectangle r = new Rectangle(detailHost.ClientSize.Width - s.Width - 44, detailHost.ClientSize.Height - 38, s.Width + 24, 28);
   using (SolidBrush b = new SolidBrush(Color.FromArgb(190, Albion.Leather))) Albion.FillRounded(g, b, r, 6);
   using (Pen p = new Pen(toastColor, 1.2f)) Albion.DrawRounded(g, p, r, 6);
   TextRenderer.DrawText(g, toastText, Albion.SmallBold, r, toastColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
  }

  // ---------------------------------------------------------------- quick pads

  public void OpenGoldPad() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   if (categories != null) {
    int i = Array.IndexOf(groupNames, "Kingdom");
    if (i >= 0) categories.SelectedIndex = i;
   }
   GameCommand target = catalogue.FirstOrDefault(c => c.id == "gold");
   if (target == null) { Error("Gold command missing from catalogue."); return; }
   if (features.Items.Contains(target)) features.SelectedItem = target;
   InvalidateDetail();
   OpenAmountPad();
  }

  public void OpenAmountPad() {
    if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
    GameCommand c = Selected();
    if (c == null || c.@params == null || c.@params.Count == 0 || c.@params[0].type == "select") {
     Notice("Choose a numeric feature such as Add hero gold, Royal treasury or Guild Seals first.", "Exact amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
     return;
    }
    Parameter param = c.@params[0];
    if (param.max > 1000000000 || param.min < 0 || param.min > param.max) { Notice("This parameter cannot use the exact amount pad.", "CROWN"); return; }

    PopupForm f = new PopupForm();
    f.Text = "CROWN â€” Exact amount: " + c.title;
    f.ClientSize = new Size(600, 560);
    f.FormBorderStyle = FormBorderStyle.FixedDialog;
    f.MaximizeBox = false; f.MinimizeBox = false;
    f.ShowInTaskbar = false; f.KeyPreview = true;
    f.BackColor = Albion.Leather; f.ForeColor = Albion.Text; f.Font = Albion.Body;
    f.StartPosition = FormStartPosition.CenterParent;

    f.Controls.Add(MakeLabel("EXACT AMOUNT", Albion.H1, Albion.GoldBright, 22, 14, 500, 34));
    f.Controls.Add(MakeLabel(c.title + " â€” This sets the parameter value for EXECUTE or QUEUE. It does not change the game until you confirm.", Albion.Small, Albion.Muted, 22, 50, 550, 40));

    // Live Lua preview
    TextBox luaPreview = new TextBox();
    luaPreview.SetBounds(22, 90, 552, 80);
    luaPreview.Multiline = true; luaPreview.ReadOnly = true; luaPreview.ScrollBars = ScrollBars.Vertical;
    luaPreview.BackColor = Albion.LeatherHi; luaPreview.ForeColor = Albion.GoldDim; luaPreview.Font = new Font("Consolas", 8.5f);
    luaPreview.BorderStyle = BorderStyle.FixedSingle;
    f.Controls.Add(luaPreview);

    NumericUpDown amount = new NumericUpDown();
    amount.SetBounds(22, 180, 552, 38);
    amount.Minimum = (decimal)param.min;
    amount.Maximum = (decimal)param.max;
    amount.DecimalPlaces = param.step < 1 ? 2 : 0;
    amount.Increment = (decimal)Math.Max(param.step, 0.01);
    amount.ThousandsSeparator = true;
    amount.Font = Albion.Numeral;
    amount.BackColor = Albion.PanelHi; amount.ForeColor = Albion.GoldBright;
    double initial = param.value;
    try { initial = ParameterValue(c); } catch { }
    amount.Value = (decimal)Math.Max(param.min, Math.Min(param.max, initial));
    f.Controls.Add(amount);

    Action refreshPreview = delegate {
     try {
      string preview = Compile(c, (double)amount.Value);
      luaPreview.Text = "-- Preview of the Lua that will be sent:\r\n" + preview;
     } catch (Exception ex) { luaPreview.Text = "-- " + ex.Message; }
    };
    amount.ValueChanged += delegate { refreshPreview(); };
    refreshPreview();

    f.Controls.Add(MakeLabel("QUICK PRESETS â€” CLICK TO APPLY, THEN USE OR QUEUE", Albion.H3, Albion.Gold, 22, 226, 550, 22));
    FlowLayoutPanel quicks = new FlowLayoutPanel();
    quicks.SetBounds(20, 254, 560, 96);
    quicks.WrapContents = true; quicks.AutoScroll = false; quicks.BackColor = Color.Transparent;
    f.Controls.Add(quicks);

    // Smart presets based on the command type
    int[] quickVals = GetSmartPresets(c.id, param);
    foreach (int quickVal in quickVals) {
     if (quickVal < param.min || quickVal > param.max) continue;
     int captured = quickVal;
     AlbionButton b = new AlbionButton();
     b.Text = quickVal.ToString("N0"); b.Style = AlbionButtonStyle.Quiet; b.Font = Albion.SmallBold;
     b.Size = new Size(96, 34); b.Margin = new Padding(4);
     b.Click += delegate { amount.Value = captured; };
     quicks.Controls.Add(b);
    }

    AlbionSlider fine = new AlbionSlider();
    fine.SetBounds(22, 358, 552, 40);
    bool fineUsable = param.max <= 100000 && param.min >= 0 && (param.max - param.min) > 10;
    if (fineUsable) { fine.Minimum = param.min; fine.Maximum = param.max; fine.Step = Math.Max(param.step, 1); fine.Value = (double)amount.Value; }
    fine.Visible = fineUsable;
    f.Controls.Add(fine);

    Label fineLabel = MakeLabel("FINE CONTROL â€” drag or arrow keys, mouse wheel also works", Albion.Small, Albion.Muted, 22, 396, 550, 20);
    fineLabel.Visible = fineUsable;
    f.Controls.Add(fineLabel);

    if (fineUsable) {
     fine.ValueChanged += delegate { decimal v = (decimal)fine.Value; if (v < amount.Minimum) v = amount.Minimum; if (v > amount.Maximum) v = amount.Maximum; amount.Value = v; };
     amount.ValueChanged += delegate { double v = (double)amount.Value; if (v >= fine.Minimum && v <= fine.Maximum) fine.Value = v; };
    }

    // Parameter info
    f.Controls.Add(MakeLabel(
     "Range: " + param.min.ToString("0.#####", CultureInfo.InvariantCulture) + " â€“ " + param.max.ToString("0.#####", CultureInfo.InvariantCulture) +
     "  |  Step: " + (param.step > 0 ? param.step.ToString("0.#####", CultureInfo.InvariantCulture) : "1") +
     (param.type == "number" ? "" : "  (" + param.type + ")"),
     Albion.Tiny, Albion.Muted, 22, 420, 550, 20));

    AlbionButton use = new AlbionButton(); use.Text = "USE VALUE"; use.Style = AlbionButtonStyle.Gold;
    use.SetBounds(22, 450, 160, 44);
    use.Click += delegate { SetEditorValue(amount.Value); f.Close(); };
    f.Controls.Add(use);

    AlbionButton queueIt = new AlbionButton(); queueIt.Text = "USE + QUEUE"; queueIt.Style = AlbionButtonStyle.Primary;
    queueIt.SetBounds(190, 450, 190, 44);
    queueIt.Click += delegate { SetEditorValue(amount.Value); QueueSelected(); f.Close(); };
    f.Controls.Add(queueIt);

    AlbionButton cancel = new AlbionButton(); cancel.Text = "CANCEL"; cancel.Style = AlbionButtonStyle.Ghost;
    cancel.SetBounds(392, 450, 180, 44);
    cancel.Click += delegate { f.Close(); };
    f.Controls.Add(cancel);

    f.Controls.Add(MakeLabel("KEYBOARD:  â†/â†’ adjust by step   â†‘/â†“ adjust by 10Ã—step   Home/End jump to min/max   Enter = USE VALUE   Ctrl+Enter = USE + QUEUE   Esc = CANCEL", Albion.Tiny, Albion.Muted, 22, 500, 550, 28));
    f.Controls.Add(MakeLabel("PAD:  LB/RB Â±1 step  â€¢  LT/RT hold for 10Ã—/100Ã—  â€¢  D-pad Left/Right presets  â€¢  A = USE VALUE  â€¢  X = USE + QUEUE  â€¢  B = CANCEL", Albion.Tiny, Albion.Muted, 22, 528, 550, 28));

    int quickIndex = -1;
    RegisterPad(f, delegate(Native.XState st, ushort press) {
     int multiplier = st.pad.rt >= 180 ? 100 : st.pad.lt >= 180 ? 10 : 1;
     double step = (double)amount.Increment * multiplier;
     if ((press & Native.PAD_LB) != 0) amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)step);
     if ((press & Native.PAD_RB) != 0) amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)step);
     if ((press & Native.PAD_DPAD_UP) != 0) amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)(1000 * multiplier));
     if ((press & Native.PAD_DPAD_DOWN) != 0) amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)(1000 * multiplier));
     if ((press & 0x000C) != 0 && quicks.Controls.Count > 0) {
      quickIndex = Math.Max(0, Math.Min(quicks.Controls.Count - 1, quickIndex + (((press & Native.PAD_DPAD_RIGHT) != 0) ? 1 : -1)));
      ((AlbionButton)quicks.Controls[quickIndex]).PerformClick();
      quicks.Controls[quickIndex].Focus();
     }
     if ((press & Native.PAD_A) != 0) { use.PerformClick(); return; }
     if ((press & Native.PAD_X) != 0) { queueIt.PerformClick(); return; }
    });

    // Keyboard shortcuts for the amount pad
    f.KeyDown += delegate(object s, KeyEventArgs e) {
     double step = (double)amount.Increment;
     if (e.Control) step *= 10;
     if (e.Shift) step *= 100;
     if (e.KeyCode == Keys.Left) { amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)step); e.Handled = true; }
     else if (e.KeyCode == Keys.Right) { amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)step); e.Handled = true; }
     else if (e.KeyCode == Keys.Up) { amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)(step * 10)); e.Handled = true; }
     else if (e.KeyCode == Keys.Down) { amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)(step * 10)); e.Handled = true; }
     else if (e.KeyCode == Keys.Home) { amount.Value = amount.Minimum; e.Handled = true; }
     else if (e.KeyCode == Keys.End) { amount.Value = amount.Maximum; e.Handled = true; }
     else if (e.KeyCode == Keys.Enter && e.Control) { queueIt.PerformClick(); e.Handled = true; }
     else if (e.KeyCode == Keys.Enter) { use.PerformClick(); e.Handled = true; }
     else if (e.KeyCode == Keys.Escape) { cancel.PerformClick(); e.Handled = true; }
    };

    f.AcceptButton = use; f.CancelButton = cancel;
    ShowPopup(f);
   }

   int[] GetSmartPresets(string commandId, Parameter p) {
    // Command-specific smart presets
    switch (commandId) {
     case "gold": return new int[] { 1000, 5000, 10000, 25000, 50000, 100000, 250000, 500000, 1000000, 5000000 };
     case "treasury": return new int[] { 50000, 100000, 250000, 500000, 1000000, 2500000, 5000000, 10000000 };
     case "seals": return new int[] { 10, 50, 100, 250, 500, 1000, 2500, 5000, 10000 };
     case "renown": return new int[] { 1000, 5000, 10000, 25000, 50000, 100000 };
     case "xp-strength": case "xp-skill": case "xp-will": case "xp-general": return new int[] { 10000, 50000, 100000, 250000, 500000, 1000000 };
     case "followers-up": case "followers-down": return new int[] { 5000, 10000, 25000, 50000, 100000 };
     case "spawn-wolves": case "spawn-renegades": case "spawn-minions": return new int[] { 1, 2, 3, 5, 8, 10, 15, 20 };
     case "spawn-dervish": return new int[] { 1, 2, 3, 5, 8, 10 };
     case "morality": case "purity": case "primal": return new int[] { -1000, -500, -100, 0, 100, 500, 1000 };
     default:
      // Generic presets based on range
      double range = p.max - p.min;
      if (range <= 20) return Enumerable.Range((int)Math.Ceiling(p.min), (int)range + 1).ToArray();
      if (range <= 100) return new int[] { (int)p.min, (int)(p.min + range * 0.25), (int)(p.min + range * 0.5), (int)(p.min + range * 0.75), (int)p.max };
      return new int[] { 1, 10, 50, 100, 500, 1000, 5000, 10000, 25000, 50000, 100000, 1000000 };
    }
   }void SetEditorValue(decimal value) {
   numericText.Text = value.ToString(CultureInfo.InvariantCulture);
   detailHost.Invalidate();
  }

  // ---------------------------------------------------------------- help

  public void ShowHelp() {
   if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
   StringBuilder s = new StringBuilder();
   s.AppendLine("KEYBOARD");
   s.AppendLine("  F8  show / hide the menu      F1  this help      Esc  return to game");
   s.AppendLine("  Ctrl+F search      Ctrl+G exact gold      Ctrl+Q queue review");
   s.AppendLine("  Ctrl+L research library      Ctrl+I item vault      Ctrl+D diagnostics");
   s.AppendLine("  Enter  queue the selected command      Ctrl+Enter  run the queue");
   s.AppendLine();
   s.AppendLine("XBOX PAD (no game input hooks; menu must be focused)");
   s.AppendLine("  L3 + R3 together : toggle the menu");
   s.AppendLine("  D-pad Up/Down    : features        D-pad Left/Right : categories");
   s.AppendLine("  A                : select / open exact amount, otherwise queue");
   s.AppendLine("  X : favourite      Y (hold) : run selected      Start (hold) : run queue");
   s.AppendLine("  Back / View      : review the queue");
   s.AppendLine("  LB / RB          : change the value (hold LT x10, RT x100)");
   s.AppendLine("  Tap B            : cancel / back   Hold B 0.65s : close the whole menu");
   s.AppendLine();
   s.AppendLine("SAFETY MODEL");
   s.AppendLine("  This trainer never edits Fable3.exe, xlive.dll, DLC or saves.");
   s.AppendLine("  It writes a marked Lua payload to the registered MyMod script path and");
   s.AppendLine("  clears it again. The injector reads that script about once per 60 world");
   s.AppendLine("  frames, so delivery is best-effort, not exactly-once. Keep co-op");
   s.AppendLine("  protection ON for XLLN sessions and always test on a spare save.");
   Notice(s.ToString(), "CROWN's ALBION — Controls", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }
 }
}
