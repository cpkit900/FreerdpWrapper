# AeroRemote Desktop v3.4.0
**Release Date**: 2026-11-10  
**FreeRDP Base**: 3.33.1 (SDL3 client, D3D11 renderer)

---

## 🚀 What's New in v3.4.0

### 🌐 Network & Protocol Scanner
* **Header Select / Unselect All Checkbox**: Added a three-state master checkbox embedded directly over the `Source / Interface / IP` header column to toggle selection for all discovered hosts in one click.
* **Shortcuts & Context Menu**: Added `Ctrl+A` Select All shortcut and right-click menu with *Select All* and *Unselect All*.
* **Host Discovery & Concurrent Probing**: Scan any local adapter subnet, parse ARP tables, and check port 3389 (RDP) and 22 (SSH) simultaneously with one-click batch import.

### 💼 Unified Workspace Explorer & Clean Navigation
* **Clean Tree Display**: Navigation nodes now show clean Name headers by default (`🖥️ ServerName`, `🔑 Title`) eliminating clutter.
* **Customizable Headers**: New Settings options to toggle additional item metadata in tree labels:
  * Show Host / IP Address
  * Show Username
  * Show Live Ping / Reachability status indicators (`🟢` / `🔴`)
* **Live Search**: Simultaneous instant search across all connections, automation scripts, and credentials.
* **Z-Order & Layout Fixes**: Fixed search box overlap and tree docking calculations.

### 🔐 Authenticated AES-256-GCM Vault & Master Token
* **"Remember Password on this Machine"**: Option to save an authenticated DPAPI token in `%APPDATA%\FreeRdpWrapper\master.bin`.
* **Zero-Friction Startup**: Automatically unlocks on startup if `master.bin` exists and matches.
* **Security Control**: Simply delete `master.bin` (or click *Clear Saved Token* in Settings) to immediately re-enable password prompts. Includes a direct `📂 Open Folder` button in Settings.
* **Credential Vault**: Identity management with title, username, password, domain, and notes. One-click `🔑` autofill into Connection Properties and 30-second automated clipboard wipe.

### 📐 Workspace Layout & Window Persistence
* **Save Window Position & Size**: Remembers desktop position, dimensions, and window state (maximized/normal).
* **Save Workspace Layout & Tabs**: Saves navigation splitter distance and reopens active connection tabs cleanly on startup.
* **Theme Stability**: Fixed theme color changing across restarts; your selected theme is consistently preserved.
* **Active Sessions Warning**: Alerts you when closing the application with open connection tabs to prevent accidental disconnects.
* **App Icon**: Bundled `Assets\app.ico` ensuring high-resolution application icons in the taskbar and title bar.
* **Customizable & Movable Toolbars**: Interactive drag handles (`⠿`) and *Move Up* / *Move Down* controls in the Toolbar Customize dialog.

### 📜 Dual-Protocol Automation Scripts Engine
* **Windows Targets (WinRM)**: Execute automation scripts against Windows/RDP servers via PowerShell Remoting (`Invoke-Command`) with automatic credential negotiation.
* **Linux/Unix Targets (SSH)**: Execute Bash/shell commands via Windows native OpenSSH client.
* **Scalable Target Picker Dialog**: Designed for environments with 1,000s of servers, featuring real-time search, protocol filter pills, group filtering, and batch execution with unified live logging.
* **Enhanced Script Editor**: Full multi-line editing support for PowerShell and Bash scripts with return key handling, tab key indentation, and `Ctrl+S` quick save shortcut.

### 🛡️ FreeRDP 3.33.1 & SDL3 Engine
* **Security Patches**: Built against FreeRDP 3.33.1.
* **D3D11 Acceleration**: Hardware-accelerated SDL3 client with container embedding (`/parent-window`).
* **Protocol Features**: RAIL RemoteApp support, audio/microphone redirection, drive redirection, and multi-monitor topology.

---

## 📦 Download & Quick Start
1. Download **`FreeRdpWrapper_v3.4.0.zip`** below.
2. Extract the archive into any folder.
3. Launch **`FreeRdpWrapper.exe`** (FreeRDP 3.33.1 and SDL3 runtimes are bundled in `Bin\FreeRDP`).
