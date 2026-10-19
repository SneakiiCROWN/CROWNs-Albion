$file = "C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs"
$content = [IO.File]::ReadAllText($file)

$q = '"'

# 3. Add drag-drop reorder - there IS a blank line (two CRLF) between SelectedIndexChanged and up button
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

[IO.File]::WriteAllText($file, $content, [Text.Encoding]::UTF8)
"Done"