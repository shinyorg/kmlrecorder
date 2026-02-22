using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Shiny.Locations;

namespace ShinyKmlRecorder;

[Activity(
    LaunchMode = LaunchMode.SingleTop,
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density
)]
[IntentFilter(
    [
        Platform.Intent.ActionAppAction,
        global::Android.Content.Intent.ActionView
    ],
    Categories =
    [
        global::Android.Content.Intent.CategoryDefault,
        global::Android.Content.Intent.CategoryBrowsable
    ]
)]
public class MainActivity : MauiAppCompatActivity
{
    const string ActionToggle = "org.shiny.kmlrecorder.TOGGLE";

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent?.Action == ActionToggle)
            _ = HandleToggleFromWidget();
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (Intent?.Action == ActionToggle)
        {
            _ = HandleToggleFromWidget();
            Intent.SetAction(null);
        }
    }

    static async Task HandleToggleFromWidget()
    {
        if (IPlatformApplication.Current?.Services == null)
            return;

        var logService = IPlatformApplication.Current.Services.GetRequiredService<ILogService>();
        var gpsManager = IPlatformApplication.Current.Services.GetRequiredService<IGpsManager>();

        if (logService.DateCheckedIn != null)
        {
            if (gpsManager.IsListening())
                await gpsManager.StopListener();

            await logService.Checkout();
        }
        else
        {
            await logService.Checkin();

            if (!gpsManager.IsListening())
                await gpsManager.StartListener(GpsRequest.Realtime(true));
        }

        var context = global::Android.App.Application.Context;
        KmlRecorderWidget.UpdateAllWidgets(context);
    }
}