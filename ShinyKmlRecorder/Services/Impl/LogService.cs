using Shiny.Locations;
using Shiny.SqliteDocumentDb;

namespace ShinyKmlRecorder.Services.Impl;


public partial class LogService(
    TimeProvider timeProvider,
    IDocumentStore data, 
    IBattery battery
) : ObservableObject, ILogService
{
    [ObservableProperty] public partial Guid? WorkId { get; set; }
    [ObservableProperty] public partial DateTimeOffset? DateCheckedIn { get; set; }
    
    public async Task ClearLogs() => await data.Clear<LogRecord>();

    public async Task<IList<LogRecord>> GetLogs()
    {
        var results = await data.Query<LogRecord>()
            .OrderByDescending(x => x.Timestamp)
            .Paginate(0, 500)
            .ToList();
        
        return results.ToList();
    }

    public async Task<int> GetTripPointCount(Guid workId)
    {
        var count = await data.Query<LogRecord>()
            .Where(x => x.WorkId == workId && x.EventType == LogEventType.GpsPing)
            .Count();
        return (int)count;
    }

    public async Task<IList<LogRecord>> GetTripPoints(Guid workId)
    {
        var results = await data.Query<LogRecord>()
            .Where(x => x.WorkId == workId && x.EventType == LogEventType.GpsPing)
            .OrderBy(x => x.Timestamp)
            .ToList();
        return results.ToList();
    }

    public async Task<IList<TripSummary>> GetTrips()
    {
        var allLogs = await data.Query<LogRecord>()
            .OrderBy(x => x.Timestamp)
            .ToList();
            
        return allLogs
            .GroupBy(x => x.WorkId)
            .Select(g => new TripSummary
            {
                WorkId = g.Key,
                StartDate = g.Where(x => x.EventType == LogEventType.Checkin).Select(x => x.Timestamp).FirstOrDefault(g.Min(x => x.Timestamp)),
                EndDate = g.Where(x => x.EventType == LogEventType.Checkout).Select(x => (DateTimeOffset?)x.Timestamp).FirstOrDefault(),
                PointCount = g.Count(x => x.EventType == LogEventType.GpsPing)
            })
            .OrderByDescending(x => x.StartDate)
            .ToList();
    }

    
    public Task Checkin()
    {
        if (this.DateCheckedIn != null)
            return Task.CompletedTask;
        
        this.DateCheckedIn = timeProvider.GetUtcNow();
        this.WorkId = Guid.NewGuid();
        this.SyncWidgetState();
        
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
        this.SyncWidgetState();
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
        
        return data.Insert(record);
    }

    partial void SyncWidgetState();
}