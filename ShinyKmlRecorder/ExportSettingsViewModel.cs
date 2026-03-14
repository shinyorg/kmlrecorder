using System.Collections.ObjectModel;
using Shiny.SqliteDocumentDb;

namespace ShinyKmlRecorder;

[ShellMap<ExportSettingsPage>]
public partial class ExportSettingsViewModel(
    IExportSampleService sampleService,
    IDocumentStore data,
    BaseServices services
) : ObservableObject, IPageLifecycleAware
{
    public ObservableCollection<TripOption> Trips { get; } = new();
    public IReadOnlyList<ExportFormat> Formats { get; } = new[] { ExportFormat.Kml, ExportFormat.GeoJson };
    public IReadOnlyList<ExportDelivery> Deliveries { get; } = new[] { ExportDelivery.LocalOnly, ExportDelivery.AzureBlob, ExportDelivery.Email, ExportDelivery.Share };

    [ObservableProperty] public partial TripOption? SelectedTrip { get; set; }
    [ObservableProperty] public partial ExportFormat SelectedFormat { get; set; }
    [ObservableProperty] public partial ExportDelivery SelectedDelivery { get; set; }

    [ObservableProperty] public partial string AzureAccountUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial string AzureContainerName { get; set; } = string.Empty;
    [ObservableProperty] public partial string AzureSasToken { get; set; } = string.Empty;

    [ObservableProperty] public partial string EmailTo { get; set; } = string.Empty;
    [ObservableProperty] public partial string EmailSubject { get; set; } = string.Empty;
    [ObservableProperty] public partial string EmailBody { get; set; } = string.Empty;

    public void OnAppearing()
    {
        LoadSettings();
        _ = LoadTripsAsync();
    }

    public void OnDisappearing()
    {
        SaveSettings();
    }

    async Task LoadTripsAsync()
    {
        Trips.Clear();
        Trips.Add(TripOption.AllTrips());

        var logs = await data.Query<LogRecord>().ToList();
        var trips = logs
            .Where(x => x.WorkId != Guid.Empty)
            .GroupBy(x => x.WorkId)
            .Select(g => new TripOption(
                g.Key,
                g.Min(x => x.Timestamp),
                g.Max(x => x.Timestamp),
                g.Count(x => x.EventType == LogEventType.GpsPing)
            ))
            .OrderByDescending(x => x.Start)
            .ToList();

        foreach (var trip in trips)
            Trips.Add(trip);

        SelectedTrip = sampleService.SelectedWorkId.HasValue
            ? Trips.FirstOrDefault(x => x.WorkId == sampleService.SelectedWorkId.Value) ?? Trips[0]
            : Trips[0];
    }

    void LoadSettings()
    {
        SelectedFormat = sampleService.ExportFormat;
        SelectedDelivery = sampleService.ExportDelivery;

        AzureAccountUrl = sampleService.AzureAccountUrl ?? string.Empty;
        AzureContainerName = sampleService.AzureContainerName ?? string.Empty;
        AzureSasToken = sampleService.AzureSasToken ?? string.Empty;

        EmailTo = sampleService.EmailTo ?? string.Empty;
        EmailSubject = sampleService.EmailSubject ?? string.Empty;
        EmailBody = sampleService.EmailBody ?? string.Empty;
    }

    void SaveSettings()
    {
        sampleService.SelectedWorkId = SelectedTrip?.WorkId;
        sampleService.ExportFormat = SelectedFormat;
        sampleService.ExportDelivery = SelectedDelivery;

        sampleService.AzureAccountUrl = AzureAccountUrl;
        sampleService.AzureContainerName = AzureContainerName;
        sampleService.AzureSasToken = AzureSasToken;

        sampleService.EmailTo = EmailTo;
        sampleService.EmailSubject = EmailSubject;
        sampleService.EmailBody = EmailBody;
    }

    partial void OnSelectedTripChanged(TripOption? value)
    {
        sampleService.SelectedWorkId = value?.WorkId;
    }

    partial void OnSelectedFormatChanged(ExportFormat value)
    {
        sampleService.ExportFormat = value;
    }

    partial void OnSelectedDeliveryChanged(ExportDelivery value)
    {
        sampleService.ExportDelivery = value;
    }

    [RelayCommand]
    async Task ResetFilters()
    {
        var confirm = await services.Dialogs.Confirm(
            "Reset Settings", 
            "Are you sure you want to reset all export settings to defaults?"
        );
        
        if (confirm)
        {
            sampleService.ResetFilters();
            LoadSettings();
            await LoadTripsAsync();
            await services.Dialogs.Alert("Done", "All export settings have been reset");
        }
    }
}

public record TripOption(Guid? WorkId, DateTimeOffset Start, DateTimeOffset End, int Points)
{
    public string Label => WorkId == null
        ? "All Trips"
        : $"{Start.LocalDateTime:yyyy-MM-dd HH:mm} - {End.LocalDateTime:HH:mm} ({Points} pts)";

    public static TripOption AllTrips() => new(null, DateTimeOffset.MinValue, DateTimeOffset.MinValue, 0);
}
