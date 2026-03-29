using System.Diagnostics;
using System.IO;

namespace HwidSpoofer.Services;

public static class ProcessKillerService
{
    private static readonly string[] TargetProcesses =
    [
        "steam",
        "steamwebhelper",
        "SteamService",
        "Origin",
        "OriginWebHelperService",
        "EpicGamesLauncher",
        "RiotClientServices",
        "RiotClientUx",
        "RiotClientUxRender",
        "RiotClientCrashHandler",
        "FortniteClient-Win64-Shipping",
        "OneDrive",
        "RustClient",
        "r5apex",
        "vgtray",
        "BEService",
        "EasyAntiCheat",
    ];

    public static int KillAll(Action<string>? log = null)
    {
        int killed = 0;
        foreach (var name in TargetProcesses)
        {
            try
            {
                var procs = Process.GetProcessesByName(name);
                foreach (var proc in procs)
                {
                    try
                    {
                        log?.Invoke($"Killing {proc.ProcessName} (PID {proc.Id})");
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(3000);
                        killed++;
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }
            }
            catch { }
        }
        return killed;
    }

    public static void KillVanguard(Action<string>? log = null)
    {
        log?.Invoke("Removing Vanguard services...");
        RunSilent("sc", "delete vgc");
        RunSilent("sc", "delete vgk");

        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Riot Vanguard"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RiotVanguard"),
        };

        foreach (var path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch { }
        }
    }

    private static void RunSilent(string fileName, string arguments)
    {
        try
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            proc.Start();
            proc.WaitForExit(5000);
        }
        catch { }
    }
}
