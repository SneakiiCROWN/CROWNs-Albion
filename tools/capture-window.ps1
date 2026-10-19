<#
  Launches a WinForms build, waits for its main window, captures it to a PNG and
  closes the process again. Used for headless QA screenshots of CROWN's ALBION.
  Everything happens inside one invocation so the child process is never orphaned.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [int]$WaitMs = 5000,
  [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public class WinCap {
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  public static IntPtr Best(uint target, out string info) {
    IntPtr best = IntPtr.Zero; int bestArea = -1; string bestInfo = "";
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      uint pid; GetWindowThreadProcessId(h, out pid);
      if (pid != target || !IsWindowVisible(h)) return true;
      RECT r;
      if (!GetWindowRect(h, out r)) return true;
      int w = r.Right - r.Left, hh = r.Bottom - r.Top;
      if (w < 120 || hh < 80) return true;
      StringBuilder t = new StringBuilder(256); GetWindowText(h, t, 256);
      StringBuilder c = new StringBuilder(256); GetClassName(h, c, 256);
      if (t.Length == 0) return true;
      int area = w * hh;
      if (area > bestArea) { bestArea = area; best = h; bestInfo = c.ToString() + " | " + t.ToString() + " | " + w + "x" + hh + " @ " + r.Left + "," + r.Top; }
      return true;
    }, IntPtr.Zero);
    info = bestInfo;
    return best;
  }
}
"@

$proc = Start-Process -FilePath $Exe -PassThru
try {
  Start-Sleep -Milliseconds $WaitMs
  if ($proc.HasExited) { throw "Process exited early with code $($proc.ExitCode)." }

  $info = ""
  $hwnd = [WinCap]::Best([uint32]$proc.Id, [ref]$info)
  if ($hwnd -eq [IntPtr]::Zero) { throw "No visible window found for pid $($proc.Id)." }
  Write-Host "window: $info"

  [void][WinCap]::ShowWindow($hwnd, 9)
  [void][WinCap]::SetForegroundWindow($hwnd)
  Start-Sleep -Milliseconds 600

  $r = New-Object WinCap+RECT
  [void][WinCap]::GetWindowRect($hwnd, [ref]$r)
  $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top

  $bmp = New-Object System.Drawing.Bitmap($w, $h)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  $ok = [WinCap]::PrintWindow($hwnd, $hdc, 2)
  $g.ReleaseHdc($hdc)
  if (-not $ok) {
    Write-Host "PrintWindow failed; falling back to screen capture."
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
  }
  $g.Dispose()

  $full = [System.IO.Path]::GetFullPath($Out)
  [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($full)) | Out-Null
  $bmp.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()
  Write-Host "saved: $full ($w x $h)"
}
finally {
  if ($proc -and -not $proc.HasExited) { $proc.Kill(); $proc.WaitForExit(3000) | Out-Null }
}
