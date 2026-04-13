$ErrorActionPreference = "Stop"

$version   = "v3.0.1"
$baseDir   = "c:\Users\Junnu\Freerdp Wrapper"
$releaseDir = "$baseDir\FreeRdpWrapper_Release"
$publishDir = "$baseDir\PublishOutput"
$zipFile   = "$baseDir\FreeRdpWrapper_$version.zip"

Write-Host "Cleaning up old directories..."
if (Test-Path $releaseDir) { Remove-Item -Recurse -Force $releaseDir }
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $zipFile)    { Remove-Item -Force $zipFile }

Write-Host "Publishing .NET App..."
dotnet publish "$baseDir\FreeRdpWrapper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir

Write-Host "Assembling release folder..."
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item "$publishDir\FreeRdpWrapper.exe" -Destination $releaseDir -Force

$binDir = "$releaseDir\Bin\FreeRDP"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null
Copy-Item "$baseDir\Bin\FreeRDP\Compiled\bin\*" -Destination $binDir -Recurse -Force

Write-Host "Writing release notes..."
@"
FreeRDP Wrapper $version
========================

Release Date : 2026-04-13
FreeRDP Base : 3.24.2 (SDL3 client, D3D11 renderer)

## Changes in $version

### Native SDL3 Parent-Window Embedding (NEW)
- FreeRDP's sdl3-freerdp.exe now correctly supports /parent-window:N.
  SDL3 creates its window normally then reparents it into the host HWND via
  Win32 SetParent(), stripping title-bar styles so it fills the tab seamlessly.
  Previously SDL_PROP_WINDOW_CREATE_WIN32_HWND_POINTER was misused, causing a
  fatal [handleShow] error on every connection attempt.

### Embedded Dialog Suppression (BUG FIX)
- When running embedded (parent-window supplied), the SDL3 connection dialog is
  never created and SDL_RunOnMainThread is never called. This eliminates the
  spurious [ERROR][handleShow] log error that appeared on every disconnect.

### Tab-Switch Repaint (BUG FIX)
- Added SDL_EVENT_WINDOW_EXPOSED / SDL_EVENT_WINDOW_SHOWN handlers that
  immediately blit the last D3D11 frame when a tab becomes visible.
  Previously SDL3 waited for the next server frame, causing a visible
  blank/stale delay on tab switch.

### wfreerdp Keyboard Hook (BUG FIX)
- The low-level WH_KEYBOARD_LL hook in wfreerdp now accepts input when focus
  is on any descendant child of hWndParent (IsChild check), or when the
  foreground window belongs to the same process as the parent. This fixes
  keyboard input when wfreerdp is embedded inside a .NET TabControl.

### Wrapper Cleanup
- Removed all pseudo-embedding infrastructure (SyncFloatingWindow, SetWindowPos
  sync loop, floating-window tracking, BringWindowToTop hacks).
- Clean native /parent-window embedding with EnumChildWindows + MoveWindow.
- Added RepaintRdpWindow() as a belt-and-suspenders C#-side repaint helper.

## Usage Notes
- Drop the Bin\FreeRDP folder alongside FreeRdpWrapper.exe.
- Select "sdl3-freerdp" as the client in connection settings.
- /parent-window is passed automatically; no extra flags needed.
"@ | Set-Content "$releaseDir\RELEASE_NOTES.txt" -Encoding UTF8

Write-Host "Zipping release..."
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipFile -Force

Write-Host ""
Write-Host "=========================================="
Write-Host " Release $version packaged successfully!"
Write-Host " Output: $zipFile"
Write-Host "=========================================="
