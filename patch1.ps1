$file = "C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs"
$content = [IO.File]::ReadAllText($file)

# 1. Add Escape handling to NewDialog
$old1 = "  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
   PopupForm d = new PopupForm();
   d.Text = title;
   d.ClientSize = new Size(width, height);
   d.BackColor = Albion.Leather; d.ForeColor = Albion.Text; d.Font = Albion.Body;
   d.KeyPreview = true; d.ShowInTaskbar = false;
   d.StartPosition = FormStartPosition.CenterParent;
   if (fixedSize) { d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; }
   return d;
  }"

$new1 = "  PopupForm NewDialog(string title, int width, int height, bool fixedSize) {
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
  }"

if ($content.Contains($old1)) {
 $content = $content.Replace($old1, $new1)
 [IO.File]::WriteAllText($file, $content, [Text.Encoding]::UTF8)
 "Step 1: NewDialog Escape handling - DONE"
} else {
 "Step 1: Old text not found exactly"
}