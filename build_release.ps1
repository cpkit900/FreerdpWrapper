$ErrorActionPreference = "Stop"

$version   = "v3.3.0"
$baseDir   = "c:\Users\Junnu\Freerdp Wrapper"
$releaseDir = "$baseDir\FreeRdpWrapper_Release"
$publishDir = "$baseDir\PublishOutput"
$zipFile   = "$baseDir\FreeRdpWrapper_$version.zip"

Write-Host "Cleaning up old directories..."
if (Test-Path $releaseDir) { Remove-Item -Recurse -Force $releaseDir }
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $zipFile)    { Remove-Item -Force $zipFile }

Write-Host "Publishing .NET App..."
dotnet publish "$baseDir\FreeRdpWrapper.csproj" -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir

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

Release Date : 2026-11-04
FreeRDP Base : 3.33.0 (SDL3 client, D3D11 renderer)

## Changes in $version

### Upgraded FreeRDP from 3.32.2 to 3.33.0
This is a major FreeRDP feature release with 13 CVE security fixes and
numerous improvements. Highlights relevant to this wrapper:

### Security Fixes (13 CVEs)
- Fixed multiple security vulnerabilities across codec, core, and channel
  subsystems. See FreeRDP 3.33.0 security advisories for full details.

### SDL3 Client Improvements
- RemoteApp (RAIL) support for Windows — popups, window positioning, sizing,
  minimize, and activation now work correctly on Windows.
- Fixed fullscreen toggle releasing Ctrl+Alt properly on the server side,
  preventing stuck modifier keys after Alt+Ctrl+Enter.
- Wheel/scroll input now sends one axis per event (fixes erratic scrolling).
- Fixed duplicate presentation in SDL3 input dialogs.
- Fixed monitor order with /monitors: flag on multi-display setups.
- Connection dialog can now be turned off; queued messages are drained
  before the dialog is dismissed, preventing missed error messages.
- Display layout is resent when the display channel reconnects.

### wfreerdp Client Improvements
- Fixed fullscreen toggle — Ctrl+Alt+Enter now works via WM_SYSKEYDOWN and
  releases Ctrl/Alt on the server before toggling.
- Keyboard hook correctly resets key states on release events during
  focus flip, preventing permanently stuck keys.

### Core / Protocol
- Experimental UDP multitransport support for direct RDP connections.
- Broker redirect detection for gateway scenarios.
- Gateway (RDG) header parsing fixes — partially read headers no longer
  cause connection drops.
- Kerberos realm prompter fixes for domain authentication.
- Auto-reconnect now stops when a reconnect attempt is rejected by server.
- DisconnectProviderUltimatum reason encoding fix.
- Input events are discarded before activation (prevents stale input).

### Codec / Audio
- Opus microphone encoding fix (audio capture reliability).
- Sound channel accepts format names in /sound:format: option.
- Dropped undecodable audio waves no longer stop playback.
- RDPSND WinMM backend keeps audio queued ahead for smoother playback.
- Pulse backend has default target latency and silence padding on restart.

### Clipboard
- Windows clipboard: image conversion, BMP layout, and UTF-8 text fixes.
- HTML clipboard synthesizer destination format checks corrected.
- FileGroupDescriptorW to file list conversion fixes.
- Handles CB_RESPONSE_FAIL and malformed FAIL responses gracefully.

### Other Notable Changes
- Smartcard logon partial domain match and inverted domain match fixes.
- OpenH264 2.6 (libopenh264.so.8) loader support.
- PNG write: aborts on size wrap or realloc failure (prevents corruption).
- Stream pool uses InterlockedDecrement for thread safety.

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
