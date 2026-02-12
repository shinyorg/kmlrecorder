using Shiny.Locations;

namespace ShinyKmlRecorder.Services;

public interface ILogService
{
    DateTimeOffset? DateCheckedIn { get; }
    
    Task Checkin();
    Task Checkout();
    Task AddLog(GpsReading reading);
    Task ClearLogs();
    Task<IList<LogRecord>> GetLogs();
}