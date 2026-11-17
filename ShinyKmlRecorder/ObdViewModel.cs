using System.Collections.ObjectModel;
using Shiny.Obd;

namespace ShinyKmlRecorder;


[ShellMap<ObdPage>]
public partial class ObdViewModel(
    BaseServices services,
    IObdManager obdManager,
    ObdSettings settings
) : ObservableObject, IPageLifecycleAware
{
    CancellationTokenSource? scanCts;

    public IReadOnlyList<ObdTransportOption> Transports { get; } = obdManager.AvailableTransports.Select(x => new ObdTransportOption(x)).ToList();
    public ObservableCollection<ObdDeviceItem> Devices { get; } = new();
    public ObservableCollection<ObdLiveValue> LiveValues { get; } = new();

    ObdTransportType SelectedTransport => this.Transports.FirstOrDefault(x => x.IsSelected)?.Type ?? this.Transports[0].Type;
    [ObservableProperty] public partial bool IsScanning { get; private set; }
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial bool IsConnected { get; private set; }
    [ObservableProperty] public partial bool IsDisconnected { get; private set; } = true;
    [ObservableProperty] public partial bool HasVin { get; private set; }
    [ObservableProperty] public partial bool HasStatusMessage { get; private set; }
    [ObservableProperty] public partial bool HasLastDevice { get; private set; }
    [ObservableProperty] public partial string ScanText { get; private set; } = "Scan";
    [ObservableProperty] public partial bool IsRecording { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = "Disconnected";
    [ObservableProperty] public partial string? StatusMessage { get; private set; }
    [ObservableProperty] public partial string? Vin { get; private set; }
    [ObservableProperty] public partial string? LastDeviceName { get; private set; }
    [ObservableProperty] public partial string RecordText { get; private set; } = "Start Recording";
    [ObservableProperty] public partial string ParametersSummary { get; private set; } = String.Empty;

    [RelayCommand] Task NavToParameters() => services.Navigator.NavigateTo<ObdParametersViewModel>();
    [RelayCommand] Task NavToSessions() => services.Navigator.NavigateTo<ObdSessionsViewModel>();


    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.IsScanning)
        {
            this.StopScan();
            return;
        }

        this.Devices.Clear();
        this.scanCts = new CancellationTokenSource();
        this.IsScanning = true;
        try
        {
            await obdManager.Scan(
                this.SelectedTransport,
                device => MainThread.BeginInvokeOnMainThread(() => this.Devices.Add(new ObdDeviceItem(device))),
                this.scanCts.Token
            );
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await services.Dialogs.Alert("Scan Failed", ex.Message);
        }
        finally
        {
            this.IsScanning = false;
        }
    }


    [RelayCommand]
    async Task Connect(ObdDeviceItem item)
    {
        // an open scan slows the connect (BLE/classic) or holds the only socket (WiFi)
        this.StopScan();
        await this.Run("Connection Failed", () => obdManager.Connect(this.SelectedTransport, item.Device));
    }


    [RelayCommand]
    async Task ConnectLast()
    {
        this.StopScan();
        await this.Run("Connection Failed", async () =>
        {
            var connected = await obdManager.ConnectToLast(TimeSpan.FromSeconds(20));
            if (!connected)
                await services.Dialogs.Alert("Not Found", $"Could not find {settings.DeviceName}. Make sure the adapter is plugged in and the ignition is on.");
        });
    }


    [RelayCommand]
    Task Disconnect() => this.Run("Disconnect Failed", obdManager.Disconnect);


    [RelayCommand]
    Task ToggleRecording() => this.Run(
        "Recording Failed",
        () => obdManager.Status == ObdStatus.Recording
            ? obdManager.StopRecording()
            : obdManager.StartRecording()
    );


    public void OnAppearing()
    {
        var current = this.Transports.Any(x => x.Type == settings.Transport) ? settings.Transport : this.Transports[0].Type;
        foreach (var option in this.Transports)
            option.IsSelected = option.Type == current;

        var count = settings.SelectedParameters.Count;
        this.ParametersSummary = $"{count} parameter{(count == 1 ? "" : "s")} selected";

        obdManager.StateChanged += this.OnStateChanged;
        this.UpdateState();
    }


    public void OnDisappearing()
    {
        obdManager.StateChanged -= this.OnStateChanged;
        this.StopScan();
    }


    partial void OnIsScanningChanged(bool value) => this.ScanText = value ? "Stop Scan" : "Scan";


    [RelayCommand]
    void SelectTransport(ObdTransportOption option)
    {
        if (option.IsSelected)
            return;

        foreach (var o in this.Transports)
            o.IsSelected = o == option;

        if (this.IsScanning)
            this.StopScan();

        this.Devices.Clear();
    }


    void StopScan()
    {
        this.scanCts?.Cancel();
        this.scanCts = null;
    }


    async Task Run(string errorTitle, Func<Task> action)
    {
        if (this.IsBusy)
            return;

        try
        {
            this.IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            await services.Dialogs.Alert(errorTitle, ex.Message);
        }
        finally
        {
            this.IsBusy = false;
            this.UpdateState();
        }
    }


    void OnStateChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(this.UpdateState);


    void UpdateState()
    {
        var status = obdManager.Status;
        this.IsConnected = status is ObdStatus.Connected or ObdStatus.Recording;
        this.IsDisconnected = !this.IsConnected && status != ObdStatus.Connecting;
        this.IsRecording = status == ObdStatus.Recording;
        this.RecordText = this.IsRecording ? "Stop Recording" : "Start Recording";
        this.StatusMessage = obdManager.StatusMessage;
        this.Vin = obdManager.Vin;
        this.LastDeviceName = this.IsDisconnected ? settings.DeviceName : null;
        this.HasVin = this.Vin != null;
        this.HasStatusMessage = this.StatusMessage != null;
        this.HasLastDevice = this.LastDeviceName != null;
        this.StatusText = status switch
        {
            ObdStatus.Connecting => "Connecting...",
            ObdStatus.Connected => $"Connected to {obdManager.ConnectedDeviceName}",
            ObdStatus.Recording => $"Recording from {obdManager.ConnectedDeviceName}",
            _ => "Disconnected"
        };

        var latest = obdManager.LatestValues;
        var supported = obdManager.SupportedPids;
        var values = settings
            .SelectedParameters
            .Select(x => new ObdLiveValue(
                x.Name,
                latest.TryGetValue(x.Key, out var v)
                    ? $"{Math.Round(v, 2)} {x.Unit}"
                    : supported.Count > 0 && !supported.Contains(x.Pid) ? "Not supported" : "--"
            ))
            .ToList();

        this.LiveValues.Clear();
        if (this.IsConnected)
        {
            foreach (var value in values)
                this.LiveValues.Add(value);
        }
    }
}


public record ObdLiveValue(string Name, string Value);


public record ObdDeviceItem(ObdDiscoveredDevice Device)
{
    public string Name { get; } = ShinyKmlRecorder.Services.Impl.ObdManager.DisplayName(Device);
    public string Id => this.Device.Id;
}


public partial class ObdTransportOption(ObdTransportType type) : ObservableObject
{
    public ObdTransportType Type => type;
    public string Name => type switch
    {
        ObdTransportType.BluetoothLE => "Bluetooth LE",
        ObdTransportType.ClassicBluetooth => "Classic BT",
        _ => "WiFi"
    };

    [ObservableProperty] public partial bool IsSelected { get; set; }
}
