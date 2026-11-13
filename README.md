# AeroRemote Desktop (FreeRDP Wrapper)

[![Latest Release](https://img.shields.io/github/v/release/cpkit900/FreerdpWrapper?color=00ff66&label=Release)](https://github.com/cpkit900/FreerdpWrapper/releases/latest)
[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](https://github.com/cpkit900/FreerdpWrapper)
[![FreeRDP](https://img.shields.io/badge/FreeRDP-3.33.1-orange.svg)](https://github.com/FreeRDP/FreeRDP)

A modern, high-performance tabbed remote desktop connection manager and workspace for Windows, powered by **FreeRDP 3.33.1 (SDL3 / D3D11)** and **.NET WinForms**.

---

## ✨ Key Features

### 💼 Unified Workspace Explorer
* **Three-tier Organization**: Manage your entire infrastructure in a clean hierarchy:
  * **🌐 Connections**: RDP, SSH, SDL3, and Linux XRDP endpoints organized into folders and groups.
  * **📜 Saved Scripts**: Multi-line Bash and PowerShell automation scripts.
  * **🔑 Credentials**: Stored identity credentials with quick auto-fill into connections.
* **Simultaneous Live Filter**: Search across all endpoints, automation scripts, and credentials in real time.

### 🛡️ Authenticated AES-256-GCM Matrix Vault
* **Enterprise Security**: Secrets protected by AES-256-GCM authenticated encryption and optional PBKDF2 master password protection.
* **Windows DPAPI Fallback**: Seamless, passwordless security for personal desktop environments.
* **Vault Backup & Portability**: Encrypted export and import archives (`.frdpvault`) to safely transfer connection profiles between machines.
* **Auto-Wipe Clipboard**: Passwords copied to the clipboard automatically purge after 30 seconds.

### ⚡ Client Engines & SDL3 Embedding
* **SDL3 Client**: Hardware-accelerated Direct3D 11 rendering via `sdl3-freerdp` with native container embedding (`/parent-window`).
* **wfreerdp & xfreerdp**: Full backward compatibility with native Win32 and X11 FreeRDP clients.
* **Rich Protocol Features**: Dynamic resolution scaling, multi-monitor topology, audio/microphone redirection, drive redirection, and experimental UDP multitransport.

### 🌐 Network & Protocol Scanner
* **Devolutions-style Discovery**: Select any local network interface adapter to probe active subnets.
* **Hardware & Port Discovery**: ARP table reading, ICMP echo reachability, and concurrent TCP port checks (3389 for RDP, 22 for SSH).
* **Batch Import**: One-click import discovered hosts directly into organized connection groups.

### 📜 Remote Automation Scripts Engine
* **Embedded Script Editor**: Compose and test Bash and PowerShell automation commands with syntax helpers.
* **Direct Execution**: Right-click any SSH host to launch automation scripts with streaming output in the session logs drawer.

---

## 🚀 Quick Start

1. Download **`FreeRdpWrapper_v3.4.0.zip`** from the [Latest Releases](https://github.com/cpkit900/FreerdpWrapper/releases/latest).
2. Extract the archive into a folder of your choice.
3. Launch **`FreeRdpWrapper.exe`** (FreeRDP 3.33.1 and SDL3 binaries are bundled in `Bin\FreeRDP`).

---

## 🛠️ Building from Source

### Prerequisites
* Windows 10/11 x64 (Build 19041+)
* [.NET 9.0 or .NET 10.0 SDK](https://dotnet.microsoft.com/download)
* PowerShell 5.1+

### Build & Run
```powershell
# Clone the repository
git clone https://github.com/cpkit900/FreerdpWrapper.git
cd FreerdpWrapper

# Build debug binary
dotnet build

# Or package full release distribution
powershell -ExecutionPolicy Bypass -File .\build_release.ps1
```

---

## 📄 License

This project is licensed under the **Apache License, Version 2.0**. See the [LICENSE](LICENSE) file for details.
