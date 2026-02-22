using CarPlay;
using Foundation;
using ObjCRuntime;
using Shiny.Locations;
using UIKit;

namespace ShinyKmlRecorder;

[Register("CarPlaySceneDelegate")]
public class CarPlaySceneDelegate : CPTemplateApplicationSceneDelegate
{
    CPInterfaceController? interfaceController;
    UIWindow? carWindow;
    CarPlayMapViewController? mapViewController;
    NSTimer? refreshTimer;
    
    // Fallback for driving-task entitlement (no map window)
    public override void DidConnect(CPTemplateApplicationScene templateApplicationScene, CPInterfaceController interfaceController)
    {
        this.interfaceController = interfaceController;
        _ = this.UpdateTemplate();
        this.StartRefreshTimer();
    }

    public override void DidConnect(CPTemplateApplicationScene templateApplicationScene, CPInterfaceController interfaceController, CPWindow window)
    {
        this.interfaceController = interfaceController;
        this.carWindow = window;
        
        this.mapViewController = new CarPlayMapViewController();
        this.carWindow.RootViewController = mapViewController;
        
        _ = this.UpdateTemplate();
        this.StartRefreshTimer();
    }

    [Export("templateApplicationScene:didDisconnectInterfaceController:fromWindow:")]
    public void DidDisconnect(CPTemplateApplicationScene scene, CPInterfaceController interfaceController, UIWindow window)
    {
        this.Cleanup();
    }

    public override void DidDisconnect(CPTemplateApplicationScene templateApplicationScene, CPInterfaceController interfaceController)
    {
        this.Cleanup();
    }

    void Cleanup()
    {
        this.StopRefreshTimer();
        this.interfaceController = null;
        this.carWindow = null;
        this.mapViewController = null;
    }

    async Task UpdateTemplate()
    {
        if (interfaceController == null)
            return;

        var logService = Resolve<ILogService>();
        var isRecording = logService.DateCheckedIn != null;
        var count = 0;

        if (isRecording && logService.WorkId != null)
            count = await logService.GetCurrentTripPointCount();

        this.InvokeOnMainThread(() =>
        {
            if (mapViewController != null)
                this.SetMapTemplate(isRecording, count);
            else
                this.SetGridTemplate(isRecording, count);
        });
    }

    void SetMapTemplate(bool isRecording, int count)
    {
        if (interfaceController == null)
            return;

        var toggleImage = UIImage.GetSystemImage(isRecording ? "stop.fill" : "record.circle")!;
        var toggleButton = new CPMapButton(async _ => await ToggleRecording())
        {
            Image = toggleImage
        };

        // workaround: CPMapTemplate parameterless ctor missing in .NET 10 binding
        var mapTemplate = (CPMapTemplate)Runtime.GetNSObject(
            objc_msgSend(
                Class.GetHandle("CPMapTemplate"),
                Selector.GetHandle("new")
            )
        )!;
        mapTemplate.MapButtons = [toggleButton];

        if (isRecording)
        {
            var countButton = new CPBarButton($"{count} pts", _ => { });
            mapTemplate.TrailingNavigationBarButtons = [countButton];
        }

        interfaceController.SetRootTemplate(mapTemplate, true, null);
    }

    void SetGridTemplate(bool isRecording, int count)
    {
        if (interfaceController == null)
            return;

        var title = isRecording ? "Stop" : "Start";
        var imageName = isRecording ? "stop.fill" : "record.circle";
        var image = UIImage.GetSystemImage(imageName)!;

        var titles = isRecording
            ? new[] { title, $"{count} pts" }
            : new[] { title };

        var toggleButton = new CPGridButton(
            titles,
            image,
            async _ => await ToggleRecording()
        );

        var template = new CPGridTemplate("KML Recorder", [toggleButton]);
        interfaceController.SetRootTemplate(template, true, null);
    }

    async Task RefreshMapAndCount()
    {
        var logService = Resolve<ILogService>();
        if (logService.WorkId == null)
            return;

        var points = await logService.GetCurrentTripPoints();

        InvokeOnMainThread(() =>
        {
            mapViewController?.UpdateRoute(points);

            if (interfaceController?.RootTemplate is CPMapTemplate mapTemplate)
            {
                var countButton = new CPBarButton($"{points.Count} pts", _ => { });
                mapTemplate.TrailingNavigationBarButtons = [countButton];
            }
        });
    }

    void StartRefreshTimer()
    {
        refreshTimer = NSTimer.CreateRepeatingScheduledTimer(5.0, async _ =>
        {
            var logService = Resolve<ILogService>();
            if (logService.DateCheckedIn != null)
            {
                if (mapViewController != null)
                    await RefreshMapAndCount();
                else
                    await UpdateTemplate();
            }
        });
    }

    void StopRefreshTimer()
    {
        refreshTimer?.Invalidate();
        refreshTimer = null;
    }

    async Task ToggleRecording()
    {
        try
        {
            var logService = Resolve<ILogService>();
            var gpsManager = Resolve<IGpsManager>();
            
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

            await UpdateTemplate();
        }
        catch (Exception ex)
        {
            // TODO: log & alert
        }
    }

    T Resolve<T>() where T : notnull
        => IPlatformApplication.Current!.Services.GetRequiredService<T>();

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern NativeHandle objc_msgSend(NativeHandle receiver, NativeHandle selector);
}
