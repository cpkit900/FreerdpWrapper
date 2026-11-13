$ErrorActionPreference = "Stop"

$version   = "v3.4.0"
$baseDir   = "c:\Users\Junnu\Freerdp Wrapper"
$releaseDir = "$baseDir\FreeRdpWrapper_Release"
$publishDir = "$baseDir\PublishOutput"
$zipFile   = "$baseDir\FreeRdpWrapper_$version.zip"

Write-Host "Cleaning up old directories..."
if (Test-Path $releaseDir) { 
    try { Remove-Item -Recurse -Force $releaseDir -ErrorAction Stop } 
    catch { Get-ChildItem -Path $releaseDir | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue } 
}
if (Test-Path $publishDir) { 
    try { Remove-Item -Recurse -Force $publishDir -ErrorAction Stop } 
    catch { Get-ChildItem -Path $publishDir | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue } 
}
if (Test-Path $zipFile)    { Remove-Item -Force $zipFile -ErrorAction SilentlyContinue }

Write-Host "Publishing .NET App..."
dotnet publish "$baseDir\FreeRdpWrapper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir

Write-Host "Assembling release folder..."
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item "$publishDir\FreeRdpWrapper.exe" -Destination $releaseDir -Force

$binDir = "$releaseDir\Bin\FreeRDP"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null
Copy-Item "$baseDir\Bin\FreeRDP\Compiled\bin\*" -Destination $binDir -Recurse -Force

if (Test-Path "$baseDir\Assets") {
    $assetsDir = "$releaseDir\Assets"
    New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
    Copy-Item "$baseDir\Assets\*" -Destination $assetsDir -Recurse -Force
}

Write-Host "Writing release notes..."
@"
AeroRemote Desktop $version
==================================

Release Date : $(Get-Date -Format 'yyyy-MM-dd')
FreeRDP Base : 3.33.1 (SDL3 client, D3D11 renderer)

## What's New in $version

### 💼 Unified Workspace Explorer
- Replaced separate panes with a unified three-tier navigation hierarchy:
  - 🌐 Connections: Folders, servers, and endpoints (RDP, SSH, SDL3, XRDP).
  - 📜 Saved Scripts: PowerShell & Bash automation scripts.
  - 🔑 Credentials: Encrypted identity vault items.
- Fixed navigation search bar overlap by correcting the WinForms docking calculation z-order.
- Clean presentation without noisy checkboxes; execute scripts directly from context menus.
- Real-time live filtering across all connections, scripts, and credentials simultaneously.

### 🔑 Stored Credentials Vault (AES-256-GCM)
- Dedicated Credential Identity Manager with title, username, password, domain, and notes.
- Master password protection and Windows DPAPI authenticated envelope encryption.
- One-click '🔑' credential auto-fill in Connection Properties dialog.
- Secure clipboard copy for passwords with 30-second automated clipboard wipe.

### 📜 Automation Scripts Engine
- Built-in multi-line Bash and PowerShell script editor with syntax hints.
- Direct execution against target SSH hosts with real-time logs and output display.

### 🌐 Network & Protocol Scanner
- Devolutions-style network discovery tool with active subnet adapter selector.
- Scans ARP table and concurrent TCP port reachability (3389 RDP, 22 SSH).
- One-click batch import into connection groups.

### 🛡️ FreeRDP 3.33.1 & SDL3 Engine
- Built against FreeRDP 3.33.1.
- D3D11 accelerated SDL3 client with /parent-window embedding.
- RAIL RemoteApp, sound redirection, multi-monitor, and drive mapping.

## Usage Notes
- Unzip `FreeRdpWrapper_$version.zip`.
- Run `FreeRdpWrapper.exe` (all FreeRDP 3.33.1 binaries are pre-packaged in `Bin\FreeRDP`).
"@ | Set-Content "$releaseDir\RELEASE_NOTES.txt" -Encoding UTF8

Write-Host "Zipping release..."
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipFile -Force

Write-Host ""
Write-Host "=========================================="
Write-Host " Release $version packaged successfully!"
Write-Host " Output: $zipFile"
Write-Host "=========================================="
