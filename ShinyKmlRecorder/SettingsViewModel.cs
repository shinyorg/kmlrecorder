namespace ShinyKmlRecorder;


[ShellMap<SettingsPage>]
public partial class SettingsViewModel(
    BaseServices services,
    GpsSettings settings
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] public partial double MinimumDistanceMeters { get; set; }
    [ObservableProperty] public partial int MinimumTimeSeconds { get; set; }
    [ObservableProperty] public partial double MaximumDistanceMeters { get; set; }
    [ObservableProperty] public partial int MaximumTimeSeconds { get; set; }

    public void OnAppearing()
    {
        this.MinimumDistanceMeters = settings.MinimumDistanceMeters;
        this.MinimumTimeSeconds = settings.MinimumTimeSeconds;
        this.MaximumDistanceMeters = settings.MaximumDistanceMeters;
        this.MaximumTimeSeconds = settings.MaximumTimeSeconds;
    }

    public void OnDisappearing() { }

    partial void OnMinimumDistanceMetersChanged(double value) => settings.MinimumDistanceMeters = value;
    partial void OnMinimumTimeSecondsChanged(int value) => settings.MinimumTimeSeconds = value;
    partial void OnMaximumDistanceMetersChanged(double value) => settings.MaximumDistanceMeters = value;
    partial void OnMaximumTimeSecondsChanged(int value) => settings.MaximumTimeSeconds = value;

    [RelayCommand]
    async Task ResetDefaults()
    {
        var confirm = await services.Dialogs.Confirm(
            "Reset GPS Settings",
            "Reset all GPS filter values to defaults?"
        );
        if (!confirm)
            return;

        this.MinimumDistanceMeters = 10;
        this.MinimumTimeSeconds = 5;
        this.MaximumDistanceMeters = 0;
        this.MaximumTimeSeconds = 0;
    }
}
