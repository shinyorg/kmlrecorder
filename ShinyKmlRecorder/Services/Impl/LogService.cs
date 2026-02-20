using Shiny.Locations;

namespace ShinyKmlRecorder.Services.Impl;


public partial class LogService(
    TimeProvider timeProvider,
    MySqliteConnection data, 
    IBattery battery
) : ObservableObject, ILogService
{
    [ObservableProperty] public partial Guid? WorkId { get; set; }
    [ObservableProperty] public partial DateTimeOffset? DateCheckedIn { get; set; }
    
    public Task ClearLogs() => data.DeleteAllLogs();
    public async Task<IList<LogRecord>> GetLogs() => await data
        .Logs
        .Take(500)
        .OrderByDescending(x => x.Timestamp)
        .ToListAsync();

    public async Task<int> GetTripPointCount(Guid workId) => await data
        .Logs
        .Where(x => x.WorkId == workId && x.EventType == LogEventType.GpsPing)
        .CountAsync();

    public async Task<IList<LogRecord>> GetTripPoints(Guid workId) => await data
        .Logs
        .Where(x => x.WorkId == workId && x.EventType == LogEventType.GpsPing)
        .OrderBy(x => x.Timestamp)
        .ToListAsync();

    
    public Task Checkin()
    {
        if (this.DateCheckedIn != null)
            return Task.CompletedTask;
        
        this.DateCheckedIn = timeProvider.GetUtcNow();
        this.WorkId = Guid.NewGuid();
        
        return this.Complete(new LogRecord
        {
            EventType = LogEventType.Checkin
        });
    }


    public async Task Checkout()
    {
        if (this.DateCheckedIn == null)
            return;
        
        await this.Complete(new LogRecord
        {
            EventType = LogEventType.Checkout
        });
        this.DateCheckedIn = null;
        this.WorkId = null;
    }
    
    
    public Task AddLog(GpsReading reading)
    {
        var record = new LogRecord
        {
            EventType = LogEventType.GpsPing,
            Latitude = reading.Position.Latitude,
            Longitude = reading.Position.Longitude,
            PositionAccuracy = reading.PositionAccuracy,
            Speed = reading.Speed,
            GpsTimestamp = reading.Timestamp
        };
        
        return this.Complete(record);
    }


    Task Complete(LogRecord record)
    {
        record.Id = Guid.NewGuid();
        record.WorkId = this.WorkId!.Value;
        record.IsEnergySaverOn = battery.EnergySaverStatus == EnergySaverStatus.On;
        record.Timestamp = timeProvider.GetUtcNow();
        record.BatteryLevel = battery.ChargeLevel;
        record.BatteryStatus = battery.State;
        
        return data.InsertAsync(record);
    }
}