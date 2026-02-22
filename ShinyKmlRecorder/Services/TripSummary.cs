namespace ShinyKmlRecorder.Services;

public class TripSummary
{
    public Guid WorkId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int PointCount { get; set; }
}
