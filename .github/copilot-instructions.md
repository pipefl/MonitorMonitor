# MonitorMonitor Project Setup

## Project Overview
.NET Console application for Windows CLI utility (mmcli.exe) that saves and loads multi-monitor configurations using Windows Display APIs.

## Setup Checklist

- [x] Verify copilot-instructions.md file created
- [x] Clarify Project Requirements - Windows native CLI for monitor management
- [ ] Scaffold the Project - Create .NET Console app with Windows API support
- [ ] Customize the Project - Implement monitor config save/load functionality
- [ ] Install Required Extensions - None required
- [ ] Compile the Project - Build to mmcli.exe
- [ ] Create and Run Task - Not needed for CLI utility
- [ ] Launch the Project - Test CLI commands
- [ ] Ensure Documentation is Complete - README with usage examples

## Technical Requirements
- .NET 9.0 Console Application
- Windows APIs: EnumDisplayDevices, EnumDisplaySettings, ChangeDisplaySettingsEx
- Commands: `mmcli -save <profile>`, `mmcli -load <profile>`
- Profile storage: JSON files in local config directory
