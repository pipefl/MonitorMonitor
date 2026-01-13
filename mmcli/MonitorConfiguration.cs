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
        
        // Build fingerprint map for currently active displays
        Dictionary<string, string> fingerprintToDevice = new Dictionary<string, string>();
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);
        List<string> attachedDevices = new List<string>();
        
        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                attachedDevices.Add(d.DeviceName);
                
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
                {
                    string fingerprint = $"{dm.dmPelsWidth}x{dm.dmPelsHeight}@{dm.dmDisplayFrequency}Hz";
                    fingerprintToDevice[fingerprint] = d.DeviceName;
                }
            }
            d.cb = Marshal.SizeOf(d);
        }

        // STEP 1: Enable/configure monitors from profile FIRST (before disabling others)
        foreach (var monitor in monitors)
        {
            if (monitor.IsAttached)
            {
                string fingerprint = $"{monitor.Width}x{monitor.Height}@{monitor.Frequency}Hz";
                string? targetDevice = fingerprintToDevice.ContainsKey(fingerprint) ? fingerprintToDevice[fingerprint] : null;
                
                if (targetDevice == null)
                {
                    // Monitor not currently active - find available disabled display that supports the mode
                    Console.WriteLine($"Monitor {fingerprint} not active, searching for compatible display...");
                    
                    DISPLAY_DEVICE searchD = new DISPLAY_DEVICE();
                    searchD.cb = Marshal.SizeOf(searchD);
                    
                    for (uint id = 0; EnumDisplayDevices(null, id, ref searchD, 0); id++)
                    {
                        if ((searchD.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == 0)
                        {
                            // Check if this display supports the desired resolution
                            bool supportsMode = false;
                            DEVMODE testDm = new DEVMODE();
                            testDm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                            
                            for (int modeNum = 0; EnumDisplaySettings(searchD.DeviceName, modeNum, ref testDm); modeNum++)
                            {
                                if (testDm.dmPelsWidth == monitor.Width && 
                                    testDm.dmPelsHeight == monitor.Height &&
                                    testDm.dmDisplayFrequency == monitor.Frequency)
                                {
                                    supportsMode = true;
                                    break;
                                }
                            }
                            
                            if (supportsMode)
                            {
                                targetDevice = searchD.DeviceName;
                                Console.WriteLine($"  Found compatible display: {targetDevice}");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"  {searchD.DeviceName} doesn't support {fingerprint}");
                            }
                        }
                        searchD.cb = Marshal.SizeOf(searchD);
                    }
                    
                    if (targetDevice == null)
                    {
                        Console.WriteLine($"Warning: No compatible display output for {fingerprint}");
                        continue;
                    }
                }
                
                Console.WriteLine($"Configuring {fingerprint} on {targetDevice}...");
                
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                dm.dmDeviceName = targetDevice;
                dm.dmPelsWidth = monitor.Width;
                dm.dmPelsHeight = monitor.Height;
                dm.dmBitsPerPel = monitor.BitsPerPixel;
                dm.dmDisplayFrequency = monitor.Frequency;
                dm.dmPositionX = monitor.PositionX;
                dm.dmPositionY = monitor.PositionY;
                dm.dmDisplayOrientation = monitor.Orientation;
                dm.dmFields = 0x1C0000 | 0x20 | 0x80000 | 0x100000 | 0x40000;

                int result = ChangeDisplaySettingsEx(targetDevice, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
                
                if (result != DISP_CHANGE_SUCCESSFUL)
                {
                    Console.WriteLine($"Warning: Failed to configure {targetDevice} (Error code: {result})");
                    success = false;
                }
            }
        }
        
        // STEP 2: Now disable monitors that shouldn't be enabled
        d.cb = Marshal.SizeOf(d);
        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                DEVMODE currentDm = new DEVMODE();
                currentDm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref currentDm))
                {
                    bool shouldBeEnabled = monitors.Any(m => m.IsAttached && 
                        m.Width == currentDm.dmPelsWidth && 
                        m.Height == currentDm.dmPelsHeight &&
                        m.Frequency == currentDm.dmDisplayFrequency);
                    
                    if (!shouldBeEnabled)
                    {
                        Console.WriteLine($"Disabling {d.DeviceName} ({currentDm.dmPelsWidth}x{currentDm.dmPelsHeight}@{currentDm.dmDisplayFrequency}Hz)...");
                        
                        DEVMODE dmDisable = new DEVMODE();
                        dmDisable.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                        dmDisable.dmPelsWidth = 0;
                        dmDisable.dmPelsHeight = 0;
                        dmDisable.dmPositionX = 0;
                        dmDisable.dmPositionY = 0;
                        dmDisable.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;
                        
                        int result = ChangeDisplaySettingsEx(d.DeviceName, ref dmDisable, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
                        
                        if (result != DISP_CHANGE_SUCCESSFUL)
                        {
                            Console.WriteLine($"Warning: Failed to disable {d.DeviceName} (Error code: {result})");
                        }
                    }
                }
            }
            d.cb = Marshal.SizeOf(d);
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
