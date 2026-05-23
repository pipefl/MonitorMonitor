using System.Runtime.InteropServices;

namespace mmcli;

public static class DisplayConfig
{
    public class MonitorIdentity
    {
        public string GdiDeviceName { get; set; } = string.Empty;
        public string MonitorDevicePath { get; set; } = string.Empty;
        public string MonitorFriendlyName { get; set; } = string.Empty;
        public ushort ManufacturerId { get; set; }
        public ushort ProductCodeId { get; set; }
        public uint ConnectorInstance { get; set; }
        public uint OutputTechnology { get; set; }
    }

    public static Dictionary<string, MonitorIdentity> GetActiveIdentities()
    {
        var result = new Dictionary<string, MonitorIdentity>(StringComparer.OrdinalIgnoreCase);

        int err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount);
        if (err != ERROR_SUCCESS) return result;

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        if (err != ERROR_SUCCESS) return result;

        for (int i = 0; i < pathCount; i++)
        {
            var path = paths[i];
            if ((path.flags & DISPLAYCONFIG_PATH_ACTIVE) == 0) continue;

            // Source name -> GDI device name (\\.\DISPLAYn)
            var src = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            src.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            src.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            src.header.adapterId = path.sourceInfo.adapterId;
            src.header.id = path.sourceInfo.id;
            if (DisplayConfigGetDeviceInfo(ref src) != ERROR_SUCCESS) continue;

            // Target name -> EDID identity
            var tgt = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            tgt.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            tgt.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            tgt.header.adapterId = path.targetInfo.adapterId;
            tgt.header.id = path.targetInfo.id;
            if (DisplayConfigGetDeviceInfo(ref tgt) != ERROR_SUCCESS) continue;

            // Skip if EDID IDs not valid (driver-virtual displays etc.)
            bool edidValid = (tgt.flags & 0x1) != 0; // friendlyNameFromEdid bit
            string devicePath = tgt.monitorDevicePath ?? string.Empty;

            var identity = new MonitorIdentity
            {
                GdiDeviceName = src.viewGdiDeviceName ?? string.Empty,
                MonitorDevicePath = devicePath,
                MonitorFriendlyName = tgt.monitorFriendlyDeviceName ?? string.Empty,
                ManufacturerId = tgt.edidManufactureId,
                ProductCodeId = tgt.edidProductCodeId,
                ConnectorInstance = tgt.connectorInstance,
                OutputTechnology = (uint)tgt.outputTechnology,
            };

            if (!string.IsNullOrEmpty(identity.GdiDeviceName))
            {
                result[identity.GdiDeviceName] = identity;
            }
        }

        return result;
    }

    public static string DecodeManufacturerId(ushort id)
    {
        if (id == 0) return string.Empty;
        // EDID PNP ID: 3 chars, 5 bits each, packed in big-endian, A=1
        ushort be = (ushort)((id >> 8) | (id << 8));
        char c1 = (char)('A' - 1 + ((be >> 10) & 0x1F));
        char c2 = (char)('A' - 1 + ((be >> 5) & 0x1F));
        char c3 = (char)('A' - 1 + (be & 0x1F));
        return $"{c1}{c2}{c3}";
    }

    private const int ERROR_SUCCESS = 0;
    private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    private const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;
    private const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        [MarshalAs(UnmanagedType.Bool)] public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        // Followed by 48 bytes of union (sourceMode/targetMode/desktopImageInfo).
        // We never read these; Size=64 forces correct stride.
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);
}
