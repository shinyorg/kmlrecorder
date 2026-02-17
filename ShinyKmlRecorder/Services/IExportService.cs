namespace ShinyKmlRecorder.Services;

public interface IExportService
{
    /// <summary>
    /// Exports all GPS logs to KML format and saves to a local file
    /// </summary>
    /// <returns>Path to the generated KML file</returns>
    Task<string> ExportToKmlAsync();

    /// <summary>
    /// Exports all GPS logs to GeoJSON format and saves to a local file
    /// </summary>
    /// <returns>Path to the generated GeoJSON file</returns>
    Task<string> ExportToGeoJsonAsync();

    /// <summary>
    /// Gets the directory where export files are stored
    /// </summary>
    string ExportDirectory { get; }
}

