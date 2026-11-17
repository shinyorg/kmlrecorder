namespace ShinyKmlRecorder.Services;

[Singleton]
[BindNotify]
public partial class ObdSettings
{
    [Bind(Default = 0)]
    public partial int TransportType { get; set; }

    /// <summary>
    /// BLE peripheral UUID, classic bluetooth address, or WiFi host:port of the last connected adapter
    /// </summary>
    [Bind]
    public partial string? DeviceId { get; set; }

    [Bind]
    public partial string? DeviceName { get; set; }

    /// <summary>
    /// The negotiated ELM protocol for the last adapter - pinning it saves the protocol search on reconnect
    /// </summary>
    [Bind]
    public partial string? Protocol { get; set; }

    /// <summary>
    /// Comma separated parameter keys - null means use the defaults
    /// </summary>
    [Bind]
    public partial string? SelectedParameterKeys { get; set; }

    [Bind(Default = 1)]
    public partial int PollIntervalSeconds { get; set; }


    public ObdTransportType Transport
    {
        get => (ObdTransportType)this.TransportType;
        set => this.TransportType = (int)value;
    }


    public IReadOnlyList<ObdParameter> SelectedParameters
    {
        get
        {
            if (this.SelectedParameterKeys == null)
                return ObdParameters.Defaults;

            var keys = this.SelectedParameterKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return ObdParameters.All.Where(x => keys.Contains(x.Key)).ToList();
        }
        set => this.SelectedParameterKeys = String.Join(",", value.Select(x => x.Key));
    }
}


public enum ObdTransportType
{
    BluetoothLE = 0,
    ClassicBluetooth = 1,
    Wifi = 2
}
