namespace ShinyKmlRecorder;

[ShellMap<TripsPage>]
public partial class TripsViewModel(
    BaseServices services
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] public partial IList<TripSummary>? Trips { get; set; }

    [RelayCommand]
    async Task Load()
    {
        var trips = await services.Logs.GetTrips();
        foreach (var trip in trips)
        {
            trip.StartDate = trip.StartDate.ToLocalTime();
            if (trip.EndDate.HasValue)
                trip.EndDate = trip.EndDate.Value.ToLocalTime();
        }
        this.Trips = trips;
    }

    [RelayCommand]
    Task SelectTrip(TripSummary trip)
        => services.Navigator.NavigateTo<TripMapViewModel>(vm => vm.WorkId = trip.WorkId);

    public void OnAppearing() => this.LoadCommand.Execute(null);
    public void OnDisappearing() { }
}
