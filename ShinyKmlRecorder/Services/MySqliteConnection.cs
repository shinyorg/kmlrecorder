using SQLite;

namespace ShinyKmlRecorder.Services;


[Singleton]
public class MySqliteConnection : SQLiteAsyncConnection
{
    MySqliteConnection(string databasePath) : base(databasePath)
    {
        var conn = this.GetConnection();
        conn.CreateTable<LogRecord>();
        conn.EnableWriteAheadLogging();
    }

#if PLATFORM
    public MySqliteConnection(
        IPlatform platform,
        ILogger<MySqliteConnection> logger
    ) : this(Path.Combine(platform.AppData.FullName, "app.db"))
    {
#if DEBUG
        var conn = this.GetConnection();
        conn.Trace = true;
        conn.Tracer = sql => logger.LogDebug("SQLite Query: " + sql);
#endif
    }
#endif

    public AsyncTableQuery<LogRecord> Logs => this.Table<LogRecord>();

    public async Task<int> DeleteAllLogs()
    {
        var count = await this.DeleteAllAsync<LogRecord>();
        return count;
    }
}
public class LogRecord
{
    [PrimaryKey]
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