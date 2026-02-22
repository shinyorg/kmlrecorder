using Android.Content;
using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using AndroidX.Car.App.Validation;
using Shiny.Locations;
using Action = AndroidX.Car.App.Model.Action;

namespace ShinyKmlRecorder;

[Android.App.Service(
    Exported = true,
    Label = "KML Recorder"
)]
[Android.App.IntentFilter(
    [ "androidx.car.app.CarAppService" ],
    Categories = [ "androidx.car.app.category.IOT" ]
)]
public class KmlCarAppService : CarAppService
{
    public override HostValidator CreateHostValidator()
        => HostValidator.AllowAllHostsValidator;

    public override Session OnCreateSession()
        => new KmlCarSession();
}

public class KmlCarSession : Session
{
    public override Screen OnCreateScreen(Intent intent)
        => new KmlCarScreen(this.CarContext);
}

public class KmlCarScreen : Screen
{
    public KmlCarScreen(CarContext carContext) : base(carContext) { }

    public override ITemplate OnGetTemplate()
    {
        var logService = this.Resolve<ILogService>();
        var isRecording = logService.DateCheckedIn != null;

        var title = isRecording ? "Stop Recording" : "Start Recording";

        var action = new Action.Builder()
            .SetTitle(title)
            .SetOnClickListener(new ClickListener(async () =>
            {
                await this.ToggleRecording();
                this.Invalidate();
            }))
            .Build();

        var pane = new Pane.Builder()
            .AddAction(action)
            .SetLoading(false)
            .Build();

        return new PaneTemplate.Builder(pane)
            .SetTitle("KML Recorder")
            .SetHeaderAction(Action.AppIcon)
            .Build();
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
    }

    T Resolve<T>() where T : notnull
        => IPlatformApplication.Current!.Services.GetRequiredService<T>();
}

class ClickListener : Java.Lang.Object, IOnClickListener
{
    readonly Func<Task> onClick;
    public ClickListener(Func<Task> onClick) => this.onClick = onClick;
    public void OnClick() => this.onClick().ContinueWith(t =>
    {
        if (t.Exception != null)
            Android.Util.Log.Error("KmlCarPlay", t.Exception.ToString());
    });
}
