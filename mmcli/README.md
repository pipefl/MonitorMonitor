# mmcli - Multi-Monitor Configuration CLI

A Windows command-line utility for saving and loading multi-monitor display configurations.

## Features

- Save current monitor configuration to named profiles
- Load and apply saved monitor configurations
- List all saved profiles
- Show current monitor setup details
- Delete saved profiles
- Profiles stored as JSON files

## Requirements

- Windows OS
- .NET 9.0 Runtime

## Installation

1. Build the project:
   ```
   dotnet build -c Release
   ```

2. The executable will be located at: `bin\Release\net9.0\mmcli.exe`

3. (Optional) Add the executable location to your PATH for system-wide access

## Usage

### Save Current Configuration
Save your current monitor setup with a custom profile name:
```
mmcli -save <profile_name>
```

Example:
```
mmcli -save 1and3
mmcli -save just2
mmcli -save work-setup
```

### Load Configuration
Apply a previously saved monitor configuration:
```
mmcli -load <profile_name>
```

Example:
```
mmcli -load 1and3
```

**Note:** Some display changes may require logging out and back in to take full effect.

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

## Troubleshooting

**Configuration not applying correctly:**
- Try running as Administrator
- Log out and log back in after applying changes
- Verify the profile was saved correctly with `mmcli -show`

**No monitors detected:**
- Ensure monitors are physically connected and powered on
- Check Windows Display Settings to verify monitors are recognized

## License

This project is provided as-is for personal use.