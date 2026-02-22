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