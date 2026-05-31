namespace ShinyKmlRecorder.Services;

[Singleton]
[BindNotify]
public partial class GpsSettings
{
    [Bind(Default = 10d)]
    public partial double MinimumDistanceMeters { get; set; }

    [Bind(Default = 5)]
    public partial int MinimumTimeSeconds { get; set; }

    [Bind(Default = 0d)]
    public partial double MaximumDistanceMeters { get; set; }

    [Bind(Default = 0)]
    public partial int MaximumTimeSeconds { get; set; }
}
