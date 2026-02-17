namespace ShinyKmlRecorder.Services;

public interface IExportDeliveryService
{
    Task DeliverAsync(string filePath, ExportFormat format, ExportDelivery delivery, IExportSampleService settings);
}

