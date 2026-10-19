using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class FinalExactPatch {
    static void Main() {
        string file = @"C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs";
        string content = File.ReadAllText(file, Encoding.UTF8);
        content = content.Replace("\r\n", "\n");
        int changes = 0;

        // ============================================================
        // 1. NewDialog: Insert Escape handling BEFORE "return d;"
        // ============================================================
        string p1 = @"  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
   PopupForm d = new PopupForm();
   d.Text = title;
   d.ClientSize = new Size(width, height);
   d.BackColor = Albion.Leather; d.ForeColor = Albion.Text; d.Font = Albion.Body;
   d.KeyPreview = true; d.ShowInTaskbar = false;
   d.StartPosition = FormStartPosition.CenterParent;
   if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }
   return d;
  }";

        string r1 = @"  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
   PopupForm d = new PopupForm();
   d.Text = title;
   d.ClientSize = new Size(width, height);
   d.BackColor = Albion.Leather; d.ForeColor = Albion.Text; d.Font = Albion.Body;
   d.KeyPreview = true; d.ShowInTaskbar = false;
   d.StartPosition = FormStartPosition.CenterParent;
   if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }
   // Global Escape handling
   d.KeyDown += delegate(object s, KeyEventArgs e) {
    if (e.KeyCode == Keys.Escape) {
     if (d.CancelButton != null) d.CancelButton.PerformClick();
     else { d.DialogResult = DialogResult.Cancel; d.Close(); }
     e.Handled = true; e.SuppressKeyPress = true;
    }
   };
   return d;
  }";

        if (content.Contains(p1)) { content = content.Replace(p1, r1); changes++; Console.WriteLine("1: OK"); } else { Console.WriteLine("1: FAIL"); }

        // ============================================================
        // 2. AllowDrop on queue list
        // ============================================================
        string p2 = @"   ListBox list = ThemedList(24, 82, 692, 268);
   list.DrawMode = DrawMode.OwnerDrawFixed;
   list.ItemHeight = 32;
   list.DrawItem += DrawQueueRow;
   d.Controls.Add(list);";

        string r2 = @"   ListBox list = ThemedList(24, 82, 692, 268);
   list.DrawMode = DrawMode.OwnerDrawFixed;
   list.ItemHeight = 32;
   list.DrawItem += DrawQueueRow;
   list.AllowDrop = true;
   d.Controls.Add(list);";

        if (content.Contains(p2)) { content = content.Replace(p2, r2); changes++; Console.WriteLine("2: OK"); } else { Console.WriteLine("2: FAIL"); }

        // ============================================================
        // 3. Drag-drop reorder - use regex for mojibake up-arrow
        // ============================================================
        string pattern3 = @"list\.SelectedIndexChanged \+= delegate \{ queueIndex = list\.SelectedIndex; \};\s*\n\s*\n\s*AlbionButton up = new AlbionButton\(\); up\.Text = ""[^""]*UP[^""]*""; up\.Style = AlbionButtonStyle\.Ghost;";
        
        string replacement3 = @"list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };

   // Drag-drop reorder
   int dragIndex = -1;
   list.MouseDown += delegate(object s, MouseEventArgs e) {
    if (e.Button == MouseButtons.Left) {
     dragIndex = list.IndexFromPoint(e.Location);
     if (dragIndex >= 0) list.DoDragDrop(dragIndex, DragDropEffects.Move);
    }
   };
   list.DragOver += delegate(object s, DragEventArgs e) {
    e.Effect = DragDropEffects.Move;
   };
   list.DragDrop += delegate(object s, DragEventArgs e) {
    int src = (int)e.Data.GetData(typeof(int));
    int dst = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));
    if (dst < 0 || dst >= queue.Count) return;
    if (src == dst) return;
    Queued q = queue[src];
    queue.RemoveAt(src);
    queue.Insert(dst, q);
    refresh();
    Toast(""Reordered queue"", Albion.GoldDim);
   };

   AlbionButton up = new AlbionButton(); up.Text = ""↑ UP""; up.Style = AlbionButtonStyle.Ghost;";

        content = Regex.Replace(content, pattern3, replacement3, RegexOptions.Singleline);
        if (!Regex.IsMatch(content, pattern3, RegexOptions.Singleline)) {
            changes++;
            Console.WriteLine("3: OK (regex)");
        } else { Console.WriteLine("3: FAIL"); }

        // ============================================================
        // 4. Keyboard accelerators in OpenQueueManager - EXACT pattern from file
        // ============================================================
        string p4 = @"   refresh();
   ShowPopup(d);
  }

  void DrawQueueRow";

        string r4 = @"   // Keyboard accelerators for queue manager
   d.KeyDown += delegate(object s, KeyEventArgs e) {
    if (e.KeyCode == Keys.Delete) { remove.PerformClick(); e.Handled = true; }
    else if (e.Control && e.KeyCode == Keys.Up) { MoveQueued(-1); refresh(); e.Handled = true; }
    else if (e.Control && e.KeyCode == Keys.Down) { MoveQueued(1); refresh(); e.Handled = true; }
    else if (e.KeyCode == Keys.Enter) { edit.PerformClick(); e.Handled = true; }
   };

   // Update hint labels
   d.Controls.Add(MakeLabel(""KEYBOARD:  Del remove  \u2022  Ctrl+\u2191/\u2193 move  \u2022  Enter edit  \u2022  Esc close"", Albion.Tiny, Albion.Muted, 24, 428, 690, 22));
   d.Controls.Add(MakeLabel(""PAD:  D-pad select  \u2022  A edit  \u2022  X remove  \u2022  Y up  \u2022  Start down  \u2022  B back"", Albion.Tiny, Albion.Muted, 24, 452, 690, 22));

   refresh();
   ShowPopup(d);
  }

  void DrawQueueRow";

        if (content.Contains(p4)) { content = content.Replace(p4, r4); changes++; Console.WriteLine("4: OK"); } else { Console.WriteLine("4: FAIL"); }

        content = content.Replace("\n", "\r\n");
        
        if (changes > 0) {
            File.WriteAllText(file, content, Encoding.UTF8);
            Console.WriteLine("Total changes: " + changes);
        } else {
            Console.WriteLine("No changes made");
        }
    }
}