
namespace ShinyKmlRecorder;

[ShellMap<LogsPage>]
public partial class LogsViewModel(
    BaseServices services, 
    ILogger<LogsViewModel> logger
) : ObservableObject, IPageLifecycleAware
{
    public IList<LogRecord> Logs { get; private set; } = null!;

    [RelayCommand]
    async Task Load()
    {
        var logs = await services.Logs.GetLogs();
        foreach (var log in logs)
        {
            log.Timestamp = log.Timestamp.ToLocalTime();
            if (log.GpsTimestamp.HasValue)
                log.GpsTimestamp = log.GpsTimestamp.Value.ToLocalTime();
        }
        this.Logs = logs;
        this.OnPropertyChanged(nameof(this.Logs));
    }

    public void OnAppearing()
    {
        this.LoadCommand.Execute(null);
    }

    public void OnDisappearing()
    {
    }
}