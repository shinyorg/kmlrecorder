using System.Globalization;
using System.Security;
using System.Text;
using Shiny.DocumentDb;

namespace ShinyKmlRecorder.Services.Impl;


[Singleton]
public class ObdExportService(
    IDocumentStore data,
    IObdManager obdManager,
    IExportService exportService,
    ILogger<ObdExportService> logger
) : IObdExportService
{
    public async Task<string> ExportToCsvAsync(Guid sessionId)
    {
        var (session, records) = await this.Load(sessionId);
        var parameters = GetParameters(session, records);

        var sb = new StringBuilder();
        var headers = new List<string> { "timestamp_utc", "latitude", "longitude" };
        headers.AddRange(parameters.Select(x => $"{x.Name} ({x.Unit})"));
        sb.AppendLine(String.Join(",", headers.Select(Escape)));

        foreach (var record in records)
        {
            var cells = new List<string>
            {
                record.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
                record.Latitude?.ToString(CultureInfo.InvariantCulture) ?? String.Empty,
                record.Longitude?.ToString(CultureInfo.InvariantCulture) ?? String.Empty
            };
            // a parameter the vehicle did not answer stays empty - never zero
            cells.AddRange(parameters.Select(x => record.Values.TryGetValue(x.Key, out var v)
                ? Format(v)
                : String.Empty
            ));
            sb.AppendLine(String.Join(",", cells));
        }

        return await this.Write(session, "csv", sb.ToString());
    }


    public async Task<string> ExportToKmlAsync(Guid sessionId)
    {
        var (session, records) = await this.Load(sessionId);
        var located = records
            .Where(x => x.Latitude.HasValue && x.Longitude.HasValue)
            .ToList();

        if (located.Count == 0)
            throw new InvalidOperationException("No samples in this session have a GPS position. Check in on the main screen while recording to capture GPS alongside the engine data.");

        var parameters = GetParameters(session, located);
        var title = $"OBD Recording - {session.StartTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        sb.AppendLine("  <Document>");
        sb.AppendLine($"    <name>{SecurityElement.Escape(title)}</name>");
        sb.AppendLine($"    <description>{SecurityElement.Escape(Describe(session))}</description>");

        sb.AppendLine("    <Style id=\"pointStyle\">");
        sb.AppendLine("      <IconStyle>");
        sb.AppendLine("        <scale>0.8</scale>");
        sb.AppendLine("        <Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon>");
        sb.AppendLine("      </IconStyle>");
        sb.AppendLine("      <LabelStyle>");
        sb.AppendLine("        <scale>0</scale>");
        sb.AppendLine("      </LabelStyle>");
        sb.AppendLine("    </Style>");

        sb.AppendLine("    <Style id=\"lineStyle\">");
        sb.AppendLine("      <LineStyle>");
        sb.AppendLine("        <color>ff0000ff</color>");
        sb.AppendLine("        <width>2</width>");
        sb.AppendLine("      </LineStyle>");
        sb.AppendLine("    </Style>");

        sb.AppendLine("    <Folder>");
        sb.AppendLine("      <name>Engine Data</name>");

        foreach (var record in located)
        {
            var available = parameters.Where(x => record.Values.ContainsKey(x.Key)).ToList();
            var coords = Coordinates(record);

            sb.AppendLine("      <Placemark>");
            sb.AppendLine($"        <name>{record.Timestamp.ToLocalTime():HH:mm:ss}</name>");
            sb.AppendLine("        <description>");
            foreach (var p in available)
                sb.AppendLine($"          {SecurityElement.Escape(p.Name)}: {Format(record.Values[p.Key])} {SecurityElement.Escape(p.Unit)}{Environment.NewLine}");
            sb.AppendLine("        </description>");
            sb.AppendLine($"        <TimeStamp><when>{record.Timestamp.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}</when></TimeStamp>");
            sb.AppendLine("        <styleUrl>#pointStyle</styleUrl>");
            sb.AppendLine("        <ExtendedData>");
            foreach (var p in available)
            {
                sb.AppendLine($"          <Data name=\"{p.Key}\">");
                sb.AppendLine($"            <displayName>{SecurityElement.Escape($"{p.Name} ({p.Unit})")}</displayName>");
                sb.AppendLine($"            <value>{Format(record.Values[p.Key])}</value>");
                sb.AppendLine("          </Data>");
            }
            sb.AppendLine("        </ExtendedData>");
            sb.AppendLine($"        <Point><coordinates>{coords}</coordinates></Point>");
            sb.AppendLine("      </Placemark>");
        }
        sb.AppendLine("    </Folder>");

        if (located.Count > 1)
        {
            sb.AppendLine("    <Placemark>");
            sb.AppendLine("      <name>Track</name>");
            sb.AppendLine("      <styleUrl>#lineStyle</styleUrl>");
            sb.AppendLine("      <LineString>");
            sb.AppendLine("        <coordinates>");
            foreach (var record in located)
                sb.AppendLine($"          {Coordinates(record)}");
            sb.AppendLine("        </coordinates>");
            sb.AppendLine("      </LineString>");
            sb.AppendLine("    </Placemark>");
        }

        sb.AppendLine("  </Document>");
        sb.AppendLine("</kml>");

        return await this.Write(session, "kml", sb.ToString());
    }


    async Task<(ObdSession Session, IList<ObdRecord> Records)> Load(Guid sessionId)
    {
        var session = await data.Get<ObdSession>(sessionId)
            ?? throw new InvalidOperationException("OBD session not found");

        var records = await obdManager.GetRecords(sessionId);
        if (records.Count == 0)
            throw new InvalidOperationException("No OBD data recorded in this session");

        return (session, records);
    }


    async Task<string> Write(ObdSession session, string extension, string content)
    {
        var fileName = $"obd_{session.StartTime.ToLocalTime():yyyyMMdd_HHmmss}.{extension}";
        var filePath = Path.Combine(exportService.ExportDirectory, fileName);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        logger.LogInformation($"OBD {extension} exported to {filePath}");

        return filePath;
    }


    // catalog order, limited to what the session asked for plus anything that actually came back
    static List<ObdParameter> GetParameters(ObdSession session, IEnumerable<ObdRecord> records)
    {
        var keys = new HashSet<string>(session.ParameterKeys);
        foreach (var record in records)
            keys.UnionWith(record.Values.Keys);

        return ObdParameters.All.Where(x => keys.Contains(x.Key)).ToList();
    }


    static string Describe(ObdSession session)
    {
        var parts = new List<string>();
        if (session.Vin != null)
            parts.Add($"VIN: {session.Vin}");

        if (session.DeviceName != null)
            parts.Add($"Adapter: {session.DeviceName} ({session.Transport})");

        if (session.Protocol != null)
            parts.Add($"Protocol: {session.Protocol}");

        return parts.Count == 0 ? "Engine data recorded by KML Recorder" : String.Join(" | ", parts);
    }


    static string Coordinates(ObdRecord record)
        => $"{record.Longitude!.Value.ToString(CultureInfo.InvariantCulture)},{record.Latitude!.Value.ToString(CultureInfo.InvariantCulture)},0";

    static string Format(double value) => Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);

    static string Escape(string value)
        => value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
