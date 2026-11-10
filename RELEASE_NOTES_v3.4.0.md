# AeroRemote Desktop v3.4.0
**Release Date**: 2026-11-10  
**FreeRDP Base**: 3.33.0 (SDL3 client, D3D11 renderer)

---

## 🚀 Highlights & Changes in v3.4.0

### 💼 Unified Workspace Explorer
* **Three-tier Unified Tree**: Replaced split panes with a unified root navigation hierarchy:
  * **🌐 Connections**: Folders, groups, and endpoints (RDP, SSH, SDL3, Linux XRDP).
  * **📜 Saved Scripts**: PowerShell & Bash automation scripts.
  * **🔑 Credentials**: Stored identity credentials protected by AES-256-GCM encryption.
* **Overlap Fixed**: Corrected the WinForms docking calculation z-order so the search filter panel no longer paints on top of the first tree entries.
* **Clean Tree UI**: Removed cluttered checkboxes from all tree items.
* **Live Search**: Real-time filtering across connections, automation scripts, and credentials simultaneously.

### 🔑 Stored Credentials Vault (AES-256-GCM)
* **Identity Management**: Added `SavedCredential` model and secure storage in `%APPDATA%\FreeRdpWrapper\credentials.json`.
* **Credential Editor**: Full dialog to add and edit credentials with title, username, password, domain, and notes, featuring an `👁️` toggle to reveal/mask passwords.
* **Quick Auto-Fill**: Added a **`🔑`** button in Connection Properties to instantly populate username, password, and domain from the vault.
* **Security & Clipboard Protection**: Passwords copied from credentials auto-wipe from the clipboard after 30 seconds.

### 📜 Automation Scripts Engine
* **Script Management**: Create, edit, and store reusable multi-line Bash and PowerShell automation scripts.
* **Direct Execution**: Right-click any SSH connection to run scripts directly, or right-click any script to execute on a chosen target server.
* **Live Logging**: Real-time output streaming directly in the application logs drawer.

### 🌐 Network & Protocol Scanner
* **Host Discovery**: Devolutions-style network scanner with local subnet adapter selector and ARP table discovery.
* **Concurrent Probing**: Concurrent TCP port reachability checks (3389 for RDP, 22 for SSH).
* **Batch Import**: One-click import of discovered network hosts into connection groups.

### 🛡️ FreeRDP 3.33.0 & SDL3 Engine
* **Security**: Built against FreeRDP 3.33.0 with 13 CVE security vulnerability patches.
* **Renderer**: Hardware-accelerated SDL3 client with D3D11 rendering and `/parent-window` native container embedding.
* **Capabilities**: RAIL RemoteApp support, audio redirection, drive redirection, and auto-reconnect.

---

## 📦 Installation & Usage
1. Download and extract **`FreeRdpWrapper_v3.4.0.zip`**.
2. Run **`FreeRdpWrapper.exe`** (FreeRDP 3.33.0 runtime and SDL3 binaries are bundled in `Bin\FreeRDP`).
