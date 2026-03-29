using System.Security.Cryptography;
using Microsoft.Win32;

namespace HwidSpoofer.Services;

public static class RegistryCleanerService
{
    private static readonly Random Rng = new();

    public static void CleanAll(Action<string>? log = null, SpoofOptions? options = null)
    {
        options ??= new SpoofOptions(true, true, true, true, true, true, true);

        var computerName = $"DESKTOP-{RandomHex(7).ToUpper()}";

        SpoofSystemIdentifiers(log);

        if (options.Diskdrive)
            SpoofHardwareIds(log);

        if (options.Gpu)
            SpoofNvidia(log);

        SpoofNetworkIds(log, computerName);
        SpoofWindowsUpdate(log);

        if (options.Bios || options.Cpu)
            SpoofWindowsNT(log);

        CleanMiscKeys(log);
        CleanAntiCheatKeys(log);
        CleanEpicKeys(log);
        CleanComputerName(log, computerName);
    }

    private static void SpoofSystemIdentifiers(Action<string>? log)
    {
        log?.Invoke("Spoofing system identifiers...");

        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\Cryptography", "MachineGuid");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\Cryptography", "GUID");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\SQMClient", "MachineId", braces: true);
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001", "HwProfileGuid", braces: true);
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\IDConfigDB\Hardware Profiles\0001", "GUID", braces: true);
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SystemInformation", "ComputerHardwareId", braces: true);

        SetRandomString(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SystemInformation", "ComputerHardwareIds");

        DeleteValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\mssmbios\Data", "SMBiosData");
    }

