using System;
using System.IO;
using System.Text;

class PatchDialogs {
    static void Main() {
        string file = @"C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs";
        string[] lines = File.ReadAllLines(file, Encoding.UTF8);
        int changes = 0;

        // 1. Add Escape handling to NewDialog - after the "return d;" line (3-space indent)
        for (int i = 0; i < lines.Length - 1; i++) {
            if (lines[i].Trim() == "return d;" && i > 0 && lines[i-1].Trim() == "if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }") {
                // Insert after this line, before the closing brace of the method
                // Find the next line that is just "  }" (method close)
                for (int j = i + 1; j < lines.Length; j++) {
                    if (lines[j] == "  }") {
                        string[] insert = new string[] {
                            "   // Global Escape handling",
                            "   d.KeyDown += delegate(object s, KeyEventArgs e) {",
                            "    if (e.KeyCode == Keys.Escape) {",
                            "     if (d.CancelButton != null) d.CancelButton.PerformClick();",
                            "     else { d.DialogResult = DialogResult.Cancel; d.Close(); }",
                            "     e.Handled = true; e.SuppressKeyPress = true;",
                            "    }",
                            "   };",
                            ""
                        };
                        var newLines = new System.Collections.Generic.List<string>(lines);
                        newLines.InsertRange(j, insert);
                        lines = newLines.ToArray();
                        changes++;
                        Console.WriteLine("1: NewDialog Escape handling - INSERTED");
                        break;
                    }
                }
                break;
            }
        }

        // 2. Add AllowDrop to the queue list - after DrawItem line
        for (int i = 0; i < lines.Length - 1; i++) {
            if (lines[i].Contains("list.DrawItem +=") && lines[i].Contains("DrawQueueRow")) {
                // Next line should be d.Controls.Add(list);
                if (i + 1 < lines.Length && lines[i + 1].Contains("d.Controls.Add(list)")) {
                    var newLines = new System.Collections.Generic.List<string>(lines);
                    newLines.Insert(i + 1, "   list.AllowDrop = true;");
                    lines = newLines.ToArray();
                    changes++;
                    Console.WriteLine("2: AllowDrop - INSERTED");
                    break;
                }
            }
        }

        // 3. Add drag-drop reorder after list.SelectedIndexChanged
        for (int i = 0; i < lines.Length - 1; i++) {
            if (lines[i].Contains("list.SelectedIndexChanged +=") && lines[i].Contains("queueIndex = list.SelectedIndex")) {
                // Next non-empty line should be the up button
                int insertAt = -1;
                for (int j = i + 1; j < lines.Length; j++) {
                    if (lines[j].Contains("AlbionButton up = new AlbionButton()") && lines[j].Contains("UP")) {
                        insertAt = j;
                        break;
                    }
                }
                if (insertAt >= 0) {
                    string[] insert = new string[] {
                        "   // Drag-drop reorder",
                        "   int dragIndex = -1;",
                        "   list.MouseDown += delegate(object s, MouseEventArgs e) {",
                        "    if (e.Button == MouseButtons.Left) {",
                        "     dragIndex = list.IndexFromPoint(e.Location);",
                        "     if (dragIndex >= 0) list.DoDragDrop(dragIndex, DragDropEffects.Move);",
                        "    }",
                        "   };",
                        "   list.DragOver += delegate(object s, DragEventArgs e) {",
                        "    e.Effect = DragDropEffects.Move;",
                        "   };",
                        "   list.DragDrop += delegate(object s, DragEventArgs e) {",
                        "    int src = (int)e.Data.GetData(typeof(int));",
                        "    int dst = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));",
                        "    if (dst < 0 || dst >= queue.Count) return;",
                        "    if (src == dst) return;",
                        "    Queued q = queue[src];",
                        "    queue.RemoveAt(src);",
                        "    queue.Insert(dst, q);",
                        "    refresh();",
                        "    Toast(\"Reordered queue\", Albion.GoldDim);",
                        "   };",
                        ""
                    };
                    var newLines = new System.Collections.Generic.List<string>(lines);
                    newLines.InsertRange(insertAt, insert);
                    lines = newLines.ToArray();
                    changes++;
                    Console.WriteLine("3: Drag-drop reorder - INSERTED");
                }
                break;
            }
        }

        // 4. Add keyboard accelerators and hint labels - before "refresh(); ShowPopup(d);"
        for (int i = 0; i < lines.Length - 2; i++) {
            if (lines[i].Trim() == "refresh();" && i + 1 < lines.Length && lines[i + 1].Trim() == "ShowPopup(d);") {
                // Check next line is "  }" (method close)
                if (i + 2 < lines.Length && lines[i + 2].Trim() == "}") {
                    string[] insert = new string[] {
                        "   // Keyboard accelerators for queue manager",
                        "   d.KeyDown += delegate(object s, KeyEventArgs e) {",
                        "    if (e.KeyCode == Keys.Delete) { remove.PerformClick(); e.Handled = true; }",
                        "    else if (e.Control && e.KeyCode == Keys.Up) { MoveQueued(-1); refresh(); e.Handled = true; }",
                        "    else if (e.Control && e.KeyCode == Keys.Down) { MoveQueued(1); refresh(); e.Handled = true; }",
                        "    else if (e.KeyCode == Keys.Enter) { edit.PerformClick(); e.Handled = true; }",
                        "   };",
                        "",
                        "   // Update hint labels",
                        "   d.Controls.Add(MakeLabel(\"KEYBOARD:  Del remove  \u2022  Ctrl+\u2191/\u2193 move  \u2022  Enter edit  \u2022  Esc close\", Albion.Tiny, Albion.Muted, 24, 428, 690, 22));",
                        "   d.Controls.Add(MakeLabel(\"PAD:  D-pad select  \u2022  A edit  \u2022  X remove  \u2022  Y up  \u2022  Start down  \u2022  B back\", Albion.Tiny, Albion.Muted, 24, 452, 690, 22));",
                        ""
                    };
                    var newLines = new System.Collections.Generic.List<string>(lines);
                    newLines.InsertRange(i, insert);
                    lines = newLines.ToArray();
                    changes++;
                    Console.WriteLine("4: Keyboard accelerators - INSERTED");
                }
                break;
            }
        }

        if (changes > 0) {
            File.WriteAllLines(file, lines, Encoding.UTF8);
            Console.WriteLine("Total changes: " + changes);
        } else {
            Console.WriteLine("No changes made");
        }
    }
}