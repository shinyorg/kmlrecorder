using System.ComponentModel;
using Shiny.Locations;

namespace ShinyKmlRecorder.Delegates;


public partial class MyGpsDelegate(ILogger<MyGpsDelegate> logger, ILogService logService, GpsSettings settings) : GpsDelegate(logger), IShinyStartupTask
{
    public void Start()
    {
        this.ApplySettings();
        settings.PropertyChanged += this.OnSettingsChanged;
    }

    protected override Task OnGpsReading(GpsReading reading) => logService.AddLog(reading);

    void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => this.ApplySettings();

    void ApplySettings()
    {
        this.MinimumDistance = settings.MinimumDistanceMeters > 0
            ? Distance.FromMeters(settings.MinimumDistanceMeters)
            : null;

        this.MinimumTime = settings.MinimumTimeSeconds > 0
            ? TimeSpan.FromSeconds(settings.MinimumTimeSeconds)
            : null;

        this.MaximumDistance = settings.MaximumDistanceMeters > 0
            ? Distance.FromMeters(settings.MaximumDistanceMeters)
            : null;

        this.MaximumTime = settings.MaximumTimeSeconds > 0
            ? TimeSpan.FromSeconds(settings.MaximumTimeSeconds)
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
