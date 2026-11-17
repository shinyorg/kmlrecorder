namespace ShinyKmlRecorder;

[ShellMap<TripMapPage>]
public partial class TripMapViewModel(
    BaseServices services
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] public partial Guid WorkId { get; set; }
    [ObservableProperty] public partial IList<LogRecord>? Points { get; set; }
    [ObservableProperty] public partial int PointCount { get; set; }

    [RelayCommand]
    async Task Load()
    {
        var points = await services.Logs.GetTripPoints(this.WorkId);
        if (points.Count == 0)
            return;

        this.PointCount = points.Count;
        this.Points = points;
    }

    public void OnAppearing() => this.LoadCommand.Execute(null);
    public void OnDisappearing() { }
}
