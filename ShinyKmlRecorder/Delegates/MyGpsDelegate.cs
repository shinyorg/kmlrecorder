using System.ComponentModel;
using Shiny.Locations;

namespace ShinyKmlRecorder.Delegates;


public partial class MyGpsDelegate : GpsDelegate
{
    readonly ILogService logService;
    readonly GpsSettings settings;

    public MyGpsDelegate(ILogger<MyGpsDelegate> logger, ILogService logService, GpsSettings settings)
        : base(logger)
    {
        this.logService = logService;
        this.settings = settings;

        this.ApplySettings();
        settings.PropertyChanged += this.OnSettingsChanged;
    }

    protected override Task OnGpsReading(GpsReading reading) => this.logService.AddLog(reading);

    void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => this.ApplySettings();

    void ApplySettings()
    {
        this.MinimumDistance = this.settings.MinimumDistanceMeters > 0
            ? Distance.FromMeters(this.settings.MinimumDistanceMeters)
            : null;

        this.MinimumTime = this.settings.MinimumTimeSeconds > 0
            ? TimeSpan.FromSeconds(this.settings.MinimumTimeSeconds)
            : null;

        this.MaximumDistance = this.settings.MaximumDistanceMeters > 0
            ? Distance.FromMeters(this.settings.MaximumDistanceMeters)
            : null;

        this.MaximumTime = this.settings.MaximumTimeSeconds > 0
            ? TimeSpan.FromSeconds(this.settings.MaximumTimeSeconds)
            : null;
    }
}

#if ANDROID
public partial class MyGpsDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentText("KML Recorder is doing its thing...recording!");
    }
}
#endif
