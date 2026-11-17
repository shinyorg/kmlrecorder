using Shiny.Obd;

namespace ShinyKmlRecorder.Services;

public interface IObdManager
{
    IReadOnlyList<ObdTransportType> AvailableTransports { get; }
    ObdStatus Status { get; }
    string? StatusMessage { get; }
    string? ConnectedDeviceName { get; }
    string? Vin { get; }
    Guid? CurrentSessionId { get; }

    /// <summary>
    /// The PIDs the connected vehicle reports it supports - empty when disconnected
    /// </summary>
    IReadOnlySet<byte> SupportedPids { get; }

    /// <summary>
    /// The most recent value per parameter key from the live poll
    /// </summary>
    IReadOnlyDictionary<string, double> LatestValues { get; }

    /// <summary>
    /// Fires on status changes and each poll - may be raised from a background thread
    /// </summary>
    event EventHandler? StateChanged;

    Task Scan(ObdTransportType transport, Action<ObdDiscoveredDevice> onDeviceFound, CancellationToken ct);
    Task Connect(ObdTransportType transport, ObdDiscoveredDevice device, CancellationToken ct = default);

    /// <summary>
    /// Scans the saved transport for the last connected adapter and connects to it
    /// </summary>
    Task<bool> ConnectToLast(TimeSpan timeout, CancellationToken ct = default);
    Task Disconnect();

    Task StartRecording();
    Task StopRecording();

    Task<IList<ObdSessionSummary>> GetSessions();
    Task<IList<ObdRecord>> GetRecords(Guid sessionId);
    Task DeleteSession(Guid sessionId);
}


public enum ObdStatus
{
    Disconnected,
    Connecting,
    Connected,
    Recording
}


public class ObdSession
{
    public Guid Id { get; set; }
    public Guid? WorkId { get; set; } // the GPS trip that was active when recording started
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ObdTransportType Transport { get; set; }
    public string? DeviceName { get; set; }
    public string? Vin { get; set; }
    public string? Protocol { get; set; }
    public List<string> ParameterKeys { get; set; } = new();
}


public class ObdRecord
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// Keyed by ObdParameter.Key - a parameter that could not be read is absent, never zero
    /// </summary>
    public Dictionary<string, double> Values { get; set; } = new();
}


public class ObdSessionSummary
{
    public required ObdSession Session { get; init; }
    public int RecordCount { get; init; }
}
