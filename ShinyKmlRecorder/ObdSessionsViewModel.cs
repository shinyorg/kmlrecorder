namespace ShinyKmlRecorder;


[ShellMap<ObdSessionsPage>]
public partial class ObdSessionsViewModel(
    BaseServices services,
    IObdManager obdManager,
    IObdExportService obdExportService,
    IExportDeliveryService deliveryService,
    IExportSampleService sampleService
) : ObservableObject, IPageLifecycleAware
{
    const string ExportCsv = "Export CSV";
    const string ExportKml = "Export KML (GPS samples)";
    const string Delete = "Delete";

    public IList<ObdSessionItem> Sessions { get; private set; } = [];
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial string DeliverySummary { get; private set; } = String.Empty;

    [RelayCommand] Task NavToExportSettings() => services.Navigator.NavigateTo<ExportSettingsViewModel>();


    [RelayCommand]
    async Task Load()
    {
        var sessions = await obdManager.GetSessions();
        this.Sessions = sessions
            .Select(x => new ObdSessionItem(
                x.Session.Id,
                x.Session.StartTime.ToLocalTime(),
                x.Session.EndTime?.ToLocalTime(),
                x.RecordCount,
                x.Session.Vin ?? x.Session.DeviceName ?? x.Session.Transport.ToString(),
                x.Session.Id == obdManager.CurrentSessionId
            ))
            .ToList();

        this.OnPropertyChanged(nameof(this.Sessions));
    }


    [RelayCommand]
    async Task Select(ObdSessionItem item)
    {
        var action = item.IsActive
            ? await services.Dialogs.ActionSheet("OBD Session", "Cancel", null, ExportCsv, ExportKml)
            : await services.Dialogs.ActionSheet("OBD Session", "Cancel", Delete, ExportCsv, ExportKml);

        switch (action)
        {
            case ExportCsv:
                await this.Export(item, ExportFormat.Csv);
                break;

            case ExportKml:
                await this.Export(item, ExportFormat.Kml);
                break;

            case Delete:
                var confirm = await services.Dialogs.Confirm("Delete Session", $"Delete {item.RecordCount} samples recorded {item.StartTime:MMM d, h:mm tt}?");
                if (confirm)
                {
                    await obdManager.DeleteSession(item.SessionId);
                    await this.Load();
                }
                break;
        }
    }


    public void OnAppearing()
    {
        this.DeliverySummary = $"Exports are delivered using: {sampleService.ExportDelivery}";
        this.LoadCommand.Execute(null);
    }


    public void OnDisappearing() { }


    async Task Export(ObdSessionItem item, ExportFormat format)
    {
        if (item.RecordCount == 0)
        {
            await services.Dialogs.Alert("No Data", "No OBD samples were recorded in this session");
            return;
        }

        try
        {
            this.IsBusy = true;
            var filePath = format == ExportFormat.Csv
                ? await obdExportService.ExportToCsvAsync(item.SessionId)
                : await obdExportService.ExportToKmlAsync(item.SessionId);

            await deliveryService.DeliverAsync(filePath, format, sampleService.ExportDelivery, sampleService);
            await services.Dialogs.Alert("Success", $"Exported to:\n{filePath}");
        }
        catch (Exception ex)
        {
            await services.Dialogs.Alert("Export Failed", ex.Message);
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}


public record ObdSessionItem(
    Guid SessionId,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    int RecordCount,
    string Vehicle,
    bool IsActive
);
