# mmcli - Multi-Monitor Configuration CLI

A Windows command-line utility for saving and loading multi-monitor display configurations.

## The problem. 
Windows has limited capabilities for quick monitor profile switching, especially if you have more than 2 displays. 

On a system with more than 2 displays, pressing Win-P still only produces a UI for switching between 2. 

<img width="377" height="323" alt="image" src="https://github.com/user-attachments/assets/50d81ba9-ef30-41f7-ba8b-33ce7301d48c" />

This is a problem if you have say, 3 displays and want to quickly swap between different display profiles. 

<img width="1014" height="319" alt="Screenshot 2026-01-12 191349" src="https://github.com/user-attachments/assets/7a3a23cd-2dfb-4867-9dcf-9444fb8dbc62" />

In this example, I have display 1, which is my primary 3440x1440 desktop display, a smaller "mini" 2560x720 display (3) for stats, youtube, etc. 

Display 2 is my wall-mounted 4k LG C3 which I switch to for couch-gaming. Using windows, I am unable to quickly swap between "displays 1 and 3" and "display 2". mmcli aims to correct this. 

## Requirements

- Windows OS
- .NET 9.0 Runtime

## Installation

Build the project:
```
dotnet build -c Release
```
(The executable will be located at: `bin\Release\net9.0\mmcli.exe`)

### or

Download the pre-compiled binary in the releases, and place it in a PATH locale. 

## Usage

### Save Current Configuration

1. Set up your monitors however you prefer using the default windows display manager.
2. Open up a command prompt window and save the current profile with:
   ```
   mmcli -save Profile1
   ```
3. Reconfigure your displays for another profile using the windows display manager.
4. Save that to it's own profile.
   ```
   mmcli -save Profile2
   ```

### Load Configuration
Apply a previously saved monitor configuration:
```
mmcli -load <profile_name>
```

Example:
```
mmcli -load Profile1
```

### List Saved Profiles
View all available profiles:
```
mmcli -list
```

### Show Current Configuration
Display detailed information about your current monitor setup:
```
mmcli -show
```

### Delete Profile
Remove a saved profile:
```
mmcli -delete <profile_name>
```

### Help
Display usage information:
```
mmcli -help
```

## Profile Storage

Profiles are stored as JSON files in:
```
%LocalAppData%\mmcli\profiles\
```

Typically: `C:\Users\<YourUsername>\AppData\Local\mmcli\profiles\`

## Example Workflow

```powershell
# Set up monitors 1 and 3, then save
mmcli -save 1and3

# Set up only monitor 2, then save  
mmcli -save just2

# Later, switch between configurations
mmcli -load 1and3
mmcli -load just2

# Check what profiles you have
mmcli -list
```

## Technical Details

The tool uses Windows Display APIs:
- `EnumDisplayDevices` - Enumerate display devices
- `EnumDisplaySettings` - Get current display settings
- `ChangeDisplaySettingsEx` - Apply display configuration changes

Each profile stores:
- Monitor device names and friendly names
- Resolution (width x height)
- Position (X, Y coordinates)
- Refresh rate
- Bits per pixel
- Primary monitor designation
- Display orientation

## License

This project is provided as-is for personal use.
