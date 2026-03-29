namespace HwidSpoofer.Services;

public record SpoofOptions(
    bool Diskdrive,
    bool Ram,
    bool Bios,
    bool Mac,
    bool Cpu,
    bool Volume,
    bool Gpu
);

public class SpoofOrchestrator
{
    public event Action<string>? LogMessage;
    public event Action? Completed;

    public async Task RunAsync(SpoofOptions options)
    {
        void Log(string msg) => LogMessage?.Invoke(msg);

#if DEBUG
        await RunSimulationAsync(options, Log);
#else
        await RunRealAsync(options, Log);
#endif

        Completed?.Invoke();
    }

    private async Task RunSimulationAsync(SpoofOptions options, Action<string> log)
    {
        log("[DEMO] Starting simulated spoof...");
        log("[DEMO] No actual changes will be made.");

        await Task.Delay(500);
        log("[1/6] [SIM] Scanning target processes...");
        await Task.Delay(800);
        log("       Found: steam.exe, EpicGamesLauncher.exe");
        log("       (Not killing - demo mode)");

        await Task.Delay(500);
        log("[2/6] [SIM] Checking Vanguard services...");
        await Task.Delay(600);
        log("       vgc: not found");
        log("       vgk: not found");

        if (options.Mac)
        {
            await Task.Delay(500);
            log("[3/6] [SIM] MAC address randomization...");
            await Task.Delay(700);
            log("       New MAC: 02:A1:3B:F7:22:8C (not applied)");
        }

        await Task.Delay(500);
        log("[4/6] [SIM] Registry identifiers scan...");
        await Task.Delay(1000);
        log("       MachineGuid: would be randomized");
        log("       ProductId: would be randomized");
        log("       HwProfileGuid: would be randomized");
        log("       SMBIOS: would be cleared");
        log("       NVIDIA IDs: would be randomized");

        await Task.Delay(500);
        log("[5/6] [SIM] Cache & tracking file scan...");
        await Task.Delay(800);
        log("       Temp files: 47 files found (not deleted)");
        log("       Prefetch: 23 files found (not deleted)");
        log("       Tracking logs: 5 files found (not deleted)");

        await Task.Delay(300);
        log("[6/6] [DEMO] Simulation complete!");
        log("");
        log("In Release mode, all operations above would execute for real.");
    }

    private async Task RunRealAsync(SpoofOptions options, Action<string> log)
    {
        log("Starting spoof process...");

        var tasks = new List<Task>();

        tasks.Add(Task.Run(() =>
        {
            log("[1/6] Killing target processes...");
            ProcessKillerService.KillAll(log);
        }));

        tasks.Add(Task.Run(() =>
        {
            log("[2/6] Removing Vanguard services...");
            ProcessKillerService.KillVanguard(log);
        }));

        if (options.Mac)
        {
            tasks.Add(Task.Run(() =>
            {
                log("[3/6] Randomizing MAC address...");
                MacChangerService.RandomizeMac(log);
            }));
        }

        tasks.Add(Task.Run(() =>
        {
            log("[4/6] Cleaning registry identifiers...");
            RegistryCleanerService.CleanAll(log, options);
        }));

        tasks.Add(Task.Run(() =>
        {
            log("[5/6] Cleaning caches and tracking files...");
            CacheCleanerService.CleanAll(log);
        }));

        await Task.WhenAll(tasks);

        log("[6/6] Spoof complete! Restart your PC for full effect.");
    }
}
