using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace HwidSpoofer.Services;

public record HardwareInfo(
    string MacAddress,
    string VolumeSerial,
    string ProcessorId,
    string BaseboardSerial,
    string ProductId,
    string CurrentBuild,
    string ComputerName,
    string MachineGuid
);

public static class HwidReaderService
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetVolumeInformation(
        string rootPathName,
        StringBuilder? volumeNameBuffer, int volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        StringBuilder? fileSystemNameBuffer, int fileSystemNameSize);

    public static HardwareInfo ReadAll()
    {
        return new HardwareInfo(
            MacAddress: GetMacAddress(),
            VolumeSerial: GetVolumeSerial(),
            ProcessorId: GetProcessorId(),
            BaseboardSerial: GetBaseboardSerial(),
            ProductId: GetProductId(),
            CurrentBuild: GetCurrentBuild(),
            ComputerName: Environment.MachineName,
            MachineGuid: GetMachineGuid()
        );
    }

    public static string GetMacAddress()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up
                                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            if (nic == null) return "N/A";
            var bytes = nic.GetPhysicalAddress().GetAddressBytes();
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }
        catch { return "N/A"; }
    }

    public static string GetVolumeSerial()
    {
        try
        {
            if (GetVolumeInformation("C:\\", null, 0, out uint serial, out _, out _, null, 0))
                return serial.ToString();
        }
        catch { }
        return "N/A";
    }

    public static string GetProcessorId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                var id = obj["ProcessorId"]?.ToString();
                if (!string.IsNullOrEmpty(id)) return id;
            }
        }
        catch { }
        return "N/A";
    }

    public static string GetBaseboardSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
            {
                var sn = obj["SerialNumber"]?.ToString();
                if (!string.IsNullOrEmpty(sn)) return sn;
            }
        }
        catch { }
        return "N/A";
    }

    public static string GetProductId()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("ProductId")?.ToString() ?? "N/A";
        }
        catch { return "N/A"; }
    }

    public static string GetCurrentBuild()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("CurrentBuild")?.ToString() ?? "N/A";
        }
        catch { return "N/A"; }
    }

    public static string GetMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid")?.ToString() ?? "N/A";
        }
        catch { return "N/A"; }
    }

    public static string ComputeHwidHash(HardwareInfo info)
    {
        var raw = $"{info.ProductId}{info.CurrentBuild}{info.VolumeSerial}{info.MacAddress}{info.ProcessorId}{info.BaseboardSerial}";
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
