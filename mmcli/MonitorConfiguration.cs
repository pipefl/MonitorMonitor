using System.Runtime.InteropServices;

namespace mmcli;

public class MonitorConfiguration
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [Flags]
    public enum DisplayDeviceStateFlags : uint
    {
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        PrimaryDevice = 0x4,
        MirroringDriver = 0x8,
        VGACompatible = 0x10,
        Removable = 0x20,
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    public const int ENUM_CURRENT_SETTINGS = -1;
    public const int CDS_UPDATEREGISTRY = 0x01;
    public const int CDS_TEST = 0x02;
    public const int CDS_NORESET = 0x10000000;
    public const int CDS_RESET = 0x40000000;
    public const int DISP_CHANGE_SUCCESSFUL = 0;
    public const int DISP_CHANGE_RESTART = 1;
    public const int DISP_CHANGE_FAILED = -1;
    
    // DEVMODE field flags
    public const int DM_PELSWIDTH = 0x80000;
    public const int DM_PELSHEIGHT = 0x100000;
    public const int DM_POSITION = 0x20;

    public class MonitorInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceString { get; set; } = string.Empty;
        public bool IsAttached { get; set; }
        public bool IsPrimary { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Frequency { get; set; }
        public int BitsPerPixel { get; set; }
        public int Orientation { get; set; }
    }

    public static List<MonitorInfo> GetCurrentConfiguration()
    {
        var monitors = new List<MonitorInfo>();
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);
        
        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                
                if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
                {
                    var monitor = new MonitorInfo
                    {
                        DeviceName = d.DeviceName,
                        DeviceString = d.DeviceString,
                        IsAttached = true,
                        IsPrimary = (d.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0,
                        PositionX = dm.dmPositionX,
                        PositionY = dm.dmPositionY,
                        Width = dm.dmPelsWidth,
                        Height = dm.dmPelsHeight,
                        Frequency = dm.dmDisplayFrequency,
                        BitsPerPixel = dm.dmBitsPerPel,
                        Orientation = dm.dmDisplayOrientation
                    };
                    monitors.Add(monitor);
                }
            }
            
            d.cb = Marshal.SizeOf(d);
        }
        
        return monitors;
    }

    public static bool ApplyConfiguration(List<MonitorInfo> monitors)
    {
        bool success = true;
        
        // Get all currently attached displays
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);
        List<string> attachedDevices = new List<string>();
        
        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                attachedDevices.Add(d.DeviceName);
            }
            d.cb = Marshal.SizeOf(d);
        }

        // First, disable monitors not in the profile
        foreach (var deviceName in attachedDevices)
        {
            bool inProfile = monitors.Any(m => m.DeviceName == deviceName && m.IsAttached);
            
            if (!inProfile)
            {
                Console.WriteLine($"Disabling {deviceName}...");
                
                // Create DEVMODE with zero resolution to detach the display
                DEVMODE dmDisable = new DEVMODE();
                dmDisable.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                dmDisable.dmPelsWidth = 0;
                dmDisable.dmPelsHeight = 0;
                dmDisable.dmPositionX = 0;
                dmDisable.dmPositionY = 0;
                dmDisable.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;
                
                int result = ChangeDisplaySettingsEx(deviceName, ref dmDisable, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
                
                if (result != DISP_CHANGE_SUCCESSFUL)
                {
                    Console.WriteLine($"Warning: Failed to disable {deviceName} (Error code: {result})");
                }
            }
        }
        
        // Then, configure monitors that should be enabled
        foreach (var monitor in monitors)
        {
            if (monitor.IsAttached)
            {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                dm.dmDeviceName = monitor.DeviceName;
                dm.dmPelsWidth = monitor.Width;
                dm.dmPelsHeight = monitor.Height;
                dm.dmBitsPerPel = monitor.BitsPerPixel;
                dm.dmDisplayFrequency = monitor.Frequency;
                dm.dmPositionX = monitor.PositionX;
                dm.dmPositionY = monitor.PositionY;
                dm.dmDisplayOrientation = monitor.Orientation;
                dm.dmFields = 0x1C0000 | 0x20 | 0x80000 | 0x100000 | 0x40000; // Position, PelsWidth, PelsHeight, BitsPerPel, DisplayFrequency

                int result = ChangeDisplaySettingsEx(monitor.DeviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
                
                if (result != DISP_CHANGE_SUCCESSFUL)
                {
                    Console.WriteLine($"Warning: Failed to prepare settings for {monitor.DeviceName} (Error code: {result})");
                    success = false;
                }
            }
        }

        // Apply all changes at once
        int finalResult = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (finalResult != DISP_CHANGE_SUCCESSFUL)
        {
            Console.WriteLine($"Warning: Final apply returned code: {finalResult}");
            success = false;
        }

        return success;
    }
}
