using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;
using Shiny.DocumentDb;
using Shiny.Locations;
using Shiny.Obd;
using Shiny.Obd.Ble;
using Shiny.Obd.Commands;
using Shiny.Obd.Wifi;
#if ANDROID
using Shiny.Bluetooth;
using Shiny.Obd.Bluetooth;
#endif

namespace ShinyKmlRecorder.Services.Impl;


[Singleton]
public partial class ObdManager(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    ILogger<ObdManager> logger,
    IDocumentStore data,
    ObdSettings settings,
    ILogService logs,
    IGpsManager gpsManager,
    TimeProvider timeProvider
) : IObdManager
{
    // a BLE adapter keeps reporting connected long after it stops answering, so count dead polls instead
    const int MaxFailedPolls = 5;
    static readonly TimeSpan MaxGpsAge = TimeSpan.FromSeconds(30);

    readonly BleObdConfiguration bleConfig = new();
    readonly WifiObdConfiguration wifiConfig = new();
#if ANDROID
    readonly BluetoothObdConfiguration bluetoothConfig = new() { DeviceNameFilters = [] };
#endif

    ObdConnection? connection;
    ObdTransportType? connectedTransport;
    CancellationTokenSource? pollCts;
    Task? pollTask;
    ObdSession? session;
    GpsReading? lastGpsReading;

    public event EventHandler? StateChanged;

#if ANDROID
    public IReadOnlyList<ObdTransportType> AvailableTransports { get; } = [ObdTransportType.BluetoothLE, ObdTransportType.ClassicBluetooth, ObdTransportType.Wifi];
#else
    public IReadOnlyList<ObdTransportType> AvailableTransports { get; } = [ObdTransportType.BluetoothLE, ObdTransportType.Wifi];
#endif

    public ObdStatus Status { get; private set; } = ObdStatus.Disconnected;
    public string? StatusMessage { get; private set; }
    public string? ConnectedDeviceName { get; private set; }
    public string? Vin { get; private set; }
    public Guid? CurrentSessionId => this.session?.Id;
    public IReadOnlySet<byte> SupportedPids { get; private set; } = new HashSet<byte>();
    public IReadOnlyDictionary<string, double> LatestValues { get; private set; } = new Dictionary<string, double>();


    public async Task Scan(ObdTransportType transport, Action<ObdDiscoveredDevice> onDeviceFound, CancellationToken ct)
    {
        var scanner = await this.CreateScanner(transport, ct);
        var bindWifi = transport == ObdTransportType.Wifi && this.connection == null;
        if (bindWifi)
            this.BindToWifiNetwork(true);

        try
        {
            await scanner.Scan(onDeviceFound, ct);
        }
        finally
        {
            if (bindWifi && this.connection == null)
                this.BindToWifiNetwork(false);
        }
    }


    public async Task Connect(ObdTransportType transport, ObdDiscoveredDevice device, CancellationToken ct = default)
    {
        await this.Disconnect();
        this.SetStatus(ObdStatus.Connecting, $"Connecting to {DisplayName(device)}...");

        var conn = new ObdConnection(this.CreateTransport(transport, device))
        {
            // pinning the last negotiated protocol skips the multi-second ELM search - a stale pin is verified & dropped
            Protocol = device.Id == settings.DeviceId ? settings.Protocol : null
        };

        try
        {
            if (transport == ObdTransportType.Wifi)
                this.BindToWifiNetwork(true);

            await conn.Connect(ct);
            this.SupportedPids = await this.ReadSupportedPids(conn, ct);

            // only ask after something has needed the bus - ATSP0 has not chosen anything at the end of Connect
            var protocol = await conn.RefreshNegotiatedProtocol(ct);
            this.Vin = await this.TryReadVin(conn, ct);

            settings.Transport = transport;
            settings.DeviceId = device.Id;
            settings.DeviceName = DisplayName(device);
            settings.Protocol = protocol;

            this.connection = conn;
            this.connectedTransport = transport;
            this.ConnectedDeviceName = DisplayName(device);
            logger.LogInformation("Connected to OBD adapter {Name} - Protocol: {Protocol}, Supported PIDs: {Count}", this.ConnectedDeviceName, protocol, this.SupportedPids.Count);
        }
        catch
        {
            await SafeDispose(conn);
            if (transport == ObdTransportType.Wifi)
                this.BindToWifiNetwork(false);

            this.SupportedPids = new HashSet<byte>();
            this.Vin = null;
            this.SetStatus(ObdStatus.Disconnected, "Connection failed");
            throw;
        }

        this.pollCts = new CancellationTokenSource();
        var token = this.pollCts.Token;
        this.pollTask = Task.Run(() => this.PollLoop(conn, token));
        this.SetStatus(ObdStatus.Connected, null);
    }


    public async Task<bool> ConnectToLast(TimeSpan timeout, CancellationToken ct = default)
    {
        var deviceId = settings.DeviceId;
        if (deviceId == null)
            return false;

        var transport = settings.Transport;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        ObdDiscoveredDevice? found = null;
        this.SetStatus(ObdStatus.Connecting, $"Searching for {settings.DeviceName ?? deviceId}...");
        try
        {
            // match on the id - BLE adapters frequently have no name until iOS has connected to them once
            await this.Scan(transport, device =>
            {
                if (device.Id == deviceId)
                {
                    found = device;
                    cts.Cancel();
                }
            }, cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
        }

        if (found == null)
        {
            this.SetStatus(ObdStatus.Disconnected, "Last adapter was not found");
            return false;
        }
        await this.Connect(transport, found, ct);
        return true;
    }


    public async Task Disconnect()
    {
        if (this.session != null)
            await this.StopRecording();

        if (this.pollCts != null)
        {
            this.pollCts.Cancel();
            try
            {
                if (this.pollTask != null)
                    await this.pollTask;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "OBD poll loop ended with error");
            }
            this.pollCts.Dispose();
            this.pollCts = null;
            this.pollTask = null;
        }

        if (this.connection != null)
        {
            await SafeDispose(this.connection);
            this.connection = null;

            if (this.connectedTransport == ObdTransportType.Wifi)
                this.BindToWifiNetwork(false);

            this.connectedTransport = null;
            this.ConnectedDeviceName = null;
            this.Vin = null;
            this.SupportedPids = new HashSet<byte>();
            this.LatestValues = new Dictionary<string, double>();
            this.SetStatus(ObdStatus.Disconnected, null);
        }
    }


    public async Task StartRecording()
    {
        if (this.connection == null)
            throw new InvalidOperationException("Connect to an OBD adapter first");

        if (this.session != null)
            return;

        var newSession = new ObdSession
        {
            Id = Guid.NewGuid(),
            WorkId = logs.WorkId,
            StartTime = timeProvider.GetUtcNow(),
            Transport = this.connectedTransport!.Value,
            DeviceName = this.ConnectedDeviceName,
            Vin = this.Vin,
            Protocol = settings.Protocol,
            ParameterKeys = this.GetPollableParameters().Select(x => x.Key).ToList()
        };
        await data.Insert(newSession);

        gpsManager.GpsReadingReceived += this.OnGpsReading;
        this.session = newSession;
        this.SetStatus(ObdStatus.Recording, null);
    }


    public async Task StopRecording()
    {
        var current = this.session;
        if (current == null)
            return;

        gpsManager.GpsReadingReceived -= this.OnGpsReading;
        this.session = null;
        this.lastGpsReading = null;

        current.EndTime = timeProvider.GetUtcNow();
        await data.Update(current);

        this.SetStatus(this.connection == null ? ObdStatus.Disconnected : ObdStatus.Connected, null);
    }


    public async Task<IList<ObdSessionSummary>> GetSessions()
    {
        var sessions = await data.Query<ObdSession>()
            .OrderByDescending(x => x.StartTime)
            .ToList();

        var list = new List<ObdSessionSummary>();
        foreach (var s in sessions)
        {
            var count = await data.Query<ObdRecord>()
                .Where(x => x.SessionId == s.Id)
                .Count();

            // a session left open by the app being killed mid-recording ends at its last sample
            if (s.EndTime == null && s.Id != this.CurrentSessionId)
                await this.CloseAbandoned(s);

            list.Add(new ObdSessionSummary { Session = s, RecordCount = (int)count });
        }
        return list;
    }


    async Task CloseAbandoned(ObdSession session)
    {
        var last = await data.Query<ObdRecord>()
            .Where(x => x.SessionId == session.Id)
            .OrderByDescending(x => x.Timestamp)
            .Paginate(0, 1)
            .ToList();

        session.EndTime = last.Count > 0 ? last[0].Timestamp : session.StartTime;
        await data.Update(session);
    }


    public async Task<IList<ObdRecord>> GetRecords(Guid sessionId)
    {
        var records = await data.Query<ObdRecord>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Timestamp)
            .ToList();

        return records.ToList();
    }


    public async Task DeleteSession(Guid sessionId)
    {
        var records = await this.GetRecords(sessionId);
        await data.BatchRemove<ObdRecord>(records.Select(x => (object)x.Id));
        await data.Remove<ObdSession>(sessionId);
    }


    async Task PollLoop(ObdConnection conn, CancellationToken ct)
    {
        var failedPolls = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var started = timeProvider.GetTimestamp();
                var parameters = this.GetPollableParameters();
                var values = await this.Poll(conn, parameters, ct);

                failedPolls = parameters.Count > 0 && values.Count == 0 ? failedPolls + 1 : 0;
                if (failedPolls >= MaxFailedPolls)
                {
                    failedPolls = 0;
                    await this.Reinitialize(conn, ct);
                }

                this.LatestValues = values;
                if (values.Count > 0)
                    await this.TryRecord(values);

                this.StateChanged?.Invoke(this, EventArgs.Empty);

                var delay = TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds)) - timeProvider.GetElapsedTime(started);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, timeProvider, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // disconnect
        }
    }


    async Task<Dictionary<string, double>> Poll(ObdConnection conn, IReadOnlyList<ObdParameter> parameters, CancellationToken ct)
    {
        var values = new Dictionary<string, double>();
        foreach (var parameter in parameters)
        {
            try
            {
                values[parameter.Key] = await parameter.Read(conn, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (ObdException ex)
            {
                // an unanswered PID is missing for this sample, not zero
                logger.LogDebug(ex, "Failed to read {Parameter}", parameter.Key);
            }
        }
        return values;
    }


    async Task Reinitialize(ObdConnection conn, CancellationToken ct)
    {
        logger.LogWarning("OBD adapter stopped answering - reinitializing");
        this.StatusMessage = "Adapter stopped answering - reinitializing...";
        this.StateChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            // re-runs the adapter profile, a transport-only reconnect would lose the ELM session state
            await conn.Connect(ct);
            this.StatusMessage = null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to reinitialize OBD adapter");
            this.StatusMessage = "Adapter is not responding - is the ignition on?";
        }
    }


    async Task TryRecord(Dictionary<string, double> values)
    {
        var current = this.session;
        if (current == null)
            return;

        var gps = this.lastGpsReading;
        var now = timeProvider.GetUtcNow();
        var hasGps = gps != null && now - gps.Timestamp <= MaxGpsAge;

        try
        {
            await data.Insert(new ObdRecord
            {
                Id = Guid.NewGuid(),
                SessionId = current.Id,
                Timestamp = now,
                Latitude = hasGps ? gps!.Position.Latitude : null,
                Longitude = hasGps ? gps!.Position.Longitude : null,
                Values = values
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store OBD record");
        }
    }


    async Task<HashSet<byte>> ReadSupportedPids(ObdConnection conn, CancellationToken ct)
    {
        // each block's last PID says whether the next block exists - asking for one that doesn't returns NO DATA
        var supported = new HashSet<byte>();
        foreach (var block in SupportedPidsCommand.BlockPids.Where(x => x == 0x00 || supported.Contains(x)))
        {
            try
            {
                foreach (var pid in await conn.Execute(new SupportedPidsCommand(block), ct))
                    supported.Add(pid);
            }
            catch (ObdException ex)
            {
                logger.LogDebug(ex, "Supported PID block {Block:X2} not answered", block);
            }
        }
        return supported;
    }


    async Task<string?> TryReadVin(ObdConnection conn, CancellationToken ct)
    {
        try
        {
            return await conn.Execute(StandardCommands.Vin, ct);
        }
        catch (ObdException ex)
        {
            // pre-2005ish vehicles commonly have no mode 09
            logger.LogDebug(ex, "VIN not available");
            return null;
        }
    }


    IReadOnlyList<ObdParameter> GetPollableParameters()
    {
        var supported = this.SupportedPids;
        return settings
            .SelectedParameters
            .Where(x => supported.Count == 0 || supported.Contains(x.Pid))
            .ToList();
    }


    async Task<IObdDeviceScanner> CreateScanner(ObdTransportType transport, CancellationToken ct)
    {
        switch (transport)
        {
            case ObdTransportType.BluetoothLE:
                var bleManager = services.GetRequiredService<IBleManager>();
                var access = await bleManager.RequestAccess().FirstAsync();
                if (access != AccessState.Available)
                    throw new InvalidOperationException($"Bluetooth is not available - {access}");

                return new BleObdDeviceScanner(bleManager, this.bleConfig, loggerFactory.CreateLogger<BleObdDeviceScanner>());

#if ANDROID
            case ObdTransportType.ClassicBluetooth:
                var btManager = services.GetRequiredService<IBluetoothManager>();
                var btAccess = await btManager.RequestAccess(ct);
                if (btAccess != AccessState.Available)
                    throw new InvalidOperationException($"Bluetooth is not available - {btAccess}");

                return new BluetoothObdDeviceScanner(btManager, this.bluetoothConfig, loggerFactory.CreateLogger<BluetoothObdDeviceScanner>());
#endif

            case ObdTransportType.Wifi:
                // the adapter runs the access point we joined, so its gateway is the first place to look
                string? gateway = null;
                this.FindWifiGateway(ref gateway);
                this.wifiConfig.Host = gateway;
                return new WifiObdDeviceScanner(this.wifiConfig, loggerFactory.CreateLogger<WifiObdDeviceScanner>());

            default:
                throw new PlatformNotSupportedException($"{transport} is not supported on this platform");
        }
    }


    IObdTransport CreateTransport(ObdTransportType transport, ObdDiscoveredDevice device) => transport switch
    {
        ObdTransportType.BluetoothLE => new BleObdTransport(device, this.bleConfig),
#if ANDROID
        ObdTransportType.ClassicBluetooth => new BluetoothObdTransport(
            services.GetRequiredService<IBluetoothManager>(),
            device,
            this.bluetoothConfig,
            loggerFactory.CreateLogger<BluetoothObdTransport>()
        ),
#endif
        ObdTransportType.Wifi => new WifiObdTransport(device, this.wifiConfig, loggerFactory.CreateLogger<WifiObdTransport>()),
        _ => throw new PlatformNotSupportedException($"{transport} is not supported on this platform")
    };


    void OnGpsReading(object? sender, GpsReading reading) => this.lastGpsReading = reading;


    void SetStatus(ObdStatus status, string? message)
    {
        this.Status = status;
        this.StatusMessage = message;
        this.StateChanged?.Invoke(this, EventArgs.Empty);
    }


    async Task SafeDispose(ObdConnection conn)
    {
        try
        {
            await conn.Disconnect();
            await conn.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error disposing OBD connection");
        }
    }


    // TODO: drop the line split with Shiny.Obd 1.5.2 - earlier WiFi scans name an adapter with its ATI echo ("ATI\rOBDLink...")
    internal static string DisplayName(ObdDiscoveredDevice device)
    {
        var name = device.Name?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        return String.IsNullOrWhiteSpace(name) ? device.Id : name;
    }

    /// <summary>
    /// Android keeps the default route on cellular when the adapter's access point has no internet
    /// </summary>
    partial void BindToWifiNetwork(bool bind);

    /// <summary>
    /// The WiFi network's IPv4 gateway where the platform can tell us - .NET's own gateway lookup misses
    /// Android's per-network routing tables, leaving only the well-known adapter addresses to probe
    /// </summary>
    partial void FindWifiGateway(ref string? gateway);
}
