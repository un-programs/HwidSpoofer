<p align="center">
  <img src="app_icon.png" width="120" alt="HwidSpoofer Logo"/>
</p>

<h1 align="center">HwidSpoofer</h1>

<p align="center">
  A lightweight Windows HWID spoofer built with WPF (.NET 8). Randomizes hardware identifiers at the registry level — no kernel driver required.
</p>

<p align="center">
  <a href="https://github.com/un-programs/HwidSpoofer/releases/latest/download/HwidSpoofer.exe">
    <img src="https://img.shields.io/badge/Download-HwidSpoofer.exe-4A6CF7?style=for-the-badge&logo=windows&logoColor=white" alt="Download"/>
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/github/v/release/un-programs/HwidSpoofer?color=4A6CF7&label=Version" alt="Version"/>
  <img src="https://img.shields.io/github/downloads/un-programs/HwidSpoofer/total?color=green&label=Downloads" alt="Downloads"/>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/Platform-Windows%20x64-0078D4?logo=windows" alt="Windows x64"/>
  <img src="https://img.shields.io/badge/License-MIT-green" alt="MIT License"/>
</p>

---

## Features

- **Registry-Level Spoofing** — Randomizes MachineGuid, ProductId, HwProfileGuid, SMBIOS data, and more
- **Hardware ID Cleanup** — Spoofs disk, SCSI, GPU, TPM, and monitor EDID identifiers
- **MAC Address Randomization** — Generates locally-administered unicast MACs and applies them via registry + adapter restart
- **NVIDIA ID Spoofing** — Randomizes ClientUUID, PersistenceIdentifier, and ChipsetMatchID
- **Network Identity Reset** — Hostname, domain, DHCPv6 DUID, and adapter timestamps
- **Windows Update IDs** — Resets SusClientId, PingID, and related tracking values
- **Anti-Cheat Cleanup** — Removes EasyAntiCheat, BattlEye, and Vanguard service entries and files
- **Cache & Tracking Wipe** — Clears temp files, prefetch, shadow copies, USN journals, and tracking logs
- **Process Killer** — Terminates game launchers (Steam, Epic, Riot, Origin) before spoofing
- **Driver Status Tab** — Real-time system check: admin status, Secure Boot, test signing, anti-cheat services, and spoof readiness
- **Single-File Executable** — Ships as one portable `.exe` with all dependencies embedded
- **Modern Dark UI** — Clean WPF interface with custom scrollbars and tab navigation

## Screenshots

<p align="center">
  <i>Screenshots coming soon</i>
</p>

## Requirements

- Windows 10/11 x64
- **Run as Administrator** (required for registry and service operations)

> No .NET runtime installation needed — the release binary is fully self-contained.

## Usage

1. **[Download HwidSpoofer.exe](https://github.com/un-programs/HwidSpoofer/releases/latest/download/HwidSpoofer.exe)**
2. Right-click → **Run as administrator**
3. Go to the **Serials** tab to view your current hardware identifiers
4. Close all game launchers (Steam, Epic, Riot, etc.)
5. Go to the **Spoofer** tab and click **Spoof**
6. Wait for the process to complete — watch the output log
7. **Restart your computer** for all changes to take effect

## Tabs

| Tab | Description |
|-----|-------------|
| **Spoofer** | Main spoofing interface with output log |
| **Serials** | Displays current hardware IDs (MAC, Volume Serial, CPU ID, etc.) |
| **Instructions** | Step-by-step usage guide |
| **Driver** | System status: admin, Secure Boot, anti-cheat services, spoof readiness |

## Building from Source

```bash
# Clone the repository
git clone https://github.com/un-programs/HwidSpoofer.git
cd HwidSpoofer

# Build in Debug mode (demo — no real changes applied)
dotnet build -c Debug

# Build in Release mode (real spoofing)
dotnet build -c Release

# Publish as single-file executable
dotnet publish -c Release -o publish
```

### Build Modes

| Mode | Behavior |
|------|----------|
| `Debug` | Demo mode — simulates all operations, no actual changes |
| `Release` | Live mode — applies real registry changes and cleanup |

## Project Structure

```
├── App.xaml / App.xaml.cs           # Application entry point
├── MainWindow.xaml / .cs            # Main window (dark themed UI)
├── Converters/
│   └── BoolConverters.cs            # XAML value converters
├── ViewModels/
│   ├── MainViewModel.cs             # Main MVVM view model
│   └── RelayCommand.cs              # ICommand implementation
├── Services/
│   ├── SpoofOrchestrator.cs         # Orchestrates all spoof operations
│   ├── RegistryCleanerService.cs    # Registry identifier spoofing
│   ├── HwidReaderService.cs         # Reads current hardware IDs
│   ├── MacChangerService.cs         # MAC address randomization
│   ├── ProcessKillerService.cs      # Game launcher / anti-cheat termination
│   ├── CacheCleanerService.cs       # Temp files, prefetch, tracking cleanup
│   └── DriverStatusService.cs       # System status checks
├── app.ico                          # Application icon
├── app.manifest                     # UAC elevation manifest
└── HwidSpooferCS.csproj             # Project file
```

## Disclaimer

This tool is provided for **educational and research purposes only**. Modifying hardware identifiers may violate the terms of service of certain software or online platforms. Use at your own risk. The author assumes no responsibility for any misuse or consequences arising from the use of this software.

## License

This project is licensed under the [MIT License](LICENSE).
