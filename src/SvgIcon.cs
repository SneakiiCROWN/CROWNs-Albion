// Minimal SVG path renderer for game-icons.net single-path icons.
// Supports M/L/H/V/C/S/Q/T/A/Z in absolute and relative form, with implicit repeats.
// No external dependency and no GDI+ text: pure geometry, rendered to a Bitmap.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CROWNsAlbion {

 public static class SvgIcon {
  static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

  public static Bitmap Get(string iconDir, string name, int size, Color color, float strokeHint) {
   if (string.IsNullOrEmpty(name)) return null;
   string key = name + "|" + size + "|" + color.ToArgb() + "|" + strokeHint;
   lock (Cache) {
    Bitmap cached;
    if (Cache.TryGetValue(key, out cached)) return cached;
   }
   string path = Path.Combine(iconDir, name + ".svg");
   Bitmap bmp = null;
   if (File.Exists(path)) {
    try { bmp = Render(File.ReadAllText(path, Encoding.UTF8), size, color); }
    catch { bmp = null; }
   }
   if (bmp == null) bmp = Fallback(name, size, color);
   lock (Cache) { Cache[key] = bmp; }
   return bmp;
  }

  public static Bitmap Render(string svg, int size, Color color) {
   if (string.IsNullOrEmpty(svg)) return null;
   GraphicsPath path = new GraphicsPath();
   bool any = false;
   foreach (Match m in Regex.Matches(svg, "<path[^>]*\\bd=\"([^\"]+)\"", RegexOptions.IgnoreCase)) {
    string d = m.Groups[1].Value;
    FillMode mode = svg.IndexOf("evenodd", StringComparison.OrdinalIgnoreCase) >= 0 ? FillMode.Alternate : FillMode.Winding;
    GraphicsPath p = BuildPath(d, mode);
    if (p != null) { path.AddPath(p, false); any = true; p.Dispose(); }
   }
   if (!any) { path.Dispose(); return null; }

   Bitmap bmp = new Bitmap(size, size);
   using (Graphics g = Graphics.FromImage(bmp)) {
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    // viewBox is normally 0 0 512 512.
    float vb = 512f;
    Match vbMatch = Regex.Match(svg, "viewBox=\"([^\"]+)\"");
    if (vbMatch.Success) {
     string[] parts = vbMatch.Groups[1].Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
     if (parts.Length == 4) {
      float w = Float(parts[2]);
      float h = Float(parts[3]);
      vb = Math.Max(w, h);
     }
    }
    float scale = size / vb;
    using (Matrix mx = new Matrix(scale, 0, 0, scale, 0, 0)) {
     path.Transform(mx);
     using (SolidBrush brush = new SolidBrush(color)) g.FillPath(brush, path);
    }
   }
   path.Dispose();
   return bmp;
  }

  static Bitmap Fallback(string name, int size, Color color) {
   Bitmap bmp = new Bitmap(size, size);
   using (Graphics g = Graphics.FromImage(bmp)) {
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);
    using (Pen pen = new Pen(color, Math.Max(1.4f, size / 12f)))
    using (SolidBrush brush = new SolidBrush(Color.FromArgb(60, color))) {
     g.DrawEllipse(pen, 2, 2, size - 5, size - 5);
     g.FillEllipse(brush, 2, 2, size - 5, size - 5);
    }
   }
   return bmp;
  }

  static float Float(string s) { float f; float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f); return f; }

  // ------------------------------------------------------------ path builder

  static GraphicsPath BuildPath(string d, FillMode mode) {
   List<object> tokens = Tokenize(d);
   GraphicsPath path = new GraphicsPath(mode);
   float cx = 0, cy = 0;          // current point
   float sx = 0, sy = 0;          // subpath start
   float lastC2x = 0, lastC2y = 0;
   char lastCmd = ' ';
   int i = 0;
   bool open = false;
   char cmd = ' ';

   while (i < tokens.Count) {
    object tok = tokens[i];
    if (tok is char) { cmd = (char)tok; i++; lastCmd = ' '; }
    else if (cmd == ' ') { i++; continue; }

    int start = i;
    switch (char.ToUpperInvariant(cmd)) {
     case 'M': {
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x += cx; y += cy; }
      if (open) path.CloseFigure();
      path.StartFigure(); open = true;
      cx = sx = x; cy = sy = y;
      // implicit lineto
      cmd = char.IsLower(cmd) ? 'l' : 'L';
      break;
     }
     case 'L': {
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x += cx; y += cy; }
      EnsureOpen(path, ref open, cx, cy);
      path.AddLine(cx, cy, x, y);
      cx = x; cy = y;
      break;
     }
     case 'H': {
      float x = Next(tokens, ref i);
      if (char.IsLower(cmd)) x += cx;
      EnsureOpen(path, ref open, cx, cy);
      path.AddLine(cx, cy, x, cy);
      cx = x;
      break;
     }
     case 'V': {
      float y = Next(tokens, ref i);
      if (char.IsLower(cmd)) y += cy;
      EnsureOpen(path, ref open, cx, cy);
      path.AddLine(cx, cy, cx, y);
      cy = y;
      break;
     }
     case 'C': {
      float x1 = Next(tokens, ref i), y1 = Next(tokens, ref i);
      float x2 = Next(tokens, ref i), y2 = Next(tokens, ref i);
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x1 += cx; y1 += cy; x2 += cx; y2 += cy; x += cx; y += cy; }
      EnsureOpen(path, ref open, cx, cy);
      path.AddBezier(cx, cy, x1, y1, x2, y2, x, y);
      lastC2x = x2; lastC2y = y2; cx = x; cy = y;
      break;
     }
     case 'S': {
      float x2 = Next(tokens, ref i), y2 = Next(tokens, ref i);
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x2 += cx; y2 += cy; x += cx; y += cy; }
      float x1 = cx, y1 = cy;
      if (char.ToUpperInvariant(lastCmd) == 'C' || char.ToUpperInvariant(lastCmd) == 'S') { x1 = 2 * cx - lastC2x; y1 = 2 * cy - lastC2y; }
      EnsureOpen(path, ref open, cx, cy);
      path.AddBezier(cx, cy, x1, y1, x2, y2, x, y);
      lastC2x = x2; lastC2y = y2; cx = x; cy = y;
      break;
     }
     case 'Q': {
      float x1 = Next(tokens, ref i), y1 = Next(tokens, ref i);
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x1 += cx; y1 += cy; x += cx; y += cy; }
      EnsureOpen(path, ref open, cx, cy);
      path.AddBezier(cx, cy, cx + 2f / 3f * (x1 - cx), cy + 2f / 3f * (y1 - cy), x + 2f / 3f * (x1 - x), y + 2f / 3f * (y1 - y), x, y);
      lastC2x = x1; lastC2y = y1; cx = x; cy = y;
      break;
     }
     case 'T': {
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x += cx; y += cy; }
      float x1 = cx, y1 = cy;
      if (char.ToUpperInvariant(lastCmd) == 'Q' || char.ToUpperInvariant(lastCmd) == 'T') { x1 = 2 * cx - lastC2x; y1 = 2 * cy - lastC2y; }
      EnsureOpen(path, ref open, cx, cy);
      path.AddBezier(cx, cy, cx + 2f / 3f * (x1 - cx), cy + 2f / 3f * (y1 - cy), x + 2f / 3f * (x1 - x), y + 2f / 3f * (y1 - y), x, y);
      lastC2x = x1; lastC2y = y1; cx = x; cy = y;
      break;
     }
     case 'A': {
      float rx = Next(tokens, ref i), ry = Next(tokens, ref i);
      float rot = Next(tokens, ref i);
      int large = (int)Next(tokens, ref i);
      int sweep = (int)Next(tokens, ref i);
      float x = Next(tokens, ref i), y = Next(tokens, ref i);
      if (char.IsLower(cmd)) { x += cx; y += cy; }
      EnsureOpen(path, ref open, cx, cy);
      AddArc(path, cx, cy, rx, ry, rot, large != 0, sweep != 0, x, y);
      cx = x; cy = y;
      break;
     }
     case 'Z': {
      if (open) { path.CloseFigure(); open = false; }
      cx = sx; cy = sy;
      break;
     }
     default: i++; break;
    }
    lastCmd = char.ToUpperInvariant(cmd);
    if (i == start) i++; // safety
   }
   if (open) path.CloseFigure();
   return path;
  }

  static void EnsureOpen(GraphicsPath path, ref bool open, float x, float y) {
   if (!open) { path.StartFigure(); open = true; }
  }

  // SVG elliptical arc -> cubic bezier segments (W3C implementation notes F.6).
  static void AddArc(GraphicsPath path, float x1, float y1, float rx, float ry, float phiDeg, bool large, bool sweep, float x2, float y2) {
   if (rx == 0 || ry == 0) { path.AddLine(x1, y1, x2, y2); return; }
   double phi = phiDeg * Math.PI / 180.0;
   double rxAbs = Math.Abs(rx), ryAbs = Math.Abs(ry);
   double dx2 = (x1 - x2) / 2.0, dy2 = (y1 - y2) / 2.0;
   double x1p = Math.Cos(phi) * dx2 + Math.Sin(phi) * dy2;
   double y1p = -Math.Sin(phi) * dx2 + Math.Cos(phi) * dy2;
   double lambda = (x1p * x1p) / (rxAbs * rxAbs) + (y1p * y1p) / (ryAbs * ryAbs);
   if (lambda > 1) { double s = Math.Sqrt(lambda); rxAbs *= s; ryAbs *= s; }
   double sign = (large != sweep) ? 1 : -1;
   double num = rxAbs * rxAbs * ryAbs * ryAbs - rxAbs * rxAbs * y1p * y1p - ryAbs * ryAbs * x1p * x1p;
   double den = rxAbs * rxAbs * y1p * y1p + ryAbs * ryAbs * x1p * x1p;
   double co = sign * Math.Sqrt(Math.Max(0, num / den));
   double cxp = co * (rxAbs * y1p / ryAbs);
   double cyp = co * (-ryAbs * x1p / rxAbs);
   double cx = Math.Cos(phi) * cxp - Math.Sin(phi) * cyp + (x1 + x2) / 2.0;
   double cy = Math.Sin(phi) * cxp + Math.Cos(phi) * cyp + (y1 + y2) / 2.0;
   double theta1 = Angle(1, 0, (x1p - cxp) / rxAbs, (y1p - cyp) / ryAbs);
   double dtheta = Angle((x1p - cxp) / rxAbs, (y1p - cyp) / ryAbs, (-x1p - cxp) / rxAbs, (-y1p - cyp) / ryAbs);
   if (!sweep && dtheta > 0) dtheta -= 2 * Math.PI;
   else if (sweep && dtheta < 0) dtheta += 2 * Math.PI;
   int segments = (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI / 2));
   if (segments < 1) segments = 1;
   double delta = dtheta / segments;
   double t = 8.0 / 3.0 * Math.Sin(delta / 4) * Math.Sin(delta / 4) / Math.Sin(delta / 2);
   double theta = theta1;
   double px = x1, py = y1;
   for (int s = 0; s < segments; s++) {
    double cos1 = Math.Cos(theta), sin1 = Math.Sin(theta);
    double cos2 = Math.Cos(theta + delta), sin2 = Math.Sin(theta + delta);
    double ex = cx + rxAbs * cos2 * Math.Cos(phi) - ryAbs * sin2 * Math.Sin(phi);
    double ey = cy + rxAbs * cos2 * Math.Sin(phi) + ryAbs * sin2 * Math.Cos(phi);
    double q1x = px + t * (-rxAbs * sin1 * Math.Cos(phi) - ryAbs * cos1 * Math.Sin(phi));
    double q1y = py + t * (-rxAbs * sin1 * Math.Sin(phi) + ryAbs * cos1 * Math.Cos(phi));
    double q2x = ex - t * (-rxAbs * sin2 * Math.Cos(phi) - ryAbs * cos2 * Math.Sin(phi));
    double q2y = ey - t * (-rxAbs * sin2 * Math.Sin(phi) + ryAbs * cos2 * Math.Cos(phi));
    path.AddBezier((float)px, (float)py, (float)q1x, (float)q1y, (float)q2x, (float)q2y, (float)ex, (float)ey);
    theta += delta; px = ex; py = ey;
   }
  }

  static double Angle(double ux, double uy, double vx, double vy) {
   double dot = ux * vx + uy * vy;
   double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
   double a = Math.Acos(Math.Max(-1, Math.Min(1, dot / len)));
   if (ux * vy - uy * vx < 0) a = -a;
   return a;
  }

  static float Next(List<object> tokens, ref int i) {
   while (i < tokens.Count && !(tokens[i] is float)) i++;
   if (i >= tokens.Count) return 0;
   float f = (float)tokens[i];
   i++;
   return f;
  }

  static List<object> Tokenize(string d) {
   List<object> list = new List<object>();
   int i = 0;
   while (i < d.Length) {
    char c = d[i];
    if (char.IsLetter(c)) { list.Add(char.ToUpperInvariant(c) == 'E' ? c : c); if (char.ToUpperInvariant(c) == 'E') { /* exponent inside number, skip */ } i++; continue; }
    if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }
    if (c == '-' || c == '+' || c == '.' || char.IsDigit(c)) {
     int start = i;
     if (c == '-' || c == '+') i++;
     while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '.')) i++;
     if (i < d.Length && (d[i] == 'e' || d[i] == 'E')) {
      i++;
      if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
      while (i < d.Length && char.IsDigit(d[i])) i++;
     }
     float f;
     float.TryParse(d.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out f);
     list.Add(f);
     continue;
    }
    i++;
   }
   return list;
  }
 }
}
