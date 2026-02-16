using Shiny.Locations;

namespace ShinyKmlRecorder;


[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(
    BaseServices services,
    IAppInfo appInfo,
    IGpsManager gpsManager
) : ObservableObject, IPageLifecycleAware
{
    public string AppVersion => appInfo.VersionString;
    [ObservableProperty] public partial string ActionText { get; private set; }
    [ObservableProperty] public partial AccessState GpsAccess { get; private set; }
    [ObservableProperty] public partial bool IsGpsActive { get; private set; }
    [ObservableProperty] public partial DateTimeOffset? DateCheckedIn { get; private set; }

    [RelayCommand] void OpenAppSettings() => appInfo.ShowSettingsUI();
    [RelayCommand] Task NavToLogs() => services.Navigator.NavigateToLogs();
    [RelayCommand]
    async Task ToggleTracking()
    {
        if (services.Logs.DateCheckedIn != null)
        {
            if (gpsManager.IsListening())
                await gpsManager.StopListener();
            
            await services.Logs.Checkout();
        }
        else
        {
            try
            {
                await services.Logs.Checkin();
                
                if (!gpsManager.IsListening())
                    await gpsManager.StartListener(GpsRequest.Realtime(true));
                
                this.UpdateState();
            }
            catch (Exception ex)
            {
                await services.Navigator.Alert(
                    "Error starting GPS",
                    ex.ToString()
                );
            }
        }
        
        this.UpdateState();
    }

    [RelayCommand]
    async Task ClearLogs()
    {
        var confirm = await services.Navigator.Confirm("Confirm", "Are you sure you want to clear all the logs?");
        if (confirm)
        {
            await services.Logs.ClearLogs();
            await services.Navigator.Alert("Done", "All Logs cleared");
        }
    }

    public void OnAppearing()
    {
        this.UpdateState();
    }

    public void OnDisappearing()
    {
    }
    
    void UpdateState()
    {
        this.GpsAccess = gpsManager.GetCurrentStatus(GpsRequest.Realtime(true));
        this.IsGpsActive = gpsManager.IsListening();
        this.DateCheckedIn = services.Logs.DateCheckedIn?.ToLocalTime();
        
        this.ActionText = services.Logs.DateCheckedIn != null
            ? "Check Out"
            : "Check In";
    }
}