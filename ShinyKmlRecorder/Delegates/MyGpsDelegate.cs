using System.Data;
using Shiny.Locations;
using ShinyKmlRecorder.Services;

namespace ShinyKmlRecorder.Delegates;


// this uses a Shiny lifecycle task and runs in the same scope as the GPS delegate
// if GPS isn't running, this guy will still be watching app state and the timer will keep going (as long as the app is alive)
public partial class MyGpsDelegate(
    ILogger<MyGpsDelegate> logger,
    ILogService logService,
    IGpsManager gpsManager
) : IGpsDelegate
{

    public Task OnReading(GpsReading reading) => logService.AddLog(reading);
}

#if ANDROID
public partial class MyGpsDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentText("ClearD Driver Behaviour is tracking you!  Break me if you can...");
    }
}
#endif