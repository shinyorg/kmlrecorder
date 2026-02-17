namespace ShinyKmlRecorder.Services.Impl;

[Singleton]
public class ExportSampleService : IExportSampleService
{
    const string KeySelectedWorkId = "ExportSelectedWorkId";
    const string KeyFormat = "ExportFormat";
    const string KeyDelivery = "ExportDelivery";
    const string KeyAzureAccountUrl = "ExportAzureAccountUrl";
    const string KeyAzureContainerName = "ExportAzureContainerName";
    const string KeyAzureSasToken = "ExportAzureSasToken";
    const string KeyEmailTo = "ExportEmailTo";
    const string KeyEmailSubject = "ExportEmailSubject";
    const string KeyEmailBody = "ExportEmailBody";

    public Guid? SelectedWorkId
    {
        get
        {
            var value = Preferences.Default.Get(KeySelectedWorkId, string.Empty);
            return Guid.TryParse(value, out var id) ? id : null;
        }
        set
        {
            if (value.HasValue)
                Preferences.Default.Set(KeySelectedWorkId, value.Value.ToString("D"));
            else
                Preferences.Default.Remove(KeySelectedWorkId);
        }
    }

    public ExportFormat ExportFormat
    {
        get => (ExportFormat)Preferences.Default.Get(KeyFormat, (int)ExportFormat.Kml);
        set => Preferences.Default.Set(KeyFormat, (int)value);
    }

    public ExportDelivery ExportDelivery
    {
        get => (ExportDelivery)Preferences.Default.Get(KeyDelivery, (int)ExportDelivery.LocalOnly);
        set => Preferences.Default.Set(KeyDelivery, (int)value);
    }

    public string? AzureAccountUrl
    {
        get => Preferences.Default.Get(KeyAzureAccountUrl, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyAzureAccountUrl, value);
    }

    public string? AzureContainerName
    {
        get => Preferences.Default.Get(KeyAzureContainerName, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyAzureContainerName, value);
    }

    public string? AzureSasToken
    {
        get => Preferences.Default.Get(KeyAzureSasToken, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyAzureSasToken, value);
    }

    public string? EmailTo
    {
        get => Preferences.Default.Get(KeyEmailTo, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyEmailTo, value);
    }

    public string? EmailSubject
    {
        get => Preferences.Default.Get(KeyEmailSubject, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyEmailSubject, value);
    }

    public string? EmailBody
    {
        get => Preferences.Default.Get(KeyEmailBody, string.Empty) is { Length: > 0 } v ? v : null;
        set => SetOrRemove(KeyEmailBody, value);
    }

    public void ResetFilters()
    {
        Preferences.Default.Remove(KeySelectedWorkId);
        Preferences.Default.Remove(KeyFormat);
        Preferences.Default.Remove(KeyDelivery);
        Preferences.Default.Remove(KeyAzureAccountUrl);
        Preferences.Default.Remove(KeyAzureContainerName);
        Preferences.Default.Remove(KeyAzureSasToken);
        Preferences.Default.Remove(KeyEmailTo);
        Preferences.Default.Remove(KeyEmailSubject);
        Preferences.Default.Remove(KeyEmailBody);
    }

    static void SetOrRemove(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Preferences.Default.Remove(key);
        else
            Preferences.Default.Set(key, value);
    }
}

