namespace ShinyKmlRecorder;


[ShellMap<ObdParametersPage>]
public partial class ObdParametersViewModel(
    BaseServices services,
    IObdManager obdManager,
    ObdSettings settings
) : ObservableObject, IPageLifecycleAware
{
    bool loading;

    public IList<ObdParameterItem> Parameters { get; private set; } = [];
    [ObservableProperty] public partial int PollIntervalSeconds { get; set; }


    public void OnAppearing()
    {
        this.loading = true;
        this.PollIntervalSeconds = settings.PollIntervalSeconds;

        var selected = settings.SelectedParameters;
        var supported = obdManager.SupportedPids;
        this.Parameters = ObdParameters
            .All
            .Select(x => new ObdParameterItem(x, selected.Contains(x), Describe(x, supported), this.Save))
            .ToList();

        this.OnPropertyChanged(nameof(this.Parameters));
        this.loading = false;
    }


    public void OnDisappearing() { }


    partial void OnPollIntervalSecondsChanged(int value) => settings.PollIntervalSeconds = Math.Max(1, value);


    [RelayCommand]
    async Task ResetDefaults()
    {
        var confirm = await services.Dialogs.Confirm(
            "Reset Parameters",
            "Reset the recorded parameters to the default set?"
        );
        if (!confirm)
            return;

        settings.SelectedParameterKeys = null;
        settings.PollIntervalSeconds = 1;
        this.OnAppearing();
    }


    void Save()
    {
        if (this.loading)
            return;

        settings.SelectedParameters = this.Parameters
            .Where(x => x.IsSelected)
            .Select(x => x.Parameter)
            .ToList();
    }


    static string Describe(ObdParameter parameter, IReadOnlySet<byte> supported)
    {
        var description = $"PID 01{parameter.Pid:X2} · {parameter.Unit}";
        if (supported.Count == 0)
            return description;

        return supported.Contains(parameter.Pid)
            ? $"{description} · supported"
            : $"{description} · not supported by this vehicle";
    }
}


public partial class ObdParameterItem(
    ObdParameter parameter,
    bool isSelected,
    string description,
    Action onChanged
) : ObservableObject
{
    public ObdParameter Parameter => parameter;
    public string Name => parameter.Name;
    public string Description => description;

    [ObservableProperty] public partial bool IsSelected { get; set; } = isSelected;

    partial void OnIsSelectedChanged(bool value) => onChanged();
}
