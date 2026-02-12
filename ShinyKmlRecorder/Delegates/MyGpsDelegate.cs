using Shiny.Locations;

namespace ShinyKmlRecorder.Delegates;


public partial class MyGpsDelegate(ILogService logService) : IGpsDelegate
{

    public Task OnReading(GpsReading reading) => logService.AddLog(reading);
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