using Foundation;
using Shiny.Locations;
using UIKit;

namespace ShinyKmlRecorder;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    [Export("scene:openURLContexts:")]
    public void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
    {
        foreach (var context in urlContexts.ToArray<UIOpenUrlContext>())
        {
            if (context.Url.Scheme == "kmlrecorder" && context.Url.Host == "toggle")
            {
                _ = HandleToggleFromWidget();
                return;
            }
        }
    }

    static async Task HandleToggleFromWidget()
    {
        var services = IPlatformApplication.Current!.Services;
        var logService = services.GetRequiredService<ILogService>();
        var gpsManager = services.GetRequiredService<IGpsManager>();

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
    }
}