# KuFi AnVirs (Antivirus)

KuFi AnVirs is a Windows-based antivirus application developed using .NET 8 and WPF. The project focuses on real-time threat detection and malware remediation, specifically tailored for external storage devices (Flashdrives).

> **Project Status:** Version 1.0-beta. Currently under active development. Folder scanning is currently limited to primary directories to ensure performance stability.

## Key Features
* **Real-time Guard:** Background file activity monitoring.
* **Flashdrive Rescue:** Automatic scanning and cleanup of shortcuts or hidden files on USB Drives.
* **SQLite Integration:** Persistent storage for scan logs and application settings.
* **Minimalist Dashboard:** A clean and lightweight user interface built with WPF and modern design principles.

## Tech Stack
* .NET 8 SDK
* WPF (Windows Presentation Foundation)
* SQLite (Entity Framework Core)
* Serilog (System Logging)

## Development Setup
1. Clone this repository:
   ```bash
   git clone https://github.com/maul-PG/KuFi-AnVirs.git
   ```
2. Ensure .NET 8 SDK is installed.
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project src/KuFi.UI
   ```

## User Guide
1. Download the executable from the [Releases] section.
2. Run `KuFi AnVirs.exe`.
3. Use the 'Scan Now' button on the dashboard to start scanning your Downloads and Documents folders.
4. Check the 'Logs' tab to view detection history.

## Development Roadmap v2.0
* Add 'Custom Scan' feature to select specific folders.
* Add a Stop button for active scan processes.
* Optimize scan engine for lower CPU usage.

## License
Distributed under the [MIT License](license).
