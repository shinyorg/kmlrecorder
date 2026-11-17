namespace ShinyKmlRecorder.Services;

public interface IObdExportService
{
    /// <summary>
    /// Exports an OBD session as CSV - one row per sample, one column per parameter
    /// </summary>
    /// <returns>Path to the generated CSV file</returns>
    Task<string> ExportToCsvAsync(Guid sessionId);

    /// <summary>
    /// Exports the GPS located samples of an OBD session as KML with the engine data attached to each placemark
    /// </summary>
    /// <returns>Path to the generated KML file</returns>
    Task<string> ExportToKmlAsync(Guid sessionId);
}
