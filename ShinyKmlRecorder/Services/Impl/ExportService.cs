using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShinyKmlRecorder.Services.Impl;

[Singleton]
public class ExportService(
    MySqliteConnection data,
    IPlatform platform,
    ILogger<ExportService> logger,
    IExportSampleService sampleService
) : IExportService
{
    readonly string exportDir = Path.Combine(platform.AppData.FullName, "exports");

    public string ExportDirectory
    {
        get
        {
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);
            return exportDir;
        }
    }

    public async Task<string> ExportToKmlAsync()
    {
        var logs = await data.Logs.ToListAsync();
        var gpsLogs = ApplyFilters(logs);

        if (!gpsLogs.Any())
        {
            logger.LogWarning("No GPS logs found to export to KML");
            throw new InvalidOperationException("No GPS data available to export");
        }

        var kml = GenerateKml(gpsLogs);
        var fileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.kml";
        var filePath = Path.Combine(ExportDirectory, fileName);

        await File.WriteAllTextAsync(filePath, kml, Encoding.UTF8);
        logger.LogInformation($"KML exported to {filePath}");
        
        return filePath;
    }

    public async Task<string> ExportToGeoJsonAsync()
    {
        var logs = await data.Logs.ToListAsync();
        var gpsLogs = ApplyFilters(logs);

        if (!gpsLogs.Any())
        {
            logger.LogWarning("No GPS logs found to export to GeoJSON");
            throw new InvalidOperationException("No GPS data available to export");
        }

        var geoJson = GenerateGeoJson(gpsLogs);
        var fileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.geojson";
        var filePath = Path.Combine(ExportDirectory, fileName);

        await File.WriteAllTextAsync(filePath, geoJson, Encoding.UTF8);
        logger.LogInformation($"GeoJSON exported to {filePath}");
        
        return filePath;
    }

    List<LogRecord> ApplyFilters(List<LogRecord> logs)
    {
        var filtered = logs
            .Where(x => x.EventType == LogEventType.GpsPing && x.Latitude != 0 && x.Longitude != 0);

        if (sampleService.SelectedWorkId.HasValue)
            filtered = filtered.Where(x => x.WorkId == sampleService.SelectedWorkId.Value);

        return filtered
            .OrderBy(x => x.Timestamp)
            .ToList();
    }

    private string GenerateKml(List<LogRecord> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        sb.AppendLine("  <Document>");
        sb.AppendLine($"    <name>GPS Recording - {DateTime.Now:yyyy-MM-dd HH:mm:ss}</name>");
        sb.AppendLine("    <description>GPS points recorded by KML Recorder</description>");

        // Add styles
        sb.AppendLine("    <Style id=\"pointStyle\">");
        sb.AppendLine("      <IconStyle>");
        sb.AppendLine("        <scale>1.0</scale>");
        sb.AppendLine("        <Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon>");
        sb.AppendLine("      </IconStyle>");
        sb.AppendLine("      <LabelStyle>");
        sb.AppendLine("        <scale>0.7</scale>");
        sb.AppendLine("      </LabelStyle>");
        sb.AppendLine("    </Style>");

        sb.AppendLine("    <Style id=\"lineStyle\">");
        sb.AppendLine("      <LineStyle>");
        sb.AppendLine("        <color>ff0000ff</color>");
        sb.AppendLine("        <width>2</width>");
        sb.AppendLine("      </LineStyle>");
        sb.AppendLine("    </Style>");

        // Add Folder for points
        sb.AppendLine("    <Folder>");
        sb.AppendLine("      <name>GPS Points</name>");
        
        foreach (var log in logs)
        {
            sb.AppendLine("      <Placemark>");
            sb.AppendLine($"        <name>Point - {log.Timestamp:yyyy-MM-dd HH:mm:ss}</name>");
            sb.AppendLine("        <description>");
            sb.AppendLine($"          Latitude: {log.Latitude}{Environment.NewLine}");
            sb.AppendLine($"          Longitude: {log.Longitude}{Environment.NewLine}");
            sb.AppendLine($"          Accuracy: {log.PositionAccuracy}m{Environment.NewLine}");
            sb.AppendLine($"          Speed: {(log.Speed * 3.6):F2} km/h ({log.Speed:F2} m/s){Environment.NewLine}");
            sb.AppendLine($"          Time: {log.Timestamp:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
            sb.AppendLine($"          Battery: {(log.BatteryLevel * 100):F0}% ({log.BatteryStatus}){Environment.NewLine}");
            sb.AppendLine("        </description>");
            sb.AppendLine("        <styleUrl>#pointStyle</styleUrl>");
            sb.AppendLine("        <Point>");
            sb.AppendLine($"          <coordinates>{log.Longitude.ToString(CultureInfo.InvariantCulture)},{log.Latitude.ToString(CultureInfo.InvariantCulture)},0</coordinates>");
            sb.AppendLine("        </Point>");
            sb.AppendLine("      </Placemark>");
        }

        sb.AppendLine("      </Folder>");

        // Add LineString for the track
        if (logs.Count > 1)
        {
            sb.AppendLine("    <Placemark>");
            sb.AppendLine("      <name>Track</name>");
            sb.AppendLine("      <description>GPS track showing the route</description>");
            sb.AppendLine("      <styleUrl>#lineStyle</styleUrl>");
            sb.AppendLine("      <LineString>");
            sb.AppendLine("        <coordinates>");
            
            foreach (var log in logs)
            {
                sb.AppendLine($"          {log.Longitude.ToString(CultureInfo.InvariantCulture)},{log.Latitude.ToString(CultureInfo.InvariantCulture)},0");
            }
            
            sb.AppendLine("        </coordinates>");
            sb.AppendLine("      </LineString>");
            sb.AppendLine("    </Placemark>");
        }

        sb.AppendLine("  </Document>");
        sb.AppendLine("</kml>");

        return sb.ToString();
    }

    static string GenerateGeoJson(List<LogRecord> logs)
    {
        var features = new List<GeoJsonFeature>();

        // Add point features
        foreach (var log in logs)
        {
            var properties = new Dictionary<string, object>
            {
                { "timestamp", log.Timestamp },
                { "accuracy", log.PositionAccuracy },
                { "speed_ms", log.Speed },
                { "speed_kmh", log.Speed * 3.6 },
                { "battery_level", log.BatteryLevel * 100 },
                { "battery_status", log.BatteryStatus.ToString() },
                { "is_energy_saver_on", log.IsEnergySaverOn }
            };

            var feature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = new GeoJsonGeometry
                {
                    Type = "Point",
                    Coordinates = new[] { log.Longitude, log.Latitude }
                },
                Properties = properties
            };

            features.Add(feature);
        }

        // Add LineString feature for the track
        if (logs.Count > 1)
        {
            var coordinates = logs
                .Select(x => new[] { x.Longitude, x.Latitude })
                .ToArray();

            var trackFeature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = new GeoJsonGeometry
                {
                    Type = "LineString",
                    Coordinates = coordinates
                },
                Properties = new Dictionary<string, object>
                {
                    { "name", "GPS Track" },
                    { "point_count", logs.Count }
                }
            };

            features.Add(trackFeature);
        }

        var featureCollection = new GeoJsonFeatureCollection
        {
            Type = "FeatureCollection",
            Features = features
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(featureCollection, options);
    }

    // GeoJSON helper classes
    class GeoJsonFeatureCollection
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("features")]
        public required List<GeoJsonFeature> Features { get; set; }
    }

    class GeoJsonFeature
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("geometry")]
        public required GeoJsonGeometry Geometry { get; set; }

        [JsonPropertyName("properties")]
        public required Dictionary<string, object> Properties { get; set; }
    }

    class GeoJsonGeometry
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("coordinates")]
        public required object Coordinates { get; set; }
    }
}