    private static void SpoofHardwareIds(Action<string>? log)
    {
        log?.Invoke("Spoofing hardware identifiers...");

        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\HardwareConfig", "LastConfig", braces: true);

        SetRandomString(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct");
        SetRandomString(Registry.LocalMachine, @"SYSTEM\HardwareConfig\Current", "BaseBoardProduct");

        // Disk identifiers
        string[] diskPaths =
        [
            @"HARDWARE\DESCRIPTION\System\MultifunctionAdapter\0\DiskController\0\DiskPeripheral\0",
            @"HARDWARE\DESCRIPTION\System\MultifunctionAdapter\0\DiskController\0\DiskPeripheral\1",
        ];
        foreach (var path in diskPaths)
            SetRandomString(Registry.LocalMachine, path, "Identifier");

        // SCSI identifiers
        string[] scsiPorts = ["Scsi Port 0", "Scsi Port 1"];
        foreach (var port in scsiPorts)
        {
            var scsiPath = $@"HARDWARE\DEVICEMAP\Scsi\{port}\Scsi Bus 0\Target Id 0\Logical Unit Id 0";
            SetRandomString(Registry.LocalMachine, scsiPath, "Identifier");
        }

        // GPU identifiers
        SetRandomGuid(Registry.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
            "UserModeDriverGUID", braces: true);
        SetRandomString(Registry.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
            "_DriverProviderInfo");

        // Video ID
        SetRandomGuid(Registry.LocalMachine,
            @"SYSTEM\ControlSet001\Services\BasicDisplay\Video",
            "VideoID", braces: true);

        // TPM
        SetRandomBytes(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\TPM\WMI", "WindowsAIKHash");
        SetRandomBytes(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\TPM\ODUID", "RandomSeed");

        // Monitor EDID spoofing
        SpoofMonitorEdid(log);
    }

    private static void SpoofMonitorEdid(Action<string>? log)
    {
        try
        {
            using var displayKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY");
            if (displayKey == null) return;

            foreach (var monitorName in displayKey.GetSubKeyNames())
            {
                using var monitorKey = displayKey.OpenSubKey(monitorName);
                if (monitorKey == null) continue;

                foreach (var instanceName in monitorKey.GetSubKeyNames())
                {
                    var paramPath = $@"{instanceName}\Device Parameters";
                    try
                    {
                        using var paramKey = monitorKey.OpenSubKey(paramPath, writable: true);
                        if (paramKey?.GetValue("EDID") is byte[] edid)
                        {
                            RandomizeBytes(edid, 8, Math.Min(16, edid.Length));
                            paramKey.SetValue("EDID", edid, RegistryValueKind.Binary);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static void SpoofNvidia(Action<string>? log)
    {
        log?.Invoke("Spoofing NVIDIA identifiers...");

        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\NVIDIA Corporation\Global", "ClientUUID");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\NVIDIA Corporation\Global", "PersistenceIdentifier");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\NVIDIA Corporation\Global\CoProcManager", "ChipsetMatchID");
    }

    private static void SpoofNetworkIds(Action<string>? log, string computerName)
    {
        log?.Invoke("Spoofing network identifiers...");

        SetValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Hostname", computerName);
        SetValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "NV Hostname", computerName);
        SetValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Domain", RandomHex(8));
        SetRandomBytes(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "Dhcpv6DUID");

        // Clean network adapter timestamps
        try
        {
            using var adapterKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
            if (adapterKey != null)
            {
                foreach (var sub in adapterKey.GetSubKeyNames())
                {
                    if (sub.Equals("Configuration", StringComparison.OrdinalIgnoreCase) ||
                        sub.Equals("Properties", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        using var subKey = adapterKey.OpenSubKey(sub, writable: true);
                        subKey?.DeleteValue("NetworkAddress", throwOnMissingValue: false);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static void SpoofWindowsUpdate(Action<string>? log)
    {
        log?.Invoke("Spoofing Windows Update identifiers...");

        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "AccountDomainSid");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "PingID");
        SetRandomGuid(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "SusClientId");
        SetRandomBytes(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate", "SusClientIdValidation");
    }

    private static void SpoofWindowsNT(Action<string>? log)
    {
        log?.Invoke("Spoofing Windows NT identifiers...");

        var ntPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        SetRandomGuid(Registry.LocalMachine, ntPath, "BuildGUID");
        SetRandomString(Registry.LocalMachine, ntPath, "ProductId");
        SetRandomString(Registry.LocalMachine, ntPath, "BuildLab");
        SetRandomString(Registry.LocalMachine, ntPath, "BuildLabEx");
        SetRandomString(Registry.LocalMachine, ntPath, "BuildBranch");
        SetRandomString(Registry.LocalMachine, ntPath, "RegisteredOwner");
        SetRandomString(Registry.LocalMachine, ntPath, "RegisteredOrganization");

        SetRandomBytes(Registry.LocalMachine, ntPath, "DigitalProductId");
        SetRandomBytes(Registry.LocalMachine, ntPath, "DigitalProductId4");
        SetRandomBytes(Registry.LocalMachine, ntPath, "IE Installed Date");

        // Tracing
        SetRandomGuid(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Tracing\Microsoft\Profile\Profile", "Guid");

        // Notifications
        SetRandomBytes(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Notifications\Data", "418A073AA3BC3475");
    }

    private static void CleanMiscKeys(Action<string>? log)
    {
        log?.Invoke("Cleaning miscellaneous tracking keys...");

        DeleteKey(Registry.LocalMachine, @"SYSTEM\MountedDevices");
        DeleteKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Dfrg\Statistics");
        DeleteKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\SettingsRequests");

        DeleteKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume");
        DeleteKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\CPC\Volume");
        DeleteKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2");
        DeleteValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket", "LastEnum");

        DeleteKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist");
        DeleteKey(Registry.CurrentUser, @"Software\Hex-Rays\IDA\History");
        DeleteKey(Registry.CurrentUser, @"Software\Hex-Rays\IDA\History64");

        DeleteValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "BackupProductKeyDefault");
        DeleteValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "actionlist");
        DeleteValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "ServiceSessionId");

        SetRandomBytes(Registry.CurrentUser, @"Software\Microsoft\Direct3D", "WHQLClass");

        // WMI trace GUIDs
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\ControlSet001\Services\kbdclass\Parameters", "WppRecorder_TraceGuid", braces: true);
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\ControlSet001\Services\mouhid\Parameters", "WppRecorder_TraceGuid", braces: true);

        // DevQuery UUID
        SetRandomGuid(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DevQuery\6", "UUID");
    }

    private static void CleanAntiCheatKeys(Action<string>? log)
    {
        log?.Invoke("Cleaning anti-cheat registry keys...");

        DeleteKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\EasyAntiCheat");
        DeleteKey(Registry.LocalMachine, @"SYSTEM\ControlSet001\Services\EasyAntiCheat");
        DeleteKey(Registry.LocalMachine, @"SYSTEM\ControlSet001\Services\BEService");
        DeleteKey(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation");

        DeleteKey(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
    }

    private static void CleanEpicKeys(Action<string>? log)
    {
        log?.Invoke("Cleaning Epic Games registry keys...");

        DeleteKey(Registry.CurrentUser, @"Software\Epic Games");
        DeleteKey(Registry.LocalMachine, @"SOFTWARE\Classes\com.epicgames.launcher");
        DeleteKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\EpicGames");
        DeleteKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Epic Games");
        DeleteKey(Registry.ClassesRoot, @"com.epicgames.launcher");
        DeleteKey(Registry.CurrentUser, @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\com.epicgames.launcher");
    }

    private static void CleanComputerName(Action<string>? log, string computerName)
    {
        log?.Invoke("Spoofing computer name...");
        SetValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", "ComputerName", computerName);
        SetValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", "ComputerName", computerName);
    }

    #region Helpers

    private static string RandomGuid(bool braces = false)
    {
        var g = Guid.NewGuid().ToString();
        return braces ? $"{{{g}}}" : g;
    }

    private static string RandomHex(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes((length + 1) / 2);
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()[..length];
    }

    private static string RandomAlphanumeric(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Rng.Next(chars.Length)]).ToArray());
    }

    private static void RandomizeBytes(byte[] buffer, int start, int end)
    {
        for (int i = start; i < end && i < buffer.Length; i++)
            buffer[i] = (byte)Rng.Next(256);
    }

    private static void SetRandomGuid(RegistryKey root, string subKeyPath, string valueName, bool braces = false)
    {
        try
        {
            using var key = root.CreateSubKey(subKeyPath, writable: true);
            key?.SetValue(valueName, RandomGuid(braces), RegistryValueKind.String);
        }
        catch { }
    }

    private static void SetRandomString(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.CreateSubKey(subKeyPath, writable: true);
            key?.SetValue(valueName, RandomAlphanumeric(24), RegistryValueKind.String);
        }
        catch { }
    }

    private static void SetRandomBytes(RegistryKey root, string subKeyPath, string valueName, int length = 16)
    {
        try
        {
            using var key = root.CreateSubKey(subKeyPath, writable: true);
            key?.SetValue(valueName, RandomNumberGenerator.GetBytes(length), RegistryValueKind.Binary);
        }
        catch { }
    }

    private static void SetValue(RegistryKey root, string subKeyPath, string valueName, string value)
    {
        try
        {
            using var key = root.CreateSubKey(subKeyPath, writable: true);
            key?.SetValue(valueName, value, RegistryValueKind.String);
        }
        catch { }
    }

    private static void DeleteKey(RegistryKey root, string subKeyPath)
    {
        try { root.DeleteSubKeyTree(subKeyPath, throwOnMissingSubKey: false); }
        catch { }
    }

    private static void DeleteValue(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
        }
        catch { }
    }

    #endregion
}
