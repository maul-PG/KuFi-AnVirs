# KuFi AnVirs (Antivirus)

KuFi AnVirs is a Windows-based antivirus application developed using .NET 8 and WPF. The project focuses on real-time threat detection and malware remediation, specifically tailored for external storage devices (Flashdrives).

> **Project Status:** **Version 1.0 (Stable Release)**. Now featuring integrated Watchdog services and an official installer.

## Key Features
* **Real-time Guard:** Background file activity monitoring.
* **Flashdrive Rescue:** Automatic scanning and cleanup of shortcuts or hidden files on USB Drives.
* **SQLite Integration:** Persistent storage for scan logs and application settings.
* **Minimalist Dashboard:** A clean and lightweight user interface built with WPF and modern design principles.
* **Immortal Watchdog:** A dual-process protection system (`KuFi.Watchdog`). If the main application is forcefully terminated, the Watchdog will instantly resurrect it.
* **System Repair:** One-click utility to fix Windows Registry corruptions and system anomalies caused by malware.
* **SQLite Integration:** High-speed persistent storage for scan logs and security history.

## Important Usage Notes (Reminders)

* **Scanning the C:\ Drive:** We highly recommend **avoiding a direct scan of the entire C:\ drive** unless absolutely necessary. Due to deep heuristic analysis, scanning the system partition is possible but will take a significant amount of time.

* **Exiting the Application:** To fully close the application from the System Tray, you **must disable the Watchdog feature** in the Settings first. If the Watchdog is active, the app will automatically restart itself to prevent unauthorized termination.

## Tech Stack
- **Framework:** .NET 8
- **UI:** WPF (Windows Presentation Foundation)
- **Database:** SQLite
- **Logging:** Serilog

## Development Setup
1. Clone this repository:
   git clone [https://github.com/maul-PG/KuFi-AnVirs.git](https://github.com/maul-PG/KuFi-AnVirs.git)

2. Ensure .NET 8 SDK is installed.
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project src/KuFi.UI
   ```

## Official Release
Don't want to build from source? Download the official installer from the [Releases](https://github.com/maul-PG/KuFi-AnVirs/releases) section.

Run  [KuFi_AnVirs_v1.0_Setup.exe](KuFi_AnVirs_v1.0_Setup.exe)

Follow the installation wizard.



## License
Distributed under the [MIT License](license).
