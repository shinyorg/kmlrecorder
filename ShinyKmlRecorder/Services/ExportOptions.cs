namespace ShinyKmlRecorder.Services;

public enum ExportFormat
{
    Kml = 0,
    GeoJson = 1,
    Csv = 2
}

public enum ExportDelivery
{
    LocalOnly = 0,
    AzureBlob = 1,
    Email = 2,
    Share = 3
}

