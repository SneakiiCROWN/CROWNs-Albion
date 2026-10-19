// CROWN's ALBION v2.0 — Albion-inspired theme: aged leather, parchment, gold.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CROWNsAlbion {

 public static class Albion {
  // Palette
  public static readonly Color Leather = Color.FromArgb(20, 16, 12);
  public static readonly Color LeatherHi = Color.FromArgb(31, 25, 19);
  public static readonly Color Panel = Color.FromArgb(35, 28, 21);
  public static readonly Color PanelHi = Color.FromArgb(45, 36, 27);
  public static readonly Color PanelSoft = Color.FromArgb(28, 23, 17);
  public static readonly Color Parchment = Color.FromArgb(233, 220, 190);
  public static readonly Color ParchmentShade = Color.FromArgb(214, 198, 164);
  public static readonly Color Ink = Color.FromArgb(43, 33, 24);
  public static readonly Color InkSoft = Color.FromArgb(97, 82, 62);
  public static readonly Color Gold = Color.FromArgb(201, 162, 39);
  public static readonly Color GoldBright = Color.FromArgb(240, 206, 110);
  public static readonly Color GoldDim = Color.FromArgb(129, 99, 45);
  public static readonly Color Text = Color.FromArgb(238, 228, 206);
  public static readonly Color Muted = Color.FromArgb(166, 152, 126);
  public static readonly Color Border = Color.FromArgb(84, 68, 45);
  public static readonly Color Danger = Color.FromArgb(180, 74, 62);
  public static readonly Color DangerSoft = Color.FromArgb(120, 55, 47);
  public static readonly Color Good = Color.FromArgb(106, 148, 96);
  public static readonly Color GoodSoft = Color.FromArgb(62, 92, 58);
  public static readonly Color Info = Color.FromArgb(95, 132, 168);
  public static readonly Color Selection = Color.FromArgb(72, 56, 32);
  public static readonly Color SelectionGold = Color.FromArgb(120, 94, 42);

  // Fonts
  public static readonly Font H1 = new Font("Georgia", 19, FontStyle.Bold);
  public static readonly Font H2 = new Font("Georgia", 14, FontStyle.Bold);
  public static readonly Font H3 = new Font("Georgia", 11.5f, FontStyle.Bold);
  public static readonly Font Body = new Font("Segoe UI", 9.5f);
  public static readonly Font BodyBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
  public static readonly Font Small = new Font("Segoe UI", 8.4f);
  public static readonly Font SmallBold = new Font("Segoe UI", 8.4f, FontStyle.Bold);
  public static readonly Font Tiny = new Font("Segoe UI", 7.6f);
  public static readonly Font Numeral = new Font("Consolas", 15, FontStyle.Bold);
  public static readonly Font Title = new Font("Georgia", 10.5f, FontStyle.Bold);
  public static readonly Font CardTitle = new Font("Georgia", 11, FontStyle.Bold);

  public static string IconDir = "";
  public static string FrameDir = "";

  static readonly Dictionary<string, Bitmap> FrameCache = new Dictionary<string, Bitmap>();

  public static void Init(string root) {
   IconDir = FindDir(new[] {
    Path.Combine(root, "ui", "icons"),
    Path.Combine(root, "Assets", "CROWN_UI", "icons"),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..")), "Assets", "CROWN_UI", "icons"),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..", "..")), "Assets", "CROWN_UI", "icons")
   }, Path.Combine(root, "ui", "icons"));
   FrameDir = FindDir(new[] {
    Path.Combine(root, "ui", "frames"),
    Path.Combine(root, "Assets", "CROWN_UI"),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..")), "Assets", "CROWN_UI"),
    Path.Combine(Path.GetFullPath(Path.Combine(root, "..", "..")), "Assets", "CROWN_UI")
   }, Path.Combine(root, "ui", "frames"));
  }

  static string FindDir(string[] candidates, string fallback) {
   foreach (string c in candidates) {
    try { if (Directory.Exists(c) && Directory.GetFiles(c, "*.svg").Length + Directory.GetFiles(c, "*.png").Length > 0) return c; }
    catch { }
   }
   foreach (string c in candidates) { try { if (Directory.Exists(c)) return c; } catch { } }
   return fallback;
  }

  // ------------------------------------------------------------- geometry

  public static GraphicsPath Rounded(Rectangle r, int radius) {
   GraphicsPath p = new GraphicsPath();
   if (radius <= 0) { p.AddRectangle(r); return p; }
   int d = radius * 2;
   if (d > r.Width) d = r.Width;
   if (d > r.Height) d = r.Height;
   p.AddArc(r.X, r.Y, d, d, 180, 90);
   p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
   p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
   p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
   p.CloseFigure();
   return p;
  }

  public static void FillRounded(Graphics g, Brush b, Rectangle r, int radius) {
   using (GraphicsPath p = Rounded(r, radius)) g.FillPath(b, p);
  }

  public static void DrawRounded(Graphics g, Pen pen, Rectangle r, int radius) {
   using (GraphicsPath p = Rounded(r, radius)) g.DrawPath(pen, p);
  }

  public static void Shadow(Graphics g, Rectangle r, int radius, int depth) {
   for (int i = depth; i > 0; i--) {
    Rectangle s = new Rectangle(r.X - i / 2, r.Y + i / 2, r.Width + i, r.Height + i);
    using (SolidBrush b = new SolidBrush(Color.FromArgb(Math.Max(4, 30 - i * 3), 0, 0, 0)))
     FillRounded(g, b, s, radius + i);
   }
  }

  // 9-slice a Kenney fantasy border. Falls back to a drawn gold frame.
  public static void NineSlice(Graphics g, string name, Rectangle r, Color tint, int inset) {
   Bitmap bmp = LoadFrame(name, tint);
   if (bmp == null) {
    using (Pen pen = new Pen(tint, 1.4f))
    using (Pen pen2 = new Pen(Color.FromArgb(90, tint), 1f)) {
     DrawRounded(g, pen, r, 6);
     DrawRounded(g, pen2, new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6), 4);
    }
    return;
   }
   int c = Math.Min(inset, Math.Min(bmp.Width, bmp.Height) / 2);
   int srcMidW = bmp.Width - c * 2;
   int srcMidH = bmp.Height - c * 2;
   int dstMidW = Math.Max(0, r.Width - c * 2);
   int dstMidH = Math.Max(0, r.Height - c * 2);
   InterpolationMode old = g.InterpolationMode;
   g.InterpolationMode = InterpolationMode.HighQualityBicubic;
   // corners
   g.DrawImage(bmp, new Rectangle(r.X, r.Y, c, c), new Rectangle(0, 0, c, c), GraphicsUnit.Pixel);
   g.DrawImage(bmp, new Rectangle(r.Right - c, r.Y, c, c), new Rectangle(bmp.Width - c, 0, c, c), GraphicsUnit.Pixel);
   g.DrawImage(bmp, new Rectangle(r.X, r.Bottom - c, c, c), new Rectangle(0, bmp.Height - c, c, c), GraphicsUnit.Pixel);
   g.DrawImage(bmp, new Rectangle(r.Right - c, r.Bottom - c, c, c), new Rectangle(bmp.Width - c, bmp.Height - c, c, c), GraphicsUnit.Pixel);
   // edges
   if (dstMidW > 0) {
    g.DrawImage(bmp, new Rectangle(r.X + c, r.Y, dstMidW, c), new Rectangle(c, 0, srcMidW, c), GraphicsUnit.Pixel);
    g.DrawImage(bmp, new Rectangle(r.X + c, r.Bottom - c, dstMidW, c), new Rectangle(c, bmp.Height - c, srcMidW, c), GraphicsUnit.Pixel);
   }
   if (dstMidH > 0) {
    g.DrawImage(bmp, new Rectangle(r.X, r.Y + c, c, dstMidH), new Rectangle(0, c, c, srcMidH), GraphicsUnit.Pixel);
    g.DrawImage(bmp, new Rectangle(r.Right - c, r.Y + c, c, dstMidH), new Rectangle(bmp.Width - c, c, c, srcMidH), GraphicsUnit.Pixel);
   }
   if (dstMidW > 0 && dstMidH > 0)
    g.DrawImage(bmp, new Rectangle(r.X + c, r.Y + c, dstMidW, dstMidH), new Rectangle(c, c, srcMidW, srcMidH), GraphicsUnit.Pixel);
   g.InterpolationMode = old;
  }

  static Bitmap LoadFrame(string name, Color tint) {
   if (string.IsNullOrEmpty(FrameDir) || string.IsNullOrEmpty(name)) return null;
   string key = name + "|" + tint.ToArgb();
   lock (FrameCache) {
    Bitmap cached;
    if (FrameCache.TryGetValue(key, out cached)) return cached;
   }
   string path = Path.Combine(FrameDir, name + ".png");
   Bitmap result = null;
   if (File.Exists(path)) {
    try {
     using (Bitmap src = new Bitmap(path)) {
      result = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
      using (Graphics g = Graphics.FromImage(result)) {
       g.Clear(Color.Transparent);
       ColorMatrix cm = new ColorMatrix(new float[][]{
        new float[]{tint.R/255f,0,0,0,0},
        new float[]{0,tint.G/255f,0,0,0},
        new float[]{0,0,tint.B/255f,0,0},
        new float[]{0,0,0,1,0},
        new float[]{0,0,0,0,1}
       });
       using (ImageAttributes ia = new ImageAttributes()) {
        ia.SetColorMatrix(cm);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
       }
      }
     }
    } catch { result = null; }
   }
   lock (FrameCache) { FrameCache[key] = result; }
   return result;
  }

  // ------------------------------------------------------------- text

  public static void Draw(Graphics g, string s, Font f, Color c, Rectangle r, ContentAlignment align) {
   TextRenderer.DrawText(g, s, f, r, c, Flags(align));
  }

  public static void Draw(Graphics g, string s, Font f, Color c, int x, int y, int w, int h, ContentAlignment align) {
   TextRenderer.DrawText(g, s, f, new Rectangle(x, y, w, h), c, Flags(align));
  }

  static TextFormatFlags Flags(ContentAlignment a) {
   TextFormatFlags f = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
   if (a == ContentAlignment.MiddleLeft || a == ContentAlignment.TopLeft || a == ContentAlignment.BottomLeft) f |= TextFormatFlags.Left;
   else if (a == ContentAlignment.MiddleCenter || a == ContentAlignment.TopCenter || a == ContentAlignment.BottomCenter) f |= TextFormatFlags.HorizontalCenter;
   else f |= TextFormatFlags.Right;
   if (a == ContentAlignment.MiddleLeft || a == ContentAlignment.MiddleCenter || a == ContentAlignment.MiddleRight) f |= TextFormatFlags.VerticalCenter;
   else if (a == ContentAlignment.TopLeft || a == ContentAlignment.TopCenter || a == ContentAlignment.TopRight) f |= TextFormatFlags.Top;
   else f |= TextFormatFlags.Bottom;
   return f;
  }

  public static Color RiskColor(string risk) {
   if (string.Equals(risk, "high", StringComparison.OrdinalIgnoreCase)) return Danger;
   if (string.Equals(risk, "medium", StringComparison.OrdinalIgnoreCase)) return Gold;
   return Good;
  }

  public static Color EvidenceColor(string evidence) {
   if (string.Equals(evidence, "debug-menu", StringComparison.OrdinalIgnoreCase)) return Good;
   if (string.Equals(evidence, "community", StringComparison.OrdinalIgnoreCase)) return Info;
   if (string.Equals(evidence, "legacy", StringComparison.OrdinalIgnoreCase)) return Info;
   return Muted;
  }

  public static string EvidenceLabel(string evidence) {
   if (string.Equals(evidence, "debug-menu", StringComparison.OrdinalIgnoreCase)) return "SOURCE-BACKED";
   if (string.Equals(evidence, "community", StringComparison.OrdinalIgnoreCase)) return "COMMUNITY";
   if (string.Equals(evidence, "legacy", StringComparison.OrdinalIgnoreCase)) return "v1.6.1 RECOVERED";
   return "REFERENCE ONLY";
  }

  public static Color CoopColor(string coop) {
   if (string.Equals(coop, "safe", StringComparison.OrdinalIgnoreCase)) return Good;
   if (string.Equals(coop, "local", StringComparison.OrdinalIgnoreCase)) return Good;
   if (string.Equals(coop, "unsafe", StringComparison.OrdinalIgnoreCase)) return Danger;
   return Gold;
  }

  public static string CoopLabel(string coop) {
   if (string.Equals(coop, "safe", StringComparison.OrdinalIgnoreCase)) return "CO-OP SAFE";
   if (string.Equals(coop, "local", StringComparison.OrdinalIgnoreCase)) return "LOCAL HERO";
   if (string.Equals(coop, "unsafe", StringComparison.OrdinalIgnoreCase)) return "CO-OP UNSAFE";
   return "CO-OP UNVERIFIED";
  }
 }
}
