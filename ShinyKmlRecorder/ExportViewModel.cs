namespace ShinyKmlRecorder;

[ShellMap<ExportPage>]
public partial class ExportViewModel(
    IExportService exportService,
    IExportDeliveryService deliveryService,
    IExportSampleService sampleService,
    ILogService logService,
    BaseServices services
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] public partial int LogCount { get; set; }
    [ObservableProperty] public partial string ExportStatus { get; set; } = "Ready to export";
    [ObservableProperty] public partial string ExportSummary { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsExporting { get; set; }

    public void OnAppearing()
    {
        UpdateState();
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    Task NavToSettings() => services.Navigator.NavigateTo<ExportSettingsViewModel>();

    [RelayCommand]
    async Task ExportNow()
    {
        if (LogCount == 0)
        {
            await services.Navigator.Alert("No Data", "No GPS points available to export");
            return;
        }

        try
        {
            IsExporting = true;
            ExportStatus = "Exporting...";

            var filePath = sampleService.ExportFormat == ExportFormat.Kml
                ? await exportService.ExportToKmlAsync()
                : await exportService.ExportToGeoJsonAsync();

            await deliveryService.DeliverAsync(filePath, sampleService.ExportFormat, sampleService.ExportDelivery, sampleService);

            ExportStatus = "Export complete";
            await services.Navigator.Alert("Success", $"Exported to:\n{filePath}");
        }
        catch (Exception ex)
        {
            ExportStatus = $"Error: {ex.Message}";
            await services.Navigator.Alert("Export Failed", ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    async Task OpenExportFolder()
    {
        try
        {
            var exportDir = exportService.ExportDirectory;
            await Launcher.Default.TryOpenAsync($"file://{exportDir}");
        }
        catch (Exception ex)
        {
            await services.Navigator.Alert("Error", $"Could not open folder: {ex.Message}");
        }
    }

    void UpdateState()
    {
        UpdateSummary();
        _ = UpdateLogCountAsync();
    }

    async Task UpdateLogCountAsync()
    {
        var logs = await logService.GetLogs();
        if (sampleService.SelectedWorkId.HasValue)
            logs = logs.Where(x => x.WorkId == sampleService.SelectedWorkId.Value).ToList();

        LogCount = logs.Count(x => x.EventType == LogEventType.GpsPing);
    }

    void UpdateSummary()
    {
        var tripLabel = sampleService.SelectedWorkId.HasValue ? "Selected trip" : "All trips";
        ExportSummary = $"{tripLabel} | {sampleService.ExportFormat} | {sampleService.ExportDelivery}";
    }
}
