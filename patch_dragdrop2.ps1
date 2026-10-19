$file = "C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs"
$lines = [IO.File]::ReadAllLines($file)

# Find the line with "AlbionButton up = new AlbionButton()" and "UP"
$insertIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
 if ($lines[$i].Contains("AlbionButton up = new AlbionButton()") -and $lines[$i].Contains("UP")) {
  $insertIndex = $i
  break
 }
}

if ($insertIndex -ge 0) {
 $dragDropCode = @()
 $dragDropCode += "   // Drag-drop reorder"
 $dragDropCode += "   int dragIndex = -1;"
 $dragDropCode += "   list.MouseDown += delegate(object s, MouseEventArgs e) {"
 $dragDropCode += "    if (e.Button == MouseButtons.Left) {"
 $dragDropCode += "     dragIndex = list.IndexFromPoint(e.Location);"
 $dragDropCode += "     if (dragIndex -ge 0) list.DoDragDrop(dragIndex, DragDropEffects.Move);"
 $dragDropCode += "    }"
 $dragDropCode += "   };"
 $dragDropCode += "   list.DragOver += delegate(object s, DragEventArgs e) {"
 $dragDropCode += "    e.Effect = DragDropEffects.Move;"
 $dragDropCode += "   };"
 $dragDropCode += "   list.DragDrop += delegate(object s, DragEventArgs e) {"
 $dragDropCode += "    int src = (int)e.Data.GetData(typeof(int));"
 $dragDropCode += "    int dst = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));"
 $dragDropCode += "    if (dst -lt 0 -or dst -ge queue.Count) return;"
 $dragDropCode += "    if (src -eq dst) return;"
 $dragDropCode += "    Queued q = queue[src];"
 $dragDropCode += "    queue.RemoveAt(src);"
 $dragDropCode += "    queue.Insert(dst, q);"
 $dragDropCode += "    refresh();"
 $dragDropCode += "    Toast(""Reordered queue"", Albion.GoldDim);"
 $dragDropCode += "   };"
 $dragDropCode += ""

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