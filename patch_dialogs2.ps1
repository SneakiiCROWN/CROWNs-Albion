$file = "C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs"
$content = [IO.File]::ReadAllText($file)

$q = '"'

# 2. AllowDrop on queue list - match exact 3-space indentation
$old2 = "   ListBox list = ThemedList(24, 82, 692, 268);" + "`r`n" +
"   list.DrawMode = DrawMode.OwnerDrawFixed;" + "`r`n" +
"   list.ItemHeight = 32;" + "`r`n" +
"   list.DrawItem += DrawQueueRow;" + "`r`n" +
"   d.Controls.Add(list);"

$new2 = "   ListBox list = ThemedList(24, 82, 692, 268);" + "`r`n" +
"   list.DrawMode = DrawMode.OwnerDrawFixed;" + "`r`n" +
"   list.ItemHeight = 32;" + "`r`n" +
"   list.DrawItem += DrawQueueRow;" + "`r`n" +
"   list.AllowDrop = true;" + "`r`n" +
"   d.Controls.Add(list);"

if ($content.Contains($old2)) {
 $content = $content.Replace($old2, $new2)
 "Step 2: AllowDrop - MATCHED"
} else { "Step 2: NOT FOUND" }

# 3. Add drag-drop reorder after list.SelectedIndexChanged
$old3 = "   list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };" + "`r`n" +
"`r`n" +
"   AlbionButton up = new AlbionButton(); up.Text = " + $q + "↑ UP" + $q + "; up.Style = AlbionButtonStyle.Ghost;"

$new3 = "   list.SelectedIndexChanged += delegate { queueIndex = list.SelectedIndex; };" + "`r`n" +
"`r`n" +
"   // Drag-drop reorder" + "`r`n" +
"   int dragIndex = -1;" + "`r`n" +
"   list.MouseDown += delegate(object s, MouseEventArgs e) {" + "`r`n" +
"    if (e.Button == MouseButtons.Left) {" + "`r`n" +
"     dragIndex = list.IndexFromPoint(e.Location);" + "`r`n" +
"     if (dragIndex >= 0) list.DoDragDrop(dragIndex, DragDropEffects.Move);" + "`r`n" +
"    }" + "`r`n" +
"   };" + "`r`n" +
"   list.DragOver += delegate(object s, DragEventArgs e) {" + "`r`n" +
"    e.Effect = DragDropEffects.Move;" + "`r`n" +
"   };" + "`r`n" +
"   list.DragDrop += delegate(object s, DragEventArgs e) {" + "`r`n" +
"    int src = (int)e.Data.GetData(typeof(int));" + "`r`n" +
"    int dst = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));" + "`r`n" +
"    if (dst < 0 || dst >= queue.Count) return;" + "`r`n" +
"    if (src == dst) return;" + "`r`n" +
"    Queued q = queue[src];" + "`r`n" +
"    queue.RemoveAt(src);" + "`r`n" +
"    queue.Insert(dst, q);" + "`r`n" +
"    refresh();" + "`r`n" +
"    Toast(" + $q + "Reordered queue" + $q + ", Albion.GoldDim);" + "`r`n" +
"   };" + "`r`n" +
"`r`n" +
"   AlbionButton up = new AlbionButton(); up.Text = " + $q + "↑ UP" + $q + "; up.Style = AlbionButtonStyle.Ghost;"

if ($content.Contains($old3)) {
 $content = $content.Replace($old3, $new3)
 "Step 3: Drag-drop - MATCHED"
} else { "Step 3: NOT FOUND" }

# 4. Add keyboard accelerators and hint labels
$old4 = "   refresh();" + "`r`n" +
"   ShowPopup(d);" + "`r`n" +
"  }" + "`r`n" +
"`r`n" +
"  void DrawQueueRow"

$new4 = "   // Keyboard accelerators for queue manager" + "`r`n" +
"   d.KeyDown += delegate(object s, KeyEventArgs e) {" + "`r`n" +
"    if (e.KeyCode == Keys.Delete) { remove.PerformClick(); e.Handled = true; }" + "`r`n" +
"    else if (e.Control && e.KeyCode == Keys.Up) { MoveQueued(-1); refresh(); e.Handled = true; }" + "`r`n" +
"    else if (e.Control && e.KeyCode == Keys.Down) { MoveQueued(1); refresh(); e.Handled = true; }" + "`r`n" +
"    else if (e.KeyCode == Keys.Enter) { edit.PerformClick(); e.Handled = true; }" + "`r`n" +
"   };" + "`r`n" +
"`r`n" +
"   // Update hint labels" + "`r`n" +
"   d.Controls.Add(MakeLabel(" + $q + "KEYBOARD:  Del remove  \u2022  Ctrl+\u2191/\u2193 move  \u2022  Enter edit  \u2022  Esc close" + $q + ", Albion.Tiny, Albion.Muted, 24, 428, 690, 22));" + "`r`n" +
"   d.Controls.Add(MakeLabel(" + $q + "PAD:  D-pad select  \u2022  A edit  \u2022  X remove  \u2022  Y up  \u2022  Start down  \u2022  B back" + $q + ", Albion.Tiny, Albion.Muted, 24, 452, 690, 22));" + "`r`n" +
"`r`n" +
"   refresh();" + "`r`n" +
"   ShowPopup(d);" + "`r`n" +
"  }" + "`r`n" +
"`r`n" +
"  void DrawQueueRow"

if ($content.Contains($old4)) {
 $content = $content.Replace($old4, $new4)
 "Step 4: Keyboard accelerators - MATCHED"
} else { "Step 4: NOT FOUND" }

[IO.File]::WriteAllText($file, $content, [Text.Encoding]::UTF8)
"All patches applied"