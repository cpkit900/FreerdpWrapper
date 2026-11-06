# FreeRDP Wrapper

A tabbed, multi-session FreeRDP desktop client wrapper for Windows built on .NET 10 and FreeRDP 3 (SDL3 / wfreerdp).

## Features
- **Tabbed Interface**: Manage and switch multiple active RDP and SSH sessions in a single unified window.
- **Client Engines**: Supports `sdl3-freerdp` (SDL3 hardware-accelerated D3D11 rendering), `wfreerdp` (native Win32), and `xfreerdp` (X11 / Linux).
- **Embedded Integration**: Native child-window embedding inside tab panels with automated keyboard focus routing and resize handling.
- **Hardware & Protocol**: Dynamic resolution, multi-monitor support, audio redirection, smartcard, drive mapping, and UDP multitransport toggle.
- **Credential Security**: Local profile configuration with DPAPI encryption.

## License

This project is licensed under the **Apache License, Version 2.0** (an OSI-approved open source license).  
See the [LICENSE](LICENSE) file for the full license text.
