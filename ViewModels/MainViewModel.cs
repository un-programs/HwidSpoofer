using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HwidSpoofer.Services;

namespace HwidSpoofer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SpoofOrchestrator _orchestrator = new();

    public static bool IsDebugMode =>
#if DEBUG
        true;
#else
        false;
#endif

    public string DebugTag => IsDebugMode ? "(DEMO)" : "";
    public string FooterText => IsDebugMode
        ? "DEBUG MODE - No changes will be applied"
        : "HWID Spoofer";

    public MainViewModel()
    {
        SpoofCommand = new RelayCommand(_ => RunSpoof(), _ => !IsSpoofing);
        RefreshCommand = new RelayCommand(_ => LoadHardwareInfo());
        RefreshDriverCommand = new RelayCommand(_ => LoadDriverStatus());

        _orchestrator.LogMessage += msg =>
            Application.Current.Dispatcher.Invoke(() => LogEntries.Add(msg));

        _orchestrator.Completed += () =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSpoofing = false;
                StatusText = "Spoof complete! Restart your PC.";
                var message = IsDebugMode
                    ? "DEMO mode: No actual changes were made.\nIn Release mode, real spoofing will occur."
                    : "Spoof completed successfully!\nPlease restart your computer for changes to take full effect.";
                MessageBox.Show(message, "HWID Spoofer", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        LogEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsLogEmpty));

        LoadHardwareInfo();
        LoadDriverStatus();
    }

    #region Tabs

    private bool _isTabSpoofer = true;
    public bool IsTabSpoofer
    {
        get => _isTabSpoofer;
        set => SetField(ref _isTabSpoofer, value);
    }

    private bool _isTabSerials;
    public bool IsTabSerials
    {
        get => _isTabSerials;
        set => SetField(ref _isTabSerials, value);
    }

    private bool _isTabInstructions;
    public bool IsTabInstructions
    {
        get => _isTabInstructions;
        set => SetField(ref _isTabInstructions, value);
    }

    private bool _isTabDriver;
    public bool IsTabDriver
    {
        get => _isTabDriver;
        set => SetField(ref _isTabDriver, value);
    }

    #endregion

    #region Hardware Info Properties

    private string _macAddress = "Loading...";
    public string MacAddress
    {
        get => _macAddress;
        set => SetField(ref _macAddress, value);
    }

    private string _volumeSerial = "Loading...";
    public string VolumeSerial
    {
        get => _volumeSerial;
        set => SetField(ref _volumeSerial, value);
    }

    private string _processorId = "Loading...";
    public string ProcessorId
    {
        get => _processorId;
        set => SetField(ref _processorId, value);
    }

    private string _baseboardSerial = "Loading...";
    public string BaseboardSerial
    {
        get => _baseboardSerial;
        set => SetField(ref _baseboardSerial, value);
    }

    private string _productId = "Loading...";
    public string ProductId
    {
        get => _productId;
        set => SetField(ref _productId, value);
    }

    private string _currentBuild = "Loading...";
    public string CurrentBuild
    {
        get => _currentBuild;
        set => SetField(ref _currentBuild, value);
    }

    private string _computerName = "Loading...";
    public string ComputerName
    {
        get => _computerName;
        set => SetField(ref _computerName, value);
    }

    private string _machineGuid = "Loading...";
    public string MachineGuid
    {
        get => _machineGuid;
        set => SetField(ref _machineGuid, value);
    }

    #endregion

    #region Spoof Options

    private bool _spoofDiskdrive = true;
    public bool SpoofDiskdrive { get => _spoofDiskdrive; set => SetField(ref _spoofDiskdrive, value); }

    private bool _spoofRam = true;
    public bool SpoofRam { get => _spoofRam; set => SetField(ref _spoofRam, value); }

    private bool _spoofBios = true;
    public bool SpoofBios { get => _spoofBios; set => SetField(ref _spoofBios, value); }

    private bool _spoofMac = true;
    public bool SpoofMac { get => _spoofMac; set => SetField(ref _spoofMac, value); }

    private bool _spoofCpu = true;
    public bool SpoofCpu { get => _spoofCpu; set => SetField(ref _spoofCpu, value); }

    private bool _spoofVolume = true;
    public bool SpoofVolume { get => _spoofVolume; set => SetField(ref _spoofVolume, value); }

    private bool _spoofGpu = true;
    public bool SpoofGpu { get => _spoofGpu; set => SetField(ref _spoofGpu, value); }

    #endregion

    #region Status

    private bool _isSpoofing;
    public bool IsSpoofing
    {
        get => _isSpoofing;
        set
        {
            SetField(ref _isSpoofing, value);
            OnPropertyChanged(nameof(IsNotSpoofing));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsNotSpoofing => !IsSpoofing;
    public bool IsLogEmpty => LogEntries.Count == 0;

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public ObservableCollection<string> LogEntries { get; } = [];

    #endregion

    #region Driver Status

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetField(ref _isAdmin, value);
    }

    private bool _isTestSigning;
    public bool IsTestSigning
    {
        get => _isTestSigning;
        set => SetField(ref _isTestSigning, value);
    }

    private bool _isSecureBoot;
    public bool IsSecureBoot
    {
        get => _isSecureBoot;
        set => SetField(ref _isSecureBoot, value);
    }

    private string _defenderStatus = "Checking...";
    public string DefenderStatus
    {
        get => _defenderStatus;
        set => SetField(ref _defenderStatus, value);
    }

    private string _spoofReadiness = "Checking...";
    public string SpoofReadiness
    {
        get => _spoofReadiness;
        set => SetField(ref _spoofReadiness, value);
    }

    private ObservableCollection<DriverStatusEntry> _antiCheatEntries = [];
    public ObservableCollection<DriverStatusEntry> AntiCheatEntries
    {
        get => _antiCheatEntries;
        set => SetField(ref _antiCheatEntries, value);
    }

    #endregion

    #region Commands

    public ICommand SpoofCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RefreshDriverCommand { get; }

    #endregion

    private void LoadHardwareInfo()
    {
        Task.Run(() =>
        {
            var info = HwidReaderService.ReadAll();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MacAddress = info.MacAddress;
                VolumeSerial = info.VolumeSerial;
                ProcessorId = info.ProcessorId;
                BaseboardSerial = info.BaseboardSerial;
                ProductId = info.ProductId;
                CurrentBuild = info.CurrentBuild;
                ComputerName = info.ComputerName;
                MachineGuid = info.MachineGuid;
            });
        });
    }

    private void LoadDriverStatus()
    {
        Task.Run(() =>
        {
            var isAdmin = DriverStatusService.IsRunningAsAdmin();
            var testSigning = DriverStatusService.IsTestSigningEnabled();
            var secureBoot = DriverStatusService.IsSecureBootEnabled();
            var defender = DriverStatusService.GetWindowsDefenderStatus();
            var readiness = DriverStatusService.GetSpoofReadiness();
            var antiCheats = DriverStatusService.GetAntiCheatStatuses();

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsAdmin = isAdmin;
                IsTestSigning = testSigning;
                IsSecureBoot = secureBoot;
                DefenderStatus = defender;
                SpoofReadiness = readiness;
                AntiCheatEntries = new ObservableCollection<DriverStatusEntry>(antiCheats);
            });
        });
    }

    private async void RunSpoof()
    {
        IsSpoofing = true;
        StatusText = "Spoofing in progress...";
        LogEntries.Clear();

        var options = new SpoofOptions(
            Diskdrive: SpoofDiskdrive,
            Ram: SpoofRam,
            Bios: SpoofBios,
            Mac: SpoofMac,
            Cpu: SpoofCpu,
            Volume: SpoofVolume,
            Gpu: SpoofGpu
        );

        try
        {
            await _orchestrator.RunAsync(options);
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Add($"Error: {ex.Message}");
                StatusText = "Spoof failed!";
                IsSpoofing = false;
            });
        }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    #endregion
}
