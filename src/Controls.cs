// CROWN's ALBION v2.0 — lightweight themed controls (WinForms, owner-drawn).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROWNsAlbion {

 public enum AlbionButtonStyle { Primary, Ghost, Danger, Quiet, Gold }

 public class AlbionButton : Button {
  public AlbionButtonStyle Style = AlbionButtonStyle.Ghost;
  bool hover, down;

  public AlbionButton() {
   SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
   FlatStyle = FlatStyle.Flat;
   FlatAppearance.BorderSize = 0;
   BackColor = Color.Transparent;
   ForeColor = Albion.Text;
   Font = Albion.BodyBold;
   Cursor = Cursors.Hand;
   UseVisualStyleBackColor = false;
   TabStop = true;
  }

  protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
  protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
  protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
  protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
  protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

  protected override void OnPaint(PaintEventArgs pe) {
   Graphics g = pe.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
   Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
   Color fill, border, text;
   switch (Style) {
    case AlbionButtonStyle.Primary:
     fill = hover ? Color.FromArgb(226, 188, 92) : Albion.Gold; border = Albion.GoldBright; text = Color.FromArgb(38, 28, 12); break;
    case AlbionButtonStyle.Gold:
     fill = hover ? Color.FromArgb(95, 76, 36) : Color.FromArgb(72, 57, 28); border = Albion.Gold; text = Albion.GoldBright; break;
    case AlbionButtonStyle.Danger:
     fill = hover ? Color.FromArgb(150, 62, 52) : Albion.DangerSoft; border = Albion.Danger; text = Color.FromArgb(245, 222, 214); break;
    case AlbionButtonStyle.Quiet:
     fill = hover ? Color.FromArgb(50, 41, 31) : Color.FromArgb(36, 30, 23); border = Color.FromArgb(62, 51, 37); text = Albion.Muted; break;
    default:
     fill = hover ? Color.FromArgb(62, 50, 35) : Color.FromArgb(46, 37, 27); border = Color.FromArgb(110, 87, 48); text = Albion.Text; break;
   }
   if (!Enabled) { fill = Color.FromArgb(34, 29, 23); border = Color.FromArgb(54, 47, 38); text = Color.FromArgb(110, 101, 85); }
   else if (down) { fill = ControlPaint.Dark(fill, 0.08f); }
   using (SolidBrush b = new SolidBrush(fill)) Albion.FillRounded(g, b, r, 5);
   using (Pen p = new Pen(border, 1.2f)) Albion.DrawRounded(g, p, r, 5);
   if (Focused && Enabled) { using (Pen p = new Pen(Color.FromArgb(140, Albion.GoldBright), 1f)) Albion.DrawRounded(g, p, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), 4); }
   Rectangle textRect = new Rectangle(4, 0, Width - 8, Height);
   TextRenderer.DrawText(g, Text, Font, textRect, text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
  }
 }

 public class ToggleSwitch : Control {
  bool _checked;
  string onText = "ON", offText = "OFF";
  public event EventHandler CheckedChanged;
  public bool ReadOnlyMode;

  public ToggleSwitch() {
   SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
   BackColor = Color.Transparent;
   Cursor = Cursors.Hand;
   Size = new Size(112, 34);
   TabStop = true;
  }

  public bool Checked { get { return _checked; } set { if (_checked != value) { _checked = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); } } }
  public string OnText { get { return onText; } set { onText = value; Invalidate(); } }
  public string OffText { get { return offText; } set { offText = value; Invalidate(); } }

  protected override void OnMouseUp(MouseEventArgs e) { if (!ReadOnlyMode && Enabled) { Checked = !Checked; } base.OnMouseUp(e); }
  protected override void OnKeyDown(KeyEventArgs e) { if (!ReadOnlyMode && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)) { Checked = !Checked; e.Handled = true; } base.OnKeyDown(e); }
  protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
  protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

  protected override void OnPaint(PaintEventArgs pe) {
   Graphics g = pe.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   Rectangle track = new Rectangle(0, 2, Width - 1, Height - 5);
   Color fill = _checked ? Color.FromArgb(96, 132, 84) : Color.FromArgb(54, 45, 34);
   Color border = _checked ? Albion.Good : Color.FromArgb(92, 76, 52);
   if (!Enabled) { fill = Color.FromArgb(40, 34, 27); border = Color.FromArgb(60, 52, 42); }
   using (SolidBrush b = new SolidBrush(fill)) Albion.FillRounded(g, b, track, track.Height / 2);
   using (Pen p = new Pen(border, 1.2f)) Albion.DrawRounded(g, p, track, track.Height / 2);
   int knobSize = Height - 11;
   int knobX = _checked ? Width - knobSize - 6 : 5;
   Rectangle knob = new Rectangle(knobX, (Height - knobSize) / 2, knobSize, knobSize);
   using (SolidBrush b = new SolidBrush(_checked ? Albion.GoldBright : Color.FromArgb(150, 138, 118))) g.FillEllipse(b, knob);
   string label = _checked ? onText : offText;
   Color tc = _checked ? Color.FromArgb(240, 250, 235) : Color.FromArgb(190, 178, 156);
   Rectangle textRect = _checked ? new Rectangle(8, 0, Width - knobSize - 16, Height) : new Rectangle(knobSize + 8, 0, Width - knobSize - 14, Height);
   TextRenderer.DrawText(g, label, Albion.SmallBold, textRect, tc, TextFormatFlags.VerticalCenter | (_checked ? TextFormatFlags.Left : TextFormatFlags.Left) | TextFormatFlags.NoPrefix);
   if (Focused && Enabled) { using (Pen p = new Pen(Color.FromArgb(150, Albion.GoldBright), 1f)) Albion.DrawRounded(g, p, new Rectangle(0, 0, Width - 1, Height - 1), 6); }
  }
 }

 public class AlbionSlider : Control {
  double _min, _max, _value, _step = 1;
  public event EventHandler ValueChanged;
  bool dragging;
  bool suppress;

  public AlbionSlider() {
   SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
   BackColor = Color.Transparent;
   Height = 34;
   TabStop = true;
  }

  public double Minimum { get { return _min; } set { _min = value; if (_max <= _min) _max = _min + 1; Clamp(); Invalidate(); } }
  public double Maximum { get { return _max; } set { _max = value; if (_max <= _min) _min = _max - 1; Clamp(); Invalidate(); } }
  public double Step { get { return _step; } set { _step = value <= 0 ? 0.01 : value; } }
  public double Value {
   get { return _value; }
   set {
    double v = Math.Max(_min, Math.Min(_max, value));
    if (Math.Abs(v - _value) > 1e-9) { _value = v; Invalidate(); if (!suppress && ValueChanged != null) ValueChanged(this, EventArgs.Empty); }
   }
  }

  void Clamp() { _value = Math.Max(_min, Math.Min(_max, _value)); }

  int KnobX() {
   if (_max <= _min) return 8;
   double f = (_value - _min) / (_max - _min);
   return 8 + (int)Math.Round(f * (Width - 17));
  }

  void SetFromMouse(int x) {
   double f = (double)(x - 8) / Math.Max(1, Width - 17);
   f = Math.Max(0, Math.Min(1, f));
   double v = _min + f * (_max - _min);
   if (_step > 0) v = Math.Round(v / _step) * _step;
   suppress = true; Value = v; suppress = false;
   if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
  }

  protected override void OnMouseDown(MouseEventArgs e) { dragging = true; SetFromMouse(e.X); Focus(); base.OnMouseDown(e); }
  protected override void OnMouseMove(MouseEventArgs e) { if (dragging) SetFromMouse(e.X); base.OnMouseMove(e); }
  protected override void OnMouseUp(MouseEventArgs e) { dragging = false; base.OnMouseUp(e); }
  protected override void OnMouseWheel(MouseEventArgs e) { Value = _value + (e.Delta > 0 ? _step : -_step); base.OnMouseWheel(e); }
  protected override void OnKeyDown(KeyEventArgs e) {
   if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { Value = _value - _step; e.Handled = true; }
   else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { Value = _value + _step; e.Handled = true; }
   base.OnKeyDown(e);
  }
  protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
  protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

  protected override void OnPaint(PaintEventArgs pe) {
   Graphics g = pe.Graphics;
   g.SmoothingMode = SmoothingMode.AntiAlias;
   int cy = Height / 2;
   Rectangle track = new Rectangle(6, cy - 4, Width - 13, 8);
   using (SolidBrush b = new SolidBrush(Color.FromArgb(48, 39, 29))) Albion.FillRounded(g, b, track, 4);
   int kx = KnobX();
   Rectangle fill = new Rectangle(track.X, track.Y, Math.Max(0, kx - track.X + 5), track.Height);
   using (SolidBrush b = new SolidBrush(Albion.GoldDim)) Albion.FillRounded(g, b, fill, 4);
   using (SolidBrush b = new SolidBrush(Albion.GoldBright)) g.FillEllipse(b, kx - 1, cy - 8, 16, 16);
   using (Pen p = new Pen(Color.FromArgb(60, 40, 10), 1.5f)) g.DrawEllipse(p, kx - 1, cy - 8, 16, 16);
   if (Focused) { using (Pen p = new Pen(Color.FromArgb(140, Albion.GoldBright), 1f)) Albion.DrawRounded(g, p, new Rectangle(0, 0, Width - 1, Height - 1), 5); }
  }
 }

 // Owner-draw helper shared by the category and feature lists.
 public static class ListPaint {
  public static void FillRow(Graphics g, Rectangle b, bool selected, bool hover, Color baseColor) {
   Color c = baseColor;
   if (selected) c = Albion.Selection;
   else if (hover) c = Color.FromArgb(44, 36, 26);
   using (SolidBrush br = new SolidBrush(c)) g.FillRectangle(br, b);
   if (selected) {
    using (SolidBrush br = new SolidBrush(Albion.Gold)) g.FillRectangle(br, b.X, b.Y, 3, b.Height);
   }
  }

  public static void Badge(Graphics g, string text, Font f, Color color, int x, int y) {
   Size s = TextRenderer.MeasureText(text, f);
   Rectangle r = new Rectangle(x, y, s.Width + 12, s.Height + 4);
   using (SolidBrush b = new SolidBrush(Color.FromArgb(42, color))) Albion.FillRounded(g, b, r, 3);
   using (Pen p = new Pen(Color.FromArgb(160, color), 1f)) Albion.DrawRounded(g, p, r, 3);
   TextRenderer.DrawText(g, text, f, new Rectangle(r.X, r.Y, r.Width, r.Height), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
  }
 }
}
