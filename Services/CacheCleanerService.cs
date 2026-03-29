using System.Diagnostics;
using System.IO;

namespace HwidSpoofer.Services;

public static class CacheCleanerService
{
    public static void CleanAll(Action<string>? log = null)
    {
        CleanTempFiles(log);
        CleanCacheDirectories(log);
        CleanTrackingFiles(log);
        CleanPrefetch(log);
        CleanNtUserFiles(log);
        CleanDesktopIni(log);
        DeleteShadowCopies(log);
        DeleteUsnJournal(log);
        RestartWmi(log);
    }

    private static void CleanTempFiles(Action<string>? log)
    {
        log?.Invoke("Cleaning temp files...");
        var tempPath = Path.GetTempPath();
        DeleteDirectoryContents(tempPath);
    }

    private static void CleanCacheDirectories(Action<string>? log)
    {
        log?.Invoke("Cleaning cache directories...");
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] cachePaths =
        [
            Path.Combine(localAppData, "D3DSCache"),
            Path.Combine(localAppData, "NVIDIA Corporation", "GfeSDK"),
            Path.Combine(localAppData, "Microsoft", "Feeds"),
            Path.Combine(localAppData, "Microsoft", "Feeds Cache"),
            Path.Combine(localAppData, "Microsoft", "Windows", "INetCache"),
            Path.Combine(localAppData, "Microsoft", "Windows", "INetCookies"),
            Path.Combine(localAppData, "Microsoft", "Windows", "WebCache"),
        ];

        foreach (var path in cachePaths)
            ForceDelete(path);

        var xboxCache = Path.Combine(localAppData, "Microsoft", "XboxLive", "AuthStateCache.dat");
        TryDeleteFile(xboxCache);
    }

    private static void CleanTrackingFiles(Action<string>? log)
    {
        log?.Invoke("Cleaning tracking files...");

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var root = drive.RootDirectory.FullName;

            string[] filesToDelete =
            [
                Path.Combine(root, "Windows", "System32", "restore", "MachineGuid.txt"),
                Path.Combine(root, "Users", "Public", "Libraries", "collection.dat"),
                Path.Combine(root, "System Volume Information", "IndexerVolumeGuid"),
                Path.Combine(root, "System Volume Information", "WPSettings.dat"),
                Path.Combine(root, "System Volume Information", "tracking.log"),
                Path.Combine(root, "Windows", "INF", "setupapi.dev.log"),
                Path.Combine(root, "Windows", "INF", "setupapi.setup.log"),
                Path.Combine(root, "ProgramData", "ntuser.pol"),
                Path.Combine(root, "Users", "Default", "NTUSER.DAT"),
                Path.Combine(root, "Recovery", "ntuser.sys"),
                Path.Combine(root, "desktop.ini"),
            ];

            foreach (var file in filesToDelete)
                TryDeleteFile(file);

            string[] dirsToDelete =
            [
                Path.Combine(root, "ProgramData", "Microsoft", "Windows", "WER"),
                Path.Combine(root, "Users", "Public", "Shared Files"),
                Path.Combine(root, "Users", "Public", "Libraries"),
                Path.Combine(root, "MSOCache"),
            ];

            foreach (var dir in dirsToDelete)
                ForceDelete(dir);
        }
    }

    private static void CleanPrefetch(Action<string>? log)
    {
        log?.Invoke("Cleaning Prefetch...");
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var prefetch = Path.Combine(drive.RootDirectory.FullName, "Windows", "Prefetch");
            DeleteDirectoryContents(prefetch);
        }
    }

    private static void CleanNtUserFiles(Action<string>? log)
    {
        log?.Invoke("Cleaning NTUSER files...");
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var usersDir = Path.Combine(drive.RootDirectory.FullName, "Users");
            if (!Directory.Exists(usersDir)) continue;

            try
            {
                foreach (var userDir in Directory.GetDirectories(usersDir))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(userDir, "ntuser*", SearchOption.TopDirectoryOnly))
                            TryDeleteFile(file);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    private static void CleanDesktopIni(Action<string>? log)
    {
        log?.Invoke("Cleaning desktop.ini files...");
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var usersDir = Path.Combine(drive.RootDirectory.FullName, "Users");
            if (!Directory.Exists(usersDir)) continue;

            try
            {
                foreach (var file in Directory.GetFiles(usersDir, "desktop.ini", SearchOption.AllDirectories))
                    TryDeleteFile(file);
            }
            catch { }
        }
    }

    private static void DeleteShadowCopies(Action<string>? log)
    {
        log?.Invoke("Deleting shadow copies...");
        RunSilent("vssadmin", "delete shadows /All /Quiet");
    }

    private static void DeleteUsnJournal(Action<string>? log)
    {
        log?.Invoke("Deleting USN journals...");
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var letter = drive.RootDirectory.FullName[0];
            RunSilent("fsutil", $"usn deletejournal /d {letter}:");
        }
    }

    private static void RestartWmi(Action<string>? log)
    {
        log?.Invoke("Restarting WMI service...");
        foreach (var proc in Process.GetProcessesByName("WmiPrvSE"))
        {
            try { proc.Kill(); } catch { }
            finally { proc.Dispose(); }
        }
        RunSilent("net", "stop winmgmt /Y");
        RunSilent("net", "start winmgmt");
    }

    #region Helpers

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    private static void ForceDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    private static void DeleteDirectoryContents(string path)
    {
        if (!Directory.Exists(path)) return;
        try
        {
            foreach (var file in Directory.GetFiles(path))
                TryDeleteFile(file);
            foreach (var dir in Directory.GetDirectories(path))
                ForceDelete(dir);
        }
        catch { }
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
            proc.WaitForExit(10000);
        }
        catch { }
    }

    #endregion
}
