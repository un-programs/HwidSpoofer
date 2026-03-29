using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;

namespace HwidSpoofer.Services;

public record DriverStatusEntry(string Name, string DisplayName, DriverState State, string Detail);

public enum DriverState
{
    Running,
    Stopped,
    NotInstalled,
    Unknown
}

public static class DriverStatusService
{
    private static readonly (string ServiceName, string DisplayName)[] AntiCheatServices =
    [
        ("vgc", "Vanguard Client"),
        ("vgk", "Vanguard Kernel"),
        ("EasyAntiCheat", "EasyAntiCheat"),
        ("EasyAntiCheatSys", "EasyAntiCheat (Sys)"),
        ("BEService", "BattlEye Service"),
        ("BEDaisy", "BattlEye Kernel"),
        ("FaceIt", "FACEIT AC"),
    ];

    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    public static bool IsTestSigningEnabled()
    {
        try
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "bcdedit",
                Arguments = "/enum {current}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            return output.Contains("testsigning", StringComparison.OrdinalIgnoreCase)
                && output.Contains("Yes", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static bool IsSecureBootEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key?.GetValue("UEFISecureBootEnabled") is int val)
                return val == 1;
        }
        catch { }
        return false;
    }

    public static bool IsHypervisorPresent()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Virtualization");
            return key != null;
        }
        catch { return false; }
    }

    public static List<DriverStatusEntry> GetAntiCheatStatuses()
    {
        var results = new List<DriverStatusEntry>();

        foreach (var (serviceName, displayName) in AntiCheatServices)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                var state = sc.Status switch
                {
                    ServiceControllerStatus.Running => DriverState.Running,
                    ServiceControllerStatus.StartPending => DriverState.Running,
                    ServiceControllerStatus.ContinuePending => DriverState.Running,
                    _ => DriverState.Stopped
                };
                results.Add(new DriverStatusEntry(serviceName, displayName, state, sc.Status.ToString()));
            }
            catch (InvalidOperationException)
            {
                results.Add(new DriverStatusEntry(serviceName, displayName, DriverState.NotInstalled, "Not installed"));
            }
            catch
            {
                results.Add(new DriverStatusEntry(serviceName, displayName, DriverState.Unknown, "Access denied"));
            }
        }

        return results;
    }

    public static string GetWindowsDefenderStatus()
    {
        try
        {
            using var sc = new ServiceController("WinDefend");
            return sc.Status.ToString();
        }
        catch (InvalidOperationException) { return "Not installed"; }
        catch { return "Unknown"; }
    }

    public static string GetSpoofReadiness()
    {
        if (!IsRunningAsAdmin())
            return "Not Admin - run as Administrator";

        var antiCheats = GetAntiCheatStatuses();
        var running = antiCheats.Where(a => a.State == DriverState.Running).ToList();

        if (running.Count > 0)
            return $"Warning: {string.Join(", ", running.Select(r => r.DisplayName))} active";

        return "Ready to spoof";
    }
}
