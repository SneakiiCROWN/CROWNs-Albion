$file = "C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs"
$lines = [IO.File]::ReadAllLines($file)

# Find the line with "AlbionButton up = new AlbionButton(); up.Text = "↑ UP""
$insertIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
 if ($lines[$i].Contains('up.Text = "↑ UP"') -or $lines[$i].Contains("up.Text = " + [char]0x2191 + " UP")) {
  $insertIndex = $i
  break
 }
}

if ($insertIndex -eq -1) {
 # Fallback: search for the pattern with up arrow
 for ($i = 0; $i -lt $lines.Count; $i++) {
  if ($lines[$i].Contains("AlbionButton up = new AlbionButton()") -and $lines[$i].Contains("UP")) {
   $insertIndex = $i
   break
  }
 }
}

if ($insertIndex -ge 0) {
 $dragDropCode = @(
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
 )
 $newLines = @()
 for ($j = 0; $j -lt $lines.Count; $j++) {
  $newLines += $lines[$j]
  if ($j + 1 -eq $insertIndex) {
   $newLines += $dragDropCode
  }
 }
 [IO.File]::WriteAllLines($file, $newLines, [Text.Encoding]::UTF8)
 "Inserted drag-drop at line $insertIndex"
} else {
 "Could not find insertion point"
}