using System.Net.Http.Headers;

namespace ShinyKmlRecorder.Services.Impl;

[Singleton]
public class ExportDeliveryService : IExportDeliveryService
{
    public async Task DeliverAsync(string filePath, ExportFormat format, ExportDelivery delivery, IExportSampleService settings)
    {
        switch (delivery)
        {
            case ExportDelivery.LocalOnly:
                return;
            case ExportDelivery.Email:
                await SendEmailAsync(filePath, settings);
                return;
            case ExportDelivery.Share:
                await ShareAsync(filePath);
                return;
            case ExportDelivery.AzureBlob:
                await UploadToAzureAsync(filePath, format, settings);
                return;
            default:
                throw new InvalidOperationException("Unsupported delivery option");
        }
    }

    static async Task SendEmailAsync(string filePath, IExportSampleService settings)
    {
        var message = new EmailMessage
        {
            Subject = string.IsNullOrWhiteSpace(settings.EmailSubject)
                ? "GPS Export"
                : settings.EmailSubject,
            Body = settings.EmailBody ?? string.Empty,
            To = string.IsNullOrWhiteSpace(settings.EmailTo)
                ? new List<string>()
                : new List<string> { settings.EmailTo },
            Attachments = new List<EmailAttachment> { new(filePath) }
        };

        await Email.Default.ComposeAsync(message);
    }

    static Task ShareAsync(string filePath)
        => Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share GPS Export",
            File = new ShareFile(filePath)
        });

    static async Task UploadToAzureAsync(string filePath, ExportFormat format, IExportSampleService settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AzureAccountUrl))
            throw new InvalidOperationException("Azure account URL is required");

        if (string.IsNullOrWhiteSpace(settings.AzureContainerName))
            throw new InvalidOperationException("Azure container name is required");

        if (string.IsNullOrWhiteSpace(settings.AzureSasToken))
            throw new InvalidOperationException("Azure SAS token is required");

        var blobName = Path.GetFileName(filePath);
        var baseUrl = settings.AzureAccountUrl.TrimEnd('/');
        var sas = settings.AzureSasToken.Trim();
        if (!sas.StartsWith("?", StringComparison.Ordinal))
            sas = "?" + sas;

        var blobUrl = $"{baseUrl}/{settings.AzureContainerName}/{Uri.EscapeDataString(blobName)}{sas}";

        using var client = new HttpClient();
        using var fileStream = File.OpenRead(filePath);
        using var content = new StreamContent(fileStream);

        content.Headers.ContentType = new MediaTypeHeaderValue(format == ExportFormat.Kml
            ? "application/vnd.google-earth.kml+xml"
            : "application/geo+json");

        using var request = new HttpRequestMessage(HttpMethod.Put, blobUrl);
        request.Content = content;
        request.Headers.Add("x-ms-blob-type", "BlockBlob");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
