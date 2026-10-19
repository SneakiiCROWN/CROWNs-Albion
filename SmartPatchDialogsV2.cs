using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class SmartPatchDialogs {
    static void Main() {
        string file = @"C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs";
        string[] lines = File.ReadAllLines(file, Encoding.UTF8);
        var output = new List<string>();
        int changes = 0;
        bool inQueueManager = false;
        bool inNewDialog = false;
        
        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i];
            
            // Track NewDialog method
            if (line.Contains("PopupForm NewDialog")) {
                inNewDialog = true;
            }
            
            // Track when we exit NewDialog
            if (inNewDialog && line.Trim() == "}" && i > 40) {
                inNewDialog = false;
            }
            
            // Track if we're in OpenQueueManager
            if (line.Contains("public void OpenQueueManager()")) {
                inQueueManager = true;
            }
            
            // Track when we exit OpenQueueManager (next public method)
            if (inQueueManager && line.Trim().StartsWith("public void ") && !line.Contains("OpenQueueManager")) {
                inQueueManager = false;
            }
            
            // 1. NewDialog: Insert Escape handling BEFORE "return d;"
            if (inNewDialog && line.Trim() == "return d;" && i > 0 && lines[i-1].Trim() == "if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }") {
                // Insert Escape handling BEFORE return d;
                output.Add("   // Global Escape handling");
                output.Add("   d.KeyDown += delegate(object s, KeyEventArgs e) {");
                output.Add("    if (e.KeyCode == Keys.Escape) {");
                output.Add("     if (d.CancelButton != null) d.CancelButton.PerformClick();");
                output.Add("     else { d.DialogResult = DialogResult.Cancel; d.Close(); }");
                output.Add("     e.Handled = true; e.SuppressKeyPress = true;");
                output.Add("    }");
                output.Add("   };");
                output.Add(""); // blank line
                output.Add(line); // return d;
                changes++;
                Console.WriteLine("1: NewDialog Escape - BEFORE return");
                continue;
            }
            
            // Track if we're in OpenQueueManager
            if (line.Contains("public void OpenQueueManager()")) {
                inQueueManager = true;
            }
            
            // Track when we exit OpenQueueManager (next public method)
            if (inQueueManager && line.Trim().StartsWith("public void ") && !line.Contains("OpenQueueManager")) {
                inQueueManager = false;
            }
            
            // 2. AllowDrop on queue list - after DrawItem += DrawQueueRow (only in OpenQueueManager)
            if (inQueueManager && line.Contains("list.DrawItem +=") && line.Contains("DrawQueueRow")) {
                output.Add(line);
                output.Add("   list.AllowDrop = true;");
                changes++;
                Console.WriteLine("2: AllowDrop - AFTER DrawItem");
                continue;
            }
            
            // 3. Drag-drop reorder - after list.SelectedIndexChanged, before AlbionButton up
            if (inQueueManager && line.Contains("AlbionButton up = new AlbionButton()") && line.Contains("UP")) {
                output.Add("   // Drag-drop reorder");
                output.Add("   int dragIndex = -1;");
                output.Add("   list.MouseDown += delegate(object s, MouseEventArgs e) {");
                output.Add("    if (e.Button == MouseButtons.Left) {");
                output.Add("     dragIndex = list.IndexFromPoint(e.Location);");
                output.Add("     if (dragIndex >= 0) list.DoDragDrop(dragIndex, DragDropEffects.Move);");
                output.Add("    }");
                output.Add("   };");
                output.Add("   list.DragOver += delegate(object s, DragEventArgs e) {");
                output.Add("    e.Effect = DragDropEffects.Move;");
                output.Add("   };");
                output.Add("   list.DragDrop += delegate(object s, DragEventArgs e) {");
                output.Add("    int src = (int)e.Data.GetData(typeof(int));");
                output.Add("    int dst = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));");
                output.Add("    if (dst < 0 || dst >= queue.Count) return;");
                output.Add("    if (src == dst) return;");
                output.Add("    Queued q = queue[src];");
                output.Add("    queue.RemoveAt(src);");
                output.Add("    queue.Insert(dst, q);");
                output.Add("    refresh();");
                output.Add("    Toast(\"Reordered queue\", Albion.GoldDim);");
                output.Add("   };");
                output.Add("");
                changes++;
                Console.WriteLine("3: Drag-drop reorder - BEFORE up button");
            }
            
            output.Add(line);
            
            // 4. Keyboard accelerators - ONLY in OpenQueueManager, before "refresh();" followed by "ShowPopup(d);" then "}"
            if (inQueueManager && line.Trim() == "refresh();" && i + 2 < lines.Length && lines[i+1].Trim() == "ShowPopup(d);" && lines[i+2].Trim() == "}") {
                // Insert before refresh()
                output.RemoveAt(output.Count - 1); // remove the "refresh();" we just added
                output.Add("   // Keyboard accelerators for queue manager");
                output.Add("   d.KeyDown += delegate(object s, KeyEventArgs e) {");
                output.Add("    if (e.KeyCode == Keys.Delete) { remove.PerformClick(); e.Handled = true; }");
                output.Add("    else if (e.Control && e.KeyCode == Keys.Up) { MoveQueued(-1); refresh(); e.Handled = true; }");
                output.Add("    else if (e.Control && e.KeyCode == Keys.Down) { MoveQueued(1); refresh(); e.Handled = true; }");
                output.Add("    else if (e.KeyCode == Keys.Enter) { edit.PerformClick(); e.Handled = true; }");
                output.Add("   };");
                output.Add("");
                output.Add("   // Update hint labels");
                output.Add("   d.Controls.Add(MakeLabel(\"KEYBOARD:  Del remove  \u2022  Ctrl+\u2191/\u2193 move  \u2022  Enter edit  \u2022  Esc close\", Albion.Tiny, Albion.Muted, 24, 428, 690, 22));");
                output.Add("   d.Controls.Add(MakeLabel(\"PAD:  D-pad select  \u2022  A edit  \u2022  X remove  \u2022  Y up  \u2022  Start down  \u2022  B back\", Albion.Tiny, Albion.Muted, 24, 452, 690, 22));");
                output.Add("");
                output.Add(line); // refresh();
                changes++;
                Console.WriteLine("4: Keyboard accelerators - BEFORE refresh (QueueManager)");
                continue;
            }
            
            // 5. Add Escape handling to NewDialog BEFORE return
            if (inNewDialog && line.Trim() == "return d;" && i > 0 && lines[i-1].Trim() == "if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }") {
                // Insert Escape handling BEFORE return d;
                output.Add("   // Global Escape handling");
                output.Add("   d.KeyDown += delegate(object s, KeyEventArgs e) {");
                output.Add("    if (e.KeyCode == Keys.Escape) {");
                output.Add("     if (d.CancelButton != null) d.CancelButton.PerformClick();");
                output.Add("     else { d.DialogResult = DialogResult.Cancel; d.Close(); }");
                output.Add("     e.Handled = true; e.SuppressKeyPress = true;");
                output.Add("    }");
                output.Add("   };");
                output.Add(""); // blank line
                output.Add(line); // return d;
                changes++;
                Console.WriteLine("5: NewDialog Escape - BEFORE return");
                continue;
            }
            
            output.Add(line);
        }
        
        if (changes > 0) {
            File.WriteAllLines(file, output.ToArray(), Encoding.UTF8);
            Console.WriteLine("Total changes: " + changes);
        } else {
            Console.WriteLine("No changes made");
        }
    }
}