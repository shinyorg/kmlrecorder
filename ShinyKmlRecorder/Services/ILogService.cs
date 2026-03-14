using Shiny.Locations;

namespace ShinyKmlRecorder.Services;

public interface ILogService
{
    Guid? WorkId { get; }
    DateTimeOffset? DateCheckedIn { get; }
    
    Task Checkin();
    Task Checkout();
    Task AddLog(GpsReading reading);
    Task ClearLogs();
    Task<IList<LogRecord>> GetLogs();
    Task<int> GetTripPointCount(Guid workId);
    Task<IList<LogRecord>> GetTripPoints(Guid workId);
    Task<IList<TripSummary>> GetTrips();
}

public static class LogServiceExtensions
{
    extension(ILogService logs)
    {
        public Task<int> GetCurrentTripPointCount() => logs.GetTripPointCount(logs.WorkId ?? Guid.Empty);
        public Task<IList<LogRecord>> GetCurrentTripPoints() => logs.GetTripPoints(logs.WorkId ?? Guid.Empty);
    }
}

public class LogRecord
{
    public Guid Id { get; set; }
    
    public Guid WorkId { get; set; } // this groups up the checkins
    public LogEventType EventType { get; set; }
  
    public double PositionAccuracy { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; } // Speed in m/s
    public BatteryState BatteryStatus { get; set; }
    public double BatteryLevel { get; set; }
    public bool IsEnergySaverOn { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? GpsTimestamp { get; set; }
}

public enum LogEventType
{
    Checkin = 1,
    Checkout = 2,
    GpsPing = 4
}