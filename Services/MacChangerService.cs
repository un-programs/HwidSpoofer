using System.Net.NetworkInformation;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace HwidSpoofer.Services;

public static class MacChangerService
{
    public static void RandomizeMac(Action<string>? log = null)
    {
        log?.Invoke("Randomizing MAC addresses...");

        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                         || n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Where(n => n.OperationalStatus == OperationalStatus.Up);

            foreach (var adapter in adapters)
            {
                var newMac = GenerateRandomMac();
                SetMacInRegistry(adapter.Id, newMac, log);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"MAC change error: {ex.Message}");
        }
    }

    private static string GenerateRandomMac()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        // Ensure unicast (clear multicast bit) and locally administered
        bytes[0] = (byte)((bytes[0] & 0xFE) | 0x02);
        return string.Concat(bytes.Select(b => b.ToString("X2")));
    }

    private static void SetMacInRegistry(string adapterId, string mac, Action<string>? log)
    {
        var basePath = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";

        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(basePath);
            if (baseKey == null) return;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                try
                {
                    using var subKey = baseKey.OpenSubKey(subKeyName, writable: true);
                    if (subKey == null) continue;

                    var instanceId = subKey.GetValue("NetCfgInstanceId")?.ToString();
                    if (string.Equals(instanceId, adapterId, StringComparison.OrdinalIgnoreCase))
                    {
                        subKey.SetValue("NetworkAddress", mac, RegistryValueKind.String);
                        log?.Invoke($"MAC set to {FormatMac(mac)} for adapter {adapterId[..8]}...");
                        DisableEnableAdapter(adapterId, log);
                        return;
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void DisableEnableAdapter(string adapterId, Action<string>? log)
    {
        // Use netsh to restart the adapter so the new MAC takes effect
        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => string.Equals(n.Id, adapterId, StringComparison.OrdinalIgnoreCase));

            foreach (var adapter in adapters)
            {
                var name = adapter.Name;
                log?.Invoke($"Restarting adapter: {name}");
                RunSilent("netsh", $"interface set interface \"{name}\" disable");
                Thread.Sleep(1000);
                RunSilent("netsh", $"interface set interface \"{name}\" enable");
            }
        }
        catch { }
    }

    private static string FormatMac(string mac)
    {
        if (mac.Length != 12) return mac;
        return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
    }

    private static void RunSilent(string fileName, string arguments)
    {
        try
        {
            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            proc.Start();
            proc.WaitForExit(10000);
        }
        catch { }
    }
}
