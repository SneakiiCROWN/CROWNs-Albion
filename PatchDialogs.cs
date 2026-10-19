using System;
using System.IO;
using System.Text;

class PatchDialogs {
    static void Main() {
        string file = @"C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs";
        string content = File.ReadAllText(file, Encoding.UTF8);
        bool any = false;

        // 1. NewDialog Escape handling
        string old1 = @"  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
   PopupForm d = new PopupForm();
   d.Text = title;
   d.ClientSize = new Size(width, height);
   d.BackColor = Albion.Leather; d.ForeColor = Albion.Text; d.Font = Albion.Body;
   d.KeyPreview = true; d.ShowInTaskbar = false;
   d.StartPosition = FormStartPosition.CenterParent;
   if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }
   return d;
  }";

        string new1 = @"  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
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

        if (content.Contains(old1)) { content = content.Replace(old1, new1); any = true; Console.WriteLine("1: OK"); } else { Console.WriteLine("1: FAIL"); }

        // 2. AllowDrop on queue list
        string old2 = @"   ListBox list = ThemedList(24, 82, 692, 268);
   list.DrawMode = DrawMode.OwnerDrawFixed;
   list.ItemHeight = 32;
   list.DrawItem += DrawQueueRow;
   d.Controls.Add(list);";

        string new2 = @"   ListBox list = ThemedList(24, 82, 692, 268);
   list.DrawMode = DrawMode.OwnerDrawFixed;
   list.ItemHeight = 32;
   list.DrawItem += DrawQueueRow;
   list.AllowDrop = true;
   d.Controls.Add(list);";

        if (content.Contains(old2)) { content = content.Replace(old2, new2); any = true; Console.WriteLine("2: OK"); } else { Console.WriteLine("2: FAIL"); }

        // 3. Drag-drop reorder
        string old3 = @"   list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };

   AlbionButton up = new AlbionButton(); up.Text = ""↑ UP""; up.Style = AlbionButtonStyle.Ghost;";

        string new3 = @"   list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };

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

        if (content.Contains(old3)) { content = content.Replace(old3, new3); any = true; Console.WriteLine("3: OK"); } else { Console.WriteLine("3: FAIL"); }

        // 4. Keyboard accelerators and hints
        string old4 = @"   refresh();
   ShowPopup(d);
  }

  void DrawQueueRow";

        string new4 = @"   // Keyboard accelerators for queue manager
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

        if (content.Contains(old4)) { content = content.Replace(old4, new4); any = true; Console.WriteLine("4: OK"); } else { Console.WriteLine("4: FAIL"); }

        if (any) {
            File.WriteAllText(file, content, Encoding.UTF8);
            Console.WriteLine("Patches applied successfully");
        } else {
            Console.WriteLine("No patches applied");
        }
    }
}