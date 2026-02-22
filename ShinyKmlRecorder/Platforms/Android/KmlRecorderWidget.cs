using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Microsoft.Maui;
using Shiny.Locations;

namespace ShinyKmlRecorder;

[BroadcastReceiver(
    Label = "KML Recorder",
    Exported = true
)]
[IntentFilter(["android.appwidget.action.APPWIDGET_UPDATE"])]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_info")]
public class KmlRecorderWidget : AppWidgetProvider
{
    const string ActionToggle = "org.shiny.kmlrecorder.TOGGLE";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context == null || appWidgetManager == null || appWidgetIds == null)
            return;

        foreach (var id in appWidgetIds)
            UpdateWidget(context, appWidgetManager, id);
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        base.OnReceive(context, intent);

        if (context == null || intent == null)
            return;

        if (intent.Action == ActionToggle)
        {
            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetAction(ActionToggle);
            launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
            context.StartActivity(launchIntent);

            // refresh all widget instances
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(KmlRecorderWidget)));
            var ids = appWidgetManager?.GetAppWidgetIds(componentName);
            if (ids != null && appWidgetManager != null)
            {
                foreach (var id in ids)
                    UpdateWidget(context, appWidgetManager, id);
            }
        }
    }

    static void UpdateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
    {
        var isRecording = false;
        string? sinceText = null;

        if (IPlatformApplication.Current?.Services != null)
        {
            var logService = IPlatformApplication.Current.Services.GetService<ILogService>();
            if (logService != null)
            {
                isRecording = logService.DateCheckedIn != null;
                if (isRecording && logService.DateCheckedIn is { } checkedIn)
                {
                    var elapsed = DateTimeOffset.UtcNow - checkedIn;
                    sinceText = elapsed.TotalHours >= 1
                        ? $"Since {elapsed.Hours}h {elapsed.Minutes}m ago"
                        : $"Since {elapsed.Minutes}m ago";
                }
            }
        }

        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);
        views.SetTextViewText(Resource.Id.widget_status, isRecording ? "📍 Recording" : "Stopped");
        views.SetImageViewResource(
            Resource.Id.widget_toggle,
            isRecording
                ? Android.Resource.Drawable.IcMediaPause
                : Android.Resource.Drawable.IcMediaPlay
        );

        if (sinceText != null)
        {
            views.SetTextViewText(Resource.Id.widget_since, sinceText);
            views.SetViewVisibility(Resource.Id.widget_since, Android.Views.ViewStates.Visible);
        }
        else
        {
            views.SetViewVisibility(Resource.Id.widget_since, Android.Views.ViewStates.Gone);
        }

        var toggleIntent = new Intent(context, typeof(KmlRecorderWidget));
        toggleIntent.SetAction(ActionToggle);
        var pendingIntent = PendingIntent.GetBroadcast(
            context, 0, toggleIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );
        views.SetOnClickPendingIntent(Resource.Id.widget_toggle, pendingIntent);

        appWidgetManager.UpdateAppWidget(appWidgetId, views);
    }

    public static void UpdateAllWidgets(Context context)
    {
        var appWidgetManager = AppWidgetManager.GetInstance(context);
        var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(KmlRecorderWidget)));
        var ids = appWidgetManager?.GetAppWidgetIds(componentName);
        if (ids != null && appWidgetManager != null)
        {
            foreach (var id in ids)
                UpdateWidget(context, appWidgetManager, id);
        }
    }
}
