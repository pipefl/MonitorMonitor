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

    public const int DM_PELSWIDTH = 0x80000;
    public const int DM_PELSHEIGHT = 0x100000;
    public const int DM_POSITION = 0x20;

    public class MonitorInfo
    {
        // Identity (EDID-derived; primary key for matching across reboots/replugs)
        public string MonitorDevicePath { get; set; } = string.Empty;
        public string MonitorFriendlyName { get; set; } = string.Empty;
        public ushort ManufacturerId { get; set; }
        public ushort ProductCodeId { get; set; }

        // Ephemeral: current GDI assignment. Not used for identity, only display/diagnostics.
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
        var identities = DisplayConfig.GetActiveIdentities();
        var monitors = new List<MonitorInfo>();
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);

        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();

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

                    if (identities.TryGetValue(d.DeviceName, out var ident))
                    {
                        monitor.MonitorDevicePath = ident.MonitorDevicePath;
                        monitor.MonitorFriendlyName = ident.MonitorFriendlyName;
                        monitor.ManufacturerId = ident.ManufacturerId;
                        monitor.ProductCodeId = ident.ProductCodeId;
                    }

                    monitors.Add(monitor);
                }
            }

            d.cb = Marshal.SizeOf(d);
        }

        return monitors;
    }

    /// <summary>
    /// Returns the list of monitors that cannot be safely saved (no stable EDID identity).
    /// Empty list means the configuration is OK to persist.
    /// </summary>
    public static List<MonitorInfo> ValidateForSave(List<MonitorInfo> monitors)
    {
        return monitors.Where(m => string.IsNullOrEmpty(m.MonitorDevicePath)).ToList();
    }

    public static string DescribeForUser(MonitorInfo m)
    {
        if (!string.IsNullOrEmpty(m.MonitorFriendlyName))
            return $"{m.MonitorFriendlyName} ({m.DeviceName})";
        if (!string.IsNullOrEmpty(m.DeviceString))
            return $"{m.DeviceString} ({m.DeviceName})";
        return m.DeviceName;
    }

    public static bool ApplyConfiguration(List<MonitorInfo> monitors)
    {
        // Refuse legacy profiles (no identity recorded) — they can't be matched reliably.
        var legacy = monitors.Where(m => string.IsNullOrEmpty(m.MonitorDevicePath)).ToList();
        if (legacy.Count > 0)
        {
            Console.WriteLine("Error: Profile is missing monitor identity information (saved before EDID tracking).");
            Console.WriteLine("       Please re-save the profile with the current version.");
            return false;
        }

        bool success = true;

        // Build a map of currently attached monitors keyed by identity.
        var current = GetCurrentConfiguration();
        var byIdentity = current
            .Where(m => !string.IsNullOrEmpty(m.MonitorDevicePath))
            .ToDictionary(m => m.MonitorDevicePath, StringComparer.OrdinalIgnoreCase);

        var profileIdentities = new HashSet<string>(
            monitors.Where(m => m.IsAttached).Select(m => m.MonitorDevicePath),
            StringComparer.OrdinalIgnoreCase);

        // STEP 1: Configure each monitor from the profile, addressing it by its CURRENT GDI name.
        // If a profile monitor's EDID identity isn't currently attached, fall back to enabling
        // an inactive GDI device that supports the requested mode (mirrors the original behavior).
        var usedInactiveDevices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var monitor in monitors)
        {
            if (!monitor.IsAttached) continue;

            string targetDeviceName;
            string targetLabel;

            if (byIdentity.TryGetValue(monitor.MonitorDevicePath, out var live))
            {
                targetDeviceName = live.DeviceName;
                targetLabel = DescribeForUser(live);
            }
            else
            {
                var fallback = FindInactiveDeviceForMode(monitor.Width, monitor.Height, monitor.Frequency, usedInactiveDevices);
                if (fallback == null)
                {
                    Console.WriteLine($"Warning: Monitor not currently attached and no compatible inactive display found: {monitor.MonitorFriendlyName} [{monitor.MonitorDevicePath}]");
                    success = false;
                    continue;
                }
                targetDeviceName = fallback;
                usedInactiveDevices.Add(fallback);
                targetLabel = $"{monitor.MonitorFriendlyName} (enabling on {fallback})";
            }

            Console.WriteLine($"Configuring {targetLabel} -> {monitor.Width}x{monitor.Height}@{monitor.Frequency}Hz");

            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            dm.dmDeviceName = targetDeviceName;
            dm.dmPelsWidth = monitor.Width;
            dm.dmPelsHeight = monitor.Height;
            dm.dmBitsPerPel = monitor.BitsPerPixel;
            dm.dmDisplayFrequency = monitor.Frequency;
            dm.dmPositionX = monitor.PositionX;
            dm.dmPositionY = monitor.PositionY;
            dm.dmDisplayOrientation = monitor.Orientation;
            dm.dmFields = 0x1C0000 | 0x20 | 0x80000 | 0x100000 | 0x40000;

            int result = ChangeDisplaySettingsEx(targetDeviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                Console.WriteLine($"Warning: Failed to configure {targetDeviceName} (Error code: {result})");
                success = false;
            }
        }

        // STEP 2: Disable any currently-attached monitor whose identity isn't in the profile.
        foreach (var live in current)
        {
            if (string.IsNullOrEmpty(live.MonitorDevicePath)) continue;
            if (profileIdentities.Contains(live.MonitorDevicePath)) continue;

            Console.WriteLine($"Disabling {DescribeForUser(live)}...");

            DEVMODE dmDisable = new DEVMODE();
            dmDisable.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            dmDisable.dmPelsWidth = 0;
            dmDisable.dmPelsHeight = 0;
            dmDisable.dmPositionX = 0;
            dmDisable.dmPositionY = 0;
            dmDisable.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;

            int result = ChangeDisplaySettingsEx(live.DeviceName, ref dmDisable, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                Console.WriteLine($"Warning: Failed to disable {live.DeviceName} (Error code: {result})");
                success = false;
            }
        }

        // Commit
        int finalResult = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (finalResult != DISP_CHANGE_SUCCESSFUL)
        {
            Console.WriteLine($"Warning: Final apply returned code: {finalResult}");
            success = false;
        }

        return success;
    }

    private static string? FindInactiveDeviceForMode(int width, int height, int frequency, HashSet<string> exclude)
    {
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);

        for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
        {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == 0
                && !exclude.Contains(d.DeviceName))
            {
                DEVMODE testDm = new DEVMODE();
                testDm.dmSize = (short)Marshal.SizeOf<DEVMODE>();

                for (int modeNum = 0; EnumDisplaySettings(d.DeviceName, modeNum, ref testDm); modeNum++)
                {
                    if (testDm.dmPelsWidth == width
                        && testDm.dmPelsHeight == height
                        && testDm.dmDisplayFrequency == frequency)
                    {
                        return d.DeviceName;
                    }
                }
            }
            d.cb = Marshal.SizeOf(d);
        }

        return null;
    }
}
