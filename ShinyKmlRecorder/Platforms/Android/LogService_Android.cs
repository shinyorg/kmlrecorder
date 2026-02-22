namespace ShinyKmlRecorder.Services.Impl;

public partial class LogService
{
    partial void SyncWidgetState()
    {
        var context = global::Android.App.Application.Context;
        KmlRecorderWidget.UpdateAllWidgets(context);
    }
}
