using static mmcli.DisplayConfig;

namespace mmcli;

public class MonitorConfiguration
{
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

        // what load actually matches on. DeviceName is just whichever slot it was sitting in
        public string MonitorDevicePath { get; set; } = string.Empty;
        public string MonitorManufacturerCode { get; set; } = string.Empty;
        public string MonitorProductCode { get; set; } = string.Empty;
        //public string MonitorSerialNumber { get; set; } = string.Empty;
    }

    // one possible source->target hookup windows told us about
    private sealed class PathCandidate
    {
        public DISPLAYCONFIG_PATH_INFO Path;
        public bool Active;
        public string GdiName = string.Empty;
        public string FriendlyName = string.Empty;
        public string DevicePath = string.Empty;
        public string Manufacturer = string.Empty;
        public string Product = string.Empty;

        public string SourceKey => $"{Path.sourceInfo.adapterId.HighPart:X}:{Path.sourceInfo.adapterId.LowPart:X}/{Path.sourceInfo.id}";
        public string TargetKey => $"{Path.targetInfo.adapterId.HighPart:X}:{Path.targetInfo.adapterId.LowPart:X}/{Path.targetInfo.id}";
    }

    private static int BppFromPixelFormat(uint pixelFormat) => pixelFormat switch { 1 => 8, 2 => 16, 3 => 24, _ => 32 };
    private static uint PixelFormatFromBpp(int bpp) => bpp switch { 8 => 1u, 16 => 2u, 24 => 3u, _ => 4u };

    //public static List<MonitorInfo> GetCurrentConfiguration()
    //{
    //    var monitors = new List<MonitorInfo>();
    //    DISPLAY_DEVICE d = new DISPLAY_DEVICE();
    //    d.cb = Marshal.SizeOf(d);

    //    for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
    //    {
    //        if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
    //        {
    //            DEVMODE dm = new DEVMODE();
    //            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();

    //            if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
    //            {
    //                // Read EDID data for stable monitor matching
    //                var (serial, manufacturer, product) = ReadMonitorEDID(d.DeviceName);
    //                
    //                var monitor = new MonitorInfo
    //                {
    //                    DeviceName = d.DeviceName,
    //                    DeviceString = d.DeviceString,
    //                    IsAttached = true,
    //                    IsPrimary = (d.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0,
    //                    PositionX = dm.dmPositionX,
    //                    PositionY = dm.dmPositionY,
    //                    Width = dm.dmPelsWidth,
    //                    Height = dm.dmPelsHeight,
    //                    Frequency = dm.dmDisplayFrequency,
    //                    BitsPerPixel = dm.dmBitsPerPel,
    //                    Orientation = dm.dmDisplayOrientation,
    //                    MonitorSerialNumber = serial,
    //                    MonitorManufacturerCode = manufacturer,
    //                    MonitorProductCode = product
    //                };
    //                monitors.Add(monitor);
    //            }
    //        }

    //        d.cb = Marshal.SizeOf(d);
    //    }

    //    return monitors;
    //}

    public static List<MonitorInfo> GetCurrentConfiguration()
    {
        var monitors = new List<MonitorInfo>();
        if (!TryQuery(QDC_ONLY_ACTIVE_PATHS, out var paths, out var modes, out _))
            return monitors;

        foreach (var path in paths)
        {
            if ((path.flags & DISPLAYCONFIG_PATH_ACTIVE) == 0)
                continue;
            if (path.sourceInfo.modeInfoIdx >= modes.Length)
                continue;

            var src = modes[path.sourceInfo.modeInfoIdx].sourceMode;
            TryGetTargetName(path.targetInfo.adapterId, path.targetInfo.id, out var target);

            // hz lives on the path, fall back to the target mode if its blank
            var rr = path.targetInfo.refreshRate;
            if (rr.Denominator == 0 && path.targetInfo.modeInfoIdx < modes.Length)
                rr = modes[path.targetInfo.modeInfoIdx].targetMode.vSyncFreq;

            var monitor = new MonitorInfo
            {
                DeviceName = GetSourceName(path.sourceInfo.adapterId, path.sourceInfo.id),
                DeviceString = target.monitorFriendlyDeviceName ?? string.Empty,
                IsAttached = true,
                IsPrimary = src.position.x == 0 && src.position.y == 0,
                PositionX = src.position.x,
                PositionY = src.position.y,
                Width = (int)src.width,
                Height = (int)src.height,
                Frequency = rr.Denominator == 0 ? 0 : (int)Math.Round(rr.Numerator / (double)rr.Denominator),
                BitsPerPixel = BppFromPixelFormat(src.pixelFormat),
                Orientation = path.targetInfo.rotation == 0 ? 0 : (int)path.targetInfo.rotation - 1,
                MonitorDevicePath = target.monitorDevicePath ?? string.Empty,
                MonitorManufacturerCode = DecodeManufacturer(target.edidManufactureId),
                MonitorProductCode = target.edidProductCodeId == 0 ? string.Empty : target.edidProductCodeId.ToString("X4"),
            };
            if (string.IsNullOrWhiteSpace(monitor.DeviceString))
                monitor.DeviceString = $"{monitor.MonitorManufacturerCode} {monitor.MonitorProductCode}".Trim();

            monitors.Add(monitor);
        }

        return monitors.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.DeviceName).ToList();
    }

    //public static bool ApplyConfiguration(List<MonitorInfo> monitors)
    //{
    //    bool success = true;
    //    
    //    // Wake up displays before making changes
    //    WakeUpDisplays();

    //    DISPLAY_DEVICE d = new DISPLAY_DEVICE();
    //    d.cb = Marshal.SizeOf(d);
    //    List<string> attachedDevices = new List<string>();

    //    for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
    //    {
    //        if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
    //        {
    //            attachedDevices.Add(d.DeviceName);
    //        }
    //        d.cb = Marshal.SizeOf(d);
    //    }

    //    // Build a map of currently attached monitors with their EDID data
    //    var currentMonitors = GetCurrentConfiguration();

    //    foreach (var deviceName in attachedDevices)
    //    {
    //        // Try EDID matching first, then fall back to DeviceName
    //        bool inProfile = monitors.Any(m => 
    //            (m.MonitorSerialNumber != string.Empty && 
    //             currentMonitors.Any(cm => cm.MonitorSerialNumber == m.MonitorSerialNumber && m.IsAttached)) ||
    //            m.DeviceName == deviceName && m.IsAttached);

    //        if (!inProfile)
    //        {
    //            DEVMODE dmDisable = new DEVMODE();
    //            dmDisable.dmSize = (short)Marshal.SizeOf<DEVMODE>();
    //            dmDisable.dmPelsWidth = 0;
    //            dmDisable.dmPelsHeight = 0;
    //            dmDisable.dmPositionX = 0;
    //            dmDisable.dmPositionY = 0;
    //            dmDisable.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_POSITION;

    //            int result = ChangeDisplaySettingsEx(deviceName, ref dmDisable, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);

    //            if (result != DISP_CHANGE_SUCCESSFUL)
    //            {
    //                success = false;
    //            }
    //        }
    //    }

    //    var primary = monitors.FirstOrDefault(m => m.IsAttached && m.IsPrimary);
    //    var ordered = primary != null
    //        ? new[] { primary }.Concat(monitors.Where(m => m.IsAttached && m != primary))
    //        : monitors.Where(m => m.IsAttached);

    //    foreach (var monitor in ordered)
    //    {
    //        // Find the current device name for this monitor (EDID matching)
    //        var currentDevice = FindDeviceByEDID(currentMonitors, monitor);

    //        if (string.IsNullOrEmpty(currentDevice))
    //        {
    //            Console.WriteLine($"Warning: Could not find device for monitor {monitor.MonitorSerialNumber} (may be disconnected)");
    //            success = false;
    //            continue;
    //        }

    //        DEVMODE dm = new DEVMODE();
    //        dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
    //        dm.dmDeviceName = currentDevice;
    //        dm.dmPelsWidth = monitor.Width;
    //        dm.dmPelsHeight = monitor.Height;
    //        dm.dmBitsPerPel = monitor.BitsPerPixel;
    //        dm.dmDisplayFrequency = monitor.Frequency;
    //        dm.dmPositionX = monitor.PositionX;
    //        dm.dmPositionY = monitor.PositionY;
    //        dm.dmDisplayOrientation = monitor.Orientation;
    //        dm.dmFields = DM_POSITION | DM_BITSPERPEL | DM_PELSWIDTH | DM_PELSHEIGHT
    //                      | DM_DISPLAYFREQUENCY | DM_DISPLAYORIENTATION | DM_DISPLAYFLAGS;

    //        uint flags = (uint)(CDS_UPDATEREGISTRY | CDS_NORESET);
    //        if (monitor.IsPrimary)
    //        {
    //            flags |= (uint)CDS_SET_PRIMARY;
    //        }

    //        int result = ChangeDisplaySettingsEx(currentDevice, ref dm, IntPtr.Zero, flags, IntPtr.Zero);

    //        if (result != DISP_CHANGE_SUCCESSFUL)
    //        {
    //            success = false;
    //        }
    //    }

    //    int finalResult = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    //    if (finalResult != DISP_CHANGE_SUCCESSFUL)
    //    {
    //        success = false;
    //    }

    //    return success;
    //}

    ///// <summary>
    ///// Finds the current device name for a monitor by matching EDID data
    ///// </summary>
    //private static string FindDeviceByEDID(List<MonitorInfo> currentMonitors, MonitorInfo target)
    //{
    //    // Try exact serial match first
    //    if (!string.IsNullOrEmpty(target.MonitorSerialNumber))
    //    {
    //        var match = currentMonitors.FirstOrDefault(m => m.MonitorSerialNumber == target.MonitorSerialNumber);
    //        if (match != null && !string.IsNullOrEmpty(match.DeviceName))
    //        {
    //            return match.DeviceName;
    //        }
    //    }
    //    
    //    // Fallback: try manufacturer + product code
    //    if (!string.IsNullOrEmpty(target.MonitorManufacturerCode) && !string.IsNullOrEmpty(target.MonitorProductCode))
    //    {
    //        var match = currentMonitors.FirstOrDefault(m => 
    //            m.MonitorManufacturerCode == target.MonitorManufacturerCode && 
    //            m.MonitorProductCode == target.MonitorProductCode);
    //        if (match != null && !string.IsNullOrEmpty(match.DeviceName))
    //        {
    //            return match.DeviceName;
    //        }
    //    }
    //    
    //    // Last resort: fall back to DeviceName (for old profiles without EDID)
    //    return target.DeviceName;
    //}

    public static bool ApplyConfiguration(List<MonitorInfo> monitors, out string? error)
    {
        error = null;
        var wanted = monitors.Where(m => m.IsAttached).ToList();
        if (wanted.Count == 0)
        {
            error = "Profile has no monitors in it.";
            return false;
        }

        var candidates = GetCandidates(out var queryError);
        if (candidates == null)
        {
            error = $"QueryDisplayConfig failed (error {queryError}).";
            return false;
        }

        var newPaths = new List<DISPLAYCONFIG_PATH_INFO>();
        var newModes = new List<DISPLAYCONFIG_MODE_INFO>();
        var usedSources = new HashSet<string>();
        var usedTargets = new HashSet<string>();
        bool legacyProfile = false;

        // primary first so 0,0 lands where windows wants it
        foreach (var m in wanted.OrderByDescending(x => x.IsPrimary))
        {
            var matches = FindMatches(candidates, m, ref legacyProfile)
                .Where(c => !usedSources.Contains(c.SourceKey) && !usedTargets.Contains(c.TargetKey))
                .ToList();
            if (matches.Count == 0)
            {
                error = $"'{m.DeviceString}' isn't connected right now (is it powered on?).";
                return false;
            }

            // same slot it was saved in if we can get it, else whatever is lit now, else lowest
            var pick = matches
                .OrderByDescending(c => c.GdiName == m.DeviceName)
                .ThenByDescending(c => c.Active)
                .ThenBy(c => c.Path.sourceInfo.id)
                .First();

            var path = pick.Path;
            path.flags = DISPLAYCONFIG_PATH_ACTIVE;
            path.sourceInfo.modeInfoIdx = (uint)newModes.Count;
            path.sourceInfo.statusFlags = 0;
            path.targetInfo.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            path.targetInfo.statusFlags = 0;
            path.targetInfo.refreshRate = new DISPLAYCONFIG_RATIONAL { Numerator = (uint)Math.Max(m.Frequency, 0), Denominator = m.Frequency > 0 ? 1u : 0u };
            path.targetInfo.scanLineOrdering = m.Frequency > 0 ? DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE : DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED;
            path.targetInfo.rotation = (uint)Math.Clamp(m.Orientation + 1, 1, 4);
            path.targetInfo.scaling = DISPLAYCONFIG_SCALING_PREFERRED;

            newModes.Add(new DISPLAYCONFIG_MODE_INFO
            {
                infoType = DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE,
                id = path.sourceInfo.id,
                adapterId = path.sourceInfo.adapterId,
                sourceMode = new DISPLAYCONFIG_SOURCE_MODE
                {
                    width = (uint)m.Width,
                    height = (uint)m.Height,
                    pixelFormat = PixelFormatFromBpp(m.BitsPerPixel),
                    position = new POINTL { x = m.PositionX, y = m.PositionY },
                },
            });
            newPaths.Add(path);
            usedSources.Add(pick.SourceKey);
            usedTargets.Add(pick.TargetKey);
        }

        var pathArray = newPaths.ToArray();
        var modeArray = newModes.ToArray();
        const uint common = SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES;

        int rc = SetDisplayConfig((uint)pathArray.Length, pathArray, (uint)modeArray.Length, modeArray, SDC_VALIDATE | common);
        if (rc != ERROR_SUCCESS)
        {
            // exact hz didnt fly (239.989 vs 240 etc), let windows pick the rate
            for (int i = 0; i < pathArray.Length; i++)
            {
                pathArray[i].targetInfo.refreshRate = default;
                pathArray[i].targetInfo.scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED;
            }
            rc = SetDisplayConfig((uint)pathArray.Length, pathArray, (uint)modeArray.Length, modeArray, SDC_VALIDATE | common);
        }
        if (rc != ERROR_SUCCESS)
        {
            error = $"Windows rejected that layout (error {rc}).";
            if (legacyProfile)
                error += " This profile predates monitor identity, re-save it.";
            return false;
        }

        rc = SetDisplayConfig((uint)pathArray.Length, pathArray, (uint)modeArray.Length, modeArray, SDC_APPLY | SDC_SAVE_TO_DATABASE | common);
        if (rc != ERROR_SUCCESS)
        {
            error = $"SetDisplayConfig failed (error {rc}).";
            return false;
        }

        return true;
    }

    // exact monitor first, then same model on another port, then the old slot-name profiles
    private static List<PathCandidate> FindMatches(List<PathCandidate> candidates, MonitorInfo m, ref bool legacyProfile)
    {
        if (!string.IsNullOrEmpty(m.MonitorDevicePath))
        {
            var byPath = candidates.Where(c => string.Equals(c.DevicePath, m.MonitorDevicePath, StringComparison.OrdinalIgnoreCase)).ToList();
            if (byPath.Count > 0)
                return byPath;
        }

        if (!string.IsNullOrEmpty(m.MonitorProductCode))
        {
            var byModel = candidates.Where(c => c.Manufacturer == m.MonitorManufacturerCode && c.Product == m.MonitorProductCode).ToList();
            if (byModel.Count > 0)
                return byModel;
        }

        if (string.IsNullOrEmpty(m.MonitorDevicePath) && string.IsNullOrEmpty(m.MonitorProductCode))
        {
            legacyProfile = true;
            return candidates.Where(c => c.GdiName == m.DeviceName).ToList();
        }

        return new List<PathCandidate>();
    }

    // every path windows will let us light up, and who is on the far end of it
    private static List<PathCandidate>? GetCandidates(out int error)
    {
        if (!TryQuery(QDC_ALL_PATHS, out var paths, out _, out error))
            return null;

        var list = new List<PathCandidate>();
        foreach (var path in paths)
        {
            if (path.targetInfo.targetAvailable == 0)
                continue;
            if (!TryGetTargetName(path.targetInfo.adapterId, path.targetInfo.id, out var target))
                continue;

            list.Add(new PathCandidate
            {
                Path = path,
                Active = (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0,
                GdiName = GetSourceName(path.sourceInfo.adapterId, path.sourceInfo.id),
                FriendlyName = target.monitorFriendlyDeviceName ?? string.Empty,
                DevicePath = target.monitorDevicePath ?? string.Empty,
                Manufacturer = DecodeManufacturer(target.edidManufactureId),
                Product = target.edidProductCodeId == 0 ? string.Empty : target.edidProductCodeId.ToString("X4"),
            });
        }

        return list;
    }


    // old user32 + registry edid plumbing. never actually found the edid (null hklm handle, and it was
    // looking at the gpu's pci path) so every profile saved with blank identity. replaced by DisplayConfig.cs

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    //public struct DISPLAY_DEVICE
    //{
    //    [MarshalAs(UnmanagedType.U4)]
    //    public int cb;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    //    public string DeviceName;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    //    public string DeviceString;
    //    [MarshalAs(UnmanagedType.U4)]
    //    public DisplayDeviceStateFlags StateFlags;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    //    public string DeviceID;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    //    public string DeviceKey;
    //}

    //[Flags]
    //public enum DisplayDeviceStateFlags : uint
    //{
    //    AttachedToDesktop = 0x1,
    //    MultiDriver = 0x2,
    //    PrimaryDevice = 0x4,
    //    MirroringDriver = 0x8,
    //    VGACompatible = 0x10,
    //    Removable = 0x20,
    //    ModesPruned = 0x8000000,
    //    Remote = 0x4000000,
    //    Disconnect = 0x2000000
    //}

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    //public struct DEVMODE
    //{
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    //    public string dmDeviceName;
    //    public short dmSpecVersion;
    //    public short dmDriverVersion;
    //    public short dmSize;
    //    public short dmDriverExtra;
    //    public int dmFields;
    //    public int dmPositionX;
    //    public int dmPositionY;
    //    public int dmDisplayOrientation;
    //    public int dmDisplayFixedOutput;
    //    public short dmColor;
    //    public short dmDuplex;
    //    public short dmYResolution;
    //    public short dmTTOption;
    //    public short dmCollate;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    //    public string dmFormName;
    //    public short dmLogPixels;
    //    public int dmBitsPerPel;
    //    public int dmPelsWidth;
    //    public int dmPelsHeight;
    //    public int dmDisplayFlags;
    //    public int dmDisplayFrequency;
    //    public int dmICMMethod;
    //    public int dmICMIntent;
    //    public int dmMediaType;
    //    public int dmDitherType;
    //    public int dmReserved1;
    //    public int dmReserved2;
    //    public int dmPanningWidth;
    //    public int dmPanningHeight;
    //}

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    //[StructLayout(LayoutKind.Sequential)]
    //public struct MONITORINFOEX
    //{
    //    public int cbSize;
    //    public RECT rcMonitor;
    //    public RECT rcWork;
    //    public uint dwFlags;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    //    public string szDevice;
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct RECT
    //{
    //    public int left;
    //    public int top;
    //    public int right;
    //    public int bottom;
    //}

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern IntPtr MonitorFromPoint(int x, int y, uint flags);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    //public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    //// Power management for waking displays
    //[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    //private static extern void SetThreadExecutionState(uint esFlags);

    //private const uint ES_CONTINUOUS = 0x80000000;
    //private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    ///// <summary>
    ///// Wakes up all displays by simulating user activity
    ///// </summary>
    //private static void WakeUpDisplays()
    //{
    //    // Set thread execution state to prevent display sleep
    //    SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED);
    //}

    ///// <summary>
    ///// Reads EDID data from a monitor via its device interface
    ///// Returns serial number, manufacturer code, and product code
    ///// </summary>
    //private static (string serial, string manufacturer, string product) ReadMonitorEDID(string deviceName)
    //{
    //    var serial = string.Empty;
    //    var manufacturer = string.Empty;
    //    var product = string.Empty;

    //    try
    //    {
    //        // Get the monitor handle via EnumDisplayMonitors
    //        var monitorRect = new RECT();
    //        var monitorHandle = IntPtr.Zero;

    //        bool EnumMonitors(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    //        {
    //            var mi = new MONITORINFOEX();
    //            mi.cbSize = Marshal.SizeOf<MONITORINFOEX>();
    //            if (GetMonitorInfo(hMonitor, ref mi))
    //            {
    //                // Check if this monitor matches our device name
    //                if (mi.szDevice == deviceName)
    //                {
    //                    monitorHandle = hMonitor;
    //                    monitorRect = lprcMonitor;
    //                    return false; // Stop enumeration
    //                }
    //            }
    //            return true; // Continue enumeration
    //        }

    //        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitors, IntPtr.Zero);

    //        if (monitorHandle == IntPtr.Zero)
    //        {
    //            Console.WriteLine($"Warning: Could not find monitor handle for {deviceName}");
    //            return (string.Empty, string.Empty, string.Empty);
    //        }

    //        // Get monitor capabilities via GetMonitorInfo
    //        var mi2 = new MONITORINFOEX();
    //        mi2.cbSize = Marshal.SizeOf<MONITORINFOEX>();
    //        if (GetMonitorInfo(monitorHandle, ref mi2))
    //        {
    //            // The monitor rectangle gives us position info
    //            // For EDID, we need to read from the registry
    //            // EDID is stored in: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\DISPLAY\<DeviceID>\Device Parameters
    //            // The EDID value contains the raw EDID data
    //        }

    //        // Alternative: Read EDID from registry
    //        // This is more reliable than trying to parse from DEVMODE
    //        var devicePath = GetDevicePathFromDeviceName(deviceName);
    //        if (!string.IsNullOrEmpty(devicePath))
    //        {
    //            var edidData = ReadEDIDFromRegistry(devicePath);
    //            if (edidData != null && edidData.Length >= 128)
    //            {
    //                (serial, manufacturer, product) = ParseEDID(edidData);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Warning: Failed to read EDID for {deviceName}: {ex.Message}");
    //    }

    //    return (serial, manufacturer, product);
    //}

    ///// <summary>
    ///// Gets the device instance path from a display device name
    ///// </summary>
    //private static string GetDevicePathFromDeviceName(string deviceName)
    //{
    //    // DeviceName format: \\.\DISPLAY1, \\.\DISPLAY2, etc.
    //    // We need to find the corresponding device instance path
    //    // This requires EnumDisplayDevices with more details
    //    
    //    var d = new DISPLAY_DEVICE();
    //    d.cb = Marshal.SizeOf(d);

    //    for (uint id = 0; id < 10; id++) // Try up to 10 displays
    //    {
    //        if (EnumDisplayDevices(null, id, ref d, 0))
    //        {
    //            // Check if this matches our device name
    //            if (d.DeviceName == deviceName)
    //            {
    //                return d.DeviceID;
    //            }
    //        }
    //        d.cb = Marshal.SizeOf(d);
    //    }

    //    return string.Empty;
    //}

    ///// <summary>
    ///// Reads EDID data from the registry for a given device path
    ///// </summary>
    //private static byte[]? ReadEDIDFromRegistry(string devicePath)
    //{
    //    if (string.IsNullOrEmpty(devicePath))
    //        return null;

    //    try
    //    {
    //        // Registry path format: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\DISPLAY\<DeviceID>\Device Parameters
    //        // The EDID is stored in the "EDID" value
    //        // DevicePath format example: DISPLAY\GSM4D5F\4&1D2A5C1D&0&000000
    //        
    //        // Extract the display device identifier from the path
    //        // DevicePath looks like: DISPLAY\GSM4D5F\4&1D2A5C1D&0&000000
    //        // Registry path: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\DISPLAY\GSM4D5F\4&1D2A5C1D&0&000000\Device Parameters
    //        var parts = devicePath.Split('\\');
    //        if (parts.Length < 3)
    //            return null;
    //        
    //        // Build registry subkey path
    //        // Skip "DISPLAY" part, join the rest with backslashes
    //        var subKey = string.Join("\\", parts[1..]) + "\\Device Parameters";
    //        
    //        // Read EDID from registry using P/Invoke
    //        var edidBytes = ReadRegistryValue("HKLM", subKey, "EDID");
    //        
    //        return edidBytes;
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    ///// <summary>
    ///// Reads a registry value using P/Invoke
    ///// </summary>
    //private static byte[]? ReadRegistryValue(string hive, string subKey, string valueName)
    //{
    //    try
    //    {
    //        // Map hive to IntPtr
    //        IntPtr hKey = IntPtr.Zero;
    //        
    //        uint result;
    //        if (hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase))
    //        {
    //            result = RegOpenKeyEx(IntPtr.Zero, subKey, 0, KEY_READ, ref hKey);
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //        
    //        if (result != ERROR_SUCCESS)
    //            return null;
    //        
    //        try
    //        {
    //            // First call to get the size of the data
    //            uint dataType = 0;
    //            byte[] data = null;
    //            uint dataSize = 0;
    //            
    //            result = RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref dataType, null, ref dataSize);
    //            if (result != ERROR_SUCCESS && result != ERROR_MORE_DATA)
    //                return null;
    //            
    //            // Allocate buffer and read the data
    //            data = new byte[dataSize];
    //            result = RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref dataType, data, ref dataSize);
    //            
    //            if (result == ERROR_SUCCESS)
    //            {
    //                return data;
    //            }
    //            
    //            return null;
    //        }
    //        finally
    //        {
    //            if (hKey != IntPtr.Zero)
    //            {
    //                RegCloseKey(hKey);
    //            }
    //        }
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    //private const uint KEY_READ = 0x20019;
    //private const uint ERROR_SUCCESS = 0;
    //private const uint ERROR_MORE_DATA = 234;

    //[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    //private static extern uint RegOpenKeyEx(IntPtr hKey, string subKey, uint ulOptions, uint samDesired, ref IntPtr hKeyOut);

    //[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    //private static extern uint RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr lpReserved, ref uint lpType, [Out] byte[] lpData, ref uint lpcbData);

    //[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    //private static extern uint RegCloseKey(IntPtr hKey);

    ///// <summary>
    ///// Parses EDID data to extract serial number, manufacturer code, and product code
    ///// </summary>
    //private static (string serial, string manufacturer, string product) ParseEDID(byte[] edid)
    //{
    //    var serial = string.Empty;
    //    var manufacturer = string.Empty;
    //    var product = string.Empty;

    //    try
    //    {
    //        // EDID structure:
    //        // Bytes 0-15: Header (must be 00 FF FF FF FF FF FF 00)
    //        // Bytes 16-35: Manufacturer and product info
    //        // Bytes 36-39: Serial number (4 bytes)
    //        
    //        // Check EDID header
    //        if (edid.Length < 128 ||
    //            edid[0] != 0x00 || edid[1] != 0xFF || edid[2] != 0xFF || edid[3] != 0xFF ||
    //            edid[4] != 0xFF || edid[5] != 0xFF || edid[6] != 0xFF || edid[7] != 0x00)
    //        {
    //            return (string.Empty, string.Empty, string.Empty);
    //        }

    //        // Manufacturer code (bytes 8-9) - 3 character code
    //        manufacturer = ParseManufacturerCode(edid[8], edid[9]);

    //        // Product code (bytes 10-11) - little endian
    //        var productCode = BitConverter.ToUInt16(edid, 10);
    //        product = productCode.ToString("X");

    //        // Serial number (bytes 12-15) - little endian
    //        var serialNumber = BitConverter.ToUInt32(edid, 12);
    //        if (serialNumber != 0)
    //        {
    //            serial = serialNumber.ToString();
    //        }
    //        else
    //        {
    //            // Fallback: use manufacturer + product as pseudo-serial
    //            serial = $"{manufacturer}-{product}";
    //        }
    //    }
    //    catch
    //    {
    //        // If parsing fails, return empty strings (will fall back to DeviceName)
    //    }

    //    return (serial, manufacturer, product);
    //}

    ///// <summary>
    ///// Converts two bytes to a 3-character manufacturer code
    ///// </summary>
    //private static string ParseManufacturerCode(byte b1, byte b2)
    //{
    //    // Manufacturer code is stored as 5 bits per character (packed)
    //    // First byte: first 5 bits = char1, last 3 bits = first 3 bits of char2
    //    // Second byte: last 5 bits = remaining 3 bits of char2 + char3
    //    
    //    var c1 = ((b1 & 0xF8) >> 3) + 64;
    //    var c2 = (((b1 & 0x07) << 2) | ((b2 & 0xC0) >> 6)) + 64;
    //    var c3 = (b2 & 0x3F) + 64;

    //    return $"{(char)c1}{(char)c2}{(char)c3}";
    //}

    //public const int ENUM_CURRENT_SETTINGS = -1;
    //public const int CDS_UPDATEREGISTRY = 0x01;
    //public const int CDS_TEST = 0x02;
    //public const int CDS_SET_PRIMARY = 0x10;
    //public const int CDS_NORESET = 0x10000000;
    //public const int CDS_RESET = 0x40000000;
    //public const int DISP_CHANGE_SUCCESSFUL = 0;
    //public const int DISP_CHANGE_RESTART = 1;
    //public const int DISP_CHANGE_FAILED = -1;

    //public const int DM_POSITION = 0x20;
    //public const int DM_DISPLAYORIENTATION = 0x80;
    //public const int DM_BITSPERPEL = 0x40000;
    //public const int DM_PELSWIDTH = 0x80000;
    //public const int DM_PELSHEIGHT = 0x100000;
    //public const int DM_DISPLAYFLAGS = 0x200000;
    //public const int DM_DISPLAYFREQUENCY = 0x400000;
}
