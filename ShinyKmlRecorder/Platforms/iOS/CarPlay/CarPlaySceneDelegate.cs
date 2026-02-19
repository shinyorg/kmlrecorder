#if ADD_CAR_APPS
using CarPlay;
using Foundation;
using Shiny.Locations;
using UIKit;

namespace ShinyKmlRecorder;

[Register("CarPlaySceneDelegate")]
public class CarPlaySceneDelegate : CPTemplateApplicationSceneDelegate
{
    CPInterfaceController? interfaceController;
    CPGridButton? toggleButton;

    public override void DidConnect(CPTemplateApplicationScene templateApplicationScene, CPInterfaceController interfaceController)
    {
        this.interfaceController = interfaceController;
        this.UpdateTemplate();
    }

    public override void DidDisconnect(CPTemplateApplicationScene templateApplicationScene, CPInterfaceController interfaceController)
    {
        this.interfaceController = null;
    }

    void UpdateTemplate()
    {
        if (this.interfaceController == null)
            return;

        var logService = this.Resolve<ILogService>();
        var isRecording = logService.DateCheckedIn != null;

        var title = isRecording ? "Stop" : "Start";
        var imageName = isRecording ? "stop.fill" : "record.circle";
        var image = UIImage.GetSystemImage(imageName)!;

        this.toggleButton = new CPGridButton(
            new[] { title },
            image,
            async (button) => await this.ToggleRecording()
        );

        var template = new CPGridTemplate(
            "KML Recorder",
            new[] { this.toggleButton }
        );

        this.interfaceController.SetRootTemplate(template, true, null);
    }

    async Task ToggleRecording()
    {
        var logService = this.Resolve<ILogService>();
        var gpsManager = this.Resolve<IGpsManager>();

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

        this.UpdateTemplate();
    }

    T Resolve<T>() where T : notnull
        => IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
#endif
